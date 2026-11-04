using System.Text;
using Whisper.net;
using Whisper.net.LibraryLoader;

namespace BlahBlah3000.Whisper;

internal enum TranscriberState { Idle, ModelMissing, Downloading, Loading, Ready, Error }

/// <summary>Built-in Whisper (whisper.cpp through Whisper.net); one model, transcriptions run one at a time behind a lock.</summary>
internal sealed class LocalTranscriber
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly object _loadLock = new();
    private WhisperFactory? _factory;
    private Task? _loading;
    private CancellationTokenSource? _download;
    private DateTime _lastUse = DateTime.UtcNow;

    public TranscriberState State { get; private set; } = TranscriberState.Idle;

    /// <summary>Detail of the state: the error text (for Error and for ModelMissing after a failed download).</summary>
    public string Detail { get; private set; } = "";

    /// <summary>Download progress from 0 to 1.</summary>
    public double Progress { get; private set; }

    public event Action? StateChanged;

    private void SetState(TranscriberState state, string detail = "")
    {
        State = state;
        Detail = detail;
        StateChanged?.Invoke();
    }

    /// <summary>At startup: an existing model is loaded; a missing one is only reported, never downloaded.</summary>
    public void Start()
    {
        if (!ModelStore.Exists())
        {
            SetState(TranscriberState.ModelMissing);
            return;
        }
        _ = EnsureLoadedAsync().ContinueWith(_ => { }, TaskScheduler.Default);
    }

    /// <summary>Starts loading the model (once); repeated calls return the same task. A missing model is an exception.</summary>
    public Task EnsureLoadedAsync()
    {
        lock (_loadLock)
        {
            if (_factory != null) return Task.CompletedTask;
            if (State == TranscriberState.Downloading)
            {
                return Task.FromException(new InvalidOperationException(Loc.T("LT.Downloading")));
            }
            if (!ModelStore.Exists())
            {
                SetState(TranscriberState.ModelMissing);
                return Task.FromException(new ModelMissingException());
            }
            return _loading ??= Task.Run(LoadAsync);
        }
    }

    /// <summary>Downloads the model (only when the user asks for it) and loads it; <see cref="CancelDownload"/> interrupts the download.</summary>
    public async Task DownloadModelAsync()
    {
        CancellationTokenSource cts;
        lock (_loadLock)
        {
            if (State == TranscriberState.Downloading || ModelStore.Exists() && _factory != null) return;
            cts = _download = new CancellationTokenSource();
            Progress = 0;
            SetState(TranscriberState.Downloading);
        }
        try
        {
            var progress = new Progress<double>(p =>
            {
                Progress = p;
                StateChanged?.Invoke();
            });
            await ModelStore.DownloadAsync(progress, cts.Token);
            // Leave the Downloading state, otherwise EnsureLoadedAsync below refuses to load and the UI stays stuck at 100 %.
            SetState(TranscriberState.Idle);
        }
        catch (OperationCanceledException)
        {
            Log.Write("Model download cancelled");
            SetState(TranscriberState.ModelMissing);
            return;
        }
        catch (Exception ex)
        {
            Log.Write($"Model download failed: {ex.Message}");
            SetState(TranscriberState.ModelMissing, Loc.T("LT.DownloadFailed", ex.Message));
            return;
        }
        finally
        {
            cts.Dispose();
            _download = null;
        }
        try
        {
            await EnsureLoadedAsync();
        }
        catch (Exception)
        {
            // The loading itself has already set the state and the error.
        }
    }

    public void CancelDownload() => _download?.Cancel();

    private async Task LoadAsync()
    {
        try
        {
            SetState(TranscriberState.Loading);
            var started = System.Diagnostics.Stopwatch.StartNew();
            RuntimeOptions.RuntimeLibraryOrder = [RuntimeLibrary.Vulkan, RuntimeLibrary.Cpu];
            var factory = WhisperFactory.FromPath(ModelStore.ModelPath);
            Log.Write($"Model loaded in {started.ElapsedMilliseconds} ms, runtime: {RuntimeOptions.LoadedLibrary}");
            // Warm-up on 1 s of silence so the first real sentence does not wait for GPU initialization.
            await TranscribeCoreAsync(factory, new float[16_000], "", "auto", CancellationToken.None);
            Log.Write($"Warm-up finished in {started.ElapsedMilliseconds} ms");
            _lastUse = DateTime.UtcNow;
            _factory = factory;
            SetState(TranscriberState.Ready);
        }
        catch (Exception ex)
        {
            Log.Write($"Loading the model failed: {ex}");
            lock (_loadLock) _loading = null;
            SetState(TranscriberState.Error, ex.Message);
            throw;
        }
    }

    /// <summary>Unloads the model from memory if it has not been used for longer than <paramref name="idle"/> and nothing is being transcribed.</summary>
    public void UnloadIfIdle(TimeSpan idle)
    {
        if (_factory == null || DateTime.UtcNow - _lastUse < idle || !_gate.Wait(0)) return;
        try
        {
            lock (_loadLock)
            {
                _factory?.Dispose();
                _factory = null;
                _loading = null;
            }
            Log.Write("Model unloaded from memory (inactivity)");
            SetState(TranscriberState.Idle);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Transcribes PCM s16le 16 kHz mono; language is "auto" or a language code (en, cs…).</summary>
    public async Task<string> TranscribeAsync(byte[] pcm, string prompt, string language, CancellationToken ct)
    {
        var samples = new float[pcm.Length / 2];
        for (var i = 0; i < samples.Length; i++)
        {
            samples[i] = BitConverter.ToInt16(pcm, i * 2) / 32768f;
        }
        while (true)
        {
            await EnsureLoadedAsync().WaitAsync(ct);
            await _gate.WaitAsync(ct);
            try
            {
                // The idle timer may have unloaded the model in the meantime; it is then loaded again.
                if (_factory == null) continue;
                var result = await TranscribeCoreAsync(_factory, samples, prompt, language, ct);
                _lastUse = DateTime.UtcNow;
                return result;
            }
            finally
            {
                _gate.Release();
            }
        }
    }

    private static async Task<string> TranscribeCoreAsync(WhisperFactory factory, float[] samples, string prompt, string language, CancellationToken ct)
    {
        var builder = factory.CreateBuilder().WithTemperature(0);
        builder = string.IsNullOrWhiteSpace(language) || language == "auto" ? builder.WithLanguageDetection() : builder.WithLanguage(language);
        if (prompt.Length > 0) builder = builder.WithPrompt(prompt);
        await using var processor = builder.Build();
        var text = new StringBuilder();
        await foreach (var segment in processor.ProcessAsync(samples, ct))
        {
            text.Append(segment.Text);
        }
        var result = text.ToString().Trim();
        // An echo of the prompt on noise without speech is discarded.
        return prompt.Length > 0 && result == prompt.Trim() ? "" : result;
    }
}

/// <summary>The model is not on disk; the user has to approve and download it first.</summary>
internal sealed class ModelMissingException() : InvalidOperationException(Loc.T("LT.Missing"));
