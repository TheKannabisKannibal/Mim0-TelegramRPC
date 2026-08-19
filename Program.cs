using DiscordRPC;
using DiscordRPC.Logging;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using Windows.Media.Control;
using WindowsMediaController;

namespace Mim0.TelegramRPC;

internal static class Program
{
    private const string DiscordApplicationId = "1538974940643070062";
    private const string FallbackAssetKey = "default";
    private const string GitHubUrl = "https://github.com/TheKannabisKannibal/Mim0-TelegramRPC";
    private const string GitHubButtonLabel = "Mim0 на GitHub";

    private static readonly string[] TelegramSourceHints =
        ["telegram", "telegramdesktop", "org.telegram.desktop", "ayugram", "exteragram"];

    private static readonly CoverService CoverService = new(GetAppVersion());

    private static DiscordRpcClient? discord;
    private static MediaManager? mediaManager;
    private static NotifyIcon? tray;
    private static System.Windows.Forms.Timer? timer;
    private static ToolStripMenuItem? trackItem;
    private static AppSettings settings = new();
    private static string? lastSignature;
    private static bool updateInProgress;
    private static bool stopping;
    private static DateTime nextDiscordRetryUtc = DateTime.MinValue;
    private static string currentStatus = "запуск";
    private static string currentTrack = "—";
    private static string currentSource = "—";
    private static bool currentPaused;

    private static string AppVersion => GetAppVersion();

    private static string GetAppVersion() =>
        Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.5.0";

    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        settings = SettingsStore.Load();

        tray = CreateTray();
        Application.ApplicationExit += (_, _) => Cleanup();

        try
        {
            mediaManager = new MediaManager();
            mediaManager.Start();

            timer = new System.Windows.Forms.Timer { Interval = 1000 };
            timer.Tick += async (_, _) => await UpdatePresenceSafe();
            timer.Start();

            SetTrayStatus(Localization.WaitingMusic);
            Application.Run(new ApplicationContext());
        }
        catch (Exception ex)
        {
            SetTrayStatus($"{Localization.Error}: {Limit(ex.Message, 42)}");
            MessageBox.Show(
                $"{Localization.Error}: {ex.Message}",
                "Mim0 | TelegramRPC",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            Cleanup();
        }
    }

    private static NotifyIcon CreateTray()
    {
        var menu = new ContextMenuStrip();
        var statusItem = new ToolStripMenuItem("Mim0 | TelegramRPC") { Enabled = false };
        trackItem = new ToolStripMenuItem(Localization.MusicWaiting) { Enabled = false };

        menu.Items.Add(statusItem);
        menu.Items.Add(trackItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(Localization.SettingsMenu, null, (_, _) => OpenSettings());
        menu.Items.Add(Localization.CheckNow, null, async (_, _) => await UpdatePresenceSafe(force: true));
        menu.Items.Add(Localization.ReconnectDiscord, null, (_, _) => ForceDiscordReconnect());
        menu.Items.Add(Localization.CopyDiagnostics, null, (_, _) => CopyDiagnostics());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(Localization.OpenGitHub, null, (_, _) => OpenUrl(GitHubUrl));
        menu.Items.Add(Localization.OpenProgramFolder, null, (_, _) => OpenUrl(AppContext.BaseDirectory));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(Localization.About, null, (_, _) => ShowAbout());
        menu.Items.Add(Localization.Exit, null, (_, _) => Application.Exit());

        tray = new NotifyIcon
        {
            Icon = System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath!)
                ?? System.Drawing.SystemIcons.Application,
            Visible = true,
            Text = $"Mim0 | TelegramRPC v{AppVersion}",
            ContextMenuStrip = menu
        };

        tray.MouseDoubleClick += (_, _) => OpenSettings();
        return tray;
    }

    internal static void RefreshTrayLocalization()
    {
        if (tray?.ContextMenuStrip == null)
            return;

        var items = tray.ContextMenuStrip.Items;
        if (items.Count < 13)
            return;

        items[0].Text = "Mim0 | TelegramRPC";
        items[1].Text = currentTrack == "—"
            ? Localization.MusicWaiting
            : $"{(currentPaused ? "⏸" : "▶")} {currentTrack}";
        items[3].Text = Localization.SettingsMenu;
        items[4].Text = Localization.CheckNow;
        items[5].Text = Localization.ReconnectDiscord;
        items[6].Text = Localization.CopyDiagnostics;
        items[8].Text = Localization.OpenGitHub;
        items[9].Text = Localization.OpenProgramFolder;
        items[11].Text = Localization.About;
        items[12].Text = Localization.Exit;

        RefreshTrayTooltip();
    }

    private static void RefreshTrayTooltip()
    {
        if (tray == null)
            return;

        var status = string.IsNullOrWhiteSpace(currentStatus) ? Localization.WaitingMusic : currentStatus;
        try { tray.Text = Limit($"Mim0 | TelegramRPC — {status}", 63); } catch { }
    }

