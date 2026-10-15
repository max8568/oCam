using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace FrameCam.UI;

/// <summary>
/// A light-gray area that holds a borderless text box or a list (design 21): fully round ends unless a corner
/// radius is set, red-tinted with a red outline when Failed. Bordered draws a thin gray outline instead.
/// </summary>
internal sealed class FieldBox : Panel
{
    private bool _failed;

    public FieldBox()
    {
        DoubleBuffered = true;
        BackColor = Palette.Field;
    }

    /// <summary>Corner radius at 100 % scale; null for fully round ends.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int? CornerRadius { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Bordered { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Failed
    {
        get => _failed;
        set
        {
            _failed = value;
            BackColor = value ? Palette.ErrorField : Palette.Field;
            foreach (var box in Controls.OfType<TextBox>())
                box.BackColor = BackColor;
            Invalidate();
        }
    }

    /// <summary>A round field holding a borderless text box, and maybe a button at its right end.</summary>
    public static FieldBox Around(Rectangle bounds, TextBox box, Control? button = null)
    {
        var field = new FieldBox { Bounds = bounds };
        box.Location = new Point(14, 10);
        box.Width = (button is null ? bounds.Width - 14 : button.Left - 8) - 14;
        field.Controls.Add(box);
        if (button is not null)
            field.Controls.Add(button);
        field.Failed = false;
        return field;
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(Parent?.BackColor ?? Palette.Surface);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.Half;
        var box = new RectangleF(0, 0, Width, Height);
        float radius = CornerRadius is { } r ? LogicalToDeviceUnits(r) : Height / 2f;
        using (var fill = new SolidBrush(BackColor))
        using (var shape = Shapes.Rounded(box, radius))
            g.FillPath(fill, shape);
        if (!_failed && !Bordered)
            return;
        using var pen = new Pen(_failed ? Palette.ErrorBorder : Palette.Border, 1);
        using var outline = Shapes.Rounded(RectangleF.Inflate(box, -0.5f, -0.5f), radius - 0.5f);
        g.DrawPath(pen, outline);
    }
}
