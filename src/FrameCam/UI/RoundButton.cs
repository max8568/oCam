using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace FrameCam.UI;

/// <summary>
/// A button drawn as a circle or pill (design 21), with an icon or text. It is still a Button, so Enter,
/// Space, AcceptButton and CancelButton work as usual.
/// </summary>
internal sealed class RoundButton : Button
{
    private bool _hover;
    private bool _down;
    private Color _fill = Palette.Field;
    private Color _hoverFill = Palette.Pressed;
    private Color _downFill = Palette.FieldButtonHover;
    private Color _ink = Palette.Ink;

    public RoundButton()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Glyph? Glyph { get; set; }

    /// <summary>Icon size in pixels at 100 % scale.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int GlyphSize { get; set; } = 16;

    /// <summary>Sets the colors: normal, under the mouse, pressed, and the icon or text.</summary>
    public void SetColors(Color fill, Color hover, Color down, Color ink)
    {
        (_fill, _hoverFill, _downFill, _ink) = (fill, hover, down, ink);
        Invalidate();
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hover = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hover = _down = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        _down = e.Button == MouseButtons.Left;
        Invalidate();
        base.OnMouseDown(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        _down = false;
        Invalidate();
        base.OnMouseUp(e);
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        _hover = _down = false; // a disabled button gets no mouse-leave, so do not keep looking hovered
        Invalidate();
        base.OnEnabledChanged(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(Parent?.BackColor ?? BackColor);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.Half;
        var box = new RectangleF(0, 0, Width, Height);
        using (var back = new SolidBrush(!Enabled ? _fill : _down ? _downFill : _hover ? _hoverFill : _fill))
        using (var shape = Shapes.Rounded(box, Height / 2f))
            g.FillPath(back, shape);
        if (Focused && ShowFocusCues)
        {
            using var ring = new Pen(Palette.Accent, 2);
            using var outline = Shapes.Rounded(RectangleF.Inflate(box, -1, -1), Height / 2f - 1);
            g.DrawPath(ring, outline);
        }
        var ink = Enabled ? _ink : Palette.Dots; // a button that cannot be pressed now is drawn faded
        if (Glyph is { } glyph)
        {
            float size = LogicalToDeviceUnits(GlyphSize);
            Glyphs.Draw(g, glyph, new RectangleF((Width - size) / 2, (Height - size) / 2, size, size), ink);
        }
        else
        {
            TextRenderer.DrawText(g, Text, Font, ClientRectangle, ink,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
        }
    }
}
