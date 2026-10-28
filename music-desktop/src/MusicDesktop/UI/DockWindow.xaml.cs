using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using MusicDesktop.Core;

namespace MusicDesktop.UI;

public partial class DockWindow : Window
{
    private AppSettings _settings;

    public DockWindow(AppSettings settings)
    {
        InitializeComponent();
        _settings = settings;
        Loaded += (_, _) => { RestorePosition(); Rebuild(); };
        Closing += (_, e) => { e.Cancel = true; Hide(); };
    }

    public event Action? SettingsRequested;

    public void ApplySettings(AppSettings s)
    {
        _settings = s;
        Rebuild();
    }

    private void RestorePosition()
    {
        var wa = SystemParameters.WorkArea;
        if (!double.IsNaN(_settings.DockX) && !double.IsNaN(_settings.DockY))
        {
            Left = _settings.DockX;
            Top = _settings.DockY;
        }
        else
        {
            Left = wa.Left + (wa.Width - ActualWidth) / 2;
            Top = wa.Bottom - ActualHeight - 8;
        }
    }

    public void Rebuild()
    {
        ItemsHost.Children.Clear();
        foreach (var item in _settings.DockItems)
            ItemsHost.Children.Add(CreateIcon(item, false));

        ItemsHost.Children.Add(CreateAddButton());
        Dispatcher.BeginInvoke(new Action(() =>
        {
            if (double.IsNaN(_settings.DockX))
            {
                var wa = SystemParameters.WorkArea;
                Left = wa.Left + (wa.Width - ActualWidth) / 2;
                Top = wa.Bottom - ActualHeight - 8;
            }
        }));
    }

    private FrameworkElement CreateIcon(DockItem item, bool small)
    {
        var size = small ? 40 : 50;
        var root = new Grid { Width = size + 14, Height = size + 18, Background = Brushes.Transparent };
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var btn = new Button
        {
            Width = size,
            Height = size,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand,
            ToolTip = item.DisplayName,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            RenderTransformOrigin = new Point(0.5, 0.5),
            RenderTransform = new ScaleTransform(1, 1),
        };

        var icon = IconLoader.Load(item.Path, 48);
        btn.Content = icon != null
            ? new Image { Source = icon, Width = size - 10, Height = size - 10, SnapsToDevicePixels = true }
            : (object)new TextBlock
            {
                Text = item.DisplayName.Length > 0 ? item.DisplayName[..1] : "?",
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };

        btn.MouseEnter += (_, _) => Animate(btn, 1.12, 120);
        btn.MouseLeave += (_, _) => Animate(btn, 1.0, 140);
        btn.Click += (_, _) => Launch(item);
        btn.MouseRightButtonUp += (_, e) => { ShowItemMenu(item, e); e.Handled = true; };
        Grid.SetRow(btn, 0);
        root.Children.Add(btn);

        var running = Process.GetProcessesByName(
            System.IO.Path.GetFileNameWithoutExtension(item.Path)).Length > 0;
        var dot = new Ellipse
        {
            Width = 4,
            Height = 4,
            Fill = running ? TryBrush("AccentSolid") : Brushes.Transparent,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 2, 0, 0),
        };
        Grid.SetRow(dot, 1);
        root.Children.Add(dot);
        return root;
    }

    private static Brush TryBrush(string key) =>
        Application.Current.TryFindResource(key) as Brush ?? Brushes.White;

    private static void Animate(UIElement el, double scale, int ms)
    {
        if (el.RenderTransform is ScaleTransform st)
        {
            st.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(scale, TimeSpan.FromMilliseconds(ms))
            { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
            st.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(scale, TimeSpan.FromMilliseconds(ms))
            { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
        }
    }

    private void Launch(DockItem item)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = item.Path,
                Arguments = item.Arguments ?? "",
                UseShellExecute = true,
                WorkingDirectory = System.IO.Path.GetDirectoryName(item.Path) ?? "",
            });
        }
        catch (Exception ex)
        {
            Log.Warn($"启动 {item.DisplayName} 失败: {ex.Message}");
        }
    }

    private void ShowItemMenu(DockItem item, MouseButtonEventArgs e)
    {
        var menu = new ContextMenu();
        var open = new MenuItem { Header = "打开" };
        open.Click += (_, _) => Launch(item);
        var remove = new MenuItem { Header = "从 Dock 移除" };
        remove.Click += (_, _) =>
        {
            _settings.DockItems.Remove(item);
            SettingsStore.Save(_settings);
            Rebuild();
        };
        menu.Items.Add(open);
        menu.Items.Add(remove);
        menu.IsOpen = true;
        e.Handled = true;
    }

    private FrameworkElement CreateAddButton()
    {
        var btn = new Button
        {
            Width = 50,
            Height = 50,
            Margin = new Thickness(0, 0, 0, 6),
            Content = new TextBlock
            {
                Text = "\uE710",
                FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),
                FontSize = 16,
                Foreground = new SolidColorBrush(Color.FromArgb(0x99, 0xFF, 0xFF, 0xFF)),
            },
            Style = (Style)Application.Current.FindResource("GhostButton"),
            ToolTip = "添加 / 管理 Dock",
        };
        btn.Click += (_, _) => SettingsRequested?.Invoke();
        return btn;
    }

    private void DockRoot_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is Button || FindAncestor<Button>(e.OriginalSource as DependencyObject) != null) return;
        try { DragMove(); } catch { }
        _settings.DockX = Left;
        _settings.DockY = Top;
        SettingsStore.Save(_settings);
    }

    private void DockRoot_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (FindAncestor<Button>(e.OriginalSource as DependencyObject) != null) return;
        var menu = new ContextMenu();
        var settings = new MenuItem { Header = "设置…" };
        settings.Click += (_, _) => SettingsRequested?.Invoke();
        var hide = new MenuItem { Header = "隐藏 Dock" };
        hide.Click += (_, _) => Hide();
        menu.Items.Add(settings);
        menu.Items.Add(hide);
        menu.IsOpen = true;
        e.Handled = true;
    }

    private static T? FindAncestor<T>(DependencyObject? d) where T : DependencyObject
    {
        while (d != null && d is not T) d = VisualTreeHelper.GetParent(d);
        return d as T;
    }
}
