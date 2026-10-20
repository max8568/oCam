using FrameCam.Core;

namespace FrameCam.UI;

/// <summary>
/// The manual's text (spec 8.4), written for colleagues who are not engineers. A line starting with "# " is a
/// heading, one starting with "- " a bullet point, any other line a paragraph.
/// </summary>
internal static class HelpText
{
    /// <summary>The manual, naming the hotkeys the settings hold now.</summary>
    public static IReadOnlyList<HelpBlock> Blocks(AppSettings settings) => Parse($"""
        控制列上的按鈕和下面的快捷鍵作用一樣。快捷鍵可以在設定裡改。

        # 錄影和截圖
        - {settings.RecordHotkey}：開始錄影，再按一次停止。
        - {PauseLine(settings.PauseHotkey)}
        - {settings.ScreenshotHotkey}：把錄影框裡的畫面存成截圖。
        - {settings.PickHotkey}：點一個視窗，錄影框就對齊那個視窗。按住 Ctrl 可以只選視窗裡的一塊，按 Esc 取消。

        # 調整錄影框
        - 拖角：同時改寬和高。
        - 拖邊：只改那一邊。
        - 按住 Alt 拖角或拖邊：不會自動貼齊其他視窗的邊緣。
        - 拖左上角的寬高標籤或正中央的十字箭頭：移動整個框。
        - 方向鍵：整個框移動 1 像素。先點一下錄影框，方向鍵才有作用。
        - Ctrl + 方向鍵：移動最後拖過的那條邊或那個角。

        # 控制列上的其他按鈕
        - 常用尺寸：把錄影框改成選單裡的尺寸。最下面的「自訂尺寸…」可以存或修改自己的尺寸。
        - 開啟存檔資料夾：影片存成 MP4、截圖存成 PNG，都在這個資料夾。
        - 設定：改快捷鍵、存檔資料夾、每秒格數，以及要不要錄滑鼠游標。
        - 縮到最小：控制列和錄影框一起收起來，點工作列上的 FrameCam 就回來。錄影中不能縮。
        """);

    /// <summary>Only Space alone is worth a warning: while recording, no other program gets it.</summary>
    private static string PauseLine(string key) => key == "Space"
        ? "空白鍵：錄影中暫停，再按一次繼續。錄影時其他程式打不出空白，要打字的話到設定裡換掉暫停鍵。"
        : $"{key}：錄影中暫停，再按一次繼續。";

    private static IReadOnlyList<HelpBlock> Parse(string text) =>
        text.Split('\n')
            .Select(line => line.Trim())
            .Where(line => line.Length > 0)
            .Select(line =>
                line.StartsWith("# ") ? new HelpBlock(HelpKind.Heading, line[2..])
                : line.StartsWith("- ") ? new HelpBlock(HelpKind.Bullet, line[2..])
                : new HelpBlock(HelpKind.Paragraph, line))
            .ToList();
}
