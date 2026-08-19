using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Mim0.TelegramRPC;

internal sealed record UpdateInfo(string Version, string TagName, string InstallerUrl);

internal static class UpdateChecker
{
    private const string LatestReleaseUrl = "https://api.github.com/repos/TheKannabisKannibal/Mim0-TelegramRPC/releases/latest";
    private static readonly HttpClient Http = CreateClient();

    public static async Task<UpdateInfo?> CheckAsync(string currentVersion, CancellationToken cancellationToken = default)
    {
        using var response = await Http.GetAsync(LatestReleaseUrl, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = json.RootElement;

        var tagName = root.TryGetProperty("tag_name", out var tag) ? tag.GetString() : null;
        if (string.IsNullOrWhiteSpace(tagName))
            return null;

        var version = tagName.Trim().TrimStart('v', 'V');
        if (!Version.TryParse(version, out var latest) || !Version.TryParse(currentVersion, out var current))
            return null;

        if (latest <= current)
            return null;

        if (!root.TryGetProperty("assets", out var assets) || assets.ValueKind != JsonValueKind.Array)
            return null;

        foreach (var asset in assets.EnumerateArray())
        {
            var name = asset.TryGetProperty("name", out var nameElement) ? nameElement.GetString() : null;
            var url = asset.TryGetProperty("browser_download_url", out var urlElement) ? urlElement.GetString() : null;

            if (string.Equals(name, "Mim0.TelegramRPC.Setup.exe", StringComparison.OrdinalIgnoreCase) &&
                Uri.TryCreate(url, UriKind.Absolute, out var installerUri) &&
                installerUri.Scheme == Uri.UriSchemeHttps)
            {
                return new UpdateInfo(version, tagName, installerUri.ToString());
            }
        }

        return null;
    }

    public static async Task<bool> DownloadAndLaunchInstallerAsync(UpdateInfo update, CancellationToken cancellationToken = default)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "Mim0-TelegramRPC", "updates");
        Directory.CreateDirectory(tempDir);

        var installerPath = Path.Combine(tempDir, $"Mim0.TelegramRPC.Setup-v{update.Version}.exe");
        if (File.Exists(installerPath))
            File.Delete(installerPath);

        using var response = await Http.GetAsync(update.InstallerUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using (var input = await response.Content.ReadAsStreamAsync(cancellationToken))
        await using (var output = File.Create(installerPath))
        {
            await input.CopyToAsync(output, cancellationToken);
        }

        Process.Start(new ProcessStartInfo { FileName = installerPath, UseShellExecute = true });
        return true;
    }

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Mim0-TelegramRPC", "1.3.0"));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        return client;
    }
}
