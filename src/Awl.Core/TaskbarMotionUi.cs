using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Awl {partial class Shell {
 void TaskbarAppTooltip(Button button,string title){
  var background=SurfaceBrush(baseColor,1,false);var stack=new StackPanel();
  var label=Text(title,12,ink);label.MaxWidth=340;label.TextWrapping=TextWrapping.NoWrap;label.TextTrimming=TextTrimming.CharacterEllipsis;
  stack.Children.Add(new Border{Background=background,BorderBrush=SoftBorder(),BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(9),Padding=new Thickness(10,6,10,6),Child=label});
  stack.Children.Add(new Path{Data=Geometry.Parse("M 0,0 L 6,6 L 12,0 Z"),Fill=background,Stroke=SoftBorder(),StrokeThickness=1,Width=12,Height=7,HorizontalAlignment=HorizontalAlignment.Center,Margin=new Thickness(0,-1,0,0)});
  ApplySurfaceScale(stack,"Taskbar");
  var tip=new ToolTip{Content=stack,PlacementTarget=button,Placement=PlacementMode.Custom,Background=Brushes.Transparent,BorderThickness=new Thickness(0),Padding=new Thickness(0)};
  var template=new ControlTemplate(typeof(ToolTip));var presenter=new FrameworkElementFactory(typeof(ContentPresenter));presenter.SetValue(ContentPresenter.ContentProperty,new TemplateBindingExtension(ContentControl.ContentProperty));template.VisualTree=presenter;tip.Template=template;
  tip.CustomPopupPlacementCallback=(size,target,offset)=>new[]{new CustomPopupPlacement(new Point((target.Width-size.Width)/2,-size.Height-9*SurfaceScale("Taskbar")),PopupPrimaryAxis.Horizontal)};
  button.ToolTip=tip;ToolTipService.SetInitialShowDelay(button,260);ToolTipService.SetBetweenShowDelay(button,80);ToolTipService.SetShowDuration(button,12000);
  button.Unloaded+=delegate{tip.IsOpen=false;};
 }
 void ActivateTaskbarApp(IntPtr handle,Button button){
  QueueTaskbarPromotion(handle);if(Native.IsIconic(handle))Native.ShowWindowAsync(handle,9);Native.SetForegroundWindow(handle);
  // Let a restored window publish its final bounds before creating its compositor preview.
  var timer=new System.Windows.Threading.DispatcherTimer{Interval=TimeSpan.FromMilliseconds(35)};int attempts=0;
  timer.Tick+=delegate{if(Native.IsIconic(handle)&&++attempts<8)return;timer.Stop();AnimateNativeApp(handle,button);};timer.Start();
 }
}}
