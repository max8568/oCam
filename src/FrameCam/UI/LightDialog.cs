using FrameCam.Native;

namespace FrameCam.UI;

/// <summary>
/// The look FrameCam's dialogs share (design 21): no title bar (drag the dialog by its top), Windows 11 round
/// corners and a thin border, a bold heading, a round close button at the top right, and a gray bar along the
/// bottom for the closing buttons. Always on top, because the control bar that owns it is; a dialog that is
/// not would open behind it.
/// </summary>
internal abstract class LightDialog : Form
{
    protected const int HeaderHeight = 52;
    protected const int BottomBarHeight = 64;

    private readonly string _heading;

    /// <param name="clientSize">At 100 % scale; the dialog scales with the display like any WinForms form.</param>
    protected LightDialog(string heading, string windowText, Size clientSize)
    {
        _heading = heading;
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        Text = windowText;
        FormBorderStyle = FormBorderStyle.None;
        BackColor = Palette.Surface;
        ForeColor = Palette.Strong;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = clientSize;
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ClassStyle |= NativeMethods.CS_DROPSHADOW;
            return cp;
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        // Windows 11 rounds the corners and draws a thin border; earlier versions ignore both.
        int corners = NativeMethods.DWMWCP_ROUND;
        NativeMethods.DwmSetWindowAttribute(Handle, NativeMethods.DWMWA_WINDOW_CORNER_PREFERENCE, ref corners, sizeof(int));
        int border = ColorTranslator.ToWin32(Palette.Border);
        NativeMethods.DwmSetWindowAttribute(Handle, NativeMethods.DWMWA_BORDER_COLOR, ref border, sizeof(int));
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var title = new Font(Font.FontFamily, Font.Size * 4 / 3, FontStyle.Bold, Font.Unit);
        TextRenderer.DrawText(e.Graphics, _heading, title, new Point(LogicalToDeviceUnits(22), LogicalToDeviceUnits(18)), Palette.Strong);
    }

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);
        if (m.Msg != NativeMethods.WM_NCHITTEST || m.Result != NativeMethods.HTCLIENT)
            return;
        long lParam = m.LParam;
        var p = PointToClient(new Point((short)(lParam & 0xFFFF), (short)((lParam >> 16) & 0xFFFF)));
        if (p.Y < LogicalToDeviceUnits(HeaderHeight))
            m.Result = NativeMethods.HTCAPTION; // drag the dialog by its top
    }

    /// <summary>The round X at the top right of a dialog <paramref name="width"/> wide (at 100 % scale).</summary>
    protected static RoundButton CloseButton(int width, DialogResult result)
    {
        var close = new RoundButton { Glyph = Glyph.Close, GlyphSize = 14, Bounds = new Rectangle(width - 46, 14, 32, 32), DialogResult = result, TabStop = false, AccessibleName = "關閉" };
        close.SetColors(Palette.Field, Palette.Pressed, Palette.FieldButtonHover, Palette.Muted);
        return close;
    }

    /// <summary>
    /// The gray bar along the bottom of a dialog <paramref name="width"/> wide, from <paramref name="top"/> down
    /// (at 100 % scale), with a thin line on top. Buttons in it go 14 from its top and 22 from the right edge.
    /// </summary>
    protected static Panel BottomBar(int width, int top, params Control[] controls)
    {
        var bar = new Panel { Bounds = new Rectangle(0, top, width, BottomBarHeight), BackColor = Palette.Band };
        bar.Paint += (_, e) =>
        {
            using var line = new Pen(Palette.Border, 1);
            e.Graphics.DrawLine(line, 0, 0, bar.Width, 0);
        };
        bar.Controls.AddRange(controls);
        return bar;
    }

    protected static Label Caption(string text, int x, int y) =>
        new() { Text = text, AutoSize = true, Location = new Point(x, y), ForeColor = Palette.Muted };
}
