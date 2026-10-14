using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using FrameCam.Native;

namespace FrameCam.UI;

/// <summary>
/// A small white rounded box of text near the control bar: the status message, or a button's name.
/// It never takes focus, clicks go through it, and screen captures leave it out, like the bar.
/// </summary>
internal sealed class BubbleWindow : LayeredWindow
{
    // At 100 % scale.
    private const int ShadowMargin = 6; // see-through edge for the shadow
    private const int MaxTextWidth = 416;
    private const int PadX = 12;
    private const int PadY = 4;

    private static readonly StringFormat TextFormat = new(StringFormat.GenericTypographic) { FormatFlags = StringFormatFlags.MeasureTrailingSpaces };

    private float _scale;
    private Size _boxSize;

    public BubbleWindow()
    {
        ShowInTaskbar = false;
    }

    /// <summary>The text shown; empty while hidden.</summary>
    public string Message { get; private set; } = "";

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            // Always on top through the style: the TopMost property would make Show() focus this window,
            // taking the keyboard from the bar.
            cp.ExStyle |= ClickThrough.ExStyle | NativeMethods.WS_EX_TOPMOST;
            return cp;
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        CaptureExclusion.Apply(Handle);
    }

    protected override void OnDpiChanged(DpiChangedEventArgs e) => e.Cancel = true; // the bar decides the scale

    /// <summary>
    /// Shows the text. <paramref name="place"/> gets the size of the white box and returns where its top-left
    /// corner goes on screen.
    /// </summary>
    public void Display(string text, float scale, Func<Size, Point> place)
    {
        int margin = (int)MathF.Round(ShadowMargin * scale);
        if (text != Message || scale != _scale || !Visible)
        {
            using var picture = Render(text, scale, margin, out _boxSize);
            Message = text;
            _scale = scale;
            var corner = place(_boxSize);
            Location = new Point(corner.X - margin, corner.Y - margin);
            Present(picture);
            if (!Visible)
                Show();
            return;
        }
        var moved = place(_boxSize);
        Location = new Point(moved.X - margin, moved.Y - margin);
    }

    public void Conceal()
    {
        Message = "";
        Hide();
    }

    private static Bitmap Render(string text, float scale, int margin, out Size boxSize)
    {
        using var font = new Font("Microsoft JhengHei UI", 12 * scale, FontStyle.Regular, GraphicsUnit.Pixel);
        SizeF textSize;
        using (var probe = new Bitmap(1, 1))
        using (var measure = Graphics.FromImage(probe))
        {
            measure.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            textSize = measure.MeasureString(text, font, (int)(MaxTextWidth * scale), TextFormat);
        }
        boxSize = new Size((int)MathF.Ceiling(textSize.Width + 2 * PadX * scale), (int)MathF.Ceiling(textSize.Height + 2 * PadY * scale));
        var box = new RectangleF(margin, margin, boxSize.Width, boxSize.Height);
        float radius = Math.Min(box.Height / 2, 14 * scale);

        var image = new Bitmap(boxSize.Width + 2 * margin, boxSize.Height + 2 * margin, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(image);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.Half;
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        Shapes.Shadow(g, box, radius, (int)MathF.Round(4 * scale), scale);
        using (var fill = new SolidBrush(Palette.Surface))
        using (var shape = Shapes.Rounded(box, radius))
            g.FillPath(fill, shape);
        using (var pen = new Pen(Palette.Border, 1))
        using (var outline = Shapes.Rounded(RectangleF.Inflate(box, -0.5f, -0.5f), radius - 0.5f))
            g.DrawPath(pen, outline);
        using (var ink = new SolidBrush(Palette.Ink))
            g.DrawString(text, font, ink, new RectangleF(box.Left + PadX * scale, box.Top + PadY * scale, textSize.Width + 1, textSize.Height + 1), TextFormat);
        return image;
    }
}
