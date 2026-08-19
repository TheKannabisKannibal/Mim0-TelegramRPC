using System.Drawing;
using System.Drawing.Drawing2D;
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

    private static readonly Color Background = Color.FromArgb(18, 18, 22);
    private static readonly Color Surface = Color.FromArgb(27, 27, 33);
    private static readonly Color SurfaceHover = Color.FromArgb(34, 34, 41);
    private static readonly Color Border = Color.FromArgb(48, 48, 58);
    private static readonly Color TextPrimary = Color.FromArgb(242, 242, 247);
    private static readonly Color TextSecondary = Color.FromArgb(158, 158, 170);
    private static readonly Color Accent = Color.FromArgb(91, 141, 239);

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
        ClientSize = new Size(680, 610);
        BackColor = Background;
        ForeColor = TextPrimary;
        Font = new Font("Segoe UI", 9F);

        var header = new Panel { Dock = DockStyle.Top, Height = 92, BackColor = Background };
        var logo = new Label
        {
            Text = "M",
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI Semibold", 15F),
            ForeColor = Color.White,
            BackColor = Accent,
            Size = new Size(42, 42),
            Location = new Point(24, 24)
        };
        logo.Paint += (_, _) =>
        {
            using var path = RoundedPath(logo.ClientRectangle, 10);
            logo.Region = new Region(path);
        };

        var title = new Label
        {
            Text = "Mim0 | TelegramRPC",
            Font = new Font("Segoe UI Semibold", 17F),
            AutoSize = true,
            Location = new Point(80, 20),
            ForeColor = TextPrimary
        };

        var subtitle = new Label
        {
            Text = Localization.SettingsSubtitle,
            AutoSize = true,
            Location = new Point(82, 51),
            ForeColor = TextSecondary
        };

        header.Controls.AddRange([logo, title, subtitle]);

        var content = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(24, 0, 24, 0),
            BackColor = Background,
            AutoScroll = true
        };

        var presenceCard = CreateCard(0, 0, 632, 214);
        AddSectionTitle(presenceCard, "Discord Rich Presence", Localization.SettingsSubtitle, 18);

        showAlbumArt = CreateSwitch(Localization.AlbumArt, 60, Settings.ShowAlbumArt);
        showProgress = CreateSwitch(Localization.Progress, 94, Settings.ShowProgress);
        showPaused = CreateSwitch(Localization.Paused, 128, Settings.ShowPausedState);
        telegramOnly = CreateSwitch(Localization.TelegramOnly, 162, Settings.TelegramOnly);
        presenceCard.Controls.AddRange([showAlbumArt, showProgress, showPaused, telegramOnly]);

        var displayCard = CreateCard(0, 226, 632, 206);
        AddSectionTitle(displayCard, Localization.DetailsLabel, Localization.Hint, 18);

        var detailsLabel = CreateFieldLabel(Localization.DetailsLabel, 58);
        detailsFormat = CreateTextBox(Settings.DetailsFormat, 82);
        var stateLabel = CreateFieldLabel(Localization.StateLabel, 120);
        stateFormat = CreateTextBox(Settings.StateFormat, 144);
        displayCard.Controls.AddRange([detailsLabel, detailsFormat, stateLabel, stateFormat]);

        var generalCard = CreateCard(0, 444, 632, 110);
        AddSectionTitle(generalCard, Localization.LanguageLabel, null, 18);

        language = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 150,
            Height = 30,
            Location = new Point(18, 54),
            BackColor = SurfaceHover,
            ForeColor = TextPrimary,
            FlatStyle = FlatStyle.Flat
        };
        language.Items.AddRange([Localization.Russian, Localization.English]);
        language.SelectedIndex = Settings.Language == "en" ? 1 : 0;

        startWithWindows = CreateSwitch(Localization.StartWithWindows, 60, Settings.StartWithWindows);
        startWithWindows.Location = new Point(210, 59);
        generalCard.Controls.AddRange([language, startWithWindows]);

        hint = new Label
        {
            Text = Localization.Hint,
            AutoSize = true,
            ForeColor = TextSecondary,
            Location = new Point(24, 568)
        };
        content.Controls.AddRange([presenceCard, displayCard, generalCard, hint]);

        var footer = new Panel { Dock = DockStyle.Bottom, Height = 66, BackColor = Background };
        var reset = CreateButton(Localization.Default, false, 24, 14, 105);
        reset.Click += (_, _) => ResetDefaults();
        var cancel = CreateButton(Localization.Cancel, false, 492, 14, 76);
        cancel.DialogResult = DialogResult.Cancel;
        var save = CreateButton(Localization.Save, true, 576, 14, 80);
        save.Click += (_, _) => SaveAndClose();
        footer.Controls.AddRange([reset, cancel, save]);

        AcceptButton = save;
        CancelButton = cancel;
        Controls.AddRange([content, footer, header]);
    }

    private static Panel CreateCard(int x, int y, int width, int height)
    {
        return new Panel
        {
            Location = new Point(x, y),
            Size = new Size(width, height),
            BackColor = Surface,
            BorderStyle = BorderStyle.FixedSingle
        };
    }

    private static void AddSectionTitle(Control parent, string title, string? subtitle, int top)
    {
        parent.Controls.Add(new Label
        {
            Text = title,
            Font = new Font("Segoe UI Semibold", 10.5F),
            AutoSize = true,
            Location = new Point(18, top),
            ForeColor = TextPrimary
        });

        if (!string.IsNullOrWhiteSpace(subtitle))
        {
            parent.Controls.Add(new Label
            {
                Text = subtitle,
                Font = new Font("Segoe UI", 8.5F),
                AutoSize = true,
                Location = new Point(18, top + 22),
                ForeColor = TextSecondary
            });
        }
    }

    private static Label CreateFieldLabel(string text, int top) => new()
    {
        Text = text,
        AutoSize = true,
        Location = new Point(18, top),
        ForeColor = TextSecondary
    };

    private CheckBox CreateSwitch(string text, int top, bool value) => new()
    {
        Text = text,
        AutoSize = true,
        Location = new Point(18, top),
        Checked = value,
        ForeColor = TextPrimary,
        BackColor = Surface,
        FlatStyle = FlatStyle.Standard
    };

    private TextBox CreateTextBox(string text, int top) => new()
    {
        Text = text,
        Width = 596,
        Height = 28,
        Location = new Point(18, top),
        BackColor = SurfaceHover,
        ForeColor = TextPrimary,
        BorderStyle = BorderStyle.FixedSingle
    };

    private static System.Windows.Forms.Button CreateButton(string text, bool primary, int left, int top, int width) => new()
    {
        Text = text,
        Size = new Size(width, 34),
        Location = new Point(left, top),
        FlatStyle = FlatStyle.Flat,
        BackColor = primary ? Accent : Surface,
        ForeColor = primary ? Color.White : TextPrimary,
        FlatAppearance = { BorderColor = primary ? Accent : Border, BorderSize = 1 }
    };

    private static GraphicsPath RoundedPath(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        int d = radius * 2;
        path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    private void ResetDefaults()
    {
        showAlbumArt.Checked = true;
        showProgress.Checked = true;
        showPaused.Checked = true;
        telegramOnly.Checked = true;
        startWithWindows.Checked = false;
        detailsFormat.Text = "{title}";
        stateFormat.Text = "{artist}";
        language.SelectedIndex = 0;
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
