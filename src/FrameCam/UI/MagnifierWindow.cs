using System.ComponentModel;
using System.Drawing.Imaging;
using FrameCam.Core;
using FrameCam.Native;

namespace FrameCam.UI;

/// <summary>
/// The magnifier (spec 7.3): the screen pixels around the focus point, enlarged in a circle, with pixels outside
/// the region dimmed and the region's edges drawn on the pixel seams. It never takes focus, lets clicks through,
/// and is left out of screen captures, so the pixels it copies never include itself or the frame.
/// </summary>
internal sealed class MagnifierWindow : LayeredWindow
{
    private const int HideDelayMs = 1500; // after the last arrow nudge; a drag hides it as soon as it ends

    private readonly FrameWindow _frame;
    private readonly System.Windows.Forms.Timer _hideTimer = new() { Interval = HideDelayMs };
    private readonly Bitmap _pixels = new(MagnifierMath.SourcePixels, MagnifierMath.SourcePixels, PixelFormat.Format24bppRgb);

    public MagnifierWindow(FrameWindow frame)
    {
        _frame = frame;
        ShowInTaskbar = false;
        _hideTimer.Tick += (_, _) =>
        {
            _hideTimer.Stop();
            Hide();
        };
        frame.Adjusting += (_, e) => ShowFor(e);
        frame.AdjustEnded += (_, _) =>
        {
            _hideTimer.Stop();
            Hide();
        };
        frame.Nudged += (_, _) =>
        {
            _hideTimer.Stop();
            _hideTimer.Start();
        };
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            // Always on top through the style: the TopMost property would make Show() focus this window,
            // so the next arrow press would no longer reach the bar or the frame.
            cp.ExStyle |= ClickThrough.ExStyle | NativeMethods.WS_EX_TOPMOST;
            return cp;
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        CaptureExclusion.Apply(Handle);
    }

    protected override void OnDpiChanged(DpiChangedEventArgs e) => e.Cancel = true;

    private void ShowFor(AdjustingEventArgs e)
    {
        _hideTimer.Stop();
        var monitor = Screens.At(e.Focus);
        var source = MagnifierMath.SourceRect(e.Focus);
        CopyPixels(source);
        using var picture = MagnifierPainter.Render(_pixels, source, _frame.CaptureRegion, MagnifierMath.Zoom(monitor.Scale), monitor.Scale);
        var topLeft = MagnifierMath.Placement(e.Anchor, new PixelSize(picture.Width, picture.Height), monitor.Bounds);
        Present(picture, new Point(topLeft.X, topLeft.Y));
        if (!Visible)
            Show();
    }

    private void CopyPixels(PixelRect source)
    {
        using (var g = Graphics.FromImage(_pixels))
            g.Clear(Color.Black);
        try
        {
            ScreenPixels.Copy(source, _pixels);
        }
        catch (Win32Exception)
        {
            // Part of the view is off every monitor; it stays black.
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _hideTimer.Dispose();
            _pixels.Dispose();
        }
        base.Dispose(disposing);
    }
}
