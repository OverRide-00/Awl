using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Web.Script.Serialization;

namespace Awl {partial class Shell {
 [StructLayout(LayoutKind.Sequential)]struct MotionThumbnail {public uint Flags;public Native.RECT Destination,Source;public byte Opacity;[MarshalAs(UnmanagedType.Bool)]public bool Visible;[MarshalAs(UnmanagedType.Bool)]public bool ClientOnly;}
 [DllImport("dwmapi.dll")]static extern int DwmRegisterThumbnail(IntPtr destination,IntPtr source,out IntPtr thumbnail);
 [DllImport("dwmapi.dll")]static extern int DwmUpdateThumbnailProperties(IntPtr thumbnail,ref MotionThumbnail properties);
 [DllImport("dwmapi.dll")]static extern int DwmUnregisterThumbnail(IntPtr thumbnail);
 [DllImport("user32.dll")]static extern bool SetWindowPos(IntPtr window,IntPtr after,int x,int y,int width,int height,uint flags);
 readonly Dictionary<IntPtr,Action> nativeAppMotions=new Dictionary<IntPtr,Action>();
 static readonly Dictionary<string,uint> motionCloaks=new Dictionary<string,uint>();
 static string MotionCloakFile {get{return Path.Combine(Root,"motion-cloaks.json");}}
 bool motionGuardStarted;
 public static bool RunMotionGuard(string[] args){if(args.Length!=3||args[0]!="--motion-guard")return false;Root=args[2];try{using(var parent=Process.GetProcessById(Int32.Parse(args[1])))parent.WaitForExit();}catch{}RestoreMotionCloaks();return true;}
 void EnsureMotionGuard(){if(!devMode||motionGuardStarted)return;Process.Start(new ProcessStartInfo(typeof(Shell).Assembly.Location,"--motion-guard "+Process.GetCurrentProcess().Id+" \""+Root+"\""){UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden});motionGuardStarted=true;}
 static void SaveMotionCloaks(){File.WriteAllText(MotionCloakFile,new JavaScriptSerializer().Serialize(motionCloaks));}
 static void RestoreMotionCloaks(){
  try{if(!File.Exists(MotionCloakFile))return;var saved=new JavaScriptSerializer().Deserialize<Dictionary<string,uint>>(File.ReadAllText(MotionCloakFile));foreach(var pair in saved){long raw;if(!Int64.TryParse(pair.Key,out raw))continue;var handle=new IntPtr(raw);uint pid;Native.GetWindowThreadProcessId(handle,out pid);if(pid==pair.Value){int value=0;Native.DwmSetWindowAttribute(handle,13,ref value,4);}}File.Delete(MotionCloakFile);}catch{}
 }
 void StopNativeAppMotions(){foreach(var stop in new List<Action>(nativeAppMotions.Values))stop();}
 bool AnimateNativeApp(IntPtr handle,Button icon){
  var api=VisualMotionApi;if(api==null||!cfg.Animations||!visualMotion.appLaunch||handle==IntPtr.Zero||Native.IsIconic(handle)||!icon.IsVisible)return false;
  int cloaked;if(Native.DwmGetWindowAttribute(handle,14,out cloaked,4)!=0||cloaked!=0)return false;
  Native.RECT target;if(!Native.GetWindowRect(handle,out target)||target.Right-target.Left<80||target.Bottom-target.Top<60)return false;
  uint pid;Native.GetWindowThreadProcessId(handle,out pid);Action previous;if(nativeAppMotions.TryGetValue(handle,out previous))previous();
  var point=icon.PointToScreen(new Point(icon.ActualWidth/2,icon.ActualHeight/2));double width=target.Right-target.Left,height=target.Bottom-target.Top;
  double initial=Math.Min(.12,Math.Max(24,Math.Min(icon.ActualWidth,icon.ActualHeight))/Math.Max(width,height));
  var settings=new HwndSourceParameters("Awl app motion"){WindowStyle=unchecked((int)0x80000000),ExtendedWindowStyle=0x08000000|0x80|0x20,PositionX=(int)point.X,PositionY=(int)point.Y,Width=1,Height=1};
  HwndSource host=null;IntPtr thumbnail=IntPtr.Zero;IAwlAnimationHandle animation=null;bool done=false,ownsCloak=false;
  Action finish=null;finish=()=>{if(done)return;done=true;if(animation!=null)animation.Cancel();if(ownsCloak){uint current;Native.GetWindowThreadProcessId(handle,out current);if(current==pid){int value=0;Native.DwmSetWindowAttribute(handle,13,ref value,4);}motionCloaks.Remove(handle.ToInt64().ToString());try{SaveMotionCloaks();}catch{}}if(thumbnail!=IntPtr.Zero)DwmUnregisterThumbnail(thumbnail);if(host!=null)host.Dispose();nativeAppMotions.Remove(handle);};
  try{
   host=new HwndSource(settings);host.CompositionTarget.BackgroundColor=Colors.Black;
   if(DwmRegisterThumbnail(host.Handle,handle,out thumbnail)!=0){finish();return false;}
   var first=new MotionThumbnail{Flags=1|4|8|16,Destination=new Native.RECT{Right=1,Bottom=1},Opacity=255,Visible=true,ClientOnly=false};
   if(DwmUpdateThumbnailProperties(thumbnail,ref first)!=0){finish();return false;}
   // The guard can undo our cloak if Awl exits unexpectedly.
   EnsureMotionGuard();motionCloaks[handle.ToInt64().ToString()]=pid;SaveMotionCloaks();int cloak=1;
   if(Native.DwmSetWindowAttribute(handle,13,ref cloak,4)!=0){motionCloaks.Remove(handle.ToInt64().ToString());SaveMotionCloaks();finish();return false;}ownsCloak=true;nativeAppMotions[handle]=finish;
   Action<double> frame=p=>{
    if(!Native.IsWindowVisible(handle)){finish();return;}
    double ease=api.Evaluate(AwlAnimationCurve.EaseOut,p),size=initial+(1-initial)*ease;
    int w=Math.Max(1,(int)Math.Round(width*size)),h=Math.Max(1,(int)Math.Round(height*size));
    double centerX=point.X+(target.Left+width/2-point.X)*ease,centerY=point.Y+(target.Top+height/2-point.Y)*ease;
    SetWindowPos(host.Handle,new IntPtr(-1),(int)Math.Round(centerX-w/2),(int)Math.Round(centerY-h/2),w,h,0x10|0x40);
    var props=new MotionThumbnail{Flags=1|4|8|16,Destination=new Native.RECT{Right=w,Bottom=h},Opacity=255,Visible=true,ClientOnly=false};
    if(DwmUpdateThumbnailProperties(thumbnail,ref props)!=0)finish();
   };
   frame(0);if(done)return false;
   animation=api.Animate(new AwlAnimationSpec{DurationMs=MotionDuration(visualMotion.windowDurationMs),ProceduralCurve=p=>p},frame,finish);return true;
  }catch(Exception error){finish();Log("App motion: "+error.Message);return false;}
 }
}}
