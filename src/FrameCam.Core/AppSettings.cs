using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FrameCam.Core;

/// <summary>The region at exit, with the monitor it was on.</summary>
public sealed record SavedRegion(string DeviceName, int Left, int Top, int Width, int Height)
{
    public PixelRect Rect => new(Left, Top, Width, Height);
}

/// <summary>The control bar's top-left corner at exit, with the monitor it was on.</summary>
public sealed record SavedPosition(string DeviceName, int Left, int Top);

/// <summary>Everything stored in settings.json (spec 8.5). Hotkeys are text such as "F2" or "Ctrl+Shift+R".</summary>
public sealed record AppSettings
{
    public string OutputFolder { get; init; } = OutputDirectory.Default;
    public string RecordHotkey { get; init; } = "F2";
    public string ScreenshotHotkey { get; init; } = "F3";
    public string PickHotkey { get; init; } = "F6";
    public int FramesPerSecond { get; init; } = 60;
    public SavedRegion? LastRegion { get; init; }
    public SavedPosition? LastBarPosition { get; init; }

    /// <summary>The user's own sizes for the size menu, in the order they were added.</summary>
    public IReadOnlyList<CustomSize> CustomSizes { get; init; } = [];
}

public static class SettingsStore
{
    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // keep CJK folder names readable
        // Tolerate common hand edits.
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };

    /// <summary>settings.json in FrameCam's own folder, so the settings travel with a copied FrameCam folder.</summary>
    public static string PathIn(string appFolder) => Path.Combine(appFolder, "settings.json");

    /// <summary>Where earlier versions kept the settings; read once when there is no file beside the exe yet.</summary>
    public static string OldPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FrameCam", "settings.json");

    /// <summary>
    /// Reads the settings, from <paramref name="oldPath"/> when <paramref name="path"/> does not exist yet (the
    /// next save writes to <paramref name="path"/>). A missing file gives defaults and no error; an unreadable
    /// or invalid file gives defaults and a message for the user. Unknown fields are ignored; missing or
    /// invalid ones get defaults. An invalid file is first copied to settings.json.bad, because the next save
    /// overwrites it.
    /// </summary>
    public static (AppSettings Settings, string? Error) Load(string path, string? oldPath = null)
    {
        if (!File.Exists(path) && oldPath is not null && File.Exists(oldPath))
            path = oldPath;
        if (!File.Exists(path))
            return (new AppSettings(), null);
        try
        {
            var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path), Json)
                ?? throw new JsonException("settings.json is null");
            return (Normalize(settings), null);
        }
        catch (JsonException e)
        {
            string backup = path + ".bad";
            try
            {
                File.Copy(path, backup, overwrite: true);
            }
            catch (Exception copy) when (copy is IOException or UnauthorizedAccessException)
            {
                return (new AppSettings(), $"設定檔讀取失敗，改用預設值：{e.Message}");
            }
            return (new AppSettings(), $"設定檔讀取失敗，改用預設值，原本的檔案另存為 {Path.GetFileName(backup)}：{e.Message}");
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return (new AppSettings(), $"設定檔讀取失敗，改用預設值：{e.Message}");
        }
    }

    /// <summary>
    /// Writes the settings. The default save folder is written blank, which loads as each person's own
    /// Videos\FrameCam: the file may be shared with other people, and a path into this person's profile would
    /// not work for them.
    /// </summary>
    public static void Save(string path, AppSettings settings)
    {
        if (string.Equals(Path.TrimEndingDirectorySeparator(settings.OutputFolder), OutputDirectory.Default, StringComparison.OrdinalIgnoreCase))
            settings = settings with { OutputFolder = "" };
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(settings, Json));
    }

    private static AppSettings Normalize(AppSettings s)
    {
        var defaults = new AppSettings();
        return s with
        {
            OutputFolder = string.IsNullOrWhiteSpace(s.OutputFolder) ? defaults.OutputFolder : s.OutputFolder,
            RecordHotkey = string.IsNullOrWhiteSpace(s.RecordHotkey) ? defaults.RecordHotkey : s.RecordHotkey,
            ScreenshotHotkey = string.IsNullOrWhiteSpace(s.ScreenshotHotkey) ? defaults.ScreenshotHotkey : s.ScreenshotHotkey,
            PickHotkey = string.IsNullOrWhiteSpace(s.PickHotkey) ? defaults.PickHotkey : s.PickHotkey,
            FramesPerSecond = s.FramesPerSecond is 30 or 60 ? s.FramesPerSecond : defaults.FramesPerSecond,
            CustomSizes = ValidCustomSizes(s.CustomSizes),
        };
    }

    /// <summary>Drops entries a hand edit broke and keeps the first MaxCount. No entries gives the shared empty list.</summary>
    private static IReadOnlyList<CustomSize> ValidCustomSizes(IReadOnlyList<CustomSize?>? sizes)
    {
        var valid = (sizes ?? [])
            .OfType<CustomSize>()
            .Select(c => c with { Label = (c.Label ?? "").Trim() })
            .Where(c => c.IsValid)
            .Take(CustomSize.MaxCount)
            .ToArray();
        return valid.Length == 0 ? [] : valid;
    }
}
