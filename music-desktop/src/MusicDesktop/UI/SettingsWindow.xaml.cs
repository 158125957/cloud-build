using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using MusicDesktop.Core;
using MusicDesktop.Folia;

namespace MusicDesktop.UI;

public partial class SettingsWindow : Window
{
    private readonly AppSettings _settings;
    private bool _loading;

    public event Action<AppSettings>? Saved;

    public SettingsWindow(AppSettings settings)
    {
        InitializeComponent();
        _settings = settings;
        _loading = true;
        Populate();
        _loading = false;
        TxtConfigPath.Text = "配置：" + SettingsStore.SettingsPath;
    }

    private void Populate()
    {
        TxtLyricPort.Text = _settings.LyricPort.ToString();
        TxtStagePort.Text = _settings.StagePort.ToString();
        TxtToken.Text = _settings.StageToken;
        TxtOffset.Text = _settings.LyricOffsetMs.ToString();

        CmbTheme.Items.Clear();
        foreach (var (key, name, _, _, _) in ThemeManager.Themes)
            CmbTheme.Items.Add(new ComboBoxItem { Content = name, Tag = key, IsSelected = key == _settings.Theme });

        SldFont.Value = _settings.LyricFontSize;
        TxtFont.Text = ((int)_settings.LyricFontSize).ToString();
        SldOpacity.Value = _settings.LyricOpacity;
        SldOffset.Value = _settings.LyricOffsetMs;

        ChkTranslation.IsChecked = _settings.ShowTranslation;
        ChkRomanization.IsChecked = _settings.ShowRomanization;
        ChkTopmost.IsChecked = _settings.Topmost;
        ChkLock.IsChecked = _settings.LockLyric;
        ChkDock.IsChecked = _settings.ShowDock;
        ChkMini.IsChecked = _settings.ShowMiniPlayer;
        ChkAutoStart.IsChecked = _settings.AutoStart;
        ChkHideIdle.IsChecked = _settings.HideWhenIdle;

        RefreshDockList();
    }

    private void RefreshDockList()
    {
        LstDock.Items.Clear();
        foreach (var item in _settings.DockItems)
            LstDock.Items.Add($"{item.DisplayName}    {item.Path}");
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is TextBox || e.OriginalSource is Slider || e.OriginalSource is Button) return;
        try { DragMove(); } catch { }
    }

    private void CmbTheme_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading) return;
        if (CmbTheme.SelectedItem is ComboBoxItem { Tag: string key })
            ThemeManager.Apply(key);
    }

    private void SldFont_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (TxtFont != null) TxtFont.Text = ((int)e.NewValue).ToString();
    }

    private void SldOpacity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) { }
    private void SldOffset_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_loading && TxtOffset != null) TxtOffset.Text = ((int)e.NewValue).ToString();
    }

    private async void BtnTest_Click(object sender, RoutedEventArgs e)
    {
        TxtTest.Text = "测试中…";
        var lyric = new LyricClient();
        var (status, data, error) = await lyric.FetchAsync(ParseInt(TxtLyricPort.Text, 32109));
        var stage = new StageClient();
        var ping = await stage.PingAsync(ParseInt(TxtStagePort.Text, 32107));
        var (ok, snap, serr) = await stage.GetStatusAsync(ParseInt(TxtStagePort.Text, 32107), TxtToken.Text);

        TxtTest.Text = status switch
        {
            LyricFetchStatus.Ok => $"歌词 OK（{data?.Lines.Count ?? 0} 行）",
            LyricFetchStatus.NoLyric => "歌词接口在线，当前无歌词",
            _ => "歌词接口不可达（" + (error ?? "未开启") + "）",
        };
        TxtTest.Text += " · Stage " + (ping ? "在线" : "离线") +
                        (ok ? $"（{snap?.PlayerState}）" : !string.IsNullOrEmpty(TxtToken.Text) ? $"（{serr}）" : "");
    }

    private void BtnAddDock_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "选择要添加到 Dock 的程序",
            Filter = "程序 (*.exe;*.lnk)|*.exe;*.lnk|所有文件 (*.*)|*.*",
        };
        if (dlg.ShowDialog() == true)
        {
            _settings.DockItems.Add(new DockItem
            {
                Name = System.IO.Path.GetFileNameWithoutExtension(dlg.FileName),
                Path = dlg.FileName,
            });
            RefreshDockList();
        }
    }

    private void BtnRemoveDock_Click(object sender, RoutedEventArgs e)
    {
        var idx = LstDock.SelectedIndex;
        if (idx >= 0 && idx < _settings.DockItems.Count)
        {
            _settings.DockItems.RemoveAt(idx);
            RefreshDockList();
        }
    }

    private void BtnOpenConfig_Click(object sender, RoutedEventArgs e)
    {
        Directory.CreateDirectory(SettingsStore.RootDir);
        Process.Start(new ProcessStartInfo { FileName = SettingsStore.RootDir, UseShellExecute = true });
    }

    private void BtnRestartLyric_Click(object sender, RoutedEventArgs e)
    {
        var lyric = Application.Current.Windows.OfType<LyricWindow>().FirstOrDefault();
        lyric?.Hide();
        lyric?.Show();
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        _settings.LyricPort = ParseInt(TxtLyricPort.Text, 32109);
        _settings.StagePort = ParseInt(TxtStagePort.Text, 32107);
        _settings.StageToken = TxtToken.Text.Trim();
        _settings.LyricOffsetMs = ParseInt(TxtOffset.Text, (int)SldOffset.Value);
        _settings.LyricFontSize = SldFont.Value;
        _settings.LyricOpacity = SldOpacity.Value;
        _settings.ShowTranslation = ChkTranslation.IsChecked == true;
        _settings.ShowRomanization = ChkRomanization.IsChecked == true;
        _settings.Topmost = ChkTopmost.IsChecked == true;
        _settings.LockLyric = ChkLock.IsChecked == true;
        _settings.ShowDock = ChkDock.IsChecked == true;
        _settings.ShowMiniPlayer = ChkMini.IsChecked == true;
        _settings.AutoStart = ChkAutoStart.IsChecked == true;
        _settings.HideWhenIdle = ChkHideIdle.IsChecked == true;
        if (CmbTheme.SelectedItem is ComboBoxItem { Tag: string key }) _settings.Theme = key;

        SettingsStore.Save(_settings);
        Autostart.Apply(_settings.AutoStart);
        Saved?.Invoke(_settings);
        Hide();
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e) => Hide();

    private static int ParseInt(string s, int fallback) => int.TryParse(s, out var v) ? v : fallback;
}
