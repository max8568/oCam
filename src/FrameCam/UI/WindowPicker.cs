using FrameCam.Core;
using FrameCam.Native;

namespace FrameCam.UI;

/// <summary>
/// "選視窗" mode (spec 7.5): a nearly transparent window over the whole desktop takes the mouse; what a click
/// would pick gets an orange outline. Click picks the whole window under the cursor; with Ctrl held, the smallest
/// part of it under the cursor (client area or child window, such as a browser's page area). Esc cancels.
/// </summary>
internal sealed class WindowPicker : Form
{
    private readonly List<WindowInfo> _windows = WindowEnumerator.VisibleWindows();
    private readonly HighlightWindow _highlight = new();
    private readonly Dictionary<IntPtr, List<PixelRect>> _parts = [];

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

    /// <summary>The picked window (click) or part of it (Ctrl+click), when the dialog result is OK.</summary>
    public PixelRect? Picked { get; private set; }

    /// <summary>Where the click happened; its monitor is the one the region goes on.</summary>
    public PixelPoint PickedAt { get; private set; }

    protected override void OnDpiChanged(DpiChangedEventArgs e) => e.Cancel = true;

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        Activate();
        _highlight.ShowAround(Target());
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        _highlight.ShowAround(Target());
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Button != MouseButtons.Left || Target() is not { } target)
            return;
        Picked = target;
        PickedAt = MousePosition.ToPixelPoint();
        DialogResult = DialogResult.OK;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode == Keys.Escape)
            DialogResult = DialogResult.Cancel;
        else if (e.KeyCode == Keys.ControlKey)
            _highlight.ShowAround(Target());
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        if (e.KeyCode == Keys.ControlKey)
            _highlight.ShowAround(Target());
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _highlight.Dispose();
        base.OnFormClosed(e);
    }

    /// <summary>What a click picks now; null when the cursor is over no window.</summary>
    private PixelRect? Target()
    {
        var p = MousePosition.ToPixelPoint();
        if (_windows.FirstOrDefault(w => w.Outer.Contains(p)) is not { } window)
            return null;
        if ((ModifierKeys & Keys.Control) == 0)
            return window.Outer;
        if (!_parts.TryGetValue(window.Handle, out var parts))
            _parts[window.Handle] = parts = [window.Client, .. WindowEnumerator.ChildRects(window.Handle)];
        return WindowParts.Under(p, window.Outer, parts);
    }
}
