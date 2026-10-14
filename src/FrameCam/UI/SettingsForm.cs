using System.ComponentModel;
using System.Drawing.Drawing2D;
using FrameCam.Core;
using FrameCam.Native;

namespace FrameCam.UI;

/// <summary>
/// The settings dialog (spec 8.5): save folder, the two hotkeys, frames per second. Design 21: no title bar
/// (drag it by its top), round fields, and 30 / 60 as two buttons. A hotkey that could not be registered
/// gets a red field and a note under it until another key is picked.
/// </summary>
internal sealed class SettingsForm : Form
{
    private const string FailedMessage = "登記失敗，可能被其他程式使用";
    private const int HeaderHeight = 52;

    private readonly TextBox _folder = new() { BorderStyle = BorderStyle.None, AccessibleName = "存檔資料夾" };
    private readonly HotkeyTextBox _recordKey = new() { BorderStyle = BorderStyle.None, AccessibleName = "錄影快捷鍵" };
    private readonly HotkeyTextBox _screenshotKey = new() { BorderStyle = BorderStyle.None, AccessibleName = "截圖快捷鍵" };
    private readonly RoundButton _fps30 = new() { Text = "30", AccessibleName = "每秒 30 格" };
    private readonly RoundButton _fps60 = new() { Text = "60", AccessibleName = "每秒 60 格" };
    private int _framesPerSecond;

