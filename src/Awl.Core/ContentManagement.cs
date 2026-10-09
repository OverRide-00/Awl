using System;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Awl { partial class Shell {
 Button ContentUninstallButton(string name,Action remove){var button=Action("",remove);button.Content=BuilderSvgIcon("trash",15,ink);button.ToolTip="Uninstall "+name;button.Width=30;button.Height=30;button.Padding=new Thickness(5);button.Margin=new Thickness(5,0,5,0);System.Windows.Automation.AutomationProperties.SetName(button,"Uninstall "+name);return button;}
 void RemoveIconPack(string id){try{string folder=Path.GetFullPath(Path.Combine(InstalledIconPackFolder,SafeIconPackId(id)));string root=Path.GetFullPath(InstalledIconPackFolder).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;if(!folder.StartsWith(root,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Invalid icon pack path");if(Directory.Exists(folder))Directory.Delete(folder,true);if(String.Equals(cfg.IconPack,id,StringComparison.OrdinalIgnoreCase)){cfg.IconPack="";LoadActiveIconPack();SaveConfig();signature="";RefreshApps();RefreshPinnedMenu();}DrawLauncher();}catch(Exception error){MessageBox.Show(launcher,error.Message,"Icon pack could not be removed");}}
 void InstalledIconPackControls(){
  if(!Directory.Exists(InstalledIconPackFolder))return;
  foreach(string folder in Directory.GetDirectories(InstalledIconPackFolder).OrderBy(x=>x))try{
   string file=Path.Combine(folder,"icons.json");if(!File.Exists(file))continue;
   var pack=new JavaScriptSerializer().Deserialize<AwlIconPackManifest>(File.ReadAllText(file));if(pack==null)continue;pack.id=Path.GetFileName(folder);
   var row=new DockPanel{Margin=new Thickness(0,4,0,4)};string name=String.IsNullOrWhiteSpace(pack.name)?pack.id:pack.name;
   var remove=ContentUninstallButton(name,()=>RemoveIconPack(pack.id));DockPanel.SetDock(remove,Dock.Right);row.Children.Add(remove);
   bool active=String.Equals(cfg.IconPack,pack.id,StringComparison.OrdinalIgnoreCase);var use=Action(active?"Active":"Use",()=>ActivateIconPack(pack));use.IsEnabled=!active;DockPanel.SetDock(use,Dock.Right);row.Children.Add(use);row.Children.Add(BuilderIconLabel("star",name,ink,15));page.Children.Add(row);
  }catch(Exception error){Log("Installed icon pack: "+error.Message);}
 }
} }
