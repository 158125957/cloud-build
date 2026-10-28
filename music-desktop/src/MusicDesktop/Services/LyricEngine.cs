using MusicDesktop.Folia;

namespace MusicDesktop.Services;

public sealed class LyricFrame
{
    public int LineIndex { get; init; } = -1;
    public string Text { get; init; } = "";
    public string NextText { get; init; } = "";
    public string? Translation { get; init; }
    public string? Romanization { get; init; }
    public double Progress { get; init; }
    public bool InInterlude => LineIndex < 0;
}

public static class LyricEngine
{
    /// <summary>
    /// 把「播放器进度」换算成歌词时间：
    /// 歌词按整首歌计时，播放器可能只播片段，所以要加 clipStartMs；
    /// offset 为正表示歌词延后，因此要减去。
    /// </summary>
    public static double ToLyricSeconds(LyricData data, double playerPositionMs, int userOffsetMs)
        => Math.Max(0, playerPositionMs + data.ClipStartMs - data.Offset - userOffsetMs) / 1000.0;

    public static LyricFrame Build(LyricData data, double playerPositionMs, int userOffsetMs)
    {
        var lines = data.Lines;
        if (lines.Count == 0)
            return new LyricFrame { Text = "♪", NextText = "" };

        var t = ToLyricSeconds(data, playerPositionMs, userOffsetMs);
        var idx = -1;
        for (var i = 0; i < lines.Count; i++)
        {
            if (lines[i].StartTime <= t) idx = i;
            else break;
        }

        if (idx < 0)
        {
            return new LyricFrame
            {
                LineIndex = -1,
                Text = "",
                NextText = lines[0].Text,
                Progress = 0,
            };
        }

        var line = lines[idx];
        var next = idx + 1 < lines.Count ? lines[idx + 1].Text : "";
        return new LyricFrame
        {
            LineIndex = idx,
            Text = line.Text,
            NextText = next,
            Translation = line.Translation,
            Romanization = line.Romanization,
            Progress = LineProgress(line, t),
        };
    }

    /// <summary>按逐字时间算出行内已唱比例（0..1），用于卡拉OK 扫光。</summary>
    public static double LineProgress(LyricLine line, double t)
    {
        var span = line.EndTime - line.StartTime;
        if (span <= 0) return t >= line.EndTime ? 1 : 0;

        if (line.Words.Count == 0)
            return Clamp01((t - line.StartTime) / span);

        var total = Math.Max(1, line.Text.Length);
        double sung = 0;
        foreach (var w in line.Words)
        {
            var len = Math.Max(1, w.Text.Length);
            if (t >= w.EndTime) { sung += len; continue; }
            if (t <= w.StartTime) break;
            var frac = (t - w.StartTime) / Math.Max(0.001, w.EndTime - w.StartTime);
            sung += len * Clamp01(frac);
            break;
        }
        return Clamp01(sung / total);
    }

    private static double Clamp01(double v) => v < 0 ? 0 : v > 1 ? 1 : v;
}
