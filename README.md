# Mim0 | TelegramRPC

**Telegram music → Discord Rich Presence**

A small Windows app that reads the currently playing track from the Windows
Media Session used by Telegram/AyuGram/ExteraGram and displays it in Discord.

## Features

- 🎵 Track title and artist
- 🖼️ Dynamic album art when Windows provides a thumbnail
- 🖼️ Fallback Discord asset when a track has no cover
- ⏱️ Live playback progress
- ⏸️ Pause state
- 🔄 Automatic track switching
- 🛑 Clears Rich Presence when playback stops
- 🚀 Optional Windows startup
- 🔔 Runs in the system tray

## Discord setup

Create a Discord Application and make sure its name is:

`Mim0 | TelegramRPC`

For the fallback image, create a Rich Presence Art Asset with the exact key:

`default`

The Application ID used by this build is already included in `Program.cs`. Keep Discord Desktop running while testing.
It is a public Discord Application ID, not a secret token.

## Download

Use the **Releases** section on GitHub for the ready-to-run Windows build.

## Build from source

Requires .NET 8 SDK.

```text
build.bat
```

The app is self-contained, so the end user does not need to install .NET 8.

The executable is created in:

```text
bin/Release/net8.0-windows10.0.17763.0/win-x64/publish/
```

## Troubleshooting

- Discord Desktop must be running for Rich Presence to appear.
- The app watches all active Windows Media sessions, not only the focused one, and prefers Telegram/AyuGram/ExteraGram sessions.
- The app uses a real WinForms message loop so the tray icon and Windows media session events keep working reliably.
- If Discord is closed or restarted, the app automatically retries the IPC connection.
- Dynamic album covers are uploaded temporarily to Litterbox because Discord Rich Presence can display external image URLs. The upload is configured to expire after one hour.

## Autostart

Run `install_startup.bat` to start the RPC with Windows.

Run `uninstall_startup.bat` to remove it.

## Privacy

The app reads the currently playing Windows Media Session metadata. When a
track has album art, the current build uploads that image temporarily to
Litterbox so Discord can display it as an external Rich Presence image.
Litterbox files are configured to expire after one hour.

No Telegram messages, chats, contacts, or audio are uploaded by this app.

## Credits / author

**Mim0**

© 2026 Mim0. All rights reserved.

## Installer

`installer.iss` is an Inno Setup script for a normal Windows installer. The
GitHub Actions workflow can build both the portable executable and the Setup
installer automatically.
