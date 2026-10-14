namespace FrameCam.Native;

/// <summary>Global hotkeys (spec 8.5): RegisterHotKey on a hidden message-only window. Actions run on the UI thread.</summary>
internal sealed class HotkeyManager : NativeWindow, IDisposable
{
    private const int WM_HOTKEY = 0x0312;
    private static readonly IntPtr HWND_MESSAGE = new(-3);

    private readonly Dictionary<int, Action> _actions = [];
    private int _nextId = 1;

    public HotkeyManager() => CreateHandle(new CreateParams { Parent = HWND_MESSAGE });

    /// <summary>Registers a key such as Keys.F2 or Keys.Control | Keys.R. False when another program already owns it.</summary>
    public bool Register(Keys keys, Action action)
    {
        uint modifiers = NativeMethods.MOD_NOREPEAT;
        if ((keys & Keys.Alt) != 0) modifiers |= NativeMethods.MOD_ALT;
        if ((keys & Keys.Control) != 0) modifiers |= NativeMethods.MOD_CONTROL;
        if ((keys & Keys.Shift) != 0) modifiers |= NativeMethods.MOD_SHIFT;
        int id = _nextId++;
        if (!NativeMethods.RegisterHotKey(Handle, id, modifiers, (uint)(keys & Keys.KeyCode)))
            return false;
        _actions[id] = action;
        return true;
    }

    public void UnregisterAll()
    {
        foreach (int id in _actions.Keys)
            NativeMethods.UnregisterHotKey(Handle, id);
        _actions.Clear();
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_HOTKEY && _actions.TryGetValue((int)m.WParam, out var action))
            action();
        else
            base.WndProc(ref m);
    }

    public void Dispose()
    {
        UnregisterAll();
        DestroyHandle();
    }
}
