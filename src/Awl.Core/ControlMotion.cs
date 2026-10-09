using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Awl { partial class Shell {
 readonly Dictionary<FrameworkElement,Action> controlMotionStops=new Dictionary<FrameworkElement,Action>();
 readonly Dictionary<Button,MotionChoiceGroup> motionChoices=new Dictionary<Button,MotionChoiceGroup>();
 static readonly DependencyProperty MotionChoiceRadiusProperty=DependencyProperty.RegisterAttached("MotionChoiceRadius",typeof(CornerRadius),typeof(Shell),new PropertyMetadata(new CornerRadius(0)));
 DateTime controlRedrawUntil;
 DispatcherTimer controlRedrawTimer;
 readonly HashSet<Action> deferredControlUpdates=new HashSet<Action>();
 sealed class MotionChoiceGroup {
  public Panel Panel;public List<Button> Buttons;public int Selected;
  public ControlTemplate[] Templates;public Thickness[] Margins;
  public Action Restore;public bool Stopped;
  public readonly HashSet<Button> Hovered=new HashSet<Button>();
 }
 Color MotionBrushColor(Brush brush){var solid=brush as SolidColorBrush;return solid==null?Colors.Transparent:solid.Color;}
 Color MotionColor(Color a,Color b,double p){return Color.FromArgb((byte)Math.Round(a.A+(b.A-a.A)*p),(byte)Math.Round(a.R+(b.R-a.R)*p),(byte)Math.Round(a.G+(b.G-a.G)*p),(byte)Math.Round(a.B+(b.B-a.B)*p));}
 CornerRadius MotionRadius(CornerRadius a,CornerRadius b,double p){return new CornerRadius(a.TopLeft+(b.TopLeft-a.TopLeft)*p,a.TopRight+(b.TopRight-a.TopRight)*p,a.BottomRight+(b.BottomRight-a.BottomRight)*p,a.BottomLeft+(b.BottomLeft-a.BottomLeft)*p);}
 void HoldControlRedraw(double duration=-1){controlRedrawUntil=DateTime.UtcNow.AddMilliseconds(MotionDuration(duration<0?visualMotion.controlDurationMs:duration)+25);}
 bool DeferControlRedraw(){return DeferControlUpdate(DrawLauncher);}
 bool DeferControlUpdate(Action update){
  double remaining=(controlRedrawUntil-DateTime.UtcNow).TotalMilliseconds;if(remaining<=0)return false;deferredControlUpdates.Add(update);
  if(controlRedrawTimer==null){controlRedrawTimer=new DispatcherTimer();controlRedrawTimer.Tick+=delegate{controlRedrawTimer.Stop();var updates=deferredControlUpdates.ToArray();deferredControlUpdates.Clear();foreach(var action in updates)action();};}
  controlRedrawTimer.Stop();controlRedrawTimer.Interval=TimeSpan.FromMilliseconds(Math.Max(1,remaining));controlRedrawTimer.Start();return true;
 }
 void StopControlMotion(FrameworkElement element){Action stop;if(controlMotionStops.TryGetValue(element,out stop))stop();}
 void RunControlMotion(FrameworkElement owner,double duration,Action<double> frame,Action finish,bool linear=false){
  StopControlMotion(owner);var api=VisualMotionApi;if(api==null||!cfg.Animations){frame(1);finish();return;}
  IAwlAnimationHandle handle=null;bool done=false;Action stop=null;RoutedEventHandler unloaded=null;
  stop=()=>{if(done)return;done=true;if(handle!=null)handle.Cancel();finish();controlMotionStops.Remove(owner);visualMotionCleanup.Remove(stop);owner.Unloaded-=unloaded;};
  unloaded=delegate{stop();};owner.Unloaded+=unloaded;controlMotionStops[owner]=stop;visualMotionCleanup.Add(stop);frame(0);
  handle=api.Animate(new AwlAnimationSpec{DurationMs=MotionDuration(duration),ProceduralCurve=p=>linear?p:api.Evaluate(AwlAnimationCurve.SmoothStep,p)},frame,stop);
 }
 bool AnimateMotionSwitch(Border track,Border thumb,bool state){
  if(VisualMotionApi==null||!cfg.Animations||!visualMotion.toggleSwitches)return false;
  double travel=Math.Max(0,track.Width-thumb.Width-thumb.Margin.Left-thumb.Margin.Right);
  double before=thumb.HorizontalAlignment==HorizontalAlignment.Right?travel:0;
  var oldGroup=thumb.RenderTransform as TransformGroup;var oldOffset=oldGroup==null?null:oldGroup.Children.LastOrDefault() as TranslateTransform;if(oldOffset!=null)before+=oldOffset.X;
  var oldScale=oldGroup==null?null:oldGroup.Children.OfType<ScaleTransform>().LastOrDefault();double initialScale=oldScale==null?1:oldScale.ScaleX;
  Color fromTrack=MotionBrushColor(track.Background),fromThumb=MotionBrushColor(thumb.Background);
  StopControlMotion(thumb);var previous=thumb.RenderTransform;var origin=thumb.RenderTransformOrigin;var size=new ScaleTransform(initialScale,initialScale);var offset=new TranslateTransform();var transforms=new TransformGroup();transforms.Children.Add(previous);transforms.Children.Add(size);transforms.Children.Add(offset);thumb.RenderTransform=transforms;thumb.RenderTransformOrigin=new Point(.5,.5);
  thumb.HorizontalAlignment=state?HorizontalAlignment.Right:HorizontalAlignment.Left;double target=state?travel:0;
  var trackColor=new SolidColorBrush(fromTrack);var thumbColor=new SolidColorBrush(fromThumb);Color endTrack=MotionBrushColor(state?accentBrush:cardBrush),endThumb=MotionBrushColor(state?new SolidColorBrush(Mix(baseColor,Colors.White,.85)):mutedBrush);
  track.Background=trackColor;thumb.Background=thumbColor;HoldControlRedraw(visualMotion.toggleDurationMs);var api=VisualMotionApi;
  RunControlMotion(thumb,visualMotion.toggleDurationMs,p=>{
   double press=api.Evaluate(AwlAnimationCurve.SmoothStep,Math.Min(1,p/.14));
   double move=Math.Max(0,Math.Min(1,(p-.14)/.7));double eased=api.Evaluate(AwlAnimationCurve.BackOut,move,.9);
   offset.X=(before-target)*(1-eased);
   offset.Y=p>.72?-1.5*Math.Sin(Math.PI*(p-.72)/.28):0;
   double scale=p<.14?initialScale+(.9-initialScale)*press:.9+.1*api.Evaluate(AwlAnimationCurve.EaseOut,Math.Min(1,(p-.14)/.6));
   if(p>.74)scale+=.015*Math.Sin(Math.PI*(p-.74)/.26);size.ScaleX=size.ScaleY=scale;
   double blend=api.Evaluate(AwlAnimationCurve.SmoothStep,move);trackColor.Color=MotionColor(fromTrack,endTrack,blend);thumbColor.Color=MotionColor(fromThumb,endThumb,blend);
  },()=>{thumb.RenderTransform=previous;thumb.RenderTransformOrigin=origin;trackColor.Color=endTrack;thumbColor.Color=endThumb;},true);return true;
 }
 void BindMotionSlider(Thumb thumb){
  if(visualMotion==null||!visualMotion.sliderKnobs)return;
  var previous=thumb.RenderTransform;var origin=thumb.RenderTransformOrigin;var size=new ScaleTransform(1,1);var transforms=new TransformGroup();transforms.Children.Add(previous);transforms.Children.Add(size);thumb.RenderTransform=transforms;thumb.RenderTransformOrigin=new Point(.5,.5);
  bool dragging=false,hovered=thumb.IsMouseOver,stopped=false;Action update=null,restore=null;MouseEventHandler enter=null,leave=null;DragStartedEventHandler start=null;DragCompletedEventHandler complete=null;MouseEventHandler lost=null;RoutedEventHandler unloaded=null;
  update=()=>{if(stopped)return;double from=size.ScaleX,to=VisualMotionApi!=null&&cfg.Animations&&(dragging||hovered)?1.4:1;StopControlMotion(thumb);RunControlMotion(thumb,130,p=>{size.ScaleX=size.ScaleY=from+(to-from)*p;},()=>{size.ScaleX=size.ScaleY=to;});};
  restore=()=>{if(stopped)return;stopped=true;StopControlMotion(thumb);thumb.RenderTransform=previous;thumb.RenderTransformOrigin=origin;thumb.MouseEnter-=enter;thumb.MouseLeave-=leave;thumb.DragStarted-=start;thumb.DragCompleted-=complete;thumb.LostMouseCapture-=lost;thumb.Unloaded-=unloaded;visualMotionCleanup.Remove(restore);};
  enter=delegate{hovered=true;update();};leave=delegate{hovered=false;update();};start=delegate{dragging=true;update();};complete=delegate{dragging=false;update();};lost=delegate{dragging=false;update();};unloaded=delegate{restore();};
  thumb.MouseEnter+=enter;thumb.MouseLeave+=leave;thumb.DragStarted+=start;thumb.DragCompleted+=complete;thumb.LostMouseCapture+=lost;thumb.Unloaded+=unloaded;visualMotionCleanup.Add(restore);
 }
 CornerRadius ChoiceRadius(MotionChoiceGroup group,int index){
  double radius=Math.Max(1,group.Buttons[index].ActualHeight/2);if(index==group.Selected)return new CornerRadius(radius);
  double seam=group.Hovered.Contains(group.Buttons[index])?Math.Min(6,radius*.35):0;
  return new CornerRadius(index==0?radius:seam,index==group.Buttons.Count-1?radius:seam,index==group.Buttons.Count-1?radius:seam,index==0?radius:seam);
 }
 Color ChoiceBackground(MotionChoiceGroup group,int index){Color color=MotionBrushColor(cardBrush);if(group.Hovered.Contains(group.Buttons[index]))color=Color.FromArgb(color.A,(byte)(color.R*.92),(byte)(color.G*.92),(byte)(color.B*.92));return color;}
 void HoverMotionChoice(Button button,bool hovered){
  MotionChoiceGroup group;if(!motionChoices.TryGetValue(button,out group))return;if(hovered)group.Hovered.Add(button);else group.Hovered.Remove(button);
  int index=group.Buttons.IndexOf(button);Color from=MotionBrushColor(button.Background),to=ChoiceBackground(group,index);var radius=(CornerRadius)button.GetValue(MotionChoiceRadiusProperty);var end=ChoiceRadius(group,index);
  StopControlMotion(button);var brush=new SolidColorBrush(from);button.Background=brush;RunControlMotion(button,130,p=>{brush.Color=MotionColor(from,to,p);if(!controlMotionStops.ContainsKey(group.Panel))button.SetValue(MotionChoiceRadiusProperty,MotionRadius(radius,end,p));},()=>{brush.Color=to;if(!controlMotionStops.ContainsKey(group.Panel))button.SetValue(MotionChoiceRadiusProperty,ChoiceRadius(group,index));});
 }
 void PaintMotionChoices(MotionChoiceGroup group){
  for(int i=0;i<group.Buttons.Count;i++){var b=group.Buttons[i];b.SetValue(MotionChoiceRadiusProperty,ChoiceRadius(group,i));b.Background=new SolidColorBrush(ChoiceBackground(group,i));b.BorderBrush=SoftBorder();b.Foreground=ink;PaintChoiceText(b,b.Foreground);}
 }
 void PaintChoiceText(Button button,Brush brush){if(button.Content is TextBlock)((TextBlock)button.Content).Foreground=brush;foreach(var text in FindVisual<TextBlock>(button))text.Foreground=brush;foreach(var path in FindVisual<System.Windows.Shapes.Path>(button))if(path.Stroke!=null&&path.Stroke!=Brushes.Transparent)path.Stroke=ComponentIconBrush(brush);}
 void RegisterMotionChoices(Panel panel,int selected){
  if(visualMotion==null||!visualMotion.segmentedChoices)return;
  var buttons=panel.Children.OfType<Button>().ToList();if(buttons.Count<2||buttons.Any(b=>motionChoices.ContainsKey(b)))return;
  var group=new MotionChoiceGroup{Panel=panel,Buttons=buttons,Selected=selected,Templates=buttons.Select(b=>b.Template).ToArray(),Margins=buttons.Select(b=>b.Margin).ToArray()};
  foreach(var b in buttons){motionChoices[b]=group;b.Margin=new Thickness(0);var template=new ControlTemplate(typeof(Button));var edge=new FrameworkElementFactory(typeof(Border));edge.SetBinding(Border.CornerRadiusProperty,new Binding{Source=b,Path=new PropertyPath(MotionChoiceRadiusProperty)});edge.SetValue(Border.BackgroundProperty,new TemplateBindingExtension(Control.BackgroundProperty));edge.SetValue(Border.BorderBrushProperty,new TemplateBindingExtension(Control.BorderBrushProperty));edge.SetValue(Border.BorderThicknessProperty,new TemplateBindingExtension(Control.BorderThicknessProperty));var content=new FrameworkElementFactory(typeof(ContentPresenter));content.SetValue(FrameworkElement.MarginProperty,new TemplateBindingExtension(Control.PaddingProperty));content.SetValue(FrameworkElement.HorizontalAlignmentProperty,new TemplateBindingExtension(Control.HorizontalContentAlignmentProperty));content.SetValue(FrameworkElement.VerticalAlignmentProperty,new TemplateBindingExtension(Control.VerticalContentAlignmentProperty));edge.AppendChild(content);template.VisualTree=edge;b.Template=template;}
  RoutedEventHandler loaded=null,unloaded=null;SizeChangedEventHandler resized=null;
  group.Restore=()=>{if(group.Stopped)return;group.Stopped=true;StopControlMotion(panel);for(int i=0;i<buttons.Count;i++){StopControlMotion(buttons[i]);motionChoices.Remove(buttons[i]);buttons[i].Template=group.Templates[i];buttons[i].Margin=group.Margins[i];buttons[i].ClearValue(MotionChoiceRadiusProperty);}panel.Loaded-=loaded;panel.Unloaded-=unloaded;panel.SizeChanged-=resized;visualMotionCleanup.Remove(group.Restore);};
  loaded=delegate{PaintMotionChoices(group);};unloaded=delegate{group.Restore();};resized=delegate{if(!controlMotionStops.ContainsKey(panel))PaintMotionChoices(group);};panel.Loaded+=loaded;panel.Unloaded+=unloaded;panel.SizeChanged+=resized;visualMotionCleanup.Add(group.Restore);PaintMotionChoices(group);
 }
 void RunMotionButtonAction(Button button,Action action){
  MotionChoiceGroup group;if(!motionChoices.TryGetValue(button,out group)||group.Stopped){action();return;}
  int selected=group.Buttons.IndexOf(button);if(selected==group.Selected){action();return;}
  int oldSelected=group.Selected;var radii=group.Buttons.Select(b=>(CornerRadius)b.GetValue(MotionChoiceRadiusProperty)).ToArray();
  bool animate=VisualMotionApi!=null&&cfg.Animations;if(animate)HoldControlRedraw();action();if(group.Stopped)return;StopControlMotion(group.Panel);group.Selected=selected;
  if(!animate||!cfg.Animations){PaintMotionChoices(group);return;}HoldControlRedraw();
  PaintMotionChoices(group);
  // Join the facing edges, release the old tail, then round the new leading edge and tail.
  RunControlMotion(group.Panel,visualMotion.controlDurationMs,p=>{
   for(int i=0;i<group.Buttons.Count;i++)group.Buttons[i].SetValue(MotionChoiceRadiusProperty,ChoiceTravelRadius(group,i,radii[i],oldSelected,selected,p));
  },()=>{PaintMotionChoices(group);},true);
 }
 static double MotionPhase(double p,double start,double end){double t=Math.Max(0,Math.Min(1,(p-start)/(end-start)));return t*t*(3-2*t);}
 CornerRadius ChoiceTravelRadius(MotionChoiceGroup group,int index,CornerRadius from,int oldSelected,int selected,double p){
  var end=ChoiceRadius(group,index);bool right=selected>oldSelected;double left,rightRadius;
  if(index==oldSelected){
   left=from.TopLeft*(1-MotionPhase(p,right?.24:0,right?.50:.20));rightRadius=from.TopRight*(1-MotionPhase(p,right?0:.24,right?.20:.50));
   double finish=MotionPhase(p,.86,1);return MotionRadius(new CornerRadius(left,rightRadius,rightRadius,left),end,finish);
  }
  if(index==selected){
   double radius=Math.Max(1,group.Buttons[index].ActualHeight/2),join=1-MotionPhase(p,0,.20);
   left=from.TopLeft*join+radius*MotionPhase(p,right?.68:.50,right?.88:.70);
   rightRadius=from.TopRight*join+radius*MotionPhase(p,right?.50:.68,right?.70:.88);
   return new CornerRadius(left,rightRadius,rightRadius,left);
  }
  return MotionRadius(from,end,MotionPhase(p,0,.3));
 }
 void ResetControlMotion(){controlRedrawUntil=DateTime.MinValue;if(controlRedrawTimer!=null)controlRedrawTimer.Stop();deferredControlUpdates.Clear();}
 void SetTaskbarPosition(string position){
  if(!new[]{"Top","Bottom","Left","Right"}.Contains(position))throw new ArgumentException("Unknown taskbar edge.");
  cfg.Position=position;if(dock!=null){dock.BeginAnimation(Window.LeftProperty,null);dock.BeginAnimation(Window.TopProperty,null);dock.BeginAnimation(Window.OpacityProperty,null);dock.Opacity=1;StopVisualSurface(dock.Content as FrameworkElement);dockWanted=!hiddenMode;if(!hiddenMode)dock.Show();}
  Changed(false);if(dock!=null)DockLayout();
 }
}}
