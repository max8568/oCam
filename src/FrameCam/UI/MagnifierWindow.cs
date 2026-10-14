using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using FrameCam.Core;
using FrameCam.Native;

namespace FrameCam.UI;

/// <summary>
/// The magnifier (spec 7.3): the screen pixels around the focus point, enlarged, with pixels outside the
/// region dimmed and the region's edges drawn on the pixel seams. It never takes focus, lets clicks through,
/// and is left out of screen captures, so the pixels it copies never include itself or the frame.
/// </summary>
internal sealed class MagnifierWindow : Form
{
    private const int InfoHeight = 40; // at 100% scaling
    private const int HideDelayMs = 1500;

    private readonly FrameWindow _frame;
    private readonly System.Windows.Forms.Timer _hideTimer = new() { Interval = HideDelayMs };
    private readonly Bitmap _pixels = new(MagnifierMath.SourcePixels, MagnifierMath.SourcePixels, PixelFormat.Format24bppRgb);
    private PixelRect _source;
    private int _zoom = MagnifierMath.BaseZoom;
    private double _scale;
    private Font _font = new("Segoe UI", 12f, FontStyle.Bold, GraphicsUnit.Pixel);

    public MagnifierWindow(FrameWindow frame)
    {
        _frame = frame;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        AutoScaleMode = AutoScaleMode.None;
        DoubleBuffered = true;
        BackColor = Color.Black;
        _hideTimer.Tick += (_, _) =>
        {
            _hideTimer.Stop();
            Hide();
        };
        frame.Adjusting += (_, e) => ShowFor(e);
        frame.AdjustEnded += (_, _) =>
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
            cp.ExStyle |= ClickThrough.ExStyle;
            return cp;
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ClickThrough.MakeOpaque(Handle);
        CaptureExclusion.Apply(Handle);
    }

    protected override void OnDpiChanged(DpiChangedEventArgs e) => e.Cancel = true;

    private void ShowFor(AdjustingEventArgs e)
    {
        _hideTimer.Stop();
        var monitor = Screens.At(e.Focus);
        if (monitor.Scale != _scale)
        {
            _scale = monitor.Scale;
            _zoom = MagnifierMath.Zoom(_scale);
            _font.Dispose();
            _font = new Font("Segoe UI", (float)(12 * _scale), FontStyle.Bold, GraphicsUnit.Pixel);
        }
        _source = MagnifierMath.SourceRect(e.Focus);
        CopyPixels();
        int side = MagnifierMath.SourcePixels * _zoom;
        var size = new PixelSize(side, side + (int)Math.Round(InfoHeight * _scale));
        var topLeft = MagnifierMath.Placement(e.Anchor, size, monitor.Bounds);
        Bounds = new Rectangle(topLeft.X, topLeft.Y, size.Width, size.Height);
        if (!Visible)
            Show();
        Invalidate();
    }

    private void CopyPixels()
    {
        using (var g = Graphics.FromImage(_pixels))
            g.Clear(Color.Black);
        try
        {
            ScreenPixels.Copy(_source, _pixels);
        }
        catch (Win32Exception)
        {
            // Part of the view is off every monitor; it stays black.
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        var region = _frame.CaptureRegion;
        int n = MagnifierMath.SourcePixels, z = _zoom, side = n * z;

        g.InterpolationMode = InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode = PixelOffsetMode.Half;
        g.DrawImage(_pixels, new Rectangle(0, 0, side, side));
        g.PixelOffsetMode = PixelOffsetMode.Default;

        using var dim = new SolidBrush(Color.FromArgb(150, 0, 0, 0));
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
                if (!region.Contains(new PixelPoint(_source.Left + x, _source.Top + y)))
                    g.FillRectangle(dim, x * z, y * z, z, z);

        using var grid = new Pen(Color.FromArgb(50, 255, 255, 255));
        for (int k = 1; k < n; k++)
        {
            g.DrawLine(grid, k * z, 0, k * z, side);
            g.DrawLine(grid, 0, k * z, side, k * z);
        }

        using var edge = new Pen(Color.FromArgb(0, 200, 255), Math.Max(2, z / 4));
        foreach (var line in MagnifierMath.BoundaryLines(_source, region))
        {
            if (line.Vertical)
                g.DrawLine(edge, line.Offset * z, line.From * z, line.Offset * z, line.To * z);
            else
                g.DrawLine(edge, line.From * z, line.Offset * z, line.To * z, line.Offset * z);
        }

        int infoHeight = Height - side;
        const TextFormatFlags center = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter;
        TextRenderer.DrawText(g, $"{region.Width} × {region.Height}", _font,
            new Rectangle(0, side, Width, infoHeight / 2), Color.White, center);
        TextRenderer.DrawText(g, $"({region.Left}, {region.Top})", _font,
            new Rectangle(0, side + infoHeight / 2, Width, infoHeight - infoHeight / 2), Color.White, center);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _hideTimer.Dispose();
            _pixels.Dispose();
            _font.Dispose();
        }
        base.Dispose(disposing);
    }
}
