# Manual testing — v1.2.0

Use a Windows 10/11 x64 machine with Discord Desktop and Telegram installed.

## 1. First launch

- [ ] Start `Mim0.TelegramRPC.exe`.
- [ ] Confirm the tray icon appears.
- [ ] Confirm the process stays alive after the console/launcher closes.
- [ ] Right-click the tray icon and open Settings.

## 2. Telegram playback

- [ ] Start a track with a cover.
- [ ] Wait 2–5 seconds.
- [ ] Confirm Discord shows the track title and artist.
- [ ] Confirm the cover appears.
- [ ] Confirm the progress timer moves.

## 3. Playback controls

- [ ] Pause the track.
- [ ] Confirm RPC remains visible and shows `⏸`.
- [ ] Resume.
- [ ] Confirm the timer starts again.
- [ ] Skip to another track.
- [ ] Confirm title, artist, cover, and timer update.
- [ ] Stop playback completely.
- [ ] Confirm RPC disappears.

## 4. Discord reconnect

- [ ] Start playback.
- [ ] Fully close Discord Desktop.
- [ ] Confirm Mim0 remains in the tray.
- [ ] Start Discord again.
- [ ] Confirm RPC returns without restarting Mim0.
- [ ] If needed, use **Переподключить Discord** from the tray menu.

## 5. Settings

- [ ] Disable album art and verify no cover is uploaded/displayed.
- [ ] Disable progress and verify the timer disappears.
- [ ] Change Details/State templates.
- [ ] Restart Mim0 and confirm settings persist.
- [ ] Toggle Telegram-only mode and test a non-Telegram Windows Media Session.
- [ ] Toggle Start with Windows and verify the shortcut/registry startup behavior.

## 6. Diagnostics

- [ ] Use **Скопировать диагностику**.
- [ ] Paste into a text editor and confirm it contains version, Discord state, track, source, and settings path.
- [ ] Do not publish private information in an issue.

## 7. Installer

- [ ] Build with GitHub Actions or Inno Setup.
- [ ] Install on a clean Windows user profile.
- [ ] Verify the Start Menu shortcut.
- [ ] Verify optional desktop shortcut.
- [ ] Verify optional Windows startup.
- [ ] Uninstall and verify the application is removed.

## 8. Release

- [ ] Push to `main` and verify Actions is green.
- [ ] Create tag `v1.2.0`.
- [ ] Verify Actions creates the GitHub Release.
- [ ] Download the ZIP and installer from Releases.
- [ ] Run the downloaded build on a clean machine.
