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
foreach($package in @(@('Core/animation-api','awl.animation-api'),@('Visual/motion-showcase','awl.motion-showcase'))){$destination=Join-Path $installed $package[1];[IO.Directory]::CreateDirectory($destination)|Out-Null;Copy-Item -Path (Join-Path (Join-Path $Plugins $package[0]) '*') -Destination $destination -Recurse -Force}
Call 'LoadConfig';$cfg=Field 'cfg';$cfg.EnabledPlugins.Clear();$cfg.EnabledPlugins.Add('awl.animation-api');$cfg.EnabledPlugins.Add('awl.motion-showcase');$cfg.Animations=$true;$cfg.Magnify=$false;$cfg.AnimationMs=240;$cfg.CloseOnBlur=$false;$cfg.MenuOpacity=1
Call 'LoadPlugins';Call 'ApplyTheme';SetField 'activeTab' 'Interface'
if(!$type.GetProperty('VisualMotionApi',$flags).GetValue($shell,$null)){throw 'Motion API dependency was not resolved'}
$cfg.GuiScale=1;$cfg.TaskbarScale=0;SetField 'nextPluginUpdateCheck' ([DateTime]::MaxValue)
$window=[Windows.Window]::new();$window.Width=700;$window.Height=420;$window.Left=50;$window.Top=260;$window.WindowStyle='None';$window.ShowInTaskbar=$false;$body=[Windows.Controls.StackPanel]::new();$window.Content=$body;$body.Background=[Windows.Media.SolidColorBrush]::new((Field 'baseColor'));$body.Margin=[Windows.Thickness]::new(18)
$script:states=[Collections.Generic.List[bool]]::new();$changed=[Action[bool]]{param($state)$script:states.Add($state)}
$row=Call 'Switch' @('Test toggle',$false,$changed);$body.Children.Add($row)|Out-Null;$toggle=$row.Children[1];$track=$toggle.Content;$thumb=$track.Child
$slider=[Windows.Controls.Slider]::new();$slider.Minimum=0;$slider.Maximum=100;$slider.Value=40;Call 'StyleSlider' @($slider);$body.Children.Add($slider)|Out-Null
$script:chosen='A';$select=[Action[string]]{param($value)$script:chosen=$value}
$options=Call 'Options' @('A test choice',[string[]]@('A','B','C'),[string[]]@('A','B','C'),'A',$select);$body.Children.Add($options)|Out-Null;$group=$options.Children[1];$buttons=$group.Children
$window.Show();Pump .05;Snapshot $window 'controls-initial'
# Toggle updates its logical value immediately but travels and blends visually.
$off=$track.Background.Color;$toggle.RaiseEvent([Windows.RoutedEventArgs]::new([Windows.Controls.Button]::ClickEvent));$offset=$thumb.RenderTransform.Children[2]
if($script:states.Count -ne 1 -or !$script:states[0] -or $offset.X -gt -17){throw 'Toggle did not start from its original side'}
Pump .025;if($thumb.RenderTransform.Children[1].ScaleX -ge 1){throw 'Toggle did not compress on press'};Snapshot $window 'toggle-press';Pump .075;if($offset.X -le -18 -or $offset.X -gt 3 -or $track.Background.Color -eq $off){throw 'Toggle motion/color did not advance'}
$before=$thumb.TranslatePoint([Windows.Point]::new(0,0),$track).X;$toggle.RaiseEvent([Windows.RoutedEventArgs]::new([Windows.Controls.Button]::ClickEvent));$after=$thumb.TranslatePoint([Windows.Point]::new(0,0),$track).X
if([Math]::Abs($before-$after) -gt .1){throw 'Interrupted toggle snapped'};Pump .45;if($thumb.HorizontalAlignment -ne 'Left' -or $thumb.RenderTransform -ne [Windows.Media.Transform]::Identity){throw 'Toggle did not settle cleanly'}
$toggle.RaiseEvent([Windows.RoutedEventArgs]::new([Windows.Controls.Button]::ClickEvent));$offset=$thumb.RenderTransform.Children[2];$overshot=$false;$bounced=$false
foreach($sample in 1..26){Pump .015;if($offset.X -gt .15){$overshot=$true};if($offset.Y -lt -.1){$bounced=$true}}
if(!$overshot -or !$bounced -or $thumb.RenderTransform -ne [Windows.Media.Transform]::Identity){throw 'Toggle did not overshoot, bounce, and settle'}
$toggle.RaiseEvent([Windows.RoutedEventArgs]::new([Windows.Controls.Button]::ClickEvent));Pump .45
# Slider growth is render-only: value, layout size, and pointer mapping stay intact.
$part=$slider.Template.FindName('PART_Track',$slider);$knob=$part.Thumb;$size=$knob.RenderTransform.Children[1]
$hover=[Windows.Input.MouseEventArgs]::new([Windows.Input.Mouse]::PrimaryDevice,0);$hover.RoutedEvent=[Windows.UIElement]::MouseEnterEvent;$knob.RaiseEvent($hover);Pump .18
if($size.ScaleX -lt 1.39){throw 'Slider knob did not grow on hover'}
$leave=[Windows.Input.MouseEventArgs]::new([Windows.Input.Mouse]::PrimaryDevice,0);$leave.RoutedEvent=[Windows.UIElement]::MouseLeaveEvent;$knob.RaiseEvent($leave);Pump .18;if([Math]::Abs($size.ScaleX-1) -gt .001){throw 'Slider knob did not shrink on leave'}
$knob.RaiseEvent([Windows.Controls.Primitives.DragStartedEventArgs]::new(0,0));Pump .18
if($size.ScaleX -lt 1.39 -or $knob.Width -ne 14 -or $slider.Value -ne 40){throw 'Slider knob did not grow without changing layout/value'}
$knob.RaiseEvent([Windows.Controls.Primitives.DragCompletedEventArgs]::new(0,0,$false));Pump .18
if(!$knob.IsMouseOver -and [Math]::Abs($size.ScaleX-1) -gt .001){throw 'Slider knob did not shrink after drag'}
# Exclusive options share square seams, round outer ends, and a full pill selection.
$radius=$type.GetField('MotionChoiceRadiusProperty',$static).GetValue($null)
$r0=$buttons[0].GetValue($radius);$r1=$buttons[1].GetValue($radius);$r2=$buttons[2].GetValue($radius)
if($r0.TopRight -le 0 -or $r1.TopLeft -ne 0 -or $r2.TopLeft -ne 0 -or $r2.TopRight -le 0 -or $buttons[0].Margin.Left -ne 0){throw 'Segmented choice geometry was wrong'}
if($buttons[0].Background.Color -ne $buttons[1].Background.Color -or $buttons[0].BorderBrush.Color -ne $buttons[1].BorderBrush.Color -or $buttons[0].Foreground.Color -ne $buttons[1].Foreground.Color){throw 'Selected choices did not share ordinary styling'}
Call 'HoverMotionChoice' @($buttons[1],$true);Pump .17;if($buttons[1].GetValue($radius).TopLeft -le 0){throw 'Hover did not round internal corners'};Call 'HoverMotionChoice' @($buttons[1],$false);Pump .17
$buttons[1].RaiseEvent([Windows.RoutedEventArgs]::new([Windows.Controls.Button]::ClickEvent));if($script:chosen -ne 'B'){throw 'Choice action was delayed'};Pump .12
if($buttons[0].GetValue($radius).TopRight -ne 0 -or $buttons[1].GetValue($radius).TopLeft -ne 0){throw 'Facing corners did not join before the new pill rounded'};Snapshot $window 'choice-connected';Pump .12
foreach($button in $buttons){if(!$button.RenderTransform.Value.IsIdentity){throw 'Option transition still applied a bounce transform'}}
$mid=$buttons[1].GetValue($radius);$motionGroup=(Field 'motionChoices')[$buttons[1]];$phase=Call 'ChoiceTravelRadius' @($motionGroup,[int]1,$r1,[int]0,[int]1,[double]0.6)
if($phase.TopRight -le 0 -or $phase.TopLeft -ne 0 -or $mid.TopRight -le 0 -or $mid.TopLeft -gt $mid.TopRight){throw "New outer edge did not round before its inner edge: phase=$phase actual=$mid duration=$((Field 'visualMotion').controlDurationMs)"};Snapshot $window 'choice-leading-edge'
$before=$mid.TopLeft;$buttons[2].RaiseEvent([Windows.RoutedEventArgs]::new([Windows.Controls.Button]::ClickEvent));$after=$buttons[1].GetValue($radius).TopLeft;if([Math]::Abs($before-$after) -gt .1){throw 'Interrupted choice morph snapped'}
Pump .5;$r0=$buttons[0].GetValue($radius);$r1=$buttons[1].GetValue($radius);$r2=$buttons[2].GetValue($radius)
if($r0.TopRight -ne 0 -or $r1.TopLeft -ne 0 -or [Math]::Abs($r2.TopLeft-$buttons[2].ActualHeight/2) -gt .001){throw 'Choice pills did not settle'}
# Reverse travel connects the opposite sides and also settles without changing styles.
$buttons[0].RaiseEvent([Windows.RoutedEventArgs]::new([Windows.Controls.Button]::ClickEvent));Pump .12;if($buttons[2].GetValue($radius).TopLeft -ne 0 -or $buttons[0].GetValue($radius).TopRight -ne 0){throw 'Reverse selection did not join facing corners'};Pump .41
if($buttons[0].GetValue($radius).TopRight -ne $buttons[0].ActualHeight/2 -or $buttons[0].Background.Color -ne $buttons[1].Background.Color){throw 'Reverse choice did not settle with equal styling'}
# The same controls behave instantly with global animation disabled.
$cfg.Animations=$false;$toggle.RaiseEvent([Windows.RoutedEventArgs]::new([Windows.Controls.Button]::ClickEvent));if($thumb.HorizontalAlignment -ne 'Right' -or $thumb.RenderTransform -ne [Windows.Media.Transform]::Identity){throw 'Animation-off toggle was not immediate'}
$buttons[1].RaiseEvent([Windows.RoutedEventArgs]::new([Windows.Controls.Button]::ClickEvent));if($buttons[1].GetValue($radius).TopLeft -ne $buttons[1].ActualHeight/2){throw 'Animation-off selection was not immediate'};$cfg.Animations=$true
# Disabling during interaction restores default templates/transforms and removes handlers.
$toggle.RaiseEvent([Windows.RoutedEventArgs]::new([Windows.Controls.Button]::ClickEvent));$knob.RaiseEvent([Windows.Controls.Primitives.DragStartedEventArgs]::new(0,0));$buttons[0].RaiseEvent([Windows.RoutedEventArgs]::new([Windows.Controls.Button]::ClickEvent));Pump .03;Call 'ResetVisualMotion'
if((Field 'controlMotionStops').Count -ne 0 -or (Field 'motionChoices').Count -ne 0 -or $thumb.RenderTransform -ne [Windows.Media.Transform]::Identity -or $knob.RenderTransform -ne [Windows.Media.Transform]::Identity){throw 'Plugin disable left control state behind'}
$window.Close()
# Default docking cancels stale coordinates and uses horizontal/vertical layout at all four edges.
$dock=[Windows.Window]::new();$apps=[Windows.Controls.StackPanel]::new();$dock.Content=[Windows.Controls.Border]::new();SetField 'dock' $dock;SetField 'apps' $apps;SetField 'dockLength' ([double]300)
$cfg.Style='Float';$cfg.Gap=10;$bounds=Call 'SurfaceBounds' @('Taskbar')
foreach($scale in @(.5,1.2)){$cfg.GuiScale=$scale;foreach($edge in @('Bottom','Top','Left','Right')){
 $cfg.Position=$edge;Call 'DockLayout';if([Windows.DependencyPropertyHelper]::GetValueSource($dock,[Windows.Window]::TopProperty).IsAnimated -or [Windows.DependencyPropertyHelper]::GetValueSource($dock,[Windows.Window]::LeftProperty).IsAnimated){throw 'Changing edge retained a stale slide animation'};$vertical=$edge -in @('Left','Right');if(($apps.Orientation -eq 'Vertical') -ne $vertical){throw 'Taskbar orientation did not match its edge'}
 $x=if($edge -eq 'Left'){$bounds.Left+10}elseif($edge -eq 'Right'){$bounds.Right-$dock.Width-10}else{$bounds.Left+($bounds.Width-$dock.Width)/2}
 $y=if($edge -eq 'Top'){$bounds.Top+10}elseif($edge -eq 'Bottom'){$bounds.Bottom-$dock.Height-10}else{$bounds.Top+($bounds.Height-$dock.Height)/2}
 if([Math]::Abs($dock.Left-$x) -gt .001 -or [Math]::Abs($dock.Top-$y) -gt .001){throw 'Taskbar did not land on the chosen screen edge'}
 $dock.BeginAnimation([Windows.Window]::TopProperty,[Windows.Media.Animation.DoubleAnimation]::new(-100,[TimeSpan]::FromSeconds(5)))
 Pump .02
}}
SetField 'dock' $null;SetField 'apps' $null
Write-Output 'Toggle travel/color/interruption, slider growth/drag/value, choice pill morph/interruption, animation-off, plugin cleanup and default docking on all four edges at two GUI scales passed'
$shell.Shutdown()
