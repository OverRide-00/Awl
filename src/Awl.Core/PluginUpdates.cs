using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;

namespace Awl { partial class Shell {
 AwlCatalog repositoryPluginCatalog;
 string pluginUpdateStatus="";
 bool pluginUpdateCheckBusy,pendingPluginsApplied;
 DateTime nextPluginUpdateCheck=DateTime.MinValue;
 readonly HashSet<string> installingPluginIds=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
 event Action PluginCatalogChanged;

 static int ComparePluginVersions(string left,string right){
  var pattern=@"^v?(\d+)\.(\d+)\.(\d+)(?:\.(\d+))?(?:-([0-9A-Za-z.-]+))?(?:\+[0-9A-Za-z.-]+)?$";
  var a=Regex.Match(left??"",pattern);var b=Regex.Match(right??"",pattern);
  if(!a.Success||!b.Success)return 0;
  for(int i=1;i<=4;i++){long x,y;if(!Int64.TryParse(a.Groups[i].Value,out x))x=0;if(!Int64.TryParse(b.Groups[i].Value,out y))y=0;int c=x.CompareTo(y);if(c!=0)return c;}
  string ap=a.Groups[5].Value,bp=b.Groups[5].Value;
  if(ap==bp)return 0;if(ap.Length==0)return 1;if(bp.Length==0)return -1;
  var aa=ap.Split('.');var bb=bp.Split('.');
  for(int i=0;i<Math.Min(aa.Length,bb.Length);i++){long x,y;bool xn=Int64.TryParse(aa[i],out x),yn=Int64.TryParse(bb[i],out y);int c=xn&&yn?x.CompareTo(y):xn?-1:yn?1:String.CompareOrdinal(aa[i],bb[i]);if(c!=0)return c;}
  return aa.Length.CompareTo(bb.Length);
 }
 AwlCatalog LoadPluginCatalog(){
  var local=ReadPluginCatalog();var items=new Dictionary<string,AwlCatalogItem>(StringComparer.OrdinalIgnoreCase);
  foreach(var item in local.items??new List<AwlCatalogItem>())if(item!=null&&!String.IsNullOrEmpty(item.id))items[item.id]=item;
  if(repositoryPluginCatalog!=null)foreach(var item in repositoryPluginCatalog.items??new List<AwlCatalogItem>())if(item!=null&&!String.IsNullOrEmpty(item.id)){
   AwlCatalogItem previous;if(!items.TryGetValue(item.id,out previous)||ComparePluginVersions(item.version,previous.version)>=0)items[item.id]=item;
  }
  return new AwlCatalog{items=items.Values.ToList()};
 }
 bool PluginUpdateAvailable(AwlCatalogItem item){
  var installed=installedPlugins.FirstOrDefault(p=>String.Equals(p.Manifest.id,item.id,StringComparison.OrdinalIgnoreCase));
  return installed!=null&&!PluginUpdatePending(item.id)&&ComparePluginVersions(item.version,installed.Manifest.version)>0;
 }
 bool PluginUpdatePending(string id){return File.Exists(Path.Combine(PluginFolder,"Pending",SafePluginFileName(id),"manifest.json"));}
 async Task CheckPluginUpdates(bool force=false,TextBlock status=null){
  if(pluginUpdateCheckBusy||!force&&DateTime.UtcNow<nextPluginUpdateCheck)return;
  pluginUpdateCheckBusy=true;nextPluginUpdateCheck=DateTime.UtcNow.AddHours(1);pluginUpdateStatus="Checking plugin updates…";if(status!=null)status.Text=pluginUpdateStatus;
  try{
   string json;using(var client=new HttpClient()){client.Timeout=TimeSpan.FromSeconds(12);client.DefaultRequestHeaders.UserAgent.ParseAdd("Awl/"+BuildInfo.Version);json=await client.GetStringAsync("https://raw.githubusercontent.com/"+BuildInfo.Repository+"/main/plugins/catalog.json");}
   var catalog=new JavaScriptSerializer().Deserialize<AwlCatalog>(json);if(catalog==null||catalog.items==null)throw new InvalidDataException("Invalid plugin catalog.");
   repositoryPluginCatalog=catalog;Directory.CreateDirectory(PluginFolder);File.WriteAllText(Path.Combine(PluginFolder,"repository-catalog.json"),json);
   int count=LoadPluginCatalog().items.Count(PluginUpdateAvailable);pluginUpdateStatus=count==0?"Plugins are up to date.":count+" plugin update"+(count==1?"":"s")+" available.";if(status!=null)status.Text=pluginUpdateStatus;
   if(PluginCatalogChanged!=null)PluginCatalogChanged();
  }catch(Exception error){pluginUpdateStatus="Could not check updates. Try again.";if(status!=null)status.Text=pluginUpdateStatus;Log("Plugin update check: "+error.Message);}
  finally{pluginUpdateCheckBusy=false;if(launcher!=null&&launcher.IsVisible&&activeTab=="Config")DrawLauncher();}
 }
 void ReadCachedPluginUpdates(){
  if(repositoryPluginCatalog!=null)return;try{string path=Path.Combine(PluginFolder,"repository-catalog.json");if(File.Exists(path))repositoryPluginCatalog=new JavaScriptSerializer().Deserialize<AwlCatalog>(File.ReadAllText(path));}catch(Exception error){Log("Plugin update cache: "+error.Message);}
 }
 string CatalogInstallLabel(AwlCatalogItem item,bool detail=false){
  if(installingPluginIds.Contains(item.id))return "Installing…";
  if(PluginUpdatePending(item.id))return "Reload Awl";
  if(PluginUpdateAvailable(item))return "Update";
  var installed=installedPlugins.FirstOrDefault(p=>String.Equals(p.Manifest.id,item.id,StringComparison.OrdinalIgnoreCase));
  return installed==null?(detail?"Install plugin":"Install"):pluginRestartRequired&&PluginHasCore(installed.Manifest)?"Reload Awl":"Installed";
 }
 Button CatalogPluginButton(AwlCatalogItem item,bool detail=false){
  Button button=null;Action refresh=()=>{string label=CatalogInstallLabel(item,detail);button.Content=label=="Reload Awl"?(object)BuilderSvgIcon("reset",14,ink):label;button.ToolTip=label=="Reload Awl"?"Reload Awl to apply plugin changes":label=="Update"?"Update to "+item.version:null;button.IsEnabled=label!="Installed"&&label!="Installing…";};
  button=Action("",()=>{if(CatalogInstallLabel(item,detail)=="Reload Awl"){RestartAwl();return;}InstallCatalogPlugin(item,p=>{if(p<0)refresh();else SetCatalogInstallProgress(button,p);},restart=>{refresh();});});
  refresh();return button;
 }
 bool CatalogPackageMatches(string file,AwlCatalogItem item){try{using(var zip=ZipFile.OpenRead(file)){var entry=zip.GetEntry("manifest.json");if(entry==null)return false;using(var reader=new StreamReader(entry.Open())){var manifest=new JavaScriptSerializer().Deserialize<AwlPluginManifest>(reader.ReadToEnd());return manifest!=null&&String.Equals(manifest.id,item.id,StringComparison.OrdinalIgnoreCase)&&manifest.version==item.version;}}}catch{return false;}}
 void ReplacePluginDirectory(string prepared,string destination){
  string backup=destination+".previous-"+Guid.NewGuid().ToString("N");bool moved=false;
  try{if(Directory.Exists(destination)){Directory.Move(destination,backup);moved=true;}Directory.Move(prepared,destination);}
  catch{if(moved&&!Directory.Exists(destination))Directory.Move(backup,destination);throw;}
  if(moved)try{Directory.Delete(backup,true);}catch(Exception error){Log("Plugin backup cleanup: "+error.Message);}
 }
 bool PrepareCatalogPlugin(string package,AwlCatalogItem expected,out AwlPluginManifest manifest){
  string prepared=Path.Combine(PluginFolder,"Staging",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(prepared);
  try{
   using(var zip=ZipFile.OpenRead(package)){
    var entry=zip.GetEntry("manifest.json");if(entry==null)throw new InvalidDataException("The plugin has no manifest.json.");
    using(var reader=new StreamReader(entry.Open()))manifest=new JavaScriptSerializer().Deserialize<AwlPluginManifest>(reader.ReadToEnd());ValidatePluginManifest(manifest);
    if(!String.Equals(manifest.id,expected.id,StringComparison.OrdinalIgnoreCase)||manifest.version!=expected.version)throw new InvalidDataException("The package does not match the catalog ID and version.");
    string manifestId=manifest.id;var installed=installedPlugins.FirstOrDefault(p=>String.Equals(p.Manifest.id,manifestId,StringComparison.OrdinalIgnoreCase));
    if(installed!=null&&ComparePluginVersions(manifest.version,installed.Manifest.version)<=0)throw new InvalidDataException("This package is not newer than the installed plugin.");
    foreach(var file in zip.Entries){if(String.IsNullOrEmpty(file.Name))continue;string target=SafePluginFile(prepared,file.FullName);if(String.Equals(file.FullName,"remove.on.restart",StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Invalid package marker.");Directory.CreateDirectory(Path.GetDirectoryName(target));file.ExtractToFile(target,true);}
    bool restart=PluginHasCore(manifest)||installed!=null&&PluginHasCore(installed.Manifest);
    string destination=Path.Combine(restart?Path.Combine(PluginFolder,"Pending"):InstalledPluginFolder,manifest.id);Directory.CreateDirectory(Path.GetDirectoryName(destination));ReplacePluginDirectory(prepared,destination);return restart;
   }
  }finally{if(Directory.Exists(prepared))Directory.Delete(prepared,true);}
 }
 void ApplyPendingPluginUpdates(){
  if(pendingPluginsApplied)return;pendingPluginsApplied=true;ReadCachedPluginUpdates();string pending=Path.Combine(PluginFolder,"Pending");if(!Directory.Exists(pending))return;
  foreach(string folder in Directory.GetDirectories(pending))try{
   var manifest=new JavaScriptSerializer().Deserialize<AwlPluginManifest>(File.ReadAllText(Path.Combine(folder,"manifest.json")));ValidatePluginManifest(manifest);
   if(!String.Equals(Path.GetFileName(folder),manifest.id,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Pending plugin ID mismatch.");
   ReplacePluginDirectory(folder,Path.Combine(InstalledPluginFolder,manifest.id));
  }catch(Exception error){Log("Pending plugin update: "+error.Message);pluginRestartRequired=true;}
 }
}}
