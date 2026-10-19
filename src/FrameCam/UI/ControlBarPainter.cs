using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;

namespace FrameCam.UI;

/// <summary>What the control bar shows right now.</summary>
internal sealed record BarLook
{
    public bool Recording { get; init; }

    /// <summary>The pause button shows the resume sign instead of the pause sign.</summary>
    public bool Paused { get; init; }

    /// <summary>The record hotkey written on the record button; empty when it could not be registered.</summary>
    public string RecordHint { get; init; } = "";

    /// <summary>Recording time on the stop button, hh:mm:ss.</summary>
    public string Elapsed { get; init; } = "00:00:00";

    public BarItem? Hover { get; init; }
    public BarItem? Pressed { get; init; }
    public IReadOnlySet<BarItem> Disabled { get; init; } = new HashSet<BarItem>();
}

/// <summary>
/// Draws the control bar (design 21): a white pill with a soft shadow, a red record button that becomes
/// the stop button with the recording time, and round icon buttons; the pause button becomes the resume
/// button while paused. Transparent outside the pill.
/// </summary>
internal static class ControlBarPainter
{
    private const string RecordLabel = "錄影";
    private const string LongestTime = "00:00:00";
    private const int ShadowSpread = 6; // its faint pixels still catch clicks, so keep it narrow
    private const int ShadowDrop = 2;

    // Inside the record button, at 100 % scale.
    private const float PadLeft = 10, PadRight = 14, DotSize = 18, DotGap = 6, HintGap = 6, StopSize = 16, StopGap = 8;

    private static readonly StringFormat TextFormat = new(StringFormat.GenericTypographic)
    {
        LineAlignment = StringAlignment.Center,
        FormatFlags = StringFormatFlags.NoWrap | StringFormatFlags.MeasureTrailingSpaces,
    };

    private static readonly Dictionary<BarItem, Glyph> IconOf = new()
    {
        [BarItem.Pause] = Glyph.Pause,
        [BarItem.Screenshot] = Glyph.Camera,
        [BarItem.Size] = Glyph.Size,
        [BarItem.Pick] = Glyph.PickWindow,
        [BarItem.Folder] = Glyph.Folder,
        [BarItem.Settings] = Glyph.Sliders,
        [BarItem.Help] = Glyph.Help,
        [BarItem.Minimize] = Glyph.Minimize,
        [BarItem.Close] = Glyph.Close,
    };

    /// <summary>
    /// Width of the record button: wide enough for both "● 錄影 F2" and "■ 00:00:00", so the other buttons
    /// do not move when recording starts.
    /// </summary>
    public static float RecordWidth(float scale, string hint)
    {
        using var fonts = new Fonts(scale);
        using var image = new Bitmap(1, 1);
        using var g = Graphics.FromImage(image);
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        return MathF.Ceiling(Math.Max(IdleWidth(g, fonts, scale, hint), RecordingWidth(g, fonts, scale)) + (PadLeft + PadRight) * scale);
    }

