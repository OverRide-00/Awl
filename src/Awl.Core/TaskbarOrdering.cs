using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Interop;
using System.Windows.Threading;

namespace Awl {partial class Shell {
 readonly List<IntPtr> taskbarOrder=new List<IntPtr>();
 readonly HashSet<Button> taskbarReordering=new HashSet<Button>();
 IntPtr taskbarPendingFront,taskbarObservedForeground;
 DateTime taskbarPromotionDue;bool taskbarOrderInitialized,taskbarRefreshQueued;
 DispatcherTimer taskbarPromotionTimer;
 delegate void TaskbarWinEvent(IntPtr hook,uint type,IntPtr window,int objectId,int childId,uint thread,uint time);
 [DllImport("user32.dll")]static extern IntPtr SetWinEventHook(uint first,uint last,IntPtr module,TaskbarWinEvent callback,uint process,uint thread,uint flags);
 [DllImport("user32.dll")]static extern bool UnhookWinEvent(IntPtr hook);
 TaskbarWinEvent taskbarEventCallback;IntPtr taskbarForegroundHook,taskbarShowHook;
 void QueueTaskbarPromotion(IntPtr handle){
  taskbarObservedForeground=handle;
  if(handle==IntPtr.Zero||taskbarOrder.Count==0)return;if(taskbarOrder[0]==handle){taskbarPendingFront=IntPtr.Zero;if(taskbarPromotionTimer!=null)taskbarPromotionTimer.Stop();return;}if(taskbarPendingFront==handle)return;
  taskbarPendingFront=handle;taskbarPromotionDue=DateTime.UtcNow.AddSeconds(1);
  if(taskbarPromotionTimer==null){taskbarPromotionTimer=new DispatcherTimer();taskbarPromotionTimer.Tick+=delegate{taskbarPromotionTimer.Stop();if(!closing&&apps!=null)RefreshApps();};}
  taskbarPromotionTimer.Stop();taskbarPromotionTimer.Interval=TimeSpan.FromSeconds(1);taskbarPromotionTimer.Start();
 }
 List<Tuple<IntPtr,string,string>> StableTaskbarWindows(List<Tuple<IntPtr,string,string>> windows,out bool moved){
  moved=false;var current=new HashSet<IntPtr>(windows.Select(w=>w.Item1));taskbarOrder.RemoveAll(h=>!current.Contains(h));
  foreach(var window in windows)if(!taskbarOrder.Contains(window.Item1))taskbarOrder.Add(window.Item1);
  var foreground=Native.GetForegroundWindow();
  if(!taskbarOrderInitialized){taskbarOrderInitialized=true;taskbarObservedForeground=foreground;}
  else if(current.Contains(foreground)&&foreground!=taskbarObservedForeground)QueueTaskbarPromotion(foreground);
  if(taskbarPendingFront!=IntPtr.Zero&&DateTime.UtcNow>=taskbarPromotionDue){
   var pending=taskbarPendingFront;taskbarPendingFront=IntPtr.Zero;
   if(current.Contains(pending)&&taskbarOrder.IndexOf(pending)>0){taskbarOrder.Remove(pending);taskbarOrder.Insert(0,pending);moved=true;}
  }
  return windows.OrderBy(w=>taskbarOrder.IndexOf(w.Item1)).ToList();
 }
 void StartTaskbarTracking(){
  taskbarEventCallback=delegate(IntPtr hook,uint type,IntPtr handle,int objectId,int childId,uint thread,uint time){
   if(handle==IntPtr.Zero||objectId!=0||childId!=0)return;uint pid;Native.GetWindowThreadProcessId(handle,out pid);if(pid==selfPid||(Native.GetWindowLong(handle,-20)&0x80)!=0)return;
   Dispatcher.BeginInvoke(DispatcherPriority.Background,new Action(()=>{
    if(closing)return;if(type==3)QueueTaskbarPromotion(handle);
    if(taskbarRefreshQueued)return;taskbarRefreshQueued=true;
    Dispatcher.BeginInvoke(DispatcherPriority.Loaded,new Action(()=>{taskbarRefreshQueued=false;if(!closing&&apps!=null)RefreshApps();}));
   }));
  };
  taskbarForegroundHook=SetWinEventHook(3,3,IntPtr.Zero,taskbarEventCallback,0,0,0);
  taskbarShowHook=SetWinEventHook(0x8002,0x8002,IntPtr.Zero,taskbarEventCallback,0,0,0);
 }
 void StopTaskbarTracking(){if(taskbarPromotionTimer!=null)taskbarPromotionTimer.Stop();if(taskbarForegroundHook!=IntPtr.Zero)UnhookWinEvent(taskbarForegroundHook);if(taskbarShowHook!=IntPtr.Zero)UnhookWinEvent(taskbarShowHook);taskbarForegroundHook=taskbarShowHook=IntPtr.Zero;}
 Dictionary<IntPtr,Point> TaskbarButtonPositions(){
  var positions=new Dictionary<IntPtr,Point>();if(apps==null)return positions;
  foreach(var button in apps.Children.OfType<Button>())if(button.Tag is IntPtr&&button.IsLoaded)positions[(IntPtr)button.Tag]=button.PointToScreen(new Point());return positions;
 }
 void AnimateTaskbarReorder(Dictionary<IntPtr,Point> before){
  var api=VisualMotionApi;if(api==null||!cfg.Animations||!visualMotion.taskbarReorder||before.Count==0)return;
  apps.UpdateLayout();
  var promoted=apps.Children.OfType<Button>().FirstOrDefault(b=>b.Tag is IntPtr);bool vertical=apps.Orientation==Orientation.Vertical;double gui=SurfaceScale("Taskbar");
  foreach(var button in apps.Children.OfType<Button>()){
   Point old;if(!(button.Tag is IntPtr)||!before.TryGetValue((IntPtr)button.Tag,out old))continue;
   var now=button.PointToScreen(new Point());var source=System.Windows.PresentationSource.FromVisual(button);var delta=new Vector(old.X-now.X,old.Y-now.Y);if(source!=null)delta=source.CompositionTarget.TransformFromDevice.Transform(delta);
   if(delta.Length<.5)continue;bool moving=button==promoted;var previous=button.RenderTransform;var origin=button.RenderTransformOrigin;var shift=new TranslateTransform(delta.X,delta.Y);var scale=new ScaleTransform(1,1);var tilt=new RotateTransform();var face=new TransformGroup();face.Children.Add(previous);face.Children.Add(scale);face.Children.Add(tilt);var group=new TransformGroup();group.Children.Add(face);group.Children.Add(shift);button.RenderTransformOrigin=new Point(.5,.5);taskbarReordering.Add(button);
   // The promoted icon rides above the bar, so growth/lift cannot be clipped by its window.
   Popup overlay=null;double opacity=button.Opacity;
   if(moving&&button.ActualWidth>0&&button.ActualHeight>0){
    var bitmap=new RenderTargetBitmap((int)Math.Ceiling(button.ActualWidth),(int)Math.Ceiling(button.ActualHeight),96,96,PixelFormats.Pbgra32);bitmap.Render(button);bitmap.Freeze();
    var copy=new Image{Source=bitmap,Width=button.ActualWidth,Height=button.ActualHeight,RenderTransform=group,RenderTransformOrigin=new Point(.5,.5)};var host=new Border{Padding=new Thickness(32*gui),Child=copy,IsHitTestVisible=false};
    Point location=now;if(source!=null)location=source.CompositionTarget.TransformFromDevice.Transform(location);
    overlay=new Popup{Placement=PlacementMode.AbsolutePoint,HorizontalOffset=location.X-32*gui,VerticalOffset=location.Y-32*gui,AllowsTransparency=true,StaysOpen=true,Child=host};
    overlay.Opened+=delegate{var hwnd=PresentationSource.FromVisual(host) as HwndSource;if(hwnd!=null)Native.SetWindowLong(hwnd.Handle,-20,Native.GetWindowLong(hwnd.Handle,-20)|0x20|0x08000000);};overlay.IsOpen=true;button.Opacity=0;
   }
   button.RenderTransform=group;
   IAwlAnimationHandle handle=null;bool done=false;Action restore=null;
   RoutedEventHandler unloaded=null;restore=()=>{if(done)return;done=true;if(handle!=null)handle.Cancel();if(overlay!=null)overlay.IsOpen=false;taskbarReordering.Remove(button);button.Opacity=opacity;button.RenderTransform=previous;button.RenderTransformOrigin=origin;button.Unloaded-=unloaded;visualMotionCleanup.Remove(restore);};visualMotionCleanup.Add(restore);unloaded=delegate{restore();};button.Unloaded+=unloaded;
   handle=api.Animate(new AwlAnimationSpec{DurationMs=MotionDuration(visualMotion.reorderDurationMs),ProceduralCurve=p=>p},p=>{
    double travel=Math.Max(0,Math.Min(1,(p-(moving?.16:.26))/(moving?.66:.69)));double eased=api.Evaluate(AwlAnimationCurve.BackOut,travel,moving?.55:1.35);
    shift.X=delta.X*(1-eased);shift.Y=delta.Y*(1-eased);
    if(moving){double grow=MotionPhase(p,0,.24)*(1-MotionPhase(p,.76,1));scale.ScaleX=scale.ScaleY=1+.13*grow;tilt.Angle=-7*Math.Sin(Math.PI*Math.Min(1,p/.3))*(1-MotionPhase(p,.2,.4));double lift=8*Math.Sin(Math.PI*Math.Min(1,p/.3))+3*Math.Sin(Math.PI*MotionPhase(p,.82,1));if(vertical)shift.X+=(cfg.Position=="Left"?1:-1)*lift*gui;else shift.Y+=(cfg.Position=="Top"?1:-1)*lift*gui;}
   },restore);
  }
 }
}}
