using FrameCam.Core;
using FrameCam.Native;
using FrameCam.UI;

namespace FrameCam;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        // Temporary harness until Task 11: pick a window first, then adjust the frame with snapping.
        var (region, monitor) = RegionPlacement.Restore(null, Screens.All());
        using (var picker = new WindowPicker())
        {
            if (picker.ShowDialog() == DialogResult.OK && picker.Picked is { } window)
            {
                var pickedMonitor = Screens.At(picker.PickedAt);
                if (FrameAdjust.FitToWindow(window, pickedMonitor.Bounds) is { } fitted)
                {
                    region = fitted;
                    monitor = pickedMonitor;
                }
            }
        }
        var frame = new FrameWindow(region, monitor) { SnapSource = WindowEnumerator.SnapTargetsFor };
        using var magnifier = new MagnifierWindow(frame);
        Application.Run(frame);
    }
}
