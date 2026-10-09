using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Awl { partial class Shell {
  async Task<bool> EnsureTemplatePluginsAsync(List<string> ids){
   foreach(string id in ids)if(!Regex.IsMatch(id??"",@"^[a-z0-9][a-z0-9._-]{2,63}$",RegexOptions.IgnoreCase))throw new InvalidDataException("Template has an invalid plugin ID.");
   var missing=ids.Where(id=>!File.Exists(Path.Combine(InstalledPluginFolder,id,"manifest.json"))&&!File.Exists(Path.Combine(PluginFolder,"Pending",id,"manifest.json"))).ToList();
   if(missing.Count==0)return false;
   var catalog=LoadPluginCatalog();
   if(missing.Any(id=>!catalog.items.Any(item=>String.Equals(item.id,id,StringComparison.OrdinalIgnoreCase)))){await CheckPluginUpdates(true);catalog=LoadPluginCatalog();}
   foreach(string id in missing)if(!catalog.items.Any(item=>String.Equals(item.id,id,StringComparison.OrdinalIgnoreCase)))throw new InvalidDataException("Template requires plugin '"+id+"', which is not in the plugin repository.");
   var progress=new ProgressBar{Minimum=0,Maximum=100,Height=7,Foreground=accentBrush,Background=cardBrush,Margin=new Thickness(0,12,0,0)};
   var label=Text("Preparing template plugins…",12,ink);
   var content=new StackPanel{Margin=new Thickness(20)};content.Children.Add(Text("Installing template plugins",16,ink));content.Children.Add(label);content.Children.Add(progress);
   var dialog=new Window{Title="Installing template plugins",Owner=launcher,Width=370,Height=132,WindowStartupLocation=WindowStartupLocation.CenterOwner,ResizeMode=ResizeMode.NoResize,WindowStyle=WindowStyle.None,AllowsTransparency=true,Background=Brushes.Transparent,ShowInTaskbar=false,Content=new Border{CornerRadius=new CornerRadius(16),Background=panelBrush,BorderBrush=SoftBorder(),BorderThickness=new Thickness(1),Child=content}};
   bool restart=false;dialog.Show();
   try{
    for(int i=0;i<missing.Count;i++){
     string id=missing[i];var item=catalog.items.First(x=>String.Equals(x.id,id,StringComparison.OrdinalIgnoreCase));label.Text="Installing "+item.name+" ("+(i+1)+" of "+missing.Count+")";
     string package=LocalCatalogPackage(item),temporary="";
     if(!String.IsNullOrWhiteSpace(package)&&!CatalogPackageMatches(package,item))package="";
     try{
      if(String.IsNullOrWhiteSpace(package)){
       string url=CatalogPackageUrl(item);if(String.IsNullOrWhiteSpace(url))throw new InvalidDataException("No download URL for plugin '"+id+"'.");
       temporary=Path.Combine(Path.GetTempPath(),"Awl-template-plugin-"+Guid.NewGuid().ToString("N")+".zip");
       int index=i;
       using(var client=new WebClient()){
        client.Headers.Add(HttpRequestHeader.UserAgent,"Awl/"+BuildInfo.Version);
        client.DownloadProgressChanged+=delegate(object sender,DownloadProgressChangedEventArgs args){Dispatcher.BeginInvoke(new Action(()=>progress.Value=100.0*(index+args.ProgressPercentage/100.0)/missing.Count));};
        await client.DownloadFileTaskAsync(new Uri(url),temporary);
       }
       package=temporary;
      }
      AwlPluginManifest manifest;restart|=PrepareCatalogPlugin(package,item,out manifest);progress.Value=100.0*(i+1)/missing.Count;
     }finally{if(temporary!="")try{File.Delete(temporary);}catch{}}
    }
   }finally{dialog.Close();}
   return restart;
  }
 } }
