using FrameCam.Core;
using FrameCam.Native;
using FrameCam.UI;

namespace FrameCam;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        // Temporary harness until Task 11: choose hotkeys, then press them anywhere; the window title shows which fired.
        var defaults = new AppSettings();
        AppSettings chosen;
        using (var dialog = new SettingsForm(defaults, recordHotkeyFailed: false, screenshotHotkeyFailed: true))
        {
            if (dialog.ShowDialog() != DialogResult.OK)
                return;
            chosen = dialog.ToSettings(defaults);
        }
        var form = new Form { Text = "按快捷鍵試試看", Width = 480, Height = 120 };
        using var hotkeys = new HotkeyManager();
        foreach (var (name, text) in new[] { ("錄影", chosen.RecordHotkey), ("截圖", chosen.ScreenshotHotkey) })
        {
            bool registered = HotkeyText.TryParse(text, out var keys)
                && hotkeys.Register(keys, () => form.Text = $"{name} {text} {DateTime.Now:HH:mm:ss}");
            if (!registered)
                form.Text = $"{name} {text} 登記失敗";
        }
        Application.Run(form);
    }
}
