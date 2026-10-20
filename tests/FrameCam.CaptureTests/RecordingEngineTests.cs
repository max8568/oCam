using System.Diagnostics;
using FrameCam.Core;
using FrameCam.Recording;

namespace FrameCam.CaptureTests;

/// <summary>
/// Checks ScreenRecorderLib and the screenshot code against a known on-screen pattern (spec section 10).
/// These tests take over the screen for a few seconds: do not touch the mouse or keyboard while they run.
/// </summary>
public class RecordingEngineTests
{
    private static readonly PixelSize RegionSize = new(640, 360);

    [CaptureFact]
    public void Region_on_primary_monitor_is_recorded_pixel_exact() =>
        CheckRegion(primary: true, m => new PixelRect(m.Left + 101, m.Top + 77, RegionSize.Width, RegionSize.Height));

    [CaptureFact(NeedsSecondMonitor = true)]
    public void Region_on_secondary_monitor_is_recorded_pixel_exact() =>
        CheckRegion(primary: false, m => new PixelRect(m.Left + 101, m.Top + 77, RegionSize.Width, RegionSize.Height));

    [CaptureFact]
    public void Region_touching_monitor_bottom_right_is_recorded_pixel_exact() =>
        CheckRegion(primary: true, m => new PixelRect(m.Right - RegionSize.Width, m.Bottom - RegionSize.Height, RegionSize.Width, RegionSize.Height));

    /// <summary>
    /// H.264 stores only even sizes, so a 641×361 region is recorded as 640×360 without its last column and
    /// row (spec 7.6). The screenshot has the whole region.
    /// </summary>
    [CaptureFact]
    public void Odd_size_region_leaves_the_last_column_and_row_out_of_the_video()
    {
        string folder = Directory.CreateTempSubdirectory("framecam 測試 odd ").FullName;
        string video = Path.Combine(folder, "odd.mp4");
        string png = Path.Combine(folder, "odd.png");
        Sta.Run(async () =>
        {
            var screen = Screen.PrimaryScreen!;
            var monitor = screen.Bounds.ToPixelRect();
            var region = new PixelRect(monitor.Left + 101, monitor.Top + 77, 641, 361);
            Cursor.Position = new Point(monitor.Left + 2, monitor.Top + 2);
            using var pattern = new TestPatternForm(region);
            pattern.Show();
            await Task.Delay(500);
            ScreenshotTaker.Save(region, png);
            await Record(screen.DeviceName, monitor, region, video, seconds: 2);
        });

        AssertScreenshot(png, 641, 361);
        var (w, h, frames) = Ffmpeg.DecodeLuma(video);
        Assert.Equal((640, 360), (w, h));
        Assert.NotEmpty(frames);
        foreach (byte[] luma in frames)
        {
            Assert.True(luma[180 * w] >= 200 && luma[320] >= 200, "the region's left column or top row is missing");
            Assert.InRange(luma[180 * w + w - 1], (byte)100, (byte)160); // gray: the column before the white right edge
            Assert.InRange(luma[(h - 1) * w + 320], (byte)100, (byte)160);
        }
    }

    /// <summary>While the screen changes on every refresh, 1080p at 60 fps keeps up and stays in sync with the audio.</summary>
    [CaptureFact]
    public void Full_hd_at_60_fps_with_motion_keeps_up()
    {
        string video = Path.Combine(Directory.CreateTempSubdirectory("framecam 測試 motion ").FullName, "motion.mp4");
        var run = Sta.Run(async () =>
        {
            var screen = Screen.PrimaryScreen!;
            var monitor = screen.Bounds.ToPixelRect();
            var region = FullHdRegion(monitor);
            using var animation = new AnimatedForm(region.ToRectangle());
            animation.Show();
            await Task.Delay(500);
            var recording = await Record(screen.DeviceName, monitor, region, video, seconds: 5);
            return (Region: region, Recording: recording);
        });

        Assert.Equal((run.Region.Width, run.Region.Height), Ffmpeg.VideoSize(video));
        double expected = 60 * run.Recording.Seconds;
        Assert.InRange((double)Ffmpeg.CountFrames(video), expected * 0.95, expected * 1.05);
        AssertInSync(video, run.Recording.Seconds, MovingScreenTail);
    }

