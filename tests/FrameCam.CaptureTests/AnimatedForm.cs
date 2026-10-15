namespace FrameCam.CaptureTests;

/// <summary>A window that repaints in a new color on every timer tick, so the screen changes on about every refresh.</summary>
internal sealed class AnimatedForm : OnTopForm
{
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 1 };
    private int _tick;

    public AnimatedForm(Rectangle bounds)
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        AutoScaleMode = AutoScaleMode.None;
        DoubleBuffered = true;
        Bounds = bounds;
        _timer.Tick += (_, _) =>
        {
            _tick++;
            Invalidate();
        };
        _timer.Start();
    }

    protected override void OnDpiChanged(DpiChangedEventArgs e) => e.Cancel = true;

    protected override void OnPaint(PaintEventArgs e)
    {
        int v = _tick * 37 % 256;
        e.Graphics.Clear(Color.FromArgb(v, 255 - v, v * 3 % 256));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _timer.Dispose();
        base.Dispose(disposing);
    }
}
