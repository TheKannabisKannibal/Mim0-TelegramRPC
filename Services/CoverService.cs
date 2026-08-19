using System.Net.Http.Headers;
using Windows.Media.Control;
using Windows.Storage.Streams;

namespace Mim0.TelegramRPC;

internal sealed class CoverService : IDisposable
{
    private const string LitterboxEndpoint = "https://litterbox.catbox.moe/resources/internals/api.php";
    private const int MaxCacheEntries = 32;
    private static readonly TimeSpan CacheLifetime = TimeSpan.FromMinutes(50);

    private readonly HttpClient http = new();
    private readonly Dictionary<string, CacheEntry> cache = new(StringComparer.Ordinal);

    public CoverService()
    {
        http.Timeout = TimeSpan.FromSeconds(20);
        http.DefaultRequestHeaders.UserAgent.ParseAdd("Mim0-TelegramRPC/1.5");
    }

    public async Task<string?> GetCoverUrlAsync(
        GlobalSystemMediaTransportControlsSessionMediaProperties props,
        string trackSignature)
    {
        if (cache.TryGetValue(trackSignature, out var cached) && cached.ExpiresUtc > DateTime.UtcNow)
            return cached.Url;

        cache.Remove(trackSignature);

        var bytes = await ReadThumbnailAsync(props);
        if (bytes == null)
            return null;

        var url = await UploadAsync(bytes.Value.Bytes, bytes.Value.ContentType, bytes.Value.Extension);
        if (url == null)
            return null;

        if (cache.Count >= MaxCacheEntries)
        {
            var oldest = cache.OrderBy(x => x.Value.CreatedUtc).FirstOrDefault();
            if (!string.IsNullOrEmpty(oldest.Key))
                cache.Remove(oldest.Key);
        }

        cache[trackSignature] = new CacheEntry(url, DateTime.UtcNow, DateTime.UtcNow + CacheLifetime);
        return url;
    }

    private static async Task<ThumbnailData?> ReadThumbnailAsync(
        GlobalSystemMediaTransportControlsSessionMediaProperties props)
    {
        try
        {
            if (props.Thumbnail == null)
                return null;

            using var ras = await props.Thumbnail.OpenReadAsync();
            if (ras.Size <= 0 || ras.Size > 15 * 1024 * 1024)
                return null;

            using var reader = new DataReader(ras.GetInputStreamAt(0));
            await reader.LoadAsync((uint)ras.Size);
            var bytes = new byte[(int)ras.Size];
            reader.ReadBytes(bytes);

            if (bytes.Length == 0)
                return null;

            return DetectImage(bytes);
        }
        catch
        {
            return null;
        }
    }

    private async Task<string?> UploadAsync(byte[] bytes, string contentType, string extension)
    {
        try
        {
            using var form = new MultipartFormDataContent();
            form.Add(new StringContent("fileupload"), "reqtype");
            form.Add(new StringContent("1h"), "time");

            using var file = new ByteArrayContent(bytes);
            file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            form.Add(file, "fileToUpload", $"cover{extension}");

            using var response = await http.PostAsync(LitterboxEndpoint, form);
            if (!response.IsSuccessStatusCode)
                return null;

            var url = (await response.Content.ReadAsStringAsync()).Trim();
            return Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
                   uri.Scheme == Uri.UriSchemeHttps
                ? url
                : null;
        }
        catch
        {
            return null;
        }
    }

    private static ThumbnailData? DetectImage(byte[] bytes)
    {
        if (bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
            return new ThumbnailData(bytes, "image/jpeg", ".jpg");

        if (bytes.Length >= 8 && bytes[0] == 0x89 && bytes[1] == 0x50 &&
            bytes[2] == 0x4E && bytes[3] == 0x47)
            return new ThumbnailData(bytes, "image/png", ".png");

        if (bytes.Length >= 12 && bytes[0] == 'R' && bytes[1] == 'I' &&
            bytes[2] == 'F' && bytes[3] == 'F' && bytes[8] == 'W' &&
            bytes[9] == 'E' && bytes[10] == 'B' && bytes[11] == 'P')
            return new ThumbnailData(bytes, "image/webp", ".webp");

        return null;
    }

    public void Clear() => cache.Clear();

    public void Dispose() => http.Dispose();

    private readonly record struct ThumbnailData(byte[] Bytes, string ContentType, string Extension);
    private readonly record struct CacheEntry(string Url, DateTime CreatedUtc, DateTime ExpiresUtc);
}
