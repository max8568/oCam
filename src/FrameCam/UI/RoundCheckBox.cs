using System.Drawing.Drawing2D;

namespace FrameCam.UI;

/// <summary>
/// A check box drawn in the dialogs' look (design 21): a round-cornered square, white with a gray border, filled
/// blue with a white check mark when checked. It is still a CheckBox, so Space, Tab and clicking the text work
/// as usual.
/// </summary>
internal sealed class RoundCheckBox : CheckBox
{
    private const int BoxSize = 18;
    private const int RingRoom = 4; // left of the box, for the focus ring
    private const int TextGap = 8;

    private bool _hover;

    public RoundCheckBox()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hover = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hover = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(Parent?.BackColor ?? BackColor);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.Half;
        float size = LogicalToDeviceUnits(BoxSize);
        var box = new RectangleF(LogicalToDeviceUnits(RingRoom), (Height - size) / 2, size, size);
        using (var shape = Shapes.Rounded(box, size / 4))
        {
            if (Checked)
            {
                using var fill = new SolidBrush(_hover ? Palette.AccentHover : Palette.Accent);
                g.FillPath(fill, shape);
                using var check = new Pen(Color.White, size / 9) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
                g.DrawLines(check, new[]
                {
                    new PointF(box.Left + size * 0.27f, box.Top + size * 0.52f),
                    new PointF(box.Left + size * 0.44f, box.Top + size * 0.68f),
                    new PointF(box.Left + size * 0.74f, box.Top + size * 0.35f),
                });
            }
            else
            {
                using var fill = new SolidBrush(Palette.Surface);
                g.FillPath(fill, shape);
                using var border = new Pen(_hover ? Palette.Dots : Palette.FieldButtonHover, 1.5f);
                g.DrawPath(border, shape);
            }
        }
        if (Focused && ShowFocusCues)
        {
            using var ring = new Pen(Palette.Accent, 1.5f);
            using var outline = Shapes.Rounded(RectangleF.Inflate(box, 3, 3), size / 4 + 3);
            g.DrawPath(ring, outline);
        }
        int textLeft = (int)box.Right + LogicalToDeviceUnits(TextGap);
        TextRenderer.DrawText(g, Text, Font, new Rectangle(textLeft, 0, Width - textLeft, Height), ForeColor,
            TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
    }
}
