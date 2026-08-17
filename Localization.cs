using System.Globalization;
using System.Reflection;
using System.Windows.Forms;

namespace Mim0.TelegramRPC;

internal static class Localization
{
    public static string CurrentLanguage { get; private set; } = "ru";

    public static void Configure(string? language)
    {
        CurrentLanguage = language == "en" ? "en" : "ru";
        Application.Idle -= RefreshTray;
        Application.Idle += RefreshTray;
    }

    public static string DetectDefaultLanguage() =>
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("ru", StringComparison.OrdinalIgnoreCase)
            ? "ru"
            : "en";

    public static bool IsEnglish => CurrentLanguage == "en";
    public static string T(string ru, string en) => IsEnglish ? en : ru;

    public static string SettingsTitle => T("Mim0 | TelegramRPC — Настройки", "Mim0 | TelegramRPC — Settings");
    public static string SettingsSubtitle => T("Настрой, что именно будет показываться в Discord.", "Choose what Mim0 should show in Discord.");
    public static string AlbumArt => T("Показывать обложку трека", "Show album art");
    public static string Progress => T("Показывать прогресс воспроизведения", "Show playback progress");
    public static string Paused => T("Показывать ⏸ при паузе", "Show ⏸ when paused");
    public static string TelegramOnly => T("Использовать только Telegram-плееры", "Use Telegram players only");
    public static string StartWithWindows => T("Запускать Mim0 вместе с Windows", "Start Mim0 with Windows");
    public static string DetailsLabel => T("Верхняя строка (Details)", "Top line (Details)");
    public static string StateLabel => T("Нижняя строка (State)", "Bottom line (State)");
    public static string Hint => T("Доступные переменные: {title}, {artist}, {source}\nПример: {title} / {artist}", "Available placeholders: {title}, {artist}, {source}\nExample: {title} / {artist}");
    public static string LanguageLabel => T("Язык", "Language");
    public static string Russian => "Русский";
    public static string English => "English";
    public static string Default => T("По умолчанию", "Defaults");
    public static string Cancel => T("Отмена", "Cancel");
    public static string Save => T("Сохранить", "Save");
    public static string SaveError => T("Не удалось сохранить настройки", "Failed to save settings");

    public static string WaitingMusic => T("ожидание музыки", "waiting for music");
    public static string WaitingDiscord => T("ожидание Discord", "waiting for Discord");
    public static string ReconnectingDiscord => T("переподключение Discord", "reconnecting Discord");
    public static string SettingsSaved => T("настройки сохранены", "settings saved");
    public static string Retry => T("повторная попытка", "retrying");
    public static string Playing => T("играет", "playing");
    public static string PausedStatus => T("пауза", "paused");
    public static string DiagnosticsCopied => T("диагностика скопирована", "diagnostics copied");
    public static string Error => T("ошибка", "error");
    public static string MusicWaiting => T("Музыка: ожидание", "Music: waiting");
    public static string SettingsMenu => T("Настройки", "Settings");
    public static string CheckNow => T("Проверить сейчас", "Check now");
    public static string ReconnectDiscord => T("Переподключить Discord", "Reconnect Discord");
    public static string CopyDiagnostics => T("Скопировать диагностику", "Copy diagnostics");
    public static string OpenGitHub => T("Открыть GitHub", "Open GitHub");
    public static string OpenProgramFolder => T("Открыть папку программы", "Open program folder");
    public static string About => T("О программе", "About");
    public static string Exit => T("Выход", "Exit");
    public static string AboutTitle => T("О программе", "About");
    public static string AboutText(string version) =>
        T($"Mim0 | TelegramRPC\n\nВерсия: {version}\n\nTelegram music → Discord Rich Presence\n\nGitHub: TheKannabisKannibal/Mim0-TelegramRPC",
          $"Mim0 | TelegramRPC\n\nVersion: {version}\n\nWindows music → Discord Rich Presence\n\nGitHub: TheKannabisKannibal/Mim0-TelegramRPC");

    private static void RefreshTray(object? sender, EventArgs e)
    {
        try
        {
            var field = typeof(Program).GetField("tray", BindingFlags.Static | BindingFlags.NonPublic);
            var tray = field?.GetValue(null) as NotifyIcon;
            var menu = tray?.ContextMenuStrip;
            if (tray == null || menu == null)
                return;

            var items = menu.Items;
            if (items.Count >= 13)
            {
                items[0].Text = "Mim0 | TelegramRPC";
                items[1].Text = MusicWaiting;
                items[3].Text = SettingsMenu;
                items[4].Text = CheckNow;
                items[5].Text = ReconnectDiscord;
                items[6].Text = CopyDiagnostics;
                items[8].Text = OpenGitHub;
                items[9].Text = OpenProgramFolder;
                items[11].Text = About;
                items[12].Text = Exit;
            }

            tray.Text = "Mim0 | TelegramRPC";
            Application.Idle -= RefreshTray;
        }
        catch
        {
            // Tray may not exist yet during startup.
        }
    }
}
