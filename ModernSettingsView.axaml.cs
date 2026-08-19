using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace Mim0.TelegramRPC;

internal sealed partial class ModernSettingsView : UserControl
{
    private readonly Action<AppSettings?> _close;
    private AppSettings settings;

    public ModernSettingsView(AppSettings source, Action<AppSettings?> close)
    {
        _close = close;
        settings = source.Clone();
        InitializeComponent();

        AlbumArt.IsChecked = settings.ShowAlbumArt;
        Progress.IsChecked = settings.ShowProgress;
        Paused.IsChecked = settings.ShowPausedState;
        TelegramOnly.IsChecked = settings.TelegramOnly;
        Startup.IsChecked = settings.StartWithWindows;
        Details.Text = settings.DetailsFormat;
        State.Text = settings.StateFormat;
        Language.SelectedIndex = settings.Language == "en" ? 1 : 0;
    }

    private void Save_Click(object? sender, RoutedEventArgs e)
    {
        settings.ShowAlbumArt = AlbumArt.IsChecked == true;
        settings.ShowProgress = Progress.IsChecked == true;
        settings.ShowPausedState = Paused.IsChecked == true;
        settings.TelegramOnly = TelegramOnly.IsChecked == true;
        settings.StartWithWindows = Startup.IsChecked == true;
        settings.DetailsFormat = string.IsNullOrWhiteSpace(Details.Text) ? "{title}" : Details.Text.Trim();
        settings.StateFormat = string.IsNullOrWhiteSpace(State.Text) ? "{artist}" : State.Text.Trim();
        settings.Language = Language.SelectedIndex == 1 ? "en" : "ru";
        _close(settings);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => _close(null);
    private void Close_Click(object? sender, RoutedEventArgs e) => _close(null);
}
