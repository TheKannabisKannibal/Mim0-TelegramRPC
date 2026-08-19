using Avalonia;
using Avalonia.Markup.Xaml;

namespace Mim0.TelegramRPC;

internal sealed class AvaloniaApp : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);
}
