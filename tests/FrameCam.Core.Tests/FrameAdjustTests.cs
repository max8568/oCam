using FrameCam.Core;

namespace FrameCam.Core.Tests;

public class FrameAdjustTests
{
    private static readonly PixelRect Monitor = new(0, 0, 1920, 1080);
    private static readonly PixelRect Region = new(100, 100, 640, 360); // right 740, bottom 460

    [Theory]
    [InlineData(Arrow.Left, 99, 100)]
    [InlineData(Arrow.Right, 101, 100)]
    [InlineData(Arrow.Up, 100, 99)]
    [InlineData(Arrow.Down, 100, 101)]
    public void Arrow_moves_the_region_one_pixel(Arrow arrow, int left, int top) =>
        Assert.Equal(new PixelRect(left, top, 640, 360), FrameAdjust.Nudge(Region, arrow, Monitor));

    [Fact]
    public void Arrow_stops_at_the_monitor_edge()
    {
        var atEdge = new PixelRect(0, 0, 640, 360);
        Assert.Equal(atEdge, FrameAdjust.Nudge(atEdge, Arrow.Left, Monitor));
    }

    [Theory]
    [InlineData(Handle.BottomRight, Arrow.Right, 100, 100, 741, 460)]
    [InlineData(Handle.BottomRight, Arrow.Up, 100, 100, 740, 459)]
    [InlineData(Handle.TopLeft, Arrow.Left, 99, 100, 740, 460)]
    [InlineData(Handle.TopLeft, Arrow.Down, 100, 101, 740, 460)]
    [InlineData(Handle.Left, Arrow.Right, 101, 100, 740, 460)]
    [InlineData(Handle.Left, Arrow.Up, 100, 100, 740, 460)]     // across a side: ignored
    [InlineData(Handle.Bottom, Arrow.Down, 100, 100, 740, 461)]
    [InlineData(Handle.Bottom, Arrow.Left, 100, 100, 740, 460)] // across a side: ignored
    public void Ctrl_arrow_moves_the_active_edge_one_pixel(Handle active, Arrow arrow, int left, int top, int right, int bottom) =>
        Assert.Equal(PixelRect.FromEdges(left, top, right, bottom), FrameAdjust.NudgeEdge(Region, active, arrow, Monitor));

    [Fact]
    public void Ctrl_arrow_does_nothing_past_the_monitor_or_below_the_minimum()
    {
        var atRight = PixelRect.FromEdges(1000, 100, 1920, 460);
        Assert.Equal(atRight, FrameAdjust.NudgeEdge(atRight, Handle.Right, Arrow.Right, Monitor));
        var tiny = new PixelRect(100, 100, 32, 32);
        Assert.Equal(tiny, FrameAdjust.NudgeEdge(tiny, Handle.Right, Arrow.Left, Monitor));
    }

    [Fact]
    public void Preset_keeps_the_top_left_when_it_fits() =>
        Assert.Equal(new PixelRect(100, 100, 1280, 720), FrameAdjust.ApplyPreset(Region, new PixelSize(1280, 720), Monitor));

    [Fact]
    public void Preset_moves_up_and_left_just_enough_to_fit() =>
        Assert.Equal(new PixelRect(640, 360, 1280, 720),
            FrameAdjust.ApplyPreset(new PixelRect(1500, 900, 100, 100), new PixelSize(1280, 720), Monitor));

    [Fact]
    public void Preset_larger_than_the_monitor_is_not_available() =>
        Assert.Null(FrameAdjust.ApplyPreset(Region, new PixelSize(1920, 1080), new PixelRect(0, 0, 1366, 768)));

    [Fact]
    public void Full_monitor_uses_the_whole_monitor() =>
        Assert.Equal(new PixelRect(1920, 0, 1367, 767), FrameAdjust.FullMonitor(new PixelRect(1920, 0, 1367, 767)));

    [Fact]
    public void Fit_to_window_keeps_an_odd_size() =>
        Assert.Equal(new PixelRect(10, 20, 301, 201), FrameAdjust.FitToWindow(new PixelRect(10, 20, 301, 201), Monitor));

    [Fact]
    public void Fit_to_window_keeps_only_the_part_on_the_monitor() =>
        Assert.Equal(new PixelRect(0, 0, 500, 400), FrameAdjust.FitToWindow(new PixelRect(-100, -50, 600, 450), Monitor));

    [Fact]
    public void Fit_to_window_rejects_windows_smaller_than_the_minimum() =>
        Assert.Null(FrameAdjust.FitToWindow(new PixelRect(10, 10, 33, 31), Monitor));
}
