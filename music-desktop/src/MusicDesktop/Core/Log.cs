using System.IO;

namespace MusicDesktop.Core;

public static class Log
{
    private static readonly object Gate = new();
    private static readonly string Dir = Path.Combine(SettingsStore.RootDir, "logs");

    public static void Info(string msg) => Write("INFO", msg);
    public static void Warn(string msg) => Write("WARN", msg);
    public static void Error(string msg) => Write("ERROR", msg);

    private static void Write(string level, string msg)
    {
        try
        {
            lock (Gate)
            {
                Directory.CreateDirectory(Dir);
                var file = Path.Combine(Dir, $"app-{DateTime.Now:yyyyMMdd}.log");
                File.AppendAllText(file, $"{DateTime.Now:HH:mm:ss.fff} [{level}] {msg}{Environment.NewLine}");
            }
        }
        catch { /* 日志永不拖垮 UI */ }
    }
}
