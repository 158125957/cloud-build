using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace MusicDesktop.Core;

public sealed class DockItem
{
    public string Name { get; set; } = "";
    public string Path { get; set; } = "";
    public string Arguments { get; set; } = "";

    [JsonIgnore]
    public string DisplayName => string.IsNullOrWhiteSpace(Name)
        ? System.IO.Path.GetFileNameWithoutExtension(Path)
        : Name;
}

public sealed class AppSettings
{
    // ---- Folia 连接 ----
    public int LyricPort { get; set; } = 32109;
    public int StagePort { get; set; } = 32107;
    public string StageToken { get; set; } = "";

    // ---- 外观 ----
    public string Theme { get; set; } = "aurora";
    public double LyricFontSize { get; set; } = 44;
    public double LyricOpacity { get; set; } = 1.0;
    public bool ShowTranslation { get; set; } = true;
    public bool ShowRomanization { get; set; }
    public bool LockLyric { get; set; }
    public bool Topmost { get; set; } = true;
    public int LyricOffsetMs { get; set; }

    // ---- 窗口位置 ----
    public double LyricX { get; set; } = double.NaN;
    public double LyricY { get; set; } = double.NaN;
    public double MiniX { get; set; } = double.NaN;
    public double MiniY { get; set; } = double.NaN;
    public double DockX { get; set; } = double.NaN;
    public double DockY { get; set; } = double.NaN;

    // ---- 行为 ----
    public bool ShowDock { get; set; } = true;
    public bool ShowMiniPlayer { get; set; } = true;
    public bool AutoStart { get; set; }
    public bool HideWhenIdle { get; set; }

    public ObservableCollection<DockItem> DockItems { get; set; } = new();
}
