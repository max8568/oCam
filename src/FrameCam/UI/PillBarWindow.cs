using FrameCam.Native;

namespace FrameCam.UI;

/// <summary>
/// The control bar's window (design 21): a pill drawn by <see cref="ControlBarPainter"/>. It finds the button
/// under the mouse, shows the button's name after the mouse rests on it, moves when dragged anywhere that is
/// not a button, and shows the status message in a bubble under the bar.
/// </summary>
internal abstract class PillBarWindow : LayeredWindow
{
    private readonly BubbleWindow _statusBubble = new();
    private readonly BubbleWindow _tipBubble = new();
    private readonly System.Windows.Forms.Timer _tipTimer = new() { Interval = 500 };
    private ControlBarLayout? _layout;
    private BarLook _look = new();
    private string _status = "";

    protected PillBarWindow()
    {
        _statusBubble.Owner = this;
        _tipBubble.Owner = this;
        _tipTimer.Tick += (_, _) =>
        {
            _tipTimer.Stop();
            ShowTip();
        };
    }

    internal ControlBarLayout BarLayout => _layout ?? throw new InvalidOperationException("The bar has no window yet.");
    internal BarLook Look => _look;
    internal BubbleWindow StatusBubble => _statusBubble;

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.Style |= NativeMethods.WS_MINIMIZEBOX; // without a title bar, clicking the taskbar button minimizes it only with this
            // Always on top through the style: the TopMost property set before the window exists does not
            // make it so, and the bar went behind other programs.
            cp.ExStyle |= NativeMethods.WS_EX_TOPMOST;
            return cp;
        }
    }

    private float DisplayScale => DeviceDpi / 96f;

    protected abstract void OnItemClick(BarItem item);

    /// <summary>The name shown when the mouse rests on a button; empty for none.</summary>
    protected abstract string TipFor(BarItem item);

    /// <summary>The message in the bubble under the bar; empty hides the bubble.</summary>
    protected string Status
    {
        get => _status;
        set
        {
            _status = value;
            PlaceStatus();
        }
    }

    protected void SetRecording(bool recording) => Change(_look with { Recording = recording });

    protected void SetPaused(bool paused) => Change(_look with { Paused = paused });

    protected void SetElapsed(string elapsed) => Change(_look with { Elapsed = elapsed });

    protected void SetRecordHint(string hint) => Change(_look with { RecordHint = hint }, relayout: true);

    protected void SetEnabled(BarItem item, bool enabled)
    {
        var disabled = new HashSet<BarItem>(_look.Disabled);
        if (enabled ? disabled.Remove(item) : disabled.Add(item))
            Change(_look with { Disabled = disabled });
    }

    /// <summary>Bottom-left corner of a button, in window coordinates; where its menu opens.</summary>
    protected Point BelowItem(BarItem item) => new((int)BarLayout[item].Left, (int)(BarLayout.Bar.Bottom + 4 * DisplayScale));

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        Redraw(relayout: true, keepRightEnd: false);
    }

    protected override void OnDpiChanged(DpiChangedEventArgs e)
    {
        base.OnDpiChanged(e); // moves the window to where Windows suggests
        Redraw(relayout: true, keepRightEnd: false);
    }

    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);
        PlaceStatus();
    }

    protected override void OnLocationChanged(EventArgs e)
    {
        base.OnLocationChanged(e);
        HideTip();
        PlaceStatus();
    }

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);
        if (m.Msg != NativeMethods.WM_NCHITTEST || m.Result != NativeMethods.HTCLIENT || _layout is null)
            return;
        long lParam = m.LParam;
        var p = PointToClient(new Point((short)(lParam & 0xFFFF), (short)((lParam >> 16) & 0xFFFF)));
        if (_layout.Bar.Contains(p) && _layout.ItemAt(p) is null)
            m.Result = NativeMethods.HTCAPTION; // drag the bar from anywhere that is not a button
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var item = _layout?.ItemAt(e.Location);
        if (item == _look.Hover)
            return;
        Change(_look with { Hover = item });
        HideTip();
        if (item is not null)
            _tipTimer.Start();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        HideTip();
        Change(_look with { Hover = null, Pressed = null });
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        HideTip();
        if (e.Button == MouseButtons.Left && _layout?.ItemAt(e.Location) is { } item && !_look.Disabled.Contains(item))
            Change(_look with { Pressed = item });
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (_look.Pressed is not { } pressed)
            return;
        Change(_look with { Pressed = null });
        if (e.Button == MouseButtons.Left && _layout?.ItemAt(e.Location) == pressed && !_look.Disabled.Contains(pressed))
            OnItemClick(pressed);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _tipTimer.Dispose();
        _tipBubble.Dispose();
        _statusBubble.Dispose();
        base.OnFormClosed(e);
    }

    private void Change(BarLook next, bool relayout = false)
    {
        if (next == _look)
            return;
        _look = next;
        Redraw(relayout, keepRightEnd: relayout);
    }

    /// <param name="keepRightEnd">
    /// When the width changes, keep the bar's right end where it is: the bar starts at the right edge of the
    /// screen, and growing to the right would push the X off it.
    /// </param>
    private void Redraw(bool relayout, bool keepRightEnd = false)
    {
        if (!IsHandleCreated || IsDisposed)
            return;
        float scale = DisplayScale;
        float? oldRight = _layout?.Bar.Right;
        if (relayout || _layout is null)
            _layout = ControlBarLayout.Compute(scale, ControlBarPainter.RecordWidth(scale, _look.RecordHint));
        using var picture = ControlBarPainter.Render(_layout, _look, scale);
        if (keepRightEnd && Visible && oldRight is { } right)
            Present(picture, new Point(Left + (int)MathF.Round(right - _layout.Bar.Right), Top));
        else
            Present(picture);
        if (relayout)
            PlaceStatus();
    }

    private void PlaceStatus()
    {
        if (_status.Length == 0 || !Visible || _layout is null)
        {
            _statusBubble.Conceal();
            return;
        }
        float scale = DisplayScale;
        var corner = new Point(Left + (int)(_layout.Bar.Left + 24 * scale), Top + (int)(_layout.Bar.Bottom + 8 * scale));
        _statusBubble.Display(_status, scale, size => KeepOnScreen(corner, size));
    }

    private void ShowTip()
    {
        if (_look.Hover is not { } item || _layout is null || TipFor(item) is not { Length: > 0 } text)
            return;
        float scale = DisplayScale;
        var button = _layout[item];
        int top = Top + (int)(_layout.Bar.Bottom + 6 * scale);
        if (_statusBubble.Visible)
            top = Math.Max(top, _statusBubble.Bottom);
        int center = Left + (int)(button.Left + button.Width / 2);
        _tipBubble.Display(text, scale, size => KeepOnScreen(new Point(center - size.Width / 2, top), size));
    }

    private void HideTip()
    {
        _tipTimer.Stop();
        if (_tipBubble.Visible)
            _tipBubble.Conceal();
    }

    /// <summary>Moves a box below the bar into the screen's working area; above the bar if there is no room below.</summary>
    private Point KeepOnScreen(Point corner, Size size)
    {
        var area = Screen.FromControl(this).WorkingArea;
        int x = Math.Clamp(corner.X, area.Left, Math.Max(area.Left, area.Right - size.Width));
        int y = corner.Y + size.Height <= area.Bottom
            ? corner.Y
            : Top + (int)_layout!.Bar.Top - (corner.Y - Top - (int)_layout.Bar.Bottom) - size.Height;
        return new Point(x, y);
    }
}
