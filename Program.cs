using DiscordRPC;
using DiscordRPC.Logging;
using WindowsMediaController;
using Windows.Storage.Streams;
using System.Net.Http.Headers;
using System.Windows.Forms;

internal static class Program
{
    // Public Discord Application ID. It is not a secret.
    private const string DiscordApplicationId = "1538974940643070062";

    // Rich Presence Art Asset used when a player does not expose a thumbnail.
    private const string FallbackAssetKey = "default";

    private const string LitterboxEndpoint = "https://litterbox.catbox.moe/resources/internals/api.php";
    private static readonly string[] TelegramSourceHints =
    [
        "telegram",
        "ayugram",
        "exteragram"
    ];

    private static DiscordRpcClient? discord;
    private static MediaManager? mediaManager;
    private static readonly HttpClient Http = new();
    private static string? lastSignature;
    private static string? lastCoverSignature;
    private static string? lastCoverUrl;
    private static bool wasPlaying;
    private static bool stopping;
    private static bool updateInProgress;
    private static NotifyIcon? tray;
    private static System.Windows.Forms.Timer? timer;
    private static DateTime nextDiscordRetryUtc = DateTime.MinValue;

    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        Http.Timeout = TimeSpan.FromSeconds(20);
        Http.DefaultRequestHeaders.UserAgent.ParseAdd("Mim0-TelegramRPC/1.1");

        tray = CreateTray();
        Application.ApplicationExit += (_, _) => Cleanup();

        try
        {
            mediaManager = new MediaManager();
            mediaManager.Start();

            // WinForms needs a real message loop. Without Application.Run the
            // tray icon and Windows media events are unreliable.
            timer = new System.Windows.Forms.Timer { Interval = 750 };
            timer.Tick += async (_, _) => await UpdatePresenceSafe();
            timer.Start();

            tray.Text = "Mim0 | TelegramRPC — работает";
            Application.Run(new ApplicationContext());
        }
        catch (Exception ex)
        {
            SetTrayStatus($"ошибка: {Limit(ex.Message, 45)}");
        }
        finally
        {
            Cleanup();
        }
    }

    private static NotifyIcon CreateTray()
    {
        var menu = new ContextMenuStrip();

        menu.Items.Add("Mim0 | TelegramRPC", null, (_, _) => { });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Открыть папку программы", null, (_, _) =>
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = AppContext.BaseDirectory,
                    UseShellExecute = true
                });
            }
            catch { }
        });
        menu.Items.Add("Проверить GitHub", null, (_, _) =>
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://github.com/",
                    UseShellExecute = true
                });
            }
            catch { }
        });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Выход", null, (_, _) => Application.Exit());

        return new NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
            Visible = true,
            Text = "Mim0 | TelegramRPC",
            ContextMenuStrip = menu
        };
    }

    private static async Task UpdatePresenceSafe()
    {
        if (stopping || updateInProgress)
            return;

        updateInProgress = true;
        try
        {
            await UpdatePresence();
        }
        catch
        {
            // A media session can disappear while we are reading it.
            // Keep the tray process alive and retry on the next tick.
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

        bool playing = status ==
            Windows.Media.Control.GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
        bool paused = status ==
            Windows.Media.Control.GlobalSystemMediaTransportControlsSessionPlaybackStatus.Paused;

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

        string title = string.IsNullOrWhiteSpace(props.Title)
            ? "Unknown track"
            : props.Title.Trim();

        string artist = string.IsNullOrWhiteSpace(props.Artist)
            ? "Unknown artist"
            : props.Artist.Trim();

        var trackSignature = $"{session.ControlSession.SourceAppUserModelId}\n{title}\n{artist}";

        if (lastCoverSignature != trackSignature)
        {
            lastCoverUrl = null;

            var coverPath = await SaveThumbnailAsync(props);
            if (coverPath != null)
                lastCoverUrl = await UploadToLitterboxAsync(coverPath);

            lastCoverSignature = trackSignature;
        }

        if (!EnsureDiscord())
            return;

        var signature = $"{title}\n{artist}\n{status}\n{lastCoverUrl}";
        if (signature == lastSignature && playing == wasPlaying)
            return;

        var presence = new RichPresence
        {
            Type = ActivityType.Listening,
            Details = Limit(title, 128),
            State = Limit(paused ? "⏸ " + artist : artist, 128),
            Assets = new Assets
            {
                LargeImageKey = string.IsNullOrWhiteSpace(lastCoverUrl)
                    ? FallbackAssetKey
                    : lastCoverUrl,
                LargeImageText = Limit($"{title} — {artist}", 128)
            }
        };

        if (playing)
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
            wasPlaying = playing;
            SetTrayStatus($"играет: {Limit(title, 45)}");
        }
        catch
        {
            ResetDiscord();
        }
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
                bool active =
                    status == Windows.Media.Control.GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing ||
                    status == Windows.Media.Control.GlobalSystemMediaTransportControlsSessionPlaybackStatus.Paused;

                if (!active)
                    continue;

                int score = 0;
                var source = session.ControlSession.SourceAppUserModelId ?? string.Empty;
                var sourceLower = source.ToLowerInvariant();

                if (TelegramSourceHints.Any(sourceLower.Contains))
                    score += 100;

                if (ReferenceEquals(session, focused))
                    score += 20;

                candidates.Add((session, score));
            }
            catch
            {
                // Session may close between enumeration and GetPlaybackInfo().
            }
        }

        return candidates
            .OrderByDescending(x => x.Score)
            .Select(x => x.Session)
            .FirstOrDefault();
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
            SetTrayStatus("Discord подключён");
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

    private static async Task<string?> SaveThumbnailAsync(
        Windows.Media.Control.GlobalSystemMediaTransportControlsSessionMediaProperties props)
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

            var ext = DetectImageExtension(bytes);
            var path = Path.Combine(dir, "cover_" + Guid.NewGuid().ToString("N") + ext);
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
        if (bytes.Length >= 3 &&
            bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
            return ".jpg";

        if (bytes.Length >= 8 &&
            bytes[0] == 0x89 && bytes[1] == 0x50 &&
            bytes[2] == 0x4E && bytes[3] == 0x47)
            return ".png";

        if (bytes.Length >= 12 &&
            bytes[0] == 'R' && bytes[1] == 'I' &&
            bytes[2] == 'F' && bytes[3] == 'F' &&
            bytes[8] == 'W' && bytes[9] == 'E' &&
            bytes[10] == 'B' && bytes[11] == 'P')
            return ".webp";

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

            return Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
                   uri.Scheme == Uri.UriSchemeHttps
                ? url
                : null;
        }
        catch
        {
            return null;
        }
    }

    private static string GetContentType(string path) =>
        Path.GetExtension(path).ToLowerInvariant() switch
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
        wasPlaying = false;
        lastCoverSignature = null;
        lastCoverUrl = null;
        SetTrayStatus("ожидание музыки");
    }

    private static void ResetDiscord()
    {
        try { discord?.ClearPresence(); } catch { }
        try { discord?.Dispose(); } catch { }

        discord = null;
        lastSignature = null;
        wasPlaying = false;
    }

    private static void SetTrayStatus(string status)
    {
        if (tray == null)
            return;

        try
        {
            tray.Text = Limit($"Mim0 | TelegramRPC — {status}", 63);
        }
        catch { }
    }

    private static string Limit(string value, int max) =>
        value.Length <= max ? value : value[..max];

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
