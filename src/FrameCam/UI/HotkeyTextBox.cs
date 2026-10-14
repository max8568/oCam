using System.ComponentModel;

namespace FrameCam.UI;

/// <summary>A box that records the next key combination pressed in it. Keys that HotkeyText.IsUsable rejects are ignored.</summary>
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

    /// <summary>Raised when the key combination changes, by the user or in code.</summary>
    public event EventHandler? HotkeyChanged;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Keys Hotkey
    {
        get => _hotkey;
        set
        {
            if (value == _hotkey)
                return;
            _hotkey = value;
            Text = HotkeyText.Format(value);
            HotkeyChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        var code = keyData & Keys.KeyCode;
        bool plain = (keyData & Keys.Modifiers) == Keys.None;
        if (plain && code is Keys.Tab or Keys.Escape or Keys.Enter)
            return base.ProcessCmdKey(ref msg, keyData); // keep dialog navigation
        if (HotkeyText.IsUsable(keyData))
            Hotkey = keyData;
        return true; // otherwise wait for a usable combination
    }
}
