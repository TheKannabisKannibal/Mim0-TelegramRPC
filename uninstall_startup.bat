@echo off
powershell -NoProfile -ExecutionPolicy Bypass -Command "$p=Join-Path ([Environment]::GetFolderPath('Startup')) 'Mim0.TelegramRPC.lnk'; if(Test-Path $p){Remove-Item $p -Force}; Write-Host 'Autostart removed.'"
pause
