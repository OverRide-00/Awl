using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Awl { partial class Shell {
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode)] static extern int GetPackageFamilyName(IntPtr process,ref uint length,StringBuilder value);
 static readonly string[] SharedShellExecutables={"explorer.exe","applicationframehost.exe","runtimebroker.exe","searchhost.exe","startmenuexperiencehost.exe","shellexperiencehost.exe","dwm.exe","awl.exe"};
 string PackageFamily(Process process){
  try{uint length=0;if(GetPackageFamilyName(process.Handle,ref length,null)!=122||length<2||length>512)return null;var value=new StringBuilder((int)length);return GetPackageFamilyName(process.Handle,ref length,value)==0?value.ToString():null;}catch{return null;}
 }
 string ProcessExecutable(Process process){try{return Path.GetFullPath(process.MainModule.FileName);}catch{return null;}}
 bool TaskbarKillTarget(IntPtr window,out string family,out string executable){
  family=null;executable=null;
  string appId=WindowAppModelId(window);if(!String.IsNullOrWhiteSpace(appId)&&appId.Contains("!")){family=appId.Split('!')[0];return true;}
  uint pid;Native.GetWindowThreadProcessId(window,out pid);if(pid==0||pid==selfPid)return false;
  try{using(var process=Process.GetProcessById((int)pid)){executable=ProcessExecutable(process);return executable!=null&&!SharedShellExecutables.Contains(Path.GetFileName(executable).ToLowerInvariant());}}catch{return false;}
 }
 bool CanKillTaskbarApp(IntPtr window){string family,executable;return TaskbarKillTarget(window,out family,out executable);}
 int KillMatchingProcesses(string family,string executable){
  int killed=0;foreach(var process in Process.GetProcesses())using(process){
   try{if(process.Id==selfPid||process.HasExited)continue;
    bool match=family!=null?String.Equals(PackageFamily(process),family,StringComparison.OrdinalIgnoreCase):String.Equals(ProcessExecutable(process),executable,StringComparison.OrdinalIgnoreCase);
    if(!match)continue;process.Kill();killed++;
   }catch(Exception error){Log("Kill app process "+process.Id+": "+error.Message);}
  }return killed;
 }
 async void KillTaskbarApp(IntPtr window){
  string family,executable;if(!TaskbarKillTarget(window,out family,out executable))return;
  try{
   int count=await Task.Run(()=>KillMatchingProcesses(family,executable));
   if(count==0)Log("Kill app: no matching process was available.");
   signature="";if(apps!=null)RefreshApps();
  }catch(Exception error){MessageBox.Show(error.Message,"Could not kill app");}
 }
} }
