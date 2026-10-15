using FrameCam.Core;
using ScreenRecorderLib;

namespace FrameCam.Recording;

/// <summary>IScreenRecorder backed by ScreenRecorderLib.</summary>
public sealed class ScreenRecorderLibRecorder : IScreenRecorder
{
    private Recorder? _recorder;
    private SynchronizationContext? _ui;

    public event EventHandler? Started;
    public event EventHandler<string>? Completed;
    public event EventHandler<string>? Failed;

    public bool IsRecording => _recorder is not null;

    public bool Start(RecordingRequest request)
    {
        if (_recorder is not null)
            throw new InvalidOperationException("Already recording.");
        _ui = SynchronizationContext.Current
            ?? throw new InvalidOperationException("Start must be called on a UI thread.");

        var loopback = Recorder.GetSystemAudioLoopbackDevices().FirstOrDefault(d => d.IsDefaultDevice);
        var recorder = Recorder.CreateRecorder(BuildOptions(request, loopback));
        bool started = false;
        recorder.OnStatusChanged += (_, e) =>
        {
            // Resuming after a pause also reports Recording; only the first one is the start.
            if (e.Status == RecorderStatus.Recording && !started)
            {
                started = true;
                Post(() => Started?.Invoke(this, EventArgs.Empty));
            }
        };
        recorder.OnRecordingComplete += (_, e) => Post(() => Finish(recorder, () => Completed?.Invoke(this, e.FilePath)));
        recorder.OnRecordingFailed += (_, e) => Post(() => Finish(recorder, () => Failed?.Invoke(this, e.Error)));

        _recorder = recorder;
        try
        {
            recorder.Record(request.OutputPath);
        }
        catch
        {
            _recorder = null;
            recorder.Dispose();
            throw;
        }
        return loopback is not null;
    }

    public void Stop() => _recorder?.Stop();

    public void Pause() => _recorder?.Pause();

    public void Resume() => _recorder?.Resume();

    public void Dispose()
    {
        _recorder?.Dispose();
        _recorder = null;
    }

    private void Post(Action action) => _ui!.Post(_ => action(), null);

    /// <summary>Raises the ending event once per recording, after releasing the native recorder.</summary>
    private void Finish(Recorder recorder, Action raise)
    {
        if (!ReferenceEquals(_recorder, recorder))
            return;
        _recorder = null;
        recorder.Dispose();
        raise();
    }

    internal static RecorderOptions BuildOptions(RecordingRequest request, AudioSourceBase? audioSource)
    {
        var r = request.Region;
        var m = request.MonitorBounds;
        var source = new DisplayRecordingSource(request.MonitorDeviceName)
        {
            // Relative to the monitor's top-left (checked on a non-primary monitor by RecordingEngineTests).
            SourceRect = new ScreenRect(r.Left - m.Left, r.Top - m.Top, r.Width, r.Height),
            Stretch = StretchMode.None,
            IsCursorCaptureEnabled = !request.HideCursor,
        };
        return new RecorderOptions
        {
            SourceOptions = new SourceOptions { RecordingSources = { source } },
            OutputOptions = new OutputOptions
            {
                RecorderMode = RecorderMode.Video,
                OutputFrameSize = new ScreenSize(r.Width, r.Height),
                Stretch = StretchMode.None,
            },
            VideoEncoderOptions = new VideoEncoderOptions
            {
                Framerate = request.FramesPerSecond,
                // With a fixed frame rate ScreenRecorderLib times frames by count, and on a still screen it
                // writes fewer than Framerate per second, so the video ends up shorter than the audio
                // (Static_screen_keeps_video_and_audio_the_same_length). Variable timing keeps them in sync.
                IsFixedFramerate = false,
                IsHardwareEncodingEnabled = true,
                IsFragmentedMp4Enabled = true,
                Encoder = new H264VideoEncoder
                {
                    BitrateMode = H264BitrateControlMode.Quality,
                    EncoderProfile = H264Profile.High,
                },
            },
            AudioOptions = new AudioOptions
            {
                IsAudioEnabled = audioSource is not null,
                Bitrate = AudioBitrate.bitrate_192kbps,
                Channels = AudioChannels.Stereo,
                AudioSources = audioSource is null ? [] : [audioSource],
            },
            MouseOptions = new MouseOptions { IsMousePointerEnabled = !request.HideCursor },
        };
    }
}
