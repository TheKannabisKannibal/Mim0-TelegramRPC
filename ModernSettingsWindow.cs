using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;

namespace Mim0.TelegramRPC;

internal sealed class ModernSettingsWindow : Window
{
    private static readonly Brush Background = BrushFromHex("#0B0C0F");
    private static readonly Brush Sidebar = BrushFromHex("#101217");
    private static readonly Brush Card = BrushFromHex("#14161B");
    private static readonly Brush CardAlt = BrushFromHex("#191C22");
    private static readonly Brush Border = BrushFromHex("#262A32");
    private static readonly Brush TextPrimary = BrushFromHex("#F3F4F6");
    private static readonly Brush TextSecondary = BrushFromHex("#9CA3AF");
    private static readonly Brush TextMuted = BrushFromHex("#667085");
    private static readonly Brush Accent = BrushFromHex("#E85B8A");
    private static readonly Brush AccentSoft = BrushFromHex("#3A1E2A");
    private static readonly Brush Green = BrushFromHex("#48D597");

    private readonly CheckBox showAlbumArt;
    private readonly CheckBox showProgress;
    private readonly CheckBox showPaused;
    private readonly CheckBox telegramOnly;
    private readonly CheckBox startWithWindows;
    private readonly TextBox detailsFormat;
    private readonly TextBox stateFormat;
    private readonly ComboBox language;

    public AppSettings Settings { get; private set; }

    public ModernSettingsWindow(AppSettings settings)
    {
        Settings = settings.Clone();

        Title = Localization.SettingsTitle;
        Width = 1040;
        Height = 700;
        MinWidth = 900;
        MinHeight = 620;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;

        showAlbumArt = CreateCheckBox(Localization.AlbumArt, Settings.ShowAlbumArt);
        showProgress = CreateCheckBox(Localization.Progress, Settings.ShowProgress);
        showPaused = CreateCheckBox(Localization.Paused, Settings.ShowPausedState);
        telegramOnly = CreateCheckBox(Localization.TelegramOnly, Settings.TelegramOnly);
        startWithWindows = CreateCheckBox(Localization.StartWithWindows, Settings.StartWithWindows);
        detailsFormat = CreateTextBox(Settings.DetailsFormat);
        stateFormat = CreateTextBox(Settings.StateFormat);
        language = CreateLanguageComboBox();

        Content = BuildLayout();
    }

    private UIElement BuildLayout()
    {
        var shell = new Border
        {
            Background = Background,
            BorderBrush = Border,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(18),
            SnapsToDevicePixels = true
        };

        var root = new Grid();
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        shell.Child = root;

        root.Children.Add(BuildSidebar());

        var content = new Grid();
        Grid.SetColumn(content, 1);
        content.RowDefinitions.Add(new RowDefinition { Height = new GridLength(72) });
        content.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        content.RowDefinitions.Add(new RowDefinition { Height = new GridLength(68) });
        root.Children.Add(content);

        content.Children.Add(BuildHeader());
        content.Children.Add(BuildMainContent());
        content.Children.Add(BuildFooter());

        return shell;
    }

