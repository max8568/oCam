using FrameCam.Native;

namespace FrameCam.CaptureTests;

/// <summary>A red window marked "exclude from capture": it must never appear in a recording or screenshot.</summary>
internal sealed class ExcludedOverlayForm : OnTopForm
{
    public ExcludedOverlayForm(Rectangle bounds)
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        AutoScaleMode = AutoScaleMode.None;
        BackColor = Color.Red;
        Bounds = bounds;
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        CaptureExclusion.Apply(Handle);
    }

    protected override void OnDpiChanged(DpiChangedEventArgs e) => e.Cancel = true;
}
