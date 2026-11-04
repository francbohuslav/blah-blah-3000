namespace BlahBlah3000.Audio;

/// <summary>Splits a PCM s16le 16 kHz mono stream into sentences at pauses (simple RMS-based silence detection).</summary>
internal sealed class SentenceSegmenter
{
    private const int BytesPerMs = 32;
    private const int MinSpeechMs = 250;
    private const int PreRollMs = 300;
    private const int MaxSentenceMs = 30_000;

    /// <summary>RMS that corresponds to level 1 in the <see cref="Level"/> event.</summary>
    public const double LevelScale = 6_000;

    /// <summary>Blocks with an RMS below the threshold count as silence.</summary>
    public double RmsThreshold { get; set; } = 400;

    /// <summary>How long a silence (ms) ends a sentence.</summary>
    public int PauseMs { get; set; } = 700;

    private readonly List<byte> _buffer = [];
    private bool _speech;
    private int _speechMs;
    private int _silenceMs;

    /// <summary>A finished sentence (PCM).</summary>
    public event Action<byte[]>? Sentence;

    /// <summary>Level of the last block in the range 0 to 1.</summary>
    public event Action<double>? Level;

    public void Feed(byte[] chunk)
    {
        var ms = chunk.Length / BytesPerMs;
        var rms = Rms(chunk);
        Level?.Invoke(Math.Min(1, rms / LevelScale));

        _buffer.AddRange(chunk);
        if (rms >= RmsThreshold)
        {
            _speech = true;
            _speechMs += ms;
            _silenceMs = 0;
        }
        else
        {
            _silenceMs += ms;
        }

        if (!_speech)
        {
            // Before speech starts, only a short pre-roll is kept.
            var excess = _buffer.Count - PreRollMs * BytesPerMs;
            if (excess > 0) _buffer.RemoveRange(0, excess);
            return;
        }
        if (_silenceMs >= PauseMs || _buffer.Count / BytesPerMs >= MaxSentenceMs) Emit();
    }

    /// <summary>Finishes the sentence in progress (e.g. after Enter).</summary>
    public void Flush() => Emit();

    private void Emit()
    {
        if (_speech && _speechMs >= MinSpeechMs) Sentence?.Invoke(_buffer.ToArray());
        _buffer.Clear();
        _speech = false;
        _speechMs = 0;
        _silenceMs = 0;
    }

    private static double Rms(byte[] pcm)
    {
        var count = pcm.Length / 2;
        if (count == 0) return 0;
        double sum = 0;
        for (var i = 0; i < count; i++)
        {
            double sample = BitConverter.ToInt16(pcm, i * 2);
            sum += sample * sample;
        }
        return Math.Sqrt(sum / count);
    }
}
