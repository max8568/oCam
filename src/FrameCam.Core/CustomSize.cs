using System.Globalization;

namespace FrameCam.Core;

/// <summary>A recording size the user saved (spec 7.5), listed in the size menu under the presets.</summary>
public sealed record CustomSize(string Label, int Width, int Height)
{
    public const int MaxLabelLength = 20;
    public const int MaxSide = 8192;
    public const int MaxCount = 20;

    public PixelSize Size => new(Width, Height);

    /// <summary>"直播（1600 × 900）", or just "1600 × 900" without a label.</summary>
    public string MenuText => Label.Length == 0 ? $"{Width} × {Height}" : $"{Label}（{Width} × {Height}）";

    /// <summary>Sides from MinSize to MaxSide, and a label of at most MaxLabelLength characters.</summary>
    public bool IsValid => LabelLength(Label) <= MaxLabelLength && IsValidSide(Width) && IsValidSide(Height);

    /// <summary>Reads what the user typed: the size, or null and a message that says what to change.</summary>
    public static (CustomSize? Size, string? Error) Parse(string label, string width, string height)
    {
        label = label.Trim();
        if (LabelLength(label) > MaxLabelLength)
            return (null, $"標籤最多 {MaxLabelLength} 個字");
        if (!TryParseNumber(width, out int w) || !TryParseNumber(height, out int h))
            return (null, "寬和高要填數字");
        if (!IsValidSide(w) || !IsValidSide(h))
            return (null, $"寬和高要在 {FrameGeometry.MinSize} 到 {MaxSide} 之間");
        return (new CustomSize(label, w, h), null);
    }

    /// <summary>Characters as the user sees them: an emoji counts once, not as its two UTF-16 halves.</summary>
    private static int LabelLength(string label) => new StringInfo(label).LengthInTextElements;

    private static bool IsValidSide(int side) => side >= FrameGeometry.MinSize && side <= MaxSide;

    private static bool TryParseNumber(string text, out int value)
    {
        // Full-width digits, as a Chinese input method may type them, count as digits too.
        var ascii = new string(text.Trim().Select(c => c is >= '０' and <= '９' ? (char)(c - '０' + '0') : c).ToArray());
        return int.TryParse(ascii, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value);
    }
}
