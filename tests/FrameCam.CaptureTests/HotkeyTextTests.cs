using FrameCam.UI;

namespace FrameCam.CaptureTests;

public class HotkeyTextTests
{
    [Theory]
    [InlineData("F2", Keys.F2)]
    [InlineData("Ctrl+F9", Keys.Control | Keys.F9)]
    [InlineData("Ctrl+Shift+R", Keys.Control | Keys.Shift | Keys.R)]
    public void Settings_text_round_trips(string text, Keys expected)
    {
        Assert.True(HotkeyText.TryParse(text, out var keys));
        Assert.Equal(expected, keys);
        Assert.Equal(text, HotkeyText.Format(keys));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Foo")]
    [InlineData("Ctrl")]
    public void Unusable_text_is_rejected(string text) =>
        Assert.False(HotkeyText.TryParse(text, out _));
}
