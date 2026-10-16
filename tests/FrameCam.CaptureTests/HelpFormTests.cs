using FrameCam.Core;
using FrameCam.UI;

namespace FrameCam.CaptureTests;

/// <summary>The manual opened from the control bar's help button.</summary>
public class HelpFormTests
{
    [Fact]
    public void The_manual_names_the_hotkeys_set_now()
    {
        var settings = new AppSettings { RecordHotkey = "F3", PauseHotkey = "Space", ScreenshotHotkey = "F4", PickHotkey = "Ctrl+F6" };

        var text = string.Join("\n", HelpText.Blocks(settings).Select(b => b.Text));

        Assert.Contains("F3：開始錄影", text);
        Assert.Contains("空白鍵：錄影中暫停", text);
        Assert.Contains("F4：把錄影框裡的畫面", text);
        Assert.Contains("Ctrl+F6：點一個視窗", text);
    }

    [Fact]
    public void Another_pause_key_drops_the_note_about_typing_spaces()
    {
        var text = string.Join("\n", HelpText.Blocks(new AppSettings { PauseHotkey = "F8" }).Select(b => b.Text));

        Assert.Contains("F8：錄影中暫停", text);
        Assert.DoesNotContain("打不出空白", text);
    }

    [Fact]
    public void A_manual_longer_than_its_view_scrolls_with_the_mouse_wheel() => Sta.Run(async () =>
    {
        var blocks = Enumerable.Range(1, 60).Select(i => new HelpBlock(HelpKind.Bullet, $"第 {i} 行")).ToList();
        using var form = new Form { ClientSize = new Size(520, 480) };
        var view = new HelpView(blocks) { Dock = DockStyle.Fill };
        form.Controls.Add(view);
        form.Show();
        await Task.Delay(200);

        Assert.True(view.VerticalScroll.Visible);
        Assert.Equal(0, view.VerticalScroll.Value);
        TestNative.WheelDown(view, new Point(view.Width / 2, view.Height / 2));
        Assert.True(view.VerticalScroll.Value > 0);
    });
}
