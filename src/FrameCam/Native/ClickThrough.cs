namespace FrameCam.Native;

/// <summary>A window that never takes focus and lets every mouse click through to the windows below.</summary>
internal static class ClickThrough
{
    public const int ExStyle =
        NativeMethods.WS_EX_LAYERED | NativeMethods.WS_EX_TRANSPARENT | NativeMethods.WS_EX_NOACTIVATE | NativeMethods.WS_EX_TOOLWINDOW;

    /// <summary>Call from OnHandleCreated: a layered window stays invisible until its opacity is set.</summary>
    public static void MakeOpaque(IntPtr hwnd) =>
        NativeMethods.SetLayeredWindowAttributes(hwnd, 0, 255, NativeMethods.LWA_ALPHA);
}
