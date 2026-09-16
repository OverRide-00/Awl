using System;
using System.Threading;
using System.Windows;

namespace Awl {
 public static class WidgetBuilderProgram {
  [STAThread] public static void Main(string[] args){
   try{Shell.InitializeDataRoot();}catch(Exception startupError){try{System.IO.File.WriteAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(),"WidgetBuilder-startup-error.txt"),startupError.ToString());}catch{}MessageBox.Show(startupError.Message,"Widget Builder could not start");return;}
   bool fresh;using(var gate=new Mutex(true,"Local\\AwlWidgetBuilderInstance",out fresh)){
    if(!fresh)return;
    var shell=new Shell();shell.ShutdownMode=ShutdownMode.OnLastWindowClose;
    shell.Startup+=delegate{shell.StartStandaloneWidgetBuilder();};
    shell.Run();
   }
  }
 }

 partial class Shell {
  bool standaloneWidgetBuilder;
  public void StartStandaloneWidgetBuilder(){standaloneWidgetBuilder=true;LoadConfig();SampleWallpaper();LoadCatalog();ApplyTheme();OpenWidgetBuilder();}
 }
}
