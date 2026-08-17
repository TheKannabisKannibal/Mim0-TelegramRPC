using DiscordRPC;
using DiscordRPC.Logging;
using WindowsMediaController;
using Windows.Storage.Streams;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Windows.Forms;
using Windows.Media.Control;

namespace Mim0.TelegramRPC;

internal static class Program
{
    private const string AppVersion = "1.2.2";
    private const string DiscordApplicationId = "1538974940643070062";
    private const string FallbackAssetKey = "default";
    private const string LitterboxEndpoint = "https://litterbox.catbox.moe/resources/internals/api.php";
    private const string GitHubUrl = "https://github.com/TheKannabisKannibal/Mim0-TelegramRPC";

    private static readonly string[] TelegramSourceHints = ["telegram", "ayugram", "exteragram"];
    private static readonly HttpClient Http = new();

    private static DiscordRpcClient? discord;
    private static MediaManager? mediaManager;
    private static NotifyIcon? tray;
    private static System.Windows.Forms.Timer? timer;
    private static AppSettings settings = new();
    private static string? lastSignature;
    private static string? lastTrackSignature;
    private static string? lastCoverUrl;
    private static bool updateInProgress;
    private static bool stopping;
    private static DateTime nextDiscordRetryUtc = DateTime.MinValue;
    private static string currentStatus = "запуск";
    private static string currentTrack = "—";
    private static string currentSource = "—";

    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        settings = SettingsStore.Load();
        Http.Timeout = TimeSpan.FromSeconds(20);
        Http.DefaultRequestHeaders.UserAgent.ParseAdd($"Mim0-TelegramRPC/{AppVersion}");

        tray = CreateTray();
        Application.ApplicationExit += (_, _) => Cleanup();

