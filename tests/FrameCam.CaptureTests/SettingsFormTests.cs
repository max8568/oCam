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

    [Theory]
    [InlineData("存檔資料夾")]
    [InlineData("錄影快捷鍵")]
    [InlineData("截圖快捷鍵")]
    [InlineData("選視窗快捷鍵")]
    [InlineData("每秒格數")]
    public void Each_name_is_on_the_left_of_its_field(string name) => Sta.Run(async () =>
    {
        using var dialog = await Show(new AppSettings(), false, false);
        var label = All<Label>(dialog).Single(l => l.Text == name);
        var field = Field(dialog, name);

        Assert.True(label.Right <= field.Left, $"{name}: the name ends at {label.Right}, the field starts at {field.Left}");
        Assert.InRange(label.Top + label.Height / 2, field.Top, field.Bottom);
    });

    [Fact]
    public void Fields_line_up_on_their_left_edge() => Sta.Run(async () =>
    {
        using var dialog = await Show(new AppSettings(), false, false);
        var lefts = new[] { "存檔資料夾", "錄影快捷鍵", "截圖快捷鍵", "選視窗快捷鍵" }.Select(n => Field(dialog, n).Left).Distinct();
        Assert.Single(lefts);
    });

    [Fact]
    public void A_failure_note_sits_under_its_field_without_covering_the_next_one() => Sta.Run(async () =>
    {
        using var dialog = await Show(new AppSettings(), recordHotkeyFailed: true, screenshotHotkeyFailed: true);
        var notes = All<Label>(dialog).Where(l => l.Visible && l.Text == FailedMessage).OrderBy(l => l.Top).ToArray();
        var record = Field(dialog, "錄影快捷鍵");
        var screenshot = Field(dialog, "截圖快捷鍵");
        var fps = Field(dialog, "每秒格數");

        Assert.Equal(2, notes.Length);
        Assert.Equal(record.Left, notes[0].Left - dialog.LogicalToDeviceUnits(12));
        Assert.InRange(notes[0].Top, record.Bottom, screenshot.Top - notes[0].Height);
        Assert.InRange(notes[1].Top, screenshot.Bottom, fps.Top - notes[1].Height);
    });

    [Fact]
    public void The_pick_window_key_is_shown_and_saved() => Sta.Run(async () =>
    {
        var original = new AppSettings { PickHotkey = "F6" };
        using var dialog = await Show(original, false, false);
        var box = All<HotkeyTextBox>(dialog).Single(b => b.AccessibleName == "選視窗快捷鍵");
        Assert.Equal(Keys.F6, box.Hotkey);

        box.Hotkey = Keys.F7;

        Assert.Equal("F7", dialog.ToSettings(original).PickHotkey);
    });

    [Fact]
    public void A_failed_pick_window_key_gets_its_note_above_the_frame_rate() => Sta.Run(async () =>
    {
        using var dialog = new SettingsForm(new AppSettings(), false, false, pickHotkeyFailed: true);
        dialog.Show();
        await Task.Delay(200);
        var note = All<Label>(dialog).Single(l => l.Visible && l.Text == FailedMessage);

        Assert.InRange(note.Top, Field(dialog, "選視窗快捷鍵").Bottom, Field(dialog, "每秒格數").Top - note.Height);
    });

    [Fact]
    public void Cancel_and_OK_sit_in_the_same_gray_bottom_bar_as_the_custom_sizes_dialog() => Sta.Run(async () =>
    {
        using var settings = await Show(new AppSettings(), false, false);
        using var sizes = new CustomSizesForm([], new PixelSize(1280, 720));
        sizes.Show();
        await Task.Delay(200);

        var bar = BottomBar(settings, "確定");
        Assert.Same(bar, All<Button>(settings).Single(b => b.Text == "取消").Parent);
        var sizesBar = BottomBar(sizes, "完成");
        Assert.Equal(sizesBar.Height, bar.Height);
        Assert.Equal(sizesBar.BackColor, bar.BackColor);
    });

    /// <summary>The bar holding the button: the dialog's full width, along its bottom edge, gray.</summary>
    private static Control BottomBar(Form dialog, string button)
    {
        var bar = All<Button>(dialog).Single(b => b.Text == button).Parent!;
        Assert.Same(dialog, bar.Parent);
        Assert.Equal(new Rectangle(0, dialog.ClientSize.Height - bar.Height, dialog.ClientSize.Width, bar.Height), bar.Bounds);
        Assert.NotEqual(dialog.BackColor, bar.BackColor);
        return bar;
    }

    private static async Task<SettingsForm> Show(AppSettings settings, bool recordHotkeyFailed, bool screenshotHotkeyFailed)
    {
        var dialog = new SettingsForm(settings, recordHotkeyFailed, screenshotHotkeyFailed);
        dialog.Show();
        await Task.Delay(200);
        return dialog;
    }

    /// <summary>The round field that holds the setting with this name.</summary>
    private static Control Field(Control dialog, string name) => name == "每秒格數"
        ? All<Button>(dialog).Single(b => b.Text == "30").Parent!
        : All<TextBox>(dialog).Single(b => b.AccessibleName == name).Parent!;
}
