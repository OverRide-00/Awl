using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;
using Microsoft.Win32;

namespace Awl { partial class Shell {
 string cursorSignature="";Cursor themedCursor;

 bool OrdinaryWindow(IntPtr handle){
  if(handle==IntPtr.Zero||!Native.IsWindowVisible(handle)||Native.IsIconic(handle)||Native.IsZoomed(handle))return false;
  uint pid;Native.GetWindowThreadProcessId(handle,out pid);if(pid==selfPid)return false;
  int style=Native.GetWindowLong(handle,-16),extended=Native.GetWindowLong(handle,-20);if((style&0x00C00000)==0||(extended&0x80)!=0)return false;
  Native.RECT rect;if(!Native.GetWindowRect(handle,out rect))return false;
  var screen=System.Windows.Forms.Screen.FromHandle(handle).Bounds;
  return !(rect.Left<=screen.Left&&rect.Top<=screen.Top&&rect.Right>=screen.Right&&rect.Bottom>=screen.Bottom);
 }
 void ApplyWindowCustomizations(){
  try{
   int corner=cfg.WindowCornerRadius<=0?1:cfg.WindowCornerRadius<=12?3:2;
   if(!devMode)Native.EnumWindows(delegate(IntPtr handle,IntPtr unused){if(!OrdinaryWindow(handle))return true;try{Native.DwmSetWindowAttribute(handle,33,ref corner,4);var frame=new Native.MARGINS{Left=cfg.WindowShadows?1:0,Right=cfg.WindowShadows?1:0,Top=cfg.WindowShadows?1:0,Bottom=cfg.WindowShadows?1:0};Native.DwmExtendFrameIntoClientArea(handle,ref frame);}catch{}return true;},IntPtr.Zero);
  }catch(Exception error){Log("Window styling: "+error.Message);}
  ApplyShellShadowAndCursor();
 }
 void ApplySnapLayouts(){
  try{using(var key=Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced")){key.SetValue("EnableSnapAssistFlyout",cfg.SnapLayouts?1:0,RegistryValueKind.DWord);key.SetValue("EnableSnapBar",cfg.SnapLayouts?1:0,RegistryValueKind.DWord);}using(var key=Registry.CurrentUser.CreateSubKey("Control Panel\\Desktop"))key.SetValue("WindowArrangementActive",cfg.SnapLayouts?"1":"0",RegistryValueKind.String);Native.SystemParametersInfo(0x0083,cfg.SnapLayouts?1u:0u,IntPtr.Zero,3);}
  catch(Exception error){Log("Snap layouts: "+error.Message);}
 }
 void ApplyShellShadowAndCursor(){
  foreach(Window window in Current.Windows){window.Cursor=AppCursor();var surface=window.Content as Border;if(surface!=null&&surface.CornerRadius.TopLeft>0)surface.Effect=ShellShadow();ApplyVisualStyle(window);}if(systemPopup!=null){var popupSurface=systemPopup.Child as Border;if(popupSurface!=null)popupSurface.Effect=ShellShadow();}
 }
 Effect ShellShadow(){return cfg.WindowShadows?(Effect)new DropShadowEffect{Color=Colors.Black,BlurRadius=18,ShadowDepth=5,Opacity=.36}:null;}
 void ApplyVisualStyle(DependencyObject node){
  var button=node as Button;if(button!=null)button.Cursor=AppCursor(true);
  for(int i=0;i<VisualTreeHelper.GetChildrenCount(node);i++)ApplyVisualStyle(VisualTreeHelper.GetChild(node,i));
 }
 void InvalidateAppCursor(){cursorSignature="";if(themedCursor!=null){try{themedCursor.Dispose();}catch{}themedCursor=null;}}
 Cursor AppCursor(bool interactive=false){if(interactive)return Cursors.Hand;
  if(cfg.CursorStyle=="System")return Cursors.Arrow;
  string signature=cfg.CursorStyle+"|"+Math.Round(cfg.CursorSize)+"|"+Hex(Parse(cfg.IconColor,Colors.White));if(themedCursor!=null&&signature==cursorSignature)return themedCursor;
  try{InvalidateAppCursor();int size=Math.Max(16,Math.Min(64,(int)Math.Round(32*cfg.CursorSize/100)));string path=Path.Combine(Root,"cursor-"+signature.Replace("|","-").Replace("#","")+".cur");if(!File.Exists(path))WriteCursor(path,size,cfg.CursorStyle=="Accent");themedCursor=new Cursor(path);cursorSignature=signature;return themedCursor;}catch(Exception error){Log("Cursor: "+error.Message);return Cursors.Arrow;}
 }
 void WriteCursor(string path,int size,bool filled){
  using(var bitmap=new System.Drawing.Bitmap(size,size,System.Drawing.Imaging.PixelFormat.Format32bppArgb))using(var graphics=System.Drawing.Graphics.FromImage(bitmap))using(var shape=new System.Drawing.Drawing2D.GraphicsPath()){
   graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;float s=size/32f;shape.AddPolygon(new[]{new System.Drawing.PointF(2*s,1*s),new System.Drawing.PointF(2*s,25*s),new System.Drawing.PointF(8*s,19*s),new System.Drawing.PointF(13*s,30*s),new System.Drawing.PointF(18*s,27*s),new System.Drawing.PointF(12*s,17*s),new System.Drawing.PointF(23*s,17*s)});var color=Parse(cfg.IconColor,Colors.White);var draw=System.Drawing.Color.FromArgb(color.A,color.R,color.G,color.B);using(var fill=new System.Drawing.SolidBrush(filled?draw:System.Drawing.Color.FromArgb(55,draw)))graphics.FillPath(fill,shape);using(var pen=new System.Drawing.Pen(filled?System.Drawing.Color.White:draw,Math.Max(1.3f,1.6f*s))){pen.LineJoin=System.Drawing.Drawing2D.LineJoin.Round;graphics.DrawPath(pen,shape);}
   int xor=size*size*4,maskStride=((size+31)/32)*4,mask=maskStride*size;using(var stream=File.Create(path))using(var writer=new BinaryWriter(stream)){writer.Write((ushort)0);writer.Write((ushort)2);writer.Write((ushort)1);writer.Write((byte)(size==256?0:size));writer.Write((byte)(size==256?0:size));writer.Write((byte)0);writer.Write((byte)0);writer.Write((ushort)Math.Max(1,(int)(2*s)));writer.Write((ushort)Math.Max(1,(int)(2*s)));writer.Write(40+xor+mask);writer.Write(22);writer.Write(40);writer.Write(size);writer.Write(size*2);writer.Write((ushort)1);writer.Write((ushort)32);writer.Write(0);writer.Write(xor);writer.Write(0);writer.Write(0);writer.Write(0);writer.Write(0);for(int y=size-1;y>=0;y--)for(int x=0;x<size;x++){var pixel=bitmap.GetPixel(x,y);writer.Write(pixel.B);writer.Write(pixel.G);writer.Write(pixel.R);writer.Write(pixel.A);}writer.Write(new byte[mask]);}
  }
 }
} }
