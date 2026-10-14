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
        Type(dialog, "x", "801", "600");
        Button(dialog, "新增").PerformClick();

        Assert.Empty(dialog.Sizes);
        Assert.Contains(All<Label>(dialog), l => l.Visible && l.Text == "寬和高要是 32 到 8192 之間的偶數");
    });

    [Fact]
    public void Message_goes_away_after_a_size_is_added() => Sta.Run(async () =>
    {
        using var dialog = await Show([]);
        Type(dialog, "x", "801", "600");
        Button(dialog, "新增").PerformClick();
        Type(dialog, "x", "800", "600");
        Button(dialog, "新增").PerformClick();

        Assert.DoesNotContain(All<Label>(dialog), l => l.Visible && l.Text == "寬和高要是 32 到 8192 之間的偶數");
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
    public void Deleting_removes_the_selected_size() => Sta.Run(async () =>
    {
        using var dialog = await Show([new CustomSize("a", 800, 600), new CustomSize("b", 640, 480), new CustomSize("c", 320, 240)]);
        Assert.False(Button(dialog, "刪除").Enabled); // nothing selected yet
        List(dialog).SelectedIndex = 1;
        Assert.True(Button(dialog, "刪除").Enabled);

        Button(dialog, "刪除").PerformClick();

        Assert.Equal([new CustomSize("a", 800, 600), new CustomSize("c", 320, 240)], dialog.Sizes);
        Assert.Equal(1, List(dialog).SelectedIndex); // the next one, so pressing again keeps deleting
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

    private static TextBox Box(Control dialog, string name) => All<TextBox>(dialog).Single(b => b.AccessibleName == name);

    private static Button Button(Control dialog, string text) => All<Button>(dialog).Single(b => b.Text == text);

    private static ListBox List(Control dialog) => All<ListBox>(dialog).Single();
}
