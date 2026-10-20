using FrameCam.Core;
using FrameCam.Native;
using FrameCam.UI;
using static FrameCam.CaptureTests.ControlTree;

namespace FrameCam.CaptureTests;

/// <summary>The control bar with its frame, on screen. Uses hotkeys nobody else is likely to own.</summary>
public class ControlBarTests
{
    private const Keys RecordKey = Keys.Control | Keys.Alt | Keys.Shift | Keys.F11;
    private const Keys PickKey = Keys.Control | Keys.Alt | Keys.Shift | Keys.F10;
    private const Keys PauseKey = Keys.Control | Keys.Alt | Keys.Shift | Keys.F8; // F9 is taken on the dev machine

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
    public void The_pick_window_key_belongs_to_the_bar_while_it_is_open() => Sta.Run(async () =>
    {
        var bar = new ControlBarForm(NewSettingsFile());
        try
        {
            bar.Show();
            await Task.Delay(300);
            Assert.False(CanRegister(PickKey));
        }
        finally
        {
            bar.Close();
        }
        Assert.True(CanRegister(PickKey));
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
    public void Clicking_the_help_icon_opens_the_manual_with_the_hotkeys_paused() => Sta.Run(async () =>
    {
        var bar = new ControlBarForm(NewSettingsFile());
        try
        {
            bar.Show();
            await Task.Delay(300);
            bool opened = false, keyFree = false;
            SynchronizationContext.Current!.Post(async _ =>
            {
                await Task.Delay(300);
                var dialog = Application.OpenForms.OfType<HelpForm>().SingleOrDefault();
                opened = dialog is not null;
                keyFree = CanRegister(RecordKey); // otherwise the record key could record the manual
                dialog?.Close();
            }, null);
            TestNative.Click(bar.Handle, Center(bar.BarLayout[BarItem.Help])); // returns when the dialog has closed

            Assert.True(opened);
            Assert.True(keyFree);
            Assert.False(CanRegister(RecordKey)); // the bar has it again
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
    public void The_bar_has_a_taskbar_button_that_minimizes_it() => Sta.Run(async () =>
    {
        var bar = new ControlBarForm(NewSettingsFile());
        try
        {
            bar.Show();
            await Task.Delay(300);
            Assert.False(TestNative.IsToolWindow(bar.Handle));
            Assert.True(TestNative.HasMinimizeBox(bar.Handle));
        }
        finally
        {
            bar.Close();
        }
    });

    [Fact]
    public void The_frame_hides_while_the_bar_is_minimized_and_comes_back_with_it() => Sta.Run(async () =>
    {
        var bar = new ControlBarForm(NewSettingsFile());
        try
        {
            bar.Show();
            await Task.Delay(300);
            var region = bar.Frame.CaptureRegion;

            TestNative.Click(bar.Handle, Center(bar.BarLayout[BarItem.Minimize]));
            await Task.Delay(300);
            Assert.Equal(FormWindowState.Minimized, bar.WindowState);
            Assert.False(bar.Frame.Visible);

            TestNative.Restore(bar.Handle);
            await Task.Delay(300);
            Assert.True(bar.Frame.Visible);
            Assert.Equal(region, bar.Frame.CaptureRegion);

            TestNative.Minimize(bar.Handle); // clicking the taskbar button, or Win+M
            await Task.Delay(300);
            Assert.False(bar.Frame.Visible);
        }
        finally
        {
            bar.Close();
        }
    });

    [Fact]
    public void The_bar_comes_back_in_front_of_the_frame() => Sta.Run(async () =>
    {
        var bar = new ControlBarForm(NewSettingsFile());
        try
        {
            bar.Show();
            await Task.Delay(300);
            var label = bar.Frame.CurrentLayout.Label;
            var button = Center(bar.BarLayout[BarItem.Settings]);
            bar.Location = new Point(label.Left + label.Width / 2 - button.X, label.Top + label.Height / 2 - button.Y); // the button on the size label
            await Task.Delay(300);
            var point = bar.PointToScreen(button);
            Assert.Equal(bar.Handle, TestNative.TopLevelWindowAt(point));

            bar.Hide();
            Assert.Equal(bar.Frame.Handle, TestNative.TopLevelWindowAt(point)); // the frame is under the button
            bar.Show();
            await Task.Delay(300);

            TestNative.Minimize(bar.Handle);
            await Task.Delay(300);
            TestNative.Restore(bar.Handle);
            await Task.Delay(300);
            Assert.Equal(bar.Handle, TestNative.TopLevelWindowAt(point));

            TestNative.Minimize(bar.Handle);
            await Task.Delay(300);
            bar.ToggleRecording(); // the record key brings it back too
            await Task.Delay(300);
            Assert.Equal(bar.Handle, TestNative.TopLevelWindowAt(point));
        }
        finally
        {
            bar.Close();
            await WaitFor(() => bar.IsDisposed);
        }
    });

    [Fact]
    public void The_bar_cannot_be_minimized_while_recording() => Sta.Run(async () =>
    {
        var bar = new ControlBarForm(NewSettingsFile());
        try
        {
            bar.Show();
            await Task.Delay(300);
            bar.ToggleRecording();
            Assert.Contains(BarItem.Minimize, bar.Look.Disabled);
            await WaitFor(() => !bar.Look.Disabled.Contains(BarItem.Pause));

            TestNative.Minimize(bar.Handle); // clicking the taskbar button, or Win+M
            await WaitFor(() => bar.WindowState == FormWindowState.Normal);
            Assert.True(bar.Frame.Visible);

            bar.ToggleRecording();
            await WaitFor(() => !bar.Look.Recording);
            Assert.DoesNotContain(BarItem.Minimize, bar.Look.Disabled);
        }
        finally
        {
            bar.Close();
            await WaitFor(() => bar.IsDisposed);
        }
    });

    [Fact]
    public void The_record_key_brings_a_minimized_bar_back_and_records() => Sta.Run(async () =>
    {
        var bar = new ControlBarForm(NewSettingsFile());
        try
        {
            bar.Show();
            await Task.Delay(300);
            TestNative.Minimize(bar.Handle);
            await Task.Delay(300);

            bar.ToggleRecording(); // what the record key does
            Assert.Equal(FormWindowState.Normal, bar.WindowState);
            Assert.True(bar.Frame.Visible);
            await WaitFor(() => !bar.Look.Disabled.Contains(BarItem.Pause));
        }
        finally
        {
            bar.Close();
            await WaitFor(() => bar.IsDisposed);
        }
    });

    [Fact]
    public void The_pick_window_key_brings_a_minimized_bar_back() => Sta.Run(async () =>
    {
        var bar = new ControlBarForm(NewSettingsFile());
        try
        {
            bar.Show();
            await Task.Delay(300);
            TestNative.Minimize(bar.Handle);
            await Task.Delay(300);

            SynchronizationContext.Current!.Post(_ => Application.OpenForms.OfType<WindowPicker>().Single().Close(), null);
            bar.PickWindow(); // what the pick window key does; returns when the posted callback has closed the picker
            Assert.Equal(FormWindowState.Normal, bar.WindowState);
            Assert.True(bar.Frame.Visible);
        }
        finally
        {
            bar.Close();
        }
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
    public void Showing_the_status_bubble_does_not_take_the_keyboard() => Sta.Run(async () =>
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
            // It must not take the keyboard from the bar. (Whether the bar is active depends on what else is in
            // front, for example while someone uses the computer during the test.)
            Assert.NotSame(bar.StatusBubble, Form.ActiveForm);
        }
        finally
        {
            taken?.Dispose();
            bar.Close();
        }
    });

    [Fact]
    public void Showing_the_magnifier_does_not_take_the_keyboard() => Sta.Run(async () =>
    {
        var bar = new ControlBarForm(NewSettingsFile());
        try
        {
            bar.Show();
            await Task.Delay(300);

            bar.Frame.TryHandleArrow(Keys.Right); // what an arrow press does; it shows the magnifier
            await Task.Delay(100);

            var magnifier = Application.OpenForms.OfType<MagnifierWindow>().Single(); // listed once it has been shown
            Assert.True(magnifier.Visible);
            Assert.NotSame(magnifier, Form.ActiveForm); // so the next arrow press still reaches the bar or the frame
        }
        finally
        {
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

    private const string ManageSizes = "自訂尺寸…";

    [Fact]
    public void Settings_from_the_old_place_are_used_and_then_saved_in_the_new_one() => Sta.Run(async () =>
    {
        string old = NewSettingsFile(new CustomSize("直播", 1600, 900));
        string beside = Path.Combine(Directory.CreateTempSubdirectory("framecam 測試 exe ").FullName, "settings.json");
        var bar = new ControlBarForm(beside, old);
        try
        {
            bar.Show();
            await Task.Delay(300);
            using var menu = bar.BuildSizeMenu();
            Assert.Contains(menu.Items.Cast<ToolStripItem>(), i => i.Text == "直播（1600 × 900）");
        }
        finally
        {
            bar.Close();
        }

        Assert.Equal([new CustomSize("直播", 1600, 900)], SettingsStore.Load(beside).Settings.CustomSizes);
    });

    [Fact]
    public void The_bar_opens_where_it_was_when_it_was_closed() => Sta.Run(async () =>
    {
        string path = NewSettingsFile();
        var bar = new ControlBarForm(path);
        bar.Show();
        await Task.Delay(300);
        var moved = new Point(bar.Left - 200, bar.Top + 150); // what dragging it does
        bar.Location = moved;
        bar.Close();
        await Task.Delay(300);

        var again = new ControlBarForm(path);
        try
        {
            again.Show();
            await Task.Delay(300);
            Assert.Equal(moved, again.Location);
        }
        finally
        {
            again.Close();
        }
    });

    [Fact]
    public void A_bar_closed_while_minimized_opens_where_it_was() => Sta.Run(async () =>
    {
        string path = NewSettingsFile();
        var bar = new ControlBarForm(path);
        bar.Show();
        await Task.Delay(300);
        var moved = new Point(bar.Left - 200, bar.Top + 150);
        bar.Location = moved;
        TestNative.Minimize(bar.Handle);
        await Task.Delay(300);
        bar.Close(); // from the taskbar button's menu
        await Task.Delay(300);

        var again = new ControlBarForm(path);
        try
        {
            again.Show();
            await Task.Delay(300);
            Assert.Equal(moved, again.Location);
        }
        finally
        {
            again.Close();
        }
    });

    [Fact]
    public void The_size_menu_is_white_with_round_corners_and_a_light_gray_row_under_the_mouse() => Sta.Run(async () =>
    {
        var bar = new ControlBarForm(NewSettingsFile());
        try
        {
            bar.Show();
            await Task.Delay(300);
            using var menu = bar.BuildSizeMenu();
            menu.Show(bar, new Point(20, 40));
            await Task.Delay(300);
            var items = menu.Items.OfType<ToolStripMenuItem>().Where(i => i.Enabled).ToArray();
            items[0].Select();
            await Task.Delay(200);

            var hovered = menu.RectangleToScreen(items[0].Bounds);
            var other = menu.RectangleToScreen(items[1].Bounds);
            int x = hovered.Right - 16; // right of the text, which is short
            Assert.Equal(Palette.Hover.ToArgb(), ScreenPixel(x, hovered.Top + hovered.Height / 2).ToArgb());
            Assert.Equal(Palette.Surface.ToArgb(), ScreenPixel(x, other.Top + other.Height / 2).ToArgb());
            Assert.Equal(0, NativeMethods.DwmGetWindowAttribute(menu.Handle, NativeMethods.DWMWA_WINDOW_CORNER_PREFERENCE, out int corners, sizeof(int)));
            Assert.Equal(NativeMethods.DWMWCP_ROUND, corners);
            menu.Close();
        }
        finally
        {
            bar.Close();
        }
    });

    [Fact]
    public void Size_menu_text_is_in_the_middle_of_its_row() => Sta.Run(async () =>
    {
        var bar = new ControlBarForm(NewSettingsFile(new CustomSize("直播", 1600, 900)));
        try
        {
            bar.Show();
            await Task.Delay(300);
            using var menu = bar.BuildSizeMenu();
            menu.Show(bar, new Point(20, 40));
            await Task.Delay(300);

            foreach (var item in menu.Items.OfType<ToolStripMenuItem>())
            {
                var row = menu.RectangleToScreen(item.Bounds);
                using var shot = new Bitmap(row.Width, row.Height);
                using (var g = Graphics.FromImage(shot))
                    g.CopyFromScreen(row.Location, Point.Empty, row.Size);
                var inked = Enumerable.Range(0, row.Height) // away from the round corners, where the screen behind shows
                    .Where(y => Enumerable.Range(10, row.Width - 20).Any(x => shot.GetPixel(x, y) is var c && c.R + c.G + c.B < 600))
                    .ToArray();
                Assert.NotEmpty(inked);
                float text = (inked[0] + inked[^1]) / 2f, middle = (row.Height - 1) / 2f;
                Assert.True(Math.Abs(text - middle) <= 1.5f, $"{item.Text}: text on rows {inked[0]}–{inked[^1]} of 0–{row.Height - 1}");
            }
            menu.Close();
        }
        finally
        {
            bar.Close();
        }
    });

    [Fact]
    public void The_bar_stays_on_top_of_other_windows() => Sta.Run(async () =>
    {
        var bar = new ControlBarForm(NewSettingsFile());
        try
        {
            bar.Show();
            await Task.Delay(300);
            Assert.True(TestNative.IsTopmost(bar.Handle), "after opening");

            // A window the bar owns that stops being on top takes the bar with it; check after each one.
            var menu = bar.BuildSizeMenu();
            menu.Show(bar, new Point(20, 40));
            await Task.Delay(300);
            menu.Close();
            menu.Dispose();
            await Task.Delay(300);
            Assert.True(TestNative.IsTopmost(bar.Handle), "after the size menu");

            bar.BeginInvoke(bar.OpenSettings);
            await Task.Delay(500);
            Application.OpenForms.OfType<SettingsForm>().Single().Close();
            await Task.Delay(300);
            Assert.True(TestNative.IsTopmost(bar.Handle), "after the settings dialog");

            bar.BeginInvoke(bar.OpenCustomSizes);
            await Task.Delay(500);
            Application.OpenForms.OfType<CustomSizesForm>().Single().Close();
            await Task.Delay(300);
            Assert.True(TestNative.IsTopmost(bar.Handle), "after the custom sizes dialog");

            bar.BeginInvoke(() => { using var picker = new WindowPicker(); picker.ShowDialog(bar); });
            await Task.Delay(500);
            Application.OpenForms.OfType<WindowPicker>().Single().Close();
            await Task.Delay(300);
            Assert.True(TestNative.IsTopmost(bar.Handle), "after picking a window");
        }
        finally
        {
            bar.Close();
        }
    });

    [Fact]
    public void Size_menu_lists_saved_custom_sizes_under_the_presets() => Sta.Run(async () =>
    {
        var bar = new ControlBarForm(NewSettingsFile(new CustomSize("直播", 1600, 900), new CustomSize("", 800, 600)));
        try
        {
            bar.Show();
            await Task.Delay(300);
            using var menu = bar.BuildSizeMenu();
            Assert.Equal(
                ["1920 × 1080", "1280 × 720", "854 × 480", "640 × 360", "整個螢幕", "", "直播（1600 × 900）", "800 × 600", "", ManageSizes],
                menu.Items.Cast<ToolStripItem>().Select(i => i.Text ?? ""));
        }
        finally
        {
            bar.Close();
        }
    });

    [Fact]
    public void Size_menu_without_custom_sizes_offers_to_add_one() => Sta.Run(async () =>
    {
        var bar = new ControlBarForm(NewSettingsFile());
        try
        {
            bar.Show();
            await Task.Delay(300);
            using var menu = bar.BuildSizeMenu();
            Assert.Equal(["整個螢幕", "", ManageSizes], menu.Items.Cast<ToolStripItem>().Skip(4).Select(i => i.Text ?? ""));
        }
        finally
        {
            bar.Close();
        }
    });

    [Fact]
    public void Picking_a_custom_size_gives_the_region_that_size() => Sta.Run(async () =>
    {
        var bar = new ControlBarForm(NewSettingsFile(new CustomSize("", 800, 600)));
        try
        {
            bar.Show();
            await Task.Delay(300);
            using var menu = bar.BuildSizeMenu();
            menu.Items.Cast<ToolStripItem>().Single(i => i.Text == "800 × 600").PerformClick();

            Assert.Equal(new PixelSize(800, 600), new PixelSize(bar.Frame.CaptureRegion.Width, bar.Frame.CaptureRegion.Height));
        }
        finally
        {
            bar.Close();
        }
    });

    [Fact]
    public void Custom_size_larger_than_the_screen_cannot_be_picked() => Sta.Run(async () =>
    {
        var bar = new ControlBarForm(NewSettingsFile(new CustomSize("太大", 8000, 4000)));
        try
        {
            bar.Show();
            await Task.Delay(300);
            using var menu = bar.BuildSizeMenu();
            Assert.False(menu.Items.Cast<ToolStripItem>().Single(i => i.Text == "太大（8000 × 4000）").Enabled);
        }
        finally
        {
            bar.Close();
        }
    });

    [Fact]
    public void Picking_a_size_while_recording_leaves_the_region_alone() => Sta.Run(async () =>
    {
        var bar = new ControlBarForm(NewSettingsFile(new CustomSize("", 800, 600)));
        try
        {
            bar.Show();
            await Task.Delay(300);
            using var menu = bar.BuildSizeMenu(); // opened just before recording started, e.g. with the hotkey
            var before = bar.Frame.CaptureRegion;
            bar.Frame.Locked = true;               // what starting a recording does to the frame
            menu.Items.Cast<ToolStripItem>().Single(i => i.Text == "800 × 600").PerformClick();
            menu.Items.Cast<ToolStripItem>().Single(i => i.Text == "整個螢幕").PerformClick();

            Assert.Equal(before, bar.Frame.CaptureRegion);
        }
        finally
        {
            bar.Frame.Locked = false;
            bar.Close();
        }
    });

    [Fact]
    public void Hotkeys_are_released_while_the_custom_sizes_dialog_is_open() => Sta.Run(async () =>
    {
        var bar = new ControlBarForm(NewSettingsFile());
        try
        {
            bar.Show();
            await Task.Delay(300);
            bool freeDuringDialog = false;
            SynchronizationContext.Current!.Post(_ =>
            {
                freeDuringDialog = CanRegister(RecordKey); // otherwise F2 could record the dialog itself
                Application.OpenForms.OfType<CustomSizesForm>().Single().Close();
            }, null);
            bar.OpenCustomSizes();

            Assert.True(freeDuringDialog);
            Assert.False(CanRegister(RecordKey)); // registered again afterwards
        }
        finally
        {
            bar.Close();
        }
    });

    [Fact]
    public void Sizes_added_in_the_dialog_are_saved_and_listed() => Sta.Run(async () =>
    {
        string path = NewSettingsFile();
        var bar = new ControlBarForm(path);
        try
        {
            bar.Show();
            await Task.Delay(300);
            SynchronizationContext.Current!.Post(async _ =>
            {
                await Task.Delay(200);
                var dialog = Application.OpenForms.OfType<CustomSizesForm>().Single();
                All<TextBox>(dialog).Single(b => b.AccessibleName == "標籤").Text = "直播";
                All<TextBox>(dialog).Single(b => b.AccessibleName == "寬").Text = "1600";
                All<TextBox>(dialog).Single(b => b.AccessibleName == "高").Text = "900";
                All<Button>(dialog).Single(b => b.Text == "新增").PerformClick();
                dialog.Close();
            }, null);
            bar.OpenCustomSizes(); // returns when the posted callback has closed the dialog

            Assert.Equal([new CustomSize("直播", 1600, 900)], SettingsStore.Load(path).Settings.CustomSizes);
            using var menu = bar.BuildSizeMenu();
            Assert.Contains(menu.Items.Cast<ToolStripItem>(), i => i.Text == "直播（1600 × 900）");
        }
        finally
        {
            bar.Close();
        }
    });

    [Fact]
    public void Pausing_works_only_while_recording() => Sta.Run(async () =>
    {
        var bar = new ControlBarForm(NewSettingsFile());
        try
        {
            bar.Show();
            await Task.Delay(300);
            Assert.Contains(BarItem.Pause, bar.Look.Disabled);
            Assert.True(CanRegister(PauseKey), "the pause key is taken before recording");

            bar.ToggleRecording();
            await WaitFor(() => !bar.Look.Disabled.Contains(BarItem.Pause));
            Assert.False(CanRegister(PauseKey), "the pause key is free while recording");

            bar.ToggleRecording();
            await WaitFor(() => !bar.Look.Recording);
            Assert.Contains(BarItem.Pause, bar.Look.Disabled);
            Assert.True(CanRegister(PauseKey), "the pause key is still taken after recording");
        }
        finally
        {
            bar.Close();
        }
    });

    [Fact]
    public void Pausing_stops_the_recording_time_and_resuming_goes_on_from_there() => Sta.Run(async () =>
    {
        var bar = new ControlBarForm(NewSettingsFile());
        try
        {
            bar.Show();
            await Task.Delay(300);
            bar.ToggleRecording();
            await WaitFor(() => !bar.Look.Disabled.Contains(BarItem.Pause));
            await Task.Delay(1300);

            bar.TogglePause();
            Assert.True(bar.Look.Paused);
            Assert.Equal("00:00:01", bar.Look.Elapsed);
            await Task.Delay(2000);
            Assert.Equal("00:00:01", bar.Look.Elapsed); // without the pause: 3

            bar.TogglePause();
            Assert.False(bar.Look.Paused);
            await Task.Delay(1200);
            Assert.Equal("00:00:02", bar.Look.Elapsed); // from zero again: 1; counting the pause: 4
        }
        finally
        {
            bar.Close();
            await WaitFor(() => bar.IsDisposed);
        }
    });

    [Fact]
    public void Pausing_says_so_under_the_bar_and_turns_the_frame_orange() => Sta.Run(async () =>
    {
        var bar = new ControlBarForm(NewSettingsFile());
        try
        {
            bar.Show();
            await Task.Delay(300);
            bar.ToggleRecording();
            await WaitFor(() => !bar.Look.Disabled.Contains(BarItem.Pause));

            bar.TogglePause();
            Assert.True(bar.Frame.Paused);
            Assert.Equal("已暫停", bar.StatusBubble.Message);
            Assert.True(bar.StatusBubble.Visible);

            bar.TogglePause();
            Assert.False(bar.Frame.Paused);
            Assert.False(bar.StatusBubble.Visible);
        }
        finally
        {
            bar.Close();
            await WaitFor(() => bar.IsDisposed);
        }
    });

    [Fact]
    public void Stopping_while_paused_saves_the_video_and_ends_the_pause() => Sta.Run(async () =>
    {
        string settings = NewSettingsFile();
        var bar = new ControlBarForm(settings);
        try
        {
            bar.Show();
            await Task.Delay(300);
            bar.ToggleRecording();
            await WaitFor(() => !bar.Look.Disabled.Contains(BarItem.Pause));
            await Task.Delay(500);
            bar.TogglePause();

            bar.ToggleRecording();
            await WaitFor(() => !bar.Look.Recording);

            Assert.False(bar.Look.Paused);
            Assert.False(bar.Frame.Paused);
            Assert.False(bar.Frame.Locked);
            Assert.Single(Directory.GetFiles(Path.GetDirectoryName(settings)!, "*.mp4"));
        }
        finally
        {
            bar.Close();
        }
    });

    [Fact]
    public void A_taken_pause_key_is_reported_when_recording_starts() => Sta.Run(async () =>
    {
        using var taken = new HotkeyManager();
        Assert.True(taken.Register(PauseKey, () => { }));
        var bar = new ControlBarForm(NewSettingsFile());
        try
        {
            bar.Show();
            await Task.Delay(300);
            bar.ToggleRecording();
            await WaitFor(() => !bar.Look.Disabled.Contains(BarItem.Pause));

            Assert.Contains($"暫停快捷鍵 {HotkeyText.Format(PauseKey)} 登記失敗", bar.StatusBubble.Message);
        }
        finally
        {
            bar.Close();
            await WaitFor(() => bar.IsDisposed);
        }
    });

    // ---- Do not disturb while recording (spec 8.1) ----

    [Fact]
    public void With_the_box_checked_do_not_disturb_is_on_for_the_whole_recording_and_off_after() => Sta.Run(async () =>
    {
        var dnd = new FakeDoNotDisturb(on: false);
        var bar = new ControlBarForm(NewSettingsFile(doNotDisturb: true), doNotDisturb: dnd);
        try
        {
            bar.Show();
            await Task.Delay(300);
            bar.ToggleRecording();
            Assert.True(dnd.On); // before the first frame
            await WaitFor(() => !bar.Look.Disabled.Contains(BarItem.Pause));
            bar.TogglePause();
            Assert.True(dnd.On);
            bar.TogglePause();

            bar.ToggleRecording();
            await WaitFor(() => !bar.Look.Recording);
            Assert.False(dnd.On);
        }
        finally
        {
            bar.Close();
            await WaitFor(() => bar.IsDisposed);
        }
    });

    [Fact]
    public void Do_not_disturb_that_was_already_on_stays_on_after_a_recording() => Sta.Run(async () =>
    {
        var dnd = new FakeDoNotDisturb(on: true);
        var bar = new ControlBarForm(NewSettingsFile(doNotDisturb: true), doNotDisturb: dnd);
        try
        {
            bar.Show();
            await Task.Delay(300);
            await RecordBriefly(bar);

            Assert.True(dnd.On);
            Assert.Equal(0, dnd.Changes);
        }
        finally
        {
            bar.Close();
            await WaitFor(() => bar.IsDisposed);
        }
    });

    [Fact]
    public void With_the_box_unchecked_a_recording_leaves_do_not_disturb_alone() => Sta.Run(async () =>
    {
        var dnd = new FakeDoNotDisturb(on: false);
        var bar = new ControlBarForm(NewSettingsFile(doNotDisturb: false), doNotDisturb: dnd);
        try
        {
            bar.Show();
            await Task.Delay(300);
            await RecordBriefly(bar);

            Assert.Equal(0, dnd.Calls);
        }
        finally
        {
            bar.Close();
            await WaitFor(() => bar.IsDisposed);
        }
    });

    [Fact]
    public void Without_a_do_not_disturb_this_program_can_change_the_recording_goes_ahead_without_a_word() => Sta.Run(async () =>
    {
        var dnd = new FakeDoNotDisturb(on: null) { CanTurnOn = false };
        var bar = new ControlBarForm(NewSettingsFile(doNotDisturb: true), doNotDisturb: dnd);
        try
        {
            bar.Show();
            await Task.Delay(300);
            bar.ToggleRecording();
            await WaitFor(() => !bar.Look.Disabled.Contains(BarItem.Pause)); // recording
            Assert.DoesNotContain("勿打擾", bar.StatusBubble.Message);

            bar.ToggleRecording();
            await WaitFor(() => !bar.Look.Recording);
            Assert.DoesNotContain("勿打擾", bar.StatusBubble.Message);
        }
        finally
        {
            bar.Close();
            await WaitFor(() => bar.IsDisposed);
        }
    });

    [Fact]
    public void Do_not_disturb_that_will_not_turn_back_off_is_reported_under_the_bar() => Sta.Run(async () =>
    {
        var dnd = new FakeDoNotDisturb(on: false) { CanTurnOff = false };
        var bar = new ControlBarForm(NewSettingsFile(doNotDisturb: true), doNotDisturb: dnd);
        try
        {
            bar.Show();
            await Task.Delay(300);
            await RecordBriefly(bar);

            Assert.Contains("已存檔", bar.StatusBubble.Message);
            Assert.Contains("勿打擾沒有自動關掉，請手動關閉", bar.StatusBubble.Message);
        }
        finally
        {
            bar.Close();
            await WaitFor(() => bar.IsDisposed);
        }
    });

    [Fact]
    public void The_settings_file_notes_do_not_disturb_is_to_be_turned_off_until_the_recording_ends() => Sta.Run(async () =>
    {
        string path = NewSettingsFile(doNotDisturb: true);
        var bar = new ControlBarForm(path, doNotDisturb: new FakeDoNotDisturb(on: false));
        try
        {
            bar.Show();
            await Task.Delay(300);
            bar.ToggleRecording();
            Assert.True(SettingsStore.Load(path).Settings.DoNotDisturbToTurnOff);
            await WaitFor(() => !bar.Look.Disabled.Contains(BarItem.Pause));

            bar.ToggleRecording();
            await WaitFor(() => !bar.Look.Recording);
            Assert.False(SettingsStore.Load(path).Settings.DoNotDisturbToTurnOff);
        }
        finally
        {
            bar.Close();
            await WaitFor(() => bar.IsDisposed);
        }
    });

    [Fact]
    public void Do_not_disturb_left_on_by_a_FrameCam_that_crashed_is_turned_off_at_the_next_start() => Sta.Run(async () =>
    {
        string path = NewSettingsFile(doNotDisturb: true, toTurnOff: true);
        var dnd = new FakeDoNotDisturb(on: true);
        var bar = new ControlBarForm(path, doNotDisturb: dnd);
        try
        {
            bar.Show();
            await Task.Delay(300);

            Assert.False(dnd.On);
            Assert.False(SettingsStore.Load(path).Settings.DoNotDisturbToTurnOff);
        }
        finally
        {
            bar.Close();
            await WaitFor(() => bar.IsDisposed);
        }
    });

    private static async Task RecordBriefly(ControlBarForm bar)
    {
        bar.ToggleRecording();
        await WaitFor(() => !bar.Look.Disabled.Contains(BarItem.Pause));
        bar.ToggleRecording();
        await WaitFor(() => !bar.Look.Recording);
    }

    /// <summary>Stands in for Windows' do not disturb, so the tests leave the real one alone.</summary>
    private sealed class FakeDoNotDisturb(bool? on) : IDoNotDisturb
    {
        /// <summary>Null: this Windows does not let a program read it.</summary>
        public bool? On { get; private set; } = on;
        public bool CanTurnOn { get; init; } = true;
        public bool CanTurnOff { get; init; } = true;

        /// <summary>Reads and attempted changes.</summary>
        public int Calls { get; private set; }

        public int Changes { get; private set; }

        public bool? IsOn()
        {
            Calls++;
            return On;
        }

        public bool TrySet(bool on)
        {
            Calls++;
            if (!(on ? CanTurnOn : CanTurnOff))
                return false;
            On = on;
            Changes++;
            return true;
        }
    }

    private static async Task WaitFor(Func<bool> condition)
    {
        for (int i = 0; i < 100 && !condition(); i++)
            await Task.Delay(100);
        Assert.True(condition(), "timed out after 10 seconds");
    }

    private static Point Center(RectangleF r) => new((int)(r.Left + r.Width / 2), (int)(r.Top + r.Height / 2));

    private static Color ScreenPixel(int x, int y)
    {
        using var bmp = new Bitmap(1, 1);
        using (var g = Graphics.FromImage(bmp))
            g.CopyFromScreen(x, y, 0, 0, bmp.Size);
        return bmp.GetPixel(0, 0);
    }

    private static bool CanRegister(Keys keys)
    {
        using var probe = new HotkeyManager();
        return probe.Register(keys, () => { });
    }

    private static string NewSettingsFile(params CustomSize[] customSizes)
    {
        string folder = Directory.CreateTempSubdirectory("framecam 測試 bar ").FullName;
        string path = Path.Combine(folder, "settings.json");
        SettingsStore.Save(path, new AppSettings
        {
            OutputFolder = folder,
            RecordHotkey = HotkeyText.Format(RecordKey),
            PauseHotkey = HotkeyText.Format(PauseKey),
            ScreenshotHotkey = HotkeyText.Format(Keys.Control | Keys.Alt | Keys.Shift | Keys.F12),
            PickHotkey = HotkeyText.Format(PickKey),
            CustomSizes = customSizes,
        });
        return path;
    }

    private static string NewSettingsFile(bool doNotDisturb, bool toTurnOff = false)
    {
        string path = NewSettingsFile();
        var (settings, _) = SettingsStore.Load(path);
        SettingsStore.Save(path, settings with { DoNotDisturbWhileRecording = doNotDisturb, DoNotDisturbToTurnOff = toTurnOff });
        return path;
    }
}
