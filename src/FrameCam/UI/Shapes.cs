using System.Drawing.Drawing2D;

namespace FrameCam.UI;

internal static class Shapes
{
    /// <summary>A rectangle with round corners; a radius of half the height or more makes a pill.</summary>
    public static GraphicsPath Rounded(RectangleF r, float radius)
    {
        float d = 2 * Math.Min(radius, Math.Min(r.Width, r.Height) / 2);
        var path = new GraphicsPath();
        if (d <= 0)
        {
            path.AddRectangle(r);
            return path;
        }
        path.AddArc(r.Left, r.Top, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Top, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    /// <summary>
    /// A soft shadow under a rounded rectangle: <paramref name="spread"/> faint layers, each one pixel larger,
    /// so it is darkest at the edge and fades out <paramref name="spread"/> pixels away.
    /// </summary>
    public static void Shadow(Graphics g, RectangleF r, float radius, int spread, float drop)
    {
        using var brush = new SolidBrush(Color.FromArgb(5, Palette.Shadow));
        for (int i = spread; i >= 1; i--)
        {
            var layer = RectangleF.Inflate(r, i, i);
            layer.Offset(0, drop);
            using var path = Rounded(layer, radius + i);
            g.FillPath(brush, path);
        }
    }
}
