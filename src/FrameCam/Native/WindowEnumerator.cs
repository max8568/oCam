using System.Runtime.InteropServices;
using FrameCam.Core;

namespace FrameCam.Native;

/// <summary>A top-level window's outer bounds (without the invisible shadow border) and client area, in physical pixels.</summary>
internal sealed record WindowInfo(IntPtr Handle, PixelRect Outer, PixelRect Client);

internal static class WindowEnumerator
{
    /// <summary>Visible, non-minimized, non-cloaked top-level windows of other processes, topmost first.</summary>
    public static List<WindowInfo> VisibleWindows()
    {
        var result = new List<WindowInfo>();
        uint ownProcess = (uint)Environment.ProcessId;
        NativeMethods.EnumWindows((hwnd, _) =>
        {
            if (TryDescribe(hwnd, ownProcess) is { } window)
                result.Add(window);
            return true;
        }, IntPtr.Zero);
        return result;
    }

    /// <summary>Outer and client edges of the visible windows on the monitor (spec 7.5).</summary>
    public static SnapTargets SnapTargetsFor(PixelRect monitor) =>
        SnapTargets.FromRects(VisibleWindows().SelectMany(w => new[] { w.Outer, w.Client }), monitor);

    /// <summary>Bounds of the window's visible child windows at any depth, such as a browser's page area.</summary>
    public static List<PixelRect> ChildRects(IntPtr hwnd)
    {
        var result = new List<PixelRect>();
        NativeMethods.EnumChildWindows(hwnd, (child, _) =>
        {
            if (NativeMethods.IsWindowVisible(child) && NativeMethods.GetWindowRect(child, out var r))
                result.Add(PixelRect.FromEdges(r.Left, r.Top, r.Right, r.Bottom));
            return true;
        }, IntPtr.Zero);
        return result;
    }

    private static WindowInfo? TryDescribe(IntPtr hwnd, uint ownProcess)
    {
        if (!NativeMethods.IsWindowVisible(hwnd) || NativeMethods.IsIconic(hwnd))
            return null;
        NativeMethods.GetWindowThreadProcessId(hwnd, out uint process);
        if (process == ownProcess)
            return null;
        if (NativeMethods.DwmGetWindowAttribute(hwnd, NativeMethods.DWMWA_CLOAKED, out int cloaked, sizeof(int)) == 0 && cloaked != 0)
            return null;
        if (NativeMethods.DwmGetWindowAttribute(hwnd, NativeMethods.DWMWA_EXTENDED_FRAME_BOUNDS, out NativeMethods.RECT outer, Marshal.SizeOf<NativeMethods.RECT>()) != 0)
            return null;
        if (!NativeMethods.GetClientRect(hwnd, out var client))
            return null;
        var origin = new NativeMethods.POINT(0, 0);
        NativeMethods.ClientToScreen(hwnd, ref origin);

        var outerRect = PixelRect.FromEdges(outer.Left, outer.Top, outer.Right, outer.Bottom);
        if (outerRect.Width <= 0 || outerRect.Height <= 0)
            return null;
        return new WindowInfo(hwnd, outerRect, new PixelRect(origin.X, origin.Y, client.Right, client.Bottom));
    }
}
