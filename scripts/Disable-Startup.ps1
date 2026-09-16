$ErrorActionPreference = 'Stop'
$shortcut = Join-Path ([Environment]::GetFolderPath('Startup')) 'Awl.lnk'
if (Test-Path -LiteralPath $shortcut) { Remove-Item -LiteralPath $shortcut }
Start-Process -FilePath (Join-Path (Split-Path -Parent $PSScriptRoot) 'dist\Awl.exe') -ArgumentList '--restore' -WindowStyle Hidden -Wait
Write-Output 'Startup disabled and original Windows taskbar auto-hide setting restored.'
