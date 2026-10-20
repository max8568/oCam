using FrameCam.Core;

namespace FrameCam.Core.Tests;

public class SnapTargetsTests
{
    private static readonly PixelRect Monitor = new(0, 0, 1920, 1080);
    private static readonly PixelRect Start = new(100, 100, 640, 360); // right 740, bottom 460

    [Theory]
    [InlineData(502, 500)] // 2 pixels away: snaps
    [InlineData(504, 500)] // 4 pixels away: snaps
    [InlineData(505, 505)] // 5 pixels away: no snap
    public void Dragged_edge_snaps_within_four_pixels(int rawRight, int expectedRight)
    {
        var snap = new SnapTargets([500], []);
        var result = FrameGeometry.Resize(Start, Handle.Right, new PixelPoint(rawRight - 740, 0), Monitor, snap);
        Assert.Equal(expectedRight, result.Right);
    }

    [Fact]
    public void Snapping_to_an_odd_size_lands_on_the_target()
    {
        var snap = new SnapTargets([601, 51], []);
        Assert.Equal(601, FrameGeometry.Resize(Start, Handle.Right, new PixelPoint(601 - 740, 0), Monitor, snap).Right);
        var left = FrameGeometry.Resize(Start, Handle.Left, new PixelPoint(51 - 100, 0), Monitor, snap);
        Assert.Equal(PixelRect.FromEdges(51, 100, 740, 460), left);
    }

    [Fact]
    public void Horizontal_edges_snap_to_y_targets()
    {
        var snap = new SnapTargets([], [300]);
        Assert.Equal(300, FrameGeometry.Resize(Start, Handle.Bottom, new PixelPoint(0, 303 - 460), Monitor, snap).Bottom);
    }

    [Fact]
    public void None_never_snaps() =>
        Assert.Equal(504, FrameGeometry.Resize(Start, Handle.Right, new PixelPoint(504 - 740, 0), Monitor, SnapTargets.None).Right);

    [Fact]
    public void FromRects_uses_window_edges_on_the_monitor_only()
    {
        var targets = SnapTargets.FromRects([new PixelRect(200, 300, 400, 100), new PixelRect(2000, 300, 100, 100)], Monitor);
        Assert.Equal(200, targets.SnapX(204));
        Assert.Equal(600, targets.SnapX(596));
        Assert.Equal(300, targets.SnapY(303));
        Assert.Equal(400, targets.SnapY(399));
        Assert.Equal(2003, targets.SnapX(2003)); // the window on another monitor is ignored
    }
}
