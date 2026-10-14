using System.Runtime.InteropServices;

namespace FrameCam.CaptureTests;

internal static class TestNative
{
    private const int SM_CMONITORS = 80;

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int index);

    public static int MonitorCount() => GetSystemMetrics(SM_CMONITORS);
}
