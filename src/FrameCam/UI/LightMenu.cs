using System.Drawing.Drawing2D;
using FrameCam.Native;

namespace FrameCam.UI;

/// <summary>
/// A pop-up menu in the look of the bar and the dialogs (design 21): white, Windows 11 round corners and a thin
/// border, a light gray round row under the mouse, and gray text for a choice that cannot be picked.
/// </summary>
internal sealed class LightMenu : ContextMenuStrip
{
    public LightMenu()
    {
        Renderer = new LightRenderer(this);
        ShowImageMargin = false;
        ShowCheckMargin = false;
        BackColor = Palette.Surface;
        ForeColor = Palette.Ink;
    }

    protected override void OnItemAdded(ToolStripItemEventArgs e)
    {
        base.OnItemAdded(e);
        // Taller rows; the menu sets the left and right spacing itself and does not count more.
        if (e.Item is ToolStripMenuItem)
            e.Item.Padding = e.Item.Padding with { Top = LogicalToDeviceUnits(6), Bottom = LogicalToDeviceUnits(6) };
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

    private sealed class LightRenderer(LightMenu menu) : ToolStripRenderer
    {
        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e) => e.Graphics.Clear(Palette.Surface);

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            // Windows draws the border along the round corners.
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (!e.Item.Selected || !e.Item.Enabled)
                return;
            float scale = menu.DeviceDpi / 96f;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var bounds = RectangleF.Inflate(new RectangleF(0, 0, e.Item.Width, e.Item.Height), -4 * scale, -1 * scale);
            using var row = Shapes.Rounded(bounds, 6 * scale);
            using var brush = new SolidBrush(Palette.Hover);
            e.Graphics.FillPath(brush, row);
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item.Enabled ? Palette.Ink : Palette.Dots;
            base.OnRenderItemText(e);
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            int inset = menu.LogicalToDeviceUnits(8);
            int y = e.Item.Height / 2;
            using var pen = new Pen(Palette.Border);
            e.Graphics.DrawLine(pen, inset, y, e.Item.Width - inset, y);
        }
    }
}
