using FrameCam.Core;
using FrameCam.Native;

namespace FrameCam.UI;

/// <summary>An orange ring just outside a rectangle; clicks pass through it.</summary>
internal sealed class HighlightWindow : Form
{
    private const int Thickness = 3;

    public HighlightWindow()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        AutoScaleMode = AutoScaleMode.None;
        BackColor = Color.Orange;
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
    }

    protected override void OnDpiChanged(DpiChangedEventArgs e) => e.Cancel = true;

    /// <summary>Outlines the rectangle, or hides when it is null.</summary>
    public void ShowAround(PixelRect? rect)
    {
        if (rect is not { } r)
        {
            Hide();
            return;
        }
        var outer = r.Inflate(Thickness);
        Bounds = outer.ToRectangle();
        var ring = new Region(new Rectangle(0, 0, outer.Width, outer.Height));
        ring.Exclude(new Rectangle(Thickness, Thickness, r.Width, r.Height));
        var old = Region;
        Region = ring;
        old?.Dispose();
        if (!Visible)
            Show();
    }
}
