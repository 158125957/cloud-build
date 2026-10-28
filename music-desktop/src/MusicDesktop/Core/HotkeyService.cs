using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace MusicDesktop.Core;

public sealed class HotkeyService : IDisposable
{
    private const int WM_HOTKEY = 0x0312;
    [DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
    [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private const uint MOD_ALT = 0x1;
    private const uint MOD_CONTROL = 0x2;

    private readonly HwndSource _source;
    private readonly Dictionary<int, Action> _actions = new();

    public HotkeyService()
    {
        var p = new HwndSourceParameters("MusicDesktopHotkeys")
        {
            Width = 0,
            Height = 0,
            WindowStyle = 0,
        };
        _source = new HwndSource(p);
        _source.AddHook(Hook);
    }

    private const uint MOD_SHIFT = 0x4;

    /// <summary>按候选列表依次尝试注册，返回实际生效的组合（失败返回 null）。</summary>
    public string? Register(int id, (uint mods, uint vk, string name)[] candidates, Action action)
    {
        foreach (var (mods, vk, name) in candidates)
        {
            if (RegisterHotKey(_source.Handle, id, mods, vk))
            {
                _actions[id] = action;
                Log.Info($"快捷键已注册: {name}");
                return name;
            }
        }
        Log.Warn($"快捷键全部被占用: id={id}");
        return null;
    }

    /// <summary>默认 Ctrl+Alt+<vk> 的简化注册（保留原调用形式）。</summary>
    public void Register(int id, uint vk, Action action)
        => Register(id, new[] { (MOD_CONTROL | MOD_ALT, vk, $"Ctrl+Alt+{(char)vk}") }, action);

    public static readonly (uint mods, uint vk, string name)[] LyricCandidates =
    {
        (MOD_CONTROL | MOD_ALT, 0x4C, "Ctrl+Alt+L"),
        (MOD_CONTROL | MOD_SHIFT, 0x4C, "Ctrl+Shift+L"),
        (MOD_CONTROL | MOD_ALT, 0x4B, "Ctrl+Alt+K"),
        (MOD_CONTROL | MOD_SHIFT, 0x4B, "Ctrl+Shift+K"),
    };

    private IntPtr Hook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && _actions.TryGetValue(wParam.ToInt32(), out var action))
        {
            action();
            handled = true;
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        foreach (var id in _actions.Keys) UnregisterHotKey(_source.Handle, id);
        _source.Dispose();
    }
}
