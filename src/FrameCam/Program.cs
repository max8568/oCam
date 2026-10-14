using FrameCam.Core;
using FrameCam.UI;

namespace FrameCam;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new ControlBarForm(SettingsStore.DefaultPath));
    }
}
