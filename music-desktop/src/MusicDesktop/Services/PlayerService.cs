using System.IO;
using System.Net.Http;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using MusicDesktop.Core;
using MusicDesktop.Folia;
using MusicDesktop.Media;

namespace MusicDesktop.Services;

public enum SourceMode { Offline, Demo, Smtc, Folia }

public sealed partial class PlayerService
{
    private readonly bool _demo;
    private readonly AppSettings _settings;
    private readonly LyricClient _lyricClient = new();
    private readonly StageClient _stage = new();
    private readonly SmtcReader _smtc = new();
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(6) };
    private readonly DispatcherTimer _timer;

    private double _pos;
    private DateTime _posAnchor = DateTime.UtcNow;
    private bool _playing;
    private DateTime _lastStagePoll = DateTime.MinValue;
    private DateTime _lastSmtcPoll = DateTime.MinValue;
    private DateTime _lastLyricFetch = DateTime.MinValue;
    private string _trackKey = "";
    private string _coverKey = "";
    private BitmapSource? _cover;
    private bool _busy;

    public PlayerService(AppSettings settings, bool demo = false)
    {
        _settings = settings;
        _demo = demo;
        _timer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(200),
        };
        _timer.Tick += async (_, _) => await TickAsync();
    }

    public SourceMode Mode { get; private set; } = SourceMode.Offline;
    public string? StatusDetail { get; private set; }
    public string Title { get; private set; } = "";
    public string Artist { get; private set; } = "";
    public string Album { get; private set; } = "";
    public double DurationMs { get; private set; }
    public double PositionMs => _pos;
    public bool IsPlaying => _playing;
    public BitmapSource? Cover => _cover;
    public LyricData? Lyric { get; private set; }
    public LyricFrame? Frame { get; private set; }
    public bool CanSeek { get; private set; }
    public bool HasStageToken => !string.IsNullOrWhiteSpace(_settings.StageToken);

    public event Action? Updated;
    public event Action? TrackChanged;

    public void Start()
    {
        _timer.Start();
        Log.Info("PlayerService 启动");
    }

    public void Stop() => _timer.Stop();

    public void RefreshNow() => _lastStagePoll = DateTime.MinValue;

    private async Task TickAsync()
    {
        if (_busy) return;
        _busy = true;
        try
        {
            if (_demo) UpdateDemo(); else await PollAsync();
            var t = DateTime.UtcNow;
            if (_playing && Mode != SourceMode.Offline)
                _pos = Math.Min(DurationMs > 0 ? DurationMs : double.MaxValue,
                                _pos + (t - _posAnchor).TotalMilliseconds);
            _posAnchor = t;

            Frame = Lyric != null ? LyricEngine.Build(Lyric, _pos, _settings.LyricOffsetMs) : null;
            Updated?.Invoke();
        }
        catch (Exception ex)
        {
            Log.Warn("tick 异常: " + ex.Message);
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task PollAsync()
    {
        var now = DateTime.UtcNow;
        StageSnapshot? stage = null;
        var stageOk = false;

        if (HasStageToken && (now - _lastStagePoll).TotalMilliseconds >= 900)
        {
            _lastStagePoll = now;
            var res = await _stage.GetStatusAsync(_settings.StagePort, _settings.StageToken);
            stageOk = res.ok;
            stage = res.snapshot;
            if (!stageOk && res.error != null) StatusDetail = res.error;
        }

        SmtcSnapshot? smtc = null;
        if ((!stageOk || stage?.Current == null) && (now - _lastSmtcPoll).TotalMilliseconds >= 1000)
        {
            _lastSmtcPoll = now;
            smtc = await _smtc.ReadAsync();
        }

        if (stageOk && stage?.Current != null)
        {
            Mode = SourceMode.Folia;
            ApplyTrack(stage.Current.Title, stage.Current.Artist, stage.Current.Album,
                       stage.DurationMs, stage.Current.CoverUrl, null);
            _playing = stage.PlayerState == "PLAYING";
            CanSeek = stage.CanSeek;
            StatusDetail = "Stage 已连接";

            if (now.Millisecond < 220 || Math.Abs(stage.PositionMs - _pos) > 900)
            {
                _pos = stage.PositionMs;
                _posAnchor = now;
            }
        }
        else if (smtc != null && !string.IsNullOrWhiteSpace(smtc.Title))
        {
            Mode = SourceMode.Smtc;
            ApplyTrack(smtc.Title, smtc.Artist, smtc.Album, smtc.DurationMs, null, smtc.Cover);
            if (Math.Abs(smtc.PositionMs - _pos) > 900)
            {
                _pos = smtc.PositionMs;
                _posAnchor = now;
            }
            _playing = smtc.IsPlaying;
            CanSeek = smtc.CanSeek;
            StatusDetail = "系统媒体会话";
        }
        else
        {
            Mode = SourceMode.Offline;
            StatusDetail = stageOk ? "Folia 空闲" : (_smtc.LastError ?? "未发现媒体会话");
            _playing = false;
            CanSeek = false;
            if (Lyric == null && Title.Length == 0)
            {
                Title = "音乐桌面";
                Artist = "等待 Folia 歌词接口 / 系统媒体会话";
            }
        }

        await MaybeFetchLyricAsync();
    }

    private void ApplyTrack(string title, string artist, string album, double duration, string? coverUrl, BitmapSource? cover)
    {
        var key = $"{title}|{artist}";
        var changed = key != _trackKey;
        Title = title;
        Artist = artist;
        Album = album;
        if (duration > 0) DurationMs = duration;

        if (cover != null && cover != _cover)
        {
            _cover = cover;
            _coverKey = "smtc";
        }
        else if (!string.IsNullOrWhiteSpace(coverUrl) && coverUrl != _coverKey)
        {
            _coverKey = coverUrl;
            _ = LoadCoverAsync(coverUrl);
        }

        if (changed)
        {
            _trackKey = key;
            Lyric = null;
            Frame = null;
            TrackChanged?.Invoke();
            _lastLyricFetch = DateTime.MinValue;
        }
    }

    private async Task LoadCoverAsync(string url)
    {
        try
        {
            var bytes = await _http.GetByteArrayAsync(url);
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.StreamSource = new MemoryStream(bytes);
            bmp.EndInit();
            bmp.Freeze();
            _cover = bmp;
        }
        catch (Exception ex)
        {
            Log.Warn("封面下载失败: " + ex.Message);
        }
    }

    private async Task MaybeFetchLyricAsync()
    {
        var now = DateTime.UtcNow;
        var interval = Lyric == null ? 2500 : 30000;
        if ((now - _lastLyricFetch).TotalMilliseconds < interval) return;

        _lastLyricFetch = now;
        var (status, data, error) = await _lyricClient.FetchAsync(_settings.LyricPort);
        switch (status)
        {
            case LyricFetchStatus.Ok when data != null && data.Lines.Count > 0:
                Lyric = data;
                if (!string.IsNullOrWhiteSpace(data.Title) && string.IsNullOrWhiteSpace(Title))
                {
                    Title = data.Title;
                    Artist = data.Artist ?? Artist;
                }
                if (Mode == SourceMode.Offline) StatusDetail = "仅歌词接口";
                break;
            case LyricFetchStatus.NoLyric:
                if (Lyric == null) StatusDetail = "Folia 已连接，当前无歌词";
                break;
            case LyricFetchStatus.Unreachable:
                if (Lyric == null) StatusDetail = error ?? "歌词接口未开启";
                break;
        }
    }

    // ---------- 控制 ----------

    public async Task ToggleAsync()
    {
        if (_demo) { _playing = !_playing; _posAnchor = DateTime.UtcNow; Updated?.Invoke(); return; }
        if (Mode == SourceMode.Folia && HasStageToken)
        {
            var action = _playing ? "pause" : "resume";
            if (await _stage.ControlAsync(_settings.StagePort, _settings.StageToken, action))
            {
                _playing = !_playing;
                _posAnchor = DateTime.UtcNow;
                Updated?.Invoke();
                return;
            }
        }
        MediaKeys.PlayPause();
    }

    public async Task NextAsync()
    {
        if (Mode == SourceMode.Folia && HasStageToken &&
            await _stage.ControlAsync(_settings.StagePort, _settings.StageToken, "next"))
            return;
        MediaKeys.Next();
    }

    public async Task PrevAsync()
    {
        if (Mode == SourceMode.Folia && HasStageToken &&
            await _stage.ControlAsync(_settings.StagePort, _settings.StageToken, "prev"))
            return;
        MediaKeys.Previous();
    }

    public async Task SeekAsync(double positionMs)
    {
        positionMs = Math.Clamp(positionMs, 0, Math.Max(0, DurationMs));
        _pos = positionMs;
        _posAnchor = DateTime.UtcNow;
        Updated?.Invoke();

        if (_demo)
        {
            _demoStart = DateTime.UtcNow.AddMilliseconds(-positionMs);
            return;
        }

        if (Mode == SourceMode.Folia && HasStageToken &&
            await _stage.ControlAsync(_settings.StagePort, _settings.StageToken, "seek", positionMs))
            return;

        await _smtc.SeekAsync(positionMs);
    }
}
