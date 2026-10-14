using System.ComponentModel;
using FrameCam.Native;

namespace FrameCam.UI;

/// <summary>
/// A borderless window that shows a picture with see-through parts, so it can have any shape with smooth
/// edges. Clicks on fully see-through pixels go to the window below. It cannot hold ordinary controls.
/// </summary>
internal abstract class LayeredWindow : Form
{
    protected LayeredWindow()
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        AutoScaleMode = AutoScaleMode.None;
        MaximizeBox = false;
        MinimizeBox = false;
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= NativeMethods.WS_EX_LAYERED;
            return cp;
        }
    }

    /// <summary>
    /// Shows the picture as the window; the window takes the picture's size. It stays where it is, or moves
    /// to <paramref name="position"/> in the same step.
    /// </summary>
    protected void Present(Bitmap picture, Point? position = null)
    {
        IntPtr screen = NativeMethods.GetDC(IntPtr.Zero);
        IntPtr memory = NativeMethods.CreateCompatibleDC(screen);
        IntPtr bitmap = picture.GetHbitmap(Color.FromArgb(0));
        IntPtr previous = NativeMethods.SelectObject(memory, bitmap);
        try
        {
            var size = new NativeMethods.SIZE(picture.Width, picture.Height);
            var origin = new NativeMethods.POINT(0, 0);
            var blend = new NativeMethods.BLENDFUNCTION
            {
                BlendOp = NativeMethods.AC_SRC_OVER,
                SourceConstantAlpha = 255,
                AlphaFormat = NativeMethods.AC_SRC_ALPHA,
            };
            bool updated;
            if (position is { } p)
            {
                var target = new NativeMethods.POINT(p.X, p.Y);
                updated = NativeMethods.UpdateLayeredWindow(Handle, screen, ref target, ref size, memory, ref origin, 0, ref blend, NativeMethods.ULW_ALPHA);
            }
            else
            {
                updated = NativeMethods.UpdateLayeredWindow(Handle, screen, IntPtr.Zero, ref size, memory, ref origin, 0, ref blend, NativeMethods.ULW_ALPHA);
            }
            if (!updated)
                throw new Win32Exception();
            UpdateBounds(); // let Left, Top, Size and the Move event follow the window
        }
        finally
        {
            NativeMethods.SelectObject(memory, previous);
            NativeMethods.DeleteObject(bitmap);
            NativeMethods.DeleteDC(memory);
            NativeMethods.ReleaseDC(IntPtr.Zero, screen);
        }
    }
}
