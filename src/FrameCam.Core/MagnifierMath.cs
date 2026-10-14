namespace FrameCam.Core;

/// <summary>
/// A region edge inside the magnifier, in source pixels from the view's top-left. A vertical line at
/// Offset k lies on the seam between source columns k-1 and k and spans rows From..To (To exclusive).
/// </summary>
public readonly record struct MagnifierLine(bool Vertical, int Offset, int From, int To);

/// <summary>Calculations for the magnifier (spec 7.3).</summary>
public static class MagnifierMath
{
    public const int SourcePixels = 21;
    public const int BaseZoom = 8;
    public const int CursorOffset = 24;

    /// <summary>
    /// The pixel to center on: just inside the grabbed corner, the point on the grabbed edge nearest the
    /// cursor, or the top-left pixel when the whole frame moves.
    /// </summary>
    public static PixelPoint FocusPoint(PixelRect region, Handle handle, PixelPoint cursor)
    {
        int x = Math.Clamp(cursor.X, region.Left, region.Right - 1);
        int y = Math.Clamp(cursor.Y, region.Top, region.Bottom - 1);
        return handle switch
        {
            Handle.Move or Handle.TopLeft => new(region.Left, region.Top),
            Handle.TopRight => new(region.Right - 1, region.Top),
            Handle.BottomLeft => new(region.Left, region.Bottom - 1),
            Handle.BottomRight => new(region.Right - 1, region.Bottom - 1),
            Handle.Left => new(region.Left, y),
            Handle.Right => new(region.Right - 1, y),
            Handle.Top => new(x, region.Top),
            Handle.Bottom => new(x, region.Bottom - 1),
            _ => throw new ArgumentOutOfRangeException(nameof(handle)),
        };
    }

    /// <summary>The screen pixels shown, centered on the focus pixel.</summary>
    public static PixelRect SourceRect(PixelPoint focus) =>
        new(focus.X - SourcePixels / 2, focus.Y - SourcePixels / 2, SourcePixels, SourcePixels);

    /// <summary>Screen pixels per source pixel: 8 at 100% scaling, 12 at 150%.</summary>
    public static int Zoom(double monitorScale) => Math.Max(1, (int)Math.Round(BaseZoom * monitorScale));

    /// <summary>The region's edges that cross the view, clipped to the view.</summary>
    public static IReadOnlyList<MagnifierLine> BoundaryLines(PixelRect source, PixelRect region)
    {
        var lines = new List<MagnifierLine>();
        int rowFrom = Math.Clamp(region.Top - source.Top, 0, source.Height);
        int rowTo = Math.Clamp(region.Bottom - source.Top, 0, source.Height);
        int colFrom = Math.Clamp(region.Left - source.Left, 0, source.Width);
        int colTo = Math.Clamp(region.Right - source.Left, 0, source.Width);
        if (rowFrom < rowTo)
        {
            AddIfInView(lines, true, region.Left - source.Left, rowFrom, rowTo, source.Width);
            AddIfInView(lines, true, region.Right - source.Left, rowFrom, rowTo, source.Width);
        }
        if (colFrom < colTo)
        {
            AddIfInView(lines, false, region.Top - source.Top, colFrom, colTo, source.Height);
            AddIfInView(lines, false, region.Bottom - source.Top, colFrom, colTo, source.Height);
        }
        return lines;
    }

    /// <summary>
    /// Top-left of the magnifier window: CursorOffset below and right of the anchor, flipped to the other
    /// side where it would leave the monitor.
    /// </summary>
    public static PixelPoint Placement(PixelPoint anchor, PixelSize size, PixelRect monitor)
    {
        int x = anchor.X + CursorOffset;
        if (x + size.Width > monitor.Right)
            x = anchor.X - CursorOffset - size.Width;
        int y = anchor.Y + CursorOffset;
        if (y + size.Height > monitor.Bottom)
            y = anchor.Y - CursorOffset - size.Height;
        return new PixelPoint(
            Math.Clamp(x, monitor.Left, Math.Max(monitor.Left, monitor.Right - size.Width)),
            Math.Clamp(y, monitor.Top, Math.Max(monitor.Top, monitor.Bottom - size.Height)));
    }

    private static void AddIfInView(List<MagnifierLine> lines, bool vertical, int offset, int from, int to, int extent)
    {
        if (offset >= 0 && offset <= extent)
            lines.Add(new MagnifierLine(vertical, offset, from, to));
    }
}
