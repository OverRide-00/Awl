using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Microsoft.Win32;

namespace Awl {
 public static class DevModeProgram {
  [STAThread] public static void Main(string[] args){if(args.Contains("--dev-self-test")){Environment.ExitCode=Shell.DevIsolationSelfTest()?0:1;return;}Shell.RunDevMode(args.Contains("--dev-smoke-test"));}
 }

 partial class Shell {
  static bool devMode;
  static string devSessionRoot="";
  static string devOriginalWallpaper="";
  static bool devWallpaperEngineWasRunning;
  string devBuildStatus="Ready to build.";
  bool devBuildRunning;

  public static bool DevIsolationSelfTest(){
   string testRoot=Path.Combine(Path.GetTempPath(),"Awl-DevMode-Test-"+Guid.NewGuid().ToString("N"));try{devMode=true;devSessionRoot=testRoot;Root=testRoot;State=Path.Combine(Root,"original-taskbar-state.txt");InitializeDataRoot();bool valid=File.Exists(Path.Combine(Root,"config.json"))&&File.Exists(Path.Combine(Root,"desktop-widgets.json"))&&File.Exists(Path.Combine(Root,"widget-builder.json"));File.WriteAllText(Path.Combine(Root,"session-only.txt"),"temporary");return valid;}catch{return false;}finally{try{if(Directory.Exists(testRoot))Directory.Delete(testRoot,true);}catch{}}
  }

