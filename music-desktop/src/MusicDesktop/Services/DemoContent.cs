using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MusicDesktop.Folia;

namespace MusicDesktop.Services;

/// <summary>--demo 用的本地演示数据：没有 Folia、没有 SMTC 也能看到完整歌词效果。</summary>
public static class DemoContent
{
    public static LyricData Build()
    {
        var data = new LyricData
        {
            Offset = 0,
            ClipStartMs = 0,
            WordByWord = true,
            Title = "夜色落在窗台（演示）",
            Artist = "音乐桌面 DEMO",
        };

        void Line(string text, double start, double end, string? tr = null, string? ro = null, string[]? words = null)
        {
            var line = new LyricLine { Text = text, StartTime = start, EndTime = end, Translation = tr, Romanization = ro };
            if (words != null)
            {
                var t = start;
                var step = (end - start) / words.Length;
                foreach (var w in words)
                {
                    line.Words.Add(new LyricWord { Text = w, StartTime = t, EndTime = t + step });
                    t += step;
                }
            }
            data.Lines.Add(line);
        }

        Line("夜色落在窗台", 2.0, 6.0, "Night falls on the windowsill", "yè sè luò zài chuāng tái",
             new[] { "夜色", "落在", "窗台" });
        Line("风把城市吹得很轻", 6.4, 11.0, "The wind blows the city softly", "fēng bǎ chéng shì chuī de hěn qīng",
             new[] { "风", "把城市", "吹得", "很轻" });
        Line("耳机里那首歌", 11.4, 15.2, "That song inside my headphones", "ěr jī lǐ nà shǒu gē",
             new[] { "耳机里", "那首", "歌" });
        Line("刚好唱到我们", 15.6, 19.6, "Happens to sing about us", "gāng hǎo chàng dào wǒ men",
             new[] { "刚好", "唱到", "我们" });
        Line("时间慢慢亮起来", 20.4, 25.0, "Time slowly lights up", "shí jiān màn màn liàng qǐ lái",
             new[] { "时间", "慢慢", "亮起来" });
        Line("像你回头的那一眼", 25.4, 30.2, "Like the glance you gave back", "xiàng nǐ huí tóu de nà yī yǎn",
             new[] { "像你", "回头", "的那", "一眼" });
        return data;
    }

    /// <summary>演示用封面：渐变底 + 音符，避免播放器左边空一块。</summary>
    public static BitmapSource CreateCover(int size = 512)
    {
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            var rect = new Rect(0, 0, size, size);
            var bg = new LinearGradientBrush(
                (Color)ColorConverter.ConvertFromString("#FF1B2340"),
                (Color)ColorConverter.ConvertFromString("#FF3B2A5A"), 45);
            dc.DrawRectangle(bg, null, rect);

            var glow = new RadialGradientBrush(
                Color.FromArgb(0x88, 0x9C, 0x8C, 0xFF), Color.FromArgb(0x00, 0x9C, 0x8C, 0xFF));
            dc.DrawEllipse(glow, null, new Point(size * 0.5, size * 0.44), size * 0.42, size * 0.42);

            var accent = new LinearGradientBrush(
                (Color)ColorConverter.ConvertFromString("#FF7CE7FF"),
                (Color)ColorConverter.ConvertFromString("#FFFF9AD5"), 30);
            var note = new FormattedText("\u266B", System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Segoe UI Symbol"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
                size * 0.52, accent, 96);
            dc.DrawText(note, new Point((size - note.Width) / 2, (size - note.Height) / 2 - size * 0.02));
        }

        var bmp = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
        bmp.Render(visual);
        bmp.Freeze();
        return bmp;
    }
}
