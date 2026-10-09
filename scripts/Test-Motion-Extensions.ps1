param([Parameter(Mandatory=$true)][string]$Binary,[Parameter(Mandatory=$true)][string]$Output,[Parameter(Mandatory=$true)][string]$Plugins,[switch]$DevMode)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName PresentationFramework,PresentationCore,WindowsBase,System.Drawing
$assembly=[Reflection.Assembly]::LoadFrom($Binary);$type=$assembly.GetType('Awl.Shell');$flags=[Reflection.BindingFlags]'Instance,NonPublic';$static=[Reflection.BindingFlags]'Static,NonPublic'
$Output=[IO.Path]::GetFullPath($Output);$root=Join-Path $Output 'profile';[IO.Directory]::CreateDirectory($root)|Out-Null
Copy-Item -LiteralPath "$env:LOCALAPPDATA\Awl\config.json" -Destination $root
$type.GetField('Root',$static).SetValue($null,$root);if($DevMode){$type.GetField('devMode',$static).SetValue($null,$true);$type.GetField('devLibraryRoot',$static).SetValue($null,(Join-Path $root 'Library'))};$shell=[Activator]::CreateInstance($type);$shell.ShutdownMode=[Windows.ShutdownMode]::OnExplicitShutdown
if($type.GetField('configPath',$flags).GetValue($shell) -ne (Join-Path $root 'config.json')){throw 'Motion test profile isolation failed'}
function Call($name,[object[]]$values=@()){$args=[object[]]::new($values.Length);for($i=0;$i -lt $values.Length;$i++){$args[$i]=$values[$i].PSObject.BaseObject};$type.GetMethod($name,$flags).Invoke($shell,$args)}
function Field($name){$type.GetField($name,$flags).GetValue($shell)}
function SetField($name,$value){$type.GetField($name,$flags).SetValue($shell,$value)}
function Pump([double]$seconds){$frame=[Windows.Threading.DispatcherFrame]::new();$timer=[Windows.Threading.DispatcherTimer]::new();$timer.Interval=[TimeSpan]::FromSeconds($seconds);$timer.Add_Tick({$timer.Stop();$frame.Continue=$false}.GetNewClosure());$timer.Start();[Windows.Threading.Dispatcher]::PushFrame($frame)}
function Snapshot($window,$name){$window.UpdateLayout();$bitmap=[Windows.Media.Imaging.RenderTargetBitmap]::new([int][Math]::Ceiling($window.Width),[int][Math]::Ceiling($window.Height),96,96,[Windows.Media.PixelFormats]::Pbgra32);$bitmap.Render($window.Content);$encoder=[Windows.Media.Imaging.PngBitmapEncoder]::new();$encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($bitmap));$stream=[IO.File]::Create((Join-Path $Output ($name+'.png')));$encoder.Save($stream);$stream.Dispose()}
$installed=if($DevMode){Join-Path $root 'Library/Plugins/Installed'}else{Join-Path $root 'Plugins/Installed'};[IO.Directory]::CreateDirectory($installed)|Out-Null
Copy-Item -LiteralPath (Join-Path $Plugins 'Core/animation-api') -Destination (Join-Path $installed 'awl.animation-api') -Recurse
Copy-Item -LiteralPath (Join-Path $Plugins 'Visual/motion-showcase') -Destination (Join-Path $installed 'awl.motion-showcase') -Recurse
Call 'LoadConfig';$cfg=Field 'cfg';$cfg.EnabledPlugins.Clear();$cfg.EnabledPlugins.Add('awl.animation-api');$cfg.EnabledPlugins.Add('awl.motion-showcase');$cfg.Animations=$true;$cfg.Magnify=$false;$cfg.AnimationMs=240;$cfg.CloseOnBlur=$false;$cfg.MenuOpacity=1
Call 'LoadPlugins';Call 'ApplyTheme';SetField 'activeTab' 'Interface'
if(!$type.GetProperty('VisualMotionApi',$flags).GetValue($shell,$null)){throw 'Motion API dependency was not resolved'}
$cfg.GuiScale=1
# Ball-like rebounds stay positive, shrink, and return exactly to ground.
$bounce=$type.GetMethod('AppIconBounce',$static)
$peaks=@(.18,.64,.83,.95)|ForEach-Object {$bounce.Invoke($null,@([double]$_))}
for($i=1;$i -lt $peaks.Count;$i++){if($peaks[$i] -le 0 -or $peaks[$i] -ge $peaks[$i-1]){throw 'Bounce rebounds did not diminish'}}
foreach($p in @(0,.52,.76,.90,1)){if([Math]::Abs($bounce.Invoke($null,@([double]$p))) -gt .000001){throw 'Bounce did not land between rebounds'}}
foreach($p in 0..1000){$height=$bounce.Invoke($null,@([double]($p/1000)));if($height -lt 0 -or $height -gt 1){throw 'Bounce passed through ground or overshot its peak'}}
Write-Output 'Three diminishing ball rebounds and clean landings passed'
# Stable ordering holds through focus changes until the actual delay expires.
$windows=[Collections.Generic.List[Tuple[IntPtr,string,string]]]::new()
foreach($id in @(111,222,333)){$windows.Add([Tuple[IntPtr,string,string]]::new([IntPtr]$id,'Test '+$id,''))}
$args=[object[]]@($windows,$false);$type.GetMethod('StableTaskbarWindows',$flags).Invoke($shell,$args)|Out-Null
Call 'QueueTaskbarPromotion' @([IntPtr]333);$clock=[Diagnostics.Stopwatch]::StartNew();Pump .45
$args=[object[]]@($windows,$false);$held=$type.GetMethod('StableTaskbarWindows',$flags).Invoke($shell,$args)
if($held[0].Item1 -ne [IntPtr]111 -or $args[1]){throw 'Foreground app reordered before its one-second delay'}
Pump .7;$args=[object[]]@($windows,$false);$moved=$type.GetMethod('StableTaskbarWindows',$flags).Invoke($shell,$args)
if($clock.Elapsed.TotalSeconds -lt 1 -or $moved[0].Item1 -ne [IntPtr]333 -or !$args[1]){throw 'App did not promote after its delay'}
Call 'QueueTaskbarPromotion' @([IntPtr]222);Call 'QueueTaskbarPromotion' @([IntPtr]333);if((Field 'taskbarPendingFront') -ne [IntPtr]::Zero){throw 'Returning to the first app did not cancel stale promotion'}
Write-Output 'One-second stable order and stale focus cancellation passed'
# All neighboring app slots move from their old coordinates to their new coordinates.
$apps=[Windows.Controls.StackPanel]::new();$apps.Orientation='Horizontal';SetField 'apps' $apps
$bar=[Windows.Window]::new();$bar.Width=320;$bar.Height=60;$bar.Left=100;$bar.Top=750;$bar.WindowStyle='None';$bar.ShowInTaskbar=$false;$bar.Content=$apps;$bar.Show()
foreach($id in @(111,222,333)){$button=[Windows.Controls.Button]::new();$button.Width=80;$button.Height=48;$button.Tag=[IntPtr]$id;$button.Content='App '+$id;$apps.Children.Add($button)|Out-Null};Pump .05
$before=Call 'TaskbarButtonPositions';$apps.Children.Clear()
foreach($id in @(333,111,222)){$button=[Windows.Controls.Button]::new();$button.Width=80;$button.Height=48;$button.Tag=[IntPtr]$id;$button.Content='App '+$id;$apps.Children.Add($button)|Out-Null};$apps.UpdateLayout()
Call 'AnimateTaskbarReorder' @($before)
if($apps.Children[0].RenderTransform.Children[1].X -lt 150 -or $apps.Children[1].RenderTransform.Children[1].X -gt -70){throw 'Reorder did not start from old app positions'}
Pump .12;if($apps.Children[0].RenderTransform.Children[0].Children[1].ScaleX -le 1.05){throw 'Promoted app did not grow before sliding'}
$overshot=$false;foreach($sample in 1..25){Pump .015;$transform=$apps.Children[1].RenderTransform;if($transform -is [Windows.Media.TransformGroup] -and $transform.Children[1].X -gt .1){$overshot=$true}}
if(!$overshot){throw 'Pushed neighbor did not overshoot its new slot'};Snapshot $bar 'taskbar-reorder-settled';Pump .25
foreach($button in $apps.Children){if($button.RenderTransform -ne [Windows.Media.Transform]::Identity){throw 'Reorder transform did not restore'}}
if((Field 'taskbarReordering').Count -ne 0 -or $apps.Children[0].Opacity -ne 1){throw 'Reorder left hidden icons or tracking state'}
$bar.Close();SetField 'apps' $null
# Real Quick Search has a magnifier, growing circle, expanding pill, then falling buttons.
Call 'ShowQuickSearch';$search=Field 'quickSearchWindow';$original=Field 'quickSearchRoot';$animated=$search.Content
if($animated -isnot [Windows.Controls.Grid] -or $animated.Children.Count -ne 3 -or $animated.Children[0].Width -ne 0){throw 'Quick Search did not begin with the lone magnifier'}
Pump .08;if($animated.Children[0].Width -ne 0){throw 'Quick Search magnifier did not pause before the circle'};Snapshot $search 'quick-magnifier'
Pump .20;Snapshot $search 'quick-circle';$circleWidth=$animated.Children[0].Width;if($circleWidth -gt $search.Height+.1){throw 'Quick Search expanded before the circle phase finished'}
Pump .30;Snapshot $search 'quick-expanding';$expandedWidth=$animated.Children[0].Width
if($circleWidth -le 0 -or $expandedWidth -le $circleWidth){throw 'Quick Search circle did not expand into the pill'}
Pump .25;Snapshot $search 'quick-falling';Pump .35
if($search.Content -isnot [Windows.Controls.Border] -or $search.Content.Opacity -ne 1 -or (Field 'restoreSearchMotion')){throw 'Quick Search did not restore its full UI'}
Snapshot $search 'quick-ready';Call 'CloseQuickSearch';Pump .3
# Typing during the reveal immediately restores a usable search surface.
foreach($delay in @(0,.28,.62,.85)){
 Call 'ShowQuickSearch';$input=Field 'quickSearchInput';if($delay -gt 0){Pump $delay}
 if(!$input.IsKeyboardFocused){throw 'Reparenting the opening search lost typing focus'}
 $composition=[Windows.Input.TextComposition]::new([Windows.Input.InputManager]::Current,$input,'x');$textEvent=[Windows.Input.TextCompositionEventArgs]::new([Windows.Input.Keyboard]::PrimaryDevice,$composition);$textEvent.RoutedEvent=[Windows.Input.TextCompositionManager]::PreviewTextInputEvent;$input.RaiseEvent($textEvent)
 if((Field 'restoreSearchMotion') -or (Field 'quickSearchRevealing') -or (Field 'quickSearchWindow').Content -isnot [Windows.Controls.Border]){throw 'First typing event did not immediately skip the reveal'}
 $input.Text='18+4';if((Field 'quickQuery') -ne '18+4' -or !$input.IsKeyboardFocused){throw 'Skipping the animation lost the query or focus'};Call 'CloseQuickSearch';Pump .3
}
# Text changes also skip immediately for paste and whitespace before any result expands.
Call 'ShowQuickSearch';$input=Field 'quickSearchInput';$input.Text=' '
if((Field 'restoreSearchMotion') -or (Field 'quickSearchWindow').Content.Opacity -ne 1){throw 'Whitespace/paste did not immediately reveal the bar'};Call 'CloseQuickSearch';Pump .3
# Capture and recorder use the previous quick search spring/fade without reparenting.
Call 'ShowCaptureTool';$capture=Field 'captureWindow';$surface=$capture.Content
if($surface.Opacity -ne 0 -or !(Field 'surfaceMotionStops').ContainsKey($surface)){throw 'Capture entrance was not animated'}
Pump .35;if($capture.Content -ne $surface -or $surface.Opacity -ne 1){throw 'Capture entrance changed the live surface'};Snapshot $capture 'capture-ready'
Call 'ShowRecordingBar' @([Drawing.Rectangle]::new(60,240,320,180));$recorder=Field 'captureRecordWindow';$recordSurface=$recorder.Content
if($recordSurface.Opacity -ne 0){throw 'Recorder entrance was not animated'};Pump .35;if($recorder.Content -ne $recordSurface -or $recordSurface.Opacity -ne 1){throw 'Recorder entrance did not restore'}
Snapshot $recorder 'recorder-ready';$recorder.Close();$capture.Close()
# Topbar popups use a top-anchored spring and cancel safely when closed mid-entrance.
$anchor=[Windows.Window]::new();$anchor.Width=60;$anchor.Height=40;$anchor.Left=100;$anchor.Top=50;$anchor.Show();$popup=[Windows.Controls.Primitives.Popup]::new();$popup.PlacementTarget=$anchor;$popup.Placement='Bottom';$popup.AllowsTransparency=$true
$panel=[Windows.Controls.Border]::new();$panel.Width=280;$panel.Height=150;$panel.Background=[Windows.Media.Brushes]::Teal;$popup.Child=$panel;$popup.IsOpen=$true
Call 'AnimateVisualSurface' @($panel,$true)|Out-Null;if($panel.Opacity -ne 0 -or $panel.RenderTransformOrigin.Y -ne 0){throw 'Topbar opening was not anchored to the top'};Pump .08;Call 'StopVisualSurface' @($panel);$popup.IsOpen=$false
if($panel.Opacity -ne 1 -or (Field 'surfaceMotionStops').ContainsKey($panel)){throw 'Popup cancellation left animation state behind'};$anchor.Close()
# Shared menu/pinned/bar entrances preserve GUI layout scale and work on reopen.
foreach($kind in @('Pinned','Dropdown','Taskbar','Topbar')){
 $popup=[Windows.Controls.Primitives.Popup]::new();$popup.Placement='Top';$popup.AllowsTransparency=$true
 $panel=[Windows.Controls.Border]::new();$panel.Width=160;$panel.Height=80;$panel.Background=[Windows.Media.Brushes]::Teal;$panel.LayoutTransform=[Windows.Media.ScaleTransform]::new(.5,.5);$layout=$panel.LayoutTransform;$popup.Child=$panel
 Call 'BindVisualPopup' @($popup,$kind);$popup.IsOpen=$true;Pump .025
 if($panel.Opacity -ge 1 -or $panel.RenderTransformOrigin.Y -ne 1){throw "$kind popup did not animate from the attachment edge"}
 Pump .32;if($panel.Opacity -ne 1 -or $panel.LayoutTransform -ne $layout){throw "$kind popup did not settle with scale intact"}
 $popup.IsOpen=$false;$popup.IsOpen=$true;Pump .025;if($panel.Opacity -ge 1){throw "$kind popup did not animate on reopen"}
 $popup.IsOpen=$false;Pump .03;if($panel.Opacity -ne 1 -or (Field 'surfaceMotionStops').ContainsKey($panel)){throw "$kind popup close did not cancel animation"}
 $cfg.Animations=$false;$popup.IsOpen=$true;Pump .025;if($panel.Opacity -ne 1){throw 'Animation-off still animated a menu'};$popup.IsOpen=$false;$cfg.Animations=$true
}
Write-Output 'Pinned, dropdown, taskbar and topbar entrances, reopen, close cancellation, scale and animation-off passed'
# Actual themed context menus use the shared dropdown entrance and close cleanup.
$context=[Windows.Controls.ContextMenu]::new();Call 'ThemeContextMenu' @($context);$entry=[Windows.Controls.MenuItem]::new();$entry.Header='A test action';$context.Items.Add($entry)|Out-Null;$context.IsOpen=$true;Pump .025
if($context.Opacity -ge 1 -or !(Field 'surfaceMotionStops').ContainsKey($context)){throw 'Themed context menu did not animate'}
$context.IsOpen=$false;Pump .3;if($context.Opacity -ne 1 -or (Field 'surfaceMotionStops').ContainsKey($context)){throw 'Context menu close did not clean up'}
Call 'ResetVisualMotion';if((Field 'visualMotionCleanup').Count -ne 0 -or (Field 'surfaceMotionStops').Count -ne 0){throw 'Plugin disable leaked active animations'}
Write-Output 'Taskbar slide and neighbor movement, Quick Search staged reveal/typing, capture/recorder spring, popup origin and cancellation, and disable cleanup passed'
