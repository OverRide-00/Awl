$ErrorActionPreference = 'Stop'
$exe = Join-Path (Split-Path -Parent $PSScriptRoot) 'dist\Awl.exe'
if (!(Test-Path -LiteralPath $exe)) { throw 'Build Awl first.' }
$startup = [Environment]::GetFolderPath('Startup')
$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut((Join-Path $startup 'Awl.lnk'))
$shortcut.TargetPath = $exe
$shortcut.WorkingDirectory = Split-Path -Parent $exe
$shortcut.Description = 'Awl dock and transparent status bar'
$shortcut.WindowStyle = 7
$shortcut.Save()
Write-Output 'Awl will start when you sign in.'
