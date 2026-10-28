using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MusicDesktop.Core;

public static class IconLoader
{
    private const uint SHGFI_ICON = 0x100;
    private const uint SHGFI_LARGEICON = 0x0;
    private const uint SHGFI_USEFILEATTRIBUTES = 0x10;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct SHFILEINFO
    {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)] public string szTypeName;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes,
        ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    public static ImageSource? Load(string path, int size = 48)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path)) return null;
            var info = new SHFILEINFO();
            var flags = SHGFI_ICON | SHGFI_LARGEICON;
            var res = SHGetFileInfo(path, 0, ref info, (uint)Marshal.SizeOf<SHFILEINFO>(), flags);
            if (res == IntPtr.Zero || info.hIcon == IntPtr.Zero) return null;

            var bmp = Imaging.CreateBitmapSourceFromHIcon(info.hIcon, Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            bmp.Freeze();
            DestroyIcon(info.hIcon);
            return bmp;
        }
        catch { return null; }
    }
}
