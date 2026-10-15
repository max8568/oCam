using FrameCam.Core;
using FrameCam.UI;

namespace FrameCam.CaptureTests;

/// <summary>The picture the magnifier shows. Drawn into a bitmap; nothing is shown on screen.</summary>
public class MagnifierPainterTests
{
    private const int Zoom = 8;
    private const int Side = MagnifierMath.SourcePixels * Zoom; // the circle's diameter: 168
    private static readonly Color Red = Color.FromArgb(200, 30, 30);

    [Fact]
    public void The_view_is_a_circle_so_its_corners_are_see_through()
    {
        using var image = Render();

        Assert.Equal(Side, image.Width);
        Assert.Equal(0, image.GetPixel(0, 0).A);
        Assert.Equal(0, image.GetPixel(Side - 1, 0).A);
        Assert.Equal(0, image.GetPixel(0, Side - 1).A);
        Assert.Equal(0, image.GetPixel(Side - 1, Side - 1).A);
        Assert.Equal(Red.ToArgb(), image.GetPixel(Side / 2, Side / 2).ToArgb()); // the focus pixel, enlarged
    }

    [Fact]
    public void The_circle_has_a_gray_rim()
    {
        using var image = Render();

        AssertGray(image.GetPixel(Side / 2, 1));        // top
        AssertGray(image.GetPixel(1, Side / 2));        // left
        AssertGray(image.GetPixel(Side / 2, Side - 2)); // bottom
    }

    [Fact]
    public void The_label_sits_on_its_own_below_the_circle()
    {
        using var image = Render();
        int labelTop = Side + 6;

        Assert.Equal(0, image.GetPixel(Side / 2, Side + 3).A); // the gap between the circle and the label
        AssertGray(image.GetPixel(Side / 2, labelTop + 1));    // the label's rim
        Assert.Equal(0, image.GetPixel(0, image.Height - 1).A); // the label is narrower than the circle
        bool text = false;
        for (int y = labelTop; y < image.Height && !text; y++)
            for (int x = 0; x < image.Width && !text; x++)
                text = image.GetPixel(x, y) is { A: 255, R: > 220, G: > 220, B: > 220 };
        Assert.True(text, "no white text in the label");
    }

    [Fact]
    public void The_label_gives_the_position_of_the_pixel_in_the_middle() =>
        Assert.Equal("(1691, -5)", MagnifierPainter.Label(MagnifierMath.SourceRect(new PixelPoint(1691, -5))));

    [Fact]
    public void The_label_leaves_out_the_region_size()
    {
        using var large = Render(new PixelRect(0, 0, 1280, 720));
        using var small = Render(new PixelRect(0, 0, 1000, 600)); // still around every pixel shown

        Assert.Equal(large.Size, small.Size);
        for (int y = 0; y < large.Height; y++)
            for (int x = 0; x < large.Width; x++)
                Assert.True(large.GetPixel(x, y) == small.GetPixel(x, y), $"the pictures differ at ({x}, {y})");
    }

    /// <summary>A red screen around (100, 100), all of it inside the region, at 100% scaling.</summary>
    private static Bitmap Render(PixelRect? region = null)
    {
        using var pixels = new Bitmap(MagnifierMath.SourcePixels, MagnifierMath.SourcePixels);
        using (var g = Graphics.FromImage(pixels))
            g.Clear(Red);
        var source = MagnifierMath.SourceRect(new PixelPoint(100, 100));
        return MagnifierPainter.Render(pixels, source, region ?? new PixelRect(0, 0, 1280, 720), Zoom, 1.0);
    }

    private static void AssertGray(Color c)
    {
        Assert.Equal(255, c.A);
        Assert.InRange(c.R, MagnifierPainter.Rim.R - 12, MagnifierPainter.Rim.R + 12);
        Assert.InRange(c.G, MagnifierPainter.Rim.G - 12, MagnifierPainter.Rim.G + 12);
        Assert.InRange(c.B, MagnifierPainter.Rim.B - 12, MagnifierPainter.Rim.B + 12);
    }
}
