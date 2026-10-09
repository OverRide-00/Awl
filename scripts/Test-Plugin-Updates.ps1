param([Parameter(Mandatory=$true)][string]$Binary,[Parameter(Mandatory=$true)][string]$Output,[Parameter(Mandatory=$true)][string]$Plugins,[switch]$DevMode)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName PresentationFramework,PresentationCore,WindowsBase,System.Drawing,System.IO.Compression.FileSystem
$assembly=[Reflection.Assembly]::LoadFrom($Binary);$type=$assembly.GetType('Awl.Shell');$flags=[Reflection.BindingFlags]'Instance,NonPublic';$static=[Reflection.BindingFlags]'Static,NonPublic'
$Output=[IO.Path]::GetFullPath($Output);$root=Join-Path $Output 'profile';[IO.Directory]::CreateDirectory($root)|Out-Null
Copy-Item -LiteralPath "$env:LOCALAPPDATA\Awl\config.json" -Destination $root
$type.GetField('Root',$static).SetValue($null,$root);if($DevMode){$type.GetField('devMode',$static).SetValue($null,$true);$type.GetField('devLibraryRoot',$static).SetValue($null,(Join-Path $root 'Library'))};$shell=[Activator]::CreateInstance($type);$shell.ShutdownMode=[Windows.ShutdownMode]::OnExplicitShutdown
if($type.GetField('configPath',$flags).GetValue($shell) -ne (Join-Path $root 'config.json')){throw 'Motion test profile isolation failed'}
function Call($name,[object[]]$values=@()){$args=[object[]]::new($values.Length);for($i=0;$i -lt $values.Length;$i++){$args[$i]=$values[$i].PSObject.BaseObject};$type.GetMethod($name,$flags).Invoke($shell,$args)}
function Field($name){$type.GetField($name,$flags).GetValue($shell)}
function SetField($name,$value){$type.GetField($name,$flags).SetValue($shell,$value)}
function Pump([double]$seconds){$frame=[Windows.Threading.DispatcherFrame]::new();$timer=[Windows.Threading.DispatcherTimer]::new();$timer.Interval=[TimeSpan]::FromSeconds($seconds);$timer.Add_Tick({$timer.Stop();$frame.Continue=$false}.GetNewClosure());$timer.Start();[Windows.Threading.Dispatcher]::PushFrame($frame)}
function Snapshot($window,$name){$window.UpdateLayout();$bitmap=[Windows.Media.Imaging.RenderTargetBitmap]::new([int][Math]::Ceiling($window.Width),[int][Math]::Ceiling($window.Height),96,96,[Windows.Media.PixelFormats]::Pbgra32);$bitmap.Render($window.Content);$encoder=[Windows.Media.Imaging.PngBitmapEncoder]::new();$encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($bitmap));$stream=[IO.File]::Create((Join-Path $Output ($name+'.png')));$encoder.Save($stream);$stream.Dispose()}
$installed=if($DevMode){Join-Path $root 'Library/Plugins/Installed'}else{Join-Path $root 'Plugins/Installed'};[IO.Directory]::CreateDirectory($installed)|Out-Null
Copy-Item -LiteralPath (Join-Path $Plugins 'Core/animation-api') -Destination (Join-Path $installed 'awl.animation-api') -Recurse
Copy-Item -LiteralPath (Join-Path $Plugins 'Visual/motion-showcase') -Destination (Join-Path $installed 'awl.motion-showcase') -Recurse
Call 'LoadConfig';$cfg=Field 'cfg';$cfg.EnabledPlugins.Clear();$cfg.EnabledPlugins.Add('awl.animation-api');$cfg.EnabledPlugins.Add('awl.motion-showcase');$cfg.Animations=$true;$cfg.Magnify=$false;$cfg.AnimationMs=240;$cfg.CloseOnBlur=$false;$cfg.MenuOpacity=1
Call 'LoadPlugins';Call 'ApplyTheme';SetField 'activeTab' 'Interface'
if(!$type.GetProperty('VisualMotionApi',$flags).GetValue($shell,$null)){throw 'Motion API dependency was not resolved'}
$type.GetField('nextPluginUpdateCheck',$flags).SetValue($shell,[DateTime]::MaxValue)
$catalogType=$assembly.GetType('Awl.AwlCatalogItem')
function CatalogItem($id,$version){$item=[Activator]::CreateInstance($catalogType);$item.id=$id;$item.version=$version;$item.name=$id;return $item}
function Package($id,$version,$kind='Visual',$escape=$false){
 $name=$id+'-'+$version+$(if($escape){'-escape'}else{''});$folder=Join-Path $Output $name;[IO.Directory]::CreateDirectory($folder)|Out-Null
 $manifest=@{format='Awl Plugin';manifestVersion=1;id=$id;name=$id;version=$version;type=$kind;style='style.css';actions=@()}
 if($kind -eq 'Core'){$manifest.actions=@(@{id='hello';title='Hello';target='hello.ps1'})}
 [IO.File]::WriteAllText((Join-Path $folder 'manifest.json'),($manifest|ConvertTo-Json -Depth 8));[IO.File]::WriteAllText((Join-Path $folder 'style.css'),':root { --card-radius: 18px; }')
 $zip=Join-Path $Output ($name+'.zip');[IO.Compression.ZipFile]::CreateFromDirectory($folder,$zip)
 if($escape){$archive=[IO.Compression.ZipFile]::Open($zip,'Update');$entry=$archive.CreateEntry('../outside.txt');$writer=[IO.StreamWriter]::new($entry.Open());$writer.Write('bad');$writer.Dispose();$archive.Dispose()}
 return $zip
}
function Prepare($zip,$item){$args=[object[]]@($zip,$item,$null);$args[0]=$zip.PSObject.BaseObject;$args[1]=$item.PSObject.BaseObject;$restart=$type.GetMethod('PrepareCatalogPlugin',$flags).Invoke($shell,$args);return $restart}
function Reload {SetField 'pluginsLoaded' $false;Call 'LoadPlugins'}
$compare=$type.GetMethod('ComparePluginVersions',$static)
foreach($pair in @(@('0.1.10','0.1.9',1),@('1.0.0','1.0.0-beta',1),@('1.0.0-beta.10','1.0.0-beta.2',1),@('1.0.0+new','1.0.0+old',0),@('0.9.0','1.0.0',-1),@('bad','1.0.0',0))){$result=$compare.Invoke($null,[object[]]@($pair[0].PSObject.BaseObject,$pair[1].PSObject.BaseObject));if([Math]::Sign($result) -ne $pair[2]){throw ('Wrong version precedence: '+$pair[0])}}
# Install a visual fixture, then update it while disabled without losing its data.
$old=Package 'test.update-visual' '1.0.0';if(Prepare $old (CatalogItem 'test.update-visual' '1.0.0')){throw 'Visual install required restart'};Reload
$data=Join-Path ($type.GetProperty('PluginFolder',$flags).GetValue($shell,$null)) 'Data/test.update-visual';[IO.Directory]::CreateDirectory($data)|Out-Null;[IO.File]::WriteAllText((Join-Path $data 'settings.json'),'keep me')
$new=Package 'test.update-visual' '1.1.0';$item=CatalogItem 'test.update-visual' '1.1.0'
if(!(Call 'PluginUpdateAvailable' @($item))){throw 'Newer version was not offered'}
$button=Call 'CatalogPluginButton' @($item,$false);if($button.Content -ne 'Update' -or !$button.IsEnabled){throw 'Update button was not offered'}
if(Prepare $new $item){throw 'Visual update incorrectly required restart'};Reload
$installed=Field 'installedPlugins';$visual=$installed|Where-Object {$_.Manifest.id -eq 'test.update-visual'}
if($visual.Enabled -or $visual.Manifest.version -ne '1.1.0'){throw 'Visual update changed disabled state or version'}
if([IO.File]::ReadAllText((Join-Path $data 'settings.json')) -ne 'keep me'){throw 'Update discarded plugin data'}
if((Call 'CatalogPluginButton' @($item,$false)).Content -ne 'Installed'){throw 'Update button did not clear after update'}
# Exercise the real async download/install pipeline on the WPF dispatcher with an isolated ZIP source.
[Threading.SynchronizationContext]::SetSynchronizationContext([Windows.Threading.DispatcherSynchronizationContext]::new([Windows.Threading.Dispatcher]::CurrentDispatcher))
$download=Package 'test.update-visual' '1.2.0';$downloadItem=CatalogItem 'test.update-visual' '1.2.0';$downloadItem.downloadUrl=([Uri]::new($download)).AbsoluteUri
$script:progressValues=[Collections.Generic.List[double]]::new();$script:finished=$false
$progress=[Action[double]]{param($value)$script:progressValues.Add($value)};$complete=[Action[bool]]{param($restart)if($restart){throw 'Downloaded Visual update needed restart'};$script:finished=$true}
Call 'InstallCatalogPlugin' @($downloadItem,$progress,$complete)
for($i=0;$i -lt 50 -and !$script:finished;$i++){Pump .05}
if(!$script:finished -or !$script:progressValues.Contains(100) -or !$script:progressValues.Contains(0)){throw 'Download/install progress or completion failed'}
$visual=Field 'installedPlugins'|Where-Object {$_.Manifest.id -eq 'test.update-visual'}
if($visual.Manifest.version -ne '1.2.0' -or $visual.Enabled){throw 'Downloaded update did not persist or enabled a disabled plugin'}
if((Field 'installingPluginIds').Count -ne 0){throw 'Install did not release its busy state'}
# Invalid ID, path traversal, version mismatch, and downgrade leave the installed package intact.
$bad=Package 'test.other' '2.0.0';$escape=Package 'test.update-visual' '2.0.0' 'Visual' $true
foreach($attempt in @(@($old,(CatalogItem 'test.update-visual' '1.0.0')),@($bad,(CatalogItem 'test.update-visual' '2.0.0')),@($escape,(CatalogItem 'test.update-visual' '2.0.0')),@($new,(CatalogItem 'test.update-visual' '1.2.0')))){
 $rejected=$false;try{Prepare $attempt[0] $attempt[1]|Out-Null}catch{$rejected=$true};if(!$rejected){throw 'Invalid update was accepted'}
 $manifest=Get-Content -LiteralPath (Join-Path $visual.Folder 'manifest.json') -Raw|ConvertFrom-Json;if($manifest.version -ne '1.2.0'){throw 'Rejected update damaged installed files'}
}
# Core installs/updates are staged until a fresh plugin-load session, in the same persistent library.
$core=Package 'test.update-core' '1.0.0' 'Core';if(!(Prepare $core (CatalogItem 'test.update-core' '1.0.0'))){throw 'Core install was not staged'}
if(!(Call 'PluginUpdatePending' @('test.update-core'))){throw 'Core staging did not persist'}
Reload;if((Field 'installedPlugins'|Where-Object {$_.Manifest.id -eq 'test.update-core'})){throw 'Core package activated without restart'}
SetField 'pendingPluginsApplied' $false;Reload
$cfg.EnabledPlugins.Add('test.update-core');Call 'SaveConfig';Reload
$coreNew=Package 'test.update-core' '1.0.1' 'Core';$coreItem=CatalogItem 'test.update-core' '1.0.1'
if(!(Prepare $coreNew $coreItem)){throw 'Core update was not staged'}
if((Call 'CatalogPluginButton' @($coreItem,$false)).ToolTip -notmatch 'Reload'){throw 'Pending update did not show Reload'}
$corePlugin=Field 'installedPlugins'|Where-Object {$_.Manifest.id -eq 'test.update-core'}
if($corePlugin.Manifest.version -ne '1.0.0'){throw 'Core update changed active code before restart'}
SetField 'pendingPluginsApplied' $false;Call 'LoadConfig';Reload
$corePlugin=Field 'installedPlugins'|Where-Object {$_.Manifest.id -eq 'test.update-core'}
if($corePlugin.Manifest.version -ne '1.0.1' -or !$corePlugin.Enabled -or (Call 'PluginUpdatePending' @('test.update-core'))){throw 'Core update did not persist through reload'}
# Local and repository catalogs merge by semantic version rather than string order.
$remote=[Activator]::CreateInstance($assembly.GetType('Awl.AwlCatalog'));$remote.items.Add((CatalogItem 'test.update-visual' '1.10.0'));SetField 'repositoryPluginCatalog' $remote
$merged=Call 'LoadPluginCatalog';if(($merged.items|Where-Object {$_.id -eq 'test.update-visual'}).version -ne '1.10.0'){throw 'Repository catalog updates were not merged'}
Write-Output 'Plugin semantic versions, Update/Reload controls, disabled-state/data persistence, live visual replacement, Core restart staging, catalog merge, package validation and rollback passed'
Call 'ResetVisualMotion';$shell.Shutdown()
