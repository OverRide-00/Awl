using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Windows.Media.Control;
using Windows.Networking.Connectivity;
using Windows.Devices.Radios;
using Forms = System.Windows.Forms;

namespace Awl {
static class Native {
 [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left,Top,Right,Bottom; }
 [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X,Y; }
 [StructLayout(LayoutKind.Sequential)] public struct APPBAR { public int Size; public IntPtr Hwnd; public uint Callback,Edge; public RECT Rect; public IntPtr Param; }
 [DllImport("shell32.dll")] public static extern UIntPtr SHAppBarMessage(uint msg, ref APPBAR data);
 [DllImport("user32.dll")] public static extern IntPtr FindWindow(string cls,string title);
 [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc proc,IntPtr arg);
 public delegate bool EnumProc(IntPtr hwnd,IntPtr arg);
 [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr hwnd);
 [DllImport("user32.dll")] public static extern int GetWindowText(IntPtr hwnd,StringBuilder text,int length);
 [DllImport("user32.dll")] public static extern int GetWindowLong(IntPtr hwnd,int index);
 [DllImport("user32.dll")] public static extern int SetWindowLong(IntPtr hwnd,int index,int value);
 [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hwnd,out uint pid);
 [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hwnd);
 [DllImport("user32.dll")] public static extern bool ShowWindowAsync(IntPtr hwnd,int command);
 [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hwnd,int command);
 [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr hwnd,uint message,IntPtr wparam,IntPtr lparam);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)] public static extern int GetClassName(IntPtr hwnd,StringBuilder text,int length);
 [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)] public struct SHFILEINFO {public IntPtr Icon;public int Index;public uint Attributes;[MarshalAs(UnmanagedType.ByValTStr,SizeConst=260)]public string Name;[MarshalAs(UnmanagedType.ByValTStr,SizeConst=80)]public string Type;}
 [DllImport("shell32.dll",CharSet=CharSet.Unicode)] public static extern IntPtr SHGetFileInfo(string path,uint attributes,out SHFILEINFO info,uint size,uint flags);
 [DllImport("user32.dll")] public static extern bool DestroyIcon(IntPtr icon);
 [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr hwnd);
 [DllImport("user32.dll")] public static extern bool IsZoomed(IntPtr hwnd);
 [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
 [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hwnd,out RECT rect);
 [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr hwnd,IntPtr hdc,uint flags);
 [DllImport("user32.dll")] public static extern bool GetCursorPos(out POINT p);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)] public static extern uint RegisterWindowMessage(string message);
 [DllImport("user32.dll")] public static extern void keybd_event(byte key,byte scan,uint flags,UIntPtr extra);
 [DllImport("dwmapi.dll")] public static extern int DwmGetWindowAttribute(IntPtr hwnd,int attr,out int value,int size);
 [DllImport("dwmapi.dll")] public static extern int DwmSetWindowAttribute(IntPtr hwnd,int attr,ref int value,int size);
 [StructLayout(LayoutKind.Sequential)] public struct MARGINS {public int Left,Right,Top,Bottom;}
 [DllImport("dwmapi.dll")] public static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd,ref MARGINS margins);
 [DllImport("user32.dll")] public static extern bool SystemParametersInfo(uint action,uint parameter,IntPtr value,uint flags);
 [DllImport("bthprops.cpl")] public static extern IntPtr BluetoothFindFirstRadio(ref int size,out IntPtr radio);
 [DllImport("bthprops.cpl")] public static extern bool BluetoothFindRadioClose(IntPtr handle);
 [DllImport("kernel32.dll")] public static extern bool CloseHandle(IntPtr handle);
 public static uint TaskbarState() { var a=new APPBAR(); a.Size=Marshal.SizeOf(a); return (uint)SHAppBarMessage(4,ref a).ToUInt64(); }
 public static void TaskbarState(uint state) { var a=new APPBAR(); a.Size=Marshal.SizeOf(a); a.Hwnd=FindWindow("Shell_TrayWnd",null); a.Param=(IntPtr)state; SHAppBarMessage(10,ref a); }
 public static void Keys(byte key) { keybd_event(0x5B,0,0,UIntPtr.Zero); keybd_event(key,0,0,UIntPtr.Zero); keybd_event(key,0,2,UIntPtr.Zero); keybd_event(0x5B,0,2,UIntPtr.Zero); }
 public static void StartMenu() { keybd_event(0x5B,0,0,UIntPtr.Zero); keybd_event(0x5B,0,2,UIntPtr.Zero); }
}
[ComImport,Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")] class MMEnumerator {}
[ComImport,Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] interface IMMEnumerator {
 [PreserveSig] int EnumAudioEndpoints(int flow,int state,out IntPtr devices);
 [PreserveSig] int GetDefaultAudioEndpoint(int flow,int role,out IMMDevice device);
}
[ComImport,Guid("D666063F-1587-4E43-81F1-B948E807363F"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] interface IMMDevice {
 [PreserveSig] int Activate(ref Guid iid,int context,IntPtr activation,[MarshalAs(UnmanagedType.IUnknown)] out object result);
}
[ComImport,Guid("5CDF2C82-841E-4546-9722-0CF74078229A"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] interface IEndpointVolume {
 [PreserveSig] int RegisterControlChangeNotify(IntPtr p); [PreserveSig] int UnregisterControlChangeNotify(IntPtr p);
 [PreserveSig] int GetChannelCount(out uint count); [PreserveSig] int SetMasterVolumeLevel(float v,ref Guid c); [PreserveSig] int SetMasterVolumeLevelScalar(float v,ref Guid c);
 [PreserveSig] int GetMasterVolumeLevel(out float v); [PreserveSig] int GetMasterVolumeLevelScalar(out float v);
 [PreserveSig] int SetChannelVolumeLevel(uint c,float v,ref Guid e); [PreserveSig] int SetChannelVolumeLevelScalar(uint c,float v,ref Guid e);
 [PreserveSig] int GetChannelVolumeLevel(uint c,out float v); [PreserveSig] int GetChannelVolumeLevelScalar(uint c,out float v);
 [PreserveSig] int SetMute([MarshalAs(UnmanagedType.Bool)] bool m,ref Guid c); [PreserveSig] int GetMute([MarshalAs(UnmanagedType.Bool)] out bool m);
}
class Audio {
 public static bool Write(int flow,float? level,bool? muted){object e=null,v=null;IMMDevice d=null;try{e=new MMEnumerator();if(((IMMEnumerator)e).GetDefaultAudioEndpoint(flow,flow==1?2:0,out d)!=0)return false;var id=typeof(IEndpointVolume).GUID;if(d.Activate(ref id,23,IntPtr.Zero,out v)!=0)return false;var ep=(IEndpointVolume)v;var context=Guid.Empty;if(level.HasValue&&ep.SetMasterVolumeLevelScalar(Math.Max(0,Math.Min(1,level.Value)),ref context)!=0)return false;if(muted.HasValue&&ep.SetMute(muted.Value,ref context)!=0)return false;return true;}catch{return false;}finally{if(v!=null)Marshal.ReleaseComObject(v);if(d!=null)Marshal.ReleaseComObject(d);if(e!=null)Marshal.ReleaseComObject(e);}}
 public static bool Read(int flow,out float level,out bool muted) {
  level=0; muted=false; object e=null,v=null; IMMDevice d=null;
  try { e=new MMEnumerator(); if(((IMMEnumerator)e).GetDefaultAudioEndpoint(flow,flow==1?2:0,out d)!=0)return false;
   var id=typeof(IEndpointVolume).GUID; if(d.Activate(ref id,23,IntPtr.Zero,out v)!=0)return false;
   var ep=(IEndpointVolume)v; return ep.GetMasterVolumeLevelScalar(out level)==0 && ep.GetMute(out muted)==0;
  } catch{return false;} finally { if(v!=null)Marshal.ReleaseComObject(v); if(d!=null)Marshal.ReleaseComObject(d); if(e!=null)Marshal.ReleaseComObject(e); }
 }
}
partial class Shell : Application {
 static string InstallRoot=AppDomain.CurrentDomain.BaseDirectory;
 static string Root=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Awl");
 static string State=Path.Combine(Root,"original-taskbar-state.txt");
 static string LegacyRoot=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"TealShell");
 static EventWaitHandle stop,widgetReload,showLauncher; static bool diagnostics; static bool openLauncher,openConfigPreview;
 static Mutex mutex;
 Window dock,top; StackPanel apps; Popup pins; DispatcherTimer hover,poll; Forms.NotifyIcon tray;
 TextBlock battery,volume,mic,brightness,wifi,bluetooth,clock,media; ProgressBar progress; Border batteryFill;
 DateTime topEdgeSince=DateTime.MinValue,lastTopHover=DateTime.MinValue;bool topCovered;static int selfPid=Process.GetCurrentProcess().Id;
 GlobalSystemMediaTransportControlsSessionManager manager;
 bool busy=false,closing=false; string signature=""; DateTime lastHover=DateTime.Now;
 string weather="Lagos —"; DateTime nextWeather=DateTime.MinValue,weatherTime=DateTime.MinValue;
 async Task Weather() {string location=cfg.WeatherLocation;if(String.IsNullOrWhiteSpace(location)){weather="Set location";weatherTime=DateTime.Now;nextWeather=DateTime.Now.AddMinutes(15);return;}if(DateTime.Now<nextWeather)return;nextWeather=DateTime.Now.AddMinutes(15);double lat=cfg.WeatherLatitude,lon=cfg.WeatherLongitude;try{using(var client=new System.Net.Http.HttpClient()){client.Timeout=TimeSpan.FromSeconds(8);var json=await client.GetStringAsync("https://api.open-meteo.com/v1/forecast?latitude="+lat.ToString(System.Globalization.CultureInfo.InvariantCulture)+"&longitude="+lon.ToString(System.Globalization.CultureInfo.InvariantCulture)+"&current=temperature_2m&timezone=auto");if(location!=cfg.WeatherLocation||lat!=cfg.WeatherLatitude||lon!=cfg.WeatherLongitude)return;var data=new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<Dictionary<string,object>>(json);var current=(Dictionary<string,object>)data["current"];weather=location.Split(',')[0]+" "+Convert.ToDouble(current["temperature_2m"]).ToString("0")+"°C";weatherTime=DateTime.Now;}}catch{if(location==cfg.WeatherLocation&&lat==cfg.WeatherLatitude&&lon==cfg.WeatherLongitude){weather=location.Split(',')[0]+" —";nextWeather=DateTime.Now.AddMinutes(5);}}}
 Brush ink=new SolidColorBrush(Color.FromRgb(232,255,226)); Brush teal=new SolidColorBrush(Color.FromRgb(8,42,37));
 Dictionary<string,ImageSource> icons=new Dictionary<string,ImageSource>();
 public static void InitializeDataRoot(){if(!devMode&&!Directory.Exists(Root)&&Directory.Exists(LegacyRoot)){try{Directory.CreateDirectory(Root);foreach(string name in new[]{"config.json","desktop-widgets.json","widget-builder.json"}){string source=Path.Combine(LegacyRoot,name);if(File.Exists(source))File.Copy(source,Path.Combine(Root,name),true);}string legacyTemplates=Path.Combine(LegacyRoot,"Templates");if(Directory.Exists(legacyTemplates))foreach(string source in Directory.GetFiles(legacyTemplates,"*",SearchOption.AllDirectories)){string relative=source.Substring(legacyTemplates.Length).TrimStart(Path.DirectorySeparatorChar);string destination=Path.Combine(Root,"Templates",relative);Directory.CreateDirectory(Path.GetDirectoryName(destination));File.Copy(source,destination,true);}using(var run=Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run",true))if(run!=null)run.DeleteValue("TealShell",false);string oldShortcut=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup),"TealShell.lnk");if(File.Exists(oldShortcut))File.Delete(oldShortcut);}catch{}}try{Directory.CreateDirectory(Root);string probe=Path.Combine(Root,".write-test");using(File.Open(probe,FileMode.OpenOrCreate,FileAccess.Write,FileShare.None)){}File.Delete(probe);}catch{Root=Path.Combine(Path.GetTempPath(),"Awl");State=Path.Combine(Root,"original-taskbar-state.txt");Directory.CreateDirectory(Root);}var assembly=typeof(Shell).Assembly;foreach(string name in new[]{"config.json","desktop-widgets.json","widget-builder.json"}){string destination=Path.Combine(Root,name);if(File.Exists(destination))continue;string beside=Path.Combine(InstallRoot,name);if(File.Exists(beside)){File.Copy(beside,destination);continue;}using(var input=assembly.GetManifestResourceStream("Awl.Defaults."+name))if(input!=null)using(var output=File.Create(destination))input.CopyTo(output);}}
 [STAThread] static void Main(string[] args) {
  try{InitializeDataRoot();}catch(Exception startupError){try{File.WriteAllText(Path.Combine(Path.GetTempPath(),"Awl-startup-error.txt"),startupError.ToString());}catch{}MessageBox.Show(startupError.Message,"Awl could not start");return;}
  if(args.Contains("--sanitize-inbuilt-sources")){var shell=new Shell();File.WriteAllText(Path.Combine(InstallRoot,"template-sanitize.txt"),shell.SanitizeInbuiltTemplateSources());return;}
  if(args.Contains("--reliability-self-test")){var shell=new Shell();File.WriteAllText(Path.Combine(Root,"reliability-checks.txt"),shell.AppSearchSelfTest()+Environment.NewLine+shell.SystemSortSelfTest()+Environment.NewLine+shell.SwitchSelfTest()+Environment.NewLine+shell.GuiScaleSelfTest());return;}
  if(args.Contains("--capture-self-test")){var shell=new Shell();File.WriteAllText(Path.Combine(Root,"capture-checks.txt"),shell.CaptureSelfTest());return;}
  if(args.Contains("--rolling-capture-self-test")){var shell=new Shell();File.WriteAllText(Path.Combine(Root,"rolling-capture-checks.txt"),shell.RollingCaptureSelfTest());return;}
  if(args.Contains("--wifi-self-test")){var shell=new Shell();File.WriteAllText(Path.Combine(Root,"wifi-checks.txt"),shell.WifiSelfTest());return;}
  if(args.Contains("--widget-builder")){bool builderFresh;using(var builderGate=new Mutex(true,"Local\\AwlWidgetBuilderInstance",out builderFresh)){if(!builderFresh)return;var builderShell=new Shell();builderShell.ShutdownMode=ShutdownMode.OnLastWindowClose;builderShell.Startup+=delegate{builderShell.StartStandaloneWidgetBuilder();};builderShell.Run();}return;}
  if(args.Contains("--switch-self-test")){var shell=new Shell();File.WriteAllText(Path.Combine(Root,"switch-checks.txt"),shell.SwitchSelfTest());return;}
  if(args.Contains("--template-self-test")){var shell=new Shell();File.WriteAllText(Path.Combine(Root,"template-checks.txt"),shell.TemplateSelfTest());return;}
  if(args.Contains("--probe")){File.WriteAllText(Path.Combine(Root,"probe.json"),new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(new {NativeTaskbarVisible=Native.IsWindowVisible(Native.FindWindow("Shell_TrayWnd",null)),TaskbarState=Native.TaskbarState()}));return;}
  if(args.Length==2&&args[0]=="--guard") {try{using(var parent=Process.GetProcessById(int.Parse(args[1])))parent.WaitForExit();}catch{}using(var gate=new Mutex(false,"Local\\AwlInstance")){bool acquired=false;try{acquired=gate.WaitOne(0);}catch(AbandonedMutexException){acquired=true;}if(acquired){Restore();gate.ReleaseMutex();}}return;}
  if(args.Contains("--restore")) { try {EventWaitHandle.OpenExisting("Local\\AwlStop").Set();}catch{} Restore(); return; }
  System.Net.ServicePointManager.SecurityProtocol=System.Net.SecurityProtocolType.Tls12;
  openConfigPreview=args.Contains("--config-preview");openLauncher=args.Contains("--launcher")||openConfigPreview||args.Length==0;diagnostics=args.Contains("--diagnostics");bool fresh; mutex=new Mutex(true,"Local\\AwlInstance",out fresh);if(!fresh){if(!args.Contains("--background"))try{EventWaitHandle.OpenExisting("Local\\AwlShowLauncher").Set();}catch{}return;}
  stop=new EventWaitHandle(false,EventResetMode.AutoReset,"Local\\AwlStop");widgetReload=new EventWaitHandle(false,EventResetMode.AutoReset,"Local\\AwlWidgetReload");showLauncher=new EventWaitHandle(false,EventResetMode.AutoReset,"Local\\AwlShowLauncher");
  try { var s=new Shell(); s.ShutdownMode=ShutdownMode.OnExplicitShutdown;s.DispatcherUnhandledException+=delegate(object sender,DispatcherUnhandledExceptionEventArgs error){Log("UI: "+error.Exception);error.Handled=true;}; s.Startup+=delegate{s.Init();}; s.Exit+=delegate{Restore();}; s.Run(); }
  catch(Exception ex) {File.AppendAllText(Path.Combine(Root,"error.log"),DateTime.Now+" "+ex+Environment.NewLine); Restore();}
  finally {mutex.ReleaseMutex();}
 }
 static void Restore() {Native.ShowWindow(Native.FindWindow("Shell_TrayWnd",null),8);uint n; if(File.Exists(State)&&uint.TryParse(File.ReadAllText(State),out n))Native.TaskbarState(n);}
 static void HideNativeTaskbar(){var h=Native.FindWindow("Shell_TrayWnd",null);if(h!=IntPtr.Zero&&Native.IsWindowVisible(h))Native.ShowWindow(h,0);}
 Popup systemPopup;
 void SystemPanel(string kind){if(systemPopup!=null&&systemPopup.IsOpen){systemPopup.IsOpen=false;if((string)systemPopup.Tag==kind)return;}FrameworkElement content;if(kind=="Notifications"){var notices=new StackPanel{Width=440};NotificationPanel(notices);content=notices;}else content=StandardToolbarPanel(kind);var popupSurface=PopupSurface(content);string popupScaleName=kind=="Notifications"?"Notification panel":kind=="Wi-Fi"?"Wi-Fi panel":kind=="Battery"?"Battery popup":kind=="Volume"?"Volume popup":kind=="Microphone"?"Microphone popup":kind=="Brightness"?"Brightness popup":kind=="Bluetooth"?"Bluetooth popup":"Top toolbar";ApplySurfaceScale(popupSurface,popupScaleName);systemPopup=new Popup{Tag=kind,AllowsTransparency=true,StaysOpen=true,PlacementTarget=PopupAnchor(kind),Placement=PlacementMode.Bottom,HorizontalOffset=0,VerticalOffset=8,Child=popupSurface};systemPopup.Closed+=delegate{UnwatchPopup();};systemPopup.IsOpen=true;WatchPopup();}
 void AppMenu(Button button,IntPtr hwnd){var menu=new ContextMenu();ThemeContextMenu(menu);var close=new MenuItem{Header="Close"};close.Click+=delegate{Native.PostMessage(hwnd,0x0010,IntPtr.Zero,IntPtr.Zero);};menu.Items.Add(close);menu.Opened+=delegate{appMenuOpen=true;};menu.Closed+=delegate{appMenuOpen=false;lastHover=DateTime.Now;};button.ContextMenu=menu;}
 bool appMenuOpen;
 Window BaseWindow(double height) {
  var w=new Window {Height=height,WindowStyle=WindowStyle.None,AllowsTransparency=true,Background=Brushes.Transparent,ResizeMode=ResizeMode.NoResize,ShowInTaskbar=false,Topmost=true,ShowActivated=false};
  w.SourceInitialized+=delegate {var h=new WindowInteropHelper(w).Handle; Native.SetWindowLong(h,-20,Native.GetWindowLong(h,-20)|0x08000000|0x80);};
  return w;
 }
 Button Button(string text,string tip,Action action,bool symbol=false) {
  var b=new Button {Content=text,ToolTip=tip,Foreground=ink,Background=new SolidColorBrush(Color.FromArgb(8,16,32,28)),MinWidth=36,MinHeight=28,BorderThickness=new Thickness(0),Padding=new Thickness(9,4,9,4),FontSize=symbol?20:12,FontFamily=new FontFamily(symbol?"Segoe Fluent Icons":"Segoe UI"),Cursor=AppCursor(true),Focusable=false};
  var style=new Style(typeof(Button)); var template=new ControlTemplate(typeof(Button)); var edge=new FrameworkElementFactory(typeof(Border)); edge.SetValue(Border.CornerRadiusProperty,new CornerRadius(18)); edge.SetValue(Border.BackgroundProperty,new TemplateBindingExtension(Control.BackgroundProperty)); var cp=new FrameworkElementFactory(typeof(ContentPresenter)); cp.SetValue(FrameworkElement.MarginProperty,new TemplateBindingExtension(Control.PaddingProperty)); cp.SetValue(FrameworkElement.HorizontalAlignmentProperty,new TemplateBindingExtension(Control.HorizontalContentAlignmentProperty)); cp.SetValue(FrameworkElement.VerticalAlignmentProperty,new TemplateBindingExtension(Control.VerticalContentAlignmentProperty)); edge.AppendChild(cp); template.VisualTree=edge; style.Setters.Add(new Setter(Control.TemplateProperty,template)); var t=new Trigger {Property=UIElement.IsMouseOverProperty,Value=true}; t.Setters.Add(new Setter(Control.BackgroundProperty,new SolidColorBrush(Color.FromArgb(90,68,134,112)))); style.Triggers.Add(t); b.Style=style;b.SizeChanged+=delegate{if((string)b.Tag=="Pill"&&VisualTreeHelper.GetChildrenCount(b)>0){var border=VisualTreeHelper.GetChild(b,0) as Border;if(border!=null)border.CornerRadius=new CornerRadius(b.ActualHeight/2);}};if(symbol)HoverAnimation(b);
  b.Click+=delegate {try{action();}catch(Exception ex){Log(ex.Message);}}; return b;
 }
 void ReadableTopText(DependencyObject node){var text=node as TextBlock;if(text!=null){text.Foreground=Brushes.White;if(text.Effect==null)text.Effect=new System.Windows.Media.Effects.DropShadowEffect{Color=Colors.Black,BlurRadius=3,ShadowDepth=0,Opacity=1};}for(int i=0;i<VisualTreeHelper.GetChildrenCount(node);i++)ReadableTopText(VisualTreeHelper.GetChild(node,i));}
 TextBlock Status(StackPanel parent,string tip,Action action) {var text=new TextBlock {Foreground=ink,FontSize=12,VerticalAlignment=VerticalAlignment.Center}; var b=Button("",tip,action); b.Content=text; parent.Children.Add(b); return text;}
 void Glyph(TextBlock text,string glyph,string label){text.Inlines.Clear();text.Inlines.Add(new System.Windows.Documents.Run(glyph){FontFamily=new FontFamily("Segoe Fluent Icons"),FontSize=15});text.Inlines.Add(new System.Windows.Documents.Run(" "+label));}
 void Launch(string uri) {Process.Start(new ProcessStartInfo(uri){UseShellExecute=true});}
 void Init() {
  InitCustomization();
  ApplySnapLayouts();
  if(!File.Exists(State))File.WriteAllText(State,Native.TaskbarState().ToString());
  dock=BaseWindow(62); dock.Title="Awl Dock"; dock.Width=320;
  var surface=new Border {CornerRadius=new CornerRadius(18),Background=new SolidColorBrush(Color.FromArgb(217,6,35,31)),BorderBrush=new SolidColorBrush(Color.FromArgb(110,155,213,176)),BorderThickness=new Thickness(1),Padding=new Thickness(8),Margin=new Thickness(0,0,0,2)};
  apps=new StackPanel {Orientation=Orientation.Horizontal}; surface.Child=apps; dock.Content=surface;
  pins=new Popup {Placement=PlacementMode.Top,PlacementTarget=dock,AllowsTransparency=true,StaysOpen=true,VerticalOffset=-8};
  top=BaseWindow(34); top.Title="Awl Status Bar"; var grid=new Grid(); grid.ColumnDefinitions.Add(new ColumnDefinition()); grid.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto}); grid.ColumnDefinitions.Add(new ColumnDefinition()); top.Content=grid;
  var left=new StackPanel {Orientation=Orientation.Horizontal,Margin=new Thickness(12,0,0,0)}; grid.Children.Add(left);
  battery=Status(left,"Battery",()=>SystemPanel("Battery"));
  var batteryButton=(Button)left.Children[0];batteryButton.Content=null;battery.FontSize=10;battery.HorizontalAlignment=HorizontalAlignment.Center;
  var batteryInterior=new Grid();batteryFill=new Border{Background=new SolidColorBrush(Color.FromArgb(155,86,172,121)),CornerRadius=new CornerRadius(2),HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(1),Width=0};batteryInterior.Children.Add(batteryFill);batteryInterior.Children.Add(battery);
  var batteryShape=new StackPanel{Orientation=Orientation.Horizontal};batteryShape.Children.Add(new Border{BorderBrush=ink,BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(3),Width=37,Height=18,Child=batteryInterior});batteryShape.Children.Add(new Border{Width=2,Height=7,Background=ink,VerticalAlignment=VerticalAlignment.Center});batteryButton.Content=batteryShape;
  volume=Status(left,"Volume",()=>SystemPanel("Volume"));
  mic=Status(left,"Default microphone endpoint mute status; apps may mute independently",()=>SystemPanel("Microphone"));
  brightness=Status(left,"Display brightness",()=>SystemPanel("Brightness"));
  wifi=Status(left,"Wi-Fi",()=>SystemPanel("Wi-Fi"));
  bluetooth=Status(left,"Bluetooth radio status",()=>SystemPanel("Bluetooth"));
  var bell=Button("\uEA8F","Notifications",()=>SystemPanel("Notifications"),true); left.Children.Add(bell);notificationButton=bell;
  clock=new TextBlock {Foreground=ink,FontSize=13,VerticalAlignment=VerticalAlignment.Center,HorizontalAlignment=HorizontalAlignment.Center}; Grid.SetColumn(clock,1); grid.Children.Add(clock);
  var right=new StackPanel {Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(8,0,18,0)}; Grid.SetColumn(right,2); grid.Children.Add(right);
  media=new TextBlock {Foreground=ink,FontSize=12,MaxWidth=210,TextTrimming=TextTrimming.CharacterEllipsis,VerticalAlignment=VerticalAlignment.Center}; right.Children.Add(media);
  progress=new ProgressBar {Width=85,Height=3,Minimum=0,Maximum=100,Foreground=new SolidColorBrush(Color.FromRgb(176,228,170)),Background=new SolidColorBrush(Color.FromArgb(65,176,228,170)),Margin=new Thickness(10,0,0,0),Visibility=Visibility.Collapsed}; right.Children.Add(progress);
  CaptureModules();StartSystemMonitor();StartKeyboardHook();StartWidgets();surface.ContextMenu=null;

  tray=new Forms.NotifyIcon {Icon=System.Drawing.SystemIcons.Application,Text=devMode?"Awl Dev Mode — temporary session":"Awl — right-click to restore",Visible=true}; tray.ContextMenuStrip=new Forms.ContextMenuStrip(); tray.ContextMenuStrip.Items.Add("Show dock",null,delegate{lastHover=DateTime.Now;AnimateDock(true);}); tray.ContextMenuStrip.Items.Add("Exit and restore Windows taskbar",null,delegate{Quit();});
  dock.Show();top.Show(); Layout(); RefreshApps(); Native.TaskbarState(Native.TaskbarState()|1);
  if(!devMode)Process.Start(new ProcessStartInfo(typeof(Shell).Assembly.Location,"--guard "+selfPid){UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden});HideNativeTaskbar();
  hover=new DispatcherTimer {Interval=TimeSpan.FromMilliseconds(200)}; hover.Tick+=delegate{Hover();}; hover.Start();
  poll=new DispatcherTimer {Interval=TimeSpan.FromSeconds(3)}; poll.Tick+=async delegate{ConfigTick();RefreshApps();ApplyWindowCustomizations();await StatusUpdate();}; poll.Start();
  ApplyTheme();signature="";RefreshApps();InitMedia();Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,new Action(StartRollingCapture));if(!devMode)Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,new Action(()=>CheckForUpdates(false)));if(openLauncher){if(openConfigPreview)activeTab="Config";ToggleLauncher();Render(launcher,"launcher-preview.png");}
 }
 async void InitMedia() {try{manager=await System.WindowsRuntimeSystemExtensions.AsTask<GlobalSystemMediaTransportControlsSessionManager>(GlobalSystemMediaTransportControlsSessionManager.RequestAsync());}catch(Exception e){Log("Media: "+e.Message);} await StatusUpdate();}
 void Layout() {top.Left=0;top.Top=0;top.Width=SystemParameters.PrimaryScreenWidth;DockLayout();}
 void Hover() {
  if(stop.WaitOne(0)){Quit();return;}if(widgetReload!=null&&widgetReload.WaitOne(0))ReloadBuilderDesktop();if(showLauncher!=null&&showLauncher.WaitOne(0)){if(launcher==null||!launcher.IsVisible)ToggleLauncher();else launcher.Activate();}Layout();HideNativeTaskbar();RedirectNativeStart();if(top!=null)ReadableTopText(top);
  if(altTabShowing&&(GetAsyncKeyState(0x12)&0x8000)==0&&(GetAsyncKeyState(0xA4)&0x8000)==0&&(GetAsyncKeyState(0xA5)&0x8000)==0)FinishAltTab(true);
  Native.POINT p; Native.GetCursorPos(out p); var source=PresentationSource.FromVisual(top); if(source==null)return; var point=source.CompositionTarget.TransformFromDevice.Transform(new Point(p.X,p.Y));
  var screen=Forms.Screen.PrimaryScreen.Bounds;int stripHeight=(int)Math.Ceiling(top.Height*source.CompositionTarget.TransformToDevice.M22);topCovered=false;bool fullscreenCover=false;
  Native.EnumWindows(delegate(IntPtr h,IntPtr unused){
   if(!Native.IsWindowVisible(h)||Native.IsIconic(h)||(Native.GetWindowLong(h,-20)&0x80)!=0)return true;
   uint pid;Native.GetWindowThreadProcessId(h,out pid);if(pid==selfPid)return true;int cloaked;Native.DwmGetWindowAttribute(h,14,out cloaked,4);if(cloaked!=0)return true;
   var cls=new StringBuilder(128);Native.GetClassName(h,cls,128);if(cls.ToString()=="Progman"||cls.ToString()=="WorkerW"||cls.ToString().Contains("TrayWnd"))return true;
   Native.RECT r;if(Native.GetWindowRect(h,out r)&&r.Right>screen.Left&&r.Left<screen.Right&&r.Bottom>screen.Top&&r.Top<screen.Top+stripHeight){topCovered=true;int style=Native.GetWindowLong(h,-16);bool fills=r.Left<=screen.Left&&r.Top<=screen.Top&&r.Right>=screen.Right&&r.Bottom>=screen.Bottom;fullscreenCover=fills&&(style&0x00C00000)==0;return false;}return true;
  },IntPtr.Zero);
  var now=DateTime.Now;bool topEdge=point.X>=0&&point.X<top.Width&&point.Y>=0&&point.Y<=2;
  if(topEdge){if(topEdgeSince==DateTime.MinValue)topEdgeSince=now;}else topEdgeSince=DateTime.MinValue;
  bool reveal=topEdgeSince!=DateTime.MinValue&&(now-topEdgeSince).TotalMilliseconds>=350;
  if(top.IsVisible&&top.IsMouseOver)lastTopHover=now;
  bool popupOpen=systemPopup!=null&&systemPopup.IsOpen,hoverHold=(now-lastTopHover).TotalMilliseconds<700;bool showTop=cfg.ShowTop&&(popupOpen||!cfg.TopAutoHide||!topCovered||reveal||hoverHold);UpdateTopWorkspace(showTop&&topCovered&&!fullscreenCover&&(reveal||top.IsMouseOver||hoverHold||popupOpen),showTop&&topCovered&&fullscreenCover,stripHeight);top.Visibility=showTop?Visibility.Visible:Visibility.Hidden;
  OutsideClick(point);bool edge=DockEdge(point)&&DateTime.Now>dockDismissedUntil;
  var menu=((FrameworkElement)dock.Content).ContextMenu;
  if(appMenuOpen||DateTime.Now>dockDismissedUntil&&(!cfg.DockAutoHide||edge||dockWanted&&dock.IsMouseOver||pins.IsOpen||menu!=null&&menu.IsOpen||launcher!=null&&launcher.IsVisible)){lastHover=DateTime.Now;AnimateDock(true);}
  else if(DateTime.Now.Subtract(lastHover).TotalMilliseconds>cfg.HideDelay)AnimateDock(false);
 }
 ImageSource Icon(string path) {
  ImageSource img;if(icons.TryGetValue(path,out img))return img;
  try {Native.SHFILEINFO info;if(Native.SHGetFileInfo(path,0,out info,(uint)Marshal.SizeOf(typeof(Native.SHFILEINFO)),0x100)==IntPtr.Zero||info.Icon==IntPtr.Zero)return null;try{img=Imaging.CreateBitmapSourceFromHIcon(info.Icon,Int32Rect.Empty,BitmapSizeOptions.FromWidthAndHeight(32,32));img.Freeze();icons[path]=img;return img;}finally{Native.DestroyIcon(info.Icon);}}catch{return null;}
 }
 void Divider(bool twice) {double scale=SurfaceScale("Taskbar");var panel=new StackPanel {Orientation=Orientation.Horizontal,Margin=new Thickness(7*scale,6*scale,7*scale,6*scale)};for(int i=0;i<(twice?2:1);i++)panel.Children.Add(new Border {Width=Math.Max(1,scale),Background=new SolidColorBrush(Color.FromArgb(120,164,213,180)),Margin=new Thickness(scale,0,scale,0)});apps.Children.Add(panel);}
 void RefreshApps() {
  var windows=new List<Tuple<IntPtr,string,string>>();Native.EnumWindows(delegate(IntPtr h,IntPtr a){
   if(!Native.IsWindowVisible(h)||(Native.GetWindowLong(h,-20)&0x80)!=0)return true; int cloaked;Native.DwmGetWindowAttribute(h,14,out cloaked,4);if(cloaked!=0)return true;
   uint pid;Native.GetWindowThreadProcessId(h,out pid);if(pid==selfPid)return true;
   var text=new StringBuilder(512);Native.GetWindowText(h,text,512);if(text.Length==0||text.ToString()=="Program Manager")return true;
   string path="";try {using(var process=Process.GetProcessById((int)pid))path=process.MainModule.FileName;}catch{}
   windows.Add(Tuple.Create(h,text.ToString(),path));return true;
  },IntPtr.Zero);
  string sig=String.Join("|",windows.Select(w=>w.Item1+":"+w.Item2));if(sig==signature&&apps.Children.Count>0)return;signature=sig;
  double taskbarScale=SurfaceScale("Taskbar");apps.Children.Clear();var pinnedButton=Button("\uE718","Pinned applications",ShowPins,true);pinnedButton.Width=42*taskbarScale;pinnedButton.Height=42*taskbarScale;pinnedButton.FontSize=20*taskbarScale;pinnedButton.Padding=new Thickness(7*taskbarScale,4*taskbarScale,7*taskbarScale,4*taskbarScale);apps.Children.Add(pinnedButton);Divider(false);
  int limit=Math.Max(3,(int)((SystemParameters.PrimaryScreenWidth-240)/(taskbarScale*(cfg.AppLabels?162:cfg.DockIconSize+cfg.IconSpacing+9)))),n=0;
  foreach(var item in windows.Take(limit)) {var win=item;var b=Button("\uE737",win.Item2,()=>{if(Native.IsIconic(win.Item1))Native.ShowWindowAsync(win.Item1,9);Native.SetForegroundWindow(win.Item1);},true);b.Width=(cfg.AppLabels?160:cfg.DockIconSize+cfg.IconSpacing+7)*taskbarScale;b.Height=42*taskbarScale;b.FontSize=20*taskbarScale;b.Padding=new Thickness(7*taskbarScale,4*taskbarScale,7*taskbarScale,4*taskbarScale);var icon=Icon(win.Item3);if(icon!=null)b.Content=new Image {Source=Tone(icon),Opacity=cfg.IconOpacity,Width=cfg.DockIconSize*taskbarScale,Height=cfg.DockIconSize*taskbarScale};if(cfg.AppLabels){var row=new StackPanel{Orientation=Orientation.Horizontal};var art=b.Content as UIElement;b.Content=null;if(art!=null)row.Children.Add(art);var label=Text(win.Item2,11*taskbarScale);label.Width=100*taskbarScale;label.Margin=new Thickness(6*taskbarScale,0,0,0);label.TextWrapping=TextWrapping.NoWrap;label.TextTrimming=TextTrimming.CharacterEllipsis;row.Children.Add(label);b.Content=row;}AppMenu(b,win.Item1);apps.Children.Add(b);n++;}
  if(windows.Count>limit) {var moreWindows=Button("+"+(windows.Count-limit),"More windows",()=>{var m=new ContextMenu();ThemeContextMenu(m);foreach(var item in windows.Skip(limit)){var w=item;var mi=new MenuItem{Header=w.Item2};mi.Click+=delegate{Native.ShowWindowAsync(w.Item1,9);Native.SetForegroundWindow(w.Item1);};m.Items.Add(mi);}m.IsOpen=true;});moreWindows.Width=48*taskbarScale;moreWindows.Height=42*taskbarScale;moreWindows.FontSize=12*taskbarScale;apps.Children.Add(moreWindows);}
  Divider(true);var start=Button("","Start menu",()=>ToggleLauncher(),true);start.Width=42*taskbarScale;start.Height=42*taskbarScale;start.Padding=new Thickness(7*taskbarScale);var dots=new UniformGrid{Rows=3,Columns=3,Width=21*taskbarScale,Height=21*taskbarScale};for(int i=0;i<9;i++)dots.Children.Add(new Border{Background=ink,CornerRadius=new CornerRadius(1),Margin=new Thickness(1.5*taskbarScale)});start.Content=dots;apps.Children.Add(start);dockLength=Math.Min((cfg.Position=="Left"||cfg.Position=="Right"?SystemParameters.PrimaryScreenHeight:SystemParameters.PrimaryScreenWidth)-24,158+n*(cfg.AppLabels?160:cfg.DockIconSize+cfg.IconSpacing+7)+(windows.Count>limit?50:0));Layout();
 }
 void ShowPins() {
  if(pins.IsOpen){pins.IsOpen=false;return;}pins.Placement=cfg.Position=="Top"?PlacementMode.Bottom:cfg.Position=="Left"?PlacementMode.Right:cfg.Position=="Right"?PlacementMode.Left:PlacementMode.Top;pins.Child=BuildPins();pins.IsOpen=true;
 }
 FrameworkElement BuildPins(){double pinnedScale=SurfaceScale("Pinned column");var list=new StackPanel();var allPins=DockEntries();if(allPins.Count>6){var more=Button("…","All pinned apps",()=>{pins.IsOpen=false;ShowAllPins();});more.Width=48*pinnedScale;more.Height=36*pinnedScale;more.FontSize=14*pinnedScale;more.Padding=new Thickness(6*pinnedScale);list.Children.Add(more);}
  foreach(var app in allPins.Take(6)){var entry=app;var b=Button("\uE737",entry.Name,()=>{pins.IsOpen=false;OpenApp(entry);},true);b.Width=48*pinnedScale;b.Height=44*pinnedScale;b.Padding=new Thickness(6*pinnedScale);b.FontSize=20*pinnedScale;var icon=AppIcon(entry.Target);if(icon!=null)b.Content=new Image{Source=Tone(icon),Opacity=cfg.IconOpacity,Width=30*pinnedScale,Height=30*pinnedScale};System.Windows.Automation.AutomationProperties.SetName(b,entry.Name);list.Children.Add(b);}
  if(list.Children.Count==0)list.Children.Add(new TextBlock {Text="No taskbar shortcut pins found",Foreground=ink,Margin=new Thickness(12)});
  var pinnedSurface=new Border {Background=SurfaceBrush(baseColor,cfg.Transparency?cfg.Opacity:1,cfg.PinnedBarGlass??cfg.GlassEffect),BorderBrush=accentBrush,BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(12*pinnedScale),Padding=new Thickness(6*pinnedScale),Child=new ScrollViewer {Content=list,MaxHeight=SystemParameters.PrimaryScreenHeight*0.65,VerticalScrollBarVisibility=ScrollBarVisibility.Hidden,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled}};pinnedSurface.LayoutTransform=Transform.Identity;return pinnedSurface;
 }
 async Task StatusUpdate() {
  if(busy||closing)return;busy=true;
  try {
   var power=Forms.SystemInformation.PowerStatus;int pct=(int)Math.Round(power.BatteryLifePercent*100);battery.Text=power.BatteryChargeStatus.HasFlag(Forms.BatteryChargeStatus.NoSystemBattery)?"AC":(pct<=100?pct+"%":"—");battery.ToolTip=power.PowerLineStatus==Forms.PowerLineStatus.Online?"Plugged in":"On battery";
   batteryFill.Width=pct>=0&&pct<=100?33*pct/100.0:0;batteryFill.Background=new SolidColorBrush(pct<=20?Color.FromArgb(180,195,107,74):Color.FromArgb(155,86,172,121));
   float v;bool muted;bool output=Audio.Read(0,out v,out muted);Glyph(volume,muted||v<0.01?"\uE74F":v<0.34?"\uE992":v<0.67?"\uE993":"\uE994",output?Math.Round(v*100)+"%":"—");
   bool input=Audio.Read(1,out v,out muted);Glyph(mic,"\uE720",input?(muted?"muted":"on"):"—");
   try{var profile=NetworkInformation.GetInternetConnectionProfile();wifi.Text=profile==null?"Wi-Fi offline":profile.IsWlanConnectionProfile?"Wi-Fi "+(profile.GetNetworkConnectivityLevel()==NetworkConnectivityLevel.InternetAccess?"●":"limited"):"Ethernet"; if(profile!=null&&profile.IsWlanConnectionProfile)wifi.ToolTip=profile.WlanConnectionProfileDetails.GetConnectedSsid();}catch{wifi.Text="Wi-Fi —";}
   try{var radios=await System.WindowsRuntimeSystemExtensions.AsTask<System.Collections.Generic.IReadOnlyList<Radio>>(Radio.GetRadiosAsync());var bt=radios.FirstOrDefault(x=>x.Kind==RadioKind.Bluetooth);bluetooth.Text=bt==null?"BT —":bt.State==RadioState.On?"BT on":"BT off";if(bt==null){int size=4;IntPtr radio;var find=Native.BluetoothFindFirstRadio(ref size,out radio);if(find!=IntPtr.Zero){bluetooth.Text="BT ready";Native.CloseHandle(radio);Native.BluetoothFindRadioClose(find);}bluetooth.ToolTip="Bluetooth radio availability; connection status is in Bluetooth settings";}}catch{bluetooth.Text="BT —";}
   int bright=ReadBrightness();Glyph(brightness,"\uE706",bright<0?"—":bright+"%");
   Glyph(wifi,"\uE701",wifi.Text.Replace("Wi-Fi ",""));Glyph(bluetooth,"\uE702",bluetooth.Text.Replace("BT ",""));
   clock.Text=DateTime.Now.ToString(cfg.DateFormat);if(weatherLabel!=null)weatherLabel.Text=weather;clock.ToolTip=cfg.WeatherLocation+" weather · Open-Meteo.com · "+(weatherTime==DateTime.MinValue?"unavailable":"updated "+weatherTime.ToString("HH:mm"));
   media.Text="";progress.Visibility=Visibility.Collapsed;
   if(cfg.ShowMedia&&manager!=null){var session=manager.GetCurrentSession();if(session!=null&&session.GetPlaybackInfo().PlaybackStatus==GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing){var props=await System.WindowsRuntimeSystemExtensions.AsTask<GlobalSystemMediaTransportControlsSessionMediaProperties>(session.TryGetMediaPropertiesAsync());var timeline=session.GetTimelineProperties();media.Text="▶  "+props.Title;media.ToolTip=props.Artist+" — "+props.Title;var duration=(timeline.EndTime-timeline.StartTime).TotalSeconds;if(duration>0){progress.Value=Math.Max(0,Math.Min(100,(timeline.Position-timeline.StartTime).TotalSeconds/duration*100));progress.Visibility=Visibility.Visible;}}}
   await Weather();
   if(diagnostics){clock.Text=DateTime.Now.ToString(cfg.DateFormat);File.WriteAllText(Path.Combine(Root,"verification.json"),new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(new {Battery=battery.Text,Volume=volume.Text,Microphone=mic.Text,Network=wifi.Text,Bluetooth=bluetooth.Text,Weather=weather,Media=media.Text,MediaApi=manager!=null,TaskbarAutoHide=(Native.TaskbarState()&1)!=0,DockWidth=dock.Width,TopWidth=top.Width,BatteryFillWidth=batteryFill.Width,NativeTaskbarVisible=Native.IsWindowVisible(Native.FindWindow("Shell_TrayWnd",null)),TopCovered=topCovered,TopVisible=top.IsVisible}));Render(top,"top-preview.png");Render(dock,"dock-preview.png");var pinned=BuildPins();pinned.Measure(new Size(80,1000));RenderElement(pinned,"pins-preview.png",pinned.DesiredSize.Width,pinned.DesiredSize.Height);if(launcher!=null)Render(launcher,"launcher-preview.png");diagnostics=false;}
  }catch(Exception e){Log(e.Message);}finally{busy=false;}
 }
 void Render(Window w,string file){RenderElement((FrameworkElement)w.Content,file,w.Width,w.Height);}
 void RenderElement(FrameworkElement element,string file,double width,double height){element.Measure(new Size(width,height));element.Arrange(new Rect(0,0,width,height));var bmp=new RenderTargetBitmap((int)Math.Ceiling(width)*2,(int)Math.Ceiling(height)*2,192,192,PixelFormats.Pbgra32);bmp.Render(element);var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(bmp));using(var f=File.Create(Path.Combine(Root,file)))png.Save(f);}
 static string lastError=""; static void Log(string message){if(message==lastError)return;lastError=message;try{File.AppendAllText(Path.Combine(Root,"error.log"),DateTime.Now+" "+message+Environment.NewLine);}catch{}}
 void Quit(){if(closing)return;StopKeyboardHook();UnwatchPopup();ReleaseTopWorkspace();closing=true;if(hover!=null)hover.Stop();if(poll!=null)poll.Stop();if(rollingRecorder!=null){var recorder=rollingRecorder;rollingRecorder=null;Task.Run(()=>recorder.Stop());}pins.IsOpen=false;if(systemPopup!=null)systemPopup.IsOpen=false;if(altTabWindow!=null)altTabWindow.Close();if(tray!=null)tray.Dispose();Restore();Shutdown();}
}
}














