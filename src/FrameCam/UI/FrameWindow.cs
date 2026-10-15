using System.ComponentModel;
using FrameCam.Core;
using FrameCam.Native;
using FrameHandle = FrameCam.Core.Handle; // "Handle" alone means Control.Handle inside a Form

namespace FrameCam.UI;

internal sealed class AdjustingEventArgs(PixelPoint focus, PixelPoint anchor) : EventArgs
{
    /// <summary>The screen pixel the magnifier centers on.</summary>
    public PixelPoint Focus { get; } = focus;

    /// <summary>The point the magnifier sits next to: the cursor, or the focus for keyboard nudges.</summary>
    public PixelPoint Anchor { get; } = anchor;
}

/// <summary>
/// The recording frame on screen (spec 7.1). The window's shape is only the border ring, the 8 handles and
/// the size label, all outside the region, so the region's pixels stay visible and clickable. The four-way
/// arrow in the middle of the region is a window of its own (<see cref="MoverWindow"/>), hidden while
/// recording. The window is left out of every screen capture.
/// </summary>
internal sealed class FrameWindow : Form
{
    private static readonly Color IdleColor = Color.FromArgb(0, 120, 215);
    private static readonly Color RecordingColor = Color.FromArgb(220, 40, 40);
    private static readonly Font LabelFont = new("Segoe UI", 12f, FontStyle.Bold, GraphicsUnit.Pixel);

    private PixelRect _region;
    private MonitorInfo _monitor;
    private FrameLayout _layout;
    private bool _locked;
    private MoverWindow? _mover;

    private FrameHandle? _dragHandle;
    private bool _dragMagnifies;
    private PixelPoint _dragStartCursor;
    private PixelRect _dragStartRegion;
    private Point _origin; // the window's top-left on screen
    private SnapTargets _dragSnap = SnapTargets.None;

