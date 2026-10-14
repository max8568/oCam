using System.Diagnostics;
using System.Runtime.InteropServices;
using FrameCam.Core;
using FrameCam.Native;
using FrameCam.Recording;
using Microsoft.Win32;

namespace FrameCam.UI;

/// <summary>
/// The control bar (spec 8.4) and main window: owns the frame, magnifier, recorder, hotkeys and settings.
/// Closing it ends the program, after any recording in progress has been saved.
/// </summary>
internal sealed class ControlBarForm : PillBarWindow
{
    private readonly string _settingsPath;
    private AppSettings _settings;
    private readonly FrameWindow _frame;
    private readonly MagnifierWindow _magnifier;
    private readonly IScreenRecorder _recorder = new ScreenRecorderLibRecorder();
    private readonly HotkeyManager _hotkeys = new();

    private readonly System.Windows.Forms.Timer _elapsedTimer = new() { Interval = 500 };
    private readonly System.Windows.Forms.Timer _flashTimer = new() { Interval = 2000 };
    private string _baseStatus = "";
    private DateTime? _recordingSince;
    private string _recordingNote = "";
    private string _stopNote = "";
    private bool _stopping;
    private bool _closeAfterRecording;
    private bool _recordHotkeyFailed;
    private bool _screenshotHotkeyFailed;

    public ControlBarForm(string settingsPath)
    {
        _settingsPath = settingsPath;
        (_settings, string? settingsError) = SettingsStore.Load(settingsPath);
        var (region, monitor) = RegionPlacement.Restore(_settings.LastRegion, Screens.All());
        _frame = new FrameWindow(region, monitor) { SnapSource = WindowEnumerator.SnapTargetsFor };
        _magnifier = new MagnifierWindow(_frame);
        _frame.ScaleChanged += (_, _) => OnDisplayChanged();
        _frame.FormClosing += (_, e) =>
        {
            // Alt+F4 on the frame ends the program the normal way, which waits for a recording to be saved.
            if (e.CloseReason != CloseReason.UserClosing)
                return;
            e.Cancel = true;
            BeginInvoke(Close);
        };

        _recorder.Started += (_, _) => OnRecordingStarted();
        _recorder.Completed += (_, path) => OnRecordingEnded($"{_stopNote}已存檔 {Path.GetFileName(path)}");
        _recorder.Failed += (_, error) => OnRecordingEnded($"錄影失敗：{error}");
        _elapsedTimer.Tick += (_, _) => ShowRecordingTime();
        _flashTimer.Tick += (_, _) =>
        {
            _flashTimer.Stop();
            Status = _baseStatus;
        };

        Text = "FrameCam";

        if (settingsError is not null)
            SetStatus(settingsError);
    }

