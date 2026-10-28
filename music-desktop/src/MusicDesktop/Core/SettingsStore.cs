using System.Collections.ObjectModel;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace MusicDesktop.Core;

public static class SettingsStore
{
    public static string RootDir { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MusicDesktop");

    public static string SettingsPath => Path.Combine(RootDir, "settings.json");

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNameCaseInsensitive = true,
    };

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                var s = JsonSerializer.Deserialize<AppSettings>(json, Options);
                if (s != null)
                {
                    if (s.DockItems.Count == 0) s.DockItems = Defaults();
                    return s;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warn("读取设置失败，使用默认值: " + ex.Message);
        }

        var fresh = new AppSettings();
        foreach (var item in Defaults()) fresh.DockItems.Add(item);
        return fresh;
    }

    public static void Save(AppSettings s)
    {
        try
        {
            Directory.CreateDirectory(RootDir);
            var tmp = SettingsPath + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(s, Options));
            File.Move(tmp, SettingsPath, overwrite: true);
        }
        catch (Exception ex)
        {
            Log.Warn("保存设置失败: " + ex.Message);
        }
    }

    private static ObservableCollection<DockItem> Defaults()
    {
        var list = new ObservableCollection<DockItem>();
        var win = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        var pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        void Add(string name, string path, string args = "")
        {
            if (File.Exists(path)) list.Add(new DockItem { Name = name, Path = path, Arguments = args });
        }

        Add("文件", Path.Combine(win, "explorer.exe"));
        Add("Edge", Path.Combine(pf, "Microsoft", "Edge", "Application", "msedge.exe"));
        Add("终端", Path.Combine(local, "Microsoft", "WindowsApps", "wt.exe"));
        Add("记事本", Path.Combine(win, "notepad.exe"));
        Add("计算器", Path.Combine(win, "System32", "calc.exe"));
        Add("任务管理器", Path.Combine(win, "System32", "Taskmgr.exe"));
        Add("Folia", Path.Combine(local, "Programs", "folia", "Folia.exe"));
        return list;
    }
}
