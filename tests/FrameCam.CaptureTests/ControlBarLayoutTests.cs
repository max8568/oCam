using FrameCam.UI;

namespace FrameCam.CaptureTests;

/// <summary>Where the control bar's buttons go. Pure geometry; nothing is shown on screen.</summary>
public class ControlBarLayoutTests
{
    private static readonly BarItem[] InOrder = Enum.GetValues<BarItem>();

    [Fact]
    public void Buttons_are_in_one_row_in_order_without_overlapping()
    {
        var layout = ControlBarLayout.Compute(1f, 92);

        for (int i = 1; i < InOrder.Length; i++)
            Assert.True(layout[InOrder[i - 1]].Right <= layout[InOrder[i]].Left, $"{InOrder[i - 1]} overlaps {InOrder[i]}");
        Assert.True(layout.Grip.Right <= layout[BarItem.Record].Left);
        Assert.True(layout.Bar.Left <= layout.Grip.Left);
        Assert.True(layout[BarItem.Close].Right <= layout.Bar.Right);
        foreach (var item in InOrder)
            Assert.Equal(layout.Bar.Top + 6, layout[item].Top);
    }

    [Fact]
    public void The_help_button_comes_after_settings_and_the_line_before_the_close_button()
    {
        var layout = ControlBarLayout.Compute(1f, 92);

        Assert.Equal(layout[BarItem.Settings].Right + 4, layout[BarItem.Help].Left);
        float line = layout.Separators[^1];
        Assert.True(layout[BarItem.Help].Right < line && line < layout[BarItem.Close].Left);
    }

    [Fact]
    public void The_minimize_button_sits_between_the_line_and_the_close_button()
    {
        var layout = ControlBarLayout.Compute(1f, 92);

        float line = layout.Separators[^1];
        Assert.True(line < layout[BarItem.Minimize].Left);
        Assert.Equal(layout[BarItem.Minimize].Right + 4, layout[BarItem.Close].Left);
    }

    [Fact]
    public void The_pause_button_sits_between_the_record_and_screenshot_buttons()
    {
        var layout = ControlBarLayout.Compute(1f, 92);

        Assert.Equal(36, layout[BarItem.Pause].Width);
        Assert.Equal(layout[BarItem.Record].Right + 4, layout[BarItem.Pause].Left);
        Assert.Equal(layout[BarItem.Pause].Right + 4, layout[BarItem.Screenshot].Left);
    }

    [Fact]
    public void Bar_is_48_pixels_high_with_a_see_through_margin_for_its_shadow()
    {
        var layout = ControlBarLayout.Compute(1f, 92);

        Assert.Equal(48, layout.Bar.Height);
        Assert.Equal(new PointF(ControlBarLayout.Margin, ControlBarLayout.Margin), layout.Bar.Location);
        Assert.Equal(new Size((int)layout.Bar.Width + 2 * ControlBarLayout.Margin, 48 + 2 * ControlBarLayout.Margin), layout.WindowSize);
    }

    [Fact]
    public void Record_button_is_as_wide_as_its_measured_text_needs()
    {
        var narrow = ControlBarLayout.Compute(1f, 92);
        var wide = ControlBarLayout.Compute(1f, 140);

        Assert.Equal(92, narrow[BarItem.Record].Width);
        Assert.Equal(140, wide[BarItem.Record].Width);
        Assert.Equal(narrow.Bar.Width + 48, wide.Bar.Width);
    }

    [Fact]
    public void Everything_grows_with_the_display_scale()
    {
        var normal = ControlBarLayout.Compute(1f, 92);
        var large = ControlBarLayout.Compute(1.5f, 138);

        Assert.Equal(72, large.Bar.Height);
        Assert.Equal(54, large[BarItem.Settings].Width);
        Assert.Equal(normal.Bar.Width * 1.5f, large.Bar.Width, 0.01f);
        Assert.Equal(normal[BarItem.Close].Left * 1.5f, large[BarItem.Close].Left, 0.01f);
        Assert.Equal(ControlBarLayout.Margin * 1.5f, large.Bar.Left, 0.01f);
    }

    [Fact]
    public void ItemAt_finds_the_button_under_a_point()
    {
        var layout = ControlBarLayout.Compute(1f, 92);

        foreach (var item in InOrder)
            Assert.Equal(item, layout.ItemAt(Center(layout[item])));
        Assert.Null(layout.ItemAt(new Point((int)layout[BarItem.Size].Right + 2, (int)Center(layout[BarItem.Size]).Y))); // the gap after it
        Assert.Null(layout.ItemAt(Center(layout.Grip)));
        Assert.Null(layout.ItemAt(new Point(0, 0)));
    }

    private static Point Center(RectangleF r) => new((int)(r.Left + r.Width / 2), (int)(r.Top + r.Height / 2));
}
