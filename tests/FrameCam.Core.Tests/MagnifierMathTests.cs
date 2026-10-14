using FrameCam.Core;

namespace FrameCam.Core.Tests;

public class MagnifierMathTests
{
    // Region edges: left 100, top 100, right 300, bottom 200.
    private static readonly PixelRect Region = new(100, 100, 200, 100);

    [Theory]
    [InlineData(Handle.TopLeft, 0, 0, 100, 100)]
    [InlineData(Handle.Move, 0, 0, 100, 100)]
    [InlineData(Handle.TopRight, 0, 0, 299, 100)]
    [InlineData(Handle.BottomLeft, 0, 0, 100, 199)]
    [InlineData(Handle.BottomRight, 0, 0, 299, 199)]
    [InlineData(Handle.Left, 97, 150, 100, 150)]
    [InlineData(Handle.Left, 97, 50, 100, 100)]    // cursor above the region: clamped to the edge
    [InlineData(Handle.Right, 305, 250, 299, 199)]
    [InlineData(Handle.Top, 180, 95, 180, 100)]
    [InlineData(Handle.Bottom, 180, 203, 180, 199)]
    public void Focus_is_the_pixel_just_inside_the_grabbed_part(Handle handle, int cursorX, int cursorY, int x, int y) =>
        Assert.Equal(new PixelPoint(x, y), MagnifierMath.FocusPoint(Region, handle, new PixelPoint(cursorX, cursorY)));

    [Fact]
    public void Source_is_21_pixels_centered_on_the_focus() =>
        Assert.Equal(new PixelRect(90, 90, 21, 21), MagnifierMath.SourceRect(new PixelPoint(100, 100)));

    [Theory]
    [InlineData(1.0, 8)]
    [InlineData(1.25, 10)]
    [InlineData(1.5, 12)]
    [InlineData(2.0, 16)]
    public void Zoom_follows_monitor_scaling(double scale, int zoom) =>
        Assert.Equal(zoom, MagnifierMath.Zoom(scale));

    [Fact]
    public void Lines_show_the_edges_that_cross_the_view()
    {
        var source = MagnifierMath.SourceRect(new PixelPoint(100, 100)); // 90..110
        Assert.Equal(
            new[] { new MagnifierLine(true, 10, 10, 21), new MagnifierLine(false, 10, 10, 21) },
            MagnifierMath.BoundaryLines(source, Region));
    }

    [Fact]
    public void Lines_include_the_far_edges_when_they_are_in_view()
    {
        var source = MagnifierMath.SourceRect(new PixelPoint(299, 199)); // 289..309, 189..209
        Assert.Equal(
            new[] { new MagnifierLine(true, 11, 0, 11), new MagnifierLine(false, 11, 0, 11) },
            MagnifierMath.BoundaryLines(source, Region));
    }

    [Fact]
    public void No_lines_when_the_region_is_out_of_view() =>
        Assert.Empty(MagnifierMath.BoundaryLines(MagnifierMath.SourceRect(new PixelPoint(500, 500)), Region));

    [Fact]
    public void Placement_is_below_right_of_the_anchor() =>
        Assert.Equal(new PixelPoint(524, 524), MagnifierMath.Placement(new PixelPoint(500, 500), new PixelSize(168, 208), new PixelRect(0, 0, 1920, 1080)));

    [Fact]
    public void Placement_flips_left_near_the_right_edge() =>
        Assert.Equal(new PixelPoint(1658, 524), MagnifierMath.Placement(new PixelPoint(1850, 500), new PixelSize(168, 208), new PixelRect(0, 0, 1920, 1080)));

    [Fact]
    public void Placement_flips_up_near_the_bottom_edge() =>
        Assert.Equal(new PixelPoint(524, 768), MagnifierMath.Placement(new PixelPoint(500, 1000), new PixelSize(168, 208), new PixelRect(0, 0, 1920, 1080)));

    [Fact]
    public void Placement_works_on_a_monitor_right_of_the_primary() =>
        Assert.Equal(new PixelPoint(1954, 34), MagnifierMath.Placement(new PixelPoint(1930, 10), new PixelSize(168, 208), new PixelRect(1920, 0, 1920, 1080)));
}
