param([Parameter(Mandatory=$true)][string]$Binary,[Parameter(Mandatory=$true)][string]$Output,[switch]$DevMode)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName PresentationFramework,PresentationCore,WindowsBase,System.Drawing
$assembly=[Reflection.Assembly]::LoadFrom($Binary);$type=$assembly.GetType('Awl.Shell')
$flags=[Reflection.BindingFlags]'Instance,NonPublic';$static=[Reflection.BindingFlags]'Static,NonPublic'
$Output=[IO.Path]::GetFullPath($Output);$root=Join-Path $Output 'profile';[IO.Directory]::CreateDirectory($root)|Out-Null
Copy-Item -LiteralPath "$env:LOCALAPPDATA\Awl\config.json" -Destination $root -Force
$type.GetField('Root',$static).SetValue($null,$root)
if($DevMode){$type.GetField('devMode',$static).SetValue($null,$true);$type.GetField('devLibraryRoot',$static).SetValue($null,(Join-Path $root 'Library'))}
$shell=[Activator]::CreateInstance($type);$shell.ShutdownMode=[Windows.ShutdownMode]::OnExplicitShutdown
function Call($name,[object[]]$values=@()){$args=[object[]]::new($values.Length);for($i=0;$i -lt $values.Length;$i++){$args[$i]=$values[$i].PSObject.BaseObject};$type.GetMethod($name,$flags).Invoke($shell,$args)}
function Field($name){$type.GetField($name,$flags).GetValue($shell)}
function Pump([double]$seconds){$frame=[Windows.Threading.DispatcherFrame]::new();$timer=[Windows.Threading.DispatcherTimer]::new();$timer.Interval=[TimeSpan]::FromSeconds($seconds);$timer.Add_Tick({$timer.Stop();$frame.Continue=$false}.GetNewClosure());$timer.Start();[Windows.Threading.Dispatcher]::PushFrame($frame)}
Call 'LoadConfig';$cfg=Field 'cfg'
if((Field 'configPath') -ne (Join-Path $root 'config.json')){throw 'Shortcut test did not isolate the profile'}
foreach($id in @('StartFallback','QuickSearch','HiddenMode','QuitAwl','CaptureTool','AppSwitcher','WidgetBuilder')){if(!$cfg.KeyBinds.ContainsKey($id)){throw "Missing default binding for $id"}}
if($cfg.StartSearchModifier -ne 'Ctrl' -or (Call 'StartModifierMask') -ne 1){throw 'Default Start app-search modifier changed'}
if((Call 'ResolveHotkey' @([int]0x1B,[int]1)) -ne 'StartFallback' -or (Call 'ResolveHotkey' @([int]0x20,[int]5)) -ne 'QuickSearch' -or (Call 'ResolveHotkey' @([int]0x09,[int]6)) -ne 'AppSwitcher'){throw 'Default shortcuts did not resolve, including reverse app switch'}
if((Call 'ResolveHotkey' @([int]0x57,[int]9)) -ne 'WidgetBuilder'){throw 'Widget Builder shortcut was not routed through the shared bindings'}
$cfg.ReplaceWindowsStart=$false;if((Call 'ResolveHotkey' @([int]0x1B,[int]1))){throw 'Start fallback overrode Windows while Start replacement was off'};$cfg.ReplaceWindowsStart=$true
Call 'SaveKeyBind' @('QuickSearch','Ctrl+Shift+K');if((Call 'ResolveHotkey' @([int]0x4B,[int]3)) -ne 'QuickSearch' -or (Call 'ResolveHotkey' @([int]0x20,[int]5))){throw 'Remapped Quick Search did not take effect immediately'}
if((Get-Content (Join-Path $root 'config.json') -Raw | ConvertFrom-Json).KeyBinds.QuickSearch -ne 'Ctrl+Shift+K'){throw 'Remapped shortcut was not persisted'}
try{Call 'SaveKeyBind' @('CaptureTool','Ctrl+Shift+K');throw 'Duplicate shortcut was accepted'}catch {if($_.Exception.ToString() -notlike '*already assigned*'){throw}}
Call 'SaveKeyBind' @('CaptureTool','None');if((Call 'ResolveHotkey' @([int]0x52,[int]6))){throw 'Cleared capture shortcut remained active'}
Call 'LoadConfig';$cfg=Field 'cfg';if($cfg.KeyBinds['QuickSearch'] -ne 'Ctrl+Shift+K' -or $cfg.KeyBinds['CaptureTool'] -ne 'None'){throw 'Shortcuts did not persist after reload'}
$cfg.KeyBinds['HiddenMode']='Ctrl+Alt+Space';$cfg.KeyBinds['QuickSearch']='Ctrl+Alt+Space';Call 'NormalizeKeyBindings'
if($cfg.KeyBinds['HiddenMode'] -ne 'Ctrl+Alt+H' -or $cfg.KeyBinds['QuickSearch'] -ne 'Ctrl+Alt+Space'){throw 'Invalid imported shortcut replaced another action’s default'}
$cfg.KeyBinds['QuickSearch']='Ctrl+Shift+K'
$cfg.CapturePrintScreen=$true
if((Call 'HandleShortcutEvent' @([int]0x2C,[int]1,$true,$false))){throw 'Modified Print Screen was hijacked'}
if(!(Call 'HandleShortcutEvent' @([int]0x2C,[int]0,$true,$false)) -or !(Call 'HandleShortcutEvent' @([int]0x2C,[int]0,$true,$false)) -or !(Call 'HandleShortcutEvent' @([int]0x2C,[int]0,$false,$true))){throw 'Print Screen did not consume down, repeat, and up'}
Pump .08;$capture=Field 'captureWindow';if($capture -eq $null -or !$capture.IsVisible){throw 'Print Screen did not open Awl Capture'};$capture.Close()
$cfg.CapturePrintScreen=$false;if((Call 'HandleShortcutEvent' @([int]0x2C,[int]0,$true,$false))){throw 'Disabled Print Screen replacement still intercepted'}
$top=[Windows.Window]::new();$top.Width=300;$top.Height=40;$top.Content=[Windows.Controls.Border]::new();$top.ShowInTaskbar=$false;$top.Show()
$dock=[Windows.Window]::new();$dock.Width=300;$dock.Height=60;$dock.Content=[Windows.Controls.Border]::new();$dock.ShowInTaskbar=$false;$dock.Show()
$type.GetField('top',$flags).SetValue($shell,$top);$type.GetField('dock',$flags).SetValue($shell,$dock)
Call 'ToggleHiddenMode';if(!(Field 'hiddenMode') -or $top.IsVisible -or $dock.IsVisible){throw 'Hidden Mode did not hide both bars'}
Call 'AnimateDock' @($true);if($dock.IsVisible){throw 'Hidden Mode allowed the taskbar to reopen'}
Call 'ToggleHiddenMode';if(Field 'hiddenMode'){throw 'Hidden Mode did not turn off'}
$top.Close();$dock.Close();$type.GetField('top',$flags).SetValue($shell,$null);$type.GetField('dock',$flags).SetValue($shell,$null)
$page=[Windows.Controls.StackPanel]::new();$type.GetField('page',$flags).SetValue($shell,$page);Call 'ApplyTheme';Call 'KeyBindingsCard'
if($page.Children.Count -eq 0){throw 'General shortcut controls were not built'}
$window=[Windows.Window]::new();$window.Width=860;$window.Height=700;$window.Left=60;$window.Top=240;$window.WindowStyle='None';$window.ShowInTaskbar=$false;$window.Content=[Windows.Controls.Border]::new();$window.Content.Background=[Windows.Media.SolidColorBrush]::new((Field 'baseColor'));$window.Content.Child=[Windows.Controls.ScrollViewer]::new();$window.Content.Child.Content=$page;$window.Show();$window.UpdateLayout()
$bitmap=[Windows.Media.Imaging.RenderTargetBitmap]::new(860,700,96,96,[Windows.Media.PixelFormats]::Pbgra32);$bitmap.Render($window.Content);$encoder=[Windows.Media.Imaging.PngBitmapEncoder]::new();$encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($bitmap));$stream=[IO.File]::Create((Join-Path $Output 'shortcuts.png'));$encoder.Save($stream);$stream.Dispose();$window.Close()
$cfg.KeyBinds['WidgetBuilder']='Ctrl+Win+W';$type.GetField('winDown',$flags).SetValue($shell,$true);$type.GetField('winApps',$flags).SetValue($shell,$true)
if(!(Call 'HandleShortcutEvent' @([int]0x57,[int]9,$true,$false)) -or !(Field 'winShortcutConsumed') -or (Field 'winApps')){throw 'Widget Builder binding did not consume its Windows-key chord without opening Start'}
Call 'HandleShortcutEvent' @([int]0x57,[int]9,$false,$true)|Out-Null
$shell.Shutdown()
Write-Output 'Shortcut defaults/remapping/persistence/collision handling, Start fallback, Print Screen replacement, Hidden Mode, and General controls passed'
