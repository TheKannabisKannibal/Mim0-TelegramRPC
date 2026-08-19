using Avalonia;
using System.Runtime.CompilerServices;

namespace Mim0.TelegramRPC;

internal static class AvaloniaBootstrap
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        AppBuilder.Configure<AvaloniaApp>()
            .UsePlatformDetect()
            .SetupWithoutStarting();
    }
}
