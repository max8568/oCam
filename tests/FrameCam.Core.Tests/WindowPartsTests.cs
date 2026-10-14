using FrameCam.Core;

namespace FrameCam.Core.Tests;

public class WindowPartsTests
{
    // Sizes measured on Chrome and Notepad++ windows (1920×1080 monitor at 100%).
    private static readonly PixelRect Window = new(1920, 0, 1920, 1032);

    [Fact]
    public void Smallest_part_under_the_cursor_is_picked()
    {
        // Chrome draws its own title bar, so the client area and a full-window overlay cover everything.
        var client = Window;
        var overlay = Window;
        var page = new PixelRect(1920, 121, 1920, 911);
        Assert.Equal(page, WindowParts.Under(new PixelPoint(2500, 500), Window, [client, overlay, page]));
    }

    [Fact]
    public void Cursor_above_the_page_gets_the_part_there()
    {
        var client = Window;
        var page = new PixelRect(1920, 121, 1920, 911);
        Assert.Equal(client, WindowParts.Under(new PixelPoint(2500, 50), Window, [client, page]));
    }

    [Fact]
    public void Cursor_outside_every_part_gives_the_whole_window()
    {
        // Notepad++: the cursor is on the standard title bar, above the client area.
        var client = new PixelRect(1920, 43, 1920, 989);
        Assert.Equal(Window, WindowParts.Under(new PixelPoint(2500, 20), Window, [client]));
    }

    [Fact]
    public void Parts_smaller_than_the_minimum_size_are_skipped()
    {
        var button = new PixelRect(2000, 500, 24, 24);
        var panel = new PixelRect(1920, 400, 600, 300);
        Assert.Equal(panel, WindowParts.Under(new PixelPoint(2010, 510), Window, [button, panel]));
    }

    [Fact]
    public void Parts_are_cut_at_the_window_edges()
    {
        // A scrolled child window can reach past the bottom of its window.
        var child = new PixelRect(1920, 900, 1920, 400);
        Assert.Equal(new PixelRect(1920, 900, 1920, 132), WindowParts.Under(new PixelPoint(2500, 1000), Window, [child]));
    }
}
