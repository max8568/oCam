namespace FrameCam.Core;

/// <summary>
/// Window edges a dragged frame edge can snap to. X targets are left/right edges, Y targets are top/bottom
/// edges, in the same exclusive convention as PixelRect.Right and Bottom.
/// </summary>
public sealed class SnapTargets
{
    public const int Threshold = 4;
    public static readonly SnapTargets None = new([], []);

    private readonly int[] _xs;
    private readonly int[] _ys;

    public SnapTargets(IEnumerable<int> xs, IEnumerable<int> ys)
    {
        _xs = xs.Distinct().ToArray();
        _ys = ys.Distinct().ToArray();
    }

    /// <summary>The edges of every non-empty rectangle that overlaps the monitor.</summary>
    public static SnapTargets FromRects(IEnumerable<PixelRect> rects, PixelRect monitor)
    {
        var onMonitor = rects.Where(r => r.Width > 0 && r.Height > 0 && r.IntersectsWith(monitor)).ToList();
        return new SnapTargets(
            onMonitor.SelectMany(r => new[] { r.Left, r.Right }),
            onMonitor.SelectMany(r => new[] { r.Top, r.Bottom }));
    }

    public int SnapX(int x) => Snap(x, _xs);

    public int SnapY(int y) => Snap(y, _ys);

    /// <summary>The nearest target at most Threshold pixels away, or the value itself.</summary>
    private static int Snap(int value, int[] targets)
    {
        int best = value, bestDistance = Threshold + 1;
        foreach (int target in targets)
        {
            int distance = Math.Abs(target - value);
            if (distance < bestDistance)
            {
                best = target;
                bestDistance = distance;
            }
        }
        return best;
    }
}
