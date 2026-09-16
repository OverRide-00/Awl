using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace Awl { partial class Shell {
 FrameworkElement SlidersIcon(Brush brush){var canvas=new Canvas{Width=36,Height=26,VerticalAlignment=VerticalAlignment.Center};for(int i=0;i<3;i++){var line=new Border{Width=23,Height=2,Background=brush,CornerRadius=new CornerRadius(1)};Canvas.SetTop(line,4+i*8);canvas.Children.Add(line);var knob=new Border{Width=5,Height=7,Background=brush,CornerRadius=new CornerRadius(2)};Canvas.SetLeft(knob,i==1?14:5);Canvas.SetTop(knob,1.5+i*8);canvas.Children.Add(knob);}return canvas;}
 void FrameSections(Panel host){var children=host.Children.Cast<UIElement>().ToArray();host.Children.Clear();foreach(var child in children){EmphasizeQuick(child);EnlargeQuickOptions(child);host.Children.Add(new Border{Child=child,Background=new SolidColorBrush(Mix(baseColor,Colors.White,light?.06:.035)),BorderBrush=new SolidColorBrush(Mix(baseColor,Colors.White,.12)),BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(16),Padding=new Thickness(12),Margin=new Thickness(0,0,8,8)});}}
 void WeatherLocation(){
  page.Children.Add(Text(cfg.WeatherLocation,13));
  var row=new DockPanel{Margin=new Thickness(0,8,0,8)};
  var input=new TextBox{Text=cfg.WeatherLocation,Background=cardBrush,Foreground=ink,CaretBrush=ink,BorderThickness=new Thickness(0),Padding=new Thickness(10),FontSize=13};
  System.Windows.Automation.AutomationProperties.SetName(input,"Weather city");
  var results=new StackPanel();var status=Text("",11,mutedBrush);Button search=null;
  search=Action("⌕ Search",async()=>{
   string query=input.Text.Trim();if(query.Length<2){status.Text="Enter a city name";return;}
   search.IsEnabled=false;results.Children.Clear();status.Text="Searching…";
   try{using(var client=new System.Net.Http.HttpClient()){
    client.Timeout=TimeSpan.FromSeconds(10);
    string json=await client.GetStringAsync("https://geocoding-api.open-meteo.com/v1/search?name="+Uri.EscapeDataString(query)+"&count=8&language=en&format=json");
    var data=new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<Dictionary<string,object>>(json);
    object matches;if(!data.TryGetValue("results",out matches)){status.Text="No locations found";return;}
    foreach(var item in (System.Collections.IEnumerable)matches){
     var place=(Dictionary<string,object>)item;string name=Convert.ToString(place["name"]);object region,country;string label=name;
     if(place.TryGetValue("admin1",out region)&&Convert.ToString(region)!=name)label+=", "+Convert.ToString(region);
     if(place.TryGetValue("country",out country))label+=", "+Convert.ToString(country);
     double latitude=Convert.ToDouble(place["latitude"]),longitude=Convert.ToDouble(place["longitude"]);
     var choose=Action(label,async()=>{cfg.WeatherLocation=label;cfg.WeatherLatitude=latitude;cfg.WeatherLongitude=longitude;nextWeather=DateTime.MinValue;weatherTime=DateTime.MinValue;weather=label+" —";if(weatherLabel!=null)weatherLabel.Text=weather;SaveConfig();DrawLauncher();await Weather();if(weatherLabel!=null)weatherLabel.Text=weather;});
     choose.HorizontalContentAlignment=HorizontalAlignment.Left;results.Children.Add(choose);
    }status.Text="Select a location";
   }}catch{status.Text="Location search unavailable. Try again.";}finally{search.IsEnabled=true;}
  });
  DockPanel.SetDock(search,Dock.Right);row.Children.Add(search);row.Children.Add(input);
  input.KeyDown+=delegate(object sender,System.Windows.Input.KeyEventArgs e){if(e.Key==System.Windows.Input.Key.Enter&&search.IsEnabled){search.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));e.Handled=true;}};
  page.Children.Add(row);page.Children.Add(status);page.Children.Add(results);var attribution=Text("Open-Meteo · GeoNames",10,mutedBrush);attribution.Margin=new Thickness(0,10,0,0);page.Children.Add(attribution);
 }
} }

