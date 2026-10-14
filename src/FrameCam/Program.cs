using FrameCam.Core;
using FrameCam.UI;

namespace FrameCam;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        // Temporary harness until Task 11: the frame and its magnifier. Click the frame, then Alt+F4 to quit.
        var (region, monitor) = RegionPlacement.Restore(null, Screens.All());
        var frame = new FrameWindow(region, monitor);
        using var magnifier = new MagnifierWindow(frame);
        Application.Run(frame);
    }
}
