using System.IO;
using System.Media;

namespace BlahBlah3000.Audio;

/// <summary>
/// Two short, quiet tones that tell the user push to talk started (higher) and ended (lower). They are generated in memory, so they
/// do not depend on the Windows sound scheme (which may be silent or loud) and need no files.
/// </summary>
internal static class PushToTalkSounds
{
    private const int SampleRate = 44_100;

    private static readonly Lazy<SoundPlayer?> StartPlayer = new(() => Create(880, 80, 0.12));
    private static readonly Lazy<SoundPlayer?> StopPlayer = new(() => Create(587, 80, 0.12));

    public static void PlayStart() => Play(StartPlayer.Value);

    public static void PlayStop() => Play(StopPlayer.Value);

    private static void Play(SoundPlayer? player)
    {
        try
        {
            // Asynchronous: plays on its own thread, a tone still playing is replaced.
            player?.Play();
        }
        catch (Exception ex)
        {
            Log.Write($"Cannot play the push-to-talk sound: {ex.Message}");
        }
    }

    /// <summary>A sine tone with a smooth fade in and out (no click), as a WAV in memory.</summary>
    private static SoundPlayer? Create(double frequency, int milliseconds, double volume)
    {
        try
        {
            var samples = SampleRate * milliseconds / 1000;
            var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, System.Text.Encoding.ASCII, leaveOpen: true))
            {
                writer.Write("RIFF"u8);
                writer.Write(36 + samples * 2);
                writer.Write("WAVEfmt "u8);
                writer.Write(16);
                writer.Write((short)1);
                writer.Write((short)1);
                writer.Write(SampleRate);
                writer.Write(SampleRate * 2);
                writer.Write((short)2);
                writer.Write((short)16);
                writer.Write("data"u8);
                writer.Write(samples * 2);
                for (var i = 0; i < samples; i++)
                {
                    var envelope = Math.Pow(Math.Sin(Math.PI * i / samples), 2);
                    var value = Math.Sin(2 * Math.PI * frequency * i / SampleRate) * envelope * volume;
                    writer.Write((short)(value * short.MaxValue));
                }
            }
            stream.Position = 0;
            var player = new SoundPlayer(stream);
            player.Load();
            return player;
        }
        catch (Exception ex)
        {
            Log.Write($"Cannot create the push-to-talk sound: {ex.Message}");
            return null;
        }
    }
}
