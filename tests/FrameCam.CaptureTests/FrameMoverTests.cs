using FrameCam.Core;
using FrameCam.UI;

namespace FrameCam.CaptureTests;

/// <summary>The four-way arrow in the middle of the frame that moves it. Shows windows on screen.</summary>
public class FrameMoverTests
{
    [Fact]
    public void The_whole_square_in_the_middle_takes_clicks_except_while_recording() => Sta.Run(async () =>
    {
        var monitor = Screens.All().First();
        var region = new PixelRect(monitor.Bounds.Left + 301, monitor.Bounds.Top + 203, 640, 360);
        var center = new Point(region.Center.X, region.Center.Y);
        using var frame = new FrameWindow(region, monitor);
        frame.Show();
        await Task.Delay(300);
        var mover = MoverOf(frame);
        var square = frame.CurrentLayout.Mover!.Value;
        Assert.Equal(mover.Handle, TestNative.TopLevelWindowAt(new Point(square.Left, square.Top)));
        Assert.Equal(mover.Handle, TestNative.TopLevelWindowAt(new Point(square.Right - 1, square.Bottom - 1)));
        Assert.Equal(mover.Handle, TestNative.TopLevelWindowAt(new Point(square.Left + 4, square.Top + 4))); // between the arms

        frame.Locked = true;
        await Task.Delay(100);
        Assert.False(mover.Visible);
        Assert.NotEqual(mover.Handle, TestNative.TopLevelWindowAt(center));
        Assert.NotEqual(frame.Handle, TestNative.TopLevelWindowAt(center));

        frame.Locked = false;
        await Task.Delay(100);
        Assert.Equal(mover.Handle, TestNative.TopLevelWindowAt(center));
    });

    [Fact]
    public void Square_follows_the_frame() => Sta.Run(async () =>
    {
        var monitor = Screens.All().First();
        var region = new PixelRect(monitor.Bounds.Left + 301, monitor.Bounds.Top + 203, 640, 360);
        using var frame = new FrameWindow(region, monitor);
        frame.Show();
        await Task.Delay(300);

        frame.SetRegion(new PixelRect(region.Left + 50, region.Top + 40, 400, 300), monitor);
        await Task.Delay(100);

        Assert.Equal(frame.CurrentLayout.Mover!.Value.ToRectangle(), MoverOf(frame).Bounds);
    });

    [Fact]
    public void Pressing_the_square_starts_a_drag_without_the_magnifier() => Sta.Run(async () =>
    {
        var monitor = Screens.All().First();
        var region = new PixelRect(monitor.Bounds.Left + 301, monitor.Bounds.Top + 203, 640, 360);
        using var frame = new FrameWindow(region, monitor);
        frame.Show();
        await Task.Delay(300);
        int adjusting = 0, ended = 0;
        frame.Adjusting += (_, _) => adjusting++;
        frame.AdjustEnded += (_, _) => ended++;

        TestNative.Click(MoverOf(frame).Handle, new Point(5, 5));

        Assert.Equal(0, adjusting); // the magnifier shows on Adjusting
        Assert.Equal(1, ended);     // only raised when a drag was going on
    });

    private static MoverWindow MoverOf(FrameWindow frame) =>
        Application.OpenForms.OfType<MoverWindow>().Single(m => m.Owner == frame);
}
