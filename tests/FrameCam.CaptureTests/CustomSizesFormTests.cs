using FrameCam.Core;
using FrameCam.UI;
using static FrameCam.CaptureTests.ControlTree;

namespace FrameCam.CaptureTests;

/// <summary>The custom sizes dialog, shown on screen.</summary>
public class CustomSizesFormTests
{
    private static readonly PixelSize Current = new(1280, 720);

    [Fact]
    public void Width_and_height_start_at_the_current_size() => Sta.Run(async () =>
    {
        using var dialog = await Show([]);
        Assert.Equal("1280", Box(dialog, "寬").Text);
        Assert.Equal("720", Box(dialog, "高").Text);
        Assert.Equal("", Box(dialog, "標籤").Text);
    });

    [Fact]
    public void Empty_list_says_there_are_no_custom_sizes_yet() => Sta.Run(async () =>
    {
        using var dialog = await Show([]);
        Assert.Contains(All<Label>(dialog), l => l.Visible && l.Text == "還沒有自訂尺寸");
    });

    [Fact]
    public void Added_size_is_listed_and_kept() => Sta.Run(async () =>
    {
        using var dialog = await Show([new CustomSize("舊的", 800, 600)]);
        Type(dialog, "直播", "1600", "900");
        Button(dialog, "新增").PerformClick();

        Assert.Equal([new CustomSize("舊的", 800, 600), new CustomSize("直播", 1600, 900)], dialog.Sizes);
        Assert.Equal(["舊的（800 × 600）", "直播（1600 × 900）"], List(dialog).Items.Cast<CustomSize>().Select(c => c.MenuText));
        Assert.Equal("", Box(dialog, "標籤").Text); // ready for the next one
        Assert.DoesNotContain(All<Label>(dialog), l => l.Visible && l.Text == "還沒有自訂尺寸");
    });

    [Fact]
    public void Enter_adds_the_typed_size() => Sta.Run(async () =>
    {
        using var dialog = await Show([]);
        Assert.Same(Button(dialog, "新增"), dialog.AcceptButton);
    });

    [Fact]
    public void Size_that_is_not_allowed_shows_why_and_is_not_added() => Sta.Run(async () =>
    {
        using var dialog = await Show([]);
        Type(dialog, "x", "30", "600");
        Button(dialog, "新增").PerformClick();

        Assert.Empty(dialog.Sizes);
        Assert.Contains(All<Label>(dialog), l => l.Visible && l.Text == "寬和高要在 32 到 8192 之間");
    });

    [Fact]
    public void Message_goes_away_after_a_size_is_added() => Sta.Run(async () =>
    {
        using var dialog = await Show([]);
        Type(dialog, "x", "30", "600");
        Button(dialog, "新增").PerformClick();
        Type(dialog, "x", "800", "600");
        Button(dialog, "新增").PerformClick();

        Assert.DoesNotContain(All<Label>(dialog), l => l.Visible && l.Text == "寬和高要在 32 到 8192 之間");
    });

    [Fact]
    public void Too_long_label_is_explained_rather_than_cut_off() => Sta.Run(async () =>
    {
        using var dialog = await Show([]);
        Type(dialog, "", "800", "600");
        var label = Box(dialog, "標籤");
        TestNative.Type(label.Handle, new string('字', 25)); // typing obeys a length limit on the box, setting Text does not
        Assert.Equal(25, label.Text.Length);
        Button(dialog, "新增").PerformClick();

        Assert.Empty(dialog.Sizes);
        Assert.Contains(All<Label>(dialog), l => l.Visible && l.Text == "標籤最多 20 個字");
    });

    [Fact]
    public void The_same_size_with_the_same_label_is_not_added_twice() => Sta.Run(async () =>
    {
        using var dialog = await Show([new CustomSize("直播", 1600, 900)]);
        Type(dialog, "直播", "1600", "900");
        Button(dialog, "新增").PerformClick();

        Assert.Single(dialog.Sizes);
        Assert.Contains(All<Label>(dialog), l => l.Visible && l.Text == "已經有一樣的自訂尺寸");
    });

    [Fact]
    public void At_most_20_sizes_can_be_saved() => Sta.Run(async () =>
    {
        var twenty = Enumerable.Range(1, 20).Select(i => new CustomSize($"s{i}", 100 + 2 * i, 100)).ToArray();
        using var dialog = await Show(twenty);
        Type(dialog, "one more", "800", "600");
        Button(dialog, "新增").PerformClick();

        Assert.Equal(20, dialog.Sizes.Count);
        Assert.Contains(All<Label>(dialog), l => l.Visible && l.Text == "最多存 20 個自訂尺寸，請先刪掉一些");
    });

