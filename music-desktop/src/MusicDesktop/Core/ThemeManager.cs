using System.Windows;
using System.Windows.Media;

namespace MusicDesktop.Core;

public static class ThemeManager
{
    public static readonly (string Key, string Name, string C1, string C2, string C3)[] Themes =
    {
        ("aurora",   "极光",   "#7CE7FF", "#9C8CFF", "#FF9AD5"),
        ("sunset",   "落日",   "#FFC46B", "#FF7A7A", "#FF5FA2"),
        ("ocean",    "深海",   "#5EE7FF", "#4A8CFF", "#7B5CFF"),
        ("sakura",   "樱雾",   "#FFD1E8", "#FF9AC1", "#C9A7FF"),
        ("neon",     "霓虹",   "#7CFF6B", "#00E5FF", "#FF5CE1"),
        ("obsidian", "墨玉",   "#F2F4F8", "#B9C2D4", "#7C8AA3"),
    };

    public static string Current { get; private set; } = "aurora";

    public static void Apply(string key)
    {
        var theme = Themes.FirstOrDefault(t => t.Key == key);
        if (theme.Key == null) theme = Themes[0];
        Current = theme.Key;

        var brush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 1),
        };
        brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString(theme.C1), 0));
        brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString(theme.C2), 0.55));
        brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString(theme.C3), 1));
        brush.Freeze();

        var res = Application.Current.Resources;
        res["AccentBrush"] = brush;
        res["AccentSolid"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(theme.C2));
        res["GlowColor"] = (Color)ColorConverter.ConvertFromString(theme.C2);
    }

    public static Color Glow => Application.Current.Resources["GlowColor"] is Color c ? c : Colors.MediumPurple;
}
