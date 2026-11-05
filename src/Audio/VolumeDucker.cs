using System.IO;
using System.Text.Json;
using NAudio.CoreAudioApi;

namespace BlahBlah3000.Audio;

/// <summary>
/// Lowers (or mutes) the sound of the default playback device while push to talk records, and puts it back afterwards. The state is kept
/// in a small file while it is lowered, so a crash does not leave the computer quiet: <see cref="RecoverAfterCrash"/> restores it.
/// </summary>
internal sealed class VolumeDucker : IDisposable
{
    private sealed record State(string DeviceId, float Original, float Applied, bool Muted);

    // The hardware volume moves in steps, so the volume read back after setting it differs a little from the requested one.
    private const float Tolerance = 0.02f;

    private static readonly string StatePath = Path.Combine(AppPaths.Data, "duck-state.json");

    private readonly object _lock = new();
    private MMDevice? _device;
    private State? _state;
    private bool _restored;

    /// <summary>Lowers the volume to <paramref name="percent"/> % of the current one (0 = mute) after the delay, unless it was restored meanwhile.</summary>
    public async Task DuckAsync(TimeSpan delay, int percent)
    {
        await Task.Delay(delay);
        lock (_lock)
        {
            if (_restored || _state != null) return;
            try
            {
                Apply(percent);
            }
            catch (Exception ex)
            {
                Log.Write($"Cannot lower the volume: {ex.Message}");
                Cleanup();
            }
        }
    }

    private void Apply(int percent)
    {
        using var enumerator = new MMDeviceEnumerator();
        var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        var volume = device.AudioEndpointVolume;
        // Already silent: there is nothing to lower and nothing to restore.
        if (volume.Mute)
        {
            device.Dispose();
            return;
        }
        var original = volume.MasterVolumeLevelScalar;
        var muted = percent <= 0;
        if (muted) volume.Mute = true;
        else volume.MasterVolumeLevelScalar = original * Math.Clamp(percent, 0, 100) / 100f;
        // The volume as the device really took it, to recognize later whether the user changed it.
        var applied = muted ? original : volume.MasterVolumeLevelScalar;
        _device = device;
        _state = new State(device.ID, original, applied, muted);
        try
        {
            Directory.CreateDirectory(AppPaths.Data);
            File.WriteAllText(StatePath, JsonSerializer.Serialize(_state));
        }
        catch (IOException ex)
        {
            Log.Write($"Cannot save the volume state: {ex.Message}");
        }
        Log.Write($"Volume lowered: {original:P0} → {(muted ? "muted" : applied.ToString("P0"))}");
    }

    /// <summary>Puts the volume back; safe to call repeatedly. A volume the user has changed in the meantime is left alone.</summary>
    public void Restore()
    {
        lock (_lock)
        {
            _restored = true;
            if (_state == null || _device == null) return;
            try
            {
                RestoreState(_device, _state);
                Log.Write("Volume restored");
            }
            catch (Exception ex)
            {
                Log.Write($"Cannot restore the volume: {ex.Message}");
            }
            Cleanup();
        }
    }

    private static void RestoreState(MMDevice device, State state)
    {
        var volume = device.AudioEndpointVolume;
        if (state.Muted)
        {
            if (volume.Mute) volume.Mute = false;
        }
        else if (!volume.Mute && Math.Abs(volume.MasterVolumeLevelScalar - state.Applied) < Tolerance)
        {
            volume.MasterVolumeLevelScalar = state.Original;
        }
    }

    private void Cleanup()
    {
        _device?.Dispose();
        _device = null;
        _state = null;
        try { File.Delete(StatePath); } catch (IOException) { }
    }

    /// <summary>At startup: a state file left behind means the application ended while the volume was lowered; the volume is put back.</summary>
    public static void RecoverAfterCrash()
    {
        try
        {
            if (!File.Exists(StatePath)) return;
            var state = JsonSerializer.Deserialize<State>(File.ReadAllText(StatePath));
            if (state != null)
            {
                using var enumerator = new MMDeviceEnumerator();
                using var device = enumerator.GetDevice(state.DeviceId);
                RestoreState(device, state);
                Log.Write("Volume restored after an unfinished session");
            }
        }
        catch (Exception ex)
        {
            Log.Write($"Cannot recover the volume: {ex.Message}");
        }
        try { File.Delete(StatePath); } catch (IOException) { }
    }

    public void Dispose() => Restore();
}
