namespace FrameCam.CaptureTests;

internal static class ControlTree
{
    /// <summary>Every control of type T inside root, at any depth.</summary>
    public static IEnumerable<T> All<T>(Control root) where T : Control => Descendants(root).OfType<T>();

    private static IEnumerable<Control> Descendants(Control root) =>
        root.Controls.Cast<Control>().SelectMany(c => Descendants(c).Prepend(c));
}
