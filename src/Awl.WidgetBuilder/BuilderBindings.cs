using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Awl { partial class Shell {
 IEnumerable<Tuple<string,string,string>> BuilderBindingSources(){
  foreach(var variable in builderProject.variables)yield return Tuple.Create("var:"+variable.name,"Variable · "+variable.name,"variable");
  foreach(var block in builderProject.logic_graph.Where(x=>!BuilderSequenceBlock(x.type)))yield return Tuple.Create("block:"+block.id,"Data · "+BuilderBlockLabel(block.type),BuilderBlockIcon(block.type));
 }

 void ShowBuilderBindingMenu(Button anchor,BuilderValue target,Action changed,string excludedVariable=null){
  var menu=new ContextMenu{Placement=PlacementMode.Bottom,PlacementTarget=anchor};ThemeContextMenu(menu);
  var literal=new MenuItem{Header=BuilderIconLabel("edit","Literal value",ink,17),Foreground=ink,Background=panelBrush};
  literal.Click+=delegate{target.binding=null;changed();};menu.Items.Add(literal);
  var sources=BuilderBindingSources().Where(x=>x.Item1!="var:"+excludedVariable).ToArray();if(sources.Length>0)menu.Items.Add(new Separator{Background=cardBrush});
  foreach(var source in sources){var item=source;var choice=new MenuItem{Header=BuilderIconLabel(item.Item3,item.Item2,ink,17),Foreground=ink,Background=panelBrush,Padding=new Thickness(8)};choice.Click+=delegate{target.binding=item.Item1;target.value=null;changed();};menu.Items.Add(choice);}
  if(menu.Items.Count==1){var empty=new MenuItem{Header="Add a variable or data block first",Foreground=mutedBrush,Background=panelBrush,IsEnabled=false};menu.Items.Add(empty);}
  menu.IsOpen=true;
 }

 string BuilderBlockDescription(string type){
  switch(type){
   case "events.on_load":return "Runs the connected Next flow once when the widget loads.";
   case "events.on_interval":return "Runs the connected Next flow repeatedly at the chosen interval.";
   case "control.if":return "Runs its Body flow only when Condition is true.";
   case "control.for_each":return "Runs its Body once for every item in a list.";
   case "control.while_loop":return "Repeats its Body using the selected interval.";
   case "ui.set_property":return "Changes a property on a UI element. Choose the element, property, then a literal, variable, or data value.";
   case "ui.add_child":return "Creates a new child inside the selected container.";
   case "ui.clear_children":return "Removes every generated child from the selected container.";
   case "var.set":return "Stores a value in the named variable. Drop a data block into Value or choose one with Link.";
   case "var.get":return "Reads the current named variable. Drag this block into any value socket.";
   case "access.time":return "Live current time. It updates on its own and can be dragged into a value socket.";
   case "access.cpu_usage":return "Live CPU percentage. It updates on its own and can be dragged into a value socket.";
   case "access.running_apps":return "Live list of running apps. It updates on its own and can feed lists or loops.";
   case "operators.add":return "Adds two numeric inputs and outputs the result.";
   case "operators.compare":return "Compares two inputs and outputs true or false.";
   default:return "Connect this block to a compatible flow or value socket.";
  }
 }

 void RefreshBuilderAccessData(){
  if(builderRuntime==null)return;
  try{var sample=ReadSystem(false);builderRuntime["cpu_usage"]=sample.Cpu;}catch{if(!builderRuntime.ContainsKey("cpu_usage"))builderRuntime["cpu_usage"]=0.0;}
  var apps=new List<string>();foreach(var process in Process.GetProcesses()){try{if(process.MainWindowHandle!=IntPtr.Zero&&!String.IsNullOrWhiteSpace(process.MainWindowTitle))apps.Add(process.ProcessName);}catch{}finally{process.Dispose();}}builderRuntime["running_apps"]=apps.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x=>x).ToArray();
  builderRuntime["time"]=DateTime.Now.ToString("HH:mm:ss");
  foreach(var pair in builderLiveReporterLabels.ToArray()){var block=builderProject.logic_graph.FirstOrDefault(x=>x.id==pair.Key);if(block!=null)pair.Value.Text=BuilderReporterPreview(block);}
  if(builderCanvas!=null)RenderBuilderCanvas();
 }

 string BuilderReporterPreview(BuilderBlock block){
  string kind=BuilderBlockOutput(block.type).Replace("▼ ","").Replace("○ ","").Replace("▱ ","").Replace("◇ ","");
  if(BuilderSequenceBlock(block.type))return kind;
  object value=BuilderReporterValue(block);string display=value is Array?"["+String.Join(", ",((Array)value).Cast<object>())+"]":Convert.ToString(value);
  if(display!=null&&display.Length>24)display=display.Substring(0,24)+"…";
  return kind+"  ·  "+display;
 }
} }
