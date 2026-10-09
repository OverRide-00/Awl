using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Text.RegularExpressions;
using System.Globalization;

namespace Awl { partial class Shell {
 // Raw-video input has no timestamps. Schedule frames against a monotonic clock
 // rather than assuming screen capture and pipe writes always meet the frame rate.
 sealed class CaptureClock {
  readonly Stopwatch watch=new Stopwatch();readonly object gate=new object();bool paused,stopped;
  public void Start(){lock(gate){stopped=false;paused=false;watch.Restart();}}
  public bool Paused {get{lock(gate)return paused;}set{lock(gate){paused=value;if(stopped)return;if(value)watch.Stop();else watch.Start();}}}
  public double Seconds {get{lock(gate)return watch.Elapsed.TotalSeconds;}}
  public void Stop(){lock(gate){stopped=true;watch.Stop();}}
 }
 static long CaptureFrameCount(double seconds,int fps){return Math.Max(0,(long)Math.Ceiling(seconds*fps));}
 static byte[] CapturePixels(System.Drawing.Bitmap bitmap){
  var data=bitmap.LockBits(new System.Drawing.Rectangle(0,0,bitmap.Width,bitmap.Height),System.Drawing.Imaging.ImageLockMode.ReadOnly,System.Drawing.Imaging.PixelFormat.Format32bppArgb);
  try{var bytes=new byte[bitmap.Width*bitmap.Height*4];for(int y=0;y<bitmap.Height;y++)Marshal.Copy(IntPtr.Add(data.Scan0,y*data.Stride),bytes,y*bitmap.Width*4,bitmap.Width*4);return bytes;}finally{bitmap.UnlockBits(data);}
 }
 static void WriteRealtimeCapture(Stream pipe,int width,int height,int fps,CaptureClock clock,Func<bool> running,Func<byte[]> capture,Action wroteFrame){
  byte[] latest=new byte[width*height*4];Exception captureError=null;bool producerDone=false;long written=0;int cancelled=0;
  var producer=new Thread(delegate(){try{while(running()&&Volatile.Read(ref cancelled)==0){
    if(clock.Paused){Thread.Sleep(10);continue;}
    var began=Stopwatch.StartNew();Interlocked.Exchange(ref latest,capture());
    int delay=(int)Math.Max(1,1000.0/fps-began.Elapsed.TotalMilliseconds);Thread.Sleep(delay);
   }}catch(Exception error){Interlocked.Exchange(ref captureError,error);}finally{Volatile.Write(ref producerDone,true);}}){IsBackground=true,Name="Awl capture frames"};
  producer.Start();
  try{while(running()||written<CaptureFrameCount(clock.Seconds,fps)){
    var error=Interlocked.CompareExchange(ref captureError,null,null);if(error!=null)throw new IOException("Screen capture failed",error);
    long target=CaptureFrameCount(clock.Seconds,fps);
    if(written<target){var frame=Interlocked.CompareExchange(ref latest,null,null);pipe.Write(frame,0,frame.Length);written++;if(wroteFrame!=null)wroteFrame();}
    else Thread.Sleep(2);
   }}finally{Interlocked.Exchange(ref cancelled,1);clock.Stop();if(!Volatile.Read(ref producerDone))producer.Join(2000);}
 }
 public string CaptureTimingSelfTest(){
  string output=Path.Combine(Path.GetTempPath(),"Awl-timing-"+Guid.NewGuid().ToString("N")+".mp4");
  try{string runtime=CaptureRuntime();var clock=new CaptureClock();int running=1;long count=0;clock.Start();
   var process=Process.Start(new ProcessStartInfo(runtime,"-y -f rawvideo -pixel_format bgra -video_size 64x64 -framerate 20 -i pipe:0 -an -c:v libopenh264 -rc_mode bitrate -profile high -coder cabac -b:v 900000 -pix_fmt yuv420p -g 20 -movflags +frag_keyframe+empty_moov+default_base_moof \""+output+"\""){UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardError=true});process.ErrorDataReceived+=delegate{};process.BeginErrorReadLine();
   var control=new Thread(delegate(){Thread.Sleep(1000);clock.Paused=true;double frozen=clock.Seconds;Thread.Sleep(600);if(Math.Abs(clock.Seconds-frozen)>.01)Interlocked.Exchange(ref running,-1);clock.Paused=false;Thread.Sleep(1200);clock.Stop();Interlocked.CompareExchange(ref running,0,1);});control.Start();
   WriteRealtimeCapture(process.StandardInput.BaseStream,64,64,20,clock,()=>Volatile.Read(ref running)==1,()=>{Thread.Sleep(230);var pixels=new byte[64*64*4];for(int i=0;i<pixels.Length;i+=4){pixels[i]=(byte)(clock.Seconds*70);pixels[i+3]=255;}return pixels;},()=>count++);
   control.Join();process.StandardInput.Close();if(!process.WaitForExit(10000)||process.ExitCode!=0)throw new Exception("Encoder did not finish");
   var probe=Process.Start(new ProcessStartInfo(runtime,"-hide_banner -i \""+output+"\""){UseShellExecute=false,CreateNoWindow=true,RedirectStandardError=true});string info=probe.StandardError.ReadToEnd();probe.WaitForExit();var duration=Regex.Match(info,@"Duration: (\d+):(\d+):(\d+\.\d+)");if(!duration.Success)throw new Exception("MP4 duration was missing");
   double actual=Int32.Parse(duration.Groups[1].Value)*3600+Int32.Parse(duration.Groups[2].Value)*60+Double.Parse(duration.Groups[3].Value,CultureInfo.InvariantCulture);
   if(Volatile.Read(ref running)==-1||Math.Abs(actual-clock.Seconds)>.10||count!=CaptureFrameCount(clock.Seconds,20))throw new Exception("Video timing mismatch");
   return "Delayed capture retains real-time duration: "+actual.ToString("0.00",CultureInfo.InvariantCulture)+"s; pause excluded; "+count+" frames encoded at 20 fps.";
  }catch(Exception error){return "FAILED: "+error;}finally{try{if(File.Exists(output))File.Delete(output);}catch{}}
 }
} }
