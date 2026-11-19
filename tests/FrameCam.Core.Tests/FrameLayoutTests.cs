using FrameCam.Core;

namespace FrameCam.Core.Tests;

public class FrameLayoutTests
{
    private static readonly PixelRect Monitor = new(0, 0, 1920, 1080);
    private static readonly PixelSize LabelSize = new(80, 20);
    // Region edges: left 100, top 100, right 300, bottom 200.
    private static readonly FrameLayout Layout = new(new PixelRect(100, 100, 200, 100), LabelSize, LabelSize, Monitor);

    [Fact]
    public void Label_sits_above_the_top_left_corner() =>
        Assert.Equal(new PixelRect(100, 78, 80, 20), Layout.PositionLabel);

    [Fact]
    public void Label_does_not_cover_the_top_left_handle() =>
        Assert.False(Layout.PositionLabel.IntersectsWith(Layout.Handles.Single(h => h.Handle == Handle.TopLeft).Rect));

    [Fact]
    public void Label_on_a_narrow_region_goes_above_the_handles()
    {
        // The label (80 wide) would reach the top-middle handle at x 156..164.
        var narrow = new FrameLayout(new PixelRect(100, 100, 120, 100), LabelSize, LabelSize, Monitor);
        Assert.Equal(new PixelRect(100, 71, 80, 20), narrow.PositionLabel);
    }

    [Fact]
    public void Label_on_a_narrow_region_near_the_top_goes_inside()
    {
        var narrow = new FrameLayout(new PixelRect(100, 25, 120, 100), LabelSize, LabelSize, Monitor);
        Assert.Equal(new PixelRect(100, 25, 80, 20), narrow.PositionLabel);
    }

    [Theory]
    [InlineData(32, 100)]
    [InlineData(96, 100)]
    [InlineData(120, 100)]
    [InlineData(160, 100)]
    [InlineData(200, 100)]
    [InlineData(120, 25)]
    public void Label_never_covers_a_handle(int width, int top)
    {
        var layout = new FrameLayout(new PixelRect(100, top, width, 100), LabelSize, LabelSize, Monitor);
        Assert.All(layout.Handles, h => Assert.False(layout.PositionLabel.IntersectsWith(h.Rect), $"covers {h.Handle}"));
    }

    [Fact]
    public void Label_moves_inside_the_region_when_there_is_no_room_above()
    {
        var layout = new FrameLayout(new PixelRect(100, 10, 200, 100), LabelSize, LabelSize, Monitor);
        Assert.Equal(new PixelRect(100, 10, 80, 20), layout.PositionLabel);
    }

    [Fact]
    public void Size_label_sits_below_the_bottom_right_corner() =>
        Assert.Equal(new PixelRect(220, 202, 80, 20), Layout.SizeLabel);

    [Fact]
    public void Size_label_on_a_narrow_region_goes_below_the_handles()
    {
        // The size label (x 140..220) would reach the bottom-middle handle at x 156..164.
        var narrow = new FrameLayout(new PixelRect(100, 100, 120, 100), LabelSize, LabelSize, Monitor);
        Assert.Equal(new PixelRect(140, 209, 80, 20), narrow.SizeLabel);
    }

    [Fact]
    public void Size_label_moves_inside_the_region_when_there_is_no_room_below()
    {
        var layout = new FrameLayout(new PixelRect(100, 970, 200, 100), LabelSize, LabelSize, Monitor);
        Assert.Equal(new PixelRect(220, 1050, 80, 20), layout.SizeLabel);
    }

    [Theory]
    [InlineData(32, 100)]
    [InlineData(96, 100)]
    [InlineData(120, 100)]
    [InlineData(160, 100)]
    [InlineData(200, 100)]
    [InlineData(120, 975)]
    public void Size_label_never_covers_a_handle(int width, int top)
    {
        var layout = new FrameLayout(new PixelRect(100, top, width, 100), LabelSize, LabelSize, Monitor);
        Assert.All(layout.Handles, h => Assert.False(layout.SizeLabel.IntersectsWith(h.Rect), $"covers {h.Handle}"));
    }

