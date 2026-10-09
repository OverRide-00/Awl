using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Awl { partial class Shell {
 const int HotCtrl=1,HotShift=2,HotAlt=4,HotWin=8;
 static readonly Dictionary<string,string> defaultKeyBinds=new Dictionary<string,string>{
  {"StartFallback","Ctrl+Escape"},{"QuickSearch","Ctrl+Alt+Space"},{"HiddenMode","Ctrl+Alt+H"},
  {"QuitAwl","Ctrl+Alt+Q"},{"CaptureTool","Alt+Shift+R"},{"AppSwitcher","Alt+Tab"},{"WidgetBuilder","Ctrl+Win+W"}
 };
 static readonly string[] keyBindOrder={"StartFallback","QuickSearch","CaptureTool","AppSwitcher","WidgetBuilder","HiddenMode","QuitAwl"};
 readonly HashSet<int> heldHotkeys=new HashSet<int>();
 readonly HashSet<int> swallowedHotkeys=new HashSet<int>();
 bool hiddenMode,printScreenHeld,recordingKeyBind,recordingWinHeld;

 void NormalizeKeyBindings(){
  if(cfg.KeyBinds==null)cfg.KeyBinds=new Dictionary<string,string>();
  if(!new[]{"Ctrl","Shift","Alt"}.Contains(cfg.StartSearchModifier))cfg.StartSearchModifier="Ctrl";
  foreach(string id in keyBindOrder){string value;int key,modifiers;
   if(!cfg.KeyBinds.TryGetValue(id,out value)||(!String.Equals(value,"None",StringComparison.OrdinalIgnoreCase)&&!TryParseShortcut(value,out key,out modifiers)))cfg.KeyBinds[id]=defaultKeyBinds[id];
   else if(String.Equals(value,"None",StringComparison.OrdinalIgnoreCase))cfg.KeyBinds[id]="None";
  }
  // A hand-edited or imported binding cannot take another action's default away.
  foreach(string id in keyBindOrder){string value=cfg.KeyBinds[id];if(value!="None"&&value!=defaultKeyBinds[id]&&defaultKeyBinds.Any(other=>other.Key!=id&&String.Equals(other.Value,value,StringComparison.OrdinalIgnoreCase)))cfg.KeyBinds[id]=defaultKeyBinds[id];}
  var used=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
  foreach(string id in keyBindOrder){string value=cfg.KeyBinds[id];if(value=="None")continue;if(!used.Add(value)){cfg.KeyBinds[id]=defaultKeyBinds[id];used.Add(defaultKeyBinds[id]);}}
 }
 string KeyBind(string id){string value;return cfg.KeyBinds!=null&&cfg.KeyBinds.TryGetValue(id,out value)?value:defaultKeyBinds[id];}
 static bool TryParseShortcut(string text,out int key,out int modifiers){
  key=0;modifiers=0;if(String.IsNullOrWhiteSpace(text)||text=="None")return false;
  var pieces=text.Split('+');if(pieces.Length<2||pieces.Length>4)return false;
  foreach(string piece in pieces){switch(piece.Trim().ToLowerInvariant()){
   case "ctrl":case "control":modifiers|=HotCtrl;break;
   case "shift":modifiers|=HotShift;break;
   case "alt":modifiers|=HotAlt;break;
   case "win":case "windows":modifiers|=HotWin;break;
   default:
    if(key!=0)return false;string name=piece.Trim();
    if(name.Length==1&&Char.IsDigit(name[0]))key=0x30+(name[0]-'0');
    else {Key parsed;if(!Enum.TryParse(name,true,out parsed))return false;key=KeyInterop.VirtualKeyFromKey(parsed);}
    if(key<=0||key==0x5B||key==0x5C||key==0x2C||key==0x10||key==0x11||key==0x12)return false;break;
  }}
  return key!=0&&modifiers!=0;
 }
 static string ShortcutName(Key key){
  string name=key.ToString();if(key>=Key.D0&&key<=Key.D9)return name.Substring(1);
  if(key==Key.Escape)return "Escape";if(key==Key.Space)return "Space";if(key==Key.Tab)return "Tab";
  if(key==Key.Return)return "Enter";return name;
 }
 static bool ModifierKey(int key){return key==0x10||key==0x11||key==0x12||key==0xA0||key==0xA1||key==0xA2||key==0xA3||key==0xA4||key==0xA5;}
 bool HotModifierDown(int key,bool down,int generic,int left,int right){
  if(key==generic||key==left||key==right){if(down)return true;return (key!=left&&(GetAsyncKeyState(left)&0x8000)!=0)||(key!=right&&(GetAsyncKeyState(right)&0x8000)!=0);}
  return (GetAsyncKeyState(generic)&0x8000)!=0||(GetAsyncKeyState(left)&0x8000)!=0||(GetAsyncKeyState(right)&0x8000)!=0;
 }
 int CurrentHotModifiers(int key,bool down){int result=0;
  if(HotModifierDown(key,down,0x11,0xA2,0xA3))result|=HotCtrl;
  if(HotModifierDown(key,down,0x10,0xA0,0xA1))result|=HotShift;
  if(HotModifierDown(key,down,0x12,0xA4,0xA5))result|=HotAlt;
  if(winDown||(GetAsyncKeyState(0x5B)&0x8000)!=0||(GetAsyncKeyState(0x5C)&0x8000)!=0)result|=HotWin;return result;
 }
 int StartModifierMask(){return cfg.StartSearchModifier=="Shift"?HotShift:cfg.StartSearchModifier=="Alt"?HotAlt:HotCtrl;}
 string ResolveHotkey(int key,int modifiers){
  foreach(string id in keyBindOrder){
   int trigger,required;if(!TryParseShortcut(KeyBind(id),out trigger,out required)||trigger!=key)continue;
   if(id=="StartFallback"&&!cfg.ReplaceWindowsStart)continue;
   if(id=="WidgetBuilder"&&!WidgetBuilderInstalled())continue;
   if(required==modifiers||id=="AppSwitcher"&&required==(modifiers&~HotShift)&&modifiers==(required|HotShift))return id;
  }
  return null;
 }
 void InvokeHotkey(string id,int modifiers){
  if(id=="StartFallback")ToggleWindowsStart(true);
  else if(id=="QuickSearch")ShowQuickSearch();
  else if(id=="HiddenMode")ToggleHiddenMode();
  else if(id=="QuitAwl")Quit();
  else if(id=="CaptureTool")ShowCaptureTool();
  else if(id=="AppSwitcher")ShowAltTab((modifiers&HotShift)!=0);
  else if(id=="WidgetBuilder")LaunchWidgetBuilderExecutable();
 }
 bool HandleShortcutEvent(int key,int modifiers,bool down,bool up){
  if((cfg.CapturePrintScreen||printScreenHeld)&&key==0x2C&&(modifiers==0||printScreenHeld)){
   if(down&&!printScreenHeld){printScreenHeld=true;Dispatcher.BeginInvoke(new Action(ShowCaptureTool));}
   if(up)printScreenHeld=false;return true;
  }
  if(swallowedHotkeys.Contains(key)){if(up){swallowedHotkeys.Remove(key);heldHotkeys.Remove(key);}return true;}
  if(up){heldHotkeys.Remove(key);return false;}
  if(!down||ModifierKey(key))return false;
  string id=ResolveHotkey(key,modifiers);if(id==null)return false;
  if((modifiers&HotWin)!=0&&winDown){winApps=false;winShortcutConsumed=true;winPendingForward=false;if(winUsed){keybd_event(winKey,0,2,UIntPtr.Zero);winUsed=false;}}
  swallowedHotkeys.Add(key);if(heldHotkeys.Add(key))Dispatcher.BeginInvoke(new Action(()=>InvokeHotkey(id,modifiers)));return true;
 }
 void ToggleHiddenMode(){
  hiddenMode=!hiddenMode;
  if(tray!=null)tray.Text=hiddenMode?"Awl — Hidden mode":"Awl — right-click to restore";
  if(hiddenMode){
   if(top!=null){StopVisualSurface(top.Content as FrameworkElement);top.Visibility=Visibility.Hidden;}
   ReleaseTopWorkspace();
   if(dock!=null){StopVisualSurface(dock.Content as FrameworkElement);dock.BeginAnimation(Window.LeftProperty,null);dock.BeginAnimation(Window.TopProperty,null);dock.Hide();dockWanted=false;}
  }else{lastHover=DateTime.Now;if(dock!=null)AnimateDock(true);}
 }
 void SaveKeyBind(string id,string value){
  if(cfg.KeyBinds==null)cfg.KeyBinds=new Dictionary<string,string>();
  if(value!="None"){int key,mods;if(!TryParseShortcut(value,out key,out mods))throw new ArgumentException("Choose a shortcut with Ctrl, Shift, or Alt and a regular key.");
   if(defaultKeyBinds.Keys.Any(other=>other!=id&&String.Equals(KeyBind(other),value,StringComparison.OrdinalIgnoreCase)))throw new ArgumentException("That shortcut is already assigned to another Awl action.");}
  cfg.KeyBinds[id]=value;SaveConfig();
  FrameworkElement capture;if(id=="CaptureTool"&&moduleViews!=null&&moduleViews.TryGetValue("Capture",out capture))capture.ToolTip="Capture · "+value.Replace("+"," + ");
 }
 void KeyBindingsCard(){
  page.Children.Add(Card("Keyboard shortcuts",()=>{
   page.Children.Add(Text("Click a shortcut, then press its new keys. Esc cancels; Backspace clears it. Changes apply immediately.",11,mutedBrush));
   ShortcutRow("Start menu · app search","StartSearchModifier",null);
   ShortcutRow("Start menu · fallback","StartFallback","Ctrl + Esc can be changed or cleared. The Windows key still opens Start.");
   ShortcutRow("Quick Search","QuickSearch",null);
   ShortcutRow("Capture tool","CaptureTool",null);
   ShortcutRow("App switcher","AppSwitcher",null);
   ShortcutRow("Open Widget Builder","WidgetBuilder",null);
   ShortcutRow("Toggle Hidden Mode","HiddenMode","Hides Awl’s taskbar and top toolbar until pressed again.");
   ShortcutRow("Shut down Awl","QuitAwl",null);
   Setting("Replace native Print Screen",cfg.CapturePrintScreen,v=>{cfg.CapturePrintScreen=v;SaveConfig();},"Print Screen opens Awl Capture instead of Windows Snipping Tool.");
  }));
 }
 void ShortcutRow(string label,string id,string hint){
  var row=new Grid{Margin=new Thickness(3,7,3,7)};row.ColumnDefinitions.Add(new ColumnDefinition());row.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});
  var title=new StackPanel{VerticalAlignment=VerticalAlignment.Center};title.Children.Add(Text(label,13,ink));if(!String.IsNullOrEmpty(hint)){var sub=Text(hint,10,mutedBrush);sub.TextWrapping=TextWrapping.Wrap;title.Children.Add(sub);}row.Children.Add(title);
  var controls=new StackPanel{Orientation=Orientation.Horizontal,VerticalAlignment=VerticalAlignment.Center};Grid.SetColumn(controls,1);row.Children.Add(controls);
  string initial=id=="StartSearchModifier"?cfg.StartSearchModifier+" + Win":KeyBind(id).Replace("+"," + ");
  var button=Action(initial,()=>{});button.MinWidth=126;button.Padding=new Thickness(9,6,9,6);button.Margin=new Thickness(8,0,0,0);button.Content=initial;controls.Children.Add(button);
  var reset=Action("Reset",()=>{try{if(id=="StartSearchModifier"){cfg.StartSearchModifier="Ctrl";SaveConfig();}else SaveKeyBind(id,defaultKeyBinds[id]);button.ToolTip=null;button.Content=id=="StartSearchModifier"?"Ctrl + Win":KeyBind(id).Replace("+"," + ");}catch(ArgumentException error){button.ToolTip=error.Message;button.Content="Shortcut in use";}});reset.Margin=new Thickness(6,0,0,0);controls.Children.Add(reset);
  button.Click+=delegate{recordingWinHeld=false;recordingKeyBind=true;button.Content=id=="StartSearchModifier"?"Press Ctrl / Shift / Alt…":"Press shortcut…";button.Focus();};
  button.LostKeyboardFocus+=delegate{if(recordingKeyBind){recordingKeyBind=false;recordingWinHeld=false;button.Content=id=="StartSearchModifier"?cfg.StartSearchModifier+" + Win":KeyBind(id).Replace("+"," + ");}};
  button.PreviewKeyDown+=delegate(object sender,KeyEventArgs e){
   if(!recordingKeyBind)return;e.Handled=true;Key key=e.Key==Key.System?e.SystemKey:e.Key;
   if(key==Key.Escape&&Keyboard.Modifiers==ModifierKeys.None&&!recordingWinHeld){recordingKeyBind=false;button.Content=id=="StartSearchModifier"?cfg.StartSearchModifier+" + Win":KeyBind(id).Replace("+"," + ");return;}
   if(id=="StartSearchModifier"){
    string modifier=key==Key.LeftCtrl||key==Key.RightCtrl?"Ctrl":key==Key.LeftShift||key==Key.RightShift?"Shift":key==Key.LeftAlt||key==Key.RightAlt?"Alt":null;
    if(modifier==null)return;cfg.StartSearchModifier=modifier;SaveConfig();recordingKeyBind=false;button.Content=modifier+" + Win";return;
   }
   if(key==Key.Back||key==Key.Delete){SaveKeyBind(id,"None");recordingKeyBind=false;button.Content="None";return;}
   if(key==Key.LeftCtrl||key==Key.RightCtrl||key==Key.LeftAlt||key==Key.RightAlt||key==Key.LeftShift||key==Key.RightShift||key==Key.LWin||key==Key.RWin)return;
   int mods=0;var keys=Keyboard.Modifiers;if((keys&ModifierKeys.Control)!=0)mods|=HotCtrl;if((keys&ModifierKeys.Shift)!=0)mods|=HotShift;if((keys&ModifierKeys.Alt)!=0)mods|=HotAlt;
   if(recordingWinHeld)mods|=HotWin;
   string value=(mods&HotCtrl)!=0?"Ctrl+":"";if((mods&HotAlt)!=0)value+="Alt+";if((mods&HotShift)!=0)value+="Shift+";if((mods&HotWin)!=0)value+="Win+";value+=ShortcutName(key);
   try{SaveKeyBind(id,value);recordingKeyBind=false;button.Content=value.Replace("+"," + ");button.ToolTip=null;}catch(ArgumentException error){button.ToolTip=error.Message;button.Content="Try another shortcut";}
  };
  page.Children.Add(row);page.Children.Add(new Border{Height=1,Background=SoftBorder(),Margin=new Thickness(3,2,3,0)});
 }
} }
