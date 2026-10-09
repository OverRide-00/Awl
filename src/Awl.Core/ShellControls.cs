using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
namespace Awl {partial class Shell {
 bool StartupEnabled(){try{using(var key=Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run"))return key!=null&&!String.IsNullOrEmpty(Convert.ToString(key.GetValue("Awl")));}catch{return false;}}
 void SetStartup(bool enabled){using(var key=Microsoft.Win32.Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run")){if(enabled)key.SetValue("Awl","\""+typeof(Shell).Assembly.Location+"\" --background");else key.DeleteValue("Awl",false);}}
 void ApplyStartupPreference(){if(devMode||!cfg.LaunchOnSignIn.HasValue)return;try{SetStartup(cfg.LaunchOnSignIn.Value);}catch(Exception e){Log("Startup preference: "+e.Message);}}
 TextBox appsSearch;IntPtr keyboardHook;MouseHook keyboardCallback;bool winDown,winUsed,winApps,winShortcutConsumed,winPendingForward;byte winKey=0x5B;DateTime lastWinToggle=DateTime.MinValue;IntPtr lastStartForeground;
 void FocusAppsSearch(){if(activeTab!="Apps"||appsSearch==null)return;var search=appsSearch;Dispatcher.BeginInvoke(DispatcherPriority.Input,new Action(()=>{if(launcher!=null&&launcher.IsVisible&&activeTab=="Apps"&&search==appsSearch){search.Focus();Keyboard.Focus(search);search.SelectAll();}}));}
 void LargeOption(Button button,string label){int split=label.IndexOf(' ');if(split<0)return;var row=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Center};var icon=Text(label.Substring(0,split),Math.Max(18,Math.Min(30,cfg.QuickOptionIconSize)),button.Foreground);icon.FontWeight=FontWeights.Bold;icon.Margin=new Thickness(0,0,7,0);row.Children.Add(icon);row.Children.Add(Text(label.Substring(split+1),12,button.Foreground));button.Content=row;}
 void EnlargeQuickOptions(UIElement element){var panel=element as Panel;if(panel==null)return;foreach(UIElement child in panel.Children){var button=child as Button;if(button!=null&&button.Content is string)LargeOption(button,(string)button.Content);else EnlargeQuickOptions(child);}}
 void AddPowerControl(StackPanel footer){var button=Action("Power",()=>{});button.Content=NavLabel("Power","Power",ink,17);button.HorizontalContentAlignment=HorizontalAlignment.Left;var popup=new Popup{PlacementTarget=button,Placement=PlacementMode.Top,VerticalOffset=-4,AllowsTransparency=true,StaysOpen=false};var items=new StackPanel();foreach(string name in new[]{"Sleep","Restart","Shut down"}){string operation=name;var option=Action("",()=>{popup.IsOpen=false;RequestPower(operation);});option.Content=BuilderIconLabel(name=="Sleep"?"moon":name=="Restart"?"reset":"power",name,ink,16);StyleDropdownOption(option);if(name=="Shut down")option.Foreground=Brushes.IndianRed;items.Children.Add(option);}var menuSurface=DropdownSurface(items,190);ApplySurfaceScale(menuSurface,"Start menu");popup.Child=menuSurface;BindVisualPopup(popup,"Dropdown");popup.Opened+=delegate{appMenuOpen=true;};popup.Closed+=delegate{appMenuOpen=false;};button.Click+=delegate{popup.IsOpen=!popup.IsOpen;};button.Unloaded+=delegate{popup.IsOpen=false;};footer.Children.Add(button);}
 void RequestPower(string operation){appMenuOpen=true;try{if(operation=="Sleep")System.Windows.Forms.Application.SetSuspendState(System.Windows.Forms.PowerState.Suspend,false,false);else Process.Start(new ProcessStartInfo("shutdown.exe",operation=="Restart"?"/r /t 0":"/s /t 0"){UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden});}finally{appMenuOpen=false;}}
 [DllImport("user32.dll")]static extern void keybd_event(byte key,byte scan,uint flags,UIntPtr extra);
 void StartKeyboardHook(){if(keyboardHook!=IntPtr.Zero)return;keyboardCallback=KeyboardEvent;keyboardHook=SetWindowsHookEx(13,keyboardCallback,GetModuleHandle(null),0);if(keyboardHook==IntPtr.Zero)Log("Keyboard hook unavailable: "+Marshal.GetLastWin32Error());}
 void StopKeyboardHook(){if(keyboardHook!=IntPtr.Zero){UnhookWindowsHookEx(keyboardHook);keyboardHook=IntPtr.Zero;}if(winUsed)keybd_event(winKey,0,2,UIntPtr.Zero);keyboardCallback=null;winDown=false;winUsed=false;winApps=false;winShortcutConsumed=false;winPendingForward=false;recordingWinHeld=false;heldHotkeys.Clear();swallowedHotkeys.Clear();}
 void ToggleWindowsStart(bool apps=false){if(closing||(DateTime.Now-lastWinToggle).TotalMilliseconds<110)return;lastWinToggle=DateTime.Now;ToggleLauncher(apps);}
 IntPtr KeyboardEvent(int code,IntPtr message,IntPtr data){
  if(code<0)return CallNextHookEx(keyboardHook,code,message,data);
  int key=Marshal.ReadInt32(data),flags=Marshal.ReadInt32(data,8);
  if((flags&16)!=0)return CallNextHookEx(keyboardHook,code,message,data);
  bool down=message.ToInt32()==0x100||message.ToInt32()==0x104,up=message.ToInt32()==0x101||message.ToInt32()==0x105;
  int modifiers=CurrentHotModifiers(key,down);
  if(key==0x1B&&down&&captureRecordWindow!=null&&activeRecorder==null){Dispatcher.BeginInvoke(new Action(CancelRecording));return new IntPtr(1);}
  if(key==0x1B&&down&&altTabShowing){Dispatcher.BeginInvoke(new Action(()=>FinishAltTab(false)));return new IntPtr(1);}
  if(recordingKeyBind){if(key==0x5B||key==0x5C){recordingWinHeld=down;return new IntPtr(1);}return CallNextHookEx(keyboardHook,code,message,data);}
  if(HandleShortcutEvent(key,modifiers,down,up))return new IntPtr(1);
  int switchKey,switchModifiers;
  if(up&&altTabShowing&&TryParseShortcut(KeyBind("AppSwitcher"),out switchKey,out switchModifiers)&&ModifierKey(key)){
   int released=key==0x11||key==0xA2||key==0xA3?HotCtrl:key==0x10||key==0xA0||key==0xA1?HotShift:key==0x12||key==0xA4||key==0xA5?HotAlt:0;
   if((switchModifiers&released)!=0)Dispatcher.BeginInvoke(new Action(()=>FinishAltTab(true)));
  }
  if(!cfg.ReplaceWindowsStart)return CallNextHookEx(keyboardHook,code,message,data);
  if(key==0x5B||key==0x5C){
   if(down&&!winDown){winDown=true;winKey=(byte)key;int other=modifiers&~HotWin;winApps=other==StartModifierMask();winPendingForward=!winApps&&other!=0;winUsed=false;winShortcutConsumed=false;}
   if(up&&winDown){bool shortcut=winUsed||winShortcutConsumed||winPendingForward,apps=winApps;winDown=false;winApps=false;winShortcutConsumed=false;winPendingForward=false;if(winUsed){winUsed=false;keybd_event(winKey,0,2,UIntPtr.Zero);}if(!shortcut)Dispatcher.BeginInvoke(new Action(()=>ToggleWindowsStart(apps)));}
   return new IntPtr(1);
  }
  if(winDown&&down&&!winUsed){
   if(ModifierKey(key)&&(modifiers&~HotWin)==StartModifierMask()){winApps=true;return CallNextHookEx(keyboardHook,code,message,data);}
   if(!ModifierKey(key)){winApps=false;winPendingForward=false;winUsed=true;keybd_event(winKey,0,0,UIntPtr.Zero);}
  }
  return CallNextHookEx(keyboardHook,code,message,data);
 } void RedirectNativeStart(){if(closing)return;StartKeyboardHook();if(!cfg.ReplaceWindowsStart)return;IntPtr hwnd=Native.GetForegroundWindow();if(hwnd==IntPtr.Zero)return;try{uint pid;Native.GetWindowThreadProcessId(hwnd,out pid);using(var process=Process.GetProcessById((int)pid)){if(!String.Equals(process.ProcessName,"StartMenuExperienceHost",StringComparison.OrdinalIgnoreCase)){lastStartForeground=IntPtr.Zero;return;}}if(hwnd==lastStartForeground)return;lastStartForeground=hwnd;Native.PostMessage(hwnd,0x0100,new IntPtr(0x1B),IntPtr.Zero);Native.PostMessage(hwnd,0x0101,new IntPtr(0x1B),IntPtr.Zero);StopKeyboardHook();StartKeyboardHook();if(launcher==null||!launcher.IsVisible)ToggleLauncher();else{launcher.Activate();FocusAppsSearch();}}catch{}}
} }

