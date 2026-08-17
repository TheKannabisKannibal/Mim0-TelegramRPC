using System.Diagnostics;
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
    private readonly Label hint;

    public AppSettings Settings { get; private set; }

    public SettingsForm(AppSettings settings)
    {
        Settings = settings.Clone();

        Text = "Mim0 | TelegramRPC — Настройки";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(520, 500);
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
            Text = "Настрой, что именно будет показываться в Discord.",
            AutoSize = true,
            Location = new Point(26, 51),
            ForeColor = SystemColors.GrayText
        };

        showAlbumArt = CreateCheckBox("Показывать обложку трека", 90, Settings.ShowAlbumArt);
        showProgress = CreateCheckBox("Показывать прогресс воспроизведения", 120, Settings.ShowProgress);
        showPaused = CreateCheckBox("Показывать ⏸ при паузе", 150, Settings.ShowPausedState);
        telegramOnly = CreateCheckBox("Использовать только Telegram-плееры", 180, Settings.TelegramOnly);
        startWithWindows = CreateCheckBox("Запускать Mim0 вместе с Windows", 210, Settings.StartWithWindows);

        var detailsLabel = new Label { Text = "Верхняя строка (Details)", AutoSize = true, Location = new Point(24, 252) };
        detailsFormat = CreateTextBox(Settings.DetailsFormat, 280);

        var stateLabel = new Label { Text = "Нижняя строка (State)", AutoSize = true, Location = new Point(24, 319) };
        stateFormat = CreateTextBox(Settings.StateFormat, 347);

        hint = new Label
        {
            Text = "Доступные переменные: {title}, {artist}, {source}\nПример: {title} / {artist}",
            AutoSize = true,
            Location = new Point(24, 382),
            ForeColor = SystemColors.GrayText
        };

        var reset = new Button { Text = "По умолчанию", AutoSize = true, Location = new Point(24, 438) };
        reset.Click += (_, _) => ResetDefaults();

        var cancel = new Button { Text = "Отмена", DialogResult = DialogResult.Cancel, AutoSize = true, Location = new Point(350, 438) };
        var save = new Button { Text = "Сохранить", DialogResult = DialogResult.OK, AutoSize = true, Location = new Point(430, 438) };
        save.Click += (_, _) => SaveAndClose();

        AcceptButton = save;
        CancelButton = cancel;

        Controls.AddRange([
            title, subtitle,
            showAlbumArt, showProgress, showPaused, telegramOnly, startWithWindows,
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

        Settings = new AppSettings
        {
            ShowAlbumArt = showAlbumArt.Checked,
            ShowProgress = showProgress.Checked,
            ShowPausedState = showPaused.Checked,
            TelegramOnly = telegramOnly.Checked,
            StartWithWindows = startWithWindows.Checked,
            DetailsFormat = details,
            StateFormat = state
        };

        try
        {
            SettingsStore.Save(Settings);
            SettingsStore.ApplyStartup(Settings.StartWithWindows);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Не удалось сохранить настройки:\n{ex.Message}", "Mim0", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
