using FrameCam.Core;

namespace FrameCam.Recording;

/// <summary>
/// What to record. MonitorBounds and Region are physical-pixel desktop coordinates. HideCursor leaves the mouse
/// cursor out of the video.
/// </summary>
public sealed record RecordingRequest(
    string MonitorDeviceName,
    PixelRect MonitorBounds,
    PixelRect Region,
    int FramesPerSecond,
    string OutputPath,
    bool HideCursor = false);

/// <summary>
/// Records a screen region to an MP4 file. Events are raised on the thread that called Start,
/// and exactly one of Completed or Failed is raised per recording.
/// </summary>
public interface IScreenRecorder : IDisposable
{
    /// <summary>Frames have started being written.</summary>
    event EventHandler? Started;

    /// <summary>The file is finished. Argument: the file path.</summary>
    event EventHandler<string>? Completed;

    /// <summary>Recording failed; whatever was written so far stays on disk. Argument: the error message.</summary>
    event EventHandler<string>? Failed;

    /// <summary>True from Start until Completed or Failed.</summary>
    bool IsRecording { get; }

    /// <summary>Starts recording. Returns false when there is no playback device, so only video is recorded.</summary>
    bool Start(RecordingRequest request);

    void Stop();

    /// <summary>Stops writing frames and sound until Resume; the time in between is left out of the file.</summary>
    void Pause();

    void Resume();
}
