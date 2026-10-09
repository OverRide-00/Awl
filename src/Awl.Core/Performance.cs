using System;
using System.Threading.Tasks;

namespace Awl { partial class Shell {
 bool replayTransition,replayExporting;
 DateTime replayStartedUtc, nextWindowStylingUtc;
 async void SetReplayBuffer(bool enabled){
  if(replayTransition||replayExporting)return;
  cfg.CaptureReplayEnabled=enabled;SaveConfig();
  if(enabled)StartRollingCapture();else await StopReplayBuffer();
  if(captureWindow!=null&&captureWindow.IsVisible)DrawCaptureStart(true);
 }
 async Task StopReplayBuffer(){
  if(replayTransition)return;
  var recorder=rollingRecorder;if(recorder==null)return;
  replayTransition=true;rollingRecorder=null;
  try{await Task.Run(()=>recorder.Stop());}finally{replayTransition=false;}
 }
 async Task BackgroundMaintenance(){
  if(closing)return;
  if(cfg.CaptureReplayEnabled)StartRollingCapture();
  else if(rollingRecorder!=null&&!replayExporting)await StopReplayBuffer();
  if(DateTime.UtcNow>=nextWindowStylingUtc){nextWindowStylingUtc=DateTime.UtcNow.AddSeconds(10);ApplyWindowCustomizations();}
 }
 public string PerformanceSelfTest(){
  if(new Preferences().CaptureReplayEnabled)return "FAILED: replay must be opt-in";
  bool enabled=cfg.CaptureReplayEnabled,wasClosing=closing;
  try{
   cfg.CaptureReplayEnabled=false;StartRollingCapture();
   if(rollingRecorder!=null)return "FAILED: disabled replay started a recorder";
   cfg.CaptureReplayEnabled=true;closing=true;StartRollingCapture();
   if(rollingRecorder!=null)return "FAILED: replay restarted during shutdown";
   closing=false;replayExporting=true;StartRollingCapture();
   if(rollingRecorder!=null)return "FAILED: replay started while exporting";
   replayExporting=false;replayTransition=true;StartRollingCapture();
   if(rollingRecorder!=null)return "FAILED: replay overlapped stopping recorder";
   return "Replay is opt-in; disabled, shutdown, export and stop guards prevent background capture";
  }finally{cfg.CaptureReplayEnabled=enabled;closing=wasClosing;replayTransition=replayExporting=false;}
 }
} }
