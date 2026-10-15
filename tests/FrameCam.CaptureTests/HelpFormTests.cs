using FrameCam.UI;
using static FrameCam.CaptureTests.ControlTree;

namespace FrameCam.CaptureTests;

/// <summary>The manual opened from the control bar's help button.</summary>
public class HelpFormTests
{
    [Fact]
    public void The_manual_is_longer_than_the_window_and_scrolls_with_the_mouse_wheel() => Sta.Run(async () =>
    {
        using var form = new HelpForm();
        form.Show();
        await Task.Delay(200);
        var view = All<HelpView>(form).Single();

        Assert.True(view.VerticalScroll.Visible);
        Assert.Equal(0, view.VerticalScroll.Value);
        TestNative.WheelDown(view, new Point(view.Width / 2, view.Height / 2));
        Assert.True(view.VerticalScroll.Value > 0);
    });
}
