using FrameCam.Core;
using FrameCam.Native;

namespace FrameCam.CaptureTests;

/// <summary>Child windows are what Ctrl+click picks from. Shows a window on screen.</summary>
public class WindowEnumeratorTests
{
    [Fact]
    public void Child_windows_are_listed_with_their_screen_bounds() => Sta.Run(async () =>
    {
        using var form = new Form
        {
            FormBorderStyle = FormBorderStyle.None,
            StartPosition = FormStartPosition.Manual,
            Bounds = new Rectangle(200, 200, 600, 400),
        };
        var panel = new Panel { Bounds = new Rectangle(50, 60, 300, 200) };
        form.Controls.Add(panel);
        form.Show();
        await Task.Delay(200);
        var expected = panel.RectangleToScreen(panel.ClientRectangle).ToPixelRect();
        Assert.Contains(expected, WindowEnumerator.ChildRects(form.Handle));
    });
}
