using System.Runtime.InteropServices;

namespace FrameCam.Native;

internal static partial class NativeMethods
{
    public const int WS_EX_LAYERED = 0x00080000;
    public const int WS_EX_TRANSPARENT = 0x00000020;
    public const int WS_EX_NOACTIVATE = 0x08000000;
    public const int WS_EX_TOPMOST = 0x00000008;
    public const uint LWA_ALPHA = 0x2;
    public const uint ULW_ALPHA = 0x2;
    public const byte AC_SRC_OVER = 0x0;
    public const byte AC_SRC_ALPHA = 0x1;

    [StructLayout(LayoutKind.Sequential)]
    public struct SIZE(int cx, int cy)
    {
        public int Cx = cx;
        public int Cy = cy;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BLENDFUNCTION
    {
        public byte BlendOp;
        public byte BlendFlags;
        public byte SourceConstantAlpha;
        public byte AlphaFormat;
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint colorKey, byte alpha, uint flags);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr screenDc, IntPtr position, ref SIZE size, IntPtr sourceDc,
        ref POINT sourcePosition, uint colorKey, ref BLENDFUNCTION blend, uint flags);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr screenDc, ref POINT position, ref SIZE size, IntPtr sourceDc,
        ref POINT sourcePosition, uint colorKey, ref BLENDFUNCTION blend, uint flags);
}
