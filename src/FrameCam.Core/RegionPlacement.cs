namespace FrameCam.Core;

public static class RegionPlacement
{
    public static readonly PixelSize DefaultSize = new(1280, 720);

    /// <summary>
    /// Where the region goes at startup or after the monitors change (spec 8.5): the saved region if its
    /// monitor still exists and it still fits there; otherwise the saved size, or 1280×720 on first run,
    /// centered on the primary monitor and shrunk to fit.
    /// </summary>
    public static (PixelRect Region, MonitorInfo Monitor) Restore(SavedRegion? saved, IReadOnlyList<MonitorInfo> monitors)
    {
        var primary = monitors.FirstOrDefault(m => m.IsPrimary) ?? monitors[0];
        if (saved is not null)
        {
            var monitor = monitors.FirstOrDefault(m => m.DeviceName == saved.DeviceName);
            if (monitor is not null && monitor.Bounds.Contains(saved.Rect))
                return (FrameGeometry.ClampToMonitor(saved.Rect, monitor.Bounds), monitor);
        }

        var size = saved is null ? DefaultSize : new PixelSize(saved.Width, saved.Height);
        var b = primary.Bounds;
        var centered = new PixelRect(b.Left + (b.Width - size.Width) / 2, b.Top + (b.Height - size.Height) / 2, size.Width, size.Height);
        return (FrameGeometry.ClampToMonitor(centered, b), primary);
    }

    /// <summary>
    /// Where a recording last started with this custom size, if its monitor still exists and it still fits
    /// there. Each custom size has its own place, even two with the same width and height.
    /// </summary>
    public static (PixelRect Region, MonitorInfo Monitor)? LastRecorded(CustomSize size, IReadOnlyList<CustomSizePlace> places, IReadOnlyList<MonitorInfo> monitors)
    {
        var place = places.FirstOrDefault(p => p.Size == size);
        var monitor = monitors.FirstOrDefault(m => m.DeviceName == place?.Position.DeviceName);
        return place is not null && monitor is not null && monitor.Bounds.Contains(place.Rect) ? (place.Rect, monitor) : null;
    }

    /// <summary>
    /// The places with <paramref name="recorded"/> replacing its custom size's place. Places of sizes no
    /// longer in <paramref name="customs"/> are dropped, and so is <paramref name="recorded"/> if its size is.
    /// </summary>
    public static IReadOnlyList<CustomSizePlace> RememberRecorded(IReadOnlyList<CustomSizePlace> places, CustomSizePlace recorded, IReadOnlyList<CustomSize> customs) =>
        places.Where(p => p.Size != recorded.Size).Append(recorded).Where(p => customs.Contains(p.Size)).ToArray();
}
