using System;
using System.Windows;
namespace Awl { partial class Shell {
 sealed class LayoutRatioService : IAwlLayoutRatioApi {
  readonly Shell shell;
  public LayoutRatioService(Shell owner){shell=owner;}
  public AwlRatioBaseline Baseline {get{return shell.cfg.RatioBaseline??(shell.cfg.RatioBaseline=new AwlRatioBaseline());}}
  public AwlRatioBaseline SetBaseline(){return shell.CaptureRatioBaseline();}
  public void ApplyForCurrentDisplay(){shell.ApplyRatioBaseline();}
 }
 AwlRatioBaseline CaptureRatioBaseline(){
  var baseline=cfg.RatioBaseline??(cfg.RatioBaseline=new AwlRatioBaseline());
  baseline.Width=Math.Max(1,SystemParameters.VirtualScreenWidth);baseline.Height=Math.Max(1,SystemParameters.VirtualScreenHeight);
  double left=SystemParameters.VirtualScreenLeft,top=SystemParameters.VirtualScreenTop;
  baseline.DateTimeX=(clockWidget!=null?clockWidget.Left:cfg.DateTimeWidget.X)-left;
  baseline.DateTimeY=(clockWidget!=null?clockWidget.Top:cfg.DateTimeWidget.Y)-top;
  baseline.RecentsX=(recentWindow!=null?recentWindow.Left:cfg.Recents.X)-left;
  baseline.RecentsY=(recentWindow!=null?recentWindow.Top:cfg.Recents.Y)-top;
  baseline.Enabled=true;baseline.CapturedUtc=DateTime.UtcNow;SaveConfig();return baseline;
 }
 void ApplyRatioBaseline(){
  var baseline=cfg.RatioBaseline;if(baseline==null||!baseline.Enabled||baseline.Width<=0||baseline.Height<=0)return;
  double width=Math.Max(1,SystemParameters.VirtualScreenWidth),height=Math.Max(1,SystemParameters.VirtualScreenHeight),left=SystemParameters.VirtualScreenLeft,top=SystemParameters.VirtualScreenTop;
  cfg.DateTimeWidget.X=left+baseline.DateTimeX/baseline.Width*width;cfg.DateTimeWidget.Y=top+baseline.DateTimeY/baseline.Height*height;cfg.DateTimeWidget.RelativeX=-1;cfg.DateTimeWidget.RelativeY=-1;
  cfg.Recents.X=left+baseline.RecentsX/baseline.Width*width;cfg.Recents.Y=top+baseline.RecentsY/baseline.Height*height;cfg.Recents.RelativeX=-1;cfg.Recents.RelativeY=-1;
  SaveConfig();ApplyClockWidget();recentSignature="";DrawRecents();
 }
} }
