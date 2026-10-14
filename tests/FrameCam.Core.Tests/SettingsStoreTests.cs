using FrameCam.Core;

namespace FrameCam.Core.Tests;

public class SettingsStoreTests
{
    private static string NewPath() =>
        Path.Combine(Directory.CreateTempSubdirectory("framecam-").FullName, "FrameCam", "settings.json");

    private static string WriteFile(string json)
    {
        string path = NewPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, json);
        return path;
    }

    [Fact]
    public void Missing_file_gives_defaults_without_an_error()
    {
        var (settings, error) = SettingsStore.Load(NewPath());
        Assert.Equal(new AppSettings(), settings);
        Assert.Null(error);
    }

    [Fact]
    public void Defaults_match_the_spec()
    {
        var settings = new AppSettings();
        Assert.Equal(OutputDirectory.Default, settings.OutputFolder);
        Assert.Equal("F2", settings.RecordHotkey);
        Assert.Equal("F3", settings.ScreenshotHotkey);
        Assert.Equal(60, settings.FramesPerSecond);
        Assert.Null(settings.LastRegion);
    }

    [Fact]
    public void Saved_settings_load_back()
    {
        string path = NewPath();
        var saved = new AppSettings
        {
            OutputFolder = @"D:\錄影",
            RecordHotkey = "Ctrl+F9",
            ScreenshotHotkey = "F10",
            FramesPerSecond = 30,
            LastRegion = new SavedRegion(@"\\.\DISPLAY1", 1921, 33, 640, 360),
        };
        SettingsStore.Save(path, saved);
        var (loaded, error) = SettingsStore.Load(path);
        Assert.Null(error);
        Assert.Equal(saved, loaded);
    }

    [Fact]
    public void Broken_file_gives_defaults_and_an_error()
    {
        var (settings, error) = SettingsStore.Load(WriteFile("{ not json"));
        Assert.Equal(new AppSettings(), settings);
        Assert.NotNull(error);
    }

    [Fact]
    public void Broken_file_is_kept_as_settings_json_bad_so_the_next_save_does_not_lose_it()
    {
        string path = WriteFile("{ not json");
        var (_, error) = SettingsStore.Load(path);
        Assert.Equal("{ not json", File.ReadAllText(path + ".bad"));
        Assert.Contains("settings.json.bad", error);
    }

    [Fact]
    public void Common_hand_edits_still_load()
    {
        var (settings, error) = SettingsStore.Load(WriteFile(
            """
            {
              // edited by hand
              "outputFolder": "D:\\錄影",
              "RecordHotkey": "F6",
              "FramesPerSecond": "30",
            }
            """));
        Assert.Null(error);
        Assert.Equal(new AppSettings { OutputFolder = @"D:\錄影", RecordHotkey = "F6", FramesPerSecond = 30 }, settings);
    }

    [Fact]
    public void Unknown_fields_are_ignored_and_missing_or_invalid_ones_use_defaults()
    {
        var (settings, error) = SettingsStore.Load(WriteFile(
            """{ "RecordHotkey": "F6", "FramesPerSecond": 45, "SomethingNew": true, "OutputFolder": "" }"""));
        Assert.Null(error);
        Assert.Equal(new AppSettings { RecordHotkey = "F6" }, settings);
    }
}
