using FrameCam.Core;

namespace FrameCam.UI;

/// <summary>
/// The settings dialog (spec 8.5): save folder, the hotkeys for recording, screenshots and picking a window,
/// frames per second. Design 21: round fields,
/// and 30 / 60 as two buttons. A hotkey that could not be registered gets a red field and a note under it
/// until another key is picked.
/// </summary>
internal sealed class SettingsForm : LightDialog
{
    private const string FailedMessage = "登記失敗，可能被其他程式使用";
    private const int FieldLeft = 118;
    private const int FieldWidth = 280;

    private readonly TextBox _folder = new() { BorderStyle = BorderStyle.None, AccessibleName = "存檔資料夾" };
    private readonly HotkeyTextBox _recordKey = new() { BorderStyle = BorderStyle.None, AccessibleName = "錄影快捷鍵" };
    private readonly HotkeyTextBox _screenshotKey = new() { BorderStyle = BorderStyle.None, AccessibleName = "截圖快捷鍵" };
    private readonly HotkeyTextBox _pickKey = new() { BorderStyle = BorderStyle.None, AccessibleName = "選視窗快捷鍵" };
    private readonly RoundButton _fps30 = new() { Text = "30", AccessibleName = "每秒 30 格" };
    private readonly RoundButton _fps60 = new() { Text = "60", AccessibleName = "每秒 60 格" };
    private int _framesPerSecond;

    public SettingsForm(AppSettings settings, bool recordHotkeyFailed, bool screenshotHotkeyFailed, bool pickHotkeyFailed = false)
        : base("設定", "FrameCam 設定", new Size(420, 434))
    {
        SuspendLayout();

        _folder.Text = settings.OutputFolder;
        if (HotkeyText.TryParse(settings.RecordHotkey, out var record))
            _recordKey.Hotkey = record;
        if (HotkeyText.TryParse(settings.ScreenshotHotkey, out var screenshot))
            _screenshotKey.Hotkey = screenshot;
        if (HotkeyText.TryParse(settings.PickHotkey, out var pick))
            _pickKey.Hotkey = pick;

        var close = CloseButton(420, DialogResult.Cancel);

        // Names in a column on the left, their fields lined up on the right (layout 02).
        var browse = new RoundButton { Glyph = Glyph.Folder, Bounds = new Rectangle(250, 4, 30, 30), AccessibleName = "瀏覽資料夾" };
        browse.SetColors(Palette.FieldButton, Palette.FieldButtonHover, Palette.Dots, Palette.Ink);
        browse.Click += (_, _) => BrowseFolder();
        var folderField = FieldBox.Around(new Rectangle(FieldLeft, 66, FieldWidth, 38), _folder, browse);

        var recordField = FieldBox.Around(new Rectangle(FieldLeft, 128, FieldWidth, 38), _recordKey);
        var recordNote = Note(recordField);
        var screenshotField = FieldBox.Around(new Rectangle(FieldLeft, 190, FieldWidth, 38), _screenshotKey);
        var screenshotNote = Note(screenshotField);
        var pickField = FieldBox.Around(new Rectangle(FieldLeft, 252, FieldWidth, 38), _pickKey);
        var pickNote = Note(pickField);
        ShowFailure(recordField, recordNote, recordHotkeyFailed);
        ShowFailure(screenshotField, screenshotNote, screenshotHotkeyFailed);
        ShowFailure(pickField, pickNote, pickHotkeyFailed);
        _recordKey.HotkeyChanged += (_, _) => ShowFailure(recordField, recordNote, false);
        _screenshotKey.HotkeyChanged += (_, _) => ShowFailure(screenshotField, screenshotNote, false);
        _pickKey.HotkeyChanged += (_, _) => ShowFailure(pickField, pickNote, false);

        var fps = new FieldBox { Bounds = new Rectangle(FieldLeft + FieldWidth - 112, 314, 112, 36) }; // at the right end
        _fps30.Bounds = new Rectangle(3, 3, 52, 30);
        _fps60.Bounds = new Rectangle(57, 3, 52, 30);
        fps.Controls.AddRange(new Control[] { _fps30, _fps60 });
        _fps30.Click += (_, _) => SelectFramesPerSecond(30);
        _fps60.Click += (_, _) => SelectFramesPerSecond(60);
        SelectFramesPerSecond(settings.FramesPerSecond);

        var ok = new RoundButton { Text = "確定", DialogResult = DialogResult.OK, Bounds = new Rectangle(314, 14, 84, 36) };
        ok.SetColors(Palette.Accent, Palette.AccentHover, Palette.AccentPressed, Color.White);
        var cancel = new RoundButton { Text = "取消", DialogResult = DialogResult.Cancel, Bounds = new Rectangle(222, 14, 84, 36) };
        cancel.SetColors(Palette.Field, Palette.Pressed, Palette.FieldButtonHover, Palette.Ink);
        AcceptButton = ok;
        CancelButton = cancel;

        Controls.AddRange(new Control[]
        {
            SettingName("存檔資料夾", folderField), folderField,
            SettingName("錄影快捷鍵", recordField), recordField, recordNote,
            SettingName("截圖快捷鍵", screenshotField), screenshotField, screenshotNote,
            SettingName("選視窗快捷鍵", pickField), pickField, pickNote,
            SettingName("每秒格數", fps), fps,
            BottomBar(420, 370, cancel, ok), close,
        });
        ResumeLayout(false);
        PerformLayout();
    }

    /// <summary>The original settings with this dialog's values applied.</summary>
    public AppSettings ToSettings(AppSettings original) => original with
    {
        OutputFolder = _folder.Text.Trim(),
        RecordHotkey = HotkeyText.Format(_recordKey.Hotkey),
        ScreenshotHotkey = HotkeyText.Format(_screenshotKey.Hotkey),
        PickHotkey = HotkeyText.Format(_pickKey.Hotkey),
        FramesPerSecond = _framesPerSecond,
    };

    private void SelectFramesPerSecond(int fps)
    {
        _framesPerSecond = fps;
        foreach (var (button, value) in new[] { (_fps30, 30), (_fps60, 60) })
        {
            if (value == fps)
                button.SetColors(Palette.Accent, Palette.AccentHover, Palette.AccentPressed, Color.White);
            else
                button.SetColors(Palette.Field, Palette.Pressed, Palette.FieldButtonHover, Palette.Muted);
        }
    }

    private void BrowseFolder()
    {
        using var dialog = new FolderBrowserDialog
        {
            InitialDirectory = _folder.Text,
            Description = "選擇存檔資料夾",
            UseDescriptionForTitle = true,
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
            _folder.Text = dialog.SelectedPath;
    }

    private static void ShowFailure(FieldBox field, Label note, bool failed)
    {
        field.Failed = failed;
        note.Visible = failed;
    }

    /// <summary>The note in the gap under a hotkey field.</summary>
    private static Label Note(FieldBox field) =>
        new() { Text = FailedMessage, AutoSize = true, Location = new Point(field.Left + 12, field.Bottom + 3), ForeColor = Palette.ErrorText, Visible = false };

    /// <summary>A setting's name, on the left of its field and level with it.</summary>
    private static Label SettingName(string text, Control field) => Caption(text, 24, field.Top + (field.Height - 15) / 2);
}
