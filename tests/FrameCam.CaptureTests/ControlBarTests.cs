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

    private const string ManageSizes = "新增或刪除自訂尺寸…";

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
            ScreenshotHotkey = HotkeyText.Format(Keys.Control | Keys.Alt | Keys.Shift | Keys.F12),
            PickHotkey = HotkeyText.Format(PickKey),
            CustomSizes = customSizes,
        });
        return path;
    }
}
