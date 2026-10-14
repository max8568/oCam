using FrameCam.Core;
using FrameCam.Native;
using FrameCam.UI;
using static FrameCam.CaptureTests.ControlTree;

namespace FrameCam.CaptureTests;

/// <summary>The control bar with its frame, on screen. Uses hotkeys nobody else is likely to own.</summary>
public class ControlBarTests
{
    private const Keys RecordKey = Keys.Control | Keys.Alt | Keys.Shift | Keys.F11;

    [Fact]
    public void Closing_the_frame_ends_the_program() => Sta.Run(async () =>
    {
        var bar = new ControlBarForm(NewSettingsFile());
        bar.Show();
        await Task.Delay(300);
        bar.Frame.Close(); // what Alt+F4 on the frame does
        await Task.Delay(300);
        Assert.True(bar.IsDisposed);
        Assert.True(bar.Frame.IsDisposed);
    });

    [Fact]
    public void Hotkeys_are_released_while_the_settings_dialog_is_open() => Sta.Run(async () =>
    {
        var bar = new ControlBarForm(NewSettingsFile());
        try
        {
            bar.Show();
            await Task.Delay(300);
            Assert.False(CanRegister(RecordKey)); // the bar owns it

            bool freeDuringDialog = false;
            SynchronizationContext.Current!.Post(_ =>
            {
                freeDuringDialog = CanRegister(RecordKey);
                Application.OpenForms.OfType<SettingsForm>().Single().Close();
            }, null);
            bar.OpenSettings(); // returns when the posted callback has closed the dialog

            Assert.True(freeDuringDialog);
            Assert.False(CanRegister(RecordKey)); // registered again after Cancel
        }
        finally
        {
            bar.Close();
        }
    });

    [Fact]
    public void Settings_dialog_is_not_covered_by_the_control_bar() => Sta.Run(async () =>
    {
        var bar = new ControlBarForm(NewSettingsFile());
        try
        {
            bar.Show();
            await Task.Delay(300);
            Rectangle overlap = Rectangle.Empty;
            IntPtr dialogHandle = IntPtr.Zero, windowOnTop = IntPtr.Zero;
            SynchronizationContext.Current!.Post(async _ =>
            {
                var dialog = Application.OpenForms.OfType<SettingsForm>().Single();
                await Task.Delay(300);
                overlap = Rectangle.Intersect(dialog.Bounds, bar.Bounds);
                dialogHandle = dialog.Handle;
                windowOnTop = TestNative.TopLevelWindowAt(new Point(overlap.X + overlap.Width / 2, overlap.Y + overlap.Height / 2));
                dialog.Close();
            }, null);
            bar.OpenSettings(); // returns when the posted callback has closed the dialog

            Assert.False(overlap.IsEmpty); // the dialog opens over the bar
            Assert.Equal(dialogHandle, windowOnTop);
        }
        finally
        {
            bar.Close();
        }
    });

    [Fact]
    public void Clicks_on_the_see_through_corner_reach_the_window_below() => Sta.Run(async () =>
    {
        var bar = new ControlBarForm(NewSettingsFile());
        try
        {
            bar.Show();
            await Task.Delay(300);
            Assert.Equal(bar.Handle, TestNative.TopLevelWindowAt(bar.PointToScreen(Center(bar.BarLayout[BarItem.Settings]))));
            Assert.NotEqual(bar.Handle, TestNative.TopLevelWindowAt(bar.Location)); // outside the round end
        }
        finally
        {
            bar.Close();
        }
    });

    [Fact]
    public void Clicking_the_settings_icon_opens_the_settings_dialog() => Sta.Run(async () =>
    {
        var bar = new ControlBarForm(NewSettingsFile());
        try
        {
            bar.Show();
            await Task.Delay(300);
            bool opened = false;
            SynchronizationContext.Current!.Post(async _ =>
            {
                await Task.Delay(300);
                var dialog = Application.OpenForms.OfType<SettingsForm>().SingleOrDefault();
                opened = dialog is not null;
                dialog?.Close();
            }, null);
            TestNative.Click(bar.Handle, Center(bar.BarLayout[BarItem.Settings])); // returns when the dialog has closed

            Assert.True(opened);
        }
        finally
        {
            bar.Close();
        }
    });

    [Fact]
    public void Clicking_the_close_icon_ends_the_program() => Sta.Run(async () =>
    {
        var bar = new ControlBarForm(NewSettingsFile());
        bar.Show();
        await Task.Delay(300);
        TestNative.Click(bar.Handle, Center(bar.BarLayout[BarItem.Close]));
        await Task.Delay(300);
        Assert.True(bar.IsDisposed);
    });

    [Fact]
    public void Without_a_message_there_is_no_status_bubble_and_the_record_button_shows_its_hotkey() => Sta.Run(async () =>
    {
        var bar = new ControlBarForm(NewSettingsFile());
        try
        {
            bar.Show();
            await Task.Delay(300);
            Assert.False(bar.StatusBubble.Visible);
            Assert.Equal(HotkeyText.Format(RecordKey), bar.Look.RecordHint);
        }
        finally
        {
            bar.Close();
        }
    });

