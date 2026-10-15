namespace FrameCam.CaptureTests;

/// <summary>
/// A test window that stays above ordinary windows such as the terminal running the tests. The TopMost
/// property set before the window exists does not do it (it only took effect when another test window
/// happened to be in front), so the style is set when the window is created.
/// </summary>
internal class OnTopForm : Form
{
    private const int WS_EX_TOPMOST = 0x00000008;

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= WS_EX_TOPMOST;
            return cp;
        }
    }
}
