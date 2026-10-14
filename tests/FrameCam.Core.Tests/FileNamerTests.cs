using FrameCam.Core;

namespace FrameCam.Core.Tests;

public class FileNamerTests
{
    private static readonly DateTime Now = new(2026, 10, 14, 15, 30, 5);

    [Fact]
    public void Name_is_the_local_time() =>
        Assert.Equal(Path.Combine(@"C:\out", "2026-10-14_15-30-05.mp4"), FileNamer.NextPath(@"C:\out", Now, ".mp4", _ => false));

    [Fact]
    public void Taken_names_get_a_counter()
    {
        var taken = new HashSet<string>
        {
            Path.Combine(@"C:\out", "2026-10-14_15-30-05.png"),
            Path.Combine(@"C:\out", "2026-10-14_15-30-05_2.png"),
        };
        Assert.Equal(Path.Combine(@"C:\out", "2026-10-14_15-30-05_3.png"), FileNamer.NextPath(@"C:\out", Now, ".png", taken.Contains));
    }
}
