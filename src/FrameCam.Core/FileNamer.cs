using System.Globalization;

namespace FrameCam.Core;

public static class FileNamer
{
    /// <summary>folder\yyyy-MM-dd_HH-mm-ss.ext, or ..._2.ext, ..._3.ext when that name is taken (spec 8.3).</summary>
    public static string NextPath(string folder, DateTime now, string extension, Func<string, bool> exists)
    {
        string stem = now.ToString("yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture);
        string path = Path.Combine(folder, stem + extension);
        for (int n = 2; exists(path); n++)
            path = Path.Combine(folder, $"{stem}_{n}{extension}");
        return path;
    }
}
