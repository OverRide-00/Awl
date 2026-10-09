param([Parameter(Mandatory=$true)][string]$Binary,[Parameter(Mandatory=$true)][string]$Output,[Parameter(Mandatory=$true)][string]$Plugins,[switch]$DevMode)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName PresentationFramework,PresentationCore,WindowsBase
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
$dock=[Windows.Window]::new();$dock.Width=480;$dock.Height=54;$dock.Left=360;$dock.Top=900;$dock.WindowStyle='None';$dock.ShowInTaskbar=$false;$dock.Content=[Windows.Controls.Border]::new();$dock.Content.Background=[Windows.Media.Brushes]::Teal;$dock.Show();SetField 'dock' $dock
foreach($scale in @(.5,1.2)){
 $cfg.GuiScale=$scale;Call 'ToggleLauncher' @($false);$menu=Field 'launcher';$finalWidth=[Math]::Min(1128*$scale,(Call 'SurfaceBounds' @('Start menu')).Width-48)
 $motionView=$menu.Content.Children[0];$transform=$motionView.RenderTransform;if([Math]::Abs($motionView.Width*$transform.Children[0].ScaleX-$dock.Width) -gt 1){throw 'Menu did not start at taskbar width'}
 $content=$menu.Content.Children[1];$menuSurface=Field 'launcherSurface';if($menuSurface.Child.Opacity -ne 0){throw 'Menu UI is visible while shell is compressed'};if($content.RenderTransform -ne [Windows.Media.Transform]::Identity){throw 'Menu content was stretched'};$firstTop=$transform.Children[1].Y;Pump .06;$movingTop=$transform.Children[1].Y;$movingWidth=$motionView.Width*$transform.Children[0].ScaleX;Snapshot $menu ('menu-mid-'+$scale);Pump .6
 if([Math]::Abs($menu.Width-$finalWidth) -gt 1 -or (Field 'restoreMenuMotion')){throw 'Menu did not settle and restore layout'}
 if($movingTop -ge $firstTop){throw 'Menu did not rise toward its resting position'}
 if(![Object]::ReferenceEquals($menu.Content,(Field 'launcherSurface'))){throw 'Animation wrapper did not detach'}
 Snapshot $menu ('menu-rest-'+$scale);Write-Output ('Scale '+$scale+': width '+$dock.Width+' -> '+[Math]::Round($movingWidth)+' -> '+$menu.Width)
 Call 'CloseLauncher';Pump .3;if($menu.IsVisible -or (Field 'launcherMotionClosing')){throw 'Menu exit did not finish'}
}
$cfg.GuiScale=1;Call 'ToggleLauncher' @($false);Pump .05;Call 'CloseLauncher';Pump .04;Call 'ToggleLauncher' @($false);Pump .7;if(!(Field 'launcher').IsVisible){throw 'Rapid menu reopen failed'}
Call 'DrawLauncher';if(Field 'restoreMenuMotion'){throw 'Redraw did not cancel animation'};(Field 'launcher').Hide()
$search=[Windows.Window]::new();$search.Width=640;$search.Height=70;$search.WindowStyle='None';$search.ShowInTaskbar=$false;$search.Content=[Windows.Controls.Border]::new();$search.Content.Background=[Windows.Media.Brushes]::Teal;$search.Show();SetField 'quickSearchWindow' $search
if(!(Call 'AnimateVisualSearch' @($true,$null)) -or $search.Content -isnot [Windows.Controls.Grid]){throw 'Search entrance was not prepared'};Pump .65;if((Field 'restoreSearchMotion') -or $search.Opacity -ne 1){throw 'Search entrance cleanup failed'}
$script:closed=$false;Call 'AnimateVisualSearch' @($false,[Action]{$script:closed=$true});Pump .3;if(!$script:closed){throw 'Search exit completion missing'};$search.Close();SetField 'quickSearchWindow' $null
$button=[Windows.Controls.Button]::new();$icon=[Windows.Controls.Border]::new();$icon.Width=32;$icon.Height=32;$icon.Background=[Windows.Media.Brushes]::Teal;$button.Content=$icon
$task=[Windows.Window]::new();$task.Width=140;$task.Height=100;$task.Left=320;$task.Top=670;$task.Content=$button;$task.Show();$original=$icon.RenderTransform;Call 'TaskbarAppTooltip' @($button,'Motion test app');if($button.ToolTip.Placement -ne 'Custom' -or $button.ToolTip.Content.Children.Count -ne 2){throw 'Taskbar tooltip is not an arrow bubble above the app'}
Call 'BindVisualTaskbarMotion' @($button,$true);Pump .1;$group=$icon.RenderTransform;if($group.Children[3].Y -ge 0 -or [Math]::Abs($group.Children[2].Angle) -lt .1){throw 'App opening did not bounce and tilt'}
if([Math]::Abs($group.Children[3].Y) -lt 15){throw 'Bounce was too small'};if($icon.Opacity -ne 0){throw 'Moving icon was not lifted out of clipping window'};Pump .6;if([Math]::Abs($group.Children[3].Y) -gt .01 -or [Math]::Abs($group.Children[2].Angle) -gt .01){throw 'Bounce did not settle'}
$enter=[Windows.Input.MouseEventArgs]::new([Windows.Input.Mouse]::PrimaryDevice,0);$enter.RoutedEvent=[Windows.UIElement]::MouseEnterEvent;$button.RaiseEvent($enter);Pump .24;if($group.Children[1].ScaleX -lt 1.1){throw 'Hover did not scale'}
$leave=[Windows.Input.MouseEventArgs]::new([Windows.Input.Mouse]::PrimaryDevice,0);$leave.RoutedEvent=[Windows.UIElement]::MouseLeaveEvent;$button.RaiseEvent($leave);Pump .24;if([Math]::Abs($group.Children[1].ScaleX-1) -gt .01){throw 'Hover did not restore'}
Call 'ResetVisualMotion';if(![Object]::ReferenceEquals($icon.RenderTransform,$original)){throw 'Disable did not restore icon transform'};$task.Close()
# Native-window fly-in uses a real compositor thumbnail; no app bounds are resized.
$cfg.AnimationMs=450;SetField 'pluginsLoaded' $false;Call 'LoadPlugins'
$cover=[Windows.Window]::new();$cover.Left=0;$cover.Top=0;$cover.Width=1200;$cover.Height=1000;$cover.WindowStyle='None';$cover.Topmost=$true;$cover.ShowInTaskbar=$false;$cover.Background=[Windows.Media.Brushes]::DarkSlateGray;$cover.Show()
$source=[Windows.Window]::new();$source.Left=180;$source.Top=160;$source.Width=500;$source.Height=300;$source.Topmost=$true;$source.Background=[Windows.Media.Brushes]::Teal;$source.Content=[Windows.Controls.TextBlock]::new();$source.Content.Text='Awl native animation test';$source.Content.FontSize=30;$source.Content.Foreground=[Windows.Media.Brushes]::White;$source.Show()
$task=[Windows.Window]::new();$task.Width=80;$task.Height=80;$task.Left=320;$task.Top=670;$task.Topmost=$true;$button=[Windows.Controls.Button]::new();$button.Content='Test';$task.Content=$button;$task.Show();Pump .12
$handle=[Windows.Interop.WindowInteropHelper]::new($source).Handle
if(!(Call 'AnimateNativeApp' @($handle,$button))){throw 'Native app compositor animation did not start'}
$native=$assembly.GetType('Awl.Native');$nativeFlags=[Reflection.BindingFlags]'Static,Public';$args=[object[]]@($handle,14,0,4);$native.GetMethod('DwmGetWindowAttribute',$nativeFlags).Invoke($null,$args)|Out-Null;if($args[2] -ne 1){throw 'Source window was not cloaked during app fly-in'}
Pump .07
Add-Type -AssemblyName System.Drawing
$bitmap=[Drawing.Bitmap]::new(900,850);$graphics=[Drawing.Graphics]::FromImage($bitmap);$graphics.CopyFromScreen(0,0,0,0,[Drawing.Size]::new(900,850));$bitmap.Save((Join-Path $Output 'native-app-mid.png'),[Drawing.Imaging.ImageFormat]::Png);$graphics.Dispose();$bitmap.Dispose()
Pump .5;$args=[object[]]@($handle,14,0,4);$native.GetMethod('DwmGetWindowAttribute',$nativeFlags).Invoke($null,$args)|Out-Null;if($args[2] -ne 0 -or (Field 'nativeAppMotions').Count -ne 0){throw 'Native app did not restore after motion'}
if($source.Left -ne 180 -or $source.Top -ne 160 -or $source.Width -ne 500 -or $source.Height -ne 300){throw 'Native app animation changed source geometry'}
if(!(Call 'AnimateNativeApp' @($handle,$button))){throw 'Native cancel test did not start'};Call 'StopNativeAppMotions';$args=[object[]]@($handle,14,0,4);$native.GetMethod('DwmGetWindowAttribute',$nativeFlags).Invoke($null,$args)|Out-Null;if($args[2] -ne 0){throw 'Cancel left native window cloaked'}
# Recover an orphaned temporary cloak with the same routine used by the guard.
Call 'AnimateNativeApp' @($handle,$button)|Out-Null;$saved=[IO.File]::ReadAllText((Join-Path $root 'motion-cloaks.json'));Call 'StopNativeAppMotions'
$args=[object[]]@($handle,13,1,4);$native.GetMethod('DwmSetWindowAttribute',$nativeFlags).Invoke($null,$args)|Out-Null;[IO.File]::WriteAllText((Join-Path $root 'motion-cloaks.json'),$saved);$type.GetMethod('RestoreMotionCloaks',$static).Invoke($null,@());$args=[object[]]@($handle,14,0,4);$native.GetMethod('DwmGetWindowAttribute',$nativeFlags).Invoke($null,$args)|Out-Null;if($args[2] -ne 0){throw 'Guard recovery left source cloaked'}
$source.Close();$task.Close();$cover.Close()
SetField 'pluginsLoaded' $false;Call 'LoadPlugins';$cfg.Animations=$false;Call 'ToggleLauncher' @($false);if(Field 'restoreMenuMotion'){throw 'Animation off was ignored'};(Field 'launcher').Hide()
$cfg.EnabledPlugins.Remove('awl.animation-api')|Out-Null;SetField 'pluginsLoaded' $false;Call 'LoadPlugins';if($type.GetProperty('VisualMotionApi',$flags).GetValue($shell,$null)){throw 'Missing dependency did not fall back'}
$dock.Close();Write-Output 'Motion API dependency, real rendering frames, menu scaling/travel, interrupted opens, exits, search, bounce/tilt, hover with Magnify off, bubble tooltip, native app fly-in/geometry, cancellation, guard recovery, disable cleanup and animation-off checks passed.'
