using System.Threading.Channels;
using BlahBlah3000.Audio;
using BlahBlah3000.Native;
using BlahBlah3000.Rules;

namespace BlahBlah3000.Dictation;

/// <summary>
/// One push-to-talk dictation without any window: records the microphone, splits it into sentences at pauses and types each transcribed
/// sentence into the active window at its caret (so the text is appended after what is already there).
/// </summary>
internal sealed class PushToTalkSession : IDisposable
{
    private readonly Settings _settings;
    private readonly RuleEngine _rules;
    private readonly Action<string> _onError;
    private readonly MicRecorder _recorder = new();
    private readonly SentenceSegmenter _segmenter = new();
    // Separate files, so the debug output of the dictation window is not overwritten.
    private readonly DebugDump _debug;
    private readonly VolumeDucker _ducker = new();
    private readonly Channel<byte[]> _sentences = Channel.CreateUnbounded<byte[]>();
    private readonly CancellationTokenSource _cts = new();
    private readonly object _audioLock = new();

    /// <summary>
    /// Recording continues this long after the keys are released: the microphone delivers audio with a delay and the speaker usually
    /// lets go just as the last word ends, so stopping at once would cut it off.
    /// </summary>
    private const int ReleaseTailMs = 400;

    private Task _consumer = Task.CompletedTask;
    private bool _stopped;
    private bool _typedAny;
    private bool _disposed;
    private const int SpeechIgnoreMs = 250;
    private readonly long _startTick = Environment.TickCount64;
    private volatile bool _hasSpeech;
    private long _lastSpeechTick = Environment.TickCount64;

    /// <summary>Something has been said since the start of the session (the first moments, where the start tone could be heard, do not count).</summary>
    public bool HasSpeech => _hasSpeech;

    /// <summary><see cref="Environment.TickCount64"/> of the last audio block that was loud enough to count as speech (or of the start).</summary>
    public long LastSpeechTick => Interlocked.Read(ref _lastSpeechTick);

    public PushToTalkSession(Settings settings, Action<string> onError)
    {
        _settings = settings;
        _rules = new RuleEngine(settings);
        _onError = onError;
        _debug = new DebugDump(settings.DebugRecording, "push-to-talk-");
        _segmenter.RmsThreshold = settings.SilenceThreshold;
        _segmenter.PauseMs = settings.PauseMs;
        _segmenter.Level += level =>
        {
            if (level * SentenceSegmenter.LevelScale < _segmenter.RmsThreshold) return;
            var now = Environment.TickCount64;
            Interlocked.Exchange(ref _lastSpeechTick, now);
            // The first moments are ignored: the start tone may be picked up by the microphone.
            if (now - _startTick > SpeechIgnoreMs) _hasSpeech = true;
        };
        _segmenter.Sentence += pcm =>
        {
            _debug.LogSentence(pcm);
            _sentences.Writer.TryWrite(pcm);
        };
        // NAudio raises the event on its own thread; the segmenter is also flushed from another thread when the session ends.
        _recorder.Data += chunk =>
        {
            lock (_audioLock)
            {
                if (_stopped) return;
                _debug.AddChunk(chunk);
                _segmenter.Feed(chunk);
            }
        };
    }

    /// <summary>Opens the microphone and starts the transcription loop. Throws when the microphone cannot be opened.</summary>
    public void Start()
    {
        var option = MicRecorder.Resolve(_settings.Microphone);
        _recorder.Start(option.Number);
        _consumer = Task.Run(ConsumeAsync);
        Log.Write($"Push to talk started: {option.Label} (device {option.Number})");
        if (_settings.PushToTalkSounds) PushToTalkSounds.PlayStart();
        // A moment later, so the start tone is still heard at the normal volume.
        if (_settings.PushToTalkDuckPercent < 100) _ = _ducker.DuckAsync(TimeSpan.FromMilliseconds(150), _settings.PushToTalkDuckPercent);
    }

    /// <summary>Ends the recording, waits until the last sentence is transcribed and typed, and releases the microphone.</summary>
    public async Task StopAsync()
    {
        await Task.Delay(ReleaseTailMs);
        _recorder.Stop();
        _ducker.Restore();
        // Only now, so the tone is not picked up by the microphone.
        if (_settings.PushToTalkSounds) PushToTalkSounds.PlayStop();
        lock (_audioLock)
        {
            _segmenter.Flush();
            _stopped = true;
        }
        _sentences.Writer.TryComplete();
        await _consumer;
        Log.Write("Push to talk finished");
        Dispose();
    }

    private async Task ConsumeAsync()
    {
        await foreach (var pcm in _sentences.Reader.ReadAllAsync())
        {
            try
            {
                var started = System.Diagnostics.Stopwatch.StartNew();
                var transcript = await App.Transcriber.TranscribeAsync(pcm, _rules.Prompt, _settings.Language, _cts.Token);
                // Line breaks would press Enter in the target (sending a chat message, for example).
                var text = _rules.Apply(transcript).ReplaceLineEndings(" ").Trim();
                Log.Write($"Push to talk transcript ({started.ElapsedMilliseconds} ms): \"{text}\"");
                _debug.LogTranscript(transcript, text);
                if (text.Length == 0) continue;
                var typed = Win32.TypeText(_typedAny ? " " + text : text);
                Log.Write($"Push to talk typed: {typed}");
                _typedAny = true;
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                Log.Write($"Push to talk error: {ex}");
                _onError(ex.Message);
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _cts.Cancel();
        _recorder.Dispose();
        _ducker.Restore();
        _debug.Dispose();
        _cts.Dispose();
    }
}
