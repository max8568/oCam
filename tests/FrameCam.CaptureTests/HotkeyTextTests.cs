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
    [InlineData("R")]
    [InlineData("Back")]
    public void Unusable_text_is_rejected(string text) =>
        Assert.False(HotkeyText.TryParse(text, out _));

    [Theory]
    [InlineData(Keys.Back)]
    [InlineData(Keys.Space)]
    [InlineData(Keys.Delete)]
    [InlineData(Keys.Left)]
    [InlineData(Keys.R)]
    [InlineData(Keys.Shift | Keys.R)]
    [InlineData(Keys.Control | Keys.ControlKey)]
    public void Keys_used_for_typing_cannot_be_global_hotkeys(Keys keys) =>
        Assert.False(HotkeyText.IsUsable(keys));

    [Fact]
    public void Space_alone_can_be_the_pause_key()
    {
        Assert.True(HotkeyText.IsUsable(Keys.Space, allowSpace: true));
        Assert.True(HotkeyText.TryParse("Space", out var keys, allowSpace: true));
        Assert.Equal(Keys.Space, keys);
        Assert.Equal("Space", HotkeyText.Format(keys));
    }

    [Theory]
    [InlineData(Keys.R)]
    [InlineData(Keys.Back)]
    [InlineData(Keys.Shift | Keys.Space)]
    public void Other_keys_used_for_typing_cannot_be_the_pause_key(Keys keys) =>
        Assert.False(HotkeyText.IsUsable(keys, allowSpace: true));

    [Theory]
    [InlineData(Keys.F2)]
    [InlineData(Keys.Shift | Keys.F12)]
    [InlineData(Keys.PrintScreen)]
    [InlineData(Keys.Control | Keys.R)]
    [InlineData(Keys.Alt | Keys.Back)]
    [InlineData(Keys.Control | Keys.Shift | Keys.S)]
    public void Function_keys_and_keys_with_ctrl_or_alt_can_be_global_hotkeys(Keys keys) =>
        Assert.True(HotkeyText.IsUsable(keys));
}
