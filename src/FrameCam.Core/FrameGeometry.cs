namespace FrameCam.Core;

/// <summary>
/// Calculations for dragging the recording region (spec 7.1, 7.2). Every region returned is inside its
/// monitor and is at least MinSize on each side.
/// </summary>
public static class FrameGeometry
{
    public const int MinSize = 32;

    /// <summary>The largest even number not greater than value.</summary>
    public static int FloorEven(int value) => value - (value & 1);

    /// <summary>
    /// The region while an edge or corner is dragged. start is the region when the drag began and delta is
    /// the cursor movement since then. Each dragged edge snaps to a target within SnapTargets.Threshold,
    /// stays on the monitor, and keeps at least MinSize from the opposite edge.
    /// </summary>
    public static PixelRect Resize(PixelRect start, Handle handle, PixelPoint delta, PixelRect monitor, SnapTargets snap)
    {
        if (handle == Handle.Move)
            throw new ArgumentException("Use Move for Handle.Move.", nameof(handle));

        int left = start.Left, top = start.Top, right = start.Right, bottom = start.Bottom;
        if (handle.MovesLeft())
            left = Math.Clamp(snap.SnapX(start.Left + delta.X), monitor.Left, right - MinSize);
        else if (handle.MovesRight())
            right = Math.Clamp(snap.SnapX(start.Right + delta.X), left + MinSize, monitor.Right);
        if (handle.MovesTop())
            top = Math.Clamp(snap.SnapY(start.Top + delta.Y), monitor.Top, bottom - MinSize);
        else if (handle.MovesBottom())
            bottom = Math.Clamp(snap.SnapY(start.Bottom + delta.Y), top + MinSize, monitor.Bottom);
        return PixelRect.FromEdges(left, top, right, bottom);
    }

    /// <summary>
    /// The region while the whole frame is dragged. monitor is the monitor under the cursor; the start size
    /// is shrunk to fit it when needed, so dragging back to a bigger monitor restores the size.
    /// </summary>
    public static PixelRect Move(PixelRect start, PixelPoint delta, PixelRect monitor) =>
        ClampToMonitor(start.Offset(delta.X, delta.Y), monitor);

    /// <summary>Shrinks the region to fit the monitor, then shifts it inside.</summary>
    public static PixelRect ClampToMonitor(PixelRect region, PixelRect monitor)
    {
        int width = Math.Max(MinSize, Math.Min(region.Width, monitor.Width));
        int height = Math.Max(MinSize, Math.Min(region.Height, monitor.Height));
        int left = Math.Clamp(region.Left, monitor.Left, monitor.Right - width);
        int top = Math.Clamp(region.Top, monitor.Top, monitor.Bottom - height);
        return new PixelRect(left, top, width, height);
    }
}
