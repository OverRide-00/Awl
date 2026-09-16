@echo off
start "" /wait "%~dp0..\dist\Awl.exe" --restore
timeout /t 1 /nobreak >nul
start "" "%~dp0..\dist\Awl.exe" --launcher
