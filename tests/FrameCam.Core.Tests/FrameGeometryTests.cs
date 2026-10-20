using FrameCam.Core;

namespace FrameCam.Core.Tests;

public class FrameGeometryTests
{
    private static readonly PixelRect Monitor = new(0, 0, 1920, 1080);
    private static readonly PixelRect Start = new(100, 100, 640, 360); // right 740, bottom 460

    private static PixelRect Resize(Handle handle, int dx, int dy) =>
        FrameGeometry.Resize(Start, handle, new PixelPoint(dx, dy), Monitor, SnapTargets.None);

    [Theory]
    [InlineData(5, 645)]
    [InlineData(6, 646)]
    [InlineData(-3, 637)]
    public void Dragging_right_edge_moves_it_one_pixel_at_a_time(int dx, int expectedWidth) =>
        Assert.Equal(new PixelRect(100, 100, expectedWidth, 360), Resize(Handle.Right, dx, 0));

    [Theory]
    [InlineData(-5, 95, 645)]
    [InlineData(3, 103, 637)]
    public void Dragging_left_edge_moves_only_the_left_edge(int dx, int expectedLeft, int expectedWidth) =>
        Assert.Equal(new PixelRect(expectedLeft, 100, expectedWidth, 360), Resize(Handle.Left, dx, 0));

    [Fact]
    public void Dragging_top_left_corner_changes_both_edges() =>
        Assert.Equal(new PixelRect(97, 97, 643, 363), Resize(Handle.TopLeft, -3, -3));

    [Fact]
    public void Dragging_an_edge_ignores_movement_along_it() =>
        Assert.Equal(new PixelRect(100, 100, 644, 360), Resize(Handle.Right, 4, 50));

    [Fact]
    public void Region_stops_at_the_monitor_edge()
    {
        Assert.Equal(PixelRect.FromEdges(100, 100, 1920, 1080), Resize(Handle.BottomRight, 5000, 5000));
        Assert.Equal(PixelRect.FromEdges(0, 0, 740, 460), Resize(Handle.TopLeft, -5000, -5000));
    }

    [Fact]
    public void Odd_left_edge_reaches_the_monitor_edge()
    {
        var start = new PixelRect(1001, 100, 640, 360);
        var result = FrameGeometry.Resize(start, Handle.Right, new PixelPoint(5000, 0), Monitor, SnapTargets.None);
        Assert.Equal(PixelRect.FromEdges(1001, 100, 1920, 460), result);
    }

    [Fact]
    public void Region_never_gets_smaller_than_the_minimum()
    {
        Assert.Equal(new PixelRect(708, 100, 32, 360), Resize(Handle.Left, 1000, 0));
        Assert.Equal(new PixelRect(100, 100, 32, 360), Resize(Handle.Right, -1000, 0));
    }

    [Fact]
    public void Resize_rejects_the_move_handle() =>
        Assert.Throws<ArgumentException>(() => Resize(Handle.Move, 1, 1));

    [Fact]
    public void Moving_shifts_one_pixel_at_a_time() =>
        Assert.Equal(new PixelRect(103, 99, 640, 360), FrameGeometry.Move(Start, new PixelPoint(3, -1), Monitor));

    [Fact]
    public void Moving_stops_at_the_monitor_edge() =>
        Assert.Equal(new PixelRect(1280, 0, 640, 360), FrameGeometry.Move(Start, new PixelPoint(5000, -5000), Monitor));

    [Fact]
    public void Moving_to_a_smaller_monitor_shrinks_to_fit()
    {
        var small = new PixelRect(1920, 0, 801, 599);
        var big = new PixelRect(0, 0, 1280, 720);
        Assert.Equal(new PixelRect(1920, 0, 801, 599), FrameGeometry.Move(big, new PixelPoint(1920, 0), small));
    }

    [Theory]
    [InlineData(7, 6)]
    [InlineData(8, 8)]
    [InlineData(0, 0)]
    public void FloorEven_rounds_down_to_even(int value, int expected) =>
        Assert.Equal(expected, FrameGeometry.FloorEven(value));
}
