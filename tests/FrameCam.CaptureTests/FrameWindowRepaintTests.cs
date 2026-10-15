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

    [Fact]
    public void A_dragged_side_is_blue_at_its_new_place_before_any_paint_gets_in() => Sta.Run(async () =>
    {
        // In a real drag the mouse moves keep coming, and Windows paints only when none is waiting. So each
        // step must show the moved side in blue by itself. Here the UI thread stays busy after each step.
        var monitor = Screens.At(new PixelPoint(0, 0)); // the primary monitor
        var start = new PixelRect(monitor.Bounds.Left + 400, monitor.Bounds.Top + 250, 700, 400);
        using var frame = new FrameWindow(start, monitor);
        frame.Show();
        await Task.Delay(300);
        SetWindowDisplayAffinity(frame.Handle, 0); // let the screen capture see the frame, in this test only

        int blue = 0, total = 0;
        var seen = new Dictionary<int, int>();
        using var line = new Bitmap(2, 60);
        using var g = Graphics.FromImage(line);
        for (int i = 1; i <= 30; i++)
        {
            var region = new PixelRect(start.Left - 4 * i, start.Top, start.Width + 4 * i, start.Height);
            frame.SetRegion(region, monitor);
            Thread.Sleep(40); // busy: no paint message gets handled, the screen updates meanwhile
            g.CopyFromScreen(region.Left - 2, region.Top + 40, 0, 0, line.Size);
            foreach (var c in Pixels(line))
            {
                total++;
                if (c.ToArgb() == Blue.ToArgb())
                    blue++;
                seen[c.ToArgb()] = seen.GetValueOrDefault(c.ToArgb()) + 1;
            }
        }
        var colors = string.Join(", ", seen.OrderByDescending(p => p.Value).Take(3).Select(p => $"#{p.Key & 0xFFFFFF:X6} × {p.Value}"));
        Assert.True(blue >= total * 0.99, $"blue in {blue} of {total} samples; most seen {colors}");
    });

    [Fact]
    public void A_side_dragged_quickly_never_flashes_white_or_black() => Sta.Run(async () =>
    {
        // A gray backdrop behind the frame: a sample that is gray only shows the screen has not caught up with
        // the last step yet. White or black is a flash. The UI thread moves the left side every 8 ms, like a
        // mouse, while another thread samples it.
        var monitor = Screens.At(new PixelPoint(0, 0)); // the primary monitor
        var start = new PixelRect(monitor.Bounds.Left + 400, monitor.Bounds.Top + 250, 700, 400);
        using var backdrop = new Form
        {
            FormBorderStyle = FormBorderStyle.None, ShowInTaskbar = false, TopMost = true, StartPosition = FormStartPosition.Manual,
            AutoScaleMode = AutoScaleMode.None, BackColor = Color.FromArgb(128, 128, 128),
            Bounds = Rectangle.FromLTRB(start.Left - 300, start.Top - 20, start.Right + 20, start.Bottom + 20),
        };
        backdrop.Show();
        using var frame = new FrameWindow(start, monitor);
        frame.Show();
        await Task.Delay(300);
        SetWindowDisplayAffinity(frame.Handle, 0); // let the screen capture see the frame, in this test only

        int lineX = start.Left - 2, steps = 0, flashes = 0, blue = 0, total = 0;
        var stop = false;
        var sampler = new Thread(() =>
        {
            using var bmp = new Bitmap(2, 60);
            using var g = Graphics.FromImage(bmp);
            while (!Volatile.Read(ref stop))
            {
                int before = Volatile.Read(ref steps), x = Volatile.Read(ref lineX);
                g.CopyFromScreen(x, start.Top + 40, 0, 0, bmp.Size);
                if (Volatile.Read(ref steps) != before)
                    continue; // the side moved while sampling
                foreach (var c in Pixels(bmp))
                {
                    total++;
                    if (c.ToArgb() == Blue.ToArgb())
                        blue++;
                    else if ((c.R > 230 && c.G > 230 && c.B > 230) || (c.R < 25 && c.G < 25 && c.B < 25))
                        flashes++;
                }
            }
        }) { IsBackground = true };
        sampler.Start();

        for (int i = 0; i < 400; i++)
        {
            int d = 4 * (i % 60);
            var region = new PixelRect(start.Left - d, start.Top, start.Width + d, start.Height);
            frame.SetRegion(region, monitor);
            Volatile.Write(ref lineX, region.Left - 2);
            Interlocked.Increment(ref steps);
            Thread.Sleep(8); // busy like a drag: the next mouse move comes before any waiting paint
        }
        Volatile.Write(ref stop, true);
        sampler.Join();
        Assert.True(blue > total / 2 && flashes == 0, $"of {total} samples, blue {blue}, white or black {flashes}");
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
