using FrameCam.Core;
using FrameCam.UI;

namespace FrameCam.CaptureTests;

/// <summary>
/// The frame window must sit exactly where FrameLayout says, in physical pixels, on every monitor. WinForms
/// DPI scaling would move or resize it on monitors with scaling other than 100%. Shows windows on screen.
/// </summary>
public class FrameWindowPlacementTests
{
    [Fact]
    public void Frame_window_bounds_match_the_layout_on_every_monitor() => Sta.Run(async () =>
    {
        foreach (var monitor in Screens.All())
        {
            var region = new PixelRect(monitor.Bounds.Left + 301, monitor.Bounds.Top + 203, 640, 360);
            using var frame = new FrameWindow(region, monitor);
            frame.Show();
            await Task.Delay(300);
            Assert.Equal(region, frame.CaptureRegion);
            Assert.Equal(frame.CurrentLayout.WindowBounds, frame.Bounds.ToPixelRect());
        }
    });
}
