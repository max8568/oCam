namespace FrameCam.Core;

public static class BarPlacement
{
    /// <summary>
    /// Where the control bar's top-left corner goes at startup (spec 8.4): where it was at exit, if that monitor
    /// still exists and the whole bar still fits on it. Null means its usual place, the top right.
    /// </summary>
    public static PixelPoint? Restore(SavedPosition? saved, PixelSize bar, IReadOnlyList<MonitorInfo> monitors)
    {
        if (saved is null)
            return null;
        var monitor = monitors.FirstOrDefault(m => m.DeviceName == saved.DeviceName);
        if (monitor is null || !monitor.Bounds.Contains(new PixelRect(saved.Left, saved.Top, bar.Width, bar.Height)))
            return null;
        return new PixelPoint(saved.Left, saved.Top);
    }
}
