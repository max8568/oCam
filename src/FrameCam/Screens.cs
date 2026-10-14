using FrameCam.Core;
using FrameCam.Native;

namespace FrameCam;

/// <summary>Monitors in physical pixels (the process is Per-Monitor V2 DPI aware) with their scaling.</summary>
internal static class Screens
{
    public static IReadOnlyList<MonitorInfo> All() => Screen.AllScreens.Select(ToMonitorInfo).ToList();

    public static MonitorInfo At(PixelPoint p) => ToMonitorInfo(Screen.FromPoint(new Point(p.X, p.Y)));

    private static MonitorInfo ToMonitorInfo(Screen screen) =>
        new(screen.DeviceName, screen.Bounds.ToPixelRect(), screen.Primary, ScaleOf(screen));

    private static double ScaleOf(Screen screen)
    {
        var center = new NativeMethods.POINT(screen.Bounds.X + screen.Bounds.Width / 2, screen.Bounds.Y + screen.Bounds.Height / 2);
        var monitor = NativeMethods.MonitorFromPoint(center, NativeMethods.MONITOR_DEFAULTTONEAREST);
        return NativeMethods.GetDpiForMonitor(monitor, NativeMethods.MDT_EFFECTIVE_DPI, out uint dpiX, out _) == 0
            ? dpiX / 96.0
            : 1.0;
    }
}
