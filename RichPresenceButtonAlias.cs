namespace Mim0.TelegramRPC;

// Test branch helper: avoids the name collision between DiscordRPC.Button
// and System.Windows.Forms.Button used by the existing Program.cs.
internal sealed class Button : DiscordRPC.Button
{
}