    [Fact]
    public void Add_button_is_on_the_same_line_as_the_boxes() => Sta.Run(async () =>
    {
        using var dialog = await Show([]);
        var add = Button(dialog, "新增");
        var row = Box(dialog, "寬").Parent!.Bounds; // the round field around the box
        Assert.InRange(add.Top + add.Height / 2, row.Top, row.Bottom);
        Assert.True(add.Top < List(dialog).Parent!.Top, "the add row comes before the list");
    });

    [Fact]
    public void There_is_no_separate_delete_button() => Sta.Run(async () =>
    {
        using var dialog = await Show([new CustomSize("a", 800, 600)]);
        Assert.DoesNotContain(All<Button>(dialog), b => b.Text == "刪除");
    });

    [Fact]
    public void Clicking_the_x_at_the_end_of_a_row_deletes_that_size() => Sta.Run(async () =>
    {
        using var dialog = await Show([new CustomSize("a", 800, 600), new CustomSize("b", 640, 480), new CustomSize("c", 320, 240)]);
        var list = List(dialog);
        var row = list.GetItemRectangle(1);

        TestNative.Click(list.Handle, new Point(row.Right - list.LogicalToDeviceUnits(16), row.Top + row.Height / 2));

        Assert.Equal([new CustomSize("a", 800, 600), new CustomSize("c", 320, 240)], dialog.Sizes);
        Assert.Equal(["a", "c"], list.Items.Cast<CustomSize>().Select(c => c.Label));
    });

    [Fact]
    public void Clicking_elsewhere_on_a_row_deletes_nothing() => Sta.Run(async () =>
    {
        using var dialog = await Show([new CustomSize("a", 800, 600), new CustomSize("b", 640, 480)]);
        var list = List(dialog);
        var row = list.GetItemRectangle(1);

        TestNative.Click(list.Handle, new Point(row.Left + list.LogicalToDeviceUnits(20), row.Top + row.Height / 2));

        Assert.Equal(2, dialog.Sizes.Count);
    });

    [Fact]
    public void Delete_key_deletes_the_selected_size() => Sta.Run(async () =>
    {
        using var dialog = await Show([new CustomSize("a", 800, 600), new CustomSize("b", 640, 480), new CustomSize("c", 320, 240)]);
        var list = List(dialog);
        list.SelectedIndex = 1;

        TestNative.KeyDown(list.Handle, Keys.Delete);

        Assert.Equal([new CustomSize("a", 800, 600), new CustomSize("c", 320, 240)], dialog.Sizes);
        Assert.Equal(1, list.SelectedIndex); // the next one, so pressing again keeps deleting
    });

    [Fact]
    public void Clicking_a_row_puts_its_size_in_the_boxes_to_change() => Sta.Run(async () =>
    {
        using var dialog = await Show([new CustomSize("a", 800, 600), new CustomSize("直播", 1600, 900)]);
        ClickRow(dialog, 1);

        Assert.Equal("直播", Box(dialog, "標籤").Text);
        Assert.Equal("1600", Box(dialog, "寬").Text);
        Assert.Equal("900", Box(dialog, "高").Text);
        Assert.True(Button(dialog, "儲存").Visible);
        Assert.True(Button(dialog, "取消").Visible);
        Assert.DoesNotContain(All<Button>(dialog), b => b.Text == "新增");
    });

    [Fact]
    public void Saving_replaces_the_row_in_its_place() => Sta.Run(async () =>
    {
        using var dialog = await Show([new CustomSize("a", 800, 600), new CustomSize("b", 640, 480), new CustomSize("c", 320, 240)]);
        ClickRow(dialog, 1);
        Type(dialog, "新的", "1024", "768");
        Button(dialog, "儲存").PerformClick();

        Assert.Equal([new CustomSize("a", 800, 600), new CustomSize("新的", 1024, 768), new CustomSize("c", 320, 240)], dialog.Sizes);
        Assert.Equal(["a", "新的", "c"], List(dialog).Items.Cast<CustomSize>().Select(c => c.Label));
        Assert.True(Button(dialog, "新增").Visible); // back to adding
        Assert.False(Button(dialog, "取消").Visible);
        Assert.Equal("", Box(dialog, "標籤").Text);
        Assert.Equal("1280", Box(dialog, "寬").Text);
        Assert.Equal("720", Box(dialog, "高").Text);
    });