        try
        {
            mediaManager = new MediaManager();
            mediaManager.Start();

            timer = new System.Windows.Forms.Timer { Interval = 1000 };
            timer.Tick += async (_, _) => await UpdatePresenceSafe();
            timer.Start();

            SetTrayStatus("ожидание музыки");
            Application.Run(new ApplicationContext());
        }
        catch (Exception ex)
        {
            SetTrayStatus($"ошибка: {Limit(ex.Message, 42)}");
            MessageBox.Show($"Mim0 не удалось запустить:\n\n{ex.Message}", "Mim0 | TelegramRPC", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
        var trackItem = new ToolStripMenuItem("Музыка: ожидание") { Enabled = false };
        menu.Items.Add(statusItem);
        menu.Items.Add(trackItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Настройки", null, (_, _) => OpenSettings());
        menu.Items.Add("Проверить сейчас", null, async (_, _) => await UpdatePresenceSafe(force: true));
        menu.Items.Add("Переподключить Discord", null, (_, _) => ForceDiscordReconnect());
        menu.Items.Add("Скопировать диагностику", null, (_, _) => CopyDiagnostics());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Открыть GitHub", null, (_, _) => OpenUrl(GitHubUrl));
        menu.Items.Add("Открыть папку программы", null, (_, _) => OpenUrl(AppContext.BaseDirectory));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("О программе", null, (_, _) => ShowAbout());
        menu.Items.Add("Выход", null, (_, _) => Application.Exit());

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

    private static void ShowAbout()
    {
        MessageBox.Show(
            $"Mim0 | TelegramRPC\n\nВерсия: {AppVersion}\n\nTelegram music → Discord Rich Presence\n\nGitHub: TheKannabisKannibal/Mim0-TelegramRPC",
            "О программе",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private static void OpenSettings()
    {
        if (stopping)
            return;

        using var form = new SettingsForm(settings);
        if (form.ShowDialog() != DialogResult.OK)
            return;

        settings = form.Settings;
        lastSignature = null;
        lastTrackSignature = null;
        lastCoverUrl = null;
        SetTrayStatus("настройки сохранены");
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
            SetTrayStatus("повторная попытка");
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

        if (settings.ShowAlbumArt && lastTrackSignature != trackSignature)
        {
            lastCoverUrl = null;
            var coverPath = await SaveThumbnailAsync(props);
            if (coverPath != null)
                lastCoverUrl = await UploadToLitterboxAsync(coverPath);
            lastTrackSignature = trackSignature;
        }
        else if (!settings.ShowAlbumArt)
        {
            lastCoverUrl = null;
        }

        if (!EnsureDiscord())
            return;

        string details = FormatPresence(settings.DetailsFormat, title, artist, source);
        string state = FormatPresence(settings.StateFormat, title, artist, source);
        if (paused && settings.ShowPausedState)
            state = Limit("⏸ " + state, 128);

        var signature = $"{details}\n{state}\n{status}\n{lastCoverUrl}\n{settings.ShowProgress}";
        if (signature == lastSignature)
        {
            currentTrack = $"{title} — {artist}";
            currentSource = source;
            currentStatus = paused ? "пауза" : "играет";
            SetTrayStatus($"{currentStatus}: {Limit(title, 42)}");
            return;
        }

        var presence = new RichPresence
        {
            Type = ActivityType.Listening,
            Details = Limit(details, 128),
            State = Limit(state, 128),
            Assets = new Assets
            {
                LargeImageKey = string.IsNullOrWhiteSpace(lastCoverUrl) ? FallbackAssetKey : lastCoverUrl,
                LargeImageText = Limit($"{title} — {artist}", 128)
            }
        };

        if (playing && settings.ShowProgress)
        {
            var timeline = session.ControlSession.GetTimelineProperties();
            if (timeline.EndTime > timeline.StartTime)
            {
                var duration = timeline.EndTime - timeline.StartTime;
                var position = timeline.Position;
                if (position >= TimeSpan.Zero && position < duration)
                {
                    presence.Timestamps = new Timestamps
                    {
                        Start = DateTime.UtcNow - position,
                        End = DateTime.UtcNow + (duration - position)
                    };
                }
            }
        }

        try
        {
            discord!.SetPresence(presence);
            lastSignature = signature;
            currentTrack = $"{title} — {artist}";
            currentSource = source;
            currentStatus = paused ? "пауза" : "играет";
            SetTrayStatus($"{currentStatus}: {Limit(title, 42)}");
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

        var focused = mediaManager.GetFocusedSession();
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
            SetTrayStatus("ожидание Discord");
            return false;
        }
    }

    private static void ForceDiscordReconnect()
    {
        ResetDiscord();
        nextDiscordRetryUtc = DateTime.MinValue;
        SetTrayStatus("переподключение Discord");
    }

    private static async Task<string?> SaveThumbnailAsync(GlobalSystemMediaTransportControlsSessionMediaProperties props)
    {
        try
        {
            if (props.Thumbnail == null)
                return null;

            var dir = Path.Combine(Path.GetTempPath(), "Mim0-TelegramRPC");
            Directory.CreateDirectory(dir);
            foreach (var file in Directory.EnumerateFiles(dir, "cover_*"))
            {
                try { File.Delete(file); } catch { }
            }

            using var ras = await props.Thumbnail.OpenReadAsync();
            if (ras.Size <= 0 || ras.Size > 15 * 1024 * 1024)
                return null;

            using var reader = new DataReader(ras.GetInputStreamAt(0));
            await reader.LoadAsync((uint)ras.Size);
            var bytes = new byte[(int)ras.Size];
            reader.ReadBytes(bytes);
            if (bytes.Length == 0)
                return null;

            var path = Path.Combine(dir, "cover_" + Guid.NewGuid().ToString("N") + DetectImageExtension(bytes));
            await File.WriteAllBytesAsync(path, bytes);
            return path;
        }
        catch
        {
            return null;
        }
    }

    private static string DetectImageExtension(byte[] bytes)
    {
        if (bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF) return ".jpg";
        if (bytes.Length >= 8 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47) return ".png";
        if (bytes.Length >= 12 && bytes[0] == 'R' && bytes[1] == 'I' && bytes[2] == 'F' && bytes[3] == 'F' && bytes[8] == 'W' && bytes[9] == 'E' && bytes[10] == 'B' && bytes[11] == 'P') return ".webp";
        return ".jpg";
    }

    private static async Task<string?> UploadToLitterboxAsync(string path)
    {
        try
        {
            using var form = new MultipartFormDataContent();
            form.Add(new StringContent("fileupload"), "reqtype");
            form.Add(new StringContent("1h"), "time");
            await using var stream = File.OpenRead(path);
            using var file = new StreamContent(stream);
            file.Headers.ContentType = new MediaTypeHeaderValue(GetContentType(path));
            form.Add(file, "fileToUpload", Path.GetFileName(path));
            using var response = await Http.PostAsync(LitterboxEndpoint, form);
            if (!response.IsSuccessStatusCode)
                return null;

            var url = (await response.Content.ReadAsStringAsync()).Trim();
            return Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps ? url : null;
        }
        catch
        {
            return null;
        }
    }

    private static string GetContentType(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".png" => "image/png",
        ".webp" => "image/webp",
        _ => "image/jpeg"
    };

    private static void ClearIfNeeded()
    {
        if (discord == null || lastSignature == null)
            return;

        try { discord.ClearPresence(); } catch { }
        lastSignature = null;
        lastTrackSignature = null;
        lastCoverUrl = null;
        currentTrack = "—";
        currentSource = "—";
        currentStatus = "ожидание";
        SetTrayStatus("ожидание музыки");
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
            .AppendLine($"Settings: {SettingsStore.FileLocation}")
            .ToString();

        try
        {
            Clipboard.SetText(text);
            SetTrayStatus("диагностика скопирована");
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

    private static void SetTrayStatus(string status)
    {
        currentStatus = status;
        if (tray == null)
            return;
        try { tray.Text = Limit($"Mim0 | TelegramRPC — {status}", 63); } catch { }
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
        try { Http.Dispose(); } catch { }
        try { tray?.Dispose(); } catch { }
        discord = null;
        mediaManager = null;
        timer = null;
        tray = null;
    }
}
