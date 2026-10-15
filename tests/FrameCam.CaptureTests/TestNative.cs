using System.Runtime.InteropServices;

namespace FrameCam.CaptureTests;

internal static class TestNative
{
    private const int SM_CMONITORS = 80;

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int index);

    public static int MonitorCount() => GetSystemMetrics(SM_CMONITORS);

    private const uint GA_ROOT = 2;

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT(int x, int y)
    {
        public int X = x;
        public int Y = y;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr WindowFromPoint(POINT point);

    [DllImport("user32.dll")]
    private static extern IntPtr GetAncestor(IntPtr hwnd, uint flags);

    /// <summary>The top-level window the mouse would reach at this screen point.</summary>
    public static IntPtr TopLevelWindowAt(Point p) => GetAncestor(WindowFromPoint(new POINT(p.X, p.Y)), GA_ROOT);

    private const uint WM_LBUTTONDOWN = 0x0201;
    private const uint WM_LBUTTONUP = 0x0202;
    private const int MK_LBUTTON = 0x0001;

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam);

    /// <summary>A left click at a point of the window's client area, without moving the real mouse.</summary>
    public static void Click(IntPtr hwnd, Point client)
    {
        var position = (IntPtr)((client.Y << 16) | (client.X & 0xFFFF));
        SendMessage(hwnd, WM_LBUTTONDOWN, MK_LBUTTON, position);
        SendMessage(hwnd, WM_LBUTTONUP, IntPtr.Zero, position);
    }

    private const uint WM_MOUSEMOVE = 0x0200;

    /// <summary>The mouse moving over a point of the window's client area, without moving the real mouse.</summary>
    public static void MouseMove(IntPtr hwnd, Point client) =>
        SendMessage(hwnd, WM_MOUSEMOVE, IntPtr.Zero, (IntPtr)((client.Y << 16) | (client.X & 0xFFFF)));

    /// <summary>The left button going down at a point of the window's client area, and staying down.</summary>
    public static void MouseDown(IntPtr hwnd, Point client) =>
        SendMessage(hwnd, WM_LBUTTONDOWN, MK_LBUTTON, (IntPtr)((client.Y << 16) | (client.X & 0xFFFF)));

    private const uint WM_CHAR = 0x0102;

    /// <summary>Types the text into a window as if from the keyboard, without touching the real keyboard.</summary>
    public static void Type(IntPtr hwnd, string text)
    {
        foreach (char c in text)
            SendMessage(hwnd, WM_CHAR, c, IntPtr.Zero);
    }

    private const uint WM_MOUSEWHEEL = 0x020A;

    /// <summary>One notch of the mouse wheel towards the user (scrolling down) over a point of the window's client area.</summary>
    public static void WheelDown(Control control, Point client)
    {
        var screen = control.PointToScreen(client);
        SendMessage(control.Handle, WM_MOUSEWHEEL, (IntPtr)(-120 << 16), (IntPtr)((screen.Y << 16) | (screen.X & 0xFFFF)));
    }

    private const uint WM_KEYDOWN = 0x0100;

    /// <summary>A key press sent to the window, without touching the real keyboard.</summary>
    public static void KeyDown(IntPtr hwnd, Keys key) => SendMessage(hwnd, WM_KEYDOWN, (IntPtr)(int)key, IntPtr.Zero);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hwnd, int index);

    /// <summary>Whether the window stays above windows that are not "always on top" (WS_EX_TOPMOST).</summary>
    public static bool IsTopmost(IntPtr hwnd) => (GetWindowLong(hwnd, -20) & 0x8) != 0;

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowDisplayAffinity(IntPtr hwnd, out uint affinity);

    public const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;

    public static uint DisplayAffinity(IntPtr hwnd) =>
        GetWindowDisplayAffinity(hwnd, out uint affinity) ? affinity : throw new System.ComponentModel.Win32Exception();
}
