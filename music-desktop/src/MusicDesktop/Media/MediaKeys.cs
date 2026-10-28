using System.Runtime.InteropServices;

namespace MusicDesktop.Media;

/// <summary>没有 Stage 也没有可写会话时的兜底：直接发系统媒体键。</summary>
public static class MediaKeys
{
    private const byte VK_MEDIA_NEXT_TRACK = 0xB0;
    private const byte VK_MEDIA_PREV_TRACK = 0xB1;
    private const byte VK_MEDIA_PLAY_PAUSE = 0xB3;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    public static void PlayPause() => Tap(VK_MEDIA_PLAY_PAUSE);
    public static void Next() => Tap(VK_MEDIA_NEXT_TRACK);
    public static void Previous() => Tap(VK_MEDIA_PREV_TRACK);

    private static void Tap(byte vk)
    {
        keybd_event(vk, 0, 0, UIntPtr.Zero);
        keybd_event(vk, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }
}
