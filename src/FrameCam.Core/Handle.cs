namespace FrameCam.Core;

/// <summary>What the user grabbed on the frame: the whole frame, one edge, or one corner.</summary>
public enum Handle
{
    Move,
    Left,
    Top,
    Right,
    Bottom,
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight,
}

public static class HandleExtensions
{
    public static bool MovesLeft(this Handle h) => h is Handle.Left or Handle.TopLeft or Handle.BottomLeft;
    public static bool MovesRight(this Handle h) => h is Handle.Right or Handle.TopRight or Handle.BottomRight;
    public static bool MovesTop(this Handle h) => h is Handle.Top or Handle.TopLeft or Handle.TopRight;
    public static bool MovesBottom(this Handle h) => h is Handle.Bottom or Handle.BottomLeft or Handle.BottomRight;
}