    private UIElement BuildSidebar()
    {
        var panel = new Border
        {
            Background = Sidebar,
            CornerRadius = new CornerRadius(18, 0, 0, 18),
            BorderBrush = Border,
            BorderThickness = new Thickness(0, 0, 1, 0),
            Padding = new Thickness(18, 22, 18, 18)
        };

        var stack = new StackPanel();
        panel.Child = stack;

        var brand = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(4, 0, 0, 30) };
        var logo = new Border
        {
            Width = 38,
            Height = 38,
            CornerRadius = new CornerRadius(11),
            Background = Accent,
            Child = new TextBlock
            {
                Text = "M",
                Foreground = Brushes.White,
                FontSize = 17,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
        brand.Children.Add(logo);
        brand.Children.Add(new StackPanel
        {
            Margin = new Thickness(11, 1, 0, 0),
            Children =
            {
                new TextBlock { Text = "Mim0", Foreground = TextPrimary, FontSize = 16, FontWeight = FontWeights.SemiBold },
                new TextBlock { Text = "TelegramRPC", Foreground = TextMuted, FontSize = 11, Margin = new Thickness(0, 2, 0, 0) }
            }
        });
        stack.Children.Add(brand);

        AddSidebarGroup(stack, Localization.T("Настройки", "Settings"), [
            Localization.T("Общие", "General"),
            Localization.T("Discord Presence", "Discord Presence"),
            Localization.T("Отображение", "Appearance")
        ]);

        AddSidebarGroup(stack, Localization.T("Прочее", "Other"), [
            Localization.T("Запуск", "Startup"),
            Localization.T("О программе", "About")
        ]);

        var spacer = new Border { Height = 1, Background = Border, Margin = new Thickness(2, 20, 2, 14) };
        stack.Children.Add(spacer);

        var status = new Border
        {
            Background = BrushFromHex("#111A16"),
            BorderBrush = BrushFromHex("#1D3328"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(10, 8, 10, 8)
        };
        var statusRow = new StackPanel { Orientation = Orientation.Horizontal };
        statusRow.Children.Add(new Ellipse { Width = 7, Height = 7, Fill = Green, VerticalAlignment = VerticalAlignment.Center });
        statusRow.Children.Add(new TextBlock
        {
            Text = Localization.T("Работает в фоне", "Running in background"),
            Foreground = TextSecondary,
            FontSize = 11,
            Margin = new Thickness(8, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        });
        status.Child = statusRow;
        stack.Children.Add(status);

        var version = new TextBlock
        {
            Text = "v" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3),
            Foreground = TextMuted,
            FontSize = 10,
            Margin = new Thickness(5, 12, 0, 0)
        };
        stack.Children.Add(version);

        return panel;
    }

    private static void AddSidebarGroup(Panel parent, string title, string[] items)
    {
        parent.Children.Add(new TextBlock
        {
            Text = title.ToUpperInvariant(),
            Foreground = TextMuted,
            FontSize = 9,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(5, 0, 0, 8)
        });

        for (int i = 0; i < items.Length; i++)
        {
            var selected = i == 0 && title != Localization.T("Прочее", "Other");
            var button = new Border
            {
                Background = selected ? AccentSoft : Brushes.Transparent,
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10, 8, 10, 8),
                Margin = new Thickness(0, 0, 0, 3)
            };
            button.Child = new TextBlock
            {
                Text = items[i],
                Foreground = selected ? TextPrimary : TextSecondary,
                FontSize = 12,
                FontWeight = selected ? FontWeights.SemiBold : FontWeights.Normal
            };
            parent.Children.Add(button);
        }

        parent.Children.Add(new Border { Height = 10, Background = Brushes.Transparent });
    }

    private UIElement BuildHeader()
    {
        var header = new Grid { Margin = new Thickness(28, 0, 28, 0) };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var titleStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        titleStack.Children.Add(new TextBlock
        {
            Text = Localization.T("Общие настройки", "General settings"),
            Foreground = TextPrimary,
            FontSize = 19,
            FontWeight = FontWeights.SemiBold
        });
        titleStack.Children.Add(new TextBlock
        {
            Text = Localization.SettingsSubtitle,
            Foreground = TextSecondary,
            FontSize = 11,
            Margin = new Thickness(0, 4, 0, 0)
        });
        header.Children.Add(titleStack);

        var close = CreateWindowButton("×", 34);
        close.Click += (_, _) => Close();
        Grid.SetColumn(close, 1);
        header.Children.Add(close);

        header.MouseLeftButtonDown += (_, e) =>
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        };

        return header;
    }

    private UIElement BuildMainContent()
    {
        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Margin = new Thickness(28, 0, 28, 0)
        };
        Grid.SetRow(scroll, 1);