    public FrameWindow(PixelRect region, MonitorInfo monitor)
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        AutoScaleMode = AutoScaleMode.None;
        DoubleBuffered = true;
        BackColor = IdleColor;
        _region = region;
        _monitor = monitor;
        _layout = CreateLayout();
        ApplyLayout();
    }

    /// <summary>Raised on every drag step and keyboard nudge; the magnifier shows for it. Not raised when dragging by the arrow in the middle.</summary>
    public event EventHandler<AdjustingEventArgs>? Adjusting;

    /// <summary>Raised when a drag ends, and right after each keyboard nudge.</summary>
    public event EventHandler? AdjustEnded;

    /// <summary>Raised when Windows reports a DPI change for this window: scaling changed, or it moved to a monitor with another scale.</summary>
    public event EventHandler? ScaleChanged;

    public PixelRect CaptureRegion => _region;

    public MonitorInfo Monitor => _monitor;

    internal FrameLayout CurrentLayout => _layout;

    /// <summary>The edge or corner Ctrl+arrow moves: the last one dragged, bottom-right until then.</summary>
    public FrameHandle ActiveHandle { get; private set; } = FrameHandle.BottomRight;

    /// <summary>Gives the snap targets on a monitor when an edge drag starts; null means no snapping.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Func<PixelRect, SnapTargets>? SnapSource { get; set; }

    /// <summary>While locked (recording) the frame is red and ignores the mouse and keyboard.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Locked
    {
        get => _locked;
        set
        {
            _locked = value;
            _dragHandle = null;
            Capture = false;
            BackColor = value ? RecordingColor : IdleColor;
            Cursor = Cursors.Default;
            ApplyLayout(); // the arrow in the middle goes away while recording, and comes back
        }
    }

    public void SetRegion(PixelRect region, MonitorInfo monitor)
    {
        if (region == _region && monitor == _monitor)
            return;
        _region = region;
        _monitor = monitor;
        _layout = CreateLayout();
        ApplyLayout();
    }

    /// <summary>Arrow moves the frame 1 pixel; Ctrl+arrow moves the active edge or corner 2 pixels. Returns true when handled.</summary>
    public bool TryHandleArrow(Keys keyData)
    {
        Arrow? arrow = (keyData & Keys.KeyCode) switch
        {
            Keys.Left => Arrow.Left,
            Keys.Right => Arrow.Right,
            Keys.Up => Arrow.Up,
            Keys.Down => Arrow.Down,
            _ => null,
        };
        var modifiers = keyData & Keys.Modifiers;
        if (arrow is null || _locked || (modifiers != Keys.None && modifiers != Keys.Control))
            return false;

        var handle = modifiers == Keys.Control ? ActiveHandle : FrameHandle.Move;
        var region = handle == FrameHandle.Move
            ? FrameAdjust.Nudge(_region, arrow.Value, _monitor.Bounds)
            : FrameAdjust.NudgeEdge(_region, handle, arrow.Value, _monitor.Bounds);
        SetRegion(region, _monitor);
        var focus = MagnifierMath.FocusPoint(_region, handle, _region.Center);
        Adjusting?.Invoke(this, new AdjustingEventArgs(focus, focus));
        AdjustEnded?.Invoke(this, EventArgs.Empty);
        return true;
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= NativeMethods.WS_EX_TOOLWINDOW;
            return cp;
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        CaptureExclusion.Apply(Handle);
        _mover = new MoverWindow(this, IdleColor);
    }

    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);
        PlaceMover();
    }

    protected override void OnDpiChanged(DpiChangedEventArgs e)
    {
        e.Cancel = true; // keep the exact physical-pixel bounds set by ApplyLayout
        ScaleChanged?.Invoke(this, EventArgs.Empty);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData) =>
        TryHandleArrow(keyData) || base.ProcessCmdKey(ref msg, keyData);

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Left && _layout.HitTest(MousePosition.ToPixelPoint()) is { } handle)
            StartDrag(handle);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_dragHandle is null)
            Cursor = _locked ? Cursors.Default : CursorFor(_layout.HitTest(MousePosition.ToPixelPoint()));
        else
            DragStep();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        EndDrag();
    }

    /// <summary>
    /// Starts dragging a part of the frame from where the mouse is now. The arrow in the middle uses it too,
    /// with <paramref name="magnify"/> false: no magnifier while dragging by it.
    /// </summary>
    internal void StartDrag(FrameHandle handle, bool magnify = true)
    {
        if (_locked)
            return;
        var cursor = MousePosition.ToPixelPoint();
        _dragHandle = handle;
        _dragMagnifies = magnify;
        _dragStartCursor = cursor;
        _dragStartRegion = _region;
        _dragSnap = handle == FrameHandle.Move ? SnapTargets.None : SnapSource?.Invoke(_monitor.Bounds) ?? SnapTargets.None;
        if (handle != FrameHandle.Move)
            ActiveHandle = handle;
        if (magnify)
            Adjusting?.Invoke(this, new AdjustingEventArgs(MagnifierMath.FocusPoint(_region, handle, cursor), cursor));
    }

    /// <summary>Follows the mouse while a drag is going on.</summary>
    internal void DragStep()
    {
        if (_dragHandle is not { } handle)
            return;
        var cursor = MousePosition.ToPixelPoint();
        var delta = new PixelPoint(cursor.X - _dragStartCursor.X, cursor.Y - _dragStartCursor.Y);
        if (handle == FrameHandle.Move)
        {
            var monitor = Screens.At(cursor);
            SetRegion(FrameGeometry.Move(_dragStartRegion, delta, monitor.Bounds), monitor);
        }
        else
        {
            var snap = (ModifierKeys & Keys.Alt) != 0 ? SnapTargets.None : _dragSnap;
            SetRegion(FrameGeometry.Resize(_dragStartRegion, handle, delta, _monitor.Bounds, snap), _monitor);
        }
        if (_dragMagnifies)
            Adjusting?.Invoke(this, new AdjustingEventArgs(MagnifierMath.FocusPoint(_region, handle, cursor), cursor));
    }

    /// <summary>Ends a drag; the magnifier hides a moment later.</summary>
    internal void EndDrag()
    {
        if (_dragHandle is null)
            return;
        _dragHandle = null;
        AdjustEnded?.Invoke(this, EventArgs.Empty);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        var color = _locked ? RecordingColor : IdleColor;
        using var border = new SolidBrush(color);
        using var outline = new Pen(color);
        foreach (var r in _layout.Ring)
            g.FillRectangle(border, ToClient(r));
        foreach (var (_, r) in _layout.Handles)
        {
            var c = ToClient(r);
            g.FillRectangle(_locked ? border : Brushes.White, c);
            g.DrawRectangle(outline, c.X, c.Y, c.Width - 1, c.Height - 1);
        }
        var label = ToClient(_layout.Label);
        g.FillRectangle(border, label);
        TextRenderer.DrawText(g, LabelText, LabelFont, label, Color.White,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
    }

    private string LabelText => $"{_region.Width} × {_region.Height}";

    private FrameLayout CreateLayout()
    {
        var text = TextRenderer.MeasureText(LabelText, LabelFont, Size.Empty, TextFormatFlags.NoPadding);
        return new FrameLayout(_region, new PixelSize(text.Width + 12, text.Height + 6), _monitor.Bounds);
    }

    /// <summary>
    /// The window covers the monitor and stays put while the frame is dragged; only its shape changes. Moving it
    /// on every drag step showed the far sides white or black for a moment. It leaves out one row the frame does
    /// not reach, the bottom one, else the top one: Windows takes a window over the whole monitor for a
    /// full-screen app, quiets notifications and drops the taskbar behind it.
    /// </summary>
    private void ApplyLayout()
    {
        var screen = _monitor.Bounds.ToRectangle();
        if (_layout.WindowBounds.Bottom >= _monitor.Bounds.Bottom)
            screen.Y += 1;
        screen.Height -= 1;
        if (Bounds != screen)
            Bounds = screen; // when the frame goes to another monitor, or reaches the bottom row or leaves it
        _origin = screen.Location;
        var shape = new Region(Rectangle.Empty);
        foreach (var r in _layout.Ring)
            shape.Union(ToClient(r));
        foreach (var (_, r) in _layout.Handles)
            shape.Union(ToClient(r));
        shape.Union(ToClient(_layout.Label));
        var old = Region;
        Region = shape;
        old?.Dispose();
        Invalidate();
        Update(); // paint now: in a drag the mouse moves keep coming, and a paint left waiting showed the moved side white
        PlaceMover();
    }

    /// <summary>Puts the arrow window in the middle of the region, or hides it while recording or when the region is small.</summary>
    private void PlaceMover()
    {
        if (_mover is null)
            return;
        if (Visible && !_locked && _layout.Mover is { } m)
        {
            _mover.Location = new Point(m.Left, m.Top);
            if (!_mover.Visible)
                _mover.Show();
        }
        else if (_mover.Visible)
        {
            _mover.Hide();
        }
    }

    private Rectangle ToClient(PixelRect r) =>
        new(r.Left - _origin.X, r.Top - _origin.Y, r.Width, r.Height);

    private static Cursor CursorFor(FrameHandle? handle) => handle switch
    {
        FrameHandle.Left or FrameHandle.Right => Cursors.SizeWE,
        FrameHandle.Top or FrameHandle.Bottom => Cursors.SizeNS,
        FrameHandle.TopLeft or FrameHandle.BottomRight => Cursors.SizeNWSE,
        FrameHandle.TopRight or FrameHandle.BottomLeft => Cursors.SizeNESW,
        FrameHandle.Move => Cursors.SizeAll,
        _ => Cursors.Default,
    };
}
