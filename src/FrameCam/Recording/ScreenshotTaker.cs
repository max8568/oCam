using System.Drawing.Imaging;
using FrameCam.Core;
using FrameCam.Native;

namespace FrameCam.Recording;

public static class ScreenshotTaker
{
    /// <summary>
    /// Saves the region's screen pixels as a PNG, without the mouse cursor. Semi-transparent windows are
    /// included; windows excluded from capture do not appear.
    /// </summary>
    public static void Save(PixelRect region, string path)
    {
        using var bitmap = new Bitmap(region.Width, region.Height, PixelFormat.Format24bppRgb);
        ScreenPixels.Copy(region, bitmap);
        bitmap.Save(path, ImageFormat.Png);
    }
}
