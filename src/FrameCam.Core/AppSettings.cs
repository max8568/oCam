using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FrameCam.Core;

/// <summary>The region at exit, with the monitor it was on.</summary>
public sealed record SavedRegion(string DeviceName, int Left, int Top, int Width, int Height)
{
    public PixelRect Rect => new(Left, Top, Width, Height);
}

/// <summary>Everything stored in settings.json (spec 8.5). Hotkeys are text such as "F2" or "Ctrl+Shift+R".</summary>
public sealed record AppSettings
{
    public string OutputFolder { get; init; } = OutputDirectory.Default;
    public string RecordHotkey { get; init; } = "F2";
    public string ScreenshotHotkey { get; init; } = "F3";
    public int FramesPerSecond { get; init; } = 60;
    public SavedRegion? LastRegion { get; init; }

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

    public static string DefaultPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FrameCam", "settings.json");

    /// <summary>
    /// Reads the settings. A missing file gives defaults and no error; an unreadable or invalid file gives
    /// defaults and a message for the user. Unknown fields are ignored; missing or invalid ones get defaults.
    /// An invalid file is first copied to settings.json.bad, because the next save overwrites it.
    /// </summary>
    public static (AppSettings Settings, string? Error) Load(string path)
    {
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

    public static void Save(string path, AppSettings settings)
    {
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