    [Fact]
    public void Cancel_leaves_the_row_as_it_was_and_goes_back_to_adding() => Sta.Run(async () =>
    {
        using var dialog = await Show([new CustomSize("a", 800, 600)]);
        ClickRow(dialog, 0);
        Type(dialog, "b", "640", "480");
        Button(dialog, "取消").PerformClick();

        Assert.Equal([new CustomSize("a", 800, 600)], dialog.Sizes);
        Assert.True(Button(dialog, "新增").Visible);
        Assert.False(Button(dialog, "取消").Visible);
        Assert.Equal("", Box(dialog, "標籤").Text);
        Assert.Equal("1280", Box(dialog, "寬").Text);
        Assert.Equal("720", Box(dialog, "高").Text);
    });

    [Fact]
    public void Esc_while_changing_a_size_stops_changing_and_keeps_the_dialog_open() => Sta.Run(async () =>
    {
        using var dialog = await Show([new CustomSize("a", 800, 600)]);
        ClickRow(dialog, 0);
        Type(dialog, "b", "640", "480");
        PressIn(Box(dialog, "寬"), Keys.Escape);

        Assert.Equal(DialogResult.None, dialog.DialogResult); // 完成 or the X would have set it
        Assert.Equal([new CustomSize("a", 800, 600)], dialog.Sizes);
        Assert.True(Button(dialog, "新增").Visible);
        Assert.Equal("1280", Box(dialog, "寬").Text);
    });

    [Fact]
    public void A_size_can_be_changed_when_20_are_saved() => Sta.Run(async () =>
    {
        var twenty = Enumerable.Range(1, 20).Select(i => new CustomSize($"s{i}", 100 + 2 * i, 100)).ToArray();
        using var dialog = await Show(twenty);
        ClickRow(dialog, 0);
        Type(dialog, "改過", "800", "600");
        Button(dialog, "儲存").PerformClick();

        Assert.Equal(20, dialog.Sizes.Count);
        Assert.Equal(new CustomSize("改過", 800, 600), dialog.Sizes[0]);
    });

    [Fact]
    public void A_changed_size_is_not_compared_with_itself_but_is_with_the_others() => Sta.Run(async () =>
    {
        using var dialog = await Show([new CustomSize("a", 800, 600), new CustomSize("b", 640, 480)]);
        ClickRow(dialog, 0);
        Button(dialog, "儲存").PerformClick(); // nothing changed
        Assert.Equal([new CustomSize("a", 800, 600), new CustomSize("b", 640, 480)], dialog.Sizes);
        Assert.DoesNotContain(All<Label>(dialog), l => l.Visible && l.Text == "已經有一樣的自訂尺寸");

        ClickRow(dialog, 0);
        Type(dialog, "b", "640", "480");
        Button(dialog, "儲存").PerformClick();
        Assert.Equal([new CustomSize("a", 800, 600), new CustomSize("b", 640, 480)], dialog.Sizes);
        Assert.Contains(All<Label>(dialog), l => l.Visible && l.Text == "已經有一樣的自訂尺寸");
    });

    [Fact]
    public void Deleting_the_row_being_changed_stops_changing() => Sta.Run(async () =>
    {
        using var dialog = await Show([new CustomSize("a", 800, 600), new CustomSize("b", 640, 480)]);
        var list = List(dialog);
        ClickRow(dialog, 1);
        Assert.True(Button(dialog, "儲存").Visible);
        var row = list.GetItemRectangle(1);
        TestNative.Click(list.Handle, new Point(row.Right - list.LogicalToDeviceUnits(16), row.Top + row.Height / 2));

        Assert.Equal([new CustomSize("a", 800, 600)], dialog.Sizes);
        Assert.True(Button(dialog, "新增").Visible);
        Assert.Equal("", Box(dialog, "標籤").Text);
    });

