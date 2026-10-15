using FrameCam.Core;

namespace FrameCam.UI;

/// <summary>
/// The settings dialog (spec 8.5): save folder, the hotkeys for recording, pausing, screenshots and picking a window,
/// frames per second, hiding the cursor in videos. Design 21: round fields,
/// and 30 / 60 as two buttons. A hotkey that could not be registered gets a red field and a note under it
/// until another key is picked. The bottom bar shows FrameCam's version on the left.
/// </summary>
internal sealed class SettingsForm : LightDialog
{
    private const string FailedMessage = "登記失敗，可能被其他程式使用";
    private const int FieldLeft = 118;
    private const int FieldWidth = 280;

    private readonly TextBox _folder = new() { BorderStyle = BorderStyle.None, AccessibleName = "存檔資料夾" };
    private readonly HotkeyTextBox _recordKey = new() { BorderStyle = BorderStyle.None, AccessibleName = "錄影快捷鍵" };
    private readonly HotkeyTextBox _pauseKey = new() { BorderStyle = BorderStyle.None, AccessibleName = "暫停快捷鍵", AllowSpace = true };
    private readonly HotkeyTextBox _screenshotKey = new() { BorderStyle = BorderStyle.None, AccessibleName = "截圖快捷鍵" };
    private readonly HotkeyTextBox _pickKey = new() { BorderStyle = BorderStyle.None, AccessibleName = "選視窗快捷鍵" };
    private readonly RoundButton _fps30 = new() { Text = "30", AccessibleName = "每秒 30 格" };
    private readonly RoundButton _fps60 = new() { Text = "60", AccessibleName = "每秒 60 格" };
    private readonly RoundCheckBox _hideCursor = new() { Text = "隱藏滑鼠游標", ForeColor = Palette.Muted };
    private int _framesPerSecond;

    public SettingsForm(AppSettings settings, bool recordHotkeyFailed, bool screenshotHotkeyFailed, bool pickHotkeyFailed = false, bool pauseHotkeyFailed = false)
        : base("設定", "FrameCam 設定", new Size(420, 558))
    {
        SuspendLayout();

        _folder.Text = settings.OutputFolder;
        _hideCursor.Checked = settings.HideCursor;
        if (HotkeyText.TryParse(settings.RecordHotkey, out var record))
            _recordKey.Hotkey = record;
        if (HotkeyText.TryParse(settings.PauseHotkey, out var pause, allowSpace: true))
            _pauseKey.Hotkey = pause;
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
        var pauseField = FieldBox.Around(new Rectangle(FieldLeft, 190, FieldWidth, 38), _pauseKey);
        var pauseNote = Note(pauseField);
        var screenshotField = FieldBox.Around(new Rectangle(FieldLeft, 252, FieldWidth, 38), _screenshotKey);
        var screenshotNote = Note(screenshotField);
        var pickField = FieldBox.Around(new Rectangle(FieldLeft, 314, FieldWidth, 38), _pickKey);
        var pickNote = Note(pickField);
        ShowFailure(recordField, recordNote, recordHotkeyFailed);
        ShowFailure(pauseField, pauseNote, pauseHotkeyFailed);
        ShowFailure(screenshotField, screenshotNote, screenshotHotkeyFailed);
        ShowFailure(pickField, pickNote, pickHotkeyFailed);
        _recordKey.HotkeyChanged += (_, _) => ShowFailure(recordField, recordNote, false);
        _pauseKey.HotkeyChanged += (_, _) => ShowFailure(pauseField, pauseNote, false);
        _screenshotKey.HotkeyChanged += (_, _) => ShowFailure(screenshotField, screenshotNote, false);
        _pickKey.HotkeyChanged += (_, _) => ShowFailure(pickField, pickNote, false);

        var fps = new FieldBox { Bounds = new Rectangle(FieldLeft + FieldWidth - 112, 376, 112, 36) }; // at the right end
        _fps30.Bounds = new Rectangle(3, 3, 52, 30);
        _fps60.Bounds = new Rectangle(57, 3, 52, 30);
        fps.Controls.AddRange(new Control[] { _fps30, _fps60 });
        _fps30.Click += (_, _) => SelectFramesPerSecond(30);
        _fps60.Click += (_, _) => SelectFramesPerSecond(60);
        SelectFramesPerSecond(settings.FramesPerSecond);

        // In the names' column, one row under the frame rate; the box starts where the names do.
        _hideCursor.Bounds = new Rectangle(20, 443, 200, 26);

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
            SettingName("暫停快捷鍵", pauseField), pauseField, pauseNote,
            SettingName("截圖快捷鍵", screenshotField), screenshotField, screenshotNote,
            SettingName("選視窗快捷鍵", pickField), pickField, pickNote,
            SettingName("每秒格數", fps), fps,
            _hideCursor,
            BottomBar(420, 494, Version(), cancel, ok), close,
        });
        ResumeLayout(false);
        PerformLayout();
    }

    /// <summary>The original settings with this dialog's values applied.</summary>
    public AppSettings ToSettings(AppSettings original) => original with
    {
        OutputFolder = _folder.Text.Trim(),
        RecordHotkey = HotkeyText.Format(_recordKey.Hotkey),
        PauseHotkey = HotkeyText.Format(_pauseKey.Hotkey),
        ScreenshotHotkey = HotkeyText.Format(_screenshotKey.Hotkey),
        PickHotkey = HotkeyText.Format(_pickKey.Hotkey),
        FramesPerSecond = _framesPerSecond,
        HideCursor = _hideCursor.Checked,
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

    /// <summary>"v1.0.51": 1.0 and the number of commits, set when FrameCam is built.</summary>
    private static Label Version() =>
        Caption("v" + typeof(SettingsForm).Assembly.GetName().Version!.ToString(3), 24, (BottomBarHeight - 15) / 2);

    /// <summary>A setting's name, on the left of its field and level with it.</summary>
    private static Label SettingName(string text, Control field) => Caption(text, 24, field.Top + (field.Height - 15) / 2);
}
