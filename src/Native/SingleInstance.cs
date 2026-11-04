using System.IO;
using System.IO.Pipes;

namespace BlahBlah3000.Native;

/// <summary>
/// A single running instance. A second launch (e.g. from a shortcut tool) sends the first instance, over a named pipe,
/// the window it came from and exits; the first instance then opens dictation for that window.
/// </summary>
internal sealed class SingleInstance : IDisposable
{
    private const string MutexName = @"Local\Bogan.BlahBlah3000.SingleInstance";
    private const string PipeName = "Bogan.BlahBlah3000.Activate";

    private readonly Mutex _mutex;
    private readonly CancellationTokenSource _cts = new();

    private SingleInstance(Mutex mutex) => _mutex = mutex;

    /// <summary>Raised on the pipe thread; the argument is the window that was in the foreground at the second launch.</summary>
    public event Action<IntPtr>? Activated;

    /// <summary>Returns the instance if this is the first one; otherwise null.</summary>
    public static SingleInstance? TryAcquire()
    {
        var mutex = new Mutex(true, MutexName, out var created);
        if (created) return new SingleInstance(mutex);
        mutex.Dispose();
        return null;
    }

    /// <summary>Tells the running instance to start dictation; false when it could not be reached.</summary>
    public static bool SignalRunning(IntPtr foreground)
    {
        try
        {
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            client.Connect(2000);
            using var writer = new StreamWriter(client) { AutoFlush = true };
            writer.WriteLine($"activate {foreground.ToInt64()}");
            return true;
        }
        catch (Exception ex) when (ex is IOException or TimeoutException or UnauthorizedAccessException)
        {
            Log.Write($"Cannot reach the running instance: {ex.Message}");
            return false;
        }
    }

    public void StartListening() => _ = Task.Run(ListenAsync);

    private async Task ListenAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            try
            {
                await using var server = new NamedPipeServerStream(PipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                await server.WaitForConnectionAsync(_cts.Token);
                using var reader = new StreamReader(server);
                var line = await reader.ReadLineAsync(_cts.Token);
                if (line != null && line.StartsWith("activate ", StringComparison.Ordinal)
                    && long.TryParse(line.AsSpan(9), out var handle))
                {
                    Activated?.Invoke(new IntPtr(handle));
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Log.Write($"Pipe error: {ex.Message}");
                await Task.Delay(500);
            }
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _mutex.ReleaseMutex();
        _mutex.Dispose();
    }
}
