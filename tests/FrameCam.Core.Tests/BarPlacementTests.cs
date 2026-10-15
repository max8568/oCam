using FrameCam.Core;

namespace FrameCam.Core.Tests;

public class BarPlacementTests
{
    private static readonly MonitorInfo Primary = new(@"\\.\DISPLAY2", new PixelRect(0, 0, 1920, 1080), true, 1.0);
    private static readonly MonitorInfo Second = new(@"\\.\DISPLAY1", new PixelRect(1920, 0, 1920, 1080), false, 1.0);
    private static readonly MonitorInfo[] Both = [Primary, Second];
    private static readonly PixelSize Bar = new(420, 48);

    [Fact]
    public void Saved_place_on_an_existing_monitor_is_kept() =>
        Assert.Equal(new PixelPoint(2001, 33), BarPlacement.Restore(new SavedPosition(@"\\.\DISPLAY1", 2001, 33), Bar, Both));

    [Fact]
    public void Missing_monitor_gives_no_place() =>
        Assert.Null(BarPlacement.Restore(new SavedPosition(@"\\.\DISPLAY9", 2001, 33), Bar, Both));

    [Fact]
    public void A_bar_that_would_stick_out_of_its_monitor_gives_no_place() =>
        Assert.Null(BarPlacement.Restore(new SavedPosition(@"\\.\DISPLAY1", 3600, 33), Bar, Both));

    [Fact]
    public void First_run_gives_no_place() =>
        Assert.Null(BarPlacement.Restore(null, Bar, Both));
}
