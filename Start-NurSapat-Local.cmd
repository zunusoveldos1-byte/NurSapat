@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Start-NurSapat-Local.ps1"
if errorlevel 1 pause
