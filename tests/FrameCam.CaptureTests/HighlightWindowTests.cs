using FrameCam.Core;
using FrameCam.UI;

namespace FrameCam.CaptureTests;

/// <summary>The orange outline shown while picking a window. Shows windows on screen.</summary>
public class HighlightWindowTests
{
    [Fact]
    public void Showing_the_outline_does_not_take_the_keyboard() => Sta.Run(async () =>
    {
        var monitor = Screens.All().First();
        using var picker = new Form // stands in for the window picker, which is active when the outline first shows
        {
            StartPosition = FormStartPosition.Manual,
            Bounds = new Rectangle(monitor.Bounds.Left + 100, monitor.Bounds.Top + 100, 200, 100),
            ShowInTaskbar = false,
        };
        picker.Show();
        picker.Activate();
        await Task.Delay(200);
        using var highlight = new HighlightWindow();

        highlight.ShowAround(new PixelRect(monitor.Bounds.Left + 300, monitor.Bounds.Top + 200, 400, 300));
        await Task.Delay(100);

        Assert.True(highlight.Visible);
        Assert.NotSame(highlight, Form.ActiveForm); // otherwise Esc and Ctrl no longer reach the window picker
    });
}
