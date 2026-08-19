using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace Mim0.TelegramRPC;

internal static class UpdateModuleInitializer
{
    private static readonly string StateFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Mim0",
        "TelegramRPC",
        "update-check.txt");

    [ModuleInitializer]
    internal static void Initialize() => _ = Task.Run(CheckForUpdatesAsync);

    private static async Task CheckForUpdatesAsync()
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(8));
            var currentVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "0.0.0";
            var update = await UpdateChecker.CheckAsync(currentVersion);
            if (update == null || WasAlreadyOffered(update.Version)) return;
            MarkOffered(update.Version);

            var answer = MessageBox.Show(
                $"Доступна новая версия Mim0 | TelegramRPC {update.TagName}.\n\nСкачать и установить обновление сейчас?",
                "Mim0 | TelegramRPC — обновление", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            if (answer != DialogResult.Yes) return;

            MessageBox.Show(
                "Установщик будет скачан и запущен. Mim0 закроется и после установки запустится снова.",
                "Mim0 | TelegramRPC", MessageBoxButtons.OK, MessageBoxIcon.Information);

            if (await UpdateChecker.DownloadAndLaunchInstallerAsync(update)) Environment.Exit(0);
        }
        catch { }
    }

    private static bool WasAlreadyOffered(string version)
    {
        try
        {
            return File.Exists(StateFile) &&
                   string.Equals(File.ReadAllText(StateFile).Trim(), version, StringComparison.OrdinalIgnoreCase);
        }
        catch { return false; }
    }

    private static void MarkOffered(string version)
    {
        try
        {
            var directory = Path.GetDirectoryName(StateFile);
            if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
            File.WriteAllText(StateFile, version);
        }
        catch { }
    }
}
