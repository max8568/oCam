using FrameCam.Core;
using FrameCam.Native;
using FrameCam.UI;

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
