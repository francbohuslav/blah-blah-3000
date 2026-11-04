using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace BlahBlah3000.Audio;

/// <summary>An input device to choose from; a <see cref="Name"/> of null means the Windows default microphone.</summary>
internal sealed record MicOption(string? Name, int Number, string Label);

/// <summary>Records the microphone as PCM s16le, 16 kHz, mono. The device can be switched at runtime by calling Start again.</summary>
internal sealed class MicRecorder : IDisposable
{
    /// <summary>Device number of the Windows default microphone (WAVE_MAPPER).</summary>
    public const int DefaultDevice = -1;

    private WaveInEvent? _waveIn;

    /// <summary>Raised on the NAudio thread; the first call means the microphone is really recording.</summary>
    public event Action<byte[]>? Data;

    /// <summary>Names of the input devices in the order of their numbers.</summary>
    public static IReadOnlyList<string> ListDevices()
    {
        var names = new List<string>();
        for (var i = 0; i < WaveInEvent.DeviceCount; i++)
        {
            names.Add(WaveInEvent.GetCapabilities(i).ProductName);
        }
        return names;
    }

    /// <summary>
    /// Finds the Windows default microphone. Returns its full name and the number of the matching device in <paramref name="devices"/>
    /// (device names are truncated, so the match is by content); the number is <see cref="DefaultDevice"/> when no device matches.
    /// </summary>
    public static (string? Name, int Number) ResolveDefault(IReadOnlyList<string> devices)
    {
        string name;
        try
        {
            using var enumerator = new MMDeviceEnumerator();
            using var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console);
            name = device.FriendlyName;
        }
        catch (Exception ex)
        {
            Log.Write($"Cannot determine the default microphone: {ex.Message}");
            return (null, DefaultDevice);
        }
        for (var i = 0; i < devices.Count; i++)
        {
            var wave = devices[i].Trim();
            if (wave.Length > 0 && name.Contains(wave, StringComparison.OrdinalIgnoreCase)) return (name, i);
        }
        Log.Write($"Default microphone \"{name}\" could not be matched to a device, using WAVE_MAPPER");
        return (name, DefaultDevice);
    }

    /// <summary>The Windows default microphone and all devices; the default is always first.</summary>
    public static List<MicOption> ListOptions()
    {
        var devices = ListDevices();
        var (defaultName, defaultNumber) = ResolveDefault(devices);
        var autoLabel = Loc.T("Mic.Default") + (defaultName != null ? $" – {defaultName}" : "");
        var options = new List<MicOption> { new(null, defaultNumber, autoLabel) };
        for (var i = 0; i < devices.Count; i++)
        {
            options.Add(new MicOption(devices[i], i, devices[i].Trim()));
        }
        return options;
    }

    /// <summary>Finds the saved microphone by name; when it is not connected, returns the default.</summary>
    public static MicOption Resolve(string? name)
    {
        var options = ListOptions();
        Log.Write($"Input devices: {options.Count - 1}, default: {options[0].Label}");
        var selected = options.FirstOrDefault(o => o.Name != null && o.Name == name) ?? options[0];
        if (name != null && selected.Name == null)
        {
            Log.Write($"Saved microphone \"{name}\" is not connected, using the default");
        }
        return selected;
    }

    /// <summary>Stops any previous recording and starts recording from the given device.</summary>
    public void Start(int deviceNumber)
    {
        Stop();
        var waveIn = new WaveInEvent
        {
            DeviceNumber = deviceNumber,
            WaveFormat = new WaveFormat(16_000, 16, 1),
            BufferMilliseconds = 50,
        };
        waveIn.DataAvailable += (sender, e) =>
        {
            // Leftover blocks from a device we have since left are discarded.
            if (!ReferenceEquals(sender, _waveIn)) return;
            var chunk = new byte[e.BytesRecorded];
            Buffer.BlockCopy(e.Buffer, 0, chunk, 0, e.BytesRecorded);
            Data?.Invoke(chunk);
        };
        _waveIn = waveIn;
        try
        {
            waveIn.StartRecording();
        }
        catch
        {
            _waveIn = null;
            waveIn.Dispose();
            throw;
        }
    }

    public void Stop()
    {
        var waveIn = _waveIn;
        _waveIn = null;
        if (waveIn == null) return;
        try { waveIn.StopRecording(); } catch (InvalidOperationException) { }
        waveIn.Dispose();
    }

    public void Dispose() => Stop();
}
