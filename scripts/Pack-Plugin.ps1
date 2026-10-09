param(
  [Parameter(Mandatory=$true)][string]$Source,
  [string]$Output = ""
)
$ErrorActionPreference = 'Stop'
$sourcePath = (Resolve-Path -LiteralPath $Source).Path
$manifestPath = Join-Path $sourcePath 'manifest.json'
if (-not (Test-Path -LiteralPath $manifestPath)) { throw 'The plugin source needs manifest.json at its root.' }
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
if ($manifest.format -ne 'Awl Plugin') { throw 'manifest.json format must be "Awl Plugin".' }
if ([string]::IsNullOrWhiteSpace($Output)) {
  $safeId = ($manifest.id -replace '[^a-zA-Z0-9._-]', '-')
  $Output = Join-Path (Split-Path -Parent $sourcePath) ($safeId + '-' + $manifest.version + '.awlplugin')
}
$outputPath = [IO.Path]::GetFullPath($Output)
$temporary = $outputPath + '.zip'
Remove-Item -LiteralPath $temporary,$outputPath -Force -ErrorAction SilentlyContinue
Compress-Archive -Path (Join-Path $sourcePath '*') -DestinationPath $temporary -CompressionLevel Optimal
Move-Item -LiteralPath $temporary -Destination $outputPath
Write-Host $outputPath
