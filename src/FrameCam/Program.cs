using FrameCam.Core;
using FrameCam.UI;

namespace FrameCam;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        using var iconStream = typeof(Program).Assembly.GetManifestResourceStream("FrameCam.AppIcon")
            ?? throw new InvalidOperationException("The application icon resource is missing.");
        using var appIcon = new Icon(iconStream);
        // Settings live beside the exe so they travel with a copied FrameCam folder.
        using var controlBar = new ControlBarForm(SettingsStore.PathIn(AppContext.BaseDirectory), SettingsStore.OldPath)
        {
            Icon = appIcon,
        };
        Application.Run(controlBar);
    }
}
