@echo off
setlocal
cd /d "%~dp0"
dotnet restore
if errorlevel 1 goto :fail
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
if errorlevel 1 goto :fail
echo.
echo BUILD SUCCESS
echo EXE: %CD%\bin\Release\net8.0-windows10.0.17763.0\win-x64\publish\Mim0.TelegramRPC.exe
pause
exit /b 0
:fail
echo.
echo BUILD FAILED
pause
exit /b 1
