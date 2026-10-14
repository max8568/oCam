namespace FrameCam.Core;

/// <summary>A point in physical screen pixels (virtual-desktop coordinates).</summary>
public readonly record struct PixelPoint(int X, int Y);

/// <summary>A size in physical pixels.</summary>
public readonly record struct PixelSize(int Width, int Height);

/// <summary>
/// A rectangle in physical screen pixels. Right and Bottom are exclusive: the rectangle
/// covers columns Left..Right-1 and rows Top..Bottom-1.
/// </summary>
public readonly record struct PixelRect(int Left, int Top, int Width, int Height)
{
    public int Right => Left + Width;
    public int Bottom => Top + Height;
    public PixelPoint Center => new(Left + Width / 2, Top + Height / 2);

    public static PixelRect FromEdges(int left, int top, int right, int bottom) =>
        new(left, top, right - left, bottom - top);

    public bool Contains(PixelPoint p) =>
        p.X >= Left && p.X < Right && p.Y >= Top && p.Y < Bottom;

    public bool Contains(PixelRect r) =>
        r.Left >= Left && r.Top >= Top && r.Right <= Right && r.Bottom <= Bottom;

    public bool IntersectsWith(PixelRect r) =>
        r.Left < Right && Left < r.Right && r.Top < Bottom && Top < r.Bottom;

    /// <summary>The overlapping part, or an empty rectangle (all zero) when they do not overlap.</summary>
    public PixelRect Intersect(PixelRect r)
    {
        int left = Math.Max(Left, r.Left), top = Math.Max(Top, r.Top);
        int right = Math.Min(Right, r.Right), bottom = Math.Min(Bottom, r.Bottom);
        return right > left && bottom > top ? FromEdges(left, top, right, bottom) : default;
    }

    public PixelRect Inflate(int amount) =>
        FromEdges(Left - amount, Top - amount, Right + amount, Bottom + amount);

    public PixelRect Offset(int dx, int dy) => new(Left + dx, Top + dy, Width, Height);
}

/// <summary>
/// A monitor: device name such as \\.\DISPLAY1, bounds in physical pixels,
/// and its scaling (1.0 = 100%, 1.5 = 150%).
/// </summary>
public sealed record MonitorInfo(string DeviceName, PixelRect Bounds, bool IsPrimary, double Scale);
