using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using System.Web.Script.Serialization;

namespace Awl {
 // Visual packages describe motion; the shared API owns its frame scheduling.
 public sealed class AwlVisualMotionProfile {
  public string requires="awl.animation-api";
  public bool startMenu=true,quickSearch=true,taskbarHover=true,appLaunch=true;
  public bool taskbarReorder=true,captureBars=true,topbarPopups=true,pinnedPanels=true,taskbarPopups=true,dropdowns=true,toggleSwitches=true,sliderKnobs=true,segmentedChoices=true;
  public double menuDurationMs=280,searchDurationMs=240,hoverDurationMs=140,launchDurationMs=620,exitDurationMs=130;
  public double windowDurationMs=340;
  public double reorderDurationMs=520,searchRevealDurationMs=1000,captureDurationMs=240,panelDurationMs=240,controlDurationMs=380,toggleDurationMs=340;
  public double hoverScale=1.18,hoverLift=8,bounceHeight=28,tiltDegrees=14;
 }
 partial class Shell {
  AwlVisualMotionProfile visualMotion;
  readonly List<Action> visualMotionCleanup=new List<Action>();
  Action restoreMenuMotion,restoreSearchMotion;
  bool launcherMotionClosing;
  readonly HashSet<IntPtr> motionKnownWindows=new HashSet<IntPtr>();
  bool motionWindowsInitialized;
  IAwlAnimationApi VisualMotionApi {get {object service;return visualMotion!=null&&pluginServices.TryGetValue(visualMotion.requires,out service)?service as IAwlAnimationApi:null;}}
  double MotionDuration(double duration){return Math.Max(60,Math.Min(2000,duration*Math.Max(.4,Math.Min(2,cfg.AnimationMs/240.0))));}
  void ResetVisualMotion(){
   ResetControlMotion();
   if(restoreMenuMotion!=null)restoreMenuMotion();if(restoreSearchMotion!=null)restoreSearchMotion();
   foreach(var stop in visualMotionCleanup.ToArray())stop();visualMotionCleanup.Clear();visualMotion=null;
   StopNativeAppMotions();
  }
  void LoadVisualMotion(InstalledPlugin plugin){
   if(String.IsNullOrWhiteSpace(plugin.Manifest.motion))return;
   string file=SafePluginFile(plugin.Folder,plugin.Manifest.motion);
   var profile=new JavaScriptSerializer().Deserialize<AwlVisualMotionProfile>(File.ReadAllText(file));
   if(profile==null||profile.requires!="awl.animation-api")throw new InvalidDataException("Visual motion requires awl.animation-api.");
   foreach(double value in new[]{profile.menuDurationMs,profile.searchDurationMs,profile.hoverDurationMs,profile.launchDurationMs,profile.windowDurationMs,profile.exitDurationMs,profile.reorderDurationMs,profile.searchRevealDurationMs,profile.captureDurationMs,profile.panelDurationMs,profile.controlDurationMs,profile.toggleDurationMs,profile.hoverScale,profile.hoverLift,profile.bounceHeight,profile.tiltDegrees})
    if(Double.IsNaN(value)||Double.IsInfinity(value)||value<0)throw new InvalidDataException("Motion values must be finite and nonnegative.");
   profile.hoverScale=Math.Max(1,Math.Min(1.3,profile.hoverScale));profile.hoverLift=Math.Min(12,profile.hoverLift);profile.bounceHeight=Math.Min(32,profile.bounceHeight);profile.tiltDegrees=Math.Min(20,profile.tiltDegrees);
   visualMotion=profile;
  }
  // Each landing starts a shorter, lower rebound, like a ball losing energy.
  static double AppIconBounce(double p){
   p=Math.Max(0,Math.Min(1,p));
   if(p<.18)return 1-Math.Pow(1-p/.18,2);
   if(p<.52)return 1-Math.Pow((p-.18)/.34,2);
   double start,end,height;
   if(p<.76){start=.52;end=.76;height=.32;}
   else if(p<.90){start=.76;end=.90;height=.12;}
   else {start=.90;end=1;height=.035;}
   double t=(p-start)/(end-start);
   return height*4*t*(1-t);
  }
  bool AnimateVisualMenu(){
   var api=VisualMotionApi;if(api==null||!cfg.Animations||!visualMotion.startMenu)return false;
   if(restoreMenuMotion!=null)restoreMenuMotion();
   launcher.BeginAnimation(Window.TopProperty,null);launcher.BeginAnimation(Window.OpacityProperty,null);launcher.Opacity=1;
   double left=launcher.Left,topY=launcher.Top,width=launcher.Width,height=launcher.Height;
   var surface=launcherSurface;var ui=surface.Child;double uiOpacity=ui.Opacity;var faceBackground=surface.Background;var faceBorder=surface.BorderBrush;
   // If Start is on another display, use a taskbar-shaped origin on that display.
   var bounds=SurfaceDipBounds("Start menu");double barWidth=dock!=null&&dock.IsVisible?dock.Width:Math.Min(width,480),barHeight=dock!=null?dock.Height:62;
   double x=dock!=null?dock.Left:bounds.Left+(bounds.Width-barWidth)/2,y=dock!=null?dock.Top:bounds.Bottom-barHeight;
   if(x<bounds.Left||x>=bounds.Right||y<bounds.Top||y>=bounds.Bottom){x=bounds.Left+(bounds.Width-barWidth)/2;y=bounds.Bottom-barHeight;}
   double startX=Math.Max(.05,Math.Min(2,barWidth/width)),startY=Math.Max(.025,Math.Min(1,barHeight/height));
   // A fixed envelope avoids relaying out the full menu on every animation frame.
   double envelopeLeft=Math.Min(left,x),envelopeTop=Math.Min(topY,y);
   var canvas=new Canvas();var wrapper=new Grid{Width=width,Height=height};
   // Morph the empty surface independently; content is never stretched.
   var shellFace=new Border{Background=surface.Background,BorderBrush=surface.BorderBrush,BorderThickness=surface.BorderThickness,CornerRadius=surface.CornerRadius};
   var contentLayer=new Grid{Width=width,Height=height};launcher.Content=null;contentLayer.Children.Add(surface);wrapper.Children.Add(shellFace);canvas.Children.Add(wrapper);canvas.Children.Add(contentLayer);launcher.Content=canvas;surface.Background=Brushes.Transparent;surface.BorderBrush=Brushes.Transparent;ui.Opacity=0;
   launcher.Left=envelopeLeft;launcher.Top=envelopeTop;
   launcher.Width=Math.Max(left+width,x+barWidth)-envelopeLeft;launcher.Height=Math.Max(topY+height,y+barHeight)-envelopeTop;
   var scale=new ScaleTransform(1,1);var offset=new TranslateTransform();var transform=new TransformGroup();transform.Children.Add(scale);transform.Children.Add(offset);wrapper.RenderTransform=transform;
   IAwlAnimationHandle handle=null;bool restored=false;
   Action restore=()=>{if(restored)return;restored=true;if(handle!=null)handle.Cancel();contentLayer.Children.Remove(surface);surface.Background=faceBackground;surface.BorderBrush=faceBorder;ui.Opacity=uiOpacity;if(Object.ReferenceEquals(launcher.Content,canvas))launcher.Content=surface;launcher.Left=left;launcher.Top=topY;launcher.Width=width;launcher.Height=height;launcher.Opacity=1;restoreMenuMotion=null;};
   restoreMenuMotion=restore;
   Action<double> frame=p=>{
    double travel=api.Evaluate(AwlAnimationCurve.EaseOut,Math.Min(1,p/.7));
    double reshape=api.Evaluate(AwlAnimationCurve.EaseOut,Math.Min(1,p/.7));
    scale.ScaleX=startX+(1-startX)*reshape;scale.ScaleY=startY+(1-startY)*travel;
    double centerX=x+barWidth/2+(left+width/2-x-barWidth/2)*travel;
    double centerY=y+barHeight/2+(topY+height/2-y-barHeight/2)*travel;
    offset.X=centerX-width*scale.ScaleX/2-envelopeLeft;offset.Y=centerY-height*scale.ScaleY/2-envelopeTop;
    Canvas.SetLeft(contentLayer,left-envelopeLeft);Canvas.SetTop(contentLayer,topY-envelopeTop);
    // Wait until the shell has settled, then reveal sharp UI at its final size.
    ui.Opacity=uiOpacity*api.Evaluate(AwlAnimationCurve.SmoothStep,Math.Max(0,(p-.7)/.3));
   };
   frame(0);handle=api.Animate(new AwlAnimationSpec{DurationMs=MotionDuration(visualMotion.menuDurationMs),ProceduralCurve=p=>p},frame,restore);
   return true;
  }
  void CloseLauncher(){
   if(launcher==null||!launcher.IsVisible||launcherMotionClosing)return;
   if(restoreMenuMotion!=null)restoreMenuMotion();
   var api=VisualMotionApi;
   if(api==null||!cfg.Animations||!visualMotion.startMenu){launcher.Hide();return;}
   launcherMotionClosing=true;IAwlAnimationHandle handle=null;bool done=false;
   Action finish=()=>{if(done)return;done=true;if(handle!=null)handle.Cancel();launcher.Hide();launcher.Opacity=1;launcherMotionClosing=false;restoreMenuMotion=null;};
   restoreMenuMotion=finish;handle=api.Animate(new AwlAnimationSpec{DurationMs=MotionDuration(visualMotion.exitDurationMs),ProceduralCurve=p=>p},p=>launcher.Opacity=1-p,finish);
  }
  bool AnimateVisualSearch(bool opening,Action completed=null){
   if(opening&&quickSearchWindow!=null&&quickSearchWindow.Content is Border)return AnimateSearchReveal();
   var api=VisualMotionApi;if(api==null||!cfg.Animations||!visualMotion.quickSearch)return false;
   if(restoreSearchMotion!=null)restoreSearchMotion();
   var window=quickSearchWindow;var surface=window.Content as FrameworkElement;if(surface==null)return false;
   var previous=surface.RenderTransform;var origin=surface.RenderTransformOrigin;var scale=new ScaleTransform(1,1);
   surface.RenderTransformOrigin=new Point(.5,.5);surface.RenderTransform=scale;
   IAwlAnimationHandle handle=null;bool restored=false;
   Action restore=()=>{if(restored)return;restored=true;if(handle!=null)handle.Cancel();surface.RenderTransform=previous;surface.RenderTransformOrigin=origin;window.Opacity=1;restoreSearchMotion=null;};
   restoreSearchMotion=restore;
   Action<double> frame=p=>{double shaped=opening?api.Evaluate(AwlAnimationCurve.BackOut,p,.22):p;scale.ScaleX=scale.ScaleY=opening?.92+.08*shaped:1-.04*p;window.Opacity=opening?Math.Min(1,p*2.4):1-p;};
   frame(0);handle=api.Animate(new AwlAnimationSpec{DurationMs=MotionDuration(opening?visualMotion.searchDurationMs:visualMotion.exitDurationMs),ProceduralCurve=p=>p},frame,()=>{restore();if(completed!=null)completed();});return true;
  }
  void BindVisualTaskbarMotion(Button button,bool opening){
   var api=VisualMotionApi;if(api==null||!cfg.Animations)return;
   var element=button.Content as FrameworkElement;if(element==null)return;
   var previous=element.RenderTransform;var origin=element.RenderTransformOrigin;var buttonTransform=button.RenderTransform;button.RenderTransform=Transform.Identity;var scale=new ScaleTransform();var rotate=new RotateTransform();var translate=new TranslateTransform();var transforms=new TransformGroup();transforms.Children.Add(previous);transforms.Children.Add(scale);transforms.Children.Add(rotate);transforms.Children.Add(translate);element.RenderTransform=transforms;element.RenderTransformOrigin=new Point(.5,.5);
   IAwlAnimationHandle hoverHandle=null,bounceHandle=null;double hover=0,bounce=0,tilt=0;bool stopped=false,hovered=false;
   double originalOpacity=element.Opacity;Popup popup=null;
   Action hideIcon=()=>{if(popup!=null){popup.IsOpen=false;popup=null;}element.Opacity=originalOpacity;};
   Action showIcon=()=>{if(popup!=null)return;button.UpdateLayout();if(element.ActualWidth<1||element.ActualHeight<1)return;
    var bitmap=new System.Windows.Media.Imaging.RenderTargetBitmap((int)Math.Ceiling(element.ActualWidth),(int)Math.Ceiling(element.ActualHeight),96,96,PixelFormats.Pbgra32);bitmap.Render(element);bitmap.Freeze();
    double pad=40*SurfaceScale("Taskbar");var copy=new Image{Source=bitmap,Width=element.ActualWidth,Height=element.ActualHeight,RenderTransform=transforms,RenderTransformOrigin=new Point(.5,.5)};
    var host=new Border{Padding=new Thickness(pad),Child=copy,IsHitTestVisible=false};var at=element.TranslatePoint(new Point(),button);
    popup=new Popup{Placement=PlacementMode.Relative,PlacementTarget=button,HorizontalOffset=at.X-pad,VerticalOffset=at.Y-pad,AllowsTransparency=true,StaysOpen=true,Child=host};
    popup.Opened+=delegate{var source=PresentationSource.FromVisual(host) as HwndSource;if(source!=null)Native.SetWindowLong(source.Handle,-20,Native.GetWindowLong(source.Handle,-20)|0x20|0x08000000);};popup.IsOpen=true;element.Opacity=0;
   };
   double gui=SurfaceScale("Taskbar");bool vertical=cfg.Position=="Left"||cfg.Position=="Right";
   Action apply=()=>{scale.ScaleX=scale.ScaleY=1+(visualMotion.hoverScale-1)*hover;rotate.Angle=tilt;double lift=(visualMotion.hoverLift*hover+bounce)*gui;translate.X=vertical?(cfg.Position=="Left"?lift:-lift):0;translate.Y=vertical?0:(cfg.Position=="Top"?lift:-lift);};
   Action<double> follow=target=>{if(stopped||!cfg.Animations||!visualMotion.taskbarHover||taskbarReordering.Contains(button))return;hovered=target>0;if(hovered)showIcon();if(hoverHandle!=null)hoverHandle.Cancel();double from=hover;hoverHandle=api.Animate(new AwlAnimationSpec{DurationMs=MotionDuration(visualMotion.hoverDurationMs),ProceduralCurve=p=>api.Evaluate(AwlAnimationCurve.BackOut,p,.35)},p=>{hover=from+(target-from)*p;apply();},()=>{if(!hovered&&(bounceHandle==null||!bounceHandle.IsRunning))hideIcon();});};
   Action launch=()=>{if(stopped||!cfg.Animations||!visualMotion.appLaunch)return;showIcon();if(bounceHandle!=null)bounceHandle.Cancel();bounceHandle=api.Animate(new AwlAnimationSpec{DurationMs=MotionDuration(visualMotion.launchDurationMs),ProceduralCurve=p=>p},p=>{double arc=AppIconBounce(p);bounce=visualMotion.bounceHeight*arc;tilt=visualMotion.tiltDegrees*Math.Sin(2*Math.PI*p)*(1-p);apply();},()=>{bounce=tilt=0;apply();if(!hovered)hideIcon();});};
   Action stop=null;stop=()=>{if(stopped)return;stopped=true;if(hoverHandle!=null)hoverHandle.Cancel();if(bounceHandle!=null)bounceHandle.Cancel();hideIcon();element.RenderTransform=previous;element.RenderTransformOrigin=origin;button.RenderTransform=buttonTransform;visualMotionCleanup.Remove(stop);};visualMotionCleanup.Add(stop);
   button.MouseEnter+=delegate{follow(1);};button.MouseLeave+=delegate{follow(0);};button.Click+=delegate{launch();};button.Unloaded+=delegate{stop();};button.IsVisibleChanged+=delegate{if(!button.IsVisible)hideIcon();};
   if(opening)button.Dispatcher.BeginInvoke(DispatcherPriority.Loaded,launch);
  }
 }
}
