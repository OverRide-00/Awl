using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows;
using Microsoft.Win32;

namespace Awl { partial class Shell {
 void ExportWidgetPackage(){var errors=ValidateBuilder();if(errors.Count>0){MessageBox.Show(builderWindow,String.Join("\n",errors),"Fix before export");return;}var dialog=new SaveFileDialog{Filter="Awl widget package|*.zip",FileName=SafeWidgetName(builderProject.name)+".widget.zip"};if(dialog.ShowDialog(builderWindow)!=true)return;WriteWidgetPackage(dialog.FileName,builderProject);}
 string SafeWidgetName(string name){foreach(char c in Path.GetInvalidFileNameChars())name=name.Replace(c,'-');return String.IsNullOrWhiteSpace(name)?"widget":name.Trim().Replace(' ','-');}
 IEnumerable<BuilderNode> PackageNodes(WidgetBuilderProject project){return AllBuilderNodes(project.ui_tree).Concat(project.prefabs.Where(p=>p.root!=null).SelectMany(p=>AllBuilderNodes(p.root)));}
 void WriteWidgetPackage(string file,WidgetBuilderProject source){
  var project=CloneBuilderProject(source);if(File.Exists(file))File.Delete(file);
  using(var archive=ZipFile.Open(file,ZipArchiveMode.Create)){
   var used=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
   foreach(var node in PackageNodes(project)){BuilderValue property;if(!node.properties.TryGetValue("source",out property))continue;string path=Convert.ToString(property.value);if(!File.Exists(path))continue;string name=Path.GetFileName(path),candidate=name;int index=2;while(!used.Add(candidate))candidate=Path.GetFileNameWithoutExtension(name)+"-"+(index++)+Path.GetExtension(name);archive.CreateEntryFromFile(path,"assets/"+candidate,CompressionLevel.Optimal);property.value="assets/"+candidate;}
   WriteZipText(archive,"manifest.json",Pretty(new JavaScriptSerializer().Serialize(new Dictionary<string,object>{{"format","Awl Widget"},{"version",1},{"name",project.name},{"entry","widget.json"},{"style","widget.css"},{"properties","properties.json"}})));
   WriteZipText(archive,"widget.json",Pretty(new JavaScriptSerializer().Serialize(project)));WriteZipText(archive,"properties.json",WidgetPropertiesManifest(project));WriteZipText(archive,"widget.css",WidgetCss(project));
  }
 }
 void WriteZipText(ZipArchive archive,string name,string content){var entry=archive.CreateEntry(name,CompressionLevel.Optimal);using(var writer=new StreamWriter(entry.Open(),new UTF8Encoding(false)))writer.Write(content);}
 string ReadZipText(ZipArchive archive,string name){var entry=archive.GetEntry(name);if(entry==null)throw new InvalidDataException("Package is missing "+name+".");using(var reader=new StreamReader(entry.Open(),Encoding.UTF8))return reader.ReadToEnd();}
 string WidgetPropertiesManifest(WidgetBuilderProject project){
  var schemas=new Dictionary<string,object>();
  foreach(var node in AllBuilderNodes(project.ui_tree).Concat(project.prefabs.Where(p=>p.root!=null).SelectMany(p=>AllBuilderNodes(p.root))))if(!schemas.ContainsKey(node.type))schemas[node.type]=RelevantBuilderProperties(node);
  var formats=new Dictionary<string,object>();
  formats["timePresets"]=new[]{"24-hour with seconds","24-hour","12-hour with seconds","12-hour","Date and time","Custom"};
  formats["customTokens"]=new[]{"D day","H 24-hour","h 12-hour","M minute","S second","A/a meridiem","N month","Y year","W weekday"};
  var data=new Dictionary<string,object>();data["componentProperties"]=schemas;data["dataFormats"]=formats;
  return Pretty(new JavaScriptSerializer().Serialize(data));
 }
 string WidgetCss(WidgetBuilderProject project){var css=new StringBuilder("/* Awl portable widget styles */\n.widget { position: relative; overflow: hidden; }\n");foreach(var node in AllBuilderNodes(project.ui_tree)){css.Append("#").Append(node.id).Append(" {");foreach(string key in new[]{"width","height","opacity","background","color","corner_radius"}){BuilderValue value;if(!node.properties.TryGetValue(key,out value)||value.value==null)continue;string cssKey=key=="corner_radius"?"border-radius":key;string suffix=key=="width"||key=="height"||key=="corner_radius"?"px":"";css.Append(cssKey).Append(": ").Append(value.value).Append(suffix).Append("; ");}css.Append("}\n");}return css.ToString();}
 void ImportWidgetPackage(){var dialog=new OpenFileDialog{Filter="Awl widget package|*.zip"};if(dialog.ShowDialog(launcher)!=true)return;try{WidgetBuilderProject project;string folder;using(var archive=ZipFile.OpenRead(dialog.FileName)){var manifest=new JavaScriptSerializer().DeserializeObject(ReadZipText(archive,"manifest.json")) as Dictionary<string,object>;if(manifest==null||!manifest.ContainsKey("format")||Convert.ToString(manifest["format"])!="Awl Widget")throw new InvalidDataException("This is not a Awl widget package.");project=new JavaScriptSerializer().Deserialize<WidgetBuilderProject>(ReadZipText(archive,"widget.json"));if(project==null||project.ui_tree==null)throw new InvalidDataException("The package has no widget UI.");folder=Path.Combine(Root,"ImportedWidgets",SafeWidgetName(project.name)+"-"+Guid.NewGuid().ToString("N").Substring(0,6));Directory.CreateDirectory(folder);foreach(var entry in archive.Entries.Where(e=>e.FullName.StartsWith("assets/",StringComparison.OrdinalIgnoreCase)&&!String.IsNullOrEmpty(e.Name))){string target=Path.GetFullPath(Path.Combine(folder,entry.FullName.Replace('/' ,Path.DirectorySeparatorChar)));if(!target.StartsWith(Path.GetFullPath(folder)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Unsafe asset path in package.");Directory.CreateDirectory(Path.GetDirectoryName(target));entry.ExtractToFile(target,true);}}foreach(var node in PackageNodes(project)){BuilderValue property;if(!node.properties.TryGetValue("source",out property))continue;string relative=Convert.ToString(property.value);if(relative.StartsWith("assets/",StringComparison.OrdinalIgnoreCase))property.value=Path.GetFullPath(Path.Combine(folder,relative.Replace('/',Path.DirectorySeparatorChar)));}OpenWidgetBuilder();builderProject=project;builderSelected=project.ui_tree;builderSelectedBlock=null;builderHistory.Clear();builderHistoryIndex=-1;SnapshotBuilder(false);SaveBuilderProject(BuilderFile);builderTab="UI";DrawBuilder();if(launcher!=null)launcher.Hide();}catch(Exception error){MessageBox.Show(launcher,error.Message,"Widget import failed");}}
} }
