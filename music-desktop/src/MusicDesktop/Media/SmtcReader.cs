using System.IO;
using System.Windows.Media.Imaging;
using Windows.Media.Control;

namespace MusicDesktop.Media;

public sealed class SmtcSnapshot
{
    public string AppId { get; set; } = "";
    public string Title { get; set; } = "";
    public string Artist { get; set; } = "";
    public string Album { get; set; } = "";
    public double PositionMs { get; set; }
    public double DurationMs { get; set; }
    public bool IsPlaying { get; set; }
    public bool CanSeek { get; set; }
    public BitmapSource? Cover { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// 读取 Windows 全局媒体会话（SMTC）。
/// 注意：Session 0 / 服务上下文里 RequestAsync 会抛 0x80070424，必须吞掉并降级。
/// </summary>
public sealed class SmtcReader
{
    private GlobalSystemMediaTransportControlsSessionManager? _manager;
    private GlobalSystemMediaTransportControlsSession? _session;
    private string _coverKey = "";
    private BitmapSource? _cover;
    private DateTime _lastProbe = DateTime.MinValue;

    public bool Available { get; private set; }
    public string? LastError { get; private set; }

    public async Task<SmtcSnapshot?> ReadAsync(bool forceProbe = false)
    {
        try
        {
            if (_manager == null && (forceProbe || (DateTime.UtcNow - _lastProbe).TotalSeconds > 10))
            {
                _lastProbe = DateTime.UtcNow;
                _manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            }
            if (_manager == null) return null;

            var sessions = _manager.GetSessions();
            var candidate = _manager.GetCurrentSession();
            if (candidate == null && sessions.Count > 0) candidate = sessions[0];
            if (candidate == null) { Available = false; return null; }

            _session = candidate;
            var props = await _session.TryGetMediaPropertiesAsync();
            var info = _session.GetPlaybackInfo();
            var timeline = _session.GetTimelineProperties();

            var snap = new SmtcSnapshot
            {
                AppId = candidate.SourceAppUserModelId ?? "",
                Title = props?.Title ?? "",
                Artist = props?.Artist ?? "",
                Album = props?.AlbumTitle ?? "",
                PositionMs = timeline?.Position.TotalMilliseconds ?? 0,
                DurationMs = timeline?.EndTime.TotalMilliseconds ?? 0,
                IsPlaying = info?.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing,
                CanSeek = (info?.Controls?.IsPlaybackPositionEnabled ?? false),
                Error = null,
            };

            var key = snap.AppId + "|" + snap.Title + "|" + snap.Artist;
            if (key != _coverKey)
            {
                _coverKey = key;
                _cover = null;
                try
                {
                    var thumb = props?.Thumbnail;
                    if (thumb != null)
                    {
                        using var stream = await thumb.OpenReadAsync();
                        using var ms = new MemoryStream();
                        await stream.AsStreamForRead().CopyToAsync(ms);
                        ms.Position = 0;
                        var bmp = new BitmapImage();
                        bmp.BeginInit();
                        bmp.CacheOption = BitmapCacheOption.OnLoad;
                        bmp.StreamSource = ms;
                        bmp.EndInit();
                        bmp.Freeze();
                        _cover = bmp;
                    }
                }
                catch { /* 封面失败不影响主流程 */ }
            }

            snap.Cover = _cover;
            Available = true;
            LastError = null;
            return snap;
        }
        catch (Exception ex)
        {
            Available = false;
            LastError = ex.Message;
            _manager = null;
            return null;
        }
    }

    public async Task TogglePlayPauseAsync()
    {
        try { if (_session != null) await _session.TryTogglePlayPauseAsync(); } catch { }
    }

    public async Task NextAsync()
    {
        try { if (_session != null) await _session.TrySkipNextAsync(); } catch { }
    }

    public async Task PreviousAsync()
    {
        try { if (_session != null) await _session.TrySkipPreviousAsync(); } catch { }
    }

    public async Task SeekAsync(double positionMs)
    {
        try
        {
            if (_session != null)
                await _session.TryChangePlaybackPositionAsync((long)(positionMs * 10000));
        }
        catch { }
    }
}
