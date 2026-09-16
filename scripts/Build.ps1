param(
 [string]$ProfileRoot = "",
 [string]$InbuiltRoot = "",
 [string]$Version = "",
 [ValidateSet('Release','Beta','Alpha')][string]$Channel = "Release",
 [switch]$AwlOnly
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($Version)) { $Version = (Get-Content (Join-Path $projectRoot 'VERSION') -Raw).Trim() }
if ($Version -notmatch '^\d+\.\d+\.\d+$') { throw 'Version must use major.minor.patch, for example 1.2.0.' }
if ([string]::IsNullOrWhiteSpace($ProfileRoot)) { $ProfileRoot = Join-Path $projectRoot 'build-profile' }
if ([string]::IsNullOrWhiteSpace($InbuiltRoot)) { $InbuiltRoot = Join-Path $projectRoot 'templates\Inbuilt Templates' }
$dist = Join-Path $projectRoot 'dist'
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
if (Test-Path -LiteralPath $InbuiltRoot) { $templates = Get-ChildItem -LiteralPath $InbuiltRoot -Filter '*.zip' | Sort-Object Name }
if ($templates.Count -eq 0) { $templates = Get-ChildItem -LiteralPath (Join-Path $projectRoot 'templates\Inbuilt Templates') -Filter '*.zip' | Sort-Object Name }
$resources = @(); $resourceIndex = 0
foreach ($template in $templates) { $resources += '/resource:' + $template.FullName + (',Awl.InbuiltTemplates.{0:D3}.zip' -f $resourceIndex); $resourceIndex++ }
foreach ($name in @('config.json','desktop-widgets.json','widget-builder.json')) { $file=Join-Path $ProfileRoot $name; if(-not(Test-Path -LiteralPath $file)){$file=Join-Path (Join-Path $projectRoot 'defaults') $name}; if(Test-Path -LiteralPath $file){$resources += '/resource:'+$file+',Awl.Defaults.'+$name} }
$iconResources = Get-ChildItem -LiteralPath (Join-Path $projectRoot 'assets\BuilderIcons') -Filter '*.svg' | Sort-Object Name | ForEach-Object { '/resource:' + $_.FullName + ',Awl.BuilderIcons.' + $_.Name }
$captureResources = Get-ChildItem -LiteralPath (Join-Path $projectRoot 'assets\CaptureRuntime') -File | Sort-Object Name | ForEach-Object { '/resource:' + $_.FullName + ',Awl.Capture.' + $_.Name }
& "$fx\csc.exe" /nologo /target:winexe /optimize+ /platform:x64 /win32icon:"$(Join-Path $projectRoot 'assets\Awl.ico')" /main:Awl.Shell /out:"$(Join-Path $dist 'Awl.exe')" @refs @resources @iconResources @captureResources @sources
if ($LASTEXITCODE -ne 0) { throw 'Awl compilation failed.' }
if (-not $AwlOnly) {
 & "$fx\csc.exe" /nologo /target:winexe /optimize+ /platform:x64 /win32icon:"$(Join-Path $projectRoot 'assets\Awl.WidgetBuilder.ico')" /main:Awl.WidgetBuilderProgram /out:"$(Join-Path $dist 'Awl.WidgetBuilder.exe')" @refs @resources @iconResources @sources
 if ($LASTEXITCODE -ne 0) { throw 'Awl Widget Builder compilation failed.' }
 & "$fx\csc.exe" /nologo /target:winexe /optimize+ /platform:x64 /win32icon:"$(Join-Path $projectRoot 'assets\Awl.DevMode.ico')" /main:Awl.DevModeProgram /out:"$(Join-Path $dist 'Awl.DevMode.exe')" @refs @resources @iconResources @captureResources @sources
 if ($LASTEXITCODE -ne 0) { throw 'Awl Dev Mode compilation failed.' }
}
Write-Host "Built Awl.exe with $resourceIndex embedded template(s)."
if (-not $AwlOnly) { Write-Host 'Built Awl.WidgetBuilder.exe and Awl.DevMode.exe.' }
Write-Host "Embedded $($iconResources.Count) SVG icons and $($captureResources.Count) capture runtime files."
