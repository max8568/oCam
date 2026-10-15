using System.Runtime.InteropServices;
using FrameCam.Core;
using FrameCam.UI;

namespace FrameCam.CaptureTests;

/// <summary>
/// The frame must be drawn exactly where FrameLayout says, in physical pixels, on every monitor. WinForms
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
            // The window covers the monitor but one row, and does not move while dragged; its shape is the drawn frame.
            Assert.Equal(PixelRect.FromEdges(monitor.Bounds.Left, monitor.Bounds.Top, monitor.Bounds.Right, monitor.Bounds.Bottom - 1), frame.Bounds.ToPixelRect());
            using var g = frame.CreateGraphics();
            var shape = Rectangle.Round(frame.Region!.GetBounds(g));
            shape.Offset(frame.Location);
            Assert.Equal(frame.CurrentLayout.WindowBounds, shape.ToPixelRect());
        }
    });

    [Fact]
    public void A_frame_at_the_bottom_of_the_monitor_keeps_its_bottom_line() => Sta.Run(async () =>
    {
        var monitor = Screens.At(new PixelPoint(0, 0));
        var region = new PixelRect(monitor.Bounds.Left + 300, monitor.Bounds.Bottom - 402, 640, 400); // the line is on the last two rows
        using var frame = new FrameWindow(region, monitor);
        frame.Show();
        await Task.Delay(300);

        var bottomLine = PixelRect.FromEdges(region.Left, region.Bottom, region.Right, region.Bottom + 2);
        Assert.True(frame.Bounds.ToPixelRect().Contains(bottomLine), $"window {frame.Bounds} cuts off {bottomLine}");
    });

    [DllImport("shell32.dll")]
    private static extern int SHQueryUserNotificationState(out int state);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Windows_does_not_take_the_frame_for_a_full_screen_app(bool atTheBottom) => Sta.Run(async () =>
    {
        // A window over a whole monitor counts as a full-screen app: notifications go quiet, the taskbar drops behind.
        var monitor = Screens.At(new PixelPoint(0, 0));
        SHQueryUserNotificationState(out int before);
        var region = atTheBottom
            ? new PixelRect(monitor.Bounds.Left + 300, monitor.Bounds.Bottom - 402, 640, 400)
            : new PixelRect(monitor.Bounds.Left + 300, monitor.Bounds.Top + 200, 640, 400);
        using var frame = new FrameWindow(region, monitor);
        frame.Show();
        await Task.Delay(500);

        SHQueryUserNotificationState(out int shown);
        Assert.Equal(before, shown);
    });

    [Fact]
    public void The_frame_is_on_top_even_when_FrameCam_has_no_window_in_front() => Sta.Run(async () =>
    {
        var monitor = Screens.At(new PixelPoint(0, 0));
        using var frame = new FrameWindow(new PixelRect(monitor.Bounds.Left + 300, monitor.Bounds.Top + 200, 640, 400), monitor);
        frame.Show(); // shows without taking the keyboard, so nothing of FrameCam is the active window
        await Task.Delay(300);

        Assert.True(TestNative.IsTopmost(frame.Handle));
    });
}
