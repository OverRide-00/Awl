using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Awl { partial class Shell {
 string CapturePreviewFrame(string video){
  string runtime=CaptureRuntime(),preview=Path.Combine(Path.GetTempPath(),"Awl-preview-"+Guid.NewGuid().ToString("N")+".png");
  if(String.IsNullOrEmpty(runtime))return null;
  try{foreach(string seek in new[]{"0.5","0"}){
   using(var process=Process.Start(new ProcessStartInfo(runtime,"-hide_banner -loglevel error -y -ss "+seek+" -i \""+video+"\" -frames:v 1 -update 1 \""+preview+"\""){UseShellExecute=false,CreateNoWindow=true,RedirectStandardError=true})){
    process.StandardError.ReadToEnd();if(!process.WaitForExit(8000)){process.Kill();break;}
    if(process.ExitCode==0&&File.Exists(preview)&&new FileInfo(preview).Length>0)return preview;
   }
  }}catch(Exception error){Log("Capture thumbnail: "+error.Message);}
  try{File.Delete(preview);}catch{}
  return null;
 }
 void LoadCapturePreview(string video,Window review,Image thumbnail){
  Task.Run(()=>CapturePreviewFrame(video)).ContinueWith(result=>Dispatcher.BeginInvoke(new Action(()=>{
   string file=result.Status==TaskStatus.RanToCompletion?result.Result:null;
   try{if(file!=null&&captureReviewWindow==review)thumbnail.Source=FileImage(file,960);}
   finally{if(file!=null)try{File.Delete(file);}catch{}}
  })));
 }
 void SendCaptureToQuickTool(string path,string mode){
  captureReviewKeep=true;
  if(captureReviewWindow!=null)captureReviewWindow.Close();
  if(captureWindow!=null)captureWindow.Hide();
  ShowQuickSearch();SetQuickMode(mode);SelectQuickFile(path);
 }
} }