  [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern bool SystemParametersInfoDev(uint action,uint parameter,string value,uint flags);
  public static void RunDevMode(bool smokeTest=false){
   devMode=true;devSessionRoot=Path.Combine(Path.GetTempPath(),"Awl-DevMode-"+Process.GetCurrentProcess().Id);Root=devSessionRoot;State=Path.Combine(Root,"original-taskbar-state.txt");
   Shell shell=null;try{using(var key=Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Control Panel\\Desktop"))devOriginalWallpaper=Convert.ToString(key.GetValue("WallPaper"));devWallpaperEngineWasRunning=Process.GetProcesses().Any(x=>x.ProcessName.IndexOf("wallpaper",StringComparison.OrdinalIgnoreCase)>=0);}catch{}
   try{InitializeDataRoot();stop=new EventWaitHandle(false,EventResetMode.AutoReset,"Local\\AwlDevStop-"+selfPid);widgetReload=new EventWaitHandle(false,EventResetMode.AutoReset,"Local\\AwlDevWidgetReload-"+selfPid);showLauncher=new EventWaitHandle(false,EventResetMode.AutoReset,"Local\\AwlDevShow-"+selfPid);shell=new Shell();shell.ShutdownMode=ShutdownMode.OnExplicitShutdown;shell.DispatcherUnhandledException+=delegate(object sender,System.Windows.Threading.DispatcherUnhandledExceptionEventArgs error){Log("Dev UI: "+error.Exception);error.Handled=true;};shell.Startup+=delegate{openLauncher=true;if(smokeTest)shell.activeTab="Developer";shell.Init();if(smokeTest){var smokeShutdown=Task.Run(async()=>{await Task.Delay(2500);var shutdownDispatch=shell.Dispatcher.BeginInvoke(new Action(shell.Quit));});}};shell.Exit+=delegate{Restore();};shell.Run();}
   catch(Exception error){try{File.WriteAllText(Path.Combine(Path.GetTempPath(),"Awl-dev-startup-error.txt"),error.ToString());}catch{}MessageBox.Show(error.Message,"Awl Dev Mode could not start");}
   finally{Restore();try{if(!String.IsNullOrWhiteSpace(devOriginalWallpaper)&&File.Exists(devOriginalWallpaper))SystemParametersInfoDev(20,0,devOriginalWallpaper,3);if(devWallpaperEngineWasRunning&&shell!=null)shell.WallpaperEngineControl("play");}catch{}for(int attempt=0;attempt<4;attempt++){try{if(Directory.Exists(devSessionRoot))Directory.Delete(devSessionRoot,true);break;}catch{Thread.Sleep(250);}}}
  }

  string DevProjectRoot(){
   foreach(string candidate in new[]{Path.GetFullPath(Path.Combine(InstallRoot,"..")),Environment.CurrentDirectory})if(File.Exists(Path.Combine(candidate,"scripts","Build.ps1")))return candidate;
   return Path.GetFullPath(Path.Combine(InstallRoot,".."));
  }

  async void BuildAwlFromDev(TextBlock status,Button button){
   if(devBuildRunning)return;devBuildRunning=true;button.IsEnabled=false;status.Text="Building standalone Awl…";SaveConfig();
   try{string project=DevProjectRoot(),script=Path.Combine(project,"scripts","Build.ps1"),inbuilt=Path.Combine(Root,"Templates","Inbuilt Templates");if(!File.Exists(script))throw new FileNotFoundException("Build script was not found.",script);string args="-NoProfile -ExecutionPolicy Bypass -File \""+script+"\" -ProfileRoot \""+Root+"\" -InbuiltRoot \""+inbuilt+"\" -AwlOnly";var process=Process.Start(new ProcessStartInfo("powershell.exe",args){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true});string output=await Task.Run(()=>{string text=process.StandardOutput.ReadToEnd();string errors=process.StandardError.ReadToEnd();process.WaitForExit();if(process.ExitCode!=0)throw new Exception(String.IsNullOrWhiteSpace(errors)?text:errors);return text;});devBuildStatus="Build complete · "+Path.Combine(project,"dist","Awl.exe");status.Text=devBuildStatus;}
   catch(Exception error){devBuildStatus="Build failed · "+error.Message;status.Text=devBuildStatus;}
   finally{devBuildRunning=false;button.IsEnabled=true;}
  }

  string DevArg(string value){return "\""+(value??"").Replace("\"","\\\"")+"\"";}
  async void PublishUpdateFromDev(string channel,string version,string whatsNew,TextBlock status,Button button,Window dialog){
   if(devBuildRunning)return;devBuildRunning=true;button.IsEnabled=false;status.Text="Building and publishing Awl "+version+"…";SaveConfig();
   try{string project=DevProjectRoot(),script=Path.Combine(project,"scripts","Publish-Update.ps1"),inbuilt=Path.Combine(Root,"Templates","Inbuilt Templates");if(!File.Exists(script))throw new FileNotFoundException("Publish script was not found.",script);string args="-NoProfile -ExecutionPolicy Bypass -File "+DevArg(script)+" -Channel "+DevArg(channel)+" -Version "+DevArg(version)+" -WhatsNew "+DevArg(whatsNew)+" -ProfileRoot "+DevArg(Root)+" -InbuiltRoot "+DevArg(inbuilt);var process=Process.Start(new ProcessStartInfo("powershell.exe",args){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true});string output=await Task.Run(()=>{string text=process.StandardOutput.ReadToEnd();string errors=process.StandardError.ReadToEnd();process.WaitForExit();if(process.ExitCode!=0)throw new Exception(String.IsNullOrWhiteSpace(errors)?text:errors);return text;});devBuildStatus="Published · "+output.Trim();status.Text=devBuildStatus;}
   catch(Exception error){devBuildStatus="Publish failed · "+error.Message;status.Text=devBuildStatus;}
   finally{devBuildRunning=false;button.IsEnabled=true;}
  }
  void ShowPublishUpdateDialog(){
   var dialog=new Window{Title="Publish Awl update",Owner=launcher,Width=500,Height=430,WindowStartupLocation=WindowStartupLocation.CenterOwner,ResizeMode=ResizeMode.NoResize,WindowStyle=WindowStyle.None,AllowsTransparency=true,Background=Brushes.Transparent,ShowInTaskbar=false};string channel="Release",whatsNew="";var host=new StackPanel{Margin=new Thickness(20)};var head=new DockPanel();var close=Action("×",()=>dialog.Close());DockPanel.SetDock(close,Dock.Right);head.Children.Add(close);head.Children.Add(Text("Publish update",20));host.Children.Add(head);var explanation=Text("Build a standalone Awl.exe and publish a new versioned GitHub Release. Older releases remain available.",12,mutedBrush);explanation.TextWrapping=TextWrapping.Wrap;explanation.Margin=new Thickness(0,8,0,15);host.Children.Add(explanation);host.Children.Add(Text("Channel",12,mutedBrush));var choices=new UniformGrid{Columns=3,Margin=new Thickness(0,7,0,14)};var channelButtons=new System.Collections.Generic.List<Button>();foreach(string value in new[]{"Release","Beta","Alpha"}){string selected=value;Button option=null;option=Action(value,()=>{channel=selected;foreach(var candidate in channelButtons){bool active=Convert.ToString(candidate.Tag)==channel;candidate.Background=active?accentBrush:cardBrush;candidate.Foreground=active?new SolidColorBrush(Color.FromRgb(16,37,28)):ink;}});option.Tag=value;if(value==channel){option.Background=accentBrush;option.Foreground=new SolidColorBrush(Color.FromRgb(16,37,28));}channelButtons.Add(option);choices.Children.Add(option);}host.Children.Add(choices);host.Children.Add(Text("Version",12,mutedBrush));var version=new TextBox{Text=BuildInfo.Version,Background=cardBrush,Foreground=ink,CaretBrush=ink,BorderBrush=panelBrush,BorderThickness=new Thickness(1),Padding=new Thickness(12,9,12,9),Margin=new Thickness(0,7,0,14),FontSize=14,ToolTip="major.minor.patch — for example 1.2.0"};host.Children.Add(version);host.Children.Add(Text("What's New HTML",12,mutedBrush));var fileRow=new DockPanel{Margin=new Thickness(0,7,0,10)};var fileLabel=Text("No file selected",12,mutedBrush);var choose=Action("Choose index.html",()=>{var picker=new OpenFileDialog{Title="Attach What's New HTML",Filter="HTML files|*.html;*.htm"};if(picker.ShowDialog(dialog)==true){whatsNew=picker.FileName;fileLabel.Text=Path.GetFileName(whatsNew);}});DockPanel.SetDock(choose,Dock.Right);fileRow.Children.Add(choose);fileLabel.VerticalAlignment=VerticalAlignment.Center;fileRow.Children.Add(fileLabel);host.Children.Add(fileRow);var status=Text("Ready to publish",11,mutedBrush);status.TextWrapping=TextWrapping.Wrap;status.Margin=new Thickness(0,4,0,10);host.Children.Add(status);Button publish=null;publish=Action("Build and push update",()=>{Version parsed;if(!Version.TryParse(version.Text.Trim(),out parsed)||parsed.ToString(3)!=version.Text.Trim()){status.Text="Enter a version like 1.2.0.";return;}if(!File.Exists(whatsNew)){status.Text="Choose an index.html file for What's New.";return;}PublishUpdateFromDev(channel,version.Text.Trim(),whatsNew,status,publish,dialog);});publish.HorizontalAlignment=HorizontalAlignment.Right;host.Children.Add(publish);dialog.Content=new Border{Background=SurfaceBrush(baseColor,.98,cfg.StartMenuGlass??cfg.GlassEffect),BorderBrush=accentBrush,BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(20),Effect=ShellShadow(),Child=host};dialog.Show();
  }

  void DeveloperPage(){
   Header("Developer");
   page.Children.Add(Card("Temporary session",()=>{page.Children.Add(Text("Changes in Dev Mode are stored only for this process. Closing Dev Mode deletes its config, pins, location, widgets, and temporary template state.",12,mutedBrush));var root=Text(Root,11,mutedBrush);root.Margin=new Thickness(0,10,0,0);page.Children.Add(root);}));
   page.Children.Add(Card("Build",()=>{page.Children.Add(Text("Builds one standalone Awl.exe using the settings, widgets, wallpaper configuration, and in-built templates in this Dev Mode session.",12,mutedBrush));var status=Text(devBuildStatus,11,mutedBrush);status.Margin=new Thickness(0,10,0,8);page.Children.Add(status);var buttons=new WrapPanel();Button build=null;build=Action("Build standalone Awl",()=>BuildAwlFromDev(status,build));buttons.Children.Add(build);buttons.Children.Add(Action("Push update to GitHub",ShowPublishUpdateDialog));buttons.Children.Add(Action("Open output folder",()=>Launch(Path.Combine(DevProjectRoot(),"dist"))));page.Children.Add(buttons);}));
   page.Children.Add(Card("Template tools",()=>{page.Children.Add(Text("In-built templates created here are embedded when you press Build.",12,mutedBrush));page.Children.Add(Action("Save current setup as in-built",BeginSaveInbuiltTemplate));}));
   page.Children.Add(Card("Developer actions",()=>{var buttons=new WrapPanel();buttons.Children.Add(Action("Open project folder",()=>Launch(DevProjectRoot())));buttons.Children.Add(Action("Exit Dev Mode",Quit));page.Children.Add(buttons);}));
  }
 }
}