    internal FrameWindow Frame => _frame;

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        CaptureExclusion.Apply(Handle);
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        RegisterHotkeys(); // first: it sets the record button's text, and so the bar's width
        var area = Screen.FromRectangle(_frame.Monitor.Bounds.ToRectangle()).WorkingArea;
        Location = new Point(area.Right - 16 - (int)BarLayout.Bar.Right, area.Top + 16 - (int)BarLayout.Bar.Top);
        _frame.Show();
        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
    }

    protected override void OnItemClick(BarItem item)
    {
        switch (item)
        {
            case BarItem.Record: ToggleRecording(); break;
            case BarItem.Screenshot: TakeScreenshot(); break;
            case BarItem.Size: ShowSizeMenu(); break;
            case BarItem.Pick: PickWindow(); break;
            case BarItem.Folder: OpenFolder(); break;
            case BarItem.Settings: OpenSettings(); break;
            case BarItem.Close: Close(); break;
        }
    }

    protected override string TipFor(BarItem item) => item switch
    {
        BarItem.Screenshot => _screenshotHotkeyFailed ? "截圖" : $"截圖 ({_settings.ScreenshotHotkey})",
        BarItem.Size => "常用尺寸",
        BarItem.Pick => "選視窗",
        BarItem.Folder => "開啟存檔資料夾",
        BarItem.Settings => "設定",
        BarItem.Close => "結束 FrameCam",
        _ => "",
    };

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData) =>
        _frame.TryHandleArrow(keyData) || base.ProcessCmdKey(ref msg, keyData);

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (_recorder.IsRecording)
        {
            e.Cancel = true;
            _closeAfterRecording = true;
            if (!_stopping)
                StopRecording("");
            SetStatus("正在存檔，完成後結束…");
            return;
        }
        SaveSettings();
        base.OnFormClosing(e);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
        _hotkeys.Dispose();
        _recorder.Dispose();
        _elapsedTimer.Dispose();
        _flashTimer.Dispose();
        _magnifier.Dispose();
        _frame.Dispose();
        base.OnFormClosed(e);
    }

    // ---- Recording (spec 8.1, 9) ----

    private void ToggleRecording()
    {
        if (_stopping)
            return; // already finishing the file
        if (_recorder.IsRecording)
        {
            StopRecording("");
            return;
        }
        if (OutputDirectory.EnsureWritable(_settings.OutputFolder) is { } folderError)
        {
            SetStatus(folderError);
            return;
        }
        string path = FileNamer.NextPath(_settings.OutputFolder, DateTime.Now, ".mp4", File.Exists);
        var monitor = _frame.Monitor;
        bool hasAudio;
        try
        {
            hasAudio = _recorder.Start(new RecordingRequest(monitor.DeviceName, monitor.Bounds, _frame.CaptureRegion, _settings.FramesPerSecond, path));
        }
        catch (Exception ex)
        {
            SetStatus($"無法開始錄影：{ex.Message}");
            return;
        }
        _recordingNote = hasAudio ? "" : "找不到播放裝置，只錄畫面";
        _stopNote = "";
        _frame.Locked = true;
        SetElapsed("00:00:00");
        SetRecording(true);
        SetEnabled(BarItem.Size, false);
        SetEnabled(BarItem.Pick, false);
        SetStatus(hasAudio ? "準備錄影…" : $"準備錄影…（{_recordingNote}）");
    }

    private void StopRecording(string note)
    {
        _stopping = true;
        _stopNote = note;
        SetEnabled(BarItem.Record, false);
        _elapsedTimer.Stop();
        SetStatus(note + "正在存檔…");
        _recorder.Stop();
    }

    private void OnRecordingStarted()
    {
        if (_stopping)
            return;
        _recordingSince = DateTime.Now;
        _elapsedTimer.Start();
        ShowRecordingTime();
        SetStatus(_recordingNote);
    }

    private void ShowRecordingTime()
    {
        if (_recordingSince is { } since)
            SetElapsed($"{DateTime.Now - since:hh\\:mm\\:ss}");
    }

    private void OnRecordingEnded(string message)
    {
        _elapsedTimer.Stop();
        _recordingSince = null;
        _stopping = false;
        _frame.Locked = false;
        SetRecording(false);
        SetEnabled(BarItem.Record, true);
        SetEnabled(BarItem.Size, true);
        SetEnabled(BarItem.Pick, true);
        SetStatus(message);
        if (_closeAfterRecording)
            Close();
    }

    // ---- Screenshot (spec 8.2) ----

    private void TakeScreenshot()
    {
        if (OutputDirectory.EnsureWritable(_settings.OutputFolder) is { } folderError)
        {
            SetStatus(folderError);
            return;
        }
        string path = FileNamer.NextPath(_settings.OutputFolder, DateTime.Now, ".png", File.Exists);
        try
        {
            ScreenshotTaker.Save(_frame.CaptureRegion, path);
            FlashStatus($"已存 {Path.GetFileName(path)}");
        }
        catch (Exception ex) when (ex is ExternalException or IOException or UnauthorizedAccessException)
        {
            SetStatus($"截圖存檔失敗：{ex.Message}");
        }
    }

    // ---- Region tools (spec 7.5) ----

    private void ShowSizeMenu()
    {
        var menu = new ContextMenuStrip();
        var monitor = _frame.Monitor;
        foreach (var size in FrameAdjust.Presets)
        {
            var target = FrameAdjust.ApplyPreset(_frame.CaptureRegion, size, monitor.Bounds);
            var item = new ToolStripMenuItem($"{size.Width} × {size.Height}") { Enabled = target is not null };
            if (target is { } region)
                item.Click += (_, _) => _frame.SetRegion(region, monitor);
            menu.Items.Add(item);
        }
        var full = new ToolStripMenuItem("整個螢幕");
        full.Click += (_, _) => _frame.SetRegion(FrameAdjust.FullMonitor(monitor.Bounds), monitor);
        menu.Items.Add(full);
        menu.Closed += (_, _) => BeginInvoke(menu.Dispose); // after the item's Click has run
        menu.Show(this, BelowItem(BarItem.Size));
    }

    private void PickWindow()
    {
        using var picker = new WindowPicker();
        if (picker.ShowDialog(this) != DialogResult.OK || picker.Picked is not { } window)
            return;
        var monitor = Screens.At(picker.PickedAt);
        if (FrameAdjust.FitToWindow(window, monitor.Bounds) is { } region)
            _frame.SetRegion(region, monitor);
        else
            FlashStatus($"這個視窗太小，至少要 {FrameGeometry.MinSize}×{FrameGeometry.MinSize}");
    }

    private void OpenFolder()
    {
        if (OutputDirectory.EnsureWritable(_settings.OutputFolder) is { } folderError)
        {
            SetStatus(folderError);
            return;
        }
        Process.Start(new ProcessStartInfo(_settings.OutputFolder) { UseShellExecute = true });
    }

    // ---- Settings and hotkeys (spec 8.5) ----

    internal void OpenSettings()
    {
        _hotkeys.UnregisterAll(); // otherwise pressing F2 in a hotkey box records instead of reaching the box
        try
        {
            using var dialog = new SettingsForm(_settings, _recordHotkeyFailed, _screenshotHotkeyFailed);
            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;
            _settings = dialog.ToSettings(_settings);
            SaveSettings();
        }
        finally
        {
            RegisterHotkeys();
        }
    }

    private void RegisterHotkeys()
    {
        _hotkeys.UnregisterAll();
        _recordHotkeyFailed = !TryRegister(_settings.RecordHotkey, ToggleRecording);
        _screenshotHotkeyFailed = !TryRegister(_settings.ScreenshotHotkey, TakeScreenshot);
        SetRecordHint(_recordHotkeyFailed ? "" : _settings.RecordHotkey);
        var failed = new List<string>();
        if (_recordHotkeyFailed)
            failed.Add($"錄影 {_settings.RecordHotkey}");
        if (_screenshotHotkeyFailed)
            failed.Add($"截圖 {_settings.ScreenshotHotkey}");
        if (failed.Count > 0)
            SetStatus($"快捷鍵登記失敗，可能已經被其他程式使用：{string.Join("、", failed)}。用按鈕還是可以錄影和截圖。");
    }

    private bool TryRegister(string text, Action action) =>
        HotkeyText.TryParse(text, out var keys) && _hotkeys.Register(keys, action);

    private void SaveSettings()
    {
        var r = _frame.CaptureRegion;
        _settings = _settings with { LastRegion = new SavedRegion(_frame.Monitor.DeviceName, r.Left, r.Top, r.Width, r.Height) };
        try
        {
            SettingsStore.Save(_settingsPath, _settings);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            SetStatus($"設定存檔失敗：{ex.Message}");
        }
    }

    // ---- Monitor changes (spec 9) ----

    private void OnDisplaySettingsChanged(object? sender, EventArgs e) => BeginInvoke(OnDisplayChanged);

    private void OnDisplayChanged()
    {
        if (_recorder.IsRecording && !_stopping)
            StopRecording("螢幕設定改變，已停止錄影並");
        var r = _frame.CaptureRegion;
        var (region, monitor) = RegionPlacement.Restore(
            new SavedRegion(_frame.Monitor.DeviceName, r.Left, r.Top, r.Width, r.Height), Screens.All());
        _frame.SetRegion(region, monitor);
    }

    // ---- Status line ----

    /// <summary>Shows a message that stays until the next one.</summary>
    private void SetStatus(string text)
    {
        _baseStatus = text;
        if (!_flashTimer.Enabled)
            Status = text;
    }

    /// <summary>Shows a message for 2 seconds, then the previous one again.</summary>
    private void FlashStatus(string text)
    {
        Status = text;
        _flashTimer.Stop();
        _flashTimer.Start();
    }
}
