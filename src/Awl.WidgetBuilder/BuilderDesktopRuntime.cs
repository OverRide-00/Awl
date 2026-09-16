using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Awl { partial class Shell {
 class BuilderDesktopExecution {
  public Dictionary<string,object> values=new Dictionary<string,object>();
  public Dictionary<string,DateTime> nextDue=new Dictionary<string,DateTime>();
  public bool loaded;
 }
 Dictionary<string,BuilderDesktopExecution> builderDesktopExecutions=new Dictionary<string,BuilderDesktopExecution>();

 BuilderDesktopExecution DesktopExecution(WidgetBuilderDeployment deployment){
  BuilderDesktopExecution state;if(builderDesktopExecutions.TryGetValue(deployment.id,out state))return state;
  state=new BuilderDesktopExecution();foreach(var variable in deployment.project.variables)state.values[variable.name]=variable.initial;builderDesktopExecutions[deployment.id]=state;return state;
 }

 Dictionary<string,object> TickBuilderDesktopRuntime(WidgetBuilderDeployment deployment){
  var project=deployment.project;var state=DesktopExecution(deployment);RefreshDesktopAccess(state.values);
  if(!state.loaded){state.loaded=true;foreach(var start in project.logic_graph.Where(x=>x.type=="events.on_load"))ExecuteDesktopBuilderChain(project,state,start.next,new HashSet<string>(),null);}
  DateTime now=DateTime.Now;foreach(var interval in project.logic_graph.Where(x=>x.type=="events.on_interval")){DateTime due;if(!state.nextDue.TryGetValue(interval.id,out due)||now>=due){ExecuteDesktopBuilderChain(project,state,interval.next,new HashSet<string>(),null);double seconds;try{seconds=Math.Max(.25,Convert.ToDouble(DesktopBlockParameter(project,state,interval,"seconds",1.0)));}catch{seconds=1;}state.nextDue[interval.id]=now.AddSeconds(seconds);}}
  return state.values;
 }

 void RefreshDesktopAccess(Dictionary<string,object> values){
  values["time"]=DateTime.Now.ToString("HH:mm:ss");values["date"]=DateTime.Now.ToString("ddd, d MMM");values["cpu_usage"]=systemSample==null?0.0:systemSample.Cpu;values["ram_usage"]=systemSample==null||systemSample.MemoryTotal<=0?0.0:systemSample.MemoryUsed/systemSample.MemoryTotal*100;
  var apps=new List<string>();foreach(var process in Process.GetProcesses()){try{if(process.MainWindowHandle!=IntPtr.Zero&&!String.IsNullOrWhiteSpace(process.MainWindowTitle))apps.Add(process.ProcessName);}catch{}finally{process.Dispose();}}values["running_apps"]=apps.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x=>x).ToArray();
 }

 object DesktopBlockValue(WidgetBuilderProject project,BuilderDesktopExecution state,BuilderValue value){
  if(value==null)return null;if(String.IsNullOrEmpty(value.binding))return value.value;
  if(value.binding.StartsWith("block:"))return DesktopReporterValue(project,state,project.logic_graph.FirstOrDefault(x=>x.id==value.binding.Substring(6)));
  string key=value.binding.StartsWith("var:")?value.binding.Substring(4):value.binding;object found;return state.values.TryGetValue(key,out found)?found:null;
 }
 object DesktopBlockParameter(WidgetBuilderProject project,BuilderDesktopExecution state,BuilderBlock block,string key,object fallback=null){BuilderValue value;return block.parameters.TryGetValue(key,out value)?DesktopBlockValue(project,state,value):fallback;}
 object DesktopReporterValue(WidgetBuilderProject project,BuilderDesktopExecution state,BuilderBlock block){
  if(block==null)return null;if(block.type=="access.time")return FormatBuilderTime(DateTime.Now,BuilderTimeFormat(block));if(block.type=="access.cpu_usage")return state.values.ContainsKey("cpu_usage")?state.values["cpu_usage"]:0.0;if(block.type=="access.running_apps")return state.values.ContainsKey("running_apps")?state.values["running_apps"]:new string[0];
  if(block.type=="var.get"){string name=Convert.ToString(DesktopBlockParameter(project,state,block,"name",""));object found;return state.values.TryGetValue(name,out found)?found:null;}
  if(block.type=="operators.add"){try{return Convert.ToDouble(DesktopBlockParameter(project,state,block,"a",0))+Convert.ToDouble(DesktopBlockParameter(project,state,block,"b",0));}catch{return 0.0;}}
  if(block.type=="operators.compare")return Object.Equals(DesktopBlockParameter(project,state,block,"a"),DesktopBlockParameter(project,state,block,"b"));return null;
 }

 void ExecuteDesktopBuilderChain(WidgetBuilderProject project,BuilderDesktopExecution state,string id,HashSet<string> visited,object item){
  while(!String.IsNullOrEmpty(id)&&visited.Add(id)){var block=project.logic_graph.FirstOrDefault(x=>x.id==id);if(block==null)return;
   if(block.type=="var.set"){string name=Convert.ToString(DesktopBlockParameter(project,state,block,"name","value"));state.values[name]=DesktopBlockParameter(project,state,block,"value");}
   else if(block.type=="ui.set_property"){var node=FindBuilderNode(project.ui_tree,Convert.ToString(DesktopBlockParameter(project,state,block,"node")));string property=Convert.ToString(DesktopBlockParameter(project,state,block,"property"));if(node!=null&&!String.IsNullOrEmpty(property))SetBuilder(node,property,DesktopBlockParameter(project,state,block,"value"));}
   else if(block.type=="ui.clear_children"){var node=FindBuilderNode(project.ui_tree,Convert.ToString(DesktopBlockParameter(project,state,block,"node")));if(node!=null)node.children.Clear();}
   else if(block.type=="ui.add_child"){var node=FindBuilderNode(project.ui_tree,Convert.ToString(DesktopBlockParameter(project,state,block,"node")));if(node!=null){var child=NewBuilderNode(Convert.ToString(DesktopBlockParameter(project,state,block,"type","text")));SetBuilder(child,"content",item??DesktopBlockParameter(project,state,block,"content","item"));node.children.Add(child);}}
   else if(block.type=="control.if"){bool condition=false;try{condition=Convert.ToBoolean(DesktopBlockParameter(project,state,block,"condition",false));}catch{}if(condition&&block.body.Count>0)ExecuteDesktopBuilderChain(project,state,block.body[0],new HashSet<string>(visited),item);}
   else if(block.type=="control.for_each"){var list=DesktopBlockParameter(project,state,block,"list",state.values.ContainsKey("running_apps")?state.values["running_apps"]:null) as IEnumerable;if(list!=null)foreach(var value in list)if(block.body.Count>0)ExecuteDesktopBuilderChain(project,state,block.body[0],new HashSet<string>(visited),value);}
   else if(block.type=="control.while_loop"&&block.body.Count>0)ExecuteDesktopBuilderChain(project,state,block.body[0],new HashSet<string>(visited),item);
   id=block.next;
  }
 }
} }
