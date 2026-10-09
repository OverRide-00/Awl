using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace Awl { partial class Shell {
 sealed class MicrophoneCapture {
  [StructLayout(LayoutKind.Sequential)] struct WaveFormat {public ushort Tag,Channels;public uint SamplesPerSecond,BytesPerSecond;public ushort BlockAlign,BitsPerSample,ExtraSize;}
  [StructLayout(LayoutKind.Sequential)] struct WaveHeader {public IntPtr Data;public uint Length,Recorded;public IntPtr User;public uint Flags,Loops;public IntPtr Next,Reserved;}
  [DllImport("winmm.dll")] static extern uint waveInOpen(out IntPtr handle,uint device,ref WaveFormat format,IntPtr callback,IntPtr instance,uint flags);
  [DllImport("winmm.dll")] static extern uint waveInPrepareHeader(IntPtr handle,IntPtr header,uint size);
  [DllImport("winmm.dll")] static extern uint waveInUnprepareHeader(IntPtr handle,IntPtr header,uint size);
  [DllImport("winmm.dll")] static extern uint waveInAddBuffer(IntPtr handle,IntPtr header,uint size);
  [DllImport("winmm.dll")] static extern uint waveInStart(IntPtr handle);
  [DllImport("winmm.dll")] static extern uint waveInStop(IntPtr handle);
  [DllImport("winmm.dll")] static extern uint waveInReset(IntPtr handle);
  [DllImport("winmm.dll")] static extern uint waveInClose(IntPtr handle);
  const int BufferSize=8192,BufferCount=4;
  readonly IntPtr[] headers=new IntPtr[BufferCount],buffers=new IntPtr[BufferCount];
  readonly uint headerSize=(uint)Marshal.SizeOf(typeof(WaveHeader));
  readonly string path;IntPtr device;Thread worker;volatile bool running,paused;long bytes;Exception error;
  public string Path{get{return path;}}
  public Exception Error{get{return error;}}
  public bool Paused{get{return paused;}set{paused=value;}}
  public MicrophoneCapture(string path){this.path=path;}
  public void Start(){
   var format=new WaveFormat{Tag=1,Channels=1,SamplesPerSecond=44100,BitsPerSample=16,BlockAlign=2,BytesPerSecond=88200};
   uint result=waveInOpen(out device,0xFFFFFFFF,ref format,IntPtr.Zero,IntPtr.Zero,0);
   if(result!=0)throw new IOException("Microphone could not open (Windows audio error "+result+"). Check the default recording device and microphone permission.");
   try{
    for(int i=0;i<BufferCount;i++){
     buffers[i]=Marshal.AllocHGlobal(BufferSize);headers[i]=Marshal.AllocHGlobal((int)headerSize);
     Marshal.StructureToPtr(new WaveHeader{Data=buffers[i],Length=BufferSize},headers[i],false);
     if(waveInPrepareHeader(device,headers[i],headerSize)!=0||waveInAddBuffer(device,headers[i],headerSize)!=0)throw new IOException("Microphone buffer could not start.");
    }
    if(waveInStart(device)!=0)throw new IOException("Microphone recording could not start.");
    running=true;worker=new Thread(Run){IsBackground=true,Name="Awl microphone recorder"};worker.Start();
   }catch{Stop();throw;}
  }
  void Run(){
   try{using(var stream=new FileStream(path,FileMode.Create,FileAccess.Write,FileShare.Read))using(var writer=new BinaryWriter(stream)){
    WriteHeader(writer,0);
    while(running){bool ready=false;
     for(int i=0;i<BufferCount;i++){
      var header=(WaveHeader)Marshal.PtrToStructure(headers[i],typeof(WaveHeader));
      if((header.Flags&1)==0)continue;ready=true;
      if(header.Recorded>0&&!paused){var data=new byte[header.Recorded];Marshal.Copy(buffers[i],data,0,data.Length);writer.Write(data);bytes+=data.Length;}
      header.Recorded=0;Marshal.StructureToPtr(header,headers[i],false);
      if(waveInAddBuffer(device,headers[i],headerSize)!=0)throw new IOException("Microphone input stopped unexpectedly.");
     }
     if(!ready)Thread.Sleep(8);
    }
    stream.Position=0;WriteHeader(writer,bytes);
   }}catch(Exception failure){error=failure;}
  }
  static void WriteHeader(BinaryWriter writer,long length){
   writer.Write(new[]{(byte)'R',(byte)'I',(byte)'F',(byte)'F'});writer.Write((uint)Math.Min(uint.MaxValue,length+36));
   writer.Write(new[]{(byte)'W',(byte)'A',(byte)'V',(byte)'E',(byte)'f',(byte)'m',(byte)'t',(byte)' '});
   writer.Write(16u);writer.Write((ushort)1);writer.Write((ushort)1);writer.Write(44100u);writer.Write(88200u);writer.Write((ushort)2);writer.Write((ushort)16);
   writer.Write(new[]{(byte)'d',(byte)'a',(byte)'t',(byte)'a'});writer.Write((uint)Math.Min(uint.MaxValue,length));
  }
  public void Stop(){
   running=false;if(worker!=null&&worker.IsAlive)worker.Join(2000);
   if(device!=IntPtr.Zero){waveInStop(device);waveInReset(device);for(int i=0;i<BufferCount;i++)if(headers[i]!=IntPtr.Zero)waveInUnprepareHeader(device,headers[i],headerSize);waveInClose(device);device=IntPtr.Zero;}
   for(int i=0;i<BufferCount;i++){if(headers[i]!=IntPtr.Zero){Marshal.FreeHGlobal(headers[i]);headers[i]=IntPtr.Zero;}if(buffers[i]!=IntPtr.Zero){Marshal.FreeHGlobal(buffers[i]);buffers[i]=IntPtr.Zero;}}
  }
 }
 public string MicrophoneSelfTest(){
  string file=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"Awl-microphone-test-"+Guid.NewGuid().ToString("N")+".wav");
  try{var capture=new MicrophoneCapture(file);capture.Start();Thread.Sleep(1200);capture.Stop();
   if(capture.Error!=null)throw capture.Error;
   if(!File.Exists(file)||new FileInfo(file).Length<=44)throw new IOException("The default microphone produced no audio samples.");
   return "Default microphone captured PCM audio ("+new FileInfo(file).Length+" bytes).";
  }catch(Exception failure){return "FAILED: "+failure.Message;}
  finally{try{File.Delete(file);}catch{}}
 }
} }
