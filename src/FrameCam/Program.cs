using FrameCam.Core;
using FrameCam.UI;

namespace FrameCam;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        // Settings live beside the exe so they travel with a copied FrameCam folder.
        Application.Run(new ControlBarForm(SettingsStore.PathIn(AppContext.BaseDirectory), SettingsStore.OldPath));
    }
}