    [Fact]
    public void Ring_handles_and_labels_stay_outside_the_region()
    {
        Assert.All(Layout.Ring, r => Assert.False(r.IntersectsWith(Layout.Region)));
        Assert.All(Layout.Handles, h => Assert.False(h.Rect.IntersectsWith(Layout.Region)));
        Assert.False(Layout.PositionLabel.IntersectsWith(Layout.Region));
        Assert.False(Layout.SizeLabel.IntersectsWith(Layout.Region));
    }

    [Fact]
    public void Ring_is_two_pixels_wide_around_the_region()
    {
        var covered = Layout.Ring.Sum(r => r.Width * r.Height);
        Assert.Equal(204 * 104 - 200 * 100, covered);
    }

    [Fact]
    public void Window_bounds_cover_handles_and_labels() =>
        Assert.Equal(PixelRect.FromEdges(91, 78, 309, 222), Layout.WindowBounds);

    [Theory]
    [InlineData(98, 150, Handle.Left)]
    [InlineData(98, 102, Handle.TopLeft)]    // left side, near the top corner
    [InlineData(98, 197, Handle.BottomLeft)] // left side, near the bottom corner
    [InlineData(150, 98, Handle.Top)]
    [InlineData(290, 98, Handle.TopRight)]
    [InlineData(301, 150, Handle.Right)]
    [InlineData(150, 201, Handle.Bottom)]
    [InlineData(92, 92, Handle.TopLeft)]     // corner handle beyond the ring
    [InlineData(200, 92, Handle.Top)]        // top-middle handle beyond the ring
    [InlineData(100, 80, Handle.Move)]       // position label
    [InlineData(250, 210, Handle.Move)]      // size label
    public void Hit_test_finds_the_grabbed_part(int x, int y, Handle expected) =>
        Assert.Equal(expected, Layout.HitTest(new PixelPoint(x, y)));

    [Theory]
    [InlineData(150, 150)] // inside the region
    [InlineData(50, 50)]   // far away
    [InlineData(97, 120)]  // just outside the left border, between handles
    public void Hit_test_misses_points_off_the_frame(int x, int y) =>
        Assert.Null(Layout.HitTest(new PixelPoint(x, y)));

    [Fact]
    public void Mover_sits_in_the_middle_of_the_region() =>
        Assert.Equal(new PixelRect(186, 136, 28, 28), Layout.Mover);

    [Theory]
    [InlineData(200, 150)] // center
    [InlineData(186, 136)] // its top-left pixel
    [InlineData(213, 163)] // its bottom-right pixel
    public void Pressing_the_mover_moves_the_frame(int x, int y) =>
        Assert.Equal(Handle.Move, Layout.HitTest(new PixelPoint(x, y)));

    [Theory]
    [InlineData(185, 150)]
    [InlineData(214, 150)]
    [InlineData(200, 135)]
    [InlineData(200, 164)]
    public void Points_next_to_the_mover_are_not_on_the_frame(int x, int y) =>
        Assert.Null(Layout.HitTest(new PixelPoint(x, y)));

    [Theory]
    [InlineData(200, 94)]
    [InlineData(94, 200)]
    public void Region_narrower_or_lower_than_96_has_no_mover(int width, int height)
    {
        var layout = new FrameLayout(new PixelRect(100, 100, width, height), LabelSize, LabelSize, Monitor);
        Assert.Null(layout.Mover);
        Assert.Null(layout.HitTest(new PixelPoint(100 + width / 2, 100 + height / 2)));
    }

    [Fact]
    public void Region_of_96_by_96_has_a_mover() =>
        Assert.NotNull(new FrameLayout(new PixelRect(100, 100, 96, 96), LabelSize, LabelSize, Monitor).Mover);

    [Fact]
    public void Small_region_splits_each_side_between_its_corners()
    {
        var small = new FrameLayout(new PixelRect(100, 100, 32, 32), LabelSize, LabelSize, Monitor);
        Assert.Equal(Handle.TopLeft, small.HitTest(new PixelPoint(98, 115)));
        Assert.Equal(Handle.BottomLeft, small.HitTest(new PixelPoint(98, 116)));
    }
}
