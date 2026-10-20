using FrameCam.Core;

namespace FrameCam.Core.Tests;

public class RegionPlacementTests
{
    private static readonly MonitorInfo Primary = new(@"\\.\DISPLAY2", new PixelRect(0, 0, 1920, 1080), true, 1.0);
    private static readonly MonitorInfo Second = new(@"\\.\DISPLAY1", new PixelRect(1920, 0, 1920, 1080), false, 1.0);
    private static readonly MonitorInfo[] Both = [Primary, Second];

    [Fact]
    public void Saved_region_on_an_existing_monitor_is_kept() =>
        Assert.Equal((new PixelRect(2001, 33, 640, 360), Second),
            RegionPlacement.Restore(new SavedRegion(@"\\.\DISPLAY1", 2001, 33, 640, 360), Both));

    [Fact]
    public void Missing_monitor_centers_the_saved_size_on_the_primary() =>
        Assert.Equal((new PixelRect(640, 360, 640, 360), Primary),
            RegionPlacement.Restore(new SavedRegion(@"\\.\DISPLAY9", 2001, 33, 640, 360), Both));

    [Fact]
    public void Region_no_longer_inside_its_monitor_is_centered_on_the_primary() =>
        Assert.Equal((new PixelRect(640, 360, 640, 360), Primary),
            RegionPlacement.Restore(new SavedRegion(@"\\.\DISPLAY1", 3800, 33, 640, 360), Both));

    [Fact]
    public void First_run_uses_1280x720_centered_on_the_primary() =>
        Assert.Equal((new PixelRect(320, 180, 1280, 720), Primary), RegionPlacement.Restore(null, Both));

    [Fact]
    public void Default_size_shrinks_to_a_small_primary()
    {
        var small = Primary with { Bounds = new PixelRect(0, 0, 1024, 600) };
        Assert.Equal((new PixelRect(0, 0, 1024, 600), small), RegionPlacement.Restore(null, [small]));
    }

    [Fact]
    public void Odd_saved_size_is_kept() =>
        Assert.Equal(new PixelRect(100, 100, 641, 361),
            RegionPlacement.Restore(new SavedRegion(@"\\.\DISPLAY2", 100, 100, 641, 361), Both).Region);
}
