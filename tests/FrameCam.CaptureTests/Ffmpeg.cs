using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace FrameCam.CaptureTests;

/// <summary>Runs ffmpeg/ffprobe from PATH to inspect recorded files. Test-only; never shipped.</summary>
internal static class Ffmpeg
{
    public static bool IsAvailable => Find("ffmpeg") is not null && Find("ffprobe") is not null;

    public static (int Width, int Height) VideoSize(string path)
    {
        string text = RunText("ffprobe", $"-v error -select_streams v:0 -show_entries stream=width,height -of csv=p=0:s=x \"{path}\"").Trim();
        var parts = text.Split('x');
        return (int.Parse(parts[0], CultureInfo.InvariantCulture), int.Parse(parts[1], CultureInfo.InvariantCulture));
    }

    /// <summary>Durations in seconds of the first video and first audio streams.</summary>
    public static (double Video, double Audio) StreamDurations(string path)
    {
        double Duration(string stream) => double.Parse(
            RunText("ffprobe", $"-v error -select_streams {stream}:0 -show_entries stream=duration -of csv=p=0 \"{path}\"").Trim(),
            CultureInfo.InvariantCulture);
        return (Duration("v"), Duration("a"));
    }

    public static int CountFrames(string path) =>
        int.Parse(RunText("ffprobe", $"-v error -count_frames -select_streams v:0 -show_entries stream=nb_read_frames -of csv=p=0 \"{path}\"").Trim(),
            CultureInfo.InvariantCulture);

    /// <summary>Every video frame as 8-bit luma, row by row.</summary>
    public static (int Width, int Height, List<byte[]> Frames) DecodeLuma(string path)
    {
        var (width, height) = VideoSize(path);
        byte[] bytes = RunBinary("ffmpeg", $"-v error -i \"{path}\" -map 0:v:0 -f rawvideo -pix_fmt gray -");
        int frameSize = width * height;
        var frames = new List<byte[]>();
        for (int offset = 0; offset + frameSize <= bytes.Length; offset += frameSize)
            frames.Add(bytes.AsSpan(offset, frameSize).ToArray());
        return (width, height, frames);
    }

    public static bool HasAudioStream(string path) =>
        RunText("ffprobe", $"-v error -select_streams a -show_entries stream=codec_type -of csv=p=0 \"{path}\"").Contains("audio");

    /// <summary>Mean volume of the first audio track in dB. Digital silence is about -91 dB.</summary>
    public static double MeanVolumeDb(string path)
    {
        string log = RunText("ffmpeg", $"-hide_banner -i \"{path}\" -map 0:a:0 -af volumedetect -f null -", returnStdErr: true);
        var match = Regex.Match(log, @"mean_volume:\s*(-?[\d.]+) dB");
        if (!match.Success)
            throw new InvalidOperationException("volumedetect output not found:\n" + log);
        return double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
    }

    private static string? Find(string exe) =>
        (Environment.GetEnvironmentVariable("PATH") ?? "")
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(dir => Path.Combine(dir.Trim(), exe + ".exe"))
            .FirstOrDefault(File.Exists);

    private static byte[] RunBinary(string exe, string args)
    {
        using var process = Start(exe, args);
        var stderr = process.StandardError.ReadToEndAsync();
        using var output = new MemoryStream();
        process.StandardOutput.BaseStream.CopyTo(output);
        process.WaitForExit();
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"{exe} {args} failed:\n{stderr.Result}");
        return output.ToArray();
    }

    private static string RunText(string exe, string args, bool returnStdErr = false)
    {
        using var process = Start(exe, args);
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"{exe} {args} failed:\n{stderr.Result}");
        return returnStdErr ? stderr.Result : stdout.Result;
    }

    private static Process Start(string exe, string args) =>
        Process.Start(new ProcessStartInfo(exe, args)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        }) ?? throw new InvalidOperationException($"could not start {exe}");
}
