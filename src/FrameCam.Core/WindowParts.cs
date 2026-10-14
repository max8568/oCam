namespace FrameCam.Core;

/// <summary>Ctrl+click in window picking (spec 7.5): the smallest part of a window under the cursor.</summary>
public static class WindowParts
{
    /// <summary>
    /// The smallest part (the client area or a child window) that contains the cursor, cut at the window's
    /// edges and at least MinSize on each side. The whole window when no part qualifies.
    /// </summary>
    public static PixelRect Under(PixelPoint cursor, PixelRect window, IEnumerable<PixelRect> parts)
    {
        var best = window;
        long bestArea = long.MaxValue;
        foreach (var part in parts)
        {
            var r = part.Intersect(window);
            if (r.Width < FrameGeometry.MinSize || r.Height < FrameGeometry.MinSize || !r.Contains(cursor))
                continue;
            long area = (long)r.Width * r.Height;
            if (area < bestArea)
            {
                best = r;
                bestArea = area;
            }
        }
        return best;
    }
}
