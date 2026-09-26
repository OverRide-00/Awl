using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using Forms=System.Windows.Forms;

namespace Awl { partial class Shell {
 List<Window> topDisplayMirrors=new List<Window>(),taskbarDisplayMirrors=new List<Window>(),widgetDisplayMirrors=new List<Window>();string displayMirrorSignature="";
 string MonitorPreference(string surface){return surface=="Taskbar"?cfg.TaskbarMonitor:surface=="Start menu"?cfg.StartMenuMonitor:surface=="Top toolbar"?cfg.TopToolbarMonitor:cfg.WidgetsMonitor;}
 Forms.Screen ScreenByName(string name){return Forms.Screen.AllScreens.FirstOrDefault(x=>String.Equals(x.DeviceName,name,StringComparison.OrdinalIgnoreCase))??Forms.Screen.PrimaryScreen;}
 Forms.Screen PointerScreen(){Native.POINT point;if(Native.GetCursorPos(out point))return Forms.Screen.FromPoint(new System.Drawing.Point(point.X,point.Y));return Forms.Screen.PrimaryScreen;}
 Forms.Screen SurfaceScreen(string surface){
  string selected=MonitorPreference(surface);
  if(cfg.MonitorMode=="Duplicate"||selected=="All")return surface=="Start menu"?PointerScreen():Forms.Screen.PrimaryScreen;
  if(cfg.MonitorMode!="Individual"||String.IsNullOrWhiteSpace(selected)||selected=="Primary")return Forms.Screen.PrimaryScreen;
  return ScreenByName(selected);
 }
 System.Drawing.Rectangle SurfaceBounds(string surface){return SurfaceScreen(surface).Bounds;}
 System.Drawing.Rectangle SurfaceWorkArea(string surface){return SurfaceScreen(surface).WorkingArea;}
 string[] MonitorChoices(bool allowAll){var values=new List<string>{"Primary"};if(allowAll)values.Add("All");values.AddRange(Forms.Screen.AllScreens.Where(x=>!x.Primary).Select(x=>x.DeviceName));return values.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();}
 string MonitorLabel(string value){if(value=="Primary")return "Primary display";if(value=="All")return "All displays";var screen=ScreenByName(value);int index=Array.IndexOf(Forms.Screen.AllScreens,screen)+1;return "Display "+index+"  "+screen.Bounds.Width+" × "+screen.Bounds.Height;}
 string[] MonitorLabels(string[] values){return values.Select(MonitorLabel).ToArray();}
 void NormalizeMonitorPreferences(){
  if(!new[]{"Primary","Duplicate","Individual"}.Contains(cfg.MonitorMode))cfg.MonitorMode="Primary";
  var valid=new HashSet<string>(MonitorChoices(true),StringComparer.OrdinalIgnoreCase);
  if(!valid.Contains(cfg.TaskbarMonitor))cfg.TaskbarMonitor="Primary";
  if(!valid.Contains(cfg.StartMenuMonitor))cfg.StartMenuMonitor="Primary";
  if(!valid.Contains(cfg.TopToolbarMonitor))cfg.TopToolbarMonitor="Primary";
  if(!valid.Contains(cfg.WidgetsMonitor))cfg.WidgetsMonitor="Primary";
 }
 void MonitorSettings(){
  page.Children.Add(Options("Display mode",new[]{"Primary only","Duplicate","Individual"},new[]{"Primary","Duplicate","Individual"},cfg.MonitorMode,value=>{cfg.MonitorMode=value;Changed(false);}));
  if(cfg.MonitorMode!="Individual"){page.Children.Add(Text(cfg.MonitorMode=="Duplicate"?"Awl uses every connected display. Start and search open on the display under the pointer.":"Awl uses the Windows primary display.",11,mutedBrush));return;}
  Action<string,Func<string>,Action<string>,bool> selector=(title,get,set,all)=>{var values=MonitorChoices(all);Select(title,values,get(),value=>{set(value);Changed(false);});};
  selector("Taskbar",()=>cfg.TaskbarMonitor,value=>cfg.TaskbarMonitor=value,true);
  selector("Start menu",()=>cfg.StartMenuMonitor,value=>cfg.StartMenuMonitor=value,false);
  selector("Top toolbar",()=>cfg.TopToolbarMonitor,value=>cfg.TopToolbarMonitor=value,true);
  selector("Widgets",()=>cfg.WidgetsMonitor,value=>cfg.WidgetsMonitor=value,true);
 }
 bool SurfaceUsesAllDisplays(string surface){return cfg.MonitorMode=="Duplicate"||cfg.MonitorMode=="Individual"&&MonitorPreference(surface)=="All";}
 Window CreateDisplayMirror(Visual source,string title){
  var mirror=new Window{Title=title,WindowStyle=WindowStyle.None,ResizeMode=ResizeMode.NoResize,AllowsTransparency=true,Background=Brushes.Transparent,ShowInTaskbar=false,ShowActivated=false,Topmost=true,Content=new Border{Background=new VisualBrush(source){Stretch=Stretch.Fill}}};
  mirror.SourceInitialized+=delegate{var handle=new WindowInteropHelper(mirror).Handle;Native.SetWindowLong(handle,-20,Native.GetWindowLong(handle,-20)|0x80|0x08000000|0x20);};return mirror;
 }
 void CloseDisplayMirrors(List<Window> windows){foreach(var window in windows.ToArray())try{window.Close();}catch{}windows.Clear();}
 void RefreshDisplayMirrors(){displayMirrorSignature="";CloseDisplayMirrors(topDisplayMirrors);CloseDisplayMirrors(taskbarDisplayMirrors);UpdateDisplayMirrors();}
 void LayoutTaskbarMirror(Window mirror,System.Drawing.Rectangle bounds){bool vertical=cfg.Position=="Left"||cfg.Position=="Right";mirror.Width=dock.Width;mirror.Height=dock.Height;double gap=cfg.Style=="Hug"?0:cfg.Gap;mirror.Left=cfg.Position=="Left"?bounds.Left+gap:cfg.Position=="Right"?bounds.Right-mirror.Width-gap:bounds.Left+(bounds.Width-mirror.Width)/2;if(!vertical&&cfg.DockAlignment!="Center")mirror.Left=cfg.DockAlignment=="Left"?bounds.Left+gap:bounds.Right-mirror.Width-gap;mirror.Top=cfg.Position=="Top"?bounds.Top+gap:cfg.Position=="Bottom"?bounds.Bottom-mirror.Height-gap:bounds.Top+(bounds.Height-mirror.Height)/2;}
 void UpdateDisplayMirrors(){
  string signature=cfg.MonitorMode+"|"+cfg.TaskbarMonitor+"|"+cfg.TopToolbarMonitor+"|"+String.Join(";",Forms.Screen.AllScreens.Select(x=>x.DeviceName+":"+x.Bounds));
  if(signature!=displayMirrorSignature){CloseDisplayMirrors(topDisplayMirrors);CloseDisplayMirrors(taskbarDisplayMirrors);displayMirrorSignature=signature;}
  var topSource=top==null?null:top.Content as Visual;var dockSource=dock==null?null:dock.Content as Visual;
  var extraTop=SurfaceUsesAllDisplays("Top toolbar")?Forms.Screen.AllScreens.Where(x=>x!=SurfaceScreen("Top toolbar")).ToArray():new Forms.Screen[0];
  while(topDisplayMirrors.Count<extraTop.Length&&topSource!=null){var mirror=CreateDisplayMirror(topSource,"Awl Status Bar mirror");topDisplayMirrors.Add(mirror);mirror.Show();}
  while(topDisplayMirrors.Count>extraTop.Length){topDisplayMirrors.Last().Close();topDisplayMirrors.RemoveAt(topDisplayMirrors.Count-1);}
  for(int i=0;i<topDisplayMirrors.Count;i++){var bounds=extraTop[i].Bounds;var mirror=topDisplayMirrors[i];mirror.Left=bounds.Left;mirror.Top=bounds.Top;mirror.Width=bounds.Width;mirror.Height=top.Height;mirror.Visibility=top.Visibility;}
  var extraTaskbars=SurfaceUsesAllDisplays("Taskbar")?Forms.Screen.AllScreens.Where(x=>x!=SurfaceScreen("Taskbar")).ToArray():new Forms.Screen[0];
  while(taskbarDisplayMirrors.Count<extraTaskbars.Length&&dockSource!=null){var mirror=CreateDisplayMirror(dockSource,"Awl Taskbar mirror");taskbarDisplayMirrors.Add(mirror);mirror.Show();}
  while(taskbarDisplayMirrors.Count>extraTaskbars.Length){taskbarDisplayMirrors.Last().Close();taskbarDisplayMirrors.RemoveAt(taskbarDisplayMirrors.Count-1);}
  for(int i=0;i<taskbarDisplayMirrors.Count;i++){LayoutTaskbarMirror(taskbarDisplayMirrors[i],extraTaskbars[i].Bounds);taskbarDisplayMirrors[i].Visibility=dock.Visibility;}
 }
 void UpdateWidgetMirrors(){
  bool duplicate=SurfaceUsesAllDisplays("Widgets")&&clockWidget!=null;var extra=duplicate?Forms.Screen.AllScreens.Where(x=>x!=SurfaceScreen("Widgets")).ToArray():new Forms.Screen[0];var source=clockWidget==null?null:clockWidget.Content as Visual;
  while(widgetDisplayMirrors.Count<extra.Length&&source!=null){var mirror=CreateDisplayMirror(source,"Awl Date & Time mirror");mirror.Topmost=false;widgetDisplayMirrors.Add(mirror);mirror.Show();}
  while(widgetDisplayMirrors.Count>extra.Length){widgetDisplayMirrors.Last().Close();widgetDisplayMirrors.RemoveAt(widgetDisplayMirrors.Count-1);}
  var primary=SurfaceBounds("Widgets");double relativeX=clockWidget==null?0:clockWidget.Left-primary.Left,relativeY=clockWidget==null?0:clockWidget.Top-primary.Top;
  for(int i=0;i<widgetDisplayMirrors.Count;i++){var bounds=extra[i].Bounds;var mirror=widgetDisplayMirrors[i];mirror.Width=clockWidget.Width;mirror.Height=clockWidget.Height;mirror.Left=bounds.Left+relativeX*Math.Max(0,bounds.Width-mirror.Width)/Math.Max(1,primary.Width-clockWidget.Width);mirror.Top=bounds.Top+relativeY*Math.Max(0,bounds.Height-mirror.Height)/Math.Max(1,primary.Height-clockWidget.Height);mirror.Visibility=clockWidget.Visibility;}
 }
} }