    public static Bitmap Render(ControlBarLayout layout, BarLook look, float scale)
    {
        var image = new Bitmap(layout.WindowSize.Width, layout.WindowSize.Height, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(image);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.Half;
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

        var bar = layout.Bar;
        float radius = bar.Height / 2;
        Shapes.Shadow(g, bar, radius, (int)MathF.Round(ShadowSpread * scale), ShadowDrop * scale);
        using (var fill = new SolidBrush(Palette.Surface))
        using (var pill = Shapes.Rounded(bar, radius))
            g.FillPath(fill, pill);
        using (var pen = new Pen(Palette.Border, 1))
        using (var outline = Shapes.Rounded(RectangleF.Inflate(bar, -0.5f, -0.5f), radius - 0.5f))
            g.DrawPath(pen, outline);

        Glyphs.Draw(g, Glyph.Grip, Centered(layout.Grip, 14 * scale), Palette.Dots);
        using (var line = new SolidBrush(Palette.Border))
            foreach (float x in layout.Separators)
                g.FillRectangle(line, x - scale / 2, bar.Top + bar.Height / 2 - 10 * scale, scale, 20 * scale);

        DrawRecordButton(g, layout[BarItem.Record], look, scale);
        foreach (var (item, icon) in IconOf)
        {
            var glyph = item == BarItem.Pause && look.Paused ? Glyph.Play : icon;
            var rect = layout[item];
            bool enabled = !look.Disabled.Contains(item);
            if (enabled && (look.Pressed == item || look.Hover == item))
            {
                using var back = new SolidBrush(look.Pressed == item ? Palette.Pressed : Palette.Hover);
                g.FillEllipse(back, rect);
            }
            bool windowButton = item is BarItem.Minimize or BarItem.Close; // gray and smaller, like a window's own
            var ink = windowButton ? Palette.Muted : Palette.Ink;
            Glyphs.Draw(g, glyph, Centered(rect, (windowButton ? 16 : 20) * scale), enabled ? ink : Color.FromArgb(77, ink));
        }
        return image;
    }

    private static void DrawRecordButton(Graphics g, RectangleF rect, BarLook look, float scale)
    {
        const BarItem item = BarItem.Record;
        bool enabled = !look.Disabled.Contains(item);
        Color fill = look.Recording
            ? StateColor(look, item, enabled, Palette.StopFill, Palette.StopHover, Palette.StopPressed)
            : StateColor(look, item, enabled, Palette.RecordFill, Palette.RecordHover, Palette.RecordPressed);
        int alpha = enabled ? 255 : 128;
        using (var back = new SolidBrush(Color.FromArgb(alpha, fill)))
        using (var pill = Shapes.Rounded(rect, rect.Height / 2))
            g.FillPath(back, pill);

        using var fonts = new Fonts(scale);
        float room = rect.Width - (PadLeft + PadRight) * scale;
        float content = look.Recording ? RecordingWidth(g, fonts, scale) : IdleWidth(g, fonts, scale, look.RecordHint);
        float x = rect.Left + PadLeft * scale + (room - content) / 2;
        if (look.Recording)
        {
            var white = Color.FromArgb(alpha, Color.White);
            Glyphs.Draw(g, Glyph.Stop, new RectangleF(x, rect.Top + (rect.Height - StopSize * scale) / 2, StopSize * scale, StopSize * scale), white);
            x += (StopSize + StopGap) * scale;
            DrawText(g, look.Elapsed, fonts.Timer, white, x, rect);
            return;
        }
        Glyphs.Draw(g, Glyph.Record, new RectangleF(x, rect.Top + (rect.Height - DotSize * scale) / 2, DotSize * scale, DotSize * scale), Color.FromArgb(alpha, Palette.RecordDot));
        x += (DotSize + DotGap) * scale;
        x += DrawText(g, RecordLabel, fonts.Label, Color.FromArgb(alpha, Palette.RecordInk), x, rect);
        if (look.RecordHint.Length > 0)
            DrawText(g, look.RecordHint, fonts.Hint, Color.FromArgb(alpha, Palette.RecordDot), x + HintGap * scale, rect);
    }

    private static float IdleWidth(Graphics g, Fonts fonts, float scale, string hint) =>
        (DotSize + DotGap) * scale + Measure(g, RecordLabel, fonts.Label)
        + (hint.Length > 0 ? HintGap * scale + Measure(g, hint, fonts.Hint) : 0);

    private static float RecordingWidth(Graphics g, Fonts fonts, float scale) =>
        (StopSize + StopGap) * scale + Measure(g, LongestTime, fonts.Timer);

    private static Color StateColor(BarLook look, BarItem item, bool enabled, Color normal, Color hover, Color pressed) =>
        !enabled ? normal : look.Pressed == item ? pressed : look.Hover == item ? hover : normal;

    private static float Measure(Graphics g, string text, Font font) =>
        g.MeasureString(text, font, PointF.Empty, TextFormat).Width;

    /// <summary>Draws one line vertically centered in the row; returns its width.</summary>
    private static float DrawText(Graphics g, string text, Font font, Color color, float x, RectangleF row)
    {
        float width = Measure(g, text, font);
        using var brush = new SolidBrush(color);
        g.DrawString(text, font, brush, new RectangleF(x, row.Top, width + 1, row.Height), TextFormat);
        return width;
    }

    private static RectangleF Centered(RectangleF box, float size) =>
        new(box.Left + (box.Width - size) / 2, box.Top + (box.Height - size) / 2, size, size);

    private sealed class Fonts(float scale) : IDisposable
    {
        public Font Label { get; } = new("Microsoft JhengHei UI", 13 * scale, FontStyle.Bold, GraphicsUnit.Pixel);
        public Font Hint { get; } = new("Microsoft JhengHei UI", 11 * scale, FontStyle.Regular, GraphicsUnit.Pixel);
        public Font Timer { get; } = new("Consolas", 13 * scale, FontStyle.Bold, GraphicsUnit.Pixel);

        public void Dispose()
        {
            Label.Dispose();
            Hint.Dispose();
            Timer.Dispose();
        }
    }
}