    public SettingsForm(AppSettings settings, bool recordHotkeyFailed, bool screenshotHotkeyFailed)
    {
        SuspendLayout();
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        Text = "FrameCam 設定";
        FormBorderStyle = FormBorderStyle.None;
        BackColor = Palette.Surface;
        ForeColor = Palette.Strong;
        ShowInTaskbar = false;
        TopMost = true; // its owner, the control bar, is always on top; a dialog that is not would open behind it
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(420, 328);

        _folder.Text = settings.OutputFolder;
        if (HotkeyText.TryParse(settings.RecordHotkey, out var record))
            _recordKey.Hotkey = record;
        if (HotkeyText.TryParse(settings.ScreenshotHotkey, out var screenshot))
            _screenshotKey.Hotkey = screenshot;

        var close = new RoundButton { Glyph = Glyph.Close, GlyphSize = 14, Bounds = new Rectangle(374, 14, 32, 32), DialogResult = DialogResult.Cancel, TabStop = false, AccessibleName = "關閉" };
        close.SetColors(Palette.Field, Palette.Pressed, Palette.FieldButtonHover, Palette.Muted);

        var browse = new RoundButton { Glyph = Glyph.Folder, Bounds = new Rectangle(342, 4, 30, 30), AccessibleName = "瀏覽資料夾" };
        browse.SetColors(Palette.FieldButton, Palette.FieldButtonHover, Palette.Dots, Palette.Ink);
        browse.Click += (_, _) => BrowseFolder();
        var folderField = Field(new Rectangle(22, 80, 376, 38), _folder, browse);

        var recordField = Field(new Rectangle(22, 152, 182, 38), _recordKey);
        var recordNote = Note(34, 194);
        var screenshotField = Field(new Rectangle(216, 152, 182, 38), _screenshotKey);
        var screenshotNote = Note(228, 194);
        ShowFailure(recordField, recordNote, recordHotkeyFailed);
        ShowFailure(screenshotField, screenshotNote, screenshotHotkeyFailed);
        _recordKey.HotkeyChanged += (_, _) => ShowFailure(recordField, recordNote, false);
        _screenshotKey.HotkeyChanged += (_, _) => ShowFailure(screenshotField, screenshotNote, false);

        var fps = new FieldBox { Bounds = new Rectangle(286, 218, 112, 36) };
        _fps30.Bounds = new Rectangle(3, 3, 52, 30);
        _fps60.Bounds = new Rectangle(57, 3, 52, 30);
        fps.Controls.AddRange(new Control[] { _fps30, _fps60 });
        _fps30.Click += (_, _) => SelectFramesPerSecond(30);
        _fps60.Click += (_, _) => SelectFramesPerSecond(60);
        SelectFramesPerSecond(settings.FramesPerSecond);

        var ok = new RoundButton { Text = "確定", DialogResult = DialogResult.OK, Bounds = new Rectangle(314, 272, 84, 36) };
        ok.SetColors(Palette.Accent, Palette.AccentHover, Palette.AccentPressed, Color.White);
        var cancel = new RoundButton { Text = "取消", DialogResult = DialogResult.Cancel, Bounds = new Rectangle(222, 272, 84, 36) };
        cancel.SetColors(Palette.Field, Palette.Pressed, Palette.FieldButtonHover, Palette.Ink);
        AcceptButton = ok;
        CancelButton = cancel;

        Controls.AddRange(new Control[]
        {
            Caption("存檔資料夾", 34, 58), folderField,
            Caption("錄影快捷鍵", 34, 130), recordField, recordNote,
            Caption("截圖快捷鍵", 228, 130), screenshotField, screenshotNote,
            Caption("每秒格數", 34, 228), fps,
            cancel, ok, close,
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
        FramesPerSecond = _framesPerSecond,
    };

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ClassStyle |= NativeMethods.CS_DROPSHADOW;
            return cp;
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        // Windows 11 rounds the corners and draws a thin border; earlier versions ignore both.
        int corners = NativeMethods.DWMWCP_ROUND;
        NativeMethods.DwmSetWindowAttribute(Handle, NativeMethods.DWMWA_WINDOW_CORNER_PREFERENCE, ref corners, sizeof(int));
        int border = ColorTranslator.ToWin32(Palette.Border);
        NativeMethods.DwmSetWindowAttribute(Handle, NativeMethods.DWMWA_BORDER_COLOR, ref border, sizeof(int));
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var title = new Font(Font.FontFamily, Font.Size * 4 / 3, FontStyle.Bold, Font.Unit);
        TextRenderer.DrawText(e.Graphics, "設定", title, new Point(LogicalToDeviceUnits(22), LogicalToDeviceUnits(18)), Palette.Strong);
    }

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);
        if (m.Msg != NativeMethods.WM_NCHITTEST || m.Result != NativeMethods.HTCLIENT)
            return;
        long lParam = m.LParam;
        var p = PointToClient(new Point((short)(lParam & 0xFFFF), (short)((lParam >> 16) & 0xFFFF)));
        if (p.Y < LogicalToDeviceUnits(HeaderHeight))
            m.Result = NativeMethods.HTCAPTION; // drag the dialog by its top
    }

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

    /// <summary>A round field holding a borderless text box, and maybe a button at its right end.</summary>
    private static FieldBox Field(Rectangle bounds, TextBox box, Control? button = null)
    {
        var field = new FieldBox { Bounds = bounds };
        box.Location = new Point(14, 10);
        box.Width = (button is null ? bounds.Width - 14 : button.Left - 8) - 14;
        field.Controls.Add(box);
        if (button is not null)
            field.Controls.Add(button);
        field.Failed = false;
        return field;
    }

    private static Label Caption(string text, int x, int y) =>
        new() { Text = text, AutoSize = true, Location = new Point(x, y), ForeColor = Palette.Muted };

    private static Label Note(int x, int y) =>
        new() { Text = FailedMessage, AutoSize = true, Location = new Point(x, y), ForeColor = Palette.ErrorText, Visible = false };

    /// <summary>A light-gray area with fully round ends; red-tinted with a red outline when Failed.</summary>
    private sealed class FieldBox : Panel
    {
        private bool _failed;

        public FieldBox()
        {
            DoubleBuffered = true;
            BackColor = Palette.Field;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Failed
        {
            get => _failed;
            set
            {
                _failed = value;
                BackColor = value ? Palette.ErrorField : Palette.Field;
                foreach (var box in Controls.OfType<TextBox>())
                    box.BackColor = BackColor;
                Invalidate();
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent?.BackColor ?? Palette.Surface);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            var box = new RectangleF(0, 0, Width, Height);
            using (var fill = new SolidBrush(BackColor))
            using (var shape = Shapes.Rounded(box, Height / 2f))
                g.FillPath(fill, shape);
            if (!_failed)
                return;
            using var pen = new Pen(Palette.ErrorBorder, 1);
            using var outline = Shapes.Rounded(RectangleF.Inflate(box, -0.5f, -0.5f), Height / 2f - 0.5f);
            g.DrawPath(pen, outline);
        }
    }
}
