using System;
using System.Collections.Generic;
using System.Linq;

namespace Awl { partial class Shell {
 Dictionary<string,object> builderRuntime=new Dictionary<string,object>(); bool builderLoaded;

 object BlockValue(BuilderValue value){
  if(value==null)return null;if(!String.IsNullOrEmpty(value.binding)){if(value.binding.StartsWith("block:"))return BuilderReporterValue(builderProject.logic_graph.FirstOrDefault(x=>x.id==value.binding.Substring(6)));string key=value.binding.StartsWith("var:")?value.binding.Substring(4):value.binding;object found;return builderRuntime.TryGetValue(key,out found)?found:null;}return value.value;
 }
 object BuilderReporterValue(BuilderBlock block){if(block==null)return null;if(block.type=="access.time")return FormatBuilderTime(DateTime.Now,BuilderTimeFormat(block));if(block.type=="access.cpu_usage")return builderRuntime.ContainsKey("cpu_usage")?builderRuntime["cpu_usage"]:0.0;if(block.type=="access.running_apps")return builderRuntime.ContainsKey("running_apps")?builderRuntime["running_apps"]:new string[0];if(block.type=="var.get"){string name=Convert.ToString(BlockParameter(block,"name",""));object value;return builderRuntime.TryGetValue(name,out value)?value:null;}if(block.type=="operators.add")return Convert.ToDouble(BlockParameter(block,"a",0))+Convert.ToDouble(BlockParameter(block,"b",0));if(block.type=="operators.compare")return Object.Equals(BlockParameter(block,"a"),BlockParameter(block,"b"));return null;}
 object BlockParameter(BuilderBlock block,string key,object fallback=null){BuilderValue value;return block.parameters.TryGetValue(key,out value)?BlockValue(value):fallback;}
 void SeedBuilderRuntime(){builderRuntime.Clear();foreach(var variable in builderProject.variables)builderRuntime[variable.name]=variable.initial;builderRuntime["cpu_usage"]=34.0;builderRuntime["running_apps"]=new[]{"Browser","Editor","Music"};builderRuntime["time"]=DateTime.Now.ToString("HH:mm");}
 void ExecuteBuilderEvents(string eventType){foreach(var block in builderProject.logic_graph.Where(x=>x.type==eventType).ToArray())ExecuteBuilderChain(block.next,new HashSet<string>(),null);}
 void ExecuteBuilderChain(string id,HashSet<string> visited,object item){
  while(!String.IsNullOrEmpty(id)&&visited.Add(id)){var block=builderProject.logic_graph.FirstOrDefault(x=>x.id==id);if(block==null)return;
   if(block.type=="var.set"){string name=Convert.ToString(BlockParameter(block,"name","value"));builderRuntime[name]=BlockParameter(block,"value");}
   else if(block.type=="ui.set_property"){var node=FindBuilderNode(builderProject.ui_tree,Convert.ToString(BlockParameter(block,"node")));string property=Convert.ToString(BlockParameter(block,"property"));if(node!=null&&!String.IsNullOrEmpty(property))SetBuilder(node,property,BlockParameter(block,"value"));}
   else if(block.type=="ui.clear_children"){var node=FindBuilderNode(builderProject.ui_tree,Convert.ToString(BlockParameter(block,"node")));if(node!=null)node.children.Clear();}
   else if(block.type=="control.if"){bool condition=false;try{condition=Convert.ToBoolean(BlockParameter(block,"condition",false));}catch{}if(condition&&block.body.Count>0)ExecuteBuilderChain(block.body[0],new HashSet<string>(visited),item);}
   else if(block.type=="control.for_each"){object list=BlockParameter(block,"list",builderRuntime.ContainsKey("running_apps")?builderRuntime["running_apps"]:null);var enumerable=list as System.Collections.IEnumerable;if(enumerable!=null)foreach(var value in enumerable)if(block.body.Count>0)ExecuteBuilderChain(block.body[0],new HashSet<string>(visited),value);}
   else if(block.type=="ui.add_child"){var node=FindBuilderNode(builderProject.ui_tree,Convert.ToString(BlockParameter(block,"node")));if(node!=null){var child=NewBuilderNode(Convert.ToString(BlockParameter(block,"type","text")));SetBuilder(child,"content",item??BlockParameter(block,"content","item"));node.children.Add(child);}}
   id=block.next;
  }
 }
 void RunBuilderProgram(){
  builderRuntime["cpu_usage"]=(DateTime.Now.Millisecond%83)+8;builderRuntime["running_apps"]=new[]{"Browser","Editor","Music"};builderRuntime["time"]=DateTime.Now.ToString("HH:mm:ss");
  if(!builderLoaded){builderLoaded=true;ExecuteBuilderEvents("events.on_load");}ExecuteBuilderEvents("events.on_interval");
  foreach(var variable in builderProject.variables)if(builderRuntime.ContainsKey(variable.name))variable.initial=builderRuntime[variable.name];
 }
} }
