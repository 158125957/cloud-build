using System.Windows;
using System.Windows.Input;
using MusicDesktop.Core;
using MusicDesktop.Services;

namespace MusicDesktop.UI;

public partial class MiniPlayerWindow : Window
{
    private readonly PlayerService _player;
    private AppSettings _settings;

    public MiniPlayerWindow(PlayerService player, AppSettings settings)
    {
        InitializeComponent();
        _player = player;
        _settings = settings;
        _player.Updated += OnUpdated;
        Loaded += (_, _) => RestorePosition();
        Closing += (_, e) => { e.Cancel = true; Hide(); };
    }

    public event Action? SettingsRequested;

    public void ApplySettings(AppSettings s) => _settings = s;

    private void RestorePosition()
    {
        var wa = SystemParameters.WorkArea;
        if (!double.IsNaN(_settings.MiniX) && !double.IsNaN(_settings.MiniY))
        {
            Left = _settings.MiniX;
            Top = _settings.MiniY;
        }
        else
        {
            Left = wa.Right - Width - 36;
            Top = wa.Bottom - Height - 150;
        }
    }

    private void OnUpdated()
    {
        TitleText.Text = string.IsNullOrWhiteSpace(_player.Title) ? "音乐桌面" : _player.Title;
        ArtistText.Text = string.IsNullOrWhiteSpace(_player.Artist) ? "—" : _player.Artist;
        SourceText.Text = _player.Mode switch
        {
            SourceMode.Folia => "FOLIA",
            SourceMode.Smtc => "SMTC",
            _ => "OFFLINE",
        };
        StatusText.Text = _player.StatusDetail ?? "";
        BtnPlay.Content = _player.IsPlaying ? "\uE769" : "\uE768";

        var dur = Math.Max(0, _player.DurationMs);
        var pos = Math.Max(0, Math.Min(_player.PositionMs, dur > 0 ? dur : _player.PositionMs));
        PosText.Text = Fmt(pos);
        DurText.Text = Fmt(dur);

        var frac = dur > 0 ? pos / dur : 0;
        ProgressFill.Width = Math.Max(0, ProgressHost.ActualWidth * frac);

        var cover = _player.Cover;
        CoverImage.Source = cover;
        CoverImage.Visibility = cover != null ? Visibility.Visible : Visibility.Collapsed;
    }

    private static string Fmt(double ms)
    {
        var t = TimeSpan.FromMilliseconds(Math.Max(0, ms));
        return t.TotalHours >= 1 ? $"{(int)t.TotalHours}:{t.Minutes:00}:{t.Seconds:00}" : $"{t.Minutes}:{t.Seconds:00}";
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is System.Windows.Controls.Button) return;
        try { DragMove(); } catch { }
        _settings.MiniX = Left;
        _settings.MiniY = Top;
        SettingsStore.Save(_settings);
    }

    private void Progress_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (ProgressHost.ActualWidth <= 1 || _player.DurationMs <= 0) return;
        var p = e.GetPosition(ProgressHost).X / ProgressHost.ActualWidth;
        _ = _player.SeekAsync(_player.DurationMs * Math.Clamp(p, 0, 1));
    }

    private async void BtnPlay_Click(object sender, RoutedEventArgs e) => await _player.ToggleAsync();
    private async void BtnNext_Click(object sender, RoutedEventArgs e) => await _player.NextAsync();
    private async void BtnPrev_Click(object sender, RoutedEventArgs e) => await _player.PrevAsync();
    private void BtnSettings_Click(object sender, RoutedEventArgs e) => SettingsRequested?.Invoke();
    private void BtnHide_Click(object sender, RoutedEventArgs e) => Hide();
}
