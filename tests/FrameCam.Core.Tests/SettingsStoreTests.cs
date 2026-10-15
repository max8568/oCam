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
        Assert.Equal("F6", settings.PickHotkey);
        Assert.Equal(60, settings.FramesPerSecond);
        Assert.Null(settings.LastRegion);
        Assert.Empty(settings.CustomSizes);
    }

    [Fact]
    public void Custom_sizes_load_back_in_order()
    {
        string path = NewPath();
        CustomSize[] sizes = [new("直播", 1600, 900), new("", 800, 600)];
        SettingsStore.Save(path, new AppSettings { CustomSizes = sizes });
        var (loaded, error) = SettingsStore.Load(path);
        Assert.Null(error);
        Assert.Equal(sizes, loaded.CustomSizes);
    }

    [Fact]
    public void Invalid_custom_sizes_from_a_hand_edit_are_dropped()
    {
        var (settings, error) = SettingsStore.Load(WriteFile(
            """
            { "CustomSizes": [
                { "Label": "ok", "Width": 800, "Height": 600 },
                { "Label": "odd", "Width": 801, "Height": 600 },
                { "Label": null, "Width": 640, "Height": 480 },
                null,
                { "Label": "tiny", "Width": 2, "Height": 2 }
            ] }
            """));
        Assert.Null(error);
        Assert.Equal([new CustomSize("ok", 800, 600), new CustomSize("", 640, 480)], settings.CustomSizes);
    }

    [Fact]
    public void At_most_20_custom_sizes_are_kept()
    {
        string path = NewPath();
        var sizes = Enumerable.Range(1, 25).Select(i => new CustomSize($"s{i}", 100 + 2 * i, 100)).ToArray();
        SettingsStore.Save(path, new AppSettings { CustomSizes = sizes });
        var (loaded, _) = SettingsStore.Load(path);
        Assert.Equal(sizes.Take(20), loaded.CustomSizes);
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

    [Fact]
    public void A_blank_pick_window_key_gets_the_default()
    {
        var (settings, _) = SettingsStore.Load(WriteFile("""{ "PickHotkey": " " }"""));
        Assert.Equal("F6", settings.PickHotkey);
    }

    [Fact]
    public void Settings_sit_in_the_FrameCam_folder()
    {
        Assert.Equal(@"D:\Tools\FrameCam\settings.json", SettingsStore.PathIn(@"D:\Tools\FrameCam\"));
    }

    [Fact]
    public void Without_a_file_beside_the_exe_the_old_file_is_read()
    {
        string old = WriteFile("""{ "RecordHotkey": "F3", "ScreenshotHotkey": "F4" }""");
        var (settings, error) = SettingsStore.Load(NewPath(), old);
        Assert.Null(error);
        Assert.Equal("F3", settings.RecordHotkey);
        Assert.Equal("F4", settings.ScreenshotHotkey);
    }

    [Fact]
    public void The_file_beside_the_exe_wins_over_the_old_one()
    {
        string old = WriteFile("""{ "RecordHotkey": "F3" }""");
        string beside = WriteFile("""{ "RecordHotkey": "F7" }""");
        var (settings, _) = SettingsStore.Load(beside, old);
        Assert.Equal("F7", settings.RecordHotkey);
    }

    [Fact]
    public void The_default_save_folder_is_stored_as_blank_so_a_shared_file_fits_everyone()
    {
        string path = NewPath();
        SettingsStore.Save(path, new AppSettings { OutputFolder = OutputDirectory.Default });

        Assert.Contains("\"OutputFolder\": \"\"", File.ReadAllText(path));
        Assert.Equal(OutputDirectory.Default, SettingsStore.Load(path).Settings.OutputFolder);
    }

    [Fact]
    public void A_chosen_save_folder_is_stored_as_it_is()
    {
        string path = NewPath();
        SettingsStore.Save(path, new AppSettings { OutputFolder = @"D:\錄影" });

        Assert.Equal(@"D:\錄影", SettingsStore.Load(path).Settings.OutputFolder);
    }
}
