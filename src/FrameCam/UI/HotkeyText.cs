namespace FrameCam.UI;

/// <summary>Converts hotkeys between Keys and the text in settings.json, such as "F2" or "Ctrl+Shift+R".</summary>
internal static class HotkeyText
{
    private static readonly KeysConverter Converter = new();

    public static string Format(Keys keys) => Converter.ConvertToInvariantString(keys) ?? "";

    /// <summary>False for unreadable text and for keys that IsUsable rejects.</summary>
    public static bool TryParse(string? text, out Keys keys)
    {
        keys = Keys.None;
        if (string.IsNullOrWhiteSpace(text))
            return false;
        try
        {
            if (Converter.ConvertFromInvariantString(text) is not Keys parsed || !IsUsable(parsed))
                return false;
            keys = parsed;
            return true;
        }
        catch (Exception e) when (e is FormatException or ArgumentException or NotSupportedException)
        {
            return false;
        }
    }

    /// <summary>
    /// Whether the keys can be a global hotkey. A global hotkey stops reaching every other program, so a key
    /// used for typing (letters, Backspace, arrows, ...) needs Ctrl or Alt. Function keys, Print Screen,
    /// Pause and Scroll Lock can be used alone. A modifier key alone is never a hotkey.
    /// </summary>
    public static bool IsUsable(Keys keys)
    {
        var code = keys & Keys.KeyCode;
        if (code is Keys.None or Keys.ControlKey or Keys.ShiftKey or Keys.Menu or Keys.LWin or Keys.RWin)
            return false;
        return (keys & (Keys.Control | Keys.Alt)) != 0
            || code is >= Keys.F1 and <= Keys.F24 or Keys.PrintScreen or Keys.Pause or Keys.Scroll;
    }
}
