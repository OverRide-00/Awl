using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace Awl { partial class Shell {
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode)] static extern int GetApplicationUserModelId(IntPtr process,ref uint length,StringBuilder value);
 [DllImport("user32.dll")] static extern bool EnumChildWindows(IntPtr parent,Native.EnumProc callback,IntPtr parameter);
 [DllImport("user32.dll",EntryPoint="GetClassLongPtrW")] static extern IntPtr GetClassLongPtr(IntPtr window,int index);
 string WindowAppModelId(IntPtr window){
  string id=ProcessAppModelId(window);if(!String.IsNullOrEmpty(id))return id;
  EnumChildWindows(window,delegate(IntPtr child,IntPtr unused){id=ProcessAppModelId(child);return String.IsNullOrEmpty(id);},IntPtr.Zero);
  return id;
 }
 string ProcessAppModelId(IntPtr window){
  uint pid;Native.GetWindowThreadProcessId(window,out pid);if(pid==0)return null;
  try{using(var process=Process.GetProcessById((int)pid)){
   uint length=0;if(GetApplicationUserModelId(process.Handle,ref length,null)!=122||length<2||length>512)return null;
   var value=new StringBuilder((int)length);return GetApplicationUserModelId(process.Handle,ref length,value)==0?value.ToString():null;
  }}catch{return null;}
 }
 ImageSource ExplicitWindowIcon(IntPtr window){
  IntPtr icon=IntPtr.Zero;
  foreach(int size in new[]{1,2,0}){IntPtr result;if(SendMessageTimeout(window,0x7F,new IntPtr(size),IntPtr.Zero,2,75,out result)!=IntPtr.Zero&&result!=IntPtr.Zero){icon=result;break;}}
  if(icon==IntPtr.Zero)icon=GetClassLongPtr(window,-14);
  if(icon==IntPtr.Zero)icon=GetClassLongPtr(window,-34);
  if(icon==IntPtr.Zero)return null;
  try{var image=Imaging.CreateBitmapSourceFromHIcon(icon,Int32Rect.Empty,System.Windows.Media.Imaging.BitmapSizeOptions.FromWidthAndHeight(48,48));image.Freeze();return image;}catch{return null;}
 }
 ImageSource TaskbarWindowIcon(IntPtr window,string executable){
  ImageSource packed;bool naturalPng;if(TryIconPackAppIcon(executable,out packed,out naturalPng))return packed;
  string appId=WindowAppModelId(window);
  if(!String.IsNullOrWhiteSpace(appId)){var image=AppIcon("shell:AppsFolder\\"+appId);if(image!=null)return image;}
  return ExplicitWindowIcon(window)??Icon(executable);
 }
 public string WindowIconSelfTest(){
  var results=new System.Collections.Generic.List<string>();
  foreach(string id in new[]{"windows.immersivecontrolpanel_cw5n1h2txyewy!microsoft.windows.immersivecontrolpanel","5319275A.WhatsAppDesktop_cv1g1gvanyjgm!App"}){
   var image=AppIcon("shell:AppsFolder\\"+id);results.Add(id+": "+(image==null?"missing":"resolved"));
  }
  Native.EnumWindows(delegate(IntPtr window,IntPtr unused){var title=new StringBuilder(256);Native.GetWindowText(window,title,256);if(title.ToString().IndexOf("Settings",StringComparison.OrdinalIgnoreCase)>=0||title.ToString().IndexOf("WhatsApp",StringComparison.OrdinalIgnoreCase)>=0)results.Add(title+": "+(WindowAppModelId(window)??"no process app ID"));return true;},IntPtr.Zero);
  return String.Join(Environment.NewLine,results);
 }
} }
