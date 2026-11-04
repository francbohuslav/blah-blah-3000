using System.IO;
using NAudio.Wave;

namespace BlahBlah3000.Audio;

/// <summary>
/// Debug output: the whole recording in debug\recording.wav and the transcripts before and after replacements
/// in debug\transcripts.txt. Both files are overwritten on every start.
/// </summary>
internal sealed class DebugDump : IDisposable
{
    private readonly WaveFileWriter? _file;
    private readonly string? _transcriptPath;
    private int _count;

    /// <param name="enabled">false = nothing is saved, only the loudness of sentences is logged</param>
    public DebugDump(bool enabled)
    {
        if (!enabled) return;
        string dir;
        try
        {
            dir = Path.Combine(AppPaths.Data, "debug");
            Directory.CreateDirectory(dir);
        }
        catch (Exception ex)
        {
            Log.Write($"Cannot create the debug folder: {ex.Message}");
            return;
        }
        try
        {
            _file = new WaveFileWriter(Path.Combine(dir, "recording.wav"), new WaveFormat(16_000, 16, 1));
            Log.Write($"Debug audio: {dir}\recording.wav");
        }
        catch (Exception ex)
        {
            Log.Write($"Cannot create recording.wav: {ex.Message}");
        }
        try
        {
            _transcriptPath = Path.Combine(dir, "transcripts.txt");
            File.WriteAllText(_transcriptPath, "");
            Log.Write($"Debug transcripts: {_transcriptPath}");
        }
        catch (Exception ex)
        {
            _transcriptPath = null;
            Log.Write($"Cannot create transcripts.txt: {ex.Message}");
        }
    }

    /// <summary>Writes a sentence as Whisper returned it and after the replacements, so the rules can be debugged.</summary>
    public void LogTranscript(string original, string replaced)
    {
        if (_transcriptPath == null) return;
        var after = original == replaced ? "(unchanged)" : replaced;
        try
        {
            File.AppendAllText(_transcriptPath,
                $"[{DateTime.Now:HH:mm:ss}]{Environment.NewLine}  before: {original}{Environment.NewLine}  after:  {after}{Environment.NewLine}{Environment.NewLine}");
        }
        catch (IOException ex)
        {
            Log.Write($"Cannot write transcripts.txt: {ex.Message}");
        }
    }

    public void AddChunk(byte[] chunk)
    {
        try { _file?.Write(chunk, 0, chunk.Length); } catch (IOException) { }
    }

    /// <summary>Logs the length and loudness of a sentence that is being sent to Whisper.</summary>
    public void LogSentence(byte[] pcm)
    {
        double sum = 0;
        var peak = 0;
        var samples = pcm.Length / 2;
        for (var i = 0; i < samples; i++)
        {
            var s = BitConverter.ToInt16(pcm, i * 2);
            sum += (double)s * s;
            peak = Math.Max(peak, Math.Abs((int)s));
        }
        Log.Write($"Sentence {++_count}: {pcm.Length / 32} ms, peak {peak}, rms {(samples > 0 ? Math.Sqrt(sum / samples) : 0):F0}");
    }

    public void Dispose() => _file?.Dispose();
}
