using FrameCam.Core;
using FrameCam.UI;
using static FrameCam.CaptureTests.ControlTree;

namespace FrameCam.CaptureTests;

/// <summary>The main buttons of the settings and custom sizes dialogs: blue, darker under the mouse and when pressed.</summary>
public class DialogButtonColorTests
{
    private static readonly Color Normal = Color.FromArgb(0x25, 0x63, 0xEB);
    private static readonly Color Hover = Color.FromArgb(0x20, 0x55, 0xCA);
    private static readonly Color Pressed = Color.FromArgb(0x1B, 0x47, 0xA9);

    [Theory]
    [InlineData("settings", "確定")]
    [InlineData("settings", "60")] // the frame rate that is picked
    [InlineData("sizes", "新增")]
    [InlineData("sizes", "完成")]
    public void Main_buttons_are_blue_and_darker_under_the_mouse_and_when_pressed(string dialog, string text) => Sta.Run(async () =>
    {
        using Form form = dialog == "settings"
            ? new SettingsForm(new AppSettings { FramesPerSecond = 60 }, false, false)
            : new CustomSizesForm([], new PixelSize(1280, 720));
        form.Show();
        await Task.Delay(200);
        var button = All<Button>(form).Single(b => b.Text == text);
        var inside = new Point(6, button.Height / 2); // in the fill at the round left end: clear of the text and the focus ring

        Assert.Equal(Normal.ToArgb(), FillAt(button, inside).ToArgb());
        TestNative.MouseMove(button.Handle, inside);
        Assert.Equal(Hover.ToArgb(), FillAt(button, inside).ToArgb());
        TestNative.MouseDown(button.Handle, inside);
        Assert.Equal(Pressed.ToArgb(), FillAt(button, inside).ToArgb());
    });

    private static Color FillAt(Control button, Point p)
    {
        using var bmp = new Bitmap(button.Width, button.Height);
        button.DrawToBitmap(bmp, new Rectangle(Point.Empty, button.Size));
        return bmp.GetPixel(p.X, p.Y);
    }
}
