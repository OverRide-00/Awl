using System;
using System.Diagnostics;
using System.Windows.Media;
using Awl;

namespace Awl.Motion {
 public sealed class AnimationApiPlugin : IAwlCorePlugin {
  public void Initialize(IAwlPluginHost host) {
   host.RegisterService("awl.animation-api",new ProceduralAnimationApi());
   host.Log("Motion API is ready for other plugins.");
  }
 }

 sealed class ProceduralAnimationApi : IAwlAnimationApi {
  public string Version { get { return "1.0"; } }

  public IAwlAnimationHandle Animate(AwlAnimationSpec spec,Action<double> apply,Action completed=null) {
   if(apply==null)throw new ArgumentNullException("apply");
   spec=spec??new AwlAnimationSpec();
   return Start(spec,apply,completed);
  }

  public IAwlAnimationHandle Follow(Func<double> source,Action<double> apply,AwlAnimationSpec response=null) {
   if(source==null)throw new ArgumentNullException("source");
   if(apply==null)throw new ArgumentNullException("apply");
   var spec=response??new AwlAnimationSpec{DurationMs=110};
   var clock=Stopwatch.StartNew();
   double previous=clock.Elapsed.TotalMilliseconds,current=Clamp(source());
   var handle=new Handle();
   EventHandler tick=null;
   tick=delegate {
    if(!handle.IsRunning){CompositionTarget.Rendering-=tick;return;}
    double now=clock.Elapsed.TotalMilliseconds,delta=Math.Max(0,now-previous);previous=now;
    double target=Clamp(source());
    double blend=1-Math.Exp(-delta/Math.Max(1,spec.DurationMs));
    current+= (target-current)*blend;
    apply(current);
   };
   handle.StopAction=delegate {CompositionTarget.Rendering-=tick;};
   CompositionTarget.Rendering+=tick;
   return handle;
  }

  public double Evaluate(AwlAnimationCurve curve,double progress,double strength=1,double frequency=2,double damping=7) {
   double p=Clamp(progress),s=Math.Max(.01,strength);
   switch(curve) {
    case AwlAnimationCurve.SmoothStep:return p*p*(3-2*p);
    case AwlAnimationCurve.EaseIn:return Math.Pow(p,2+s);
    case AwlAnimationCurve.EaseOut:return 1-Math.Pow(1-p,2+s);
    case AwlAnimationCurve.EaseInOut:return p<.5?Math.Pow(p*2,2+s)/2:1-Math.Pow((1-p)*2,2+s)/2;
    case AwlAnimationCurve.BackOut:{double c=1.70158*s,q=p-1;return 1+(c+1)*q*q*q+c*q*q;}
    case AwlAnimationCurve.ElasticOut:return p==0||p==1?p:Math.Pow(2,-10*p)*Math.Sin((p*10-.75)*(2*Math.PI/3))+1;
    case AwlAnimationCurve.Spring:return 1-Math.Exp(-Math.Max(.1,damping)*p)*Math.Cos(Math.Max(.1,frequency)*2*Math.PI*p);
    case AwlAnimationCurve.Wave:return .5+.5*Math.Sin((p*Math.Max(.1,frequency)*2*Math.PI)-Math.PI/2);
    default:return p;
   }
  }

  IAwlAnimationHandle Start(AwlAnimationSpec spec,Action<double> apply,Action completed) {
   var clock=Stopwatch.StartNew();
   var handle=new Handle();
   EventHandler tick=null;
   tick=delegate {
    if(!handle.IsRunning){CompositionTarget.Rendering-=tick;return;}
    double elapsed=clock.Elapsed.TotalMilliseconds-spec.DelayMs;
    if(elapsed<0)return;
    double duration=Math.Max(1,spec.DurationMs),cycles=elapsed/duration;
    int index=(int)Math.Floor(cycles);
    int repeat=Math.Max(1,spec.RepeatCount);
    if(!spec.Loop&&index>=repeat) {
     double terminal=spec.AutoReverse&&repeat%2==0?0:1;
     apply(spec.From+(spec.To-spec.From)*terminal);
     handle.Cancel();
     if(completed!=null)completed();
     return;
    }
    double phase=cycles-index;
    if(spec.AutoReverse&&index%2==1)phase=1-phase;
    double shaped=spec.ProceduralCurve==null?Evaluate(AwlAnimationCurve.SmoothStep,phase,spec.Strength,spec.Frequency,spec.Damping):spec.ProceduralCurve(phase);
    apply(spec.From+(spec.To-spec.From)*shaped);
   };
   handle.StopAction=delegate {CompositionTarget.Rendering-=tick;};
   CompositionTarget.Rendering+=tick;
   return handle;
  }

  static double Clamp(double value) {return Double.IsNaN(value)?0:Math.Max(0,Math.Min(1,value));}
  sealed class Handle : IAwlAnimationHandle {
   public Action StopAction;
   public bool IsRunning {get;private set;}
   public Handle(){IsRunning=true;}
   public void Cancel(){if(!IsRunning)return;IsRunning=false;if(StopAction!=null)StopAction();}
   public void Dispose(){Cancel();}
  }
 }
}
