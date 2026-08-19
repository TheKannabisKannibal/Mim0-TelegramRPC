# Mim0 | TelegramRPC

**Telegram music → Discord Rich Presence for Windows**

[![Windows CI](https://github.com/TheKannabisKannibal/Mim0-TelegramRPC/actions/workflows/ci-release.yml/badge.svg?branch=main)](https://github.com/TheKannabisKannibal/Mim0-TelegramRPC/actions/workflows/ci-release.yml) [![Latest release](https://img.shields.io/github/v/release/TheKannabisKannibal/Mim0-TelegramRPC)](https://github.com/TheKannabisKannibal/Mim0-TelegramRPC/releases)

[**🇬🇧 English**](README.md) · [🇷🇺 Русский](README.ru.md)

Mim0 is a lightweight tray application that reads the current Windows Media Session and publishes the track to Discord Rich Presence.

## ✨ Features

- 🎵 Track title and artist
- 🖼️ Dynamic album art when Windows provides a thumbnail
- ⏱️ Live playback progress, including seek-aware timeline updates
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

## 🖼️ Screenshots

Mim0 displays the music currently playing in Telegram directly in your Discord profile, including the track title, artist, album art and playback progress.

### Discord Rich Presence

<p align="center">
  <img src="docs/screenshots/discord-profile-1.png" alt="Mim0 Discord Rich Presence profile" width="420">
  <img src="docs/screenshots/discord-profile-2.png" alt="Mim0 Discord Rich Presence with another track" width="420">
</p>

### Discord activity details

<p align="center">
  <img src="docs/screenshots/discord-activity.png" alt="Mim0 Discord activity" width="850">
</p>

### System tray

<p align="center">
  <img src="docs/screenshots/tray-menu.png" alt="Mim0 system tray menu" width="320">
</p>

### Settings

<p align="center">
  <img src="docs/screenshots/settings.png" alt="Mim0 settings window" width="650">
</p>

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

Right-click the tray icon and choose **Settings**.

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

## 🧱 Architecture

Mim0 is intentionally a **Windows-first desktop application**. Windows Media Session is the cleanest source of playback metadata on Windows, so there is no platform abstraction layer for Linux/macOS in the current product.

The runtime is split into a small set of focused responsibilities:

```text
Telegram / compatible player
          ↓
Windows Media Session
          ↓
Program.cs ───────→ Discord IPC
     │
     └────────────→ CoverService → temporary Litterbox URL
```

`Program.cs` coordinates media-session selection and presence state, while `CoverService` owns thumbnail validation, temporary upload and short-lived in-memory caching.

This keeps the application simple without introducing interfaces or dependency-injection infrastructure that would add complexity without a current benefit.

## 🎮 Discord setup

The application uses a public Discord Application ID embedded in the source. It is **not a bot token or secret**.

For the fallback image, the Discord application should contain a Rich Presence asset with the exact key:

```text
default
```

Keep **Discord Desktop** running while testing.

## 🖼️ Album art and privacy

Windows Media Session may expose a track thumbnail. Discord Rich Presence cannot use an arbitrary local file as an external image, so Mim0 temporarily uploads the thumbnail to Litterbox when album art is enabled.

- Only the current track thumbnail is uploaded.
- Files are requested to expire after one hour.
- Mim0 keeps a small in-memory cache for up to 50 minutes, so returning to a recently played track does not immediately upload the same cover again.
- The cache is never written to disk.
- No Telegram messages, chats, contacts, or audio are uploaded.
- If the upload service is unavailable, the presence falls back to the bundled `default` Discord asset.
- You can disable album art in Settings.

If you do not want any cover image upload, turn off **Show album art**.

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

The project version is centralized in `Directory.Build.props`, so the executable, About dialog and normal local builds use the same version. Release builds still take the release version from the Git tag in GitHub Actions.

Output:

```text
bin\Release\net8.0-windows10.0.17763.0\win-x64\publish\Mim0.TelegramRPC.exe
```

## 🤖 GitHub Actions

Pushing to `main` runs a Windows build check.

Pushing a tag such as:

```text
v1.5.0
```

builds the portable ZIP and installer and publishes both files to a GitHub Release automatically.

The workflow derives the release version from the tag, so the same workflow can be reused for future releases.

## 🗺️ Roadmap

### v1.5.x

- [x] Centralized version metadata
- [x] Cached temporary album-art uploads
- [x] Seek-aware Discord timeline updates
- [x] Cleaner tray localization without reflection
- [x] Expanded Telegram source detection
- [ ] More diagnostic details for media-session failures
- [ ] Better installer localization

### Future

- [ ] Optional additional Rich Presence layout presets
- [ ] More robust media-session source identification
- [ ] Further separation of media/Discord orchestration as the feature set grows

Linux/macOS support is intentionally **not** on the current roadmap; the product is built around Windows Media Session.

## 🐛 Troubleshooting

### RPC does not appear

1. Make sure Discord Desktop is running.
2. Start Mim0.
3. Start a track in Telegram.
4. Wait a few seconds.
5. Right-click the tray icon → **Check now**.
6. If necessary, use **Reconnect Discord**.

### Telegram is playing but Mim0 sees nothing

Open Settings and make sure **Use Telegram players only** is enabled. If your Telegram client exposes a different Windows Media Session identifier, disable that option to test all compatible sessions.

### Album art does not appear

The track presence itself does not depend on the cover upload. If Litterbox is unavailable or the media session exposes an unsupported thumbnail format, Mim0 keeps the RPC active and uses the bundled Discord fallback asset instead.

### Need a bug report

Use the tray menu → **Copy diagnostics**, then paste the result into a GitHub Issue. Do not paste personal data or private tokens.

## 📁 Project structure

```text
Mim0-TelegramRPC/
├── .github/workflows/ci-release.yml
├── docs/screenshots/
├── Services/
│   └── CoverService.cs
├── Directory.Build.props
├── Program.cs
├── Settings.cs
├── SettingsForm.cs
├── Localization.cs
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
