# Release checklist

- [ ] Run the project on Windows 10/11 x64.
- [ ] Verify Discord Desktop is running.
- [ ] Verify Telegram track with a cover.
- [ ] Verify Telegram track without a cover.
- [ ] Verify pause and resume.
- [ ] Verify next track.
- [ ] Verify stopping playback clears RPC.
- [ ] Close and reopen Discord; verify automatic reconnect.
- [ ] Open tray Settings and verify settings persist after restart.
- [ ] Test Telegram-only mode.
- [ ] Test all-compatible-media mode.
- [ ] Test Windows startup option.
- [ ] Run `build.bat` locally.
- [ ] Push changes to `main` and confirm GitHub Actions is green.
- [ ] Create a tag such as `v1.2.0`.
- [ ] Confirm GitHub Actions creates the Release and both assets.
- [ ] Download the Release installer on a clean Windows machine and test again.
