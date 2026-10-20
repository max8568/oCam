using FrameCam.Native;

namespace FrameCam.CaptureTests;

/// <summary>
/// Windows' own do not disturb (spec 8.1). Turns it on for a moment, then back to how it was. Windows applies a
/// change a moment after it is made, while reading it back gives the new value at once, so each check waits.
/// </summary>
public class DoNotDisturbTests
{
    private const int Applied = 3000;

    [Fact]
    public void Do_not_disturb_turns_on_and_off() => Sta.Run(async () =>
    {
        var dnd = new WindowsDoNotDisturb();
        bool before = dnd.IsOn() ?? throw new InvalidOperationException("this Windows does not let FrameCam read do not disturb");
        try
        {
            Assert.True(dnd.TrySet(true));
            await Task.Delay(Applied);
            Assert.True(dnd.IsOn());

            Assert.True(dnd.TrySet(false));
            await Task.Delay(Applied);
            Assert.False(dnd.IsOn());
        }
        finally
        {
            dnd.TrySet(before);
        }
    });

    [Fact]
    public void Turning_it_off_right_after_turning_it_on_leaves_it_off() => Sta.Run(async () =>
    {
        var dnd = new WindowsDoNotDisturb();
        bool before = dnd.IsOn() ?? throw new InvalidOperationException("this Windows does not let FrameCam read do not disturb");
        try
        {
            Assert.True(dnd.TrySet(true));
            Assert.True(dnd.TrySet(false)); // a recording that fails as soon as it starts

            await Task.Delay(Applied);
            Assert.False(dnd.IsOn());
        }
        finally
        {
            dnd.TrySet(before);
        }
    });
}
