using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Awl;

namespace Awl.Ratio {
 public sealed class RatioPlugin : IAwlCorePlugin {
  IAwlLayoutRatioApi layout;
  DispatcherTimer monitor;
  double width,height;

  public void Initialize(IAwlPluginHost host) {
   layout=host.GetService("awl.layout-ratio-api") as IAwlLayoutRatioApi;
   if(layout==null){host.Log("The Awl layout-ratio service is unavailable.");return;}
   width=SystemParameters.VirtualScreenWidth;height=SystemParameters.VirtualScreenHeight;
   host.RegisterSection(new AwlPluginSection {
    Id="ratio", Tab="Interface", Title="Display ratio", Icon="Ratio", Build=BuildSection
   });
   SystemParameters.StaticPropertyChanged+=delegate {CheckDisplay();};
   monitor=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(750)};
   monitor.Tick+=delegate {CheckDisplay();};
   monitor.Start();
  }

  FrameworkElement BuildSection() {
   var panel=new StackPanel();
   var baseline=layout.Baseline;
   var status=new TextBlock{Text=Status(baseline),Margin=new Thickness(0,0,0,10),TextWrapping=TextWrapping.Wrap};
   var set=new Button{Content="Set ratio from this display",Padding=new Thickness(12,7,12,7),MinHeight=32};
   set.Click+=delegate {baseline=layout.SetBaseline();width=SystemParameters.VirtualScreenWidth;height=SystemParameters.VirtualScreenHeight;status.Text=Status(baseline);};
   panel.Children.Add(status);panel.Children.Add(set);
   var note=new TextBlock{Text="Awl stores the Date & Time and Recents widget locations as fractions of this screen. When the display changes, their matching ratio position is restored.",Margin=new Thickness(0,10,0,0),TextWrapping=TextWrapping.Wrap};
   panel.Children.Add(note);return panel;
  }

  void CheckDisplay() {
   if(layout==null||!layout.Baseline.Enabled)return;
   double nextWidth=SystemParameters.VirtualScreenWidth,nextHeight=SystemParameters.VirtualScreenHeight;
   if(Math.Abs(nextWidth-width)<.01&&Math.Abs(nextHeight-height)<.01)return;
   width=nextWidth;height=nextHeight;layout.ApplyForCurrentDisplay();
  }

  static string Status(AwlRatioBaseline baseline) {
   if(baseline==null||!baseline.Enabled)return "No display ratio has been set yet.";
   return "Baseline: "+Math.Round(baseline.Width)+" × "+Math.Round(baseline.Height)+". Widget positions will follow this ratio after a display change.";
  }
 }
}
