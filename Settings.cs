using System.Text.Json;
using Microsoft.Win32;

namespace Mim0.TelegramRPC;

internal sealed class AppSettings
{
    public bool ShowAlbumArt { get; set; } = true;
    public bool ShowProgress { get; set; } = true;
    public bool ShowPausedState { get; set; } = true;
    public bool TelegramOnly { get; set; } = true;
    public bool StartWithWindows { get; set; } = false;
    public string DetailsFormat { get; set; } = "{title}";
    public string StateFormat { get; set; } = "{artist}";
    public string Language { get; set; } = Localization.DetectDefaultLanguage();

    public AppSettings Clone() => new()
    {
        ShowAlbumArt = ShowAlbumArt,
        ShowProgress = ShowProgress,
        ShowPausedState = ShowPausedState,
        TelegramOnly = TelegramOnly,
        StartWithWindows = StartWithWindows,
        DetailsFormat = DetailsFormat,
        StateFormat = StateFormat,
        Language = Language
    };
}

internal static class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static readonly string DirectoryPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Mim0",
        "TelegramRPC");
    private static readonly string FilePath = Path.Combine(DirectoryPath, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(FilePath))
                return new AppSettings();

            return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath), JsonOptions)
                   ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public static void Save(AppSettings settings)
    {
        Directory.CreateDirectory(DirectoryPath);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(settings, JsonOptions));
    }

    public static void ApplyStartup(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
        if (key == null)
            return;

        const string valueName = "Mim0.TelegramRPC";
        if (enabled)
        {
            var exe = Environment.ProcessPath;
            if (!string.IsNullOrWhiteSpace(exe))
                key.SetValue(valueName, $"\"{exe}\"");
        }
        else
        {
            key.DeleteValue(valueName, false);
        }
    }

    public static string FileLocation => FilePath;
}
