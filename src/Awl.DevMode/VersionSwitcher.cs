using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Awl { partial class Shell {
 Window versionSwitcherWindow;
 bool versionSwitchBusy;
 string VersionCacheRoot {get{return Path.Combine(DevLibraryRoot,"VersionCache");}}

 static string SafeVersionTag(string tag){return Regex.IsMatch(tag??"",@"^v[0-9]+\.[0-9]+\.[0-9]+(?:-(?:alpha|beta))?$",RegexOptions.IgnoreCase)?tag:null;}
 static string ReleaseAsset(GithubRelease release,string name){var asset=(release.assets??new List<GithubAsset>()).FirstOrDefault(x=>String.Equals(x.name,name,StringComparison.OrdinalIgnoreCase));return asset==null?null:asset.browser_download_url;}
 static string Sha256File(string path){using(var sha=SHA256.Create())using(var stream=File.OpenRead(path))return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").ToLowerInvariant();}
 static string ChecksumValue(string content){var value=(content??"").Split(new[]{' ','\t','\r','\n'},StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();return value!=null&&Regex.IsMatch(value,"^[0-9a-fA-F]{64}$")?value.ToLowerInvariant():null;}
 static bool VerifiedBinary(string binary,string checksum){return File.Exists(binary)&&File.Exists(checksum)&&String.Equals(Sha256File(binary),ChecksumValue(File.ReadAllText(checksum)),StringComparison.OrdinalIgnoreCase);}

 async Task<List<GithubRelease>> VersionReleases(){
  ServicePointManager.SecurityProtocol|=SecurityProtocolType.Tls12;
  var found=new Dictionary<string,GithubRelease>(StringComparer.OrdinalIgnoreCase);
  using(var client=new HttpClient()){client.Timeout=TimeSpan.FromSeconds(20);client.DefaultRequestHeaders.UserAgent.ParseAdd("Awl-DevMode/"+BuildInfo.Version);
   for(int pageNumber=1;pageNumber<=20;pageNumber++){
    string url="https://api.github.com/repos/"+BuildInfo.Repository+"/releases?per_page=100&page="+pageNumber;
    var batch=new JavaScriptSerializer().Deserialize<List<GithubRelease>>(await client.GetStringAsync(url))??new List<GithubRelease>();
    foreach(var release in batch)if(!release.draft&&SafeVersionTag(release.tag_name)!=null&&ReleaseAsset(release,"Awl.exe")!=null)found[release.tag_name]=release;
    if(batch.Count<100)break;
   }
  }
  return found.Values.OrderByDescending(ReleaseVersion).ThenBy(x=>x.prerelease).ToList();
 }
 List<GithubRelease> LocalVersionReleases(){
  var found=new List<GithubRelease>();string root=Path.Combine(DevProjectRoot(),"releases");if(!Directory.Exists(root))return found;
  foreach(string folder in Directory.GetDirectories(root)){string tag=Path.GetFileName(folder),binary=Path.Combine(folder,"Awl.exe"),checksum=Path.Combine(folder,"Awl.exe.sha256");if(SafeVersionTag(tag)==null||!VerifiedBinary(binary,checksum))continue;
   string notes=Path.Combine(folder,"index.html");found.Add(new GithubRelease{tag_name=tag,name="Awl "+tag.Substring(1),body=File.Exists(notes)?StripHtml(File.ReadAllText(notes)):"Local release package",assets=new List<GithubAsset>{new GithubAsset{name="Awl.exe",browser_download_url=new Uri(binary).AbsoluteUri},new GithubAsset{name="Awl.exe.sha256",browser_download_url=new Uri(checksum).AbsoluteUri}}});
  }
  return found;
 }
 async Task<string> VersionBinary(GithubRelease release,Action<int> progress){
  ServicePointManager.SecurityProtocol|=SecurityProtocolType.Tls12;
  string tag=SafeVersionTag(release.tag_name);if(tag==null)throw new InvalidDataException("Invalid release tag.");
  string local=Path.Combine(DevProjectRoot(),"releases",tag);string localExe=Path.Combine(local,"Awl.exe"),localHash=Path.Combine(local,"Awl.exe.sha256");
  if(VerifiedBinary(localExe,localHash)){progress(100);return localExe;}
  string folder=Path.Combine(VersionCacheRoot,tag),exe=Path.Combine(folder,"Awl.exe"),shaFile=Path.Combine(folder,"Awl.exe.sha256");
  if(VerifiedBinary(exe,shaFile)){progress(100);return exe;}
  string binaryUrl=ReleaseAsset(release,"Awl.exe"),hashUrl=ReleaseAsset(release,"Awl.exe.sha256");
  if(String.IsNullOrWhiteSpace(binaryUrl)||String.IsNullOrWhiteSpace(hashUrl))throw new InvalidDataException("This release needs both Awl.exe and its SHA-256 checksum.");
  if(!binaryUrl.StartsWith("https://github.com/"+BuildInfo.Repository+"/releases/download/",StringComparison.OrdinalIgnoreCase)||!hashUrl.StartsWith("https://github.com/"+BuildInfo.Repository+"/releases/download/",StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Unexpected release download address.");
  Directory.CreateDirectory(folder);string staged=exe+".downloading";
  try{using(var client=new HttpClient()){client.Timeout=TimeSpan.FromMinutes(5);client.DefaultRequestHeaders.UserAgent.ParseAdd("Awl-DevMode/"+BuildInfo.Version);
    string expected=ChecksumValue(await client.GetStringAsync(hashUrl));if(expected==null)throw new InvalidDataException("Invalid release checksum.");
    using(var response=await client.GetAsync(binaryUrl,HttpCompletionOption.ResponseHeadersRead)){response.EnsureSuccessStatusCode();long total=response.Content.Headers.ContentLength??0,received=0;using(var input=await response.Content.ReadAsStreamAsync())using(var output=File.Create(staged)){var buffer=new byte[131072];int read;while((read=await input.ReadAsync(buffer,0,buffer.Length))>0){await output.WriteAsync(buffer,0,read);received+=read;if(total>0)progress((int)Math.Min(99,received*100/total));}}}
    if(!String.Equals(Sha256File(staged),expected,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("The downloaded release failed SHA-256 verification.");
    if(File.Exists(exe))File.Delete(exe);File.Move(staged,exe);File.WriteAllText(shaFile,expected+"  Awl.exe");progress(100);return exe;
   }}finally{if(File.Exists(staged))File.Delete(staged);}
 }

 void ShowVersionSwitcher(){
  if(versionSwitcherWindow!=null){versionSwitcherWindow.Activate();return;}
  // The launcher hides on blur and its desktop click watcher treats a separate
  // window as outside. Keep the owner visible for the lifetime of this dialog.
  bool priorMenuOpen=appMenuOpen;appMenuOpen=true;
  var dialog=new Window{Title="Awl versions",Owner=launcher,Width=910,Height=610,MinWidth=760,MinHeight=480,WindowStartupLocation=WindowStartupLocation.CenterOwner,WindowStyle=WindowStyle.None,ResizeMode=ResizeMode.CanResize,AllowsTransparency=true,Background=Brushes.Transparent,ShowInTaskbar=false};versionSwitcherWindow=dialog;
  dialog.Closed+=delegate{versionSwitcherWindow=null;appMenuOpen=priorMenuOpen;};dialog.KeyDown+=delegate(object sender,System.Windows.Input.KeyEventArgs e){if(e.Key==System.Windows.Input.Key.Escape)dialog.Close();};
  var layout=new Grid();layout.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});layout.RowDefinitions.Add(new RowDefinition());layout.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(245)});layout.ColumnDefinitions.Add(new ColumnDefinition());
  var header=new DockPanel{Margin=new Thickness(20,16,18,14)};header.MouseLeftButtonDown+=delegate{try{dialog.DragMove();}catch{}};Grid.SetColumnSpan(header,2);var close=Action("",()=>dialog.Close());close.Content=BuilderSvgIcon("close",14,ink);close.Width=30;close.Height=30;close.Padding=new Thickness(0);DockPanel.SetDock(close,Dock.Right);header.Children.Add(close);header.Children.Add(BuilderIconLabel("history","Version switcher",ink,18));layout.Children.Add(header);
  var versions=new StackPanel{Margin=new Thickness(13,10,13,13)};var left=new ScrollViewer{Content=versions,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled};var leftBorder=new Border{Background=new SolidColorBrush(Mix(baseColor,Colors.Black,.12)),BorderBrush=SoftBorder(),BorderThickness=new Thickness(0,1,1,0),Child=left};Grid.SetRow(leftBorder,1);layout.Children.Add(leftBorder);
  var detail=new StackPanel{Margin=new Thickness(22,18,22,18)};var right=new ScrollViewer{Content=detail,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled};Grid.SetRow(right,1);Grid.SetColumn(right,1);layout.Children.Add(right);
  dialog.Content=new Border{Background=new SolidColorBrush(baseColor),BorderBrush=SoftBorder(),BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(18),ClipToBounds=true,Effect=ShellShadow(),Child=layout};
  dialog.Show();LoadVersionSwitcher(versions,detail);
 }
 async void LoadVersionSwitcher(StackPanel versions,StackPanel detail){
  versions.Children.Clear();versions.Children.Add(Text("LOCAL",10,mutedBrush));
  var current=Action("Current development",()=>ShowVersionDetail(null,detail));current.HorizontalContentAlignment=HorizontalAlignment.Left;current.Margin=new Thickness(0,7,0,8);versions.Children.Add(current);ShowVersionDetail(null,detail);
  var loading=Text("Loading releases…",11,mutedBrush);loading.Margin=new Thickness(0,14,0,0);versions.Children.Add(loading);
  List<GithubRelease> releases=null;string error=null;
  try{releases=await VersionReleases();}catch(Exception ex){error=ex.Message;Log("Version catalog: "+ex);releases=new List<GithubRelease>();}
  foreach(var local in LocalVersionReleases())if(releases.All(x=>!String.Equals(x.tag_name,local.tag_name,StringComparison.OrdinalIgnoreCase)))releases.Add(local);
  releases=releases.OrderByDescending(ReleaseVersion).ThenBy(x=>x.prerelease).ToList();versions.Children.Remove(loading);versions.Children.Add(Text("PUBLISHED",10,mutedBrush));
  foreach(var item in releases){var release=item;var button=Action("",()=>ShowVersionDetail(release,detail));button.Content=BuilderIconLabel("history",release.tag_name+(release.prerelease?" · Preview":""),ink,13);button.HorizontalContentAlignment=HorizontalAlignment.Left;button.Margin=new Thickness(0,5,0,0);versions.Children.Add(button);}
  if(releases.Count==0)versions.Children.Add(Text(error==null?"No releases found.":"Could not load releases: "+error,11,mutedBrush));
  else if(error!=null){var notice=Text("Offline · showing downloaded releases",10,mutedBrush);notice.ToolTip=error;versions.Children.Add(notice);}
 }
 void ShowVersionDetail(GithubRelease release,StackPanel detail){
  detail.Children.Clear();bool current=release==null;string title=current?"Current development":release.tag_name;
  var heading=Text(title,23,ink);heading.FontWeight=FontWeights.SemiBold;detail.Children.Add(heading);
  var subtitle=Text(current?"The local Awl source you are editing and testing now.":(release.prerelease?"Preview release":"Published release")+" · "+(release.name??"Awl"),11,mutedBrush);subtitle.Margin=new Thickness(0,5,0,16);detail.Children.Add(subtitle);
  var notes=Text(current?"Dev Mode is running the current development build. Session settings reset when it closes; your Dev Mode plugin and template library stays available.":ReleaseNotes(release),12,mutedBrush);notes.TextWrapping=TextWrapping.Wrap;notes.Margin=new Thickness(0,0,0,14);detail.Children.Add(notes);
  if(current){var source=Text("Local project: "+DevProjectRoot(),11,mutedBrush);source.TextWrapping=TextWrapping.Wrap;detail.Children.Add(source);return;}
  var status=Text("Ready to test in an isolated session.",11,mutedBrush);status.Margin=new Thickness(0,0,0,10);detail.Children.Add(status);
  var actions=new WrapPanel{Margin=new Thickness(0,0,0,15)};Button test=null;test=Action("Test this version",()=>TestVersion(release,status,test));actions.Children.Add(test);if(!String.IsNullOrWhiteSpace(release.html_url))actions.Children.Add(Action("Release page",()=>Launch(release.html_url)));detail.Children.Add(actions);
  var changed=Text("Comparing with local development…",12,mutedBrush);changed.TextWrapping=TextWrapping.Wrap;detail.Children.Add(BuilderIconLabel("more","Changes from this release",ink,14));detail.Children.Add(changed);
  var diffButton=Action("View code diff",()=>OpenVersionDiff(release));diffButton.Margin=new Thickness(0,12,0,0);detail.Children.Add(diffButton);LoadVersionChanges(release,changed);
 }
 string ReleaseNotes(GithubRelease release){string file=Path.Combine(DevProjectRoot(),"releases",release.tag_name,"index.html");if(File.Exists(file)){string html=File.ReadAllText(file);var main=Regex.Match(html,@"<main\b[^>]*>(.*?)</main>",RegexOptions.IgnoreCase|RegexOptions.Singleline);if(main.Success)html=main.Groups[1].Value;html=Regex.Replace(html,@"<li\b[^>]*>","\n• ",RegexOptions.IgnoreCase);html=Regex.Replace(html,@"</(?:p|h1|h2|ul)>","\n",RegexOptions.IgnoreCase);html=System.Net.WebUtility.HtmlDecode(Regex.Replace(html,"<[^>]+>"," "));return String.Join("\n",html.Split(new[]{'\n'},StringSplitOptions.RemoveEmptyEntries).Select(x=>Regex.Replace(x,@"\s+"," ").Trim()).Where(x=>x.Length>0));}string body=StripHtml(release.body);return String.IsNullOrWhiteSpace(body)?"Release notes are unavailable for this version.":body;}
 static string GitResult(string folder,string arguments){var start=new ProcessStartInfo("git",arguments){WorkingDirectory=folder,UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};using(var process=Process.Start(start)){string output=process.StandardOutput.ReadToEnd(),error=process.StandardError.ReadToEnd();process.WaitForExit();if(process.ExitCode!=0)throw new InvalidOperationException(error.Trim());return output.Trim();}}
 string VersionDiff(string tag,bool full){if(SafeVersionTag(tag)==null)throw new InvalidDataException("Invalid release tag.");string folder=DevProjectRoot();try{GitResult(folder,"rev-parse --verify refs/tags/"+tag);}catch{GitResult(folder,"fetch --no-tags origin tag "+tag);}
  string result=GitResult(folder,"-c color.ui=false diff --no-ext-diff "+(full?"--unified=3":"--name-status")+" refs/tags/"+tag+" --");
  string untracked=GitResult(folder,"status --short --untracked-files=normal");var extras=untracked.Split(new[]{'\r','\n'},StringSplitOptions.RemoveEmptyEntries).Where(x=>x.StartsWith("?? ")).ToArray();
  if(extras.Length>0)result+=(result.Length>0?Environment.NewLine:"")+"Untracked in development:"+Environment.NewLine+String.Join(Environment.NewLine,extras);
  return String.IsNullOrWhiteSpace(result)?"No local source differences from this release.":result;
 }
 async void LoadVersionChanges(GithubRelease release,TextBlock label){try{string result=await Task.Run(()=>VersionDiff(release.tag_name,false));if(label.IsLoaded)label.Text=result.Length>2500?result.Substring(0,2500)+"\n…":result;}catch(Exception error){if(label.IsLoaded)label.Text="Comparison unavailable: "+error.Message;}}
 async void OpenVersionDiff(GithubRelease release){var window=new Window{Title="Awl changes since "+release.tag_name,Owner=versionSwitcherWindow,Width=930,Height=640,WindowStartupLocation=WindowStartupLocation.CenterOwner,Background=panelBrush,Foreground=ink};var output=new TextBox{Text="Loading diff…",IsReadOnly=true,TextWrapping=TextWrapping.NoWrap,AcceptsReturn=true,FontFamily=new FontFamily("Consolas"),FontSize=12,Foreground=ink,Background=panelBrush,BorderThickness=new Thickness(0),VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Auto,Padding=new Thickness(14)};window.Content=output;window.Show();try{string diff=await Task.Run(()=>VersionDiff(release.tag_name,true));output.Text=diff.Length>1000000?diff.Substring(0,1000000)+"\n…diff truncated at 1 MB":diff;}catch(Exception error){output.Text="Comparison unavailable: "+error.Message;}}
  async void TestVersion(GithubRelease release,TextBlock status,Button button){
   if(versionSwitchBusy)return;versionSwitchBusy=true;button.IsEnabled=false;string relaunch=null;bool handedOff=false;
  try{status.Text="Preparing "+release.tag_name+"…";string binary=await VersionBinary(release,value=>{status.Text=value<100?"Downloading "+release.tag_name+" · "+value+"%":"Release verified · SHA-256 passed";});
    AssemblyName.GetAssemblyName(binary);var running=Process.GetProcessesByName("Awl").Where(x=>x.Id!=selfPid).ToArray();if(running.Length>0){try{relaunch=running.First().MainModule.FileName;}catch{}status.Text="Closing the running Awl session…";try{EventWaitHandle.OpenExisting("Local\\AwlStop").Set();}catch{}for(int count=0;count<50&&Process.GetProcessesByName("Awl").Any(x=>x.Id!=selfPid);count++)await Task.Delay(100);if(Process.GetProcessesByName("Awl").Any(x=>x.Id!=selfPid))throw new InvalidOperationException("Close the running Awl app before testing another version.");}
   string self=Process.GetCurrentProcess().MainModule.FileName;string args="--preview-release "+DevArg(binary)+" "+selfPid+" "+DevArg(relaunch??"-");var child=Process.Start(new ProcessStartInfo(self,args){UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden});if(child==null)throw new InvalidOperationException("The version tester did not start.");handedOff=true;if(versionSwitcherWindow!=null)versionSwitcherWindow.Close();Quit();
  }catch(Exception error){status.Text="Could not test version: "+error.Message;button.IsEnabled=true;if(!handedOff&&!String.IsNullOrWhiteSpace(relaunch)&&File.Exists(relaunch))try{Process.Start(new ProcessStartInfo(relaunch,"--background"){UseShellExecute=true});}catch{}}
  finally{versionSwitchBusy=false;}
 }
 public static void RunVersionPreview(string binary,string parentId,string relaunch){
  bool resumeDev=true;try{int pid;if(!Int32.TryParse(parentId,out pid)||pid<=0)throw new ArgumentException("Invalid parent process.");try{using(var parent=Process.GetProcessById(pid))if(!parent.WaitForExit(30000)){resumeDev=false;throw new TimeoutException("Dev Mode did not close before the preview started.");}}catch(ArgumentException){}
   if(!File.Exists(binary))throw new FileNotFoundException("The selected release is missing.",binary);string sandbox=Path.Combine(Path.GetTempPath(),"Awl-VersionPreview-"+Process.GetCurrentProcess().Id);
   try{Directory.CreateDirectory(sandbox);var assembly=Assembly.LoadFrom(binary);var type=assembly.GetType("Awl.Shell",true);var flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static;var library=type.GetField("devLibraryRoot",flags);if(library!=null)library.SetValue(null,Path.Combine(sandbox,"Library"));var run=type.GetMethod("RunDevMode",flags);if(run==null)throw new MissingMethodException("This Awl release does not support isolated Dev Mode testing.");run.Invoke(null,new object[]{false});}
   finally{try{if(Directory.Exists(sandbox))Directory.Delete(sandbox,true);}catch{}}
  }catch(Exception error){try{File.WriteAllText(Path.Combine(Path.GetTempPath(),"Awl-version-preview-error.txt"),error.ToString());}catch{}MessageBox.Show(error.GetBaseException().Message,"Awl version test could not start");}
  finally{if(resumeDev){try{if(!String.IsNullOrWhiteSpace(relaunch)&&File.Exists(relaunch))Process.Start(new ProcessStartInfo(relaunch,"--background"){UseShellExecute=true});}catch{}try{Process.Start(new ProcessStartInfo(Process.GetCurrentProcess().MainModule.FileName){UseShellExecute=true});}catch{}}}
 }
} }
