using System.Drawing;
using System.Windows;
using System.Windows.Threading;
using MusicDesktop.Core;
using MusicDesktop.Services;
using MusicDesktop.UI;
using WinForms = System.Windows.Forms;

namespace MusicDesktop;

public partial class App : Application
{
    private SingleInstance? _guard;
    private AppSettings _settings = new();
    private PlayerService? _player;
    private LyricWindow? _lyric;
    private MiniPlayerWindow? _mini;
    private DockWindow? _dock;
    private SettingsWindow? _settingsWindow;
    private WinForms.NotifyIcon? _tray;
    private HotkeyService? _hotkeys;
    private readonly Icon _trayIcon = IconFactory.CreateAppIcon();
    private readonly System.Windows.Media.ImageSource _windowIcon = IconFactory.CreateImageSource();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _guard = new SingleInstance();
        if (!_guard.IsFirst)
        {
            Shutdown();
            return;
        }

        DispatcherUnhandledException += OnUnhandled;
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            Log.Error("未处理异常: " + args.ExceptionObject);

        _settings = SettingsStore.Load();
        ThemeManager.Apply(_settings.Theme);

        _player = new PlayerService(_settings, e.Args.Any(a => a.Equals("--demo", StringComparison.OrdinalIgnoreCase)));
        _player.Start();

        _lyric = new LyricWindow(_player, _settings) { Icon = _windowIcon };
        _mini = new MiniPlayerWindow(_player, _settings) { Icon = _windowIcon };
        _dock = new DockWindow(_settings) { Icon = _windowIcon };

        _mini.SettingsRequested += ShowSettings;
        _dock.SettingsRequested += ShowSettings;

        _lyric.Show();
        if (_settings.ShowMiniPlayer) _mini.Show();
        if (_settings.ShowDock) _dock.Show();

        BuildTray();
        RegisterHotkeys();

        Log.Info("音乐桌面启动完成");
    }

    private void OnUnhandled(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Error("UI 异常: " + e.Exception);
        e.Handled = true;
    }

    private void RegisterHotkeys()
    {
        _hotkeys = new HotkeyService();
        _hotkeys.Register(1, 0x4C, ToggleLyric);      // Ctrl+Alt+L
        _hotkeys.Register(2, 0x4B, ToggleLock);       // Ctrl+Alt+K
        _hotkeys.Register(3, 0x4D, ToggleMini);       // Ctrl+Alt+M
        _hotkeys.Register(4, 0x44, ToggleDock);       // Ctrl+Alt+D
        _hotkeys.Register(5, 0x53, ShowSettings);     // Ctrl+Alt+S
    }

    private void ToggleLyric()
    {
        if (_lyric == null) return;
        if (_lyric.IsVisible) _lyric.Hide(); else _lyric.Show();
    }

    private void ToggleLock()
    {
        _settings.LockLyric = !_settings.LockLyric;
        _lyric?.SetLocked(_settings.LockLyric);
        SettingsStore.Save(_settings);
    }

    private void ToggleMini()
    {
        if (_mini == null) return;
        if (_mini.IsVisible) _mini.Hide(); else _mini.Show();
    }

    private void ToggleDock()
    {
        if (_dock == null) return;
        if (_dock.IsVisible) _dock.Hide(); else { _dock.Show(); _dock.Rebuild(); }
    }

    private void ShowSettings()
    {
        if (_settingsWindow == null)
        {
            _settingsWindow = new SettingsWindow(_settings) { Icon = _windowIcon };
            _settingsWindow.Saved += OnSettingsSaved;
            _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        }
        _settingsWindow.Show();
        _settingsWindow.Activate();
    }

    private void OnSettingsSaved(AppSettings s)
    {
        _settings = s;
        ThemeManager.Apply(s.Theme);
        _lyric?.ApplySettings(s);
        _mini?.ApplySettings(s);
        _dock?.ApplySettings(s);
        if (s.ShowDock) { _dock?.Show(); _dock?.Rebuild(); } else _dock?.Hide();
        if (s.ShowMiniPlayer) _mini?.Show(); else _mini?.Hide();
        _player?.RefreshNow();
        UpdateTrayText();
    }

    private void BuildTray()
    {
        _tray = new WinForms.NotifyIcon
        {
            Icon = _trayIcon,
            Text = "音乐桌面",
            Visible = true,
        };

        var menu = new WinForms.ContextMenuStrip();
        menu.Items.Add("显示 / 隐藏歌词", null, (_, _) => ToggleLyric());
        menu.Items.Add("锁定歌词（鼠标穿透）", null, (_, _) => ToggleLock());
        menu.Items.Add("迷你播放器", null, (_, _) => ToggleMini());
        menu.Items.Add("Dock", null, (_, _) => ToggleDock());
        menu.Items.Add(new WinForms.ToolStripSeparator());
        menu.Items.Add("设置…", null, (_, _) => Dispatcher.Invoke(ShowSettings));
        menu.Items.Add(new WinForms.ToolStripSeparator());
        menu.Items.Add("退出", null, (_, _) => Dispatcher.Invoke(ExitApp));
        _tray.ContextMenuStrip = menu;
        _tray.DoubleClick += (_, _) => Dispatcher.Invoke(ToggleLyric);
    }

    private void UpdateTrayText()
    {
        if (_tray == null || _player == null) return;
        var text = string.IsNullOrWhiteSpace(_player.Title) ? "音乐桌面" : $"{_player.Title} - {_player.Artist}";
        _tray.Text = text.Length > 60 ? text[..60] : text;
    }

    private void ExitApp()
    {
        _settings.AutoStart = _settings.AutoStart;
        SettingsStore.Save(_settings);
        _player?.Stop();
        _hotkeys?.Dispose();
        if (_tray != null) { _tray.Visible = false; _tray.Dispose(); }
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        SettingsStore.Save(_settings);
        _guard?.Dispose();
        base.OnExit(e);
    }
}
