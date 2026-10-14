namespace FrameCam.Core;

/// <summary>
/// Where the frame window's parts go for a region, in screen pixels (spec 7.1): a 4-pixel border ring just
/// outside the region, 9×9 handles outside its corners and edge midpoints, and the size label above the
/// top-left corner, or inside the region's top-left when there is no room above on the monitor.
/// </summary>
public sealed class FrameLayout
{
    public const int BorderWidth = 4;
    public const int HandleSize = 9;
    public const int CornerReach = 16;

    public FrameLayout(PixelRect region, PixelSize labelSize, PixelRect monitor)
    {
        Region = region;

        int labelTop = region.Top - BorderWidth - labelSize.Height;
        Label = labelTop >= monitor.Top
            ? new PixelRect(Math.Max(region.Left - BorderWidth, monitor.Left), labelTop, labelSize.Width, labelSize.Height)
            : new PixelRect(region.Left, region.Top, labelSize.Width, labelSize.Height);

        var outer = region.Inflate(BorderWidth);
        Ring =
        [
            PixelRect.FromEdges(outer.Left, outer.Top, outer.Right, region.Top),
            PixelRect.FromEdges(outer.Left, region.Bottom, outer.Right, outer.Bottom),
            PixelRect.FromEdges(outer.Left, region.Top, region.Left, region.Bottom),
            PixelRect.FromEdges(region.Right, region.Top, outer.Right, region.Bottom),
        ];

        const int h = HandleSize;
        int midX = region.Left + region.Width / 2 - h / 2;
        int midY = region.Top + region.Height / 2 - h / 2;
        Handles =
        [
            (Handle.TopLeft, new PixelRect(region.Left - h, region.Top - h, h, h)),
            (Handle.Top, new PixelRect(midX, region.Top - h, h, h)),
            (Handle.TopRight, new PixelRect(region.Right, region.Top - h, h, h)),
            (Handle.Right, new PixelRect(region.Right, midY, h, h)),
            (Handle.BottomRight, new PixelRect(region.Right, region.Bottom, h, h)),
            (Handle.Bottom, new PixelRect(midX, region.Bottom, h, h)),
            (Handle.BottomLeft, new PixelRect(region.Left - h, region.Bottom, h, h)),
            (Handle.Left, new PixelRect(region.Left - h, midY, h, h)),
        ];

        var aroundHandles = region.Inflate(HandleSize);
        WindowBounds = PixelRect.FromEdges(
            Math.Min(aroundHandles.Left, Label.Left),
            Math.Min(aroundHandles.Top, Label.Top),
            Math.Max(aroundHandles.Right, Label.Right),
            Math.Max(aroundHandles.Bottom, Label.Bottom));
    }

    public PixelRect Region { get; }
    public PixelRect Label { get; }
    public IReadOnlyList<PixelRect> Ring { get; }
    public IReadOnlyList<(Handle Handle, PixelRect Rect)> Handles { get; }
    public PixelRect WindowBounds { get; }

    /// <summary>What a press at this screen point grabs, or null when the point is not on the frame.</summary>
    public Handle? HitTest(PixelPoint p)
    {
        if (Label.Contains(p))
            return Handle.Move;
        if (!Ring.Any(r => r.Contains(p)) && !Handles.Any(h => h.Rect.Contains(p)))
            return null;

        var r = Region;
        bool left = p.X < r.Left, right = p.X >= r.Right, top = p.Y < r.Top, bottom = p.Y >= r.Bottom;
        // On a side, the part within CornerReach of a corner grabs that corner.
        if ((left || right) && !(top || bottom))
        {
            top = p.Y < Math.Min(r.Top + CornerReach, r.Center.Y);
            bottom = !top && p.Y >= Math.Max(r.Bottom - CornerReach, r.Center.Y);
        }
        else if ((top || bottom) && !(left || right))
        {
            left = p.X < Math.Min(r.Left + CornerReach, r.Center.X);
            right = !left && p.X >= Math.Max(r.Right - CornerReach, r.Center.X);
        }
        return (left, right, top, bottom) switch
        {
            (true, _, true, _) => Handle.TopLeft,
            (_, true, true, _) => Handle.TopRight,
            (true, _, _, true) => Handle.BottomLeft,
            (_, true, _, true) => Handle.BottomRight,
            (true, _, _, _) => Handle.Left,
            (_, true, _, _) => Handle.Right,
            (_, _, true, _) => Handle.Top,
            _ => Handle.Bottom,
        };
    }
}
