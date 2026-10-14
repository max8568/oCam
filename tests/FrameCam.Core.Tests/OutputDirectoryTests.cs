using FrameCam.Core;

namespace FrameCam.Core.Tests;

public class OutputDirectoryTests
{
    [Fact]
    public void Creates_missing_folders_and_leaves_no_probe_file()
    {
        string folder = Path.Combine(Directory.CreateTempSubdirectory("framecam-").FullName, "a", "b");
        Assert.Null(OutputDirectory.EnsureWritable(folder));
        Assert.True(Directory.Exists(folder));
        Assert.Empty(Directory.GetFiles(folder));
    }

    [Fact]
    public void Reports_a_path_that_is_a_file()
    {
        string file = Path.Combine(Directory.CreateTempSubdirectory("framecam-").FullName, "file.txt");
        File.WriteAllText(file, "x");
        string? message = OutputDirectory.EnsureWritable(file);
        Assert.NotNull(message);
        Assert.Contains(file, message);
    }

    [Fact]
    public void Default_is_framecam_under_videos() =>
        Assert.Equal(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "FrameCam"), OutputDirectory.Default);
}
