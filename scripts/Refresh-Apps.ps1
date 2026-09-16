$ErrorActionPreference = 'Stop'
$apps = @(Get-StartApps | Sort-Object Name | ForEach-Object { [pscustomobject]@{Name=$_.Name; Target=('shell:AppsFolder\' + $_.AppID)} })
$apps | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'apps.json') -Encoding UTF8
