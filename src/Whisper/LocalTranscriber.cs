using System.IO;
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
    private string _loadedDevice = "";

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
            var devices = GpuDevices.List();
            Log.Write($"Vulkan devices: {(devices.Count == 0 ? "none" : string.Join("; ", devices.Select(d => $"{d.Index}: {d.Name}{(d.Discrete ? " (discrete)" : "")}")))}");
            _loadedDevice = App.Settings.GpuDevice;
            var device = GpuDevices.Resolve(_loadedDevice, out var deviceText);
            var options = WhisperFactoryOptions.Default;
            if (device >= 0) options.GpuDevice = device;
            var factory = WhisperFactory.FromPath(ModelStore.ModelPath, options);
            Log.Write($"Model loaded in {started.ElapsedMilliseconds} ms, runtime: {RuntimeOptions.LoadedLibrary}, GPU: {deviceText}");
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
            var missing = MissingVcRuntimeFiles();
            VcRuntimeMissing = missing.Count > 0;
            if (VcRuntimeMissing)
            {
                Log.Write($"Missing Visual C++ runtime libraries: {string.Join(", ", missing)}");
                SetState(TranscriberState.Error, Loc.T("LT.VcRuntimeMissing", string.Join(", ", missing)));
            }
            else
            {
                LogRuntimeFiles();
                SetState(TranscriberState.Error, ex.Message);
            }
            throw;
        }
    }

    /// <summary>Download page of the Microsoft Visual C++ Redistributable (x64) that the native Whisper libraries need.</summary>
    public const string VcRuntimeUrl = "https://aka.ms/vs/17/release/vc_redist.x64.exe";

    /// <summary>True when the last load failed because the Visual C++ runtime is not installed.</summary>
    public bool VcRuntimeMissing { get; private set; }

    public static void OpenVcRuntimeDownload() =>
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(VcRuntimeUrl) { UseShellExecute = true });

    /// <summary>Tries the load again after a failure (for example once the Visual C++ runtime has been installed).</summary>
    public void RetryLoad()
    {
        if (State != TranscriberState.Error) return;
        VcRuntimeMissing = false;
        _ = EnsureLoadedAsync().ContinueWith(_ => { }, TaskScheduler.Default);
    }

    private static List<string> MissingVcRuntimeFiles()
    {
        var missing = new List<string>();
        foreach (var name in new[] { "vcruntime140.dll", "vcruntime140_1.dll", "msvcp140.dll" })
        {
            if (System.Runtime.InteropServices.NativeLibrary.TryLoad(name, out var handle)) System.Runtime.InteropServices.NativeLibrary.Free(handle);
            else missing.Add(name);
        }
        return missing;
    }

    /// <summary>Writes the native Whisper libraries found next to the executable, for diagnosing a failed load.</summary>
    private static void LogRuntimeFiles()
    {
        try
        {
            var folder = Path.Combine(AppContext.BaseDirectory, "runtimes");
            var files = Directory.Exists(folder)
                ? Directory.GetFiles(folder, "*.dll", SearchOption.AllDirectories).Select(f => Path.GetRelativePath(folder, f))
                : [];
            Log.Write($"Native libraries in \"{folder}\": {(files.Any() ? string.Join("; ", files) : "none")}");
        }
        catch (Exception ex)
        {
            Log.Write($"Listing native libraries failed: {ex.Message}");
        }
    }

    /// <summary>Loads the model again when another graphics card was chosen in the settings (it is loaded on the next use if it was unloaded).</summary>
    public void ReloadIfDeviceChanged(string deviceName)
    {
        if (_factory == null || deviceName == _loadedDevice || !_gate.Wait(0)) return;
        try
        {
            lock (_loadLock)
            {
                _factory.Dispose();
                _factory = null;
                _loading = null;
            }
            Log.Write($"Graphics card changed to \"{deviceName}\", reloading the model");
        }
        finally
        {
            _gate.Release();
        }
        Start();
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
