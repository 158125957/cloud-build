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

    public void Register(int id, uint vk, Action action)
    {
        if (RegisterHotKey(_source.Handle, id, MOD_CONTROL | MOD_ALT, vk))
            _actions[id] = action;
        else
            Log.Warn($"快捷键注册失败: id={id} vk=0x{vk:X}");
    }

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
