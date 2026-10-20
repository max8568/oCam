namespace FrameCam.Core;

public enum Arrow
{
    Left,
    Up,
    Right,
    Down,
}

/// <summary>Keyboard nudges, size presets and fitting to a window (spec 7.4, 7.5).</summary>
public static class FrameAdjust
{
    public static readonly IReadOnlyList<PixelSize> Presets =
        [new(1920, 1080), new(1280, 720), new(854, 480), new(640, 360)];

    /// <summary>Arrow key: moves the whole region 1 pixel, staying on the monitor.</summary>
    public static PixelRect Nudge(PixelRect region, Arrow arrow, PixelRect monitor)
    {
        var (dx, dy) = Direction(arrow);
        return FrameGeometry.ClampToMonitor(region.Offset(dx, dy), monitor);
    }

    /// <summary>
    /// Ctrl+arrow: moves the active handle's edge 1 pixel in the arrow direction. A corner uses left/right
    /// for its vertical edge and up/down for its horizontal edge; a side ignores arrows across it. The region
    /// is returned unchanged when the move would leave the monitor or go below MinSize.
    /// </summary>
    public static PixelRect NudgeEdge(PixelRect region, Handle active, Arrow arrow, PixelRect monitor)
    {
        var (dx, dy) = Direction(arrow);
        int left = region.Left, top = region.Top, right = region.Right, bottom = region.Bottom;
        if (dx != 0)
        {
            if (active.MovesLeft()) left += dx;
            else if (active.MovesRight()) right += dx;
            else return region;
        }
        else
        {
            if (active.MovesTop()) top += dy;
            else if (active.MovesBottom()) bottom += dy;
            else return region;
        }
        var moved = PixelRect.FromEdges(left, top, right, bottom);
        bool valid = monitor.Contains(moved) && moved.Width >= FrameGeometry.MinSize && moved.Height >= FrameGeometry.MinSize;
        return valid ? moved : region;
    }

    /// <summary>The preset size with the top-left kept, moved up/left just enough to fit. Null when larger than the monitor.</summary>
    public static PixelRect? ApplyPreset(PixelRect region, PixelSize size, PixelRect monitor)
    {
        if (size.Width > monitor.Width || size.Height > monitor.Height)
            return null;
        return FrameGeometry.ClampToMonitor(new PixelRect(region.Left, region.Top, size.Width, size.Height), monitor);
    }

    public static PixelRect FullMonitor(PixelRect monitor) => monitor;

    /// <summary>
    /// The region for a window's outer bounds or client area: the part on the monitor. Null when that is
    /// smaller than MinSize.
    /// </summary>
    public static PixelRect? FitToWindow(PixelRect window, PixelRect monitor)
    {
        var visible = window.Intersect(monitor);
        return visible.Width >= FrameGeometry.MinSize && visible.Height >= FrameGeometry.MinSize ? visible : null;
    }

    private static (int Dx, int Dy) Direction(Arrow arrow) => arrow switch
    {
        Arrow.Left => (-1, 0),
        Arrow.Right => (1, 0),
        Arrow.Up => (0, -1),
        Arrow.Down => (0, 1),
        _ => throw new ArgumentOutOfRangeException(nameof(arrow)),
    };
}
