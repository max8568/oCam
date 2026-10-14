using FrameCam.Core;

namespace FrameCam.Core.Tests;

public class FrameLayoutTests
{
    private static readonly PixelRect Monitor = new(0, 0, 1920, 1080);
    private static readonly PixelSize LabelSize = new(80, 20);
    // Region edges: left 100, top 100, right 300, bottom 200.
    private static readonly FrameLayout Layout = new(new PixelRect(100, 100, 200, 100), LabelSize, Monitor);

    [Fact]
    public void Label_sits_above_the_top_left_corner() =>
        Assert.Equal(new PixelRect(96, 76, 80, 20), Layout.Label);

    [Fact]
    public void Label_moves_inside_the_region_when_there_is_no_room_above()
    {
        var layout = new FrameLayout(new PixelRect(100, 10, 200, 100), LabelSize, Monitor);
        Assert.Equal(new PixelRect(100, 10, 80, 20), layout.Label);
    }

    [Fact]
    public void Ring_handles_and_label_stay_outside_the_region()
    {
        Assert.All(Layout.Ring, r => Assert.False(r.IntersectsWith(Layout.Region)));
        Assert.All(Layout.Handles, h => Assert.False(h.Rect.IntersectsWith(Layout.Region)));
        Assert.False(Layout.Label.IntersectsWith(Layout.Region));
    }

    [Fact]
    public void Ring_is_four_pixels_wide_around_the_region()
    {
        var covered = Layout.Ring.Sum(r => r.Width * r.Height);
        Assert.Equal(208 * 108 - 200 * 100, covered);
    }

    [Fact]
    public void Window_bounds_cover_handles_and_label() =>
        Assert.Equal(PixelRect.FromEdges(91, 76, 309, 209), Layout.WindowBounds);

    [Theory]
    [InlineData(98, 150, Handle.Left)]
    [InlineData(98, 102, Handle.TopLeft)]    // left side, near the top corner
    [InlineData(98, 197, Handle.BottomLeft)] // left side, near the bottom corner
    [InlineData(150, 97, Handle.Top)]
    [InlineData(290, 97, Handle.TopRight)]
    [InlineData(301, 150, Handle.Right)]
    [InlineData(150, 202, Handle.Bottom)]
    [InlineData(92, 92, Handle.TopLeft)]     // corner handle beyond the ring
    [InlineData(200, 92, Handle.Top)]        // top-middle handle beyond the ring
    [InlineData(100, 80, Handle.Move)]       // label
    public void Hit_test_finds_the_grabbed_part(int x, int y, Handle expected) =>
        Assert.Equal(expected, Layout.HitTest(new PixelPoint(x, y)));

    [Theory]
    [InlineData(150, 150)] // inside the region
    [InlineData(50, 50)]   // far away
    [InlineData(94, 120)]  // just outside the left border, between handles
    public void Hit_test_misses_points_off_the_frame(int x, int y) =>
        Assert.Null(Layout.HitTest(new PixelPoint(x, y)));

    [Fact]
    public void Small_region_splits_each_side_between_its_corners()
    {
        var small = new FrameLayout(new PixelRect(100, 100, 32, 32), LabelSize, Monitor);
        Assert.Equal(Handle.TopLeft, small.HitTest(new PixelPoint(98, 115)));
        Assert.Equal(Handle.BottomLeft, small.HitTest(new PixelPoint(98, 116)));
    }
}
