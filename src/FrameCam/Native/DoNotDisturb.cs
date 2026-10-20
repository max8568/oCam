using System.Runtime.InteropServices;

namespace FrameCam.Native;

/// <summary>Windows' do not disturb, which keeps notifications from popping up (spec 8.1).</summary>
internal interface IDoNotDisturb
{
    /// <summary>Null when this Windows does not let a program read it.</summary>
    bool? IsOn();

    /// <summary>False when this Windows does not let a program change it.</summary>
    bool TrySet(bool on);
}

/// <summary>
/// Through the undocumented COM object behind the notification center's do not disturb button. The documented
/// FocusSessionManager needs a key from Microsoft, so this may stop working after a Windows update; then
/// recording goes ahead without it.
/// </summary>
internal sealed class WindowsDoNotDisturb : IDoNotDisturb
{
    private const string Off = "Microsoft.QuietHoursProfile.Unrestricted";
    private const string On = "Microsoft.QuietHoursProfile.PriorityOnly"; // what the button turns on

    /// <summary>
    /// Windows applies a change a moment after it is made. Turned off right after being turned on, do not
    /// disturb ended up on; 20 ms apart was enough on the dev machine.
    /// </summary>
    private const int MinGapMs = 500;

    private long _lastChange; // Environment.TickCount64

    public bool? IsOn()
    {
        try
        {
            if (Settings().get_UserSelectedProfile(out IntPtr profile) < 0)
                return null;
            try
            {
                return Marshal.PtrToStringUni(profile) != Off;
            }
            finally
            {
                Marshal.FreeCoTaskMem(profile);
            }
        }
        catch (Exception e) when (e is COMException or InvalidCastException)
        {
            return null;
        }
    }

    public bool TrySet(bool on)
    {
        long wait = _lastChange + MinGapMs - Environment.TickCount64;
        if (wait > 0)
            Thread.Sleep((int)wait); // only when a recording ends as soon as it starts
        try
        {
            bool changed = Settings().put_UserSelectedProfile(on ? On : Off) >= 0;
            _lastChange = Environment.TickCount64;
            return changed;
        }
        catch (Exception e) when (e is COMException or InvalidCastException)
        {
            return false;
        }
    }

    private static IQuietHoursSettings Settings() =>
        (IQuietHoursSettings)Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("f53321fa-34f8-4b7f-b9a3-361877cb94cf"))!)!;

    [ComImport, Guid("6bff4732-81ec-4ffb-ae67-b6c1bc29631f"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IQuietHoursSettings
    {
        [PreserveSig] int get_UserSelectedProfile(out IntPtr profileId);
        [PreserveSig] int put_UserSelectedProfile([MarshalAs(UnmanagedType.LPWStr)] string profileId);
    }
}
