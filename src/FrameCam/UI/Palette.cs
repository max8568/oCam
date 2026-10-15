namespace FrameCam.UI;

/// <summary>Colors of the control bar and the settings dialog (light theme, design 21).</summary>
internal static class Palette
{
    public static readonly Color Surface = Color.White;
    public static readonly Color Border = Color.FromArgb(0xE2, 0xE8, 0xF0);
    public static readonly Color Ink = Color.FromArgb(0x33, 0x41, 0x55);
    public static readonly Color Strong = Color.FromArgb(0x0F, 0x17, 0x2A);
    public static readonly Color Muted = Color.FromArgb(0x64, 0x74, 0x8B);
    public static readonly Color Dots = Color.FromArgb(0x94, 0xA3, 0xB8);
    public static readonly Color Hover = Color.FromArgb(0xF1, 0xF5, 0xF9);
    public static readonly Color Pressed = Color.FromArgb(0xE2, 0xE8, 0xF0);
    public static readonly Color Shadow = Color.FromArgb(0x0F, 0x17, 0x2A);

    public static readonly Color RecordFill = Color.FromArgb(0xFE, 0xE2, 0xE2);
    public static readonly Color RecordHover = Color.FromArgb(0xFE, 0xCA, 0xCA);
    public static readonly Color RecordPressed = Color.FromArgb(0xFC, 0xA5, 0xA5);
    public static readonly Color RecordInk = Color.FromArgb(0xB9, 0x1C, 0x1C);
    public static readonly Color RecordDot = Color.FromArgb(0xEF, 0x44, 0x44);
    public static readonly Color StopFill = Color.FromArgb(0xDC, 0x26, 0x26);
    public static readonly Color StopHover = Color.FromArgb(0xB9, 0x1C, 0x1C);
    public static readonly Color StopPressed = Color.FromArgb(0x99, 0x1B, 0x1B);

    public static readonly Color Band = Color.FromArgb(0xF8, 0xFA, 0xFC); // a dialog's bottom bar
    public static readonly Color Field = Color.FromArgb(0xF1, 0xF5, 0xF9);
    public static readonly Color FieldButton = Color.FromArgb(0xE2, 0xE8, 0xF0);
    public static readonly Color FieldButtonHover = Color.FromArgb(0xCB, 0xD5, 0xE1);
    public static readonly Color ErrorField = Color.FromArgb(0xFE, 0xF2, 0xF2);
    public static readonly Color ErrorBorder = Color.FromArgb(0xF8, 0x71, 0x71);
    public static readonly Color ErrorText = Color.FromArgb(0xDC, 0x26, 0x26);
    public static readonly Color Accent = Color.FromArgb(0x25, 0x63, 0xEB); // the dialogs' main buttons: blue
    public static readonly Color AccentHover = Color.FromArgb(0x20, 0x55, 0xCA);
    public static readonly Color AccentPressed = Color.FromArgb(0x1B, 0x47, 0xA9);
}
