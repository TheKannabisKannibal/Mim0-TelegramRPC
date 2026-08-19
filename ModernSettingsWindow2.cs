namespace Mim0.TelegramRPC;

// Compatibility wrapper: Program.cs keeps its existing synchronous settings contract,
// while the actual UI is now implemented with Avalonia and hosted by WinForms.
internal sealed class ModernSettingsWindow2
{
    private readonly AppSettings source;
    public AppSettings Settings { get; private set; }

    public ModernSettingsWindow2(AppSettings settings)
    {
        source = settings.Clone();
        Settings = source.Clone();
    }

    public bool? ShowDialog()
    {
        using var form = new ModernSettingsForm(source);
        var result = form.ShowDialog();
        if (result != System.Windows.Forms.DialogResult.OK)
            return false;

        Settings = form.Settings;
        return true;
    }
}
