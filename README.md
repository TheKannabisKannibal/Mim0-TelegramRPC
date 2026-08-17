# Mim0 | TelegramRPC

**Telegram music → Discord Rich Presence for Windows**

Mim0 is a lightweight tray application that reads the current Windows Media Session and publishes the track to Discord Rich Presence.

## ✨ Features

- 🎵 Track title and artist
- 🖼️ Dynamic album art when Windows provides a thumbnail
- ⏱️ Live playback progress
- ⏸️ Pause state
- 🔄 Automatic track switching
- 🔌 Automatic Discord reconnect
- 🔎 Telegram/AyuGram/ExteraGram detection
- 🌐 Optional support for other Windows Media Session players
- ⚙️ Settings window with custom Rich Presence templates
- 🔔 System tray controls
- 🧰 Built-in diagnostics for GitHub issues
- 🚀 Optional Windows startup
- 📦 Portable build + Inno Setup installer
- ℹ️ About dialog with the current app version

## 🖥️ Requirements

- Windows 10/11 x64
- Discord Desktop
- Telegram, AyuGram, ExteraGram, or another player exposing Windows Media Session

The release build is self-contained, so end users do **not** need to install .NET 8.

## 📥 Download

Open **Releases** and download either:

- `Mim0-TelegramRPC-vX.Y.Z-win-x64.zip` — portable version
- `Mim0.TelegramRPC.Setup.exe` — normal Windows installer

The installer can optionally add Mim0 to Windows startup and create a desktop shortcut.

## ⚙️ Settings

Right-click the tray icon and choose **Настройки**.

Available options:

- Show/hide album art
- Show/hide playback progress
- Show paused state
- Telegram-only mode or all compatible Windows Media Sessions
- Start with Windows
- Custom Details and State templates

Supported placeholders:

```text
{title}
{artist}
{source}
```

Example:

```text
Details: {title}
State: 🎧 {artist}
```

Settings are stored in:

```text
%APPDATA%\Mim0\TelegramRPC\settings.json
```

## 🎮 Discord setup

The application uses a public Discord Application ID embedded in the source. It is **not a bot token or secret**.

For the fallback image, the Discord application should contain a Rich Presence asset with the exact key:

```text
default
```

Keep **Discord Desktop** running while testing.

## 🖼️ Album art and privacy

Windows Media Session may expose a track thumbnail. Discord Rich Presence cannot use an arbitrary local file as an external image, so Mim0 temporarily uploads the thumbnail to Litterbox when album art is enabled.

- Upload is performed only for the current cover.
- Files are requested to expire after one hour.
- No Telegram messages, chats, contacts, or audio are uploaded.
- You can disable album art in Settings.

If you do not want any cover image upload, turn off **Показывать обложку трека**.

## 🛠️ Build from source

Requires the **.NET 8 SDK** and Windows.

```text
build.bat
```

Or:

```powershell
dotnet restore TelegramDiscordRPC.csproj
dotnet publish TelegramDiscordRPC.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

Output:

```text
bin\Release\net8.0-windows10.0.17763.0\win-x64\publish\Mim0.TelegramRPC.exe
```

## 🤖 GitHub Actions

Pushing to `main` runs a Windows build check.

Pushing a tag such as:

```text
v1.2.2
```

builds the portable ZIP and installer and publishes both files to a GitHub Release automatically.

The workflow derives the release version from the tag, so the same workflow can be reused for future releases (`v1.2.3`, `v1.3.0`, etc.).

## 🐛 Troubleshooting

### RPC does not appear

1. Make sure Discord Desktop is running.
2. Start Mim0.
3. Start a track in Telegram.
4. Wait a few seconds.
5. Right-click the tray icon → **Проверить сейчас**.
6. If necessary, use **Переподключить Discord**.

### Telegram is playing but Mim0 sees nothing

Open Settings and make sure **Использовать только Telegram-плееры** is enabled. If your Telegram client exposes a different Windows Media Session identifier, disable that option to test all compatible sessions.

### Need a bug report

Use the tray menu → **Скопировать диагностику**, then paste the result into a GitHub Issue. Do not paste personal data or private tokens.

## 📁 Project structure

```text
Mim0-TelegramRPC/
├── .github/workflows/ci-release.yml
├── Program.cs
├── Settings.cs
├── SettingsForm.cs
├── TelegramDiscordRPC.csproj
├── installer.iss
├── build.bat
└── README.md
```

## 📜 License

This project is distributed under the license in `LICENSE`. It allows personal and non-commercial use; redistribution and commercial use are restricted without permission from Mim0.

## 👤 Author

**Mim0**

GitHub: https://github.com/TheKannabisKannibal/Mim0-TelegramRPC
