using MusicDesktop.Folia;

namespace MusicDesktop.Services;

public sealed partial class PlayerService
{
    private DateTime _demoStart = DateTime.MinValue;
    private const double DemoLengthMs = 34_000;

    private void UpdateDemo()
    {
        if (_demoStart == DateTime.MinValue) _demoStart = DateTime.UtcNow;
        Mode = SourceMode.Folia;
        Title = "夜色落在窗台（演示）";
        Artist = "音乐桌面 DEMO";
        Album = "本地演示数据";
        DurationMs = DemoLengthMs;
        _playing = true;
        CanSeek = true;
        StatusDetail = "演示模式（--demo）";
        if (Lyric == null) Lyric = DemoContent.Build();
        _cover ??= DemoContent.CreateCover();
        _coverKey = "demo";

        var elapsed = (DateTime.UtcNow - _demoStart).TotalMilliseconds;
        _pos = elapsed % DemoLengthMs;
        _posAnchor = DateTime.UtcNow;
    }

    public void RestartDemo() => _demoStart = DateTime.UtcNow;
}
