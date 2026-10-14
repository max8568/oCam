using System.Media;
using System.Text;

namespace FrameCam.CaptureTests;

/// <summary>Plays a looping sine tone through the default playback device until disposed.</summary>
internal sealed class Tone : IDisposable
{
    private readonly MemoryStream _wav;
    private readonly SoundPlayer _player;

    private Tone(MemoryStream wav)
    {
        _wav = wav;
        _player = new SoundPlayer(wav);
        _player.PlayLooping();
    }

    public static Tone Play(int frequency = 440) => new(CreateWav(frequency));

    public void Dispose()
    {
        _player.Stop();
        _player.Dispose();
        _wav.Dispose();
    }

    /// <summary>One second of 16-bit mono PCM; a whole number of cycles, so it loops without a click.</summary>
    private static MemoryStream CreateWav(int frequency)
    {
        const int sampleRate = 44100;
        const int samples = sampleRate;
        var stream = new MemoryStream();
        using (var w = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true))
        {
            w.Write("RIFF"u8);
            w.Write(36 + samples * 2);
            w.Write("WAVE"u8);
            w.Write("fmt "u8);
            w.Write(16);
            w.Write((short)1);              // PCM
            w.Write((short)1);              // mono
            w.Write(sampleRate);
            w.Write(sampleRate * 2);        // bytes per second
            w.Write((short)2);              // block align
            w.Write((short)16);             // bits per sample
            w.Write("data"u8);
            w.Write(samples * 2);
            for (int i = 0; i < samples; i++)
                w.Write((short)(Math.Sin(2 * Math.PI * frequency * i / sampleRate) * short.MaxValue * 0.5));
        }
        stream.Position = 0;
        return stream;
    }
}
