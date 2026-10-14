using FrameCam.Core;

namespace FrameCam.Core.Tests;

public class PixelRectTests
{
    private static readonly PixelRect R = new(10, 20, 100, 50);

    [Fact]
    public void Right_and_bottom_are_exclusive()
    {
        Assert.Equal(110, R.Right);
        Assert.Equal(70, R.Bottom);
        Assert.True(R.Contains(new PixelPoint(109, 69)));
        Assert.False(R.Contains(new PixelPoint(110, 69)));
        Assert.False(R.Contains(new PixelPoint(109, 70)));
    }

    [Fact]
    public void Contains_rect_includes_touching_edges()
    {
        Assert.True(R.Contains(R));
        Assert.False(R.Contains(R.Offset(1, 0)));
    }

    [Fact]
    public void Intersect_returns_overlap_or_empty()
    {
        Assert.Equal(new PixelRect(60, 40, 50, 30), R.Intersect(new PixelRect(60, 40, 200, 200)));
        Assert.Equal(default(PixelRect), R.Intersect(new PixelRect(110, 20, 10, 10)));
        Assert.False(R.IntersectsWith(new PixelRect(110, 20, 10, 10)));
    }

    [Fact]
    public void Inflate_grows_every_side() =>
        Assert.Equal(new PixelRect(6, 16, 108, 58), R.Inflate(4));

    [Fact]
    public void Center_is_halfway() =>
        Assert.Equal(new PixelPoint(60, 45), R.Center);
}
