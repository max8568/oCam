using FrameCam.Core;
using FrameCam.UI;

namespace FrameCam.CaptureTests;

/// <summary>When the magnifier shows and hides (spec 7.3). Shows windows on screen.</summary>
public class MagnifierWindowTests
{
    [Fact]
    public void The_magnifier_hides_as_soon_as_a_drag_ends() => Sta.Run(async () =>
    {
        using var frame = NewFrame();
        using var magnifier = new MagnifierWindow(frame);
        frame.Show();
        await Task.Delay(300);

        frame.StartDrag(Handle.Right);
        await Task.Delay(100);
        Assert.True(magnifier.Visible);

        frame.EndDrag();
        await Task.Delay(100);
        Assert.False(magnifier.Visible);
    });

    [Fact]
    public void The_magnifier_stays_a_moment_after_an_arrow_nudge() => Sta.Run(async () =>
    {
        using var frame = NewFrame();
        using var magnifier = new MagnifierWindow(frame);
        frame.Show();
        await Task.Delay(300);

        frame.TryHandleArrow(Keys.Right);
        await Task.Delay(1000);
        Assert.True(magnifier.Visible); // it hides 1.5 seconds after the last nudge
        await Task.Delay(1000);
        Assert.False(magnifier.Visible);
    });

    private static FrameWindow NewFrame()
    {
        var monitor = Screens.All().First();
        return new FrameWindow(new PixelRect(monitor.Bounds.Left + 301, monitor.Bounds.Top + 203, 640, 360), monitor);
    }
}
