using FrameCam.Core;
using FrameCam.UI;

namespace FrameCam;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        // Temporary harness until Task 11: only the frame. Click the frame, then Alt+F4 to quit.
        var (region, monitor) = RegionPlacement.Restore(null, Screens.All());
        Application.Run(new FrameWindow(region, monitor));
    }
}
