namespace FrameCam.CaptureTests;

/// <summary>A [Fact] that is skipped when ffmpeg/ffprobe are not on PATH, or when it needs a second monitor that is missing.</summary>
public sealed class CaptureFactAttribute : FactAttribute
{
    private bool _needsSecondMonitor;

    public CaptureFactAttribute()
    {
        if (!Ffmpeg.IsAvailable)
            Skip = "ffmpeg/ffprobe not found on PATH";
    }

    public bool NeedsSecondMonitor
    {
        get => _needsSecondMonitor;
        set
        {
            _needsSecondMonitor = value;
            if (value && Skip is null && TestNative.MonitorCount() < 2)
                Skip = "needs a second monitor";
        }
    }
}