    /// <summary>
    /// When nothing on screen changes, the video must still last as long as the recording and the audio;
    /// otherwise sound and picture drift apart (spec section 3).
    /// </summary>
    [CaptureFact]
    public void Static_screen_keeps_video_and_audio_the_same_length()
    {
        string video = Path.Combine(Directory.CreateTempSubdirectory("framecam 測試 static ").FullName, "static.mp4");
        var run = Sta.Run(async () =>
        {
            var screen = Screen.PrimaryScreen!;
            var monitor = screen.Bounds.ToPixelRect();
            var region = FullHdRegion(monitor);
            using var pattern = new TestPatternForm(region);
            pattern.Show();
            await Task.Delay(500);
            return await Record(screen.DeviceName, monitor, region, video, seconds: 5);
        });

        AssertInSync(video, run.Seconds, StillScreenTail);
    }

    /// <summary>
    /// The cursor is drawn where it is on screen relative to the region, not relative to the monitor, and a
    /// cursor outside the region does not show up in the video.
    /// </summary>
    [CaptureFact]
    public void Cursor_is_drawn_where_it_is_on_screen()
    {
        string folder = Directory.CreateTempSubdirectory("framecam 測試 cursor ").FullName;
        string inside = Path.Combine(folder, "inside.mp4");
        string outside = Path.Combine(folder, "outside.mp4");
        Sta.Run(async () =>
        {
            var screen = Screen.PrimaryScreen!;
            var monitor = screen.Bounds.ToPixelRect();
            var region = new PixelRect(monitor.Left + 101, monitor.Top + 77, RegionSize.Width, RegionSize.Height);
            using var pattern = new TestPatternForm(region);
            pattern.Show();
            Cursor.Position = new Point(monitor.Left + 2, monitor.Top + 2);
            await Task.Delay(500);
            await Record(screen.DeviceName, monitor, region, outside, seconds: 1);
            Cursor.Position = new Point(region.Left + 320, region.Top + 180);
            await Task.Delay(500);
            await Record(screen.DeviceName, monitor, region, inside, seconds: 2);
        });

        var (w, _, insideFrames) = Ffmpeg.DecodeLuma(inside);
        byte[] last = insideFrames[^1];
        Assert.True(CursorPixels(last, w, 320, 180) > 20, "the cursor is not drawn at its position inside the region");
        // Where it would land if the region's offset from the monitor's corner (101, 77) were not subtracted.
        Assert.Equal(0, CursorPixels(last, w, 101 + 320, 77 + 180));

        // The cursor sits at the monitor's (2, 2), outside the region; without the offset it would land at the region's (2, 2).
        // ScreenRecorderLib 7.0.1 draws the very first frame (about 1 ms of video) without the offset, so it is skipped.
        Assert.All(Ffmpeg.DecodeLuma(outside).Frames.Skip(1), frame => Assert.Equal(0, CursorPixels(frame, w, 2, 2)));
    }

    /// <summary>With the cursor hidden, a cursor inside the region does not show up in the video.</summary>
    [CaptureFact]
    public void Hidden_cursor_is_not_drawn()
    {
        string video = Path.Combine(Directory.CreateTempSubdirectory("framecam 測試 hidden cursor ").FullName, "hidden.mp4");
        Sta.Run(async () =>
        {
            var screen = Screen.PrimaryScreen!;
            var monitor = screen.Bounds.ToPixelRect();
            var region = new PixelRect(monitor.Left + 101, monitor.Top + 77, RegionSize.Width, RegionSize.Height);
            using var pattern = new TestPatternForm(region);
            pattern.Show();
            Cursor.Position = new Point(region.Left + 320, region.Top + 180);
            await Task.Delay(500);
            await Record(screen.DeviceName, monitor, region, video, seconds: 2, hideCursor: true);
        });

        var (w, _, frames) = Ffmpeg.DecodeLuma(video);
        Assert.All(frames, frame => Assert.Equal(0, CursorPixels(frame, w, 320, 180)));
    }

