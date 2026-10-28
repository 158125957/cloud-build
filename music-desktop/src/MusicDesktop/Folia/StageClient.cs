using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace MusicDesktop.Folia;

public sealed class StageCurrent
{
    public string Id { get; set; } = "";
    public string Source { get; set; } = "";
    public string Title { get; set; } = "";
    public string Artist { get; set; } = "";
    public string Album { get; set; } = "";
    public double DurationMs { get; set; }
    public string? CoverUrl { get; set; }
}

public sealed class StageSnapshot
{
    public StageCurrent? Current { get; set; }
    public string PlayerState { get; set; } = "IDLE";
    public double PositionMs { get; set; }
    public double DurationMs { get; set; }
    public long SampledAtMs { get; set; }
    public bool CanSeek { get; set; }
    public bool CanNext { get; set; }
    public bool CanPrev { get; set; }
}

public sealed class StageClient
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromMilliseconds(1500) };
    public string? LastError { get; private set; }
    public bool TokenRejected { get; private set; }

    private static string Base(int port) => $"http://127.0.0.1:{port}";

    private HttpRequestMessage Req(HttpMethod m, string url, string token, string? json = null)
    {
        var req = new HttpRequestMessage(m, url);
        if (!string.IsNullOrWhiteSpace(token))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Trim());
        if (json != null)
            req.Content = new StringContent(json, Encoding.UTF8, "application/json");
        return req;
    }

    public async Task<(bool ok, StageSnapshot? snapshot, string? error)> GetStatusAsync(int port, string token, CancellationToken ct = default)
    {
        try
        {
            using var resp = await _http.SendAsync(Req(HttpMethod.Get, $"{Base(port)}/stage/player/status", token), ct).ConfigureAwait(false);
            if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                TokenRejected = true;
                LastError = "Token 被拒绝（401）";
                return (false, null, LastError);
            }
            if (!resp.IsSuccessStatusCode)
            {
                LastError = $"HTTP {(int)resp.StatusCode}";
                return (false, null, LastError);
            }

            var raw = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            var snap = new StageSnapshot
            {
                PlayerState = root.TryGetProperty("playerState", out var ps) ? ps.GetString() ?? "IDLE" : "IDLE",
                PositionMs = root.TryGetProperty("positionMs", out var p) ? p.GetDouble() : 0,
                DurationMs = root.TryGetProperty("durationMs", out var d) ? d.GetDouble() : 0,
                SampledAtMs = root.TryGetProperty("sampledAtMs", out var s) ? s.GetInt64() : 0,
            };

            if (root.TryGetProperty("current", out var cur) && cur.ValueKind == JsonValueKind.Object)
            {
                snap.Current = new StageCurrent
                {
                    Id = Str(cur, "id"),
                    Source = Str(cur, "source"),
                    Title = Str(cur, "title"),
                    Artist = Str(cur, "artist"),
                    Album = Str(cur, "album"),
                    DurationMs = cur.TryGetProperty("durationMs", out var cd) ? cd.GetDouble() : 0,
                    CoverUrl = cur.TryGetProperty("coverUrl", out var cv) && cv.ValueKind == JsonValueKind.String ? cv.GetString() : null,
                };
            }

            if (root.TryGetProperty("controlCapabilities", out var caps) && caps.ValueKind == JsonValueKind.Object)
            {
                snap.CanSeek = Bool(caps, "seek");
                snap.CanNext = Bool(caps, "next");
                snap.CanPrev = Bool(caps, "previous");
            }

            TokenRejected = false;
            LastError = null;
            return (true, snap, null);
        }
        catch (TaskCanceledException)
        {
            LastError = "超时";
            return (false, null, LastError);
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            return (false, null, LastError);
        }
    }

    public async Task<double?> GetTimeAsync(int port, string token)
    {
        try
        {
            using var resp = await _http.SendAsync(Req(HttpMethod.Get, $"{Base(port)}/stage/player/time", token)).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode) return null;
            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync().ConfigureAwait(false));
            return doc.RootElement.TryGetProperty("positionMs", out var p) ? p.GetDouble() : null;
        }
        catch { return null; }
    }

    public async Task<bool> ControlAsync(int port, string token, string action, double? positionMs = null)
    {
        try
        {
            var json = positionMs.HasValue
                ? $"{{\"action\":\"{action}\",\"positionMs\":{(long)positionMs.Value}}}"
                : $"{{\"action\":\"{action}\"}}";
            using var resp = await _http.SendAsync(Req(HttpMethod.Post, $"{Base(port)}/stage/player/control", token, json)).ConfigureAwait(false);
            return resp.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            return false;
        }
    }

    public async Task<bool> PingAsync(int port)
    {
        try
        {
            using var resp = await _http.GetAsync($"{Base(port)}/stage/health");
            return resp.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    private static string Str(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";

    private static bool Bool(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.True;
}
