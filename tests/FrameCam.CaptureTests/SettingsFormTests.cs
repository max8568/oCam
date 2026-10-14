using FrameCam.Core;
using FrameCam.UI;
using static FrameCam.CaptureTests.ControlTree;

namespace FrameCam.CaptureTests;

/// <summary>The settings dialog, shown on screen.</summary>
public class SettingsFormTests
{
    private const string FailedMessage = "登記失敗，可能被其他程式使用";

    [Fact]
    public void Picking_30_frames_per_second_is_saved() => Sta.Run(async () =>
    {
        var original = new AppSettings { FramesPerSecond = 60 };
        using var dialog = new SettingsForm(original, false, false);
        dialog.Show();
        await Task.Delay(200);

        All<Button>(dialog).Single(b => b.Text == "30").PerformClick();

        Assert.Equal(30, dialog.ToSettings(original).FramesPerSecond);
    });

    [Fact]
    public void A_failed_hotkey_shows_a_message_until_another_key_is_picked() => Sta.Run(async () =>
    {
        using var dialog = new SettingsForm(new AppSettings(), recordHotkeyFailed: false, screenshotHotkeyFailed: true);
        dialog.Show();
        await Task.Delay(200);
        Assert.Single(All<Label>(dialog), l => l.Visible && l.Text == FailedMessage);

        var box = All<HotkeyTextBox>(dialog).Single(b => b.AccessibleName == "截圖快捷鍵");
        box.Hotkey = Keys.F3; // the same key again: it still fails
        Assert.Single(All<Label>(dialog), l => l.Visible && l.Text == FailedMessage);
        box.Hotkey = Keys.F9;

        Assert.DoesNotContain(All<Label>(dialog), l => l.Visible && l.Text == FailedMessage);
        Assert.Equal("F9", dialog.ToSettings(new AppSettings()).ScreenshotHotkey);
    });
}