    [Fact]
    public void Save_and_cancel_fit_on_the_line_of_the_boxes() => Sta.Run(async () =>
    {
        using var dialog = await Show([new CustomSize("a", 800, 600)]);
        ClickRow(dialog, 0);
        var save = Button(dialog, "儲存");
        var cancel = Button(dialog, "取消");
        var height = Box(dialog, "高").Parent!.Bounds;

        Assert.True(height.Right < save.Left, "the height box ends before 儲存");
        Assert.True(save.Right < cancel.Left, "儲存 is on the left of 取消");
        Assert.True(cancel.Right <= dialog.ClientSize.Width - dialog.LogicalToDeviceUnits(22), "取消 keeps the right margin");
        Assert.InRange(cancel.Top + cancel.Height / 2, height.Top, height.Bottom);
        Assert.True(Box(dialog, "標籤").Parent!.Right < Box(dialog, "寬").Parent!.Left, "the label box ends before the width box");
    });

    [Fact]
    public void The_list_has_a_title_above_it_clear_of_the_message() => Sta.Run(async () =>
    {
        using var dialog = await Show([]);
        Type(dialog, "x", "30", "600");
        Button(dialog, "新增").PerformClick(); // shows the message between the boxes and the list
        var title = All<Label>(dialog).Single(l => l.Text == "已儲存的尺寸");
        var message = All<Label>(dialog).Single(l => l.Visible && l.Text == "寬和高要在 32 到 8192 之間");
        var list = List(dialog).Parent!;

        Assert.True(title.Visible);
        Assert.True(title.Bottom <= list.Top, "the title is above the list");
        Assert.True(message.Bottom <= title.Top, "the message does not cover the title");
    });

    [Fact]
    public void The_bottom_bar_says_how_many_are_saved() => Sta.Run(async () =>
    {
        using var dialog = await Show([new CustomSize("a", 800, 600), new CustomSize("b", 640, 480)]);
        Assert.Contains(All<Label>(dialog), l => l.Visible && l.Text == "已存 2 個，最多 20 個");

        Type(dialog, "c", "320", "240");
        Button(dialog, "新增").PerformClick();
        Assert.Contains(All<Label>(dialog), l => l.Visible && l.Text == "已存 3 個，最多 20 個");

        List(dialog).SelectedIndex = 0;
        TestNative.KeyDown(List(dialog).Handle, Keys.Delete);
        Assert.Contains(All<Label>(dialog), l => l.Visible && l.Text == "已存 2 個，最多 20 個");
    });

    [Fact]
    public void Done_is_on_the_same_line_as_the_count_under_the_list() => Sta.Run(async () =>
    {
        using var dialog = await Show([]);
        var count = All<Label>(dialog).Single(l => l.Text.StartsWith("已存 "));
        var done = Button(dialog, "完成");
        var countRect = dialog.RectangleToClient(count.RectangleToScreen(count.ClientRectangle));
        var doneRect = dialog.RectangleToClient(done.RectangleToScreen(done.ClientRectangle));

        Assert.InRange(countRect.Top + countRect.Height / 2, doneRect.Top, doneRect.Bottom);
        Assert.True(countRect.Top > List(dialog).Parent!.Bottom, "the count is under the list");
        Assert.True(countRect.Right < doneRect.Left, "the count is on the left of 完成");
    });

    private static async Task<CustomSizesForm> Show(CustomSize[] sizes)
    {
        var dialog = new CustomSizesForm(sizes, Current);
        dialog.Show();
        await Task.Delay(200);
        return dialog;
    }

    private static void Type(CustomSizesForm dialog, string label, string width, string height)
    {
        Box(dialog, "標籤").Text = label;
        Box(dialog, "寬").Text = width;
        Box(dialog, "高").Text = height;
    }

    /// <summary>A click on the row's size, clear of its X.</summary>
    private static void ClickRow(CustomSizesForm dialog, int index)
    {
        var list = List(dialog);
        var row = list.GetItemRectangle(index);
        TestNative.Click(list.Handle, new Point(row.Left + list.LogicalToDeviceUnits(20), row.Top + row.Height / 2));
    }

    /// <summary>A key going down in the box the way the message loop hands it over, through ProcessCmdKey.</summary>
    private static void PressIn(Control box, Keys key)
    {
        const int WM_KEYDOWN = 0x0100;
        var msg = Message.Create(box.Handle, WM_KEYDOWN, (IntPtr)(int)key, IntPtr.Zero);
        box.PreProcessMessage(ref msg);
    }

    private static TextBox Box(Control dialog, string name) => All<TextBox>(dialog).Single(b => b.AccessibleName == name);

    private static Button Button(Control dialog, string text) => All<Button>(dialog).Single(b => b.Text == text);

    private static ListBox List(Control dialog) => All<ListBox>(dialog).Single();
}
