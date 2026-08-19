using Avalonia.Win32.Interoperability;
using System.Drawing;
using WinForms = System.Windows.Forms;

namespace Mim0.TelegramRPC;

internal sealed class ModernSettingsForm : WinForms.Form
{
    public AppSettings Settings { get; private set; }

    public ModernSettingsForm(AppSettings source)
    {
        Settings = source.Clone();

        Text = Localization.SettingsTitle;
        StartPosition = WinForms.FormStartPosition.CenterScreen;
        FormBorderStyle = WinForms.FormBorderStyle.None;
        ShowInTaskbar = false;
        MinimizeBox = false;
        MaximizeBox = false;
        ClientSize = new Size(980, 680);
        BackColor = Color.FromArgb(11, 13, 17);

        var host = new WinFormsAvaloniaControlHost
        {
            Dock = WinForms.DockStyle.Fill,
            Content = new ModernSettingsView(Settings, Finish)
        };

        Controls.Add(host);
    }

    private void Finish(AppSettings? result)
    {
        if (result == null)
        {
            DialogResult = WinForms.DialogResult.Cancel;
        }
        else
        {
            Settings = result;
            SettingsStore.Save(Settings);
            SettingsStore.ApplyStartup(Settings.StartWithWindows);
            Localization.Configure(Settings.Language);
            DialogResult = WinForms.DialogResult.OK;
        }

        Close();
    }
}