    private static void ShowAbout()
    {
        MessageBox.Show(
            Localization.AboutText(AppVersion),
            Localization.AboutTitle,
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private static void OpenSettings()
    {
        if (stopping)
            return;

        var window = new ModernSettingsWindow(settings);
        var result = window.ShowDialog();
        if (result != true)
            return;

        settings = window.Settings;
        lastSignature = null;
        CoverService.Clear();
        SetTrayStatus(Localization.SettingsSaved);
    }

    private static async Task UpdatePresenceSafe(bool force = false)
    {
        if (stopping || updateInProgress)
            return;

        if (force)
            lastSignature = null;

        updateInProgress = true;
        try
        {
            await UpdatePresence();
        }
        catch
        {
            SetTrayStatus(Localization.Retry);
        }
        finally
        {
            updateInProgress = false;
        }
    }

    private static async Task UpdatePresence()
    {
        if (mediaManager == null)
            return;

        var session = FindBestMediaSession();
        if (session == null)
        {
            ClearIfNeeded();
            return;
        }

        var playback = session.ControlSession.GetPlaybackInfo();
        var status = playback.PlaybackStatus;
        bool playing = status == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
        bool paused = status == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Paused;

        if (!playing && !paused)
        {
            ClearIfNeeded();
            return;
        }

        var props = await session.ControlSession.TryGetMediaPropertiesAsync();
        if (props == null)
        {
            ClearIfNeeded();
            return;
        }

        string title = string.IsNullOrWhiteSpace(props.Title) ? "Unknown track" : props.Title.Trim();
        string artist = string.IsNullOrWhiteSpace(props.Artist) ? "Unknown artist" : props.Artist.Trim();
        string source = session.ControlSession.SourceAppUserModelId ?? "Unknown source";
        string trackSignature = $"{source}\n{title}\n{artist}";

        if (!EnsureDiscord())
            return;

        string? coverUrl = null;
        if (settings.ShowAlbumArt)
            coverUrl = await CoverService.GetCoverUrlAsync(props, trackSignature);

        string details = FormatPresence(settings.DetailsFormat, title, artist, source);
        string state = FormatPresence(settings.StateFormat, title, artist, source);
        if (paused && settings.ShowPausedState)
            state = Limit("⏸ " + state, 128);

        TimeSpan? position = null;
        TimeSpan? duration = null;

        if (playing && settings.ShowProgress)
        {
            try
            {
                var timeline = session.ControlSession.GetTimelineProperties();
                if (timeline.EndTime > timeline.StartTime)
                {
                    duration = timeline.EndTime - timeline.StartTime;
                    var timelinePosition = timeline.Position;
                    if (timelinePosition >= TimeSpan.Zero && timelinePosition < duration.Value)
                        position = timelinePosition;
                }
            }
            catch
            {
                // Timeline may disappear while the media session changes.
            }
        }

        // Include the current position so seeking is reflected in Discord.
        var positionSignature = position.HasValue
            ? position.Value.TotalSeconds.ToString("F0")
            : "none";
        var signature = $"{details}\n{state}\n{status}\n{coverUrl}\n{settings.ShowProgress}\n{positionSignature}\n{GitHubUrl}";

        if (signature == lastSignature)
        {
            currentTrack = $"{title} — {artist}";
            currentSource = source;
            SetPlaybackStatus(paused ? Localization.PausedStatus : Localization.Playing, currentTrack);
            return;
        }

        var presence = new RichPresence
        {
            Type = ActivityType.Listening,
            Details = Limit(details, 128),
            State = Limit(state, 128),
            Assets = new Assets
            {
                LargeImageKey = string.IsNullOrWhiteSpace(coverUrl) ? FallbackAssetKey : coverUrl,
                LargeImageText = Limit($"{title} — {artist}", 128),
                SmallImageKey = FallbackAssetKey,
                SmallImageText = "Mim0 | TelegramRPC"
            },
            Buttons =
            [
                new Button
                {
                    Label = GitHubButtonLabel,
                    Url = GitHubUrl
                }
            ]
        };

        if (position.HasValue && duration.HasValue)
        {
            presence.Timestamps = new Timestamps
            {
                Start = DateTime.UtcNow - position.Value,
                End = DateTime.UtcNow + (duration.Value - position.Value)
            };
        }

        try
        {
            discord!.SetPresence(presence);
            lastSignature = signature;
            currentTrack = $"{title} — {artist}";
            currentSource = source;
            SetPlaybackStatus(paused ? Localization.PausedStatus : Localization.Playing, currentTrack);
        }
        catch
        {
            ResetDiscord();
        }
    }

    private static string FormatPresence(string format, string title, string artist, string source)
    {
        var sourceName = source.Split(['!', '_'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? source;
        var result = format
            .Replace("{title}", title, StringComparison.OrdinalIgnoreCase)
            .Replace("{artist}", artist, StringComparison.OrdinalIgnoreCase)
            .Replace("{source}", sourceName, StringComparison.OrdinalIgnoreCase);
        return string.IsNullOrWhiteSpace(result) ? title : result.Trim();
    }

    private static MediaManager.MediaSession? FindBestMediaSession()
    {
        if (mediaManager == null)
            return null;

        MediaManager.MediaSession? focused = null;
        try { focused = mediaManager.GetFocusedSession(); } catch { }

        var candidates = new List<(MediaManager.MediaSession Session, int Score)>();

        foreach (var session in mediaManager.CurrentMediaSessions.Values)
        {
            try
            {
                var status = session.ControlSession.GetPlaybackInfo().PlaybackStatus;
                bool active = status == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing ||
                              status == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Paused;
                if (!active)
                    continue;

                var source = session.ControlSession.SourceAppUserModelId ?? string.Empty;
                var sourceLower = source.ToLowerInvariant();
                bool isTelegram = TelegramSourceHints.Any(sourceLower.Contains);

                if (settings.TelegramOnly && !isTelegram)
                    continue;

                int score = isTelegram ? 100 : 10;
                if (ReferenceEquals(session, focused))
                    score += 20;
                candidates.Add((session, score));
            }
            catch
            {
                // Session may disappear while being enumerated.
            }
        }

        return candidates.OrderByDescending(x => x.Score).Select(x => x.Session).FirstOrDefault();
    }

    private static bool EnsureDiscord()
    {
        if (discord != null && discord.IsInitialized)
            return true;

        if (DateTime.UtcNow < nextDiscordRetryUtc)
            return false;

        ResetDiscord();
        try
        {
            discord = new DiscordRpcClient(DiscordApplicationId)
            {
                Logger = new ConsoleLogger(LogLevel.Warning, false)
            };

            if (!discord.Initialize())
                throw new InvalidOperationException("Discord IPC is unavailable.");

            nextDiscordRetryUtc = DateTime.MinValue;
            return true;
        }
        catch
        {
            try { discord?.Dispose(); } catch { }
            discord = null;
            nextDiscordRetryUtc = DateTime.UtcNow.AddSeconds(5);
            SetTrayStatus(Localization.WaitingDiscord);
            return false;
        }
    }

    private static void ForceDiscordReconnect()
    {
        ResetDiscord();
        nextDiscordRetryUtc = DateTime.MinValue;
        SetTrayStatus(Localization.ReconnectingDiscord);
    }

    private static void ClearIfNeeded()
    {
        if (discord != null && lastSignature != null)
        {
            try { discord.ClearPresence(); } catch { }
        }

        lastSignature = null;
        currentTrack = "—";
        currentSource = "—";
        SetPlaybackStatus(Localization.WaitingMusic, "—");
    }

    private static void ResetDiscord()
    {
        try { discord?.ClearPresence(); } catch { }
        try { discord?.Dispose(); } catch { }
        discord = null;
        lastSignature = null;
    }

    private static void CopyDiagnostics()
    {
        var text = new StringBuilder()
            .AppendLine($"Mim0 | TelegramRPC {AppVersion}")
            .AppendLine($"OS: {Environment.OSVersion}")
            .AppendLine($"64-bit process: {Environment.Is64BitProcess}")
            .AppendLine($"Discord: {(discord?.IsInitialized == true ? "connected" : "disconnected")}")
            .AppendLine($"Status: {currentStatus}")
            .AppendLine($"Track: {currentTrack}")
            .AppendLine($"Source: {currentSource}")
            .AppendLine($"Telegram only: {settings.TelegramOnly}")
            .AppendLine($"Album art: {settings.ShowAlbumArt}")
            .AppendLine($"Progress: {settings.ShowProgress}")
            .AppendLine($"Settings: {SettingsStore.FileLocation}")
            .ToString();

        try
        {
            Clipboard.SetText(text);
            SetTrayStatus(Localization.DiagnosticsCopied);
        }
        catch { }
    }

    private static void OpenUrl(string path)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
        }
        catch { }
    }

    private static void SetPlaybackStatus(string status, string track)
    {
        currentStatus = status;
        currentTrack = track;
        currentPaused = status == Localization.PausedStatus;

        if (trackItem != null)
        {
            try
            {
                trackItem.Text = track == "—"
                    ? Localization.MusicWaiting
                    : $"{(currentPaused ? "⏸" : "▶")} {track}";
            }
            catch { }
        }

        RefreshTrayTooltip();
    }

    private static void SetTrayStatus(string status)
    {
        currentStatus = status;
        RefreshTrayTooltip();
    }

    private static string Limit(string value, int max) => value.Length <= max ? value : value[..max];

    private static void Cleanup()
    {
        if (stopping)
            return;

        stopping = true;
        try { timer?.Stop(); } catch { }
        try { discord?.ClearPresence(); } catch { }
        try { discord?.Dispose(); } catch { }
        try { CoverService.Dispose(); } catch { }
        try { tray?.Dispose(); } catch { }
        discord = null;
        mediaManager = null;
        timer = null;
        tray = null;
        trackItem = null;
    }
}
