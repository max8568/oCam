using FrameCam.Core;

namespace FrameCam.CaptureTests;

/// <summary>
/// The capture-test pattern, in physical pixels: the region's outermost 1-pixel ring is white, the rest of
/// the region is gray, and an 8-pixel ring just outside the region is black. A capture shifted by one pixel
/// in any direction puts gray or black into its outer ring.
/// </summary>
internal sealed class TestPatternForm : OnTopForm
{
    public const int BlackMargin = 8;
    public static readonly Color Inside = Color.FromArgb(128, 128, 128);

    private readonly PixelRect _region;

    public TestPatternForm(PixelRect region)
    {
        _region = region;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        AutoScaleMode = AutoScaleMode.None;
        BackColor = Color.Black;
        Bounds = BoundsFor(region).ToRectangle();
    }

    public static PixelRect BoundsFor(PixelRect region) => region.Inflate(BlackMargin);

    protected override void OnDpiChanged(DpiChangedEventArgs e) => e.Cancel = true;

    protected override void OnPaint(PaintEventArgs e)
    {
        using var white = new SolidBrush(Color.White);
        using var gray = new SolidBrush(Inside);
        e.Graphics.Clear(Color.Black);
        e.Graphics.FillRectangle(white, BlackMargin, BlackMargin, _region.Width, _region.Height);
        e.Graphics.FillRectangle(gray, BlackMargin + 1, BlackMargin + 1, _region.Width - 2, _region.Height - 2);
    }
}
