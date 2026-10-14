namespace FrameCam.UI;

/// <summary>Converts hotkeys between Keys and the text in settings.json, such as "F2" or "Ctrl+Shift+R".</summary>
internal static class HotkeyText
{
    private static readonly KeysConverter Converter = new();

    public static string Format(Keys keys) => Converter.ConvertToInvariantString(keys) ?? "";

    /// <summary>False for unreadable text and for modifier keys alone.</summary>
    public static bool TryParse(string? text, out Keys keys)
    {
        keys = Keys.None;
        if (string.IsNullOrWhiteSpace(text))
            return false;
        try
        {
            if (Converter.ConvertFromInvariantString(text) is not Keys parsed)
                return false;
            var code = parsed & Keys.KeyCode;
            if (code is Keys.None or Keys.ControlKey or Keys.ShiftKey or Keys.Menu or Keys.LWin or Keys.RWin)
                return false;
            keys = parsed;
            return true;
        }
        catch (Exception e) when (e is FormatException or ArgumentException or NotSupportedException)
        {
            return false;
        }
    }
}
