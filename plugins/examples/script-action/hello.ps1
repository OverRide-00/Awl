$stamp = Get-Date -Format o
Set-Content -LiteralPath (Join-Path $PSScriptRoot 'last-run.txt') -Value $stamp
