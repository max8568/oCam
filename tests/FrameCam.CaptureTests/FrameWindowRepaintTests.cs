using System.Runtime.InteropServices;
using FrameCam.Core;
using FrameCam.UI;

namespace FrameCam.CaptureTests;

/// <summary>
/// While the frame is dragged, its outline must stay blue on screen: no moment where a whole side shows white
/// or black. Samples the screen from another thread while the frame changes. Shows windows on screen.
/// </summary>
public class FrameWindowRepaintTests
{
    private static readonly Color Blue = Color.FromArgb(0, 120, 215);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowDisplayAffinity(IntPtr hwnd, uint affinity);

    [Fact]
    public void Outline_stays_blue_while_the_top_left_corner_is_dragged() => Sta.Run(async () =>
    {
        // The bottom and right sides stay put while the top-left corner moves; sample them.
        var (blue, total) = await SampleWhileChanging(
            (start, d) => new PixelRect(start.Left - d, start.Top - d, start.Width + d, start.Height + d),
            start => (new Rectangle(start.Left + 40, start.Bottom, 60, 1), new Rectangle(start.Right, start.Top + 40, 1, 60)));
        Assert.True(blue >= total * 0.99, $"blue in {blue} of {total} samples");
    });

    [Fact]
    public void Outline_stays_blue_while_the_bottom_right_corner_is_dragged() => Sta.Run(async () =>
    {
        // The top and left sides stay put while the bottom-right corner moves; sample them.
        var (blue, total) = await SampleWhileChanging(
            (start, d) => new PixelRect(start.Left, start.Top, start.Width + d, start.Height + d),
            start => (new Rectangle(start.Left + 40, start.Top - 1, 60, 1), new Rectangle(start.Left - 1, start.Top + 40, 1, 60)));
        Assert.True(blue >= total * 0.99, $"blue in {blue} of {total} samples");
    });

    private static async Task<(int Blue, int Total)> SampleWhileChanging(
        Func<PixelRect, int, PixelRect> step, Func<PixelRect, (Rectangle, Rectangle)> stillLines)
    {
        var monitor = Screens.At(new PixelPoint(0, 0)); // the primary monitor
        var start = new PixelRect(monitor.Bounds.Left + 400, monitor.Bounds.Top + 250, 700, 400);
        using var frame = new FrameWindow(start, monitor);
        frame.Show();
        await Task.Delay(300);
        SetWindowDisplayAffinity(frame.Handle, 0); // let the screen capture see the frame, in this test only

        var (a, b) = stillLines(start);
        int blue = 0, total = 0;
        var stop = false;
        var sampler = new Thread(() =>
        {
            using var bmpA = new Bitmap(a.Width, a.Height);
            using var bmpB = new Bitmap(b.Width, b.Height);
            using var ga = Graphics.FromImage(bmpA);
            using var gb = Graphics.FromImage(bmpB);
            while (!Volatile.Read(ref stop))
            {
                ga.CopyFromScreen(a.Location, Point.Empty, a.Size);
                gb.CopyFromScreen(b.Location, Point.Empty, b.Size);
                foreach (var c in Pixels(bmpA).Concat(Pixels(bmpB)))
                {
                    total++;
                    if (c.ToArgb() == Blue.ToArgb())
                        blue++;
                }
            }
        }) { IsBackground = true };
        sampler.Start();

        for (int i = 0; i < 300; i++)
        {
            frame.SetRegion(step(start, (i % 40) * 4), monitor); // grows, then jumps back, like a drag back and forth
            await Task.Delay(4);
        }
        Volatile.Write(ref stop, true);
        sampler.Join();
        return (blue, total);
    }

    private static IEnumerable<Color> Pixels(Bitmap bmp)
    {
        for (int y = 0; y < bmp.Height; y++)
            for (int x = 0; x < bmp.Width; x++)
                yield return bmp.GetPixel(x, y);
    }
}
