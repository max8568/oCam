using FrameCam.UI;

namespace FrameCam.CaptureTests;

/// <summary>The picture the control bar shows. Drawn into a bitmap; nothing is shown on screen.</summary>
public class ControlBarPainterTests
{
    [Fact]
    public void Bar_ends_are_round_so_its_corners_are_see_through()
    {
        var layout = ControlBarLayout.Compute(1f, ControlBarPainter.RecordWidth(1f, "F2"));
        using var image = ControlBarPainter.Render(layout, new BarLook { RecordHint = "F2" }, 1f);

        var bar = layout.Bar;
        Assert.Equal(layout.WindowSize, image.Size);
        Assert.Equal(0, image.GetPixel(0, 0).A);
        Assert.True(image.GetPixel((int)bar.Left + 1, (int)bar.Top + 1).A < 32, "the corner of the bar's box is painted");
        Assert.Equal(255, image.GetPixel((int)(bar.Left + bar.Height / 2), (int)bar.Top + 1).A);
        Assert.Equal(255, image.GetPixel((int)(bar.Left + bar.Width / 2), (int)(bar.Top + bar.Height / 2)).A);
    }

    [Fact]
    public void A_longer_hotkey_makes_the_record_button_wider()
    {
        float none = ControlBarPainter.RecordWidth(1f, "");
        float shortKey = ControlBarPainter.RecordWidth(1f, "F2");
        float longKey = ControlBarPainter.RecordWidth(1f, "Ctrl+Alt+Shift+F11");

        Assert.True(none > 36);
        Assert.True(longKey > shortKey);
        Assert.True(shortKey >= none);
    }
}
