using System.ComponentModel;
using System.Drawing.Drawing2D;
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
/// the size label, all outside the region, and the four-way arrow in the middle of the region, so the rest of
/// the region's pixels stay visible and clickable. While recording the arrow is gone. The window is left out
/// of every screen capture.
/// </summary>
internal sealed class FrameWindow : Form
{
    private static readonly Color IdleColor = Color.FromArgb(0, 120, 215);
    private static readonly Color RecordingColor = Color.FromArgb(220, 40, 40);
    private static readonly Font LabelFont = new("Segoe UI", 12f, FontStyle.Bold, GraphicsUnit.Pixel);
    private const int ArrowEdge = 2; // white edge around the middle arrow, so it shows on dark and light screens

    private PixelRect _region;
    private MonitorInfo _monitor;
    private FrameLayout _layout;
    private bool _locked;

    private FrameHandle? _dragHandle;
    private PixelPoint _dragStartCursor;
    private PixelRect _dragStartRegion;
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

    /// <summary>Raised on every drag step and keyboard nudge.</summary>
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
            ApplyLayout(); // the mover goes away while recording, and comes back
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
        if (_locked || e.Button != MouseButtons.Left)
            return;
        var cursor = MousePosition.ToPixelPoint();
        if (_layout.HitTest(cursor) is not { } handle)
            return;
        _dragHandle = handle;
        _dragStartCursor = cursor;
        _dragStartRegion = _region;
        _dragSnap = handle == FrameHandle.Move ? SnapTargets.None : SnapSource?.Invoke(_monitor.Bounds) ?? SnapTargets.None;
        if (handle != FrameHandle.Move)
            ActiveHandle = handle;
        Adjusting?.Invoke(this, new AdjustingEventArgs(MagnifierMath.FocusPoint(_region, handle, cursor), cursor));
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var cursor = MousePosition.ToPixelPoint();
        if (_dragHandle is not { } handle)
        {
            Cursor = _locked ? Cursors.Default : CursorFor(_layout.HitTest(cursor));
            return;
        }
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
        Adjusting?.Invoke(this, new AdjustingEventArgs(MagnifierMath.FocusPoint(_region, handle, cursor), cursor));
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
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
        if (ShowsMover(out var mover))
        {
            using var arrow = MoverArrow(ToClient(mover));
            using var edge = ArrowEdgeOf(arrow);
            g.FillPath(Brushes.White, edge);
            g.FillPath(border, arrow);
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

    private void ApplyLayout()
    {
        Bounds = _layout.WindowBounds.ToRectangle();
        var shape = new Region(Rectangle.Empty);
        foreach (var r in _layout.Ring)
            shape.Union(ToClient(r));
        foreach (var (_, r) in _layout.Handles)
            shape.Union(ToClient(r));
        shape.Union(ToClient(_layout.Label));
        if (ShowsMover(out var mover))
        {
            using var arrow = MoverArrow(ToClient(mover));
            using var edge = ArrowEdgeOf(arrow);
            shape.Union(edge);
            shape.Union(arrow); // only the arrow itself: between its arms the screen shows through
        }
        var old = Region;
        Region = shape;
        old?.Dispose();
        Invalidate();
    }

    private bool ShowsMover(out PixelRect mover)
    {
        mover = _layout.Mover ?? default;
        return !_locked && _layout.Mover is not null;
    }

    /// <summary>The four-way arrow that fills the mover's box: two thick bars crossing and a solid tip at each end.</summary>
    private static GraphicsPath MoverArrow(Rectangle box)
    {
        float s = box.Width / 40f; // drawn on a 40 × 40 grid
        PointF P(float x, float y) => new(box.Left + x * s, box.Top + y * s);
        var path = new GraphicsPath(FillMode.Winding);
        path.AddPolygon(new[] { P(7, 18), P(33, 18), P(33, 22), P(7, 22) });
        path.AddPolygon(new[] { P(18, 7), P(22, 7), P(22, 33), P(18, 33) });
        path.AddPolygon(new[] { P(1, 20), P(9, 13), P(9, 27) });
        path.AddPolygon(new[] { P(39, 20), P(31, 13), P(31, 27) });
        path.AddPolygon(new[] { P(20, 1), P(27, 9), P(13, 9) });
        path.AddPolygon(new[] { P(20, 39), P(13, 31), P(27, 31) });
        return path;
    }

    private static GraphicsPath ArrowEdgeOf(GraphicsPath arrow)
    {
        var edge = (GraphicsPath)arrow.Clone();
        using var pen = new Pen(Color.White, 2 * ArrowEdge) { LineJoin = LineJoin.Round };
        edge.Widen(pen);
        edge.FillMode = FillMode.Winding;
        return edge;
    }

    private Rectangle ToClient(PixelRect r) =>
        new(r.Left - _layout.WindowBounds.Left, r.Top - _layout.WindowBounds.Top, r.Width, r.Height);

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