    [Fact]
    public void A_taken_record_hotkey_is_reported_under_the_bar_and_left_off_the_record_button() => Sta.Run(async () =>
    {
        using var taken = new HotkeyManager();
        Assert.True(taken.Register(RecordKey, () => { }));
        var bar = new ControlBarForm(NewSettingsFile());
        try
        {
            bar.Show();
            await Task.Delay(300);
            Assert.True(bar.StatusBubble.Visible);
            Assert.Contains("快捷鍵登記失敗", bar.StatusBubble.Message);
            Assert.True(bar.StatusBubble.Top >= bar.Top + bar.BarLayout.Bar.Bottom);
            Assert.Equal("", bar.Look.RecordHint);
        }
        finally
        {
            bar.Close();
        }
    });

    [Fact]
    public void Status_bubble_moves_with_the_bar_and_is_left_out_of_screen_captures() => Sta.Run(async () =>
    {
        using var taken = new HotkeyManager();
        Assert.True(taken.Register(RecordKey, () => { })); // gives the bar a message to show
        var bar = new ControlBarForm(NewSettingsFile());
        try
        {
            bar.Show();
            var area = Screen.FromControl(bar).WorkingArea;
            bar.Location = new Point(area.Left + area.Width / 4, area.Top + area.Height / 4); // away from the edges, where the bubble is pushed back on screen
            await Task.Delay(300);
            var bubble = bar.StatusBubble;
            var before = bubble.Location;
            bar.Location = new Point(bar.Left - 40, bar.Top + 30);
            await Task.Delay(100);

            Assert.Equal(new Point(before.X - 40, before.Y + 30), bubble.Location);
            Assert.Equal(TestNative.WDA_EXCLUDEFROMCAPTURE, TestNative.DisplayAffinity(bubble.Handle));
        }
        finally
        {
            bar.Close();
        }
    });

    [Fact]
    public void Showing_the_status_bubble_leaves_the_bar_active() => Sta.Run(async () =>
    {
        var bar = new ControlBarForm(NewSettingsFile());
        HotkeyManager? taken = null;
        try
        {
            bar.Show();
            await Task.Delay(300);
            SynchronizationContext.Current!.Post(_ =>
            {
                taken = new HotkeyManager(); // the bar released its keys for the dialog; take one so it fails afterwards
                taken.Register(RecordKey, () => { });
                Application.OpenForms.OfType<SettingsForm>().Single().Close();
            }, null);
            bar.OpenSettings(); // registers the keys again and shows the failure under the bar
            await Task.Delay(300);

            Assert.True(bar.StatusBubble.Visible);
            Assert.Same(bar, Form.ActiveForm); // so the arrow keys still reach it
        }
        finally
        {
            taken?.Dispose();
            bar.Close();
        }
    });

    [Fact]
    public void A_longer_record_hotkey_keeps_the_right_end_of_the_bar_in_place() => Sta.Run(async () =>
    {
        var bar = new ControlBarForm(NewSettingsFile());
        try
        {
            bar.Show();
            await Task.Delay(300);
            int right = bar.Left + (int)bar.BarLayout.Bar.Right;
            float width = bar.BarLayout.Bar.Width;
            SynchronizationContext.Current!.Post(_ =>
            {
                var dialog = Application.OpenForms.OfType<SettingsForm>().Single();
                All<HotkeyTextBox>(dialog).Single(b => b.AccessibleName == "錄影快捷鍵").Hotkey = Keys.Control | Keys.Alt | Keys.Shift | Keys.Scroll;
                dialog.DialogResult = DialogResult.OK;
            }, null);
            bar.OpenSettings();
            await Task.Delay(300);

            Assert.True(bar.BarLayout.Bar.Width > width);
            Assert.InRange(bar.Left + (int)bar.BarLayout.Bar.Right, right - 1, right + 1);
        }
        finally
        {
            bar.Close();
        }
    });

    private static Point Center(RectangleF r) => new((int)(r.Left + r.Width / 2), (int)(r.Top + r.Height / 2));

    private static bool CanRegister(Keys keys)
    {
        using var probe = new HotkeyManager();
        return probe.Register(keys, () => { });
    }

    private static string NewSettingsFile()
    {
        string folder = Directory.CreateTempSubdirectory("framecam 測試 bar ").FullName;
        string path = Path.Combine(folder, "settings.json");
        SettingsStore.Save(path, new AppSettings
        {
            OutputFolder = folder,
            RecordHotkey = HotkeyText.Format(RecordKey),
            ScreenshotHotkey = HotkeyText.Format(Keys.Control | Keys.Alt | Keys.Shift | Keys.F12),
        });
        return path;
    }
}
