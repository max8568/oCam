using System.ComponentModel;

namespace FrameCam.Native;

internal static class CaptureExclusion
{
    /// <summary>
    /// Leaves the window out of every screen capture (BitBlt, Desktop Duplication, Windows.Graphics.Capture)
    /// while it stays visible on the monitor. Requires Windows 10 2004 or later.
    /// </summary>
    public static void Apply(IntPtr hwnd)
    {
        if (!NativeMethods.SetWindowDisplayAffinity(hwnd, NativeMethods.WDA_EXCLUDEFROMCAPTURE))
            throw new Win32Exception();
    }
}