    /// <summary>
    /// What is on screen while paused stays out of the video, the video lasts only the time actually recorded,
    /// and sound and picture are still together after resuming. The region is dark before the pause, white
    /// during it and gray after it.
    /// </summary>
    [CaptureFact]
    public void Paused_time_is_left_out_of_the_video()
    {
        string video = Path.Combine(Directory.CreateTempSubdirectory("framecam 測試 pause ").FullName, "pause.mp4");
        double recorded = Sta.Run(async () =>
        {
            var screen = Screen.PrimaryScreen!;
            var monitor = screen.Bounds.ToPixelRect();
            var region = new PixelRect(monitor.Left + 101, monitor.Top + 77, RegionSize.Width, RegionSize.Height);
            Cursor.Position = new Point(monitor.Left + 2, monitor.Top + 2);
            using var panel = new PhaseColorForm(region.ToRectangle(), Color.FromArgb(40, 40, 40));
            panel.Show();
            await Task.Delay(500);

            using var recorder = new ScreenRecorderLibRecorder();
            var started = new TaskCompletionSource();
            var finished = new TaskCompletionSource();
            recorder.Started += (_, _) => started.TrySetResult();
            recorder.Completed += (_, _) => finished.TrySetResult();
            recorder.Failed += (_, error) => finished.TrySetException(new InvalidOperationException(error));
            using var tone = Tone.Play();
            recorder.Start(new RecordingRequest(screen.DeviceName, monitor, region, 60, video));
            await started.Task.WaitAsync(TimeSpan.FromSeconds(10));
            var clock = Stopwatch.StartNew();
            await Task.Delay(2000);
            recorder.Pause();
            double before = clock.Elapsed.TotalSeconds;
            // White only from a moment after pausing to a moment before resuming, so a pause or resume that
            // takes ScreenRecorderLib a little while does not count as a failure.
            await Task.Delay(300);
            panel.Phase = Color.White;
            await Task.Delay(1500);
            panel.Phase = TestPatternForm.Inside;
            await Task.Delay(300);
            recorder.Resume();
            clock.Restart();
            await Task.Delay(2000);
            recorder.Stop();
            double after = clock.Elapsed.TotalSeconds;
            await finished.Task.WaitAsync(TimeSpan.FromSeconds(30));
            return before + after;
        });

        var (w, _, frames) = Ffmpeg.DecodeLuma(video);
        var centers = frames.Select(frame => frame[180 * w + 320]).ToList();
        Assert.DoesNotContain(centers, v => v > 200); // white: recorded while paused
        Assert.Contains(centers, v => v < 80);        // dark: before the pause
        Assert.Contains(centers, v => v is > 100 and < 160); // gray: after resuming
        AssertInSync(video, recorded, MovingScreenTail);
    }

    /// <summary>Resuming is not a new start; the control bar would set its recording time back to zero.</summary>
    [CaptureFact]
    public void Resuming_does_not_raise_Started_again()
    {
        string video = Path.Combine(Directory.CreateTempSubdirectory("framecam 測試 resume ").FullName, "resume.mp4");
        int starts = Sta.Run(async () =>
        {
            var screen = Screen.PrimaryScreen!;
            var monitor = screen.Bounds.ToPixelRect();
            using var recorder = new ScreenRecorderLibRecorder();
            int count = 0;
            var finished = new TaskCompletionSource();
            recorder.Started += (_, _) => count++;
            recorder.Completed += (_, _) => finished.TrySetResult();
            recorder.Failed += (_, error) => finished.TrySetException(new InvalidOperationException(error));
            recorder.Start(new RecordingRequest(screen.DeviceName, monitor, new PixelRect(monitor.Left, monitor.Top, 320, 240), 30, video));
            for (int i = 0; i < 100 && count == 0; i++)
                await Task.Delay(100);
            recorder.Pause();
            await Task.Delay(500);
            recorder.Resume();
            await Task.Delay(1000); // a second Started would arrive here
            recorder.Stop();
            await finished.Task.WaitAsync(TimeSpan.FromSeconds(30));
            return count;
        });

        Assert.Equal(1, starts);
    }

    [CaptureFact]
    public void Stopping_while_paused_saves_the_file()
    {
        string video = Path.Combine(Directory.CreateTempSubdirectory("framecam 測試 stop paused ").FullName, "paused.mp4");
        Sta.Run(async () =>
        {
            var screen = Screen.PrimaryScreen!;
            var monitor = screen.Bounds.ToPixelRect();
            using var recorder = new ScreenRecorderLibRecorder();
            var started = new TaskCompletionSource();
            var finished = new TaskCompletionSource();
            recorder.Started += (_, _) => started.TrySetResult();
            recorder.Completed += (_, _) => finished.TrySetResult();
            recorder.Failed += (_, error) => finished.TrySetException(new InvalidOperationException(error));
            recorder.Start(new RecordingRequest(screen.DeviceName, monitor, new PixelRect(monitor.Left, monitor.Top, 320, 240), 30, video));
            await started.Task.WaitAsync(TimeSpan.FromSeconds(10));
            await Task.Delay(1000);
            recorder.Pause();
            await Task.Delay(500);
            recorder.Stop();
            await finished.Task.WaitAsync(TimeSpan.FromSeconds(30));
        });

        Assert.True(Ffmpeg.CountFrames(video) > 0, "the saved video has no frames");
    }

