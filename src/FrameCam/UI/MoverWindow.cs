using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using FrameCam.Core;
using FrameCam.Native;
using FrameHandle = FrameCam.Core.Handle; // "Handle" alone means Control.Handle inside a Form

namespace FrameCam.UI;

/// <summary>
/// The four-way arrow in the middle of the frame (spec 7.1). Only the arrow shows, but the whole square takes
/// the mouse: pressing anywhere in it moves the frame. It is a window of its own because the frame window's
/// shape is either drawn or let through to the program below, never see-through and still clickable.
/// </summary>
internal sealed class MoverWindow : LayeredWindow
{
    private readonly FrameWindow _frame;
    private readonly Color _color;

    public MoverWindow(FrameWindow frame, Color color)
    {
        _frame = frame;
        _color = color;
        Owner = frame;
        ShowInTaskbar = false;
        Cursor = Cursors.SizeAll;
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            // Always on top through the style: the TopMost property would make Show() take the keyboard.
            cp.ExStyle |= NativeMethods.WS_EX_TOOLWINDOW | NativeMethods.WS_EX_TOPMOST;
            return cp;
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        CaptureExclusion.Apply(Handle);
        using var picture = Picture(FrameLayout.MoverSize, _color);
        Present(picture);
    }

    protected override void OnDpiChanged(DpiChangedEventArgs e) => e.Cancel = true; // fixed pixels, like the frame

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData) =>
        _frame.TryHandleArrow(keyData) || base.ProcessCmdKey(ref msg, keyData);

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Left)
            _frame.StartDrag(FrameHandle.Move, magnify: false);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        _frame.DragStep();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        _frame.EndDrag();
    }

    /// <summary>The arrow, smooth-edged with a thin white edge, on a square that is see-through but not empty.</summary>
    private static Bitmap Picture(int size, Color color)
    {
        var image = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(image);
        g.Clear(Color.FromArgb(1, 0, 0, 0)); // too faint to see, but the mouse only passes through fully clear pixels
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.Half;
        using var arrow = Arrow(size / 28f);
        using (var edge = new Pen(Color.White, 3) { LineJoin = LineJoin.Round })
            g.DrawPath(edge, arrow);
        using (var fill = new SolidBrush(color))
            g.FillPath(fill, arrow);
        return image;
    }

    /// <summary>Two 2-pixel bars crossing, with a solid tip at each end, on a 28 × 28 grid.</summary>
    private static GraphicsPath Arrow(float s)
    {
        PointF P(float x, float y) => new(x * s, y * s);
        var path = new GraphicsPath(FillMode.Winding);
        path.AddPolygon(new[] { P(6, 13), P(22, 13), P(22, 15), P(6, 15) });
        path.AddPolygon(new[] { P(13, 6), P(15, 6), P(15, 22), P(13, 22) });
        path.AddPolygon(new[] { P(2, 14), P(7, 10), P(7, 18) });
        path.AddPolygon(new[] { P(26, 14), P(21, 10), P(21, 18) });
        path.AddPolygon(new[] { P(14, 2), P(18, 7), P(10, 7) });
        path.AddPolygon(new[] { P(14, 26), P(10, 21), P(18, 21) });
        return path;
    }
}
