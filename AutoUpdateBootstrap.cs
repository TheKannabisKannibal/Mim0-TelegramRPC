using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace Mim0.TelegramRPC;

internal static class AutoUpdateBootstrap
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        _ = Task.Run(async ()
        =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(8));

                var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.4.0";
                var update = await UpdateChecker.CheckAsync(version);

                if (update == null)
                    return;

                var result = MessageBox.Show(
                    $"Доступна новая версия Mim0 | TelegramRPC: v{update.Version}\n\n" +
                    $"Текущая версия: v{version}\n\n" +
                    "Скачать и установить обновление сейчас?",
                    "Mim0 | TelegramRPC — обновление",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

                if (result != DialogResult.Yes)
                    return;

                if (await UpdateChecker.DownloadAndLaunchInstallerAsync(update))
                    Application.Exit();
            }
            catch
            {
                // Update checks must never prevent Mim0 from starting.
            }
        });
    }
}
