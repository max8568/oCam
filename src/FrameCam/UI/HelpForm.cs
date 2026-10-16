using System.Drawing.Drawing2D;
using FrameCam.Core;

namespace FrameCam.UI;

/// <summary>The manual (spec 8.4), opened from the bar's help button: how to use each part of FrameCam.</summary>
internal sealed class HelpForm : LightDialog
{
    public HelpForm(AppSettings settings) : base("操作說明", "FrameCam 操作說明", new Size(520, 600))
    {
        SuspendLayout();
        var view = new HelpView(HelpText.Blocks(settings)) { Bounds = new Rectangle(0, 56, 520, 480) };
        var done = new RoundButton { Text = "關閉", DialogResult = DialogResult.Cancel, Bounds = new Rectangle(414, 14, 84, 36) };
        done.SetColors(Palette.Accent, Palette.AccentHover, Palette.AccentPressed, Color.White);
        AcceptButton = done;
        CancelButton = done;

        Controls.AddRange(new Control[] { view, BottomBar(520, 536, done), CloseButton(520, DialogResult.Cancel) });
        ResumeLayout(false);
        PerformLayout();
    }
}

internal enum HelpKind { Heading, Paragraph, Bullet }

/// <summary>One piece of the manual: a heading, a paragraph or a bullet point.</summary>
internal sealed record HelpBlock(HelpKind Kind, string Text);

/// <summary>The manual's text, laid out for the view's width at its display scale; it scrolls with the mouse wheel and the scroll bar.</summary>
internal sealed class HelpView : ScrollableControl
{
    private const int Side = 24;
    private const int BulletIndent = 16;
    private const TextFormatFlags Flags = TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix | TextFormatFlags.TextBoxControl;

    private readonly IReadOnlyList<HelpBlock> _blocks;
    private Rectangle[] _bounds = [];
    private Font _headingFont;

    public HelpView(IReadOnlyList<HelpBlock> blocks)
    {
        _blocks = blocks;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        AutoScroll = true;
        BackColor = Palette.Surface;
        _headingFont = HeadingFont();
    }

    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        _headingFont.Dispose();
        _headingFont = HeadingFont();
        Arrange();
    }

    protected override void OnClientSizeChanged(EventArgs e)
    {
        base.OnClientSizeChanged(e);
        Arrange();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var dot = new SolidBrush(Palette.Dots);
        for (int i = 0; i < _bounds.Length; i++)
        {
            var block = _blocks[i];
            var r = _bounds[i];
            r.Offset(AutoScrollPosition);
            if (!r.IntersectsWith(e.ClipRectangle))
                continue;
            if (block.Kind == HelpKind.Bullet)
            {
                float size = LogicalToDeviceUnits(4);
                float x = r.Left - LogicalToDeviceUnits(BulletIndent) + LogicalToDeviceUnits(3);
                g.FillEllipse(dot, x, r.Top + (Font.Height - size) / 2, size, size);
            }
            var font = block.Kind == HelpKind.Heading ? _headingFont : Font;
            TextRenderer.DrawText(g, block.Text, font, r, block.Kind == HelpKind.Heading ? Palette.Strong : Palette.Ink, Flags);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _headingFont.Dispose();
        base.Dispose(disposing);
    }

    private Font HeadingFont() => new(Font.FontFamily, Font.Size * 7 / 6, FontStyle.Bold, Font.Unit);

    /// <summary>Stacks the blocks from the top for the current width, and sets how far the view scrolls.</summary>
    private void Arrange()
    {
        int side = LogicalToDeviceUnits(Side);
        int indent = LogicalToDeviceUnits(BulletIndent);
        int y = LogicalToDeviceUnits(4);
        var bounds = new Rectangle[_blocks.Count];
        for (int i = 0; i < _blocks.Count; i++)
        {
            var block = _blocks[i];
            if (block.Kind == HelpKind.Heading && i > 0)
                y += LogicalToDeviceUnits(14); // space before each section
            int left = block.Kind == HelpKind.Bullet ? side + indent : side;
            int width = Math.Max(1, ClientSize.Width - side - left);
            var font = block.Kind == HelpKind.Heading ? _headingFont : Font;
            int height = TextRenderer.MeasureText(block.Text, font, new Size(width, int.MaxValue), Flags).Height;
            bounds[i] = new Rectangle(left, y, width, height);
            y += height + LogicalToDeviceUnits(block.Kind == HelpKind.Heading ? 6 : 5);
        }
        _bounds = bounds;
        AutoScrollMinSize = new Size(0, y + LogicalToDeviceUnits(10));
        Invalidate();
    }
}
