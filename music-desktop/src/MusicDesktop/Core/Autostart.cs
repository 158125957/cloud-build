using System.Diagnostics;
using Microsoft.Win32;

namespace MusicDesktop.Core;

public static class Autostart
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "MusicDesktop";

    public static void Apply(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
            if (key == null) return;
            if (enabled)
            {
                var exe = Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(exe)) key.SetValue(AppName, $"\"{exe}\"");
            }
            else if (key.GetValue(AppName) != null)
            {
                key.DeleteValue(AppName, throwOnMissingValue: false);
            }
        }
        catch (Exception ex)
        {
            Log.Warn("设置开机启动失败: " + ex.Message);
        }
    }
}
