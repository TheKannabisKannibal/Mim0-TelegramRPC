using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using WpfButton = System.Windows.Controls.Button;
using WpfCheckBox = System.Windows.Controls.CheckBox;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfComboBoxItem = System.Windows.Controls.ComboBoxItem;
using WpfStackPanel = System.Windows.Controls.StackPanel;
using WpfPanel = System.Windows.Controls.Panel;
using WpfTextBox = System.Windows.Controls.TextBox;
using WpfTextBlock = System.Windows.Controls.TextBlock;
using WpfGrid = System.Windows.Controls.Grid;
using WpfBorder = System.Windows.Controls.Border;
using WpfScrollViewer = System.Windows.Controls.ScrollViewer;
using WpfColumnDefinition = System.Windows.Controls.ColumnDefinition;
using WpfRowDefinition = System.Windows.Controls.RowDefinition;
using WpfOrientation = System.Windows.Controls.Orientation;

namespace Mim0.TelegramRPC;

internal sealed class ModernSettingsWindow2 : Window
{
    private static readonly Brush WindowBg = BrushFromHex("#0B0D11");
    private static readonly Brush SidebarBg = BrushFromHex("#101319");
    private static readonly Brush CardBg = BrushFromHex("#15181F");
    private static readonly Brush InputBg = BrushFromHex("#0F1217");
    private static readonly Brush Line = BrushFromHex("#272C35");
    private static readonly Brush Primary = BrushFromHex("#F4F5F7");
    private static readonly Brush Secondary = BrushFromHex("#A3A8B3");
    private static readonly Brush Muted = BrushFromHex("#6F7683");
    private static readonly Brush Accent = BrushFromHex("#E85B8A");
    private static readonly Brush AccentSoft = BrushFromHex("#3A202B");

    private readonly WpfCheckBox albumArt;
    private readonly WpfCheckBox progress;
    private readonly WpfCheckBox paused;
    private readonly WpfCheckBox telegramOnly;
    private readonly WpfCheckBox startup;
    private readonly WpfTextBox details;
    private readonly WpfTextBox state;
    private readonly WpfComboBox language;

    public AppSettings Settings { get; private set; }

    public ModernSettingsWindow2(AppSettings settings)
    {
        Settings = settings.Clone();
        Title = Localization.SettingsTitle;
        Width = 980;
        Height = 680;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;

        albumArt = Check(Localization.AlbumArt, Settings.ShowAlbumArt);
        progress = Check(Localization.Progress, Settings.ShowProgress);
        paused = Check(Localization.Paused, Settings.ShowPausedState);
        telegramOnly = Check(Localization.TelegramOnly, Settings.TelegramOnly);
        startup = Check(Localization.StartWithWindows, Settings.StartWithWindows);
        details = TextBox(Settings.DetailsFormat);
        state = TextBox(Settings.StateFormat);
        language = Languages();
        Content = Build();
    }

