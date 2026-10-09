using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using Microsoft.Win32;

namespace Awl { partial class Shell {
 [DllImport("shell32.dll",CharSet=CharSet.Unicode)] static extern int SHParseDisplayName(string name,IntPtr binding,out IntPtr pidl,uint attributes,out uint result);
 [DllImport("shell32.dll")] static extern int SHOpenFolderAndSelectItems(IntPtr pidl,uint count,IntPtr children,uint flags);

 string AppFileTarget(AppEntry entry){
  string path=Environment.ExpandEnvironmentVariables(entry.Target??"");
  if(!path.EndsWith(".lnk",StringComparison.OrdinalIgnoreCase)||!File.Exists(path))return path;
  object shell=null,link=null;
  try{
   var type=Type.GetTypeFromProgID("WScript.Shell");shell=Activator.CreateInstance(type);
   link=type.InvokeMember("CreateShortcut",BindingFlags.InvokeMethod,null,shell,new object[]{path});
   string target=Convert.ToString(link.GetType().InvokeMember("TargetPath",BindingFlags.GetProperty,null,link,null));
   if(File.Exists(target)||Directory.Exists(target))return target;
  }catch(Exception error){Log("Resolve app location: "+error.Message);}
  finally{if(link!=null)Marshal.FinalReleaseComObject(link);if(shell!=null)Marshal.FinalReleaseComObject(shell);}
  return path; // Advertised/Store shortcuts may not have a filesystem target.
 }
 void OpenAppInExplorer(AppEntry entry){
  IntPtr pidl=IntPtr.Zero;
  try{
   string target=AppFileTarget(entry);uint attributes;
   Marshal.ThrowExceptionForHR(SHParseDisplayName(target,IntPtr.Zero,out pidl,0,out attributes));
   Marshal.ThrowExceptionForHR(SHOpenFolderAndSelectItems(pidl,0,IntPtr.Zero,0));
   if(launcher!=null)launcher.Hide();
  }catch(Exception error){MessageBox.Show(launcher,"Could not open the location of "+entry.Name+".\n"+error.Message,"Open in File Explorer");}
  finally{if(pidl!=IntPtr.Zero)Marshal.FreeCoTaskMem(pidl);}
 }
 static ProcessStartInfo ParseAppUninstaller(string command){
  command=Environment.ExpandEnvironmentVariables(command??"").Trim();
  if(command.Length==0)return null;
  string executable,arguments;
  if(command[0]=='"'){
   int end=command.IndexOf('"',1);if(end<0)return null;
   executable=command.Substring(1,end-1);arguments=command.Substring(end+1).Trim();
  }else{
   // Unquoted paths containing spaces are ambiguous: use Windows Settings instead.
   int end=command.IndexOf(' ');executable=end<0?command:command.Substring(0,end);arguments=end<0?"":command.Substring(end+1);
  }
  if(String.Equals(executable,"msiexec.exe",StringComparison.OrdinalIgnoreCase))executable=Path.Combine(Environment.SystemDirectory,"msiexec.exe");
  if(!Path.IsPathRooted(executable)||!File.Exists(executable)||!executable.EndsWith(".exe",StringComparison.OrdinalIgnoreCase))return null;
  return new ProcessStartInfo(executable,arguments){UseShellExecute=true};
 }
 ProcessStartInfo RegisteredAppUninstaller(AppEntry entry){
  var commands=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
  // Exact registered display names only: no fuzzy matches that could remove another app.
  foreach(var hive in new[]{RegistryHive.CurrentUser,RegistryHive.LocalMachine})foreach(var view in new[]{RegistryView.Registry64,RegistryView.Registry32}){
   try{using(var root=RegistryKey.OpenBaseKey(hive,view))using(var uninstall=root.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall")){
    if(uninstall==null)continue;
    foreach(string name in uninstall.GetSubKeyNames())using(var key=uninstall.OpenSubKey(name)){
     if(key==null||!String.Equals(Convert.ToString(key.GetValue("DisplayName")),entry.Name,StringComparison.OrdinalIgnoreCase))continue;
     if(Convert.ToString(key.GetValue("SystemComponent"))=="1"||Convert.ToString(key.GetValue("NoRemove"))=="1")continue;
     Guid product;
     string command=Convert.ToString(key.GetValue("UninstallString"));
     if(Convert.ToString(key.GetValue("WindowsInstaller"))=="1"&&Guid.TryParse(name,out product))command="\""+Path.Combine(Environment.SystemDirectory,"msiexec.exe")+"\" /x "+product.ToString("B");
     if(!String.IsNullOrWhiteSpace(command))commands.Add(command);
    }
   }}catch(Exception error){Log("Read app uninstaller: "+error.Message);}
  }
  return commands.Count==1?ParseAppUninstaller(commands.First()):null;
 }
 async void UninstallApp(AppEntry entry){
  try{
   string target=entry.Target??"",prefix="shell:AppsFolder\\";
   if(target.StartsWith(prefix,StringComparison.OrdinalIgnoreCase)&&target.Contains("!")){
    string family=target.Substring(prefix.Length).Split('!')[0];
    Launch("ms-settings:appsfeatures-app?"+Uri.EscapeDataString(family));
   }else{
    var start=await System.Threading.Tasks.Task.Run(()=>RegisteredAppUninstaller(entry));
    if(start!=null)Process.Start(start);
    else Launch("ms-settings:appsfeatures");
   }
   if(launcher!=null)launcher.Hide();
  }catch(System.ComponentModel.Win32Exception error){if(error.NativeErrorCode!=1223)MessageBox.Show(launcher,error.Message,"Uninstall "+entry.Name);}
  catch(Exception error){MessageBox.Show(launcher,error.Message,"Uninstall "+entry.Name);}
 }
 public string AppActionsSelfTest(){
  string folder=Path.Combine(Path.GetTempPath(),"Awl-app-actions-"+Guid.NewGuid().ToString("N"));
  object shell=null,link=null;IntPtr pidl=IntPtr.Zero;
  try{
   Directory.CreateDirectory(folder);string target=Path.Combine(folder,"Sample app.exe"),shortcut=Path.Combine(folder,"Sample app.lnk");File.WriteAllText(target,"");
   var type=Type.GetTypeFromProgID("WScript.Shell");shell=Activator.CreateInstance(type);
   link=type.InvokeMember("CreateShortcut",BindingFlags.InvokeMethod,null,shell,new object[]{shortcut});
   link.GetType().InvokeMember("TargetPath",BindingFlags.SetProperty,null,link,new object[]{target});
   link.GetType().InvokeMember("Save",BindingFlags.InvokeMethod,null,link,null);
   if(AppFileTarget(new AppEntry{Target=shortcut})!=target)throw new Exception("Shortcut target resolution");
   uint attributes;Marshal.ThrowExceptionForHR(SHParseDisplayName(target,IntPtr.Zero,out pidl,0,out attributes));
   var command=ParseAppUninstaller("\""+target+"\" /uninstall \"argument with spaces\"");
   if(command==null||command.FileName!=target||command.Arguments!="/uninstall \"argument with spaces\"")throw new Exception("Uninstall argument preservation");
   if(ParseAppUninstaller(target+" /uninstall")!=null||ParseAppUninstaller("cmd.exe /c del anything")!=null||ParseAppUninstaller("")!=null)throw new Exception("Ambiguous command fallback");
   if(ParseAppUninstaller("msiexec.exe /x {00000000-0000-0000-0000-000000000000}")==null)throw new Exception("MSI executable resolution");
   return "App actions: shortcut resolution, Explorer item parsing, uninstall arguments and ambiguous command fallback passed (no uninstaller executed)";
  }catch(Exception error){return "FAILED: App actions: "+error.Message;}
  finally{if(pidl!=IntPtr.Zero)Marshal.FreeCoTaskMem(pidl);if(link!=null)Marshal.FinalReleaseComObject(link);if(shell!=null)Marshal.FinalReleaseComObject(shell);try{Directory.Delete(folder,true);}catch{}}
 }
} }
