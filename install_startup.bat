@echo off
set "EXE=%~dp0bin\Release\net8.0-windows10.0.17763.0\win-x64\publish\Mim0.TelegramRPC.exe"
if not exist "%EXE%" (
  echo EXE not found. Run build.bat first.
  pause
  exit /b 1
)
powershell -NoProfile -ExecutionPolicy Bypass -Command "$startup=[Environment]::GetFolderPath('Startup'); $w=New-Object -ComObject WScript.Shell; $s=$w.CreateShortcut((Join-Path $startup 'Mim0.TelegramRPC.lnk')); $s.TargetPath='%EXE%'; $s.WorkingDirectory='%~dp0bin\Release\net8.0-windows10.0.17763.0\win-x64\publish'; $s.Save()"
echo Autostart installed.
pause
