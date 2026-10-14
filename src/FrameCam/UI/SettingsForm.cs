using FrameCam.Core;

namespace FrameCam.UI;

/// <summary>The settings dialog (spec 8.5): save folder, the two hotkeys, frames per second.</summary>
internal sealed class SettingsForm : Form
{
    private static readonly Color FailedColor = Color.MistyRose;

    private readonly TextBox _folder = new() { Width = 360 };
    private readonly HotkeyTextBox _recordKey = new();
    private readonly HotkeyTextBox _screenshotKey = new();
    private readonly ComboBox _fps = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 80 };

    public SettingsForm(AppSettings settings, bool recordHotkeyFailed, bool screenshotHotkeyFailed)
    {
        SuspendLayout();
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        Text = "FrameCam 設定";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        TopMost = true; // its owner, the control bar, is always on top; a dialog that is not would open behind it
        StartPosition = FormStartPosition.CenterParent;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;

        _folder.Text = settings.OutputFolder;
        if (HotkeyText.TryParse(settings.RecordHotkey, out var record))
            _recordKey.Hotkey = record;
        if (HotkeyText.TryParse(settings.ScreenshotHotkey, out var screenshot))
            _screenshotKey.Hotkey = screenshot;
        if (recordHotkeyFailed)
            _recordKey.BackColor = FailedColor;
        if (screenshotHotkeyFailed)
            _screenshotKey.BackColor = FailedColor;
        _fps.Items.AddRange(new object[] { 30, 60 });
        _fps.SelectedItem = settings.FramesPerSecond;

        var browse = new Button { Text = "瀏覽…", AutoSize = true };
        browse.Click += (_, _) => BrowseFolder();
        var ok = new Button { Text = "確定", DialogResult = DialogResult.OK, AutoSize = true };
        var cancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel, AutoSize = true };
        AcceptButton = ok;
        CancelButton = cancel;

        var table = new TableLayoutPanel { ColumnCount = 3, AutoSize = true, Padding = new Padding(12) };
        AddRow(table, "存檔資料夾", _folder, browse);
        AddRow(table, "錄影快捷鍵", _recordKey);
        AddRow(table, "截圖快捷鍵", _screenshotKey);
        AddRow(table, "每秒格數", _fps);
        var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, AutoSize = true, Dock = DockStyle.Fill };
        buttons.Controls.AddRange(new Control[] { cancel, ok });
        table.Controls.Add(buttons, 0, table.RowCount);
        table.SetColumnSpan(buttons, 3);
        Controls.Add(table);
        ResumeLayout(false);
        PerformLayout();
    }

    /// <summary>The original settings with this dialog's values applied.</summary>
    public AppSettings ToSettings(AppSettings original) => original with
    {
        OutputFolder = _folder.Text.Trim(),
        RecordHotkey = HotkeyText.Format(_recordKey.Hotkey),
        ScreenshotHotkey = HotkeyText.Format(_screenshotKey.Hotkey),
        FramesPerSecond = (int)_fps.SelectedItem!,
    };

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

    private static void AddRow(TableLayoutPanel table, string label, Control input, Control? extra = null)
    {
        int row = table.RowCount++;
        table.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 8, 12, 3) }, 0, row);
        table.Controls.Add(input, 1, row);
        if (extra is not null)
            table.Controls.Add(extra, 2, row);
    }
}
