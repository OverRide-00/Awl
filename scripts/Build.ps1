param(
 [string]$ProfileRoot = "",
 [string]$InbuiltRoot = "",
 [string]$PluginRoot = "",
 [string]$IconPackRoot = "",
 [string]$DistRoot = "",
 [string]$Version = "",
 [ValidateSet('Release','Beta','Alpha')][string]$Channel = "Release",
 [switch]$AwlOnly
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$projectRoot = Split-Path -Parent $PSScriptRoot
$devTemplateBuild = -not [string]::IsNullOrWhiteSpace($InbuiltRoot)
if ([string]::IsNullOrWhiteSpace($Version)) { $Version = (Get-Content (Join-Path $projectRoot 'VERSION') -Raw).Trim() }
if ($Version -notmatch '^\d+\.\d+\.\d+$') { throw 'Version must use major.minor.patch, for example 1.2.0.' }
if ([string]::IsNullOrWhiteSpace($ProfileRoot)) { $ProfileRoot = Join-Path $projectRoot 'build-profile' }
if ([string]::IsNullOrWhiteSpace($InbuiltRoot)) { $InbuiltRoot = Join-Path $projectRoot 'templates\Inbuilt Templates' }
elseif ((Split-Path -Leaf $InbuiltRoot) -ne 'Inbuilt Templates') { $InbuiltRoot = Join-Path $InbuiltRoot 'Inbuilt Templates' }
$dist = if ([string]::IsNullOrWhiteSpace($DistRoot)) { Join-Path $projectRoot 'dist' } else { [IO.Path]::GetFullPath($DistRoot) }
New-Item -ItemType Directory -Force -Path $dist | Out-Null
$fx = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319"
$refs = @('System.dll','System.Core.dll','System.Drawing.dll','System.Windows.Forms.dll','System.Xml.dll','System.Management.dll','System.Runtime.WindowsRuntime.dll') | ForEach-Object { '/r:' + (Join-Path $fx $_) }
$refs += @('PresentationCore.dll','PresentationFramework.dll','WindowsBase.dll') | ForEach-Object { '/r:' + (Join-Path "$fx\WPF" $_) }
$winmd = Get-ChildItem 'C:\Program Files (x86)\Windows Kits\10\UnionMetadata' -Recurse -Filter Windows.winmd | Where-Object Length -gt 1000000 | Sort-Object FullName -Descending | Select-Object -First 1
if (-not $winmd) { throw 'Windows.winmd was not found.' }
$refs += '/r:' + $winmd.FullName
$refs += @('System.Runtime.dll','System.Threading.Tasks.dll','System.Runtime.InteropServices.WindowsRuntime.dll','System.Xaml.dll','System.Net.Http.dll','System.Web.Extensions.dll','System.IO.Compression.dll','System.IO.Compression.FileSystem.dll') | ForEach-Object { '/r:' + (Join-Path $fx $_) }
$generatedFolder = Join-Path $projectRoot 'artifacts\generated'
New-Item -ItemType Directory -Force -Path $generatedFolder | Out-Null
$generatedInfo = Join-Path $generatedFolder 'BuildInfo.generated.cs'
Set-Content -LiteralPath $generatedInfo -Encoding UTF8 -Value ('namespace Awl { public static class BuildInfo { public const string Version = "' + $Version + '"; public const string Channel = "' + $Channel + '"; public const string Repository = "OverRide-00/Awl"; } }')
$sources = @(Get-ChildItem (Join-Path $projectRoot 'src') -Recurse -Filter '*.cs' | Sort-Object FullName | ForEach-Object FullName) + $generatedInfo
$templates = @()
if (Test-Path -LiteralPath $InbuiltRoot) { $templates = Get-ChildItem -LiteralPath $InbuiltRoot -Filter '*.zip' -Recurse | Sort-Object FullName }
if ($templates.Count -eq 0 -and -not $devTemplateBuild) { $templates = Get-ChildItem -LiteralPath (Join-Path $projectRoot 'templates\Inbuilt Templates') -Filter '*.zip' -Recurse | Sort-Object FullName }
$profile = $null
$profileFile = Join-Path $ProfileRoot 'config.json'
if (Test-Path -LiteralPath $profileFile) { try { $profile = Get-Content -LiteralPath $profileFile -Raw | ConvertFrom-Json } catch {} }
$embeddedConfig = Join-Path $projectRoot 'defaults\config.json'
if ($devTemplateBuild) {
 $cleanConfig = Get-Content -LiteralPath $embeddedConfig -Raw | ConvertFrom-Json
 $cleanConfig | Add-Member -NotePropertyName EnabledPlugins -NotePropertyValue @() -Force
 $cleanConfig | Add-Member -NotePropertyName ActiveTemplate -NotePropertyValue '' -Force
 $cleanConfig | Add-Member -NotePropertyName StartPins -NotePropertyValue @() -Force
 $cleanConfig | Add-Member -NotePropertyName DockPins -NotePropertyValue @() -Force
 $cleanConfig | Add-Member -NotePropertyName PinnedOrder -NotePropertyValue @() -Force
 $cleanConfig | Add-Member -NotePropertyName WeatherLocation -NotePropertyValue '' -Force
 if ($profile -and $profile.IconPack) { $cleanConfig | Add-Member -NotePropertyName IconPack -NotePropertyValue ([string]$profile.IconPack) -Force }
 $embeddedConfig = Join-Path $generatedFolder 'DevBuild.config.json'
 $cleanConfig | ConvertTo-Json -Depth 100 | Set-Content -LiteralPath $embeddedConfig -Encoding UTF8
}
$resources = @(); $resourceIndex = 0
foreach ($template in $templates) { $resources += '/resource:' + $template.FullName + (',Awl.InbuiltTemplates.{0:D3}.zip' -f $resourceIndex); $resourceIndex++ }
foreach ($name in @('config.json','desktop-widgets.json','widget-builder.json')) { $file=if($devTemplateBuild){if($name -eq 'config.json'){$embeddedConfig}else{Join-Path (Join-Path $projectRoot 'defaults') $name}}else{Join-Path $ProfileRoot $name}; if(-not(Test-Path -LiteralPath $file)){$file=Join-Path (Join-Path $projectRoot 'defaults') $name}; if(Test-Path -LiteralPath $file){$resources += '/resource:'+$file+',Awl.Defaults.'+$name} }
$pluginCatalog=Join-Path $projectRoot 'plugins\catalog.json'
if(Test-Path -LiteralPath $pluginCatalog){$resources += '/resource:'+$pluginCatalog+',Awl.Plugins.catalog.json'}
$iconResources = Get-ChildItem -LiteralPath (Join-Path $projectRoot 'assets\BuilderIcons') -Filter '*.svg' | Sort-Object Name | ForEach-Object { '/resource:' + $_.FullName + ',Awl.BuilderIcons.' + $_.Name }
$captureResources = Get-ChildItem -LiteralPath (Join-Path $projectRoot 'assets\CaptureRuntime') -File | Sort-Object Name | ForEach-Object { '/resource:' + $_.FullName + ',Awl.Capture.' + $_.Name }
& "$fx\csc.exe" /nologo /target:winexe /optimize+ /platform:x64 /win32icon:"$(Join-Path $projectRoot 'assets\Awl.ico')" /main:Awl.Shell /out:"$(Join-Path $dist 'Awl.exe')" @refs @resources @iconResources @captureResources @sources
if ($LASTEXITCODE -ne 0) { throw 'Awl compilation failed.' }
$builtAssets = Join-Path $dist 'Awl.BuiltAssets'
Remove-Item -LiteralPath $builtAssets -Recurse -Force -ErrorAction SilentlyContinue
$templatePlugins = @()
if ($devTemplateBuild) {
 foreach ($template in $templates) {
  $archive = [System.IO.Compression.ZipFile]::OpenRead($template.FullName)
  try {
   $entry = $archive.GetEntry('config.json')
   if (-not $entry) { throw "Template $($template.Name) has no config.json." }
   $reader = New-Object System.IO.StreamReader($entry.Open())
   try { $templateConfig = $reader.ReadToEnd() | ConvertFrom-Json } finally { $reader.Dispose() }
   $templatePlugins += @($templateConfig.EnabledPlugins)
  } finally { $archive.Dispose() }
 }
}
if ($devTemplateBuild -and $templatePlugins.Count -gt 0 -and ([string]::IsNullOrWhiteSpace($PluginRoot) -or -not (Test-Path -LiteralPath $PluginRoot))) { throw 'Built-in templates need plugins, but the Dev Mode plugin library was not found.' }
if (-not [string]::IsNullOrWhiteSpace($PluginRoot) -and (Test-Path -LiteralPath $PluginRoot)) {
 $activePlugins = if($devTemplateBuild){@($templatePlugins | Where-Object { $_ -and $_ -match '^[a-zA-Z0-9._-]+$' } | Select-Object -Unique)}elseif($profile){@($profile.EnabledPlugins | Where-Object { $_ -and $_ -match '^[a-zA-Z0-9._-]+$' })}else{@()}
 foreach ($id in $activePlugins) {
  $source = Join-Path $PluginRoot (Join-Path 'Installed' $id)
  if (Test-Path -LiteralPath $source) {
   $destination = Join-Path $builtAssets (Join-Path 'Plugins\Installed' $id)
   New-Item -ItemType Directory -Force -Path (Split-Path -Parent $destination) | Out-Null
   Copy-Item -LiteralPath $source -Destination $destination -Recurse -Force
  } else { throw "Template plugin '$id' is missing from Dev Mode's installed plugin library." }
 }
}
if ($profile -and -not [string]::IsNullOrWhiteSpace([string]$profile.IconPack) -and -not [string]::IsNullOrWhiteSpace($IconPackRoot) -and (Test-Path -LiteralPath $IconPackRoot)) {
 $iconId = [string]$profile.IconPack
 if ($iconId -match '^[a-zA-Z0-9._-]+$') {
  $source = Join-Path $IconPackRoot (Join-Path 'Installed' $iconId)
  if (Test-Path -LiteralPath $source) {
   $destination = Join-Path $builtAssets (Join-Path 'IconPacks\Installed' $iconId)
   New-Item -ItemType Directory -Force -Path (Split-Path -Parent $destination) | Out-Null
   Copy-Item -LiteralPath $source -Destination $destination -Recurse -Force
  }
 }
}
$motionPluginRoot = Join-Path $projectRoot 'plugins\Core\animation-api'
$motionPluginSource = Join-Path $motionPluginRoot 'AnimationApiPlugin.cs'
if (Test-Path -LiteralPath $motionPluginSource) {
 $motionPluginDll = Join-Path $motionPluginRoot 'AnimationApi.dll'
 $motionPluginRefs = @('/r:' + (Join-Path $dist 'Awl.exe')) + $refs
 & "$fx\csc.exe" /nologo /target:library /optimize+ /platform:x64 /out:$motionPluginDll @motionPluginRefs $motionPluginSource
 if ($LASTEXITCODE -ne 0) { throw 'Awl Motion API plugin compilation failed.' }
 $motionManifest = Get-Content (Join-Path $motionPluginRoot 'manifest.json') -Raw | ConvertFrom-Json
 $motionPackage = Join-Path $projectRoot ('plugins\Core\' + $motionManifest.id + '-' + $motionManifest.version + '.awlplugin')
 & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $projectRoot 'scripts\Pack-Plugin.ps1') -Source $motionPluginRoot -Output $motionPackage
 if ($LASTEXITCODE -ne 0) { throw 'Awl Motion API plugin packaging failed.' }
 $motionDistribution = Join-Path $dist 'plugins\Core'
 New-Item -ItemType Directory -Force -Path $motionDistribution | Out-Null
 Copy-Item -LiteralPath $motionPackage -Destination $motionDistribution -Force
}
$ratioPluginRoot = Join-Path $projectRoot 'plugins\Core\ratio'
$ratioPluginSource = Join-Path $ratioPluginRoot 'RatioPlugin.cs'
if (Test-Path -LiteralPath $ratioPluginSource) {
 $ratioPluginDll = Join-Path $ratioPluginRoot 'RatioPlugin.dll'
 $ratioPluginRefs = @('/r:' + (Join-Path $dist 'Awl.exe')) + $refs
 & "$fx\csc.exe" /nologo /target:library /optimize+ /platform:x64 /out:$ratioPluginDll @ratioPluginRefs $ratioPluginSource
 if ($LASTEXITCODE -ne 0) { throw 'Awl Ratio plugin compilation failed.' }
 $ratioManifest = Get-Content (Join-Path $ratioPluginRoot 'manifest.json') -Raw | ConvertFrom-Json
 $ratioPackage = Join-Path $projectRoot ('plugins\Core\' + $ratioManifest.id + '-' + $ratioManifest.version + '.awlplugin')
 & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $projectRoot 'scripts\Pack-Plugin.ps1') -Source $ratioPluginRoot -Output $ratioPackage
 if ($LASTEXITCODE -ne 0) { throw 'Awl Ratio plugin packaging failed.' }
 $ratioDistribution = Join-Path $dist 'plugins\Core'
 New-Item -ItemType Directory -Force -Path $ratioDistribution | Out-Null
 Copy-Item -LiteralPath $ratioPackage -Destination $ratioDistribution -Force
}
$visualPluginRoot = Join-Path $projectRoot 'plugins\Visual'
if (Test-Path -LiteralPath $visualPluginRoot) {
 foreach ($folder in Get-ChildItem -LiteralPath $visualPluginRoot -Directory) {
  $manifestFile = Join-Path $folder.FullName 'manifest.json'
  if (-not (Test-Path -LiteralPath $manifestFile)) { continue }
  $manifest = Get-Content -LiteralPath $manifestFile -Raw | ConvertFrom-Json
  $package = Join-Path $visualPluginRoot ($manifest.id + '-' + $manifest.version + '.zip')
  & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $projectRoot 'scripts\Pack-Plugin.ps1') -Source $folder.FullName -Output $package
  if ($LASTEXITCODE -ne 0) { throw ('Visual plugin packaging failed: ' + $manifest.id) }
  $destination = Join-Path $dist 'plugins\Visual'
  New-Item -ItemType Directory -Force -Path $destination | Out-Null
  Copy-Item -LiteralPath $package -Destination $destination -Force
 }
}
$iconPackSource = Join-Path $projectRoot 'icon-packs'
if (Test-Path -LiteralPath $iconPackSource) { $iconPackDistribution = Join-Path $dist 'icon-packs'; Remove-Item -LiteralPath $iconPackDistribution -Recurse -Force -ErrorAction SilentlyContinue; Copy-Item -LiteralPath $iconPackSource -Destination $iconPackDistribution -Recurse -Force }
if (-not $AwlOnly) {
 & "$fx\csc.exe" /nologo /target:winexe /optimize+ /platform:x64 /win32icon:"$(Join-Path $projectRoot 'assets\Awl.WidgetBuilder.ico')" /main:Awl.WidgetBuilderProgram /out:"$(Join-Path $dist 'Awl.WidgetBuilder.exe')" @refs @resources @iconResources @sources
 if ($LASTEXITCODE -ne 0) { throw 'Awl Widget Builder compilation failed.' }
 & "$fx\csc.exe" /nologo /target:winexe /optimize+ /platform:x64 /win32icon:"$(Join-Path $projectRoot 'assets\Awl.DevMode.ico')" /main:Awl.DevModeProgram /out:"$(Join-Path $dist 'Awl.DevMode.exe')" @refs @resources @iconResources @captureResources @sources
 if ($LASTEXITCODE -ne 0) { throw 'Awl Dev Mode compilation failed.' }
}
if (-not $AwlOnly) {
 foreach ($entry in @(@('animation-api','AnimationApiPlugin.cs','AnimationApi.DevMode.dll'),@('ratio','RatioPlugin.cs','RatioPlugin.DevMode.dll'))) {
  $folder = Join-Path $projectRoot ('plugins\Core\' + $entry[0])
  $source = Join-Path $folder $entry[1]
  if (-not (Test-Path -LiteralPath $source)) { continue }
  $devPluginRefs = @('/r:' + (Join-Path $dist 'Awl.DevMode.exe')) + $refs
  $devPluginDll = Join-Path $folder $entry[2]
  & "$fx\csc.exe" /nologo /target:library /optimize+ /platform:x64 /out:$devPluginDll @devPluginRefs $source
  if ($LASTEXITCODE -ne 0) { throw ('Dev Mode plugin compilation failed: ' + $entry[0]) }
  $manifest = Get-Content (Join-Path $folder 'manifest.json') -Raw | ConvertFrom-Json
  $package = Join-Path $projectRoot ('plugins\Core\' + $manifest.id + '-' + $manifest.version + '.awlplugin')
  & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $projectRoot 'scripts\Pack-Plugin.ps1') -Source $folder -Output $package
  if ($LASTEXITCODE -ne 0) { throw ('Dev Mode plugin packaging failed: ' + $entry[0]) }
  Copy-Item -LiteralPath $package -Destination (Join-Path $dist 'plugins\Core') -Force
 }
}
Write-Host "Built Awl.exe with $resourceIndex embedded template(s)."
if (Test-Path -LiteralPath $motionPluginSource) { Write-Host "Built Awl Motion API plugin package." }
if (Test-Path -LiteralPath $ratioPluginSource) { Write-Host "Built Awl Ratio plugin package." }
if (-not $AwlOnly) { Write-Host 'Built Awl.WidgetBuilder.exe and Awl.DevMode.exe.' }
Write-Host "Embedded $($iconResources.Count) SVG icons and $($captureResources.Count) capture runtime files."
