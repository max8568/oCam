namespace FrameCam.Core;

public static class OutputDirectory
{
    public static string Default =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "FrameCam");

    /// <summary>Creates the folder if needed and checks a file can be written there. Returns null when it can, otherwise a message for the user.</summary>
    public static string? EnsureWritable(string folder)
    {
        try
        {
            Directory.CreateDirectory(folder);
            string probe = Path.Combine(folder, $".framecam-write-test-{Guid.NewGuid():N}");
            File.WriteAllBytes(probe, []);
            File.Delete(probe);
            return null;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return $"無法寫入存檔資料夾 {folder}：{e.Message}";
        }
    }
}
