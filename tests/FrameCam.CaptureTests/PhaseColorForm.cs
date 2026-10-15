using System.ComponentModel;

namespace FrameCam.CaptureTests;

/// <summary>
/// A window filled with one color that a test can change, to tell from the video which part of the recording
/// a frame comes from. A 32 × 32 square in the top-left corner changes on every timer tick, so the screen
/// keeps changing and ScreenRecorderLib keeps writing frames.
/// </summary>
internal sealed class PhaseColorForm : OnTopForm
{
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 1 };
    private Color _phase;
    private int _tick;

    public PhaseColorForm(Rectangle bounds, Color phase)
    {
        _phase = phase;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        AutoScaleMode = AutoScaleMode.None;
        DoubleBuffered = true;
        Bounds = bounds;
        _timer.Tick += (_, _) =>
        {
            _tick++;
            Invalidate(new Rectangle(0, 0, 32, 32));
        };
        _timer.Start();
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color Phase
    {
        get => _phase;
        set
        {
            _phase = value;
            Refresh();
        }
    }

    protected override void OnDpiChanged(DpiChangedEventArgs e) => e.Cancel = true;

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(_phase);
        int v = _tick * 37 % 256;
        using var square = new SolidBrush(Color.FromArgb(v, 255 - v, v * 3 % 256));
        e.Graphics.FillRectangle(square, 0, 0, 32, 32);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _timer.Dispose();
        base.Dispose(disposing);
    }
}
