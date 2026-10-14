using System.ComponentModel;

namespace FrameCam.UI;

/// <summary>A box that records the next key combination pressed in it. Modifier keys alone are ignored.</summary>
internal sealed class HotkeyTextBox : TextBox
{
    private Keys _hotkey;

    public HotkeyTextBox()
    {
        ReadOnly = true;
        BackColor = SystemColors.Window;
        ShortcutsEnabled = false;
        Width = 160;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Keys Hotkey
    {
        get => _hotkey;
        set
        {
            _hotkey = value;
            Text = HotkeyText.Format(value);
            BackColor = SystemColors.Window;
        }
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        var code = keyData & Keys.KeyCode;
        bool plain = (keyData & Keys.Modifiers) == Keys.None;
        if (plain && code is Keys.Tab or Keys.Escape or Keys.Enter)
            return base.ProcessCmdKey(ref msg, keyData); // keep dialog navigation
        if (code is Keys.ControlKey or Keys.ShiftKey or Keys.Menu or Keys.LWin or Keys.RWin)
            return true; // wait for the actual key
        Hotkey = keyData;
        return true;
    }
}
