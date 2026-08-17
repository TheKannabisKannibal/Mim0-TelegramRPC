#define MyAppName "Mim0 | TelegramRPC"
#ifndef MyAppVersion
#define MyAppVersion "1.2.2"
#endif
#define MyAppPublisher "Mim0"
#define MyAppExeName "Mim0.TelegramRPC.exe"

[Setup]
AppId={{8C0A6C2F-8E6A-4D9C-A5D1-7F7D9C4A2E31}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\Mim0\TelegramRPC
DefaultGroupName={#MyAppName}
OutputDir=installer-output
OutputBaseFilename=Mim0.TelegramRPC.Setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
SetupIconFile=assets\Mim0.ico
UninstallDisplayIcon={app}\{#MyAppExeName}

[Files]
Source: "bin\Release\net8.0-windows10.0.17763.0\win-x64\publish\Mim0.TelegramRPC.exe"; DestDir: "{app}"; Flags: ignoreversion

[Tasks]
Name: "startup"; Description: "Запускать Mim0 | TelegramRPC вместе с Windows"; GroupDescription: "Дополнительные параметры:"
Name: "desktopicon"; Description: "Создать ярлык на рабочем столе"; GroupDescription: "Дополнительные параметры:"

[Icons]
Name: "{group}\Mim0 | TelegramRPC"; Filename: "{app}\{#MyAppExeName}"; Comment: "Mim0 | TelegramRPC v{#MyAppVersion}"
Name: "{autodesktop}\Mim0 | TelegramRPC"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon; Comment: "Mim0 | TelegramRPC v{#MyAppVersion}"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Запустить Mim0 | TelegramRPC"; Flags: nowait postinstall skipifsilent

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "Mim0.TelegramRPC"; ValueData: "{app}\{#MyAppExeName}"; Flags: uninsdeletevalue; Tasks: startup
