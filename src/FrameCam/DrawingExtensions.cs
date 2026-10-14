using FrameCam.Core;

namespace FrameCam;

internal static class DrawingExtensions
{
    public static PixelRect ToPixelRect(this Rectangle r) => new(r.X, r.Y, r.Width, r.Height);

    public static Rectangle ToRectangle(this PixelRect r) => new(r.Left, r.Top, r.Width, r.Height);

    public static PixelPoint ToPixelPoint(this Point p) => new(p.X, p.Y);
}
