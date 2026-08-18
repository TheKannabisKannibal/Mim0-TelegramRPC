using System.Drawing;
using System.Windows.Forms;

namespace Mim0.TelegramRPC;

internal sealed class SettingsForm : Form
{
    private readonly CheckBox showAlbumArt;
    private readonly CheckBox showProgress;
    private readonly CheckBox showPaused;
    private readonly CheckBox telegramOnly;
    private readonly CheckBox startWithWindows;
    private readonly TextBox detailsFormat;
    private readonly TextBox stateFormat;
    private readonly ComboBox language;
    private readonly Label hint;

    public AppSettings Settings { get; private set; }

    public SettingsForm(AppSettings settings)
    {
        Settings = settings.Clone();
        Localization.Configure(Settings.Language);

        Text = Localization.SettingsTitle;
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(520, 540);
        Font = new Font("Segoe UI", 9F);

        var title = new Label
        {
            Text = "Mim0 | TelegramRPC",
            Font = new Font("Segoe UI Semibold", 16F),
            AutoSize = true,
            Location = new Point(24, 20)
        };

        var subtitle = new Label
        {
            Text = Localization.SettingsSubtitle,
            AutoSize = true,
            Location = new Point(26, 51),
            ForeColor = SystemColors.GrayText
        };

        showAlbumArt = CreateCheckBox(Localization.AlbumArt, 90, Settings.ShowAlbumArt);
        showProgress = CreateCheckBox(Localization.Progress, 120, Settings.ShowProgress);
        showPaused = CreateCheckBox(Localization.Paused, 150, Settings.ShowPausedState);
        telegramOnly = CreateCheckBox(Localization.TelegramOnly, 180, Settings.TelegramOnly);
        startWithWindows = CreateCheckBox(Localization.StartWithWindows, 210, Settings.StartWithWindows);

        var languageLabel = new Label
        {
            Text = Localization.LanguageLabel,
            AutoSize = true,
            Location = new Point(24, 246)
        };

        language = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 140,
            Location = new Point(85, 242)
        };
        language.Items.AddRange([Localization.Russian, Localization.English]);
        language.SelectedIndex = Settings.Language == "en" ? 1 : 0;

        var detailsLabel = new Label { Text = Localization.DetailsLabel, AutoSize = true, Location = new Point(24, 282) };
        detailsFormat = CreateTextBox(Settings.DetailsFormat, 310);

        var stateLabel = new Label { Text = Localization.StateLabel, AutoSize = true, Location = new Point(24, 349) };
        stateFormat = CreateTextBox(Settings.StateFormat, 377);

        hint = new Label
        {
            Text = Localization.Hint,
            AutoSize = true,
            Location = new Point(24, 412),
            ForeColor = SystemColors.GrayText
        };

        var reset = new System.Windows.Forms.Button { Text = Localization.Default, AutoSize = true, Location = new Point(24, 478) };
        reset.Click += (_, _) => ResetDefaults();

        var cancel = new System.Windows.Forms.Button { Text = Localization.Cancel, DialogResult = DialogResult.Cancel, AutoSize = true, Location = new Point(350, 478) };
        var save = new System.Windows.Forms.Button { Text = Localization.Save, DialogResult = DialogResult.OK, AutoSize = true, Location = new Point(430, 478) };
        save.Click += (_, _) => SaveAndClose();

        AcceptButton = save;
        CancelButton = cancel;

        Controls.AddRange([
            title, subtitle,
            showAlbumArt, showProgress, showPaused, telegramOnly, startWithWindows,
            languageLabel, language,
            detailsLabel, detailsFormat, stateLabel, stateFormat, hint,
            reset, cancel, save
        ]);
    }

    private CheckBox CreateCheckBox(string text, int top, bool value) => new()
    {
        Text = text,
        AutoSize = true,
        Location = new Point(24, top),
        Checked = value
    };

    private TextBox CreateTextBox(string text, int top) => new()
    {
        Text = text,
        Width = 472,
        Location = new Point(24, top)
    };

    private void ResetDefaults()
    {
        showAlbumArt.Checked = true;
        showProgress.Checked = true;
        showPaused.Checked = true;
        telegramOnly.Checked = true;
        startWithWindows.Checked = false;
        detailsFormat.Text = "{title}";
        stateFormat.Text = "{artist}";
    }

    private void SaveAndClose()
    {
        var details = string.IsNullOrWhiteSpace(detailsFormat.Text) ? "{title}" : detailsFormat.Text.Trim();
        var state = string.IsNullOrWhiteSpace(stateFormat.Text) ? "{artist}" : stateFormat.Text.Trim();
        var selectedLanguage = language.SelectedIndex == 1 ? "en" : "ru";

        Settings = new AppSettings
        {
            ShowAlbumArt = showAlbumArt.Checked,
            ShowProgress = showProgress.Checked,
            ShowPausedState = showPaused.Checked,
            TelegramOnly = telegramOnly.Checked,
            StartWithWindows = startWithWindows.Checked,
            DetailsFormat = details,
            StateFormat = state,
            Language = selectedLanguage
        };

        try
        {
            SettingsStore.Save(Settings);
            SettingsStore.ApplyStartup(Settings.StartWithWindows);
            Localization.Configure(Settings.Language);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"{Localization.SaveError}:\n{ex.Message}", "Mim0", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
