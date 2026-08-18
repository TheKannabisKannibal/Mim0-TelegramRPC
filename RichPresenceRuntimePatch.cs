using System.Reflection;
using System.Runtime.CompilerServices;
using DiscordRPC;

namespace Mim0.TelegramRPC;

// Test-only diagnostic patch.
// Discord's current RPC docs describe timestamps for Listening activities,
// but the existing build still renders only the timer on some clients.
// This temporarily changes the activity name to Spotify while preserving
// Mim0's track data. If the progress bar appears, we have isolated a
// Discord-side Spotify special case rather than a timestamp calculation bug.
internal static class RichPresenceRuntimePatch
{
    private static readonly FieldInfo DiscordField =
        typeof(Program).GetField("discord", BindingFlags.NonPublic | BindingFlags.Static)!;

    private static int started;

    [ModuleInitializer]
    internal static void Initialize()
    {
        if (Interlocked.Exchange(ref started, 1) != 0)
            return;

        _ = Task.Run(RunAsync);
    }

    private static async Task RunAsync()
    {
        await Task.Delay(TimeSpan.FromSeconds(4));

        while (true)
        {
            try
            {
                if (TryPatchPresence())
                    await Task.Delay(TimeSpan.FromSeconds(5));
                else
                    await Task.Delay(TimeSpan.FromSeconds(2));
            }
            catch
            {
                await Task.Delay(TimeSpan.FromSeconds(3));
            }
        }
    }

    private static bool TryPatchPresence()
    {
        if (DiscordField.GetValue(null) is not DiscordRpcClient discord ||
            !discord.IsInitialized ||
            discord.CurrentPresence == null)
            return false;

        var presence = discord.CurrentPresence.Clone();

        // Diagnostic switch: Spotify is known to have a dedicated music
        // activity presentation. Track details/state/assets stay untouched.
        presence.Name = "Spotify";
        presence.Type = ActivityType.Listening;

        discord.SetPresence(presence);
        return true;
    }
}
