using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace MusicDesktop.Core;

public static class IconFactory
{
    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    private static Bitmap Draw(int size)
    {
        var bmp = new Bitmap(size, size);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);
        using var path = new GraphicsPath();
        path.AddEllipse(1, 1, size - 2, size - 2);
        using var brush = new LinearGradientBrush(new Rectangle(0, 0, size, size),
            Color.FromArgb(255, 124, 231, 255), Color.FromArgb(255, 156, 140, 255), 45f);
        g.FillPath(brush, path);

        using var font = new System.Drawing.Font("Segoe UI Symbol", size * 0.46f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Pixel);
        using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString("\u266B", font, Brushes.White, new RectangleF(0, 0, size, size), sf);
        return bmp;
    }

    public static Icon CreateAppIcon(int size = 64)
    {
        using var bmp = Draw(size);
        var hIcon = bmp.GetHicon();
        return Icon.FromHandle(hIcon);
    }

    public static System.Windows.Media.ImageSource CreateImageSource(int size = 64)
    {
        using var bmp = Draw(size);
        var hbmp = bmp.GetHbitmap();
        try
        {
            var src = Imaging.CreateBitmapSourceFromHBitmap(hbmp, IntPtr.Zero, System.Windows.Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            src.Freeze();
            return src;
        }
        finally
        {
            DeleteObject(hbmp);
        }
    }
}
