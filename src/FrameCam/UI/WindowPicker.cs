using FrameCam.Core;
using FrameCam.Native;

namespace FrameCam.UI;

/// <summary>
/// "選視窗" mode (spec 7.5): a nearly transparent window over the whole desktop takes the mouse; the window
/// under the cursor gets an orange outline. Click picks the whole window, Ctrl+click its client area, Esc cancels.
/// </summary>
internal sealed class WindowPicker : Form
{
    private readonly List<WindowInfo> _windows = WindowEnumerator.VisibleWindows();
    private readonly HighlightWindow _highlight = new();

    public WindowPicker()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        AutoScaleMode = AutoScaleMode.None;
        BackColor = Color.Black;
        Opacity = 0.01; // practically invisible, but still receives the mouse
        KeyPreview = true;
        Cursor = Cursors.Cross;
        Bounds = SystemInformation.VirtualScreen;
    }

    /// <summary>The picked window's outer bounds (click) or client area (Ctrl+click), when the dialog result is OK.</summary>
    public PixelRect? Picked { get; private set; }

    /// <summary>Where the click happened; its monitor is the one the region goes on.</summary>
    public PixelPoint PickedAt { get; private set; }

    protected override void OnDpiChanged(DpiChangedEventArgs e) => e.Cancel = true;

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        Activate();
        _highlight.ShowAround(WindowUnderCursor()?.Outer);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        _highlight.ShowAround(WindowUnderCursor()?.Outer);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Button != MouseButtons.Left || WindowUnderCursor() is not { } window)
            return;
        Picked = (ModifierKeys & Keys.Control) != 0 ? window.Client : window.Outer;
        PickedAt = MousePosition.ToPixelPoint();
        DialogResult = DialogResult.OK;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode == Keys.Escape)
            DialogResult = DialogResult.Cancel;
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _highlight.Dispose();
        base.OnFormClosed(e);
    }

    private WindowInfo? WindowUnderCursor()
    {
        var p = MousePosition.ToPixelPoint();
        return _windows.FirstOrDefault(w => w.Outer.Contains(p));
    }
}
