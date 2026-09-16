using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Awl { partial class Shell {
 Dictionary<string,Geometry> builderSvgCache=new Dictionary<string,Geometry>(StringComparer.OrdinalIgnoreCase);
 Geometry BuilderSvgGeometry(string name){Geometry cached;if(builderSvgCache.TryGetValue(name,out cached))return cached;var group=new GeometryGroup();try{string xml="",resource="Awl.BuilderIcons."+name+".svg";var assembly=typeof(Shell).Assembly;using(var stream=assembly.GetManifestResourceStream(resource))if(stream!=null)using(var reader=new StreamReader(stream))xml=reader.ReadToEnd();if(String.IsNullOrEmpty(xml)){string assemblyRoot=System.IO.Path.GetDirectoryName(assembly.Location);string file=System.IO.Path.Combine(assemblyRoot,"Assets","BuilderIcons",name+".svg");if(File.Exists(file))xml=File.ReadAllText(file);}foreach(Match match in Regex.Matches(xml,"<path[^>]*\\sd=\"([^\"]+)\"",RegexOptions.IgnoreCase))group.Children.Add(Geometry.Parse(match.Groups[1].Value));}catch{}if(group.Children.Count==0)group.Children.Add(Geometry.Parse("M4,4 L20,4 L20,20 L4,20 Z"));group.Freeze();builderSvgCache[name]=group;return group;}
 FrameworkElement BuilderSvgIcon(string name,double size=20,Brush color=null){return new Viewbox{Width=size,Height=size,Stretch=Stretch.Uniform,Child=new System.Windows.Shapes.Path{Data=BuilderSvgGeometry(name),Stroke=color??ink,StrokeThickness=1.9,StrokeStartLineCap=PenLineCap.Round,StrokeEndLineCap=PenLineCap.Round,StrokeLineJoin=PenLineJoin.Round,Fill=Brushes.Transparent}};}
 FrameworkElement BuilderIconLabel(string icon,string label,Brush color=null,double size=18){var row=new StackPanel{Orientation=Orientation.Horizontal};row.Children.Add(BuilderSvgIcon(icon,size,color));row.Children.Add(new TextBlock{Text=label,Foreground=color??ink,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(9,0,0,0)});return row;}
 string BuilderNodeIcon(string type){if(type=="container")return "container";if(type=="panel")return "panel";if(type=="column")return "column";if(type=="row")return "row";if(type=="grid")return "grid";if(type=="stack")return "stack";if(type=="text"||type=="label"||type=="rich_text")return "text";if(type=="icon")return "star";if(type=="button")return "button";if(type=="dropdown")return "dropdown";if(type=="image")return "image";if(type=="slider")return "slider";if(type=="toggle")return "toggle";if(type=="progress_bar")return "progress";if(type=="divider")return "divider";if(type=="prefab_instance")return "prefab";return "panel";}
 string BuilderBlockIcon(string type){return type.StartsWith("events")?"event":type.StartsWith("control")?"branch":type.StartsWith("access")?"database":type.StartsWith("ui")?"ui":type.StartsWith("var")?"variable":"operator";}
} }
