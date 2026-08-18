using System.Diagnostics;
using System.IO.Pipes;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using DiscordRPC;

namespace Mim0.TelegramRPC;

// Test-only diagnostic patch.
// The normal DiscordRPC library sends type=2 and timestamps, but it does not
// expose the activity.name field. This patch sends a second raw SET_ACTIVITY
// payload over Discord IPC so we can test the Spotify-style activity name
// without changing the normal application code.
internal static class RichPresenceRuntimePatch
{
    private const string DiscordApplicationId = "1538974940643070062";
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
        await Task.Delay(TimeSpan.FromSeconds(5));

        while (true)
        {
            try
            {
                await PushRawActivityAsync();
            }
            catch
            {
                // The normal RPC path remains active if raw IPC is unavailable.
            }

            await Task.Delay(TimeSpan.FromSeconds(5));
        }
    }

    private static async Task PushRawActivityAsync()
    {
        if (DiscordField.GetValue(null) is not DiscordRpcClient discord ||
            !discord.IsInitialized ||
            discord.CurrentPresence == null)
            return;

        var presence = discord.CurrentPresence;
        var activity = new Dictionary<string, object?>
        {
            ["name"] = "Spotify", // diagnostic: isolate Discord's Spotify-specific rendering
            ["type"] = (int)ActivityType.Listening,
            ["details"] = presence.Details,
            ["state"] = presence.State
        };

        if (presence.Assets != null)
        {
            activity["assets"] = new Dictionary<string, object?>
            {
                ["large_image"] = presence.Assets.LargeImageKey,
                ["large_text"] = presence.Assets.LargeImageText,
                ["small_image"] = presence.Assets.SmallImageKey,
                ["small_text"] = presence.Assets.SmallImageText
            };
        }

        if (presence.Timestamps != null)
        {
            var timestamps = new Dictionary<string, object?>();
            if (presence.Timestamps.StartUnixMilliseconds.HasValue)
                timestamps["start"] = presence.Timestamps.StartUnixMilliseconds.Value;
            if (presence.Timestamps.EndUnixMilliseconds.HasValue)
                timestamps["end"] = presence.Timestamps.EndUnixMilliseconds.Value;
            if (timestamps.Count > 0)
                activity["timestamps"] = timestamps;
        }

        if (presence.Buttons is { Length: > 0 })
        {
            activity["buttons"] = presence.Buttons
                .Take(2)
                .Select(button => new Dictionary<string, object?>
                {
                    ["label"] = button.Label,
                    ["url"] = button.Url
                })
                .ToArray();
        }

        using var pipe = await ConnectAsync();
        if (pipe == null)
            return;

        await WriteFrameAsync(pipe, 0, JsonSerializer.Serialize(new
        {
            v = 1,
            client_id = DiscordApplicationId
        }));

        // Consume the READY/handshake response before SET_ACTIVITY.
        await ReadFrameAsync(pipe);

        await WriteFrameAsync(pipe, 1, JsonSerializer.Serialize(new
        {
            cmd = "SET_ACTIVITY",
            args = new
            {
                pid = Process.GetCurrentProcess().Id,
                activity,
                nonce = Guid.NewGuid().ToString()
            }
        }));
    }

    private static async Task<NamedPipeClientStream?> ConnectAsync()
    {
        for (var index = 0; index < 10; index++)
        {
            var pipe = new NamedPipeClientStream(
                ".",
                $"discord-ipc-{index}",
                PipeDirection.InOut,
                PipeOptions.Asynchronous);

            try
            {
                await pipe.ConnectAsync(750);
                return pipe;
            }
            catch
            {
                pipe.Dispose();
            }
        }

        return null;
    }

    private static async Task WriteFrameAsync(NamedPipeClientStream pipe, int opcode, string json)
    {
        var payload = Encoding.UTF8.GetBytes(json);
        var header = new byte[8];
        BitConverter.GetBytes(opcode).CopyTo(header, 0);
        BitConverter.GetBytes(payload.Length).CopyTo(header, 4);

        await pipe.WriteAsync(header);
        await pipe.WriteAsync(payload);
        await pipe.FlushAsync();
    }

    private static async Task<string> ReadFrameAsync(NamedPipeClientStream pipe)
    {
        var header = new byte[8];
        await ReadExactlyAsync(pipe, header);
        var length = BitConverter.ToInt32(header, 4);
        if (length <= 0 || length > 1024 * 1024)
            return string.Empty;

        var payload = new byte[length];
        await ReadExactlyAsync(pipe, payload);
        return Encoding.UTF8.GetString(payload);
    }

    private static async Task ReadExactlyAsync(Stream stream, byte[] buffer)
    {
        var offset = 0;
        while (offset < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(offset, buffer.Length - offset));
            if (read == 0)
                throw new EndOfStreamException();
            offset += read;
        }
    }
}