    private UIElement Build()
    {
        var shell = new WpfBorder { Background = WindowBg, BorderBrush = Line, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(18) };
        var root = new WpfGrid();
        root.ColumnDefinitions.Add(new WpfColumnDefinition { Width = new GridLength(220) });
        root.ColumnDefinitions.Add(new WpfColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        shell.Child = root;
        root.Children.Add(Sidebar());

        var content = new WpfGrid();
        Grid.SetColumn(content, 1);
        content.RowDefinitions.Add(new WpfRowDefinition { Height = new GridLength(76) });
        content.RowDefinitions.Add(new WpfRowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        content.RowDefinitions.Add(new WpfRowDefinition { Height = new GridLength(64) });
        content.Children.Add(Header());
        content.Children.Add(Main());
        content.Children.Add(Footer());
        root.Children.Add(content);
        return shell;
    }

    private UIElement Sidebar()
    {
        var panel = new WpfBorder { Background = SidebarBg, CornerRadius = new CornerRadius(18, 0, 0, 18), BorderBrush = Line, BorderThickness = new Thickness(0, 0, 1, 0), Padding = new Thickness(18, 22, 18, 18) };
        var stack = new WpfStackPanel();
        panel.Child = stack;
        stack.Children.Add(new WpfBorder { Width = 40, Height = 40, CornerRadius = new CornerRadius(12), Background = Accent, Child = new WpfTextBlock { Text = "M", Foreground = Brushes.White, FontSize = 18, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }, Margin = new Thickness(3, 0, 0, 10) });
        stack.Children.Add(new WpfTextBlock { Text = "Mim0", Foreground = Primary, FontSize = 18, FontWeight = FontWeights.SemiBold, Margin = new Thickness(3, 0, 0, 1) });
        stack.Children.Add(new WpfTextBlock { Text = "TelegramRPC", Foreground = Muted, FontSize = 11, Margin = new Thickness(3, 0, 0, 30) });
        Nav(stack, Localization.T("Настройки", "Settings"), [Localization.T("Общие", "General"), "Discord Presence", Localization.T("Отображение", "Appearance")]);
        Nav(stack, Localization.T("Прочее", "Other"), [Localization.T("Запуск", "Startup"), Localization.T("О программе", "About")]);
        stack.Children.Add(new WpfBorder { Height = 1, Background = Line, Margin = new Thickness(3, 18, 3, 14) });
        stack.Children.Add(new WpfTextBlock { Text = Localization.T("●  Работает в фоне", "●  Running in background"), Foreground = BrushFromHex("#55D99A"), FontSize = 11, Margin = new Thickness(4, 0, 0, 0) });
        stack.Children.Add(new WpfTextBlock { Text = "v" + (Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.5.2"), Foreground = Muted, FontSize = 10, Margin = new Thickness(4, 10, 0, 0) });
        return panel;
    }

    private static void Nav(WpfPanel parent, string title, string[] items)
    {
        parent.Children.Add(new WpfTextBlock { Text = title.ToUpperInvariant(), Foreground = Muted, FontSize = 9, FontWeight = FontWeights.SemiBold, Margin = new Thickness(4, 0, 0, 7) });
        for (int i = 0; i < items.Length; i++)
            parent.Children.Add(new WpfBorder { Background = i == 0 ? AccentSoft : Brushes.Transparent, CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 9, 10, 9), Margin = new Thickness(0, 0, 0, 3), Child = new WpfTextBlock { Text = items[i], Foreground = i == 0 ? Primary : Secondary, FontSize = 12, FontWeight = i == 0 ? FontWeights.SemiBold : FontWeights.Normal } });
        parent.Children.Add(new WpfBorder { Height = 12, Background = Brushes.Transparent });
    }

    private UIElement Header()
    {
        var grid = new WpfGrid { Margin = new Thickness(28, 0, 24, 0) };
        grid.ColumnDefinitions.Add(new WpfColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new WpfColumnDefinition { Width = GridLength.Auto });
        var title = new WpfStackPanel { VerticalAlignment = VerticalAlignment.Center };
        title.Children.Add(new WpfTextBlock { Text = Localization.T("Общие настройки", "General settings"), Foreground = Primary, FontSize = 20, FontWeight = FontWeights.SemiBold });
        title.Children.Add(new WpfTextBlock { Text = Localization.SettingsSubtitle, Foreground = Secondary, FontSize = 11, Margin = new Thickness(0, 4, 0, 0) });
        grid.Children.Add(title);
        var close = Button("×", false, 34);
        close.Click += (_, _) => Close();
        Grid.SetColumn(close, 1);
        grid.Children.Add(close);
        grid.MouseLeftButtonDown += (_, e) => { if (e.LeftButton == MouseButtonState.Pressed) DragMove(); };
        return grid;
    }

    private UIElement Main()
    {
        var scroll = new WpfScrollViewer { VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Disabled, Margin = new Thickness(28, 0, 28, 0) };
        Grid.SetRow(scroll, 1);
        var root = new WpfGrid();
        root.ColumnDefinitions.Add(new WpfColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        root.ColumnDefinitions.Add(new WpfColumnDefinition { Width = new GridLength(300) });
        var left = new WpfStackPanel { Margin = new Thickness(0, 0, 16, 20) };
        left.Children.Add(Card(Localization.T("Discord Rich Presence", "Discord Rich Presence"), Localization.T("Что Mim0 отправляет в Discord", "What Mim0 sends to Discord"), albumArt, progress, paused, telegramOnly));
        left.Children.Add(FormatCard());
        left.Children.Add(GeneralCard());
        root.Children.Add(left);
        var right = new WpfStackPanel();
        right.Children.Add(new WpfTextBlock { Text = Localization.T("Предпросмотр", "Preview"), Foreground = Primary, FontSize = 13, FontWeight = FontWeights.SemiBold, Margin = new Thickness(2, 2, 0, 10) });
        right.Children.Add(PreviewCard());
        right.Children.Add(new WpfBorder { Background = InputBg, BorderBrush = Line, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(12), Padding = new Thickness(13), Margin = new Thickness(0, 12, 0, 20), Child = new WpfTextBlock { Text = Localization.T("Подсказка\nИспользуй {title}, {artist} и {source} в шаблонах.", "Tip\nUse {title}, {artist} and {source} in the templates."), Foreground = Secondary, FontSize = 11, TextWrapping = TextWrapping.Wrap } });
        Grid.SetColumn(right, 1);
        root.Children.Add(right);
        scroll.Content = root;
        return scroll;
    }

    private WpfBorder Card(string title, string subtitle, params WpfCheckBox[] checks)
    {
        var stack = new WpfStackPanel();
        stack.Children.Add(new WpfTextBlock { Text = title, Foreground = Primary, FontSize = 13, FontWeight = FontWeights.SemiBold });
        stack.Children.Add(new WpfTextBlock { Text = subtitle, Foreground = Secondary, FontSize = 10, Margin = new Thickness(0, 4, 0, 12) });
        foreach (var check in checks) { check.Margin = new Thickness(0, 0, 0, 9); stack.Children.Add(check); }
        return Wrap(stack);
    }

    private WpfBorder FormatCard()
    {
        var stack = new WpfStackPanel();
        stack.Children.Add(new WpfTextBlock { Text = Localization.T("Формат присутствия", "Presence format"), Foreground = Primary, FontSize = 13, FontWeight = FontWeights.SemiBold });
        stack.Children.Add(new WpfTextBlock { Text = Localization.Hint.Replace("\n", "  •  "), Foreground = Secondary, FontSize = 10, Margin = new Thickness(0, 4, 0, 12) });
        Field(stack, Localization.DetailsLabel, details);
        Field(stack, Localization.StateLabel, state);
        return Wrap(stack);
    }

    private WpfBorder GeneralCard()
    {
        var stack = new WpfStackPanel();
        stack.Children.Add(new WpfTextBlock { Text = Localization.T("Общие", "General"), Foreground = Primary, FontSize = 13, FontWeight = FontWeights.SemiBold });
        stack.Children.Add(new WpfTextBlock { Text = Localization.T("Язык и запуск", "Language and startup"), Foreground = Secondary, FontSize = 10, Margin = new Thickness(0, 4, 0, 12) });
        var row = new WpfGrid { Margin = new Thickness(0, 0, 0, 8) };
        row.ColumnDefinitions.Add(new WpfColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new WpfColumnDefinition { Width = GridLength.Auto });
        row.Children.Add(new WpfTextBlock { Text = Localization.LanguageLabel, Foreground = Primary, FontSize = 12, VerticalAlignment = VerticalAlignment.Center });
        Grid.SetColumn(language, 1);
        row.Children.Add(language);
        stack.Children.Add(row);
        startup.Margin = new Thickness(0, 0, 0, 9);
        stack.Children.Add(startup);
        return Wrap(stack);
    }

    private WpfBorder PreviewCard()
    {
        var stack = new WpfStackPanel();
        stack.Children.Add(new WpfTextBlock { Text = "Mim0 | TelegramRPC", Foreground = Secondary, FontSize = 10, Margin = new Thickness(0, 0, 0, 13) });
        stack.Children.Add(new WpfTextBlock { Text = "♫  Bring Eyes = Death Invite", Foreground = Primary, FontSize = 12, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap });
        stack.Children.Add(new WpfTextBlock { Text = "Imperial Circus Dead Decadence", Foreground = Secondary, FontSize = 11, Margin = new Thickness(0, 5, 0, 14), TextWrapping = TextWrapping.Wrap });
        stack.Children.Add(new WpfTextBlock { Text = Localization.T("Слушает музыку", "Listening to music"), Foreground = Muted, FontSize = 10 });
        stack.Children.Add(new WpfBorder { Height = 4, Background = Accent, CornerRadius = new CornerRadius(2), Margin = new Thickness(0, 10, 0, 0) });
        return Wrap(stack, false);
    }

    private UIElement Footer()
    {
        var grid = new WpfGrid { Margin = new Thickness(28, 0, 28, 0) };
        grid.ColumnDefinitions.Add(new WpfColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new WpfColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new WpfColumnDefinition { Width = GridLength.Auto });
        var cancel = Button(Localization.Cancel, false, 36);
        cancel.Click += (_, _) => { DialogResult = false; Close(); };
        Grid.SetColumn(cancel, 1);
        grid.Children.Add(cancel);
        var save = Button(Localization.Save, true, 36);
        save.Click += (_, _) => SaveAndClose();
        Grid.SetColumn(save, 2);
        grid.Children.Add(save);
        Grid.SetRow(grid, 2);
        return grid;
    }

    private void SaveAndClose()
    {
        Settings.ShowAlbumArt = albumArt.IsChecked == true;
        Settings.ShowProgress = progress.IsChecked == true;
        Settings.ShowPausedState = paused.IsChecked == true;
        Settings.TelegramOnly = telegramOnly.IsChecked == true;
        Settings.StartWithWindows = startup.IsChecked == true;
        Settings.DetailsFormat = string.IsNullOrWhiteSpace(details.Text) ? "{title}" : details.Text.Trim();
        Settings.StateFormat = string.IsNullOrWhiteSpace(state.Text) ? "{artist}" : state.Text.Trim();
        Settings.Language = language.SelectedIndex == 1 ? "en" : "ru";
        SettingsStore.Save(Settings);
        SettingsStore.ApplyStartup(Settings.StartWithWindows);
        Localization.Configure(Settings.Language);
        DialogResult = true;
        Close();
    }

    private static WpfBorder Wrap(WpfPanel child, bool margin = true) => new() { Background = CardBg, BorderBrush = Line, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(14), Padding = new Thickness(18), Margin = margin ? new Thickness(0, 0, 0, 12) : new Thickness(0) , Child = child };
    private static void Field(WpfPanel parent, string label, WpfTextBox box) { parent.Children.Add(new WpfTextBlock { Text = label, Foreground = Secondary, FontSize = 10, Margin = new Thickness(0, 0, 0, 5) }); parent.Children.Add(box); }
    private static WpfCheckBox Check(string text, bool value) => new() { Content = text, IsChecked = value, Foreground = Primary, FontSize = 12 };
    private static WpfTextBox TextBox(string text) => new() { Text = text, Height = 34, Background = InputBg, Foreground = Primary, BorderBrush = Line, BorderThickness = new Thickness(1), Padding = new Thickness(9, 7, 9, 7), Margin = new Thickness(0, 0, 0, 10) };
    private static WpfComboBox Languages() { var c = new WpfComboBox { Width = 125, Height = 30, Background = InputBg, Foreground = Primary, BorderBrush = Line }; c.Items.Add(new WpfComboBoxItem { Content = "Русский", Foreground = Brushes.Black }); c.Items.Add(new WpfComboBoxItem { Content = "English", Foreground = Brushes.Black }); c.SelectedIndex = Localization.CurrentLanguage == "en" ? 1 : 0; return c; }
    private static WpfButton Button(string text, bool primary, double height) => new() { Content = text, MinWidth = 94, Height = height, Margin = new Thickness(8, 0, 0, 0), Padding = new Thickness(16, 0, 16, 0), Background = primary ? Accent : InputBg, Foreground = Brushes.White, BorderBrush = primary ? Accent : Line, BorderThickness = new Thickness(1), FontSize = 12, Cursor = Cursors.Hand };
    private static Brush BrushFromHex(string hex) => (Brush)new BrushConverter().ConvertFrom(hex)!;
}
