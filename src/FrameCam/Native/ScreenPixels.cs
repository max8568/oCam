using System.ComponentModel;
using FrameCam.Core;

namespace FrameCam.Native;

internal static class ScreenPixels
{
    /// <summary>
    /// Copies the screen pixels of source (physical-pixel desktop coordinates) into the bitmap's top-left.
    /// Uses BitBlt with CAPTUREBLT so semi-transparent windows are included; Graphics.CopyFromScreen
    /// rejects that flag combination. Windows excluded from capture do not appear.
    /// </summary>
    public static void Copy(PixelRect source, Bitmap target)
    {
        using var g = Graphics.FromImage(target);
        IntPtr destination = g.GetHdc();
        IntPtr screen = NativeMethods.GetDC(IntPtr.Zero);
        try
        {
            if (!NativeMethods.BitBlt(destination, 0, 0, source.Width, source.Height, screen, source.Left, source.Top,
                    NativeMethods.SRCCOPY | NativeMethods.CAPTUREBLT))
                throw new Win32Exception();
        }
        finally
        {
            NativeMethods.ReleaseDC(IntPtr.Zero, screen);
            g.ReleaseHdc(destination);
        }
    }
}
