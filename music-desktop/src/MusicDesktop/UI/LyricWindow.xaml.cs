using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using MusicDesktop.Core;
using MusicDesktop.Services;

namespace MusicDesktop.UI;

public partial class LyricWindow : Window
{
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x20;
    private const int WS_EX_TOOLWINDOW = 0x80;
    private const int WS_EX_NOACTIVATE = 0x08000000;

    [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr hwnd, int index);
    [DllImport("user32.dll")] private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

    private readonly PlayerService _player;
    private AppSettings _settings;
    private string _lastLine = "";
    private bool _locked;

    public LyricWindow(PlayerService player, AppSettings settings)
    {
        InitializeComponent();
        _player = player;
        _settings = settings;
        _player.Updated += OnPlayerUpdated;

        SourceInitialized += (_, _) =>
        {
            // 关键：分层窗口的合成背景默认是黑色，必须显式设为全透明，否则会盖出一块黑板
            if (PresentationSource.FromVisual(this) is System.Windows.Interop.HwndSource src &&
                src.CompositionTarget != null)
            {
                src.CompositionTarget.BackgroundColor = Colors.Transparent;
            }

            var ex = GetWindowLong(new System.Windows.Interop.WindowInteropHelper(this).Handle, GWL_EXSTYLE);
            SetWindowLong(new System.Windows.Interop.WindowInteropHelper(this).Handle, GWL_EXSTYLE,
                          ex | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);
        };

        Loaded += (_, _) =>
        {
            ApplySettings(settings);
            RestorePosition();
        };

        Closing += (_, e) => { e.Cancel = true; Hide(); };
    }

    public void ApplySettings(AppSettings s)
    {
        _settings = s;
        BaseText.FontSize = s.LyricFontSize;
        FillText.FontSize = s.LyricFontSize;
        TranslationText.FontSize = Math.Max(12, s.LyricFontSize * 0.40);
        NextText.FontSize = Math.Max(12, s.LyricFontSize * 0.42);
        Opacity = Math.Clamp(s.LyricOpacity, 0.2, 1.0);
        Topmost = s.Topmost;
        TranslationText.Visibility = s.ShowTranslation ? Visibility.Visible : Visibility.Collapsed;
        FillGlow.Color = ThemeManager.Glow;
        SetLocked(s.LockLyric);
    }

    public void SetLocked(bool locked)
    {
        _locked = locked;
        BtnLock.Opacity = locked ? 1 : 0.5;
        var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero) return;
        var ex = GetWindowLong(hwnd, GWL_EXSTYLE);
        ex = locked ? (ex | WS_EX_TRANSPARENT) : (ex & ~WS_EX_TRANSPARENT);
        SetWindowLong(hwnd, GWL_EXSTYLE, ex);
    }

    private void OnPlayerUpdated()
    {
        var frame = _player.Frame;
        if (frame == null || string.IsNullOrEmpty(frame.Text))
        {
            if (string.IsNullOrEmpty(_lastLine))
            {
                ShowPlaceholder();
                return;
            }
            // 间奏：保留上一行做淡出，不闪空
            FillClip.Rect = new Rect(0, 0, FillText.ActualWidth, FillText.ActualHeight);
            return;
        }

        if (frame.Text != _lastLine)
        {
            _lastLine = frame.Text;
            BaseText.Text = frame.Text;
            FillText.Text = frame.Text;
            AnimateLineIn();
        }

        TranslationText.Text = _settings.ShowTranslation ? (frame.Translation ?? "") : "";
        NextText.Text = frame.NextText;

        var w = FillText.ActualWidth;
        var h = Math.Max(1, FillText.ActualHeight);
        FillClip.Rect = new Rect(0, 0, w * Math.Clamp(frame.Progress, 0, 1), h);
    }

    private void ShowPlaceholder()
    {
        var mode = _player.Mode;
        BaseText.Text = mode == SourceMode.Offline ? "音乐桌面" : "♪";
        FillText.Text = BaseText.Text;
        FillClip.Rect = new Rect(0, 0, 0, 1);
        TranslationText.Text = "";
        NextText.Text = _player.Mode == SourceMode.Offline
            ? (_player.StatusDetail ?? "等待 Folia 歌词接口")
            : "";
    }

    private void AnimateLineIn()
    {
        BaseText.BeginAnimation(OpacityProperty, null);
        FillText.BeginAnimation(OpacityProperty, null);
        var fade = new DoubleAnimation(0.25, 1, TimeSpan.FromMilliseconds(220))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
        };
        BaseText.BeginAnimation(OpacityProperty, fade);
        FillText.BeginAnimation(OpacityProperty, fade);
    }

    private void RestorePosition()
    {
        var wa = SystemParameters.WorkArea;
        if (!double.IsNaN(_settings.LyricX) && !double.IsNaN(_settings.LyricY))
        {
            Left = _settings.LyricX;
            Top = _settings.LyricY;
        }
        else
        {
            Left = wa.Left + (wa.Width - Width) / 2;
            Top = wa.Bottom - Height - 120;
        }
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_locked) return;
        try { DragMove(); } catch { }
        _settings.LyricX = Left;
        _settings.LyricY = Top;
        SettingsStore.Save(_settings);
    }

    private void Window_MouseEnter(object sender, MouseEventArgs e)
    {
        if (_locked) return;
        Toolbar.BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(150)));
    }

    private void Window_MouseLeave(object sender, MouseEventArgs e)
        => Toolbar.BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(200)));

    private void BtnLock_Click(object sender, RoutedEventArgs e)
    {
        _settings.LockLyric = !_settings.LockLyric;
        SetLocked(_settings.LockLyric);
        SettingsStore.Save(_settings);
    }

    private void BtnFontMinus_Click(object sender, RoutedEventArgs e) => NudgeFont(-2);
    private void BtnFontPlus_Click(object sender, RoutedEventArgs e) => NudgeFont(2);

    private void NudgeFont(double delta)
    {
        _settings.LyricFontSize = Math.Clamp(_settings.LyricFontSize + delta, 20, 96);
        ApplySettings(_settings);
        SettingsStore.Save(_settings);
    }

    private void BtnEarlier_Click(object sender, RoutedEventArgs e) => NudgeOffset(-200);
    private void BtnLater_Click(object sender, RoutedEventArgs e) => NudgeOffset(200);

    private void NudgeOffset(int delta)
    {
        _settings.LyricOffsetMs = Math.Clamp(_settings.LyricOffsetMs + delta, -5000, 5000);
        SettingsStore.Save(_settings);
        NextText.Text = $"偏移 {_settings.LyricOffsetMs:+#;-#;0} ms";
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e) => Hide();
}