    [CaptureFact]
    public void Stopping_twice_finishes_once()
    {
        string video = Path.Combine(Directory.CreateTempSubdirectory("framecam 測試 stop ").FullName, "stop.mp4");
        int endings = Sta.Run(async () =>
        {
            var screen = Screen.PrimaryScreen!;
            var monitor = screen.Bounds.ToPixelRect();
            using var recorder = new ScreenRecorderLibRecorder();
            int count = 0;
            var started = new TaskCompletionSource();
            recorder.Started += (_, _) => started.TrySetResult();
            recorder.Completed += (_, _) => count++;
            recorder.Failed += (_, _) => count++;
            recorder.Start(new RecordingRequest(screen.DeviceName, monitor, new PixelRect(monitor.Left, monitor.Top, 320, 240), 30, video));
            await started.Task.WaitAsync(TimeSpan.FromSeconds(10));
            await Task.Delay(1000);
            recorder.Stop();
            recorder.Stop();
            for (int i = 0; i < 100 && recorder.IsRecording; i++)
                await Task.Delay(100);
            await Task.Delay(1000); // a second ending event would arrive here
            return count;
        });

        Assert.Equal(1, endings);
        Assert.True(File.Exists(video));
    }

    [CaptureFact]
    public void Stopping_before_the_first_frame_still_finishes_once()
    {
        // F2 pressed twice quickly: Stop arrives before Started. The control bar waits for the ending event,
        // so it must come, or the bar could neither record nor close again.
        string video = Path.Combine(Directory.CreateTempSubdirectory("framecam 測試 early stop ").FullName, "early.mp4");
        var (endings, stillRecording) = Sta.Run(async () =>
        {
            var screen = Screen.PrimaryScreen!;
            var monitor = screen.Bounds.ToPixelRect();
            using var recorder = new ScreenRecorderLibRecorder();
            int count = 0;
            recorder.Completed += (_, _) => count++;
            recorder.Failed += (_, _) => count++;
            recorder.Start(new RecordingRequest(screen.DeviceName, monitor, new PixelRect(monitor.Left, monitor.Top, 320, 240), 30, video));
            recorder.Stop();
            for (int i = 0; i < 100 && recorder.IsRecording; i++)
                await Task.Delay(100);
            await Task.Delay(1000); // a second ending event would arrive here
            return (count, recorder.IsRecording);
        });

        Assert.False(stillRecording);
        Assert.Equal(1, endings);
    }

    private static void CheckRegion(bool primary, Func<PixelRect, PixelRect> placeRegion)
    {
        // A folder name with a space and CJK characters, like one a user might pick.
        string folder = Directory.CreateTempSubdirectory("framecam 測試 ").FullName;
        string video = Path.Combine(folder, "region.mp4");
        string png = Path.Combine(folder, "region.png");

        var run = Sta.Run(async () =>
        {
            var screen = primary ? Screen.PrimaryScreen! : Screen.AllScreens.First(s => !s.Primary);
            var monitor = screen.Bounds.ToPixelRect();
            var region = placeRegion(monitor);
            Cursor.Position = new Point(monitor.Left + 2, monitor.Top + 2); // keep the cursor out of the region

            using var pattern = new TestPatternForm(region);
            pattern.Show();
            using var overlay = new ExcludedOverlayForm(new Rectangle(region.Left + 200, region.Top + 100, 100, 100));
            overlay.Show();
            await Task.Delay(500); // let the desktop compositor show both windows

            ScreenshotTaker.Save(region, png);
            var recording = await Record(screen.DeviceName, monitor, region, video, seconds: 3);
            return (Region: region, Recording: recording, PatternBounds: pattern.Bounds.ToPixelRect());
        });

        Assert.Equal(TestPatternForm.BoundsFor(run.Region), run.PatternBounds);
        AssertScreenshot(png, run.Region.Width, run.Region.Height);
        AssertVideo(video, run.Region.Width, run.Region.Height);
        AssertInSync(video, run.Recording.Seconds, StillScreenTail);
        Assert.True(run.Recording.HasAudio, "no default playback device was found");
        Assert.True(Ffmpeg.HasAudioStream(video), "the video has no audio track");
        double volume = Ffmpeg.MeanVolumeDb(video);
        Assert.True(volume > -50, $"system audio is nearly silent ({volume} dB); is the playback device muted?");
    }

    private sealed record RecordingResult(bool HasAudio, double Seconds);

