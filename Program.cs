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

        var title = props.Title?.Trim() ?? string.Empty;
        var artist = props.Artist?.Trim() ?? string.Empty;
        var source = session.SourceAppUserModelId ?? string.Empty;
        if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(artist))
        {
            ClearIfNeeded();
            return;
        }

        var details = FormatTemplate(settings.DetailsFormat, title, artist, source);
        var state = FormatTemplate(settings.StateFormat, title, artist, source);
        var signature = $"{details}|{state}|{playing}|{paused}|{settings.ShowAlbumArt}|{settings.ShowProgress}|{settings.ShowPausedState}";

        currentTrack = title.Length > 0 ? title : "—";
        currentSource = source.Length > 0 ? source : "—";
        currentPaused = paused;
        currentStatus = paused ? Localization.PausedStatus : Localization.Playing;
        SetTrayTrack(currentTrack, currentPaused);

        if (signature == lastSignature && discord?.IsInitialized == true)
            return;

        EnsureDiscordConnected();
        if (discord == null || !discord.IsInitialized)
            return;

        var activity = new RichPresence
        {
            Details = Limit(details, 128),
            State = Limit(state, 128),
            Assets = new Assets { LargeImageKey = FallbackAssetKey, LargeImageText = "Mim0 | TelegramRPC" },
            Buttons = [new Button { Label = GitHubButtonLabel, Url = GitHubUrl }]
        };

        if (settings.ShowPausedState && paused)
            activity.State = Limit($"⏸ {state}", 128);

        if (settings.ShowProgress)
        {
            var timeline = session.ControlSession.GetTimelineProperties();
            var position = timeline.Position.TotalSeconds;
            var duration = timeline.EndTime.TotalSeconds;
            if (duration > 0 && position >= 0 && position < duration)
            {
                var now = DateTime.UtcNow;
                activity.Timestamps = new Timestamps
                {
                    Start = now.AddSeconds(-position),
                    End = now.AddSeconds(duration - position)
                };
            }
        }

        discord.SetPresence(activity);
        lastSignature = signature;
    }

    private static GlobalSystemMediaTransportControlsSession? FindBestMediaSession()
    {
        if (mediaManager == null)
            return null;

        var sessions = mediaManager.GetSessions();
        var candidates = sessions
            .Where(s => !settings.TelegramOnly || IsTelegramSource(s.SourceAppUserModelId))
            .ToList();

        return candidates
            .OrderByDescending(s => s.ControlSession.GetPlaybackInfo().PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing)
            .FirstOrDefault();
    }

    private static bool IsTelegramSource(string source)
    {
        var normalized = source.ToLowerInvariant();
        return TelegramSourceHints.Any(h => normalized.Contains(h));
    }

    private static string FormatTemplate(string template, string title, string artist, string source)
    {
        return (template ?? string.Empty)
            .Replace("{title}", title)
            .Replace("{artist}", artist)
            .Replace("{source}", source);
    }

    private static void EnsureDiscordConnected()
    {
        if (discord?.IsInitialized == true || DateTime.UtcNow < nextDiscordRetryUtc)
            return;

        try
        {
            discord?.Dispose();
            discord = new DiscordRpcClient(DiscordApplicationId)
            {
                Logger = new ConsoleLogger(LogLevel.Warning, true)
            };
            discord.Initialize();
            nextDiscordRetryUtc = DateTime.MinValue;
        }
        catch
        {
            nextDiscordRetryUtc = DateTime.UtcNow.AddSeconds(10);
            discord = null;
            SetTrayStatus(Localization.WaitingDiscord);
        }
    }

    private static void ForceDiscordReconnect()
    {
        nextDiscordRetryUtc = DateTime.MinValue;
        try { discord?.Dispose(); } catch { }
        discord = null;
        lastSignature = null;
        SetTrayStatus(Localization.ReconnectingDiscord);
    }

    private static void ClearIfNeeded()
    {
        if (discord?.IsInitialized == true && lastSignature != null)
        {
            try { discord.ClearPresence(); } catch { }
        }

        lastSignature = null;
        currentTrack = "—";
        currentSource = "—";
        currentPaused = false;
        currentStatus = Localization.WaitingMusic;
        SetTrayTrack("—", false);
    }

    private static void SetTrayTrack(string track, bool paused)
    {
        if (trackItem == null)
            return;

        try
        {
            trackItem.Text = track == "—"
                ? Localization.MusicWaiting
                : $"{(paused ? "⏸" : "▶")} {Limit(track, 48)}";
        }
        catch { }
    }

    private static void SetTrayStatus(string status)
    {
        currentStatus = status;
        RefreshTrayTooltip();
    }

    private static void CopyDiagnostics()
    {
        var version = AppVersion;
        var os = Environment.OSVersion.VersionString;
        var source = currentSource;
        var diagnostics = $"Mim0 | TelegramRPC {version}\n" +
                          $"OS: {os}\n" +
                          $"64-bit process: {Environment.Is64BitProcess}\n" +
                          $"Discord: {(discord?.IsInitialized == true ? "connected" : "disconnected")}\n" +
                          $"Status: {currentStatus}\n" +
                          $"Track: {currentTrack}\n" +
                          $"Source: {source}\n" +
                          $"Telegram only: {settings.TelegramOnly}\n" +
                          $"Album art: {settings.ShowAlbumArt}\n" +
                          $"Progress: {settings.ShowProgress}\n" +
                          $"Settings: {SettingsStore.FileLocation}";

        Clipboard.SetText(diagnostics);
        SetTrayStatus(Localization.DiagnosticsCopied);
    }

    private static void OpenUrl(string url)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch { }
    }

    private static string Limit(string value, int max) =>
        value.Length <= max ? value : value[..Math.Max(0, max - 1)] + "…";

    private static void Cleanup()
    {
        if (stopping)
            return;

        stopping = true;
        try { timer?.Stop(); } catch { }
        try { mediaManager?.Dispose(); } catch { }
        try { discord?.Dispose(); } catch { }
        try { tray?.Dispose(); } catch { }
    }
}
