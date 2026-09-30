@echo off
chcp 65001 >nul
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0install.ps1"
if errorlevel 1 (
  echo 安装未完成，请将上方错误反馈给我。
  pause
  exit /b 1
)
start "" "%LOCALAPPDATA%\LMServiceQuota\App\LMServicePet.exe"
