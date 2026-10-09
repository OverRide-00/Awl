using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Awl {partial class Shell {
 readonly Dictionary<FrameworkElement,Action> surfaceMotionStops=new Dictionary<FrameworkElement,Action>();
 bool quickSearchRevealing;
 void InterruptSearchReveal(){if(quickSearchRevealing&&restoreSearchMotion!=null)restoreSearchMotion();}
 void StopVisualSurface(FrameworkElement surface){Action stop;if(surface!=null&&surfaceMotionStops.TryGetValue(surface,out stop))stop();}
 bool AnimateVisualSurface(FrameworkElement surface,bool toolbar){
  var api=VisualMotionApi;if(api==null||!cfg.Animations||surface==null||(toolbar?!visualMotion.topbarPopups:!visualMotion.captureBars))return false;
  StopVisualSurface(surface);var previous=surface.RenderTransform;var origin=surface.RenderTransformOrigin;double opacity=surface.Opacity;
  var scale=new ScaleTransform(.92,.92);var lift=new TranslateTransform(0,toolbar?-10:0);var group=new TransformGroup();group.Children.Add(previous);group.Children.Add(scale);group.Children.Add(lift);surface.RenderTransform=group;surface.RenderTransformOrigin=new Point(.5,toolbar?0:.5);surface.Opacity=0;
  IAwlAnimationHandle handle=null;bool done=false;Action restore=null;RoutedEventHandler unloaded=null;
  restore=()=>{if(done)return;done=true;if(handle!=null)handle.Cancel();surface.RenderTransform=previous;surface.RenderTransformOrigin=origin;surface.Opacity=opacity;surfaceMotionStops.Remove(surface);visualMotionCleanup.Remove(restore);if(unloaded!=null)surface.Unloaded-=unloaded;};
  surfaceMotionStops[surface]=restore;visualMotionCleanup.Add(restore);unloaded=delegate{restore();};surface.Unloaded+=unloaded;
  handle=api.Animate(new AwlAnimationSpec{DurationMs=MotionDuration(toolbar?visualMotion.panelDurationMs:visualMotion.captureDurationMs),ProceduralCurve=p=>p},p=>{double spring=api.Evaluate(AwlAnimationCurve.BackOut,p,.22);scale.ScaleX=scale.ScaleY=.92+.08*spring;lift.Y=toolbar?-10*(1-api.Evaluate(AwlAnimationCurve.EaseOut,p)):0;surface.Opacity=opacity*Math.Min(1,p*2.4);},restore);return true;
 }
 bool MotionPanelEnabled(string kind){
  return visualMotion!=null&&(kind=="Pinned"?visualMotion.pinnedPanels:kind=="Dropdown"?visualMotion.dropdowns:kind=="Taskbar"?visualMotion.taskbarPopups:visualMotion.topbarPopups);
 }
 void BindVisualPopup(System.Windows.Controls.Primitives.Popup popup,string kind){
  popup.Opened+=delegate{AnimateVisualPanel(popup.Child as FrameworkElement,kind,popup.Placement);};
  popup.Closed+=delegate{StopVisualSurface(popup.Child as FrameworkElement);};
 }
 bool AnimateVisualPanel(FrameworkElement surface,string kind,System.Windows.Controls.Primitives.PlacementMode placement){
  var api=VisualMotionApi;if(api==null||!cfg.Animations||surface==null||!MotionPanelEnabled(kind))return false;
  StopVisualSurface(surface);var previous=surface.RenderTransform;var origin=surface.RenderTransformOrigin;double opacity=surface.Opacity;
  bool side=placement==System.Windows.Controls.Primitives.PlacementMode.Left||placement==System.Windows.Controls.Primitives.PlacementMode.Right;
  var scale=new ScaleTransform(side?.86:1,side?1:.86);var transforms=new TransformGroup();transforms.Children.Add(previous);transforms.Children.Add(scale);surface.RenderTransform=transforms;
  surface.RenderTransformOrigin=placement==System.Windows.Controls.Primitives.PlacementMode.Top?new Point(.5,1):placement==System.Windows.Controls.Primitives.PlacementMode.Left?new Point(1,.5):placement==System.Windows.Controls.Primitives.PlacementMode.Right?new Point(0,.5):new Point(.5,0);surface.Opacity=0;
  IAwlAnimationHandle handle=null;bool done=false;Action restore=null;RoutedEventHandler unloaded=null;
  restore=()=>{if(done)return;done=true;if(handle!=null)handle.Cancel();surface.RenderTransform=previous;surface.RenderTransformOrigin=origin;surface.Opacity=opacity;surfaceMotionStops.Remove(surface);visualMotionCleanup.Remove(restore);surface.Unloaded-=unloaded;};
  surfaceMotionStops[surface]=restore;visualMotionCleanup.Add(restore);unloaded=delegate{restore();};surface.Unloaded+=unloaded;
  handle=api.Animate(new AwlAnimationSpec{DurationMs=MotionDuration(visualMotion.panelDurationMs),ProceduralCurve=p=>p},p=>{double size=.86+.14*api.Evaluate(AwlAnimationCurve.EaseOut,p);if(side)scale.ScaleX=size;else scale.ScaleY=size;surface.Opacity=opacity*Math.Min(1,p*2.5);},restore);return true;
 }
 bool AnimateSearchReveal(){
  var api=VisualMotionApi;if(api==null||!cfg.Animations||!visualMotion.quickSearch)return false;
  if(restoreSearchMotion!=null)restoreSearchMotion();var window=quickSearchWindow;var surface=window.Content as Border;if(surface==null)return false;
  window.UpdateLayout();double width=window.Width,height=window.Height,gui=SurfaceScale("Quick search");
  var root=new Grid{Width=width,Height=height};var face=new Border{Background=surface.Background,BorderBrush=surface.BorderBrush,BorderThickness=surface.BorderThickness,CornerRadius=new CornerRadius(height/2),HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center,Width=0,Height=0};
  var art=BuilderSvgIcon("search",24*gui,ink);art.HorizontalAlignment=HorizontalAlignment.Center;art.VerticalAlignment=VerticalAlignment.Center;var move=new TranslateTransform();art.RenderTransform=move;
  double opacity=surface.Opacity;var originalBackground=surface.Background;var originalBorder=surface.BorderBrush;surface.Background=Brushes.Transparent;surface.BorderBrush=Brushes.Transparent;surface.Opacity=0;
  window.Content=null;root.Children.Add(face);root.Children.Add(surface);root.Children.Add(art);window.Content=root;
  var falling=quickSearchHeader==null?new List<Button>():FindVisual<Button>(quickSearchHeader).ToList();var oldTransforms=falling.Select(b=>b.RenderTransform).ToArray();var oldOpacity=falling.Select(b=>b.Opacity).ToArray();var drops=falling.Select(b=>new TranslateTransform(0,-18)).ToArray();
  for(int i=0;i<falling.Count;i++){var transforms=new TransformGroup();transforms.Children.Add(oldTransforms[i]);transforms.Children.Add(drops[i]);falling[i].RenderTransform=transforms;falling[i].Opacity=0;}
  IAwlAnimationHandle handle=null;bool done=false;
  Action restore=()=>{if(done)return;done=true;if(handle!=null)handle.Cancel();root.Children.Remove(surface);if(Object.ReferenceEquals(window.Content,root))window.Content=surface;surface.Background=originalBackground;surface.BorderBrush=originalBorder;surface.Opacity=opacity;for(int i=0;i<falling.Count;i++){falling[i].RenderTransform=oldTransforms[i];falling[i].Opacity=oldOpacity[i];}quickSearchRevealing=false;restoreSearchMotion=null;if(quickSearchInput!=null)quickSearchInput.Focus();};
  restoreSearchMotion=restore;quickSearchRevealing=true;if(quickSearchInput!=null)quickSearchInput.Focus();
  Action<double> frame=p=>{
   // Hold the magnifier, grow a circle, pause, then extend the pill before revealing controls.
   double circle=api.Evaluate(AwlAnimationCurve.EaseOut,Math.Max(0,(p-.18)/.20));
   double expand=api.Evaluate(AwlAnimationCurve.EaseOut,Math.Max(0,(p-.48)/.28));
   face.Height=height*circle;face.Width=face.Height+(width-face.Height)*expand;
   double reveal=api.Evaluate(AwlAnimationCurve.SmoothStep,Math.Max(0,(p-.80)/.20));surface.Opacity=opacity*reveal;
   double finalRadius=surface.CornerRadius.TopLeft*gui;face.CornerRadius=new CornerRadius(height/2+(finalRadius-height/2)*expand);
   move.X=(30*gui-width/2)*expand;if(art is Viewbox){art.Width=art.Height=(24-6*expand)*gui;}art.Opacity=1-reveal;
   for(int i=0;i<falling.Count;i++){double fall=Math.Max(0,Math.Min(1,(p-.80-i*.025)/.12));drops[i].Y=-18*(1-api.Evaluate(AwlAnimationCurve.BackOut,fall,.38));falling[i].Opacity=oldOpacity[i]*Math.Min(1,fall*2);}
  };
  frame(0);handle=api.Animate(new AwlAnimationSpec{DurationMs=MotionDuration(visualMotion.searchRevealDurationMs),ProceduralCurve=p=>p},frame,restore);return true;
 }
}}
