using System.Runtime.InteropServices;

namespace FrameCam.CaptureTests;

internal static class TestNative
{
    private const int SM_CMONITORS = 80;

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int index);

    public static int MonitorCount() => GetSystemMetrics(SM_CMONITORS);

    private const uint GA_ROOT = 2;

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT(int x, int y)
    {
        public int X = x;
        public int Y = y;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr WindowFromPoint(POINT point);

    [DllImport("user32.dll")]
    private static extern IntPtr GetAncestor(IntPtr hwnd, uint flags);

    /// <summary>The top-level window the mouse would reach at this screen point.</summary>
    public static IntPtr TopLevelWindowAt(Point p) => GetAncestor(WindowFromPoint(new POINT(p.X, p.Y)), GA_ROOT);
}
