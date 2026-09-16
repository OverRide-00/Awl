using System;
using System.Collections.Generic;

namespace Awl {
 public class BuilderValue { public object value; public string binding; }
 public class BuilderNode {
  public string id="node_"+Guid.NewGuid().ToString("N").Substring(0,8),type="panel",name="Panel";
  public List<BuilderNode> children=new List<BuilderNode>();
  public Dictionary<string,BuilderValue> properties=new Dictionary<string,BuilderValue>();
 }
 public class BuilderBlock {
  public string id="block_"+Guid.NewGuid().ToString("N").Substring(0,8),type="events.on_load",next;
  public double x=80,y=70;
  public Dictionary<string,BuilderValue> parameters=new Dictionary<string,BuilderValue>();
  public List<string> body=new List<string>();
 }
 public class BuilderVariable { public string name="value",type="string",scope="widget"; public object initial=""; }
 public class BuilderPrefabParam { public string name,target_node,property; }
 public class BuilderPrefab {
  public string id="prefab_"+Guid.NewGuid().ToString("N").Substring(0,8),name="Prefab";
  public BuilderNode root;
  public List<BuilderPrefabParam> exposed_params=new List<BuilderPrefabParam>();
 }
 public class WidgetBuilderProject {
  public int schema_version=1;
  public string name="Untitled widget";
  public BuilderNode ui_tree;
  public List<BuilderBlock> logic_graph=new List<BuilderBlock>();
  public List<BuilderVariable> variables=new List<BuilderVariable>();
  public List<BuilderPrefab> prefabs=new List<BuilderPrefab>();
 }
 public class WidgetBuilderDeployment {
  public string id="desktop_"+Guid.NewGuid().ToString("N").Substring(0,8);
  public string name="Widget";
  public WidgetBuilderProject project;
  public double x=120,y=120,relative_x=-1,relative_y=-1;
  public bool locked=false,enabled=true,hide_when_covered=true;
 }
 public class WidgetBuilderDesktopState {
  public int schema_version=1;
  public List<WidgetBuilderDeployment> widgets=new List<WidgetBuilderDeployment>();
 }
}
