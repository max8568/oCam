using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using FrameCam.Core;

namespace FrameCam.UI;

/// <summary>
/// The magnifier's picture (spec 7.3): the enlarged pixels in a circle with a gray rim, and the position of
/// the pixel in the middle in a label below it. Everything else is see-through.
/// </summary>
internal static class MagnifierPainter
{
    public static readonly Color Rim = Color.FromArgb(128, 128, 128);

    // Sizes at 100% scaling.
    private const float RimWidth = 2;
    private const float LabelGap = 6;
    private const float LabelHeight = 24;
    private const float LabelPadX = 10;
    private const float LabelRadius = 6;

    private static readonly StringFormat Centered = new(StringFormat.GenericTypographic)
    {
        Alignment = StringAlignment.Center,
        LineAlignment = StringAlignment.Center,
        FormatFlags = StringFormatFlags.NoWrap,
    };

    public static Bitmap Render(Bitmap pixels, PixelRect source, PixelRect region, int zoom, double scale)
    {
        float s = (float)scale;
        int side = MagnifierMath.SourcePixels * zoom;
        int gap = (int)MathF.Round(LabelGap * s), labelHeight = (int)MathF.Round(LabelHeight * s);
        float rim = MathF.Round(RimWidth * s);

        var image = new Bitmap(side, side + gap + labelHeight, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(image);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.Half;
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        using var rimPen = new Pen(Rim, rim);

        using (var view = View(pixels, source, region, zoom))
        using (var texture = new TextureBrush(view))
            g.FillEllipse(texture, 0, 0, side, side);
        g.DrawEllipse(rimPen, rim / 2, rim / 2, side - rim, side - rim);

        using var font = new Font("Segoe UI", 12 * s, FontStyle.Bold, GraphicsUnit.Pixel);
        string text = Label(source);
        float labelWidth = Math.Min(side, MathF.Ceiling(g.MeasureString(text, font, side, Centered).Width + 2 * LabelPadX * s));
        var label = new RectangleF((side - labelWidth) / 2, side + gap, labelWidth, labelHeight);
        using (var fill = new SolidBrush(Color.Black))
        using (var shape = Shapes.Rounded(label, LabelRadius * s))
            g.FillPath(fill, shape);
        using (var outline = Shapes.Rounded(RectangleF.Inflate(label, -rim / 2, -rim / 2), LabelRadius * s - rim / 2))
            g.DrawPath(rimPen, outline);
        using var ink = new SolidBrush(Color.White);
        g.DrawString(text, font, ink, label, Centered);
        return image;
    }

    /// <summary>"(1691, 949)": the desktop position of the pixel in the middle of the view, the point being adjusted.</summary>
    internal static string Label(PixelRect source) =>
        $"({source.Left + MagnifierMath.SourcePixels / 2}, {source.Top + MagnifierMath.SourcePixels / 2})";

    /// <summary>The enlarged pixels as a square: outside the region dimmed, a faint grid, the region's edges in blue.</summary>
    private static Bitmap View(Bitmap pixels, PixelRect source, PixelRect region, int z)
    {
        int n = MagnifierMath.SourcePixels, side = n * z;
        var view = new Bitmap(side, side, PixelFormat.Format24bppRgb);
        using var g = Graphics.FromImage(view);

        g.InterpolationMode = InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode = PixelOffsetMode.Half;
        g.DrawImage(pixels, new Rectangle(0, 0, side, side));
        g.PixelOffsetMode = PixelOffsetMode.Default;

        using var dim = new SolidBrush(Color.FromArgb(150, 0, 0, 0));
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
                if (!region.Contains(new PixelPoint(source.Left + x, source.Top + y)))
                    g.FillRectangle(dim, x * z, y * z, z, z);

        using var grid = new Pen(Color.FromArgb(50, 255, 255, 255));
        for (int k = 1; k < n; k++)
        {
            g.DrawLine(grid, k * z, 0, k * z, side);
            g.DrawLine(grid, 0, k * z, side, k * z);
        }

        using var edge = new Pen(Color.FromArgb(0, 200, 255), Math.Max(2, z / 4));
        foreach (var line in MagnifierMath.BoundaryLines(source, region))
        {
            if (line.Vertical)
                g.DrawLine(edge, line.Offset * z, line.From * z, line.Offset * z, line.To * z);
            else
                g.DrawLine(edge, line.From * z, line.Offset * z, line.To * z, line.Offset * z);
        }
        return view;
    }
}
