param(
 [Parameter(Mandatory=$true)][string]$Source,
 [string]$Output = ""
)
$ErrorActionPreference = 'Stop'
$sourcePath = (Resolve-Path -LiteralPath $Source).Path
foreach($required in @('icons.json','icon.png')) { if(-not(Test-Path -LiteralPath (Join-Path $sourcePath $required))){throw "An icon pack needs $required at its root."} }
$manifest = Get-Content -LiteralPath (Join-Path $sourcePath 'icons.json') -Raw | ConvertFrom-Json
if([string]::IsNullOrWhiteSpace($Output)){$id=if([string]::IsNullOrWhiteSpace($manifest.id)){Split-Path -Leaf $sourcePath}else{$manifest.id};$Output=Join-Path (Split-Path -Parent $sourcePath) ($id+'.iconpack')}
$outputPath=[IO.Path]::GetFullPath($Output);$temporary=$outputPath+'.zip';Remove-Item -LiteralPath $temporary,$outputPath -Force -ErrorAction SilentlyContinue;Compress-Archive -Path (Join-Path $sourcePath '*') -DestinationPath $temporary -CompressionLevel Optimal;Move-Item -LiteralPath $temporary -Destination $outputPath;Write-Host $outputPath
