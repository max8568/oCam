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

    private static readonly CustomSize Live = new("直播", 800, 600);
    private static readonly CustomSize Lesson = new("教學", 800, 600); // same size, its own place
    private static readonly CustomSize[] Customs = [Live, Lesson];

    private static CustomSizePlace Place(CustomSize size, string device, int left, int top) => new(size, new SavedPosition(device, left, top));

    [Fact]
    public void Each_custom_size_goes_back_to_its_own_place()
    {
        CustomSizePlace[] places = [Place(Live, @"\\.\DISPLAY2", 5, 5), Place(Lesson, @"\\.\DISPLAY1", 2001, 33)];
        Assert.Equal((new PixelRect(5, 5, 800, 600), Primary), RegionPlacement.LastPlace(Live, places, Both));
        Assert.Equal((new PixelRect(2001, 33, 800, 600), Second), RegionPlacement.LastPlace(Lesson, places, Both));
    }

    [Fact]
    public void Custom_size_never_recorded_has_no_place() =>
        Assert.Null(RegionPlacement.LastPlace(Lesson, [Place(Live, @"\\.\DISPLAY2", 5, 5)], Both));

    [Fact]
    public void Place_on_a_missing_monitor_is_not_used() =>
        Assert.Null(RegionPlacement.LastPlace(Live, [Place(Live, @"\\.\DISPLAY9", 5, 5)], Both));

    [Fact]
    public void Place_no_longer_inside_its_monitor_is_not_used() =>
        Assert.Null(RegionPlacement.LastPlace(Live, [Place(Live, @"\\.\DISPLAY1", 3500, 5)], Both));

    [Fact]
    public void Remembering_replaces_only_that_custom_sizes_place()
    {
        var lesson = Place(Lesson, @"\\.\DISPLAY2", 5, 5);
        var recorded = Place(Live, @"\\.\DISPLAY1", 2001, 33);
        Assert.Equal([lesson, recorded],
            RegionPlacement.Remember([Place(Live, @"\\.\DISPLAY2", 0, 0), lesson], recorded, Customs));
    }

    [Fact]
    public void Places_of_deleted_or_changed_custom_sizes_are_dropped() =>
        Assert.Empty(RegionPlacement.Remember([Place(new CustomSize("舊", 1024, 768), @"\\.\DISPLAY2", 5, 5)],
            Place(new CustomSize("直播", 640, 360), @"\\.\DISPLAY2", 0, 0), Customs));
}
