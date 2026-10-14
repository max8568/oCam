using FrameCam.Core;
using FrameCam.UI;

namespace FrameCam.CaptureTests;

/// <summary>The square in the middle of the frame that moves it. Shows windows on screen.</summary>
public class FrameMoverTests
{
    [Fact]
    public void Middle_of_the_frame_takes_clicks_except_while_recording() => Sta.Run(async () =>
    {
        var monitor = Screens.All().First();
        var region = new PixelRect(monitor.Bounds.Left + 301, monitor.Bounds.Top + 203, 640, 360);
        var center = new Point(region.Center.X, region.Center.Y);
        using var frame = new FrameWindow(region, monitor);
        frame.Show();
        await Task.Delay(300);
        Assert.Equal(frame.Handle, TestNative.TopLevelWindowAt(center));

        frame.Locked = true;
        await Task.Delay(100);
        Assert.NotEqual(frame.Handle, TestNative.TopLevelWindowAt(center));

        frame.Locked = false;
        await Task.Delay(100);
        Assert.Equal(frame.Handle, TestNative.TopLevelWindowAt(center));
    });
}