    /// <summary>Records while a tone plays. Must run on an STA thread (inside Sta.Run).</summary>
    private static async Task<RecordingResult> Record(string device, PixelRect monitor, PixelRect region, string path, int seconds, bool hideCursor = false)
    {
        using var recorder = new ScreenRecorderLibRecorder();
        var started = new TaskCompletionSource();
        var finished = new TaskCompletionSource();
        recorder.Started += (_, _) => started.TrySetResult();
        recorder.Completed += (_, _) => finished.TrySetResult();
        recorder.Failed += (_, error) => finished.TrySetException(new InvalidOperationException(error));

        using var tone = Tone.Play();
        bool hasAudio = recorder.Start(new RecordingRequest(device, monitor, region, 60, path, hideCursor));
        await started.Task.WaitAsync(TimeSpan.FromSeconds(10));
        var clock = Stopwatch.StartNew();
        await Task.Delay(TimeSpan.FromSeconds(seconds));
        recorder.Stop();
        double elapsed = clock.Elapsed.TotalSeconds;
        await finished.Task.WaitAsync(TimeSpan.FromSeconds(30));
        Assert.True(File.Exists(path), $"{path} was not written");
        return new RecordingResult(hasAudio, elapsed);
    }

    private static void AssertScreenshot(string png, int width, int height)
    {
        using var image = new Bitmap(png);
        Assert.Equal(new Size(width, height), image.Size);
        foreach (var (x, y) in OuterRing(width, height))
        {
            var pixel = image.GetPixel(x, y);
            Assert.True(pixel.ToArgb() == Color.White.ToArgb(), $"screenshot pixel ({x},{y}) is {pixel}, expected white");
        }
        Assert.Equal(TestPatternForm.Inside.ToArgb(), image.GetPixel(1, 1).ToArgb());
        Assert.Equal(TestPatternForm.Inside.ToArgb(), image.GetPixel(250, 150).ToArgb()); // under the excluded red window
    }

    /// <summary>Every frame shows exactly the region. Frame count is not checked here: a still screen gives few frames.</summary>
    private static void AssertVideo(string video, int width, int height)
    {
        var (w, h, frames) = Ffmpeg.DecodeLuma(video);
        Assert.Equal((width, height), (w, h));
        Assert.NotEmpty(frames);
        for (int i = 0; i < frames.Count; i++)
        {
            byte[] luma = frames[i];
            var ring = OuterRing(w, h).Select(p => luma[p.Y * w + p.X]).ToList();
            // H.264 is lossy, so compare brightness with margins: white is about 235-255, gray about 126-128,
            // black about 0-16. A one-pixel shift puts gray or black into the outer ring.
            Assert.True(ring.Min() >= 150 && ring.Average(b => b) >= 200,
                $"frame {i}: outer ring min {ring.Min()}, mean {ring.Average(b => b):F0}; the recorded area is shifted or scaled");
            Assert.InRange(luma[150 * w + 250], (byte)100, (byte)160); // gray, not the excluded red window
        }
    }

    /// <summary>
    /// On a still screen ScreenRecorderLib writes about one frame a second and, when stopped, drops the time since
    /// the last frame (video and audio together), so up to about a second can be missing at the end.
    /// </summary>
    private const double StillScreenTail = 1.1;
    private const double MovingScreenTail = 0.2;

    /// <summary>
    /// Video and audio have the same length, so sound and picture stay together, and the video covers the
    /// recording except at most missingAtEnd seconds at the end.
    /// </summary>
    private static void AssertInSync(string video, double seconds, double missingAtEnd)
    {
        var (videoLength, audioLength) = Ffmpeg.StreamDurations(video);
        Assert.True(Math.Abs(videoLength - audioLength) <= 0.1, $"video lasts {videoLength:F3} s but audio {audioLength:F3} s");
        Assert.True(videoLength >= seconds - missingAtEnd && videoLength <= seconds + 0.2,
            $"video lasts {videoLength:F3} s for a {seconds:F3} s recording");
    }

    /// <summary>Pixels in the 24×24 box right of and below (x, y) that are far from the pattern's gray: the cursor's black and white.</summary>
    private static int CursorPixels(byte[] luma, int width, int x, int y) =>
        Enumerable.Range(y, 24).SelectMany(row => Enumerable.Range(x, 24).Select(col => luma[row * width + col]))
            .Count(v => Math.Abs(v - 128) > 60);

    private static PixelRect FullHdRegion(PixelRect monitor) =>
        new(monitor.Left, monitor.Top, Math.Min(1920, monitor.Width) & ~1, Math.Min(1080, monitor.Height) & ~1);

    private static IEnumerable<(int X, int Y)> OuterRing(int width, int height)
    {
        for (int x = 0; x < width; x++)
        {
            yield return (x, 0);
            yield return (x, height - 1);
        }
        for (int y = 1; y < height - 1; y++)
        {
            yield return (0, y);
            yield return (width - 1, y);
        }
    }
}