        var main = new Grid();
        main.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        main.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(315) });
        scroll.Content = main;

        var settingsColumn = new StackPanel();
        settingsColumn.Children.Add(CreatePresenceCard());
        settingsColumn.Children.Add(CreateDisplayCard());
        settingsColumn.Children.Add(CreateGeneralCard());
        Grid.SetColumn(settingsColumn, 0);
        main.Children.Add(settingsColumn);

        var preview = CreatePreviewColumn();
        Grid.SetColumn(preview, 1);
        main.Children.Add(preview);

        return scroll;
    }

    private Border CreatePresenceCard()
    {
        var card = CreateCard();
        var stack = new StackPanel();
        card.Child = stack;
        AddCardHeader(stack, Localization.T("Discord Rich Presence", "Discord Rich Presence"), Localization.T("Управление тем, что отправляется в Discord", "Control what is sent to Discord"));
        stack.Children.Add(CreateToggleRow(showAlbumArt));
        stack.Children.Add(CreateToggleRow(showProgress));
        stack.Children.Add(CreateToggleRow(showPaused));
        stack.Children.Add(CreateToggleRow(telegramOnly));
        return card;
    }

    private Border CreateDisplayCard()
    {
        var card = CreateCard();
        var stack = new StackPanel();
        card.Child = stack;
        AddCardHeader(stack, Localization.T("Формат присутствия", "Presence format"), Localization.Hint.Replace("\n", "  •  "));
        stack.Children.Add(CreateField(Localization.DetailsLabel, detailsFormat, "{title}"));
        stack.Children.Add(CreateField(Localization.StateLabel, stateFormat, "{artist}"));
        return card;
    }

    private Border CreateGeneralCard()
    {
        var card = CreateCard();
        var stack = new StackPanel();
        card.Child = stack;
        AddCardHeader(stack, Localization.T("Общие", "General"), Localization.T("Поведение приложения", "Application behavior"));

        var languageRow = new Grid { Margin = new Thickness(0, 2, 0, 8) };
        languageRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        languageRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        languageRow.Children.Add(new TextBlock
        {
            Text = Localization.LanguageLabel,
            Foreground = TextPrimary,
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center
        });
        Grid.SetColumn(language, 1);
        languageRow.Children.Add(language);
        stack.Children.Add(languageRow);
        stack.Children.Add(CreateToggleRow(startWithWindows));
        return card;
    }

    private StackPanel CreatePreviewColumn()
    {
        var column = new StackPanel();
        column.Children.Add(new TextBlock
        {
            Text = Localization.T("Предпросмотр", "Preview"),
            Foreground = TextPrimary,
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(3, 2, 0, 9)
        });
        column.Children.Add(CreateDiscordPreview());

        var tip = new Border
        {
            Background = BrushFromHex("#111318"),
            BorderBrush = Border,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(13),
            Margin = new Thickness(0, 12, 0, 0)
        };
        tip.Child = new TextBlock
        {
            Text = Localization.T("Совет\nИзменяй шаблоны слева — будущая версия получит полноценный предпросмотр Discord.", "Tip\nChange the templates on the left — the next version can turn this into a live Discord preview."),
            Foreground = TextSecondary,
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            LineHeight = 17
        };
        column.Children.Add(tip);
        return column;
    }

    private Border CreateDiscordPreview()
    {
        var card = new Border
        {
            Background = BrushFromHex("#181A1F"),
            BorderBrush = Border,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(14)
        };

        var stack = new StackPanel();
        card.Child = stack;

        var appRow = new Grid { Margin = new Thickness(0, 0, 0, 12) };
        appRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        appRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        appRow.Children.Add(new Ellipse
        {
            Width = 8,
            Height = 8,
            Fill = Green,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(1, 0, 8, 0)
        });
        var appText = new TextBlock
        {
            Text = "Mim0 | TelegramRPC",
            Foreground = TextSecondary,
            FontSize = 10,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(appText, 1);
        appRow.Children.Add(appText);
        stack.Children.Add(appRow);

        var artworkRow = new Grid();
        artworkRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(72) });
        artworkRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var art = new Border
        {
            Width = 64,
            Height = 64,
            CornerRadius = new CornerRadius(10),
            Background = Accent,
            Child = new TextBlock
            {
                Text = "♪",
                Foreground = Brushes.White,
                FontSize = 30,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
        artworkRow.Children.Add(art);

        var info = new StackPanel { Margin = new Thickness(10, 0, 0, 0) };
        info.Children.Add(new TextBlock { Text = "Listening to", Foreground = TextMuted, FontSize = 9 });
        info.Children.Add(new TextBlock { Text = "Track title", Foreground = TextPrimary, FontSize = 12, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 3, 0, 0), TextTrimming = TextTrimming.CharacterEllipsis });
        info.Children.Add(new TextBlock { Text = "Artist name", Foreground = TextSecondary, FontSize = 10, Margin = new Thickness(0, 3, 0, 0), TextTrimming = TextTrimming.CharacterEllipsis });
        info.Children.Add(new TextBlock { Text = "Telegram", Foreground = TextMuted, FontSize = 9, Margin = new Thickness(0, 6, 0, 0) });
        Grid.SetColumn(info, 1);
        artworkRow.Children.Add(info);
        stack.Children.Add(artworkRow);

        var progress = new ProgressBar
        {
            Height = 3,
            Minimum = 0,
            Maximum = 100,
            Value = 42,
            Background = BrushFromHex("#2A2D34"),
            Foreground = Accent,
            Margin = new Thickness(0, 14, 0, 6)
        };
        stack.Children.Add(progress);
        stack.Children.Add(new Grid
        {
            Children =
            {
                new TextBlock { Text = "1:24", Foreground = TextMuted, FontSize = 8 },
                new TextBlock { Text = "3:42", Foreground = TextMuted, FontSize = 8, HorizontalAlignment = HorizontalAlignment.Right }
            }
        });

        return card;
    }

    private UIElement BuildFooter()
    {
        var footer = new Grid { Margin = new Thickness(28, 0, 28, 0) };
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var reset = CreateButton(Localization.Default, false);
        reset.Click += (_, _) => ResetDefaults();
        footer.Children.Add(reset);

        var cancel = CreateButton(Localization.Cancel, false);
        cancel.Margin = new Thickness(8, 0, 0, 0);
        cancel.Click += (_, _) => Close();
        Grid.SetColumn(cancel, 1);
        footer.Children.Add(cancel);

        var save = CreateButton(Localization.Save, true);
        save.Margin = new Thickness(8, 0, 0, 0);
        save.Click += (_, _) => SaveAndClose();
        Grid.SetColumn(save, 2);
        footer.Children.Add(save);

        return footer;
    }

    private static Border CreateCard()
    {
        return new Border
        {
            Background = Card,
            BorderBrush = Border,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(13),
            Padding = new Thickness(15),
            Margin = new Thickness(0, 0, 0, 12)
        };
    }

    private static void AddCardHeader(Panel parent, string title, string subtitle)
    {
        var stack = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };
        stack.Children.Add(new TextBlock { Text = title, Foreground = TextPrimary, FontSize = 13, FontWeight = FontWeights.SemiBold });
        stack.Children.Add(new TextBlock { Text = subtitle, Foreground = TextMuted, FontSize = 10, Margin = new Thickness(0, 3, 0, 0), TextWrapping = TextWrapping.Wrap });
        parent.Children.Add(stack);
    }

    private static Border CreateToggleRow(CheckBox checkBox)
    {
        var row = new Border
        {
            Background = Brushes.Transparent,
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(8, 7, 8, 7),
            Margin = new Thickness(-8, 0, -8, 1)
        };
        row.Child = checkBox;
        return row;
    }

    private static Border CreateField(string label, TextBox textBox, string example)
    {
        var stack = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };
        stack.Children.Add(new TextBlock { Text = label, Foreground = TextSecondary, FontSize = 10, Margin = new Thickness(0, 0, 0, 5) });
        stack.Children.Add(textBox);
        stack.Children.Add(new TextBlock { Text = example, Foreground = TextMuted, FontSize = 9, Margin = new Thickness(2, 4, 0, 0) });
        return new Border { Child = stack };
    }

    private static CheckBox CreateCheckBox(string text, bool value)
    {
        var box = new CheckBox
        {
            Content = text,
            IsChecked = value,
            Foreground = TextPrimary,
            FontSize = 11,
            Padding = new Thickness(0),
            Margin = new Thickness(0),
            HorizontalContentAlignment = HorizontalAlignment.Left
        };
        return box;
    }

    private static TextBox CreateTextBox(string text)
    {
        return new TextBox
        {
            Text = text,
            Height = 34,
            Background = CardAlt,
            Foreground = TextPrimary,
            BorderBrush = Border,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(10, 7, 10, 7),
            FontSize = 11
        };
    }

    private ComboBox CreateLanguageComboBox()
    {
        var combo = new ComboBox
        {
            Width = 125,
            Height = 32,
            Background = CardAlt,
            Foreground = TextPrimary,
            BorderBrush = Border,
            BorderThickness = new Thickness(1),
            FontSize = 11,
            ItemsSource = new[] { Localization.Russian, Localization.English },
            SelectedIndex = Settings.Language == "en" ? 1 : 0
        };
        return combo;
    }

    private static Button CreateButton(string text, bool primary)
    {
        var button = new Button
        {
            Content = text,
            Height = 36,
            MinWidth = primary ? 92 : 92,
            Padding = new Thickness(16, 0, 16, 0),
            Background = primary ? Accent : CardAlt,
            Foreground = primary ? Brushes.White : TextPrimary,
            BorderBrush = primary ? Accent : Border,
            BorderThickness = new Thickness(1),
            FontSize = 11,
            FontWeight = primary ? FontWeights.SemiBold : FontWeights.Normal
        };
        return button;
    }

    private static Button CreateWindowButton(string text, double size)
    {
        return new Button
        {
            Content = text,
            Width = size,
            Height = size,
            Background = Brushes.Transparent,
            Foreground = TextSecondary,
            BorderThickness = new Thickness(0),
            FontSize = 20,
            Padding = new Thickness(0),
            VerticalContentAlignment = VerticalAlignment.Center,
            HorizontalContentAlignment = HorizontalAlignment.Center
        };
    }

    private void ResetDefaults()
    {
        showAlbumArt.IsChecked = true;
        showProgress.IsChecked = true;
        showPaused.IsChecked = true;
        telegramOnly.IsChecked = true;
        startWithWindows.IsChecked = false;
        detailsFormat.Text = "{title}";
        stateFormat.Text = "{artist}";
        language.SelectedIndex = 0;
    }

    private void SaveAndClose()
    {
        var details = string.IsNullOrWhiteSpace(detailsFormat.Text) ? "{title}" : detailsFormat.Text.Trim();
        var state = string.IsNullOrWhiteSpace(stateFormat.Text) ? "{artist}" : stateFormat.Text.Trim();
        var selectedLanguage = language.SelectedIndex == 1 ? "en" : "ru";

        var newSettings = new AppSettings
        {
            ShowAlbumArt = showAlbumArt.IsChecked == true,
            ShowProgress = showProgress.IsChecked == true,
            ShowPausedState = showPaused.IsChecked == true,
            TelegramOnly = telegramOnly.IsChecked == true,
            StartWithWindows = startWithWindows.IsChecked == true,
            DetailsFormat = details,
            StateFormat = state,
            Language = selectedLanguage
        };

        try
        {
            SettingsStore.Save(newSettings);
            SettingsStore.ApplyStartup(newSettings.StartWithWindows);
            Settings = newSettings;
            Localization.Configure(Settings.Language);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(this, $"{Localization.SaveError}:\n{ex.Message}", "Mim0", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static Brush BrushFromHex(string hex) =>
        (Brush)new BrushConverter().ConvertFromString(hex)!;
}
