using System.Net.Http;
using System.Text.Json;

namespace MusicDesktop.Folia;

public enum LyricFetchStatus { Ok, NoLyric, Unreachable }

public sealed class LyricClient
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromMilliseconds(1200) };

    public async Task<(LyricFetchStatus status, LyricData? data, string? error)> FetchAsync(int port, CancellationToken ct = default)
    {
        var url = $"http://127.0.0.1:{port}/v1/lyric";
        try
        {
            using var resp = await _http.GetAsync(url, ct).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
                return (LyricFetchStatus.Unreachable, null, $"HTTP {(int)resp.StatusCode}");

            var raw = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(raw) || raw.Trim() == "null")
                return (LyricFetchStatus.NoLyric, null, null);

            var data = JsonSerializer.Deserialize<LyricData>(raw);
            return data == null
                ? (LyricFetchStatus.NoLyric, null, null)
                : (LyricFetchStatus.Ok, data, null);
        }
        catch (TaskCanceledException)
        {
            return (LyricFetchStatus.Unreachable, null, "超时");
        }
        catch (Exception ex)
        {
            return (LyricFetchStatus.Unreachable, null, ex.Message);
        }
    }
}
