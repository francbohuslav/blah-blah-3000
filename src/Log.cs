using System.IO;

namespace BlahBlah3000;

/// <summary>Simple log in %LOCALAPPDATA%\Bogan\BlahBlah3000\blahblah3000.log; write failures are ignored.</summary>
internal static class Log
{
    private const long MaxBytes = 2 * 1024 * 1024;
    private static readonly object Gate = new();
    private static readonly string Path_ = Path.Combine(AppPaths.Data, "blahblah3000.log");

    /// <summary>Full path of the log file, for messages that point the user to it.</summary>
    public static string FilePath => Path_;

    static Log()
    {
        // A resident application would let the log grow forever; an oversized log is moved aside as .old at startup.
        try
        {
            Directory.CreateDirectory(AppPaths.Data);
            var info = new FileInfo(Path_);
            if (info.Exists && info.Length > MaxBytes) File.Move(Path_, Path_ + ".old", overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }

    /// <summary>
    /// Starts an empty log for this run. The previous log is kept as .old (one run back), so what happened before a crash can still be read.
    /// Called only by the main instance: a second instance that only hands over to it must not touch the log of the running one.
    /// </summary>
    public static void StartNewSession()
    {
        try
        {
            lock (Gate)
            {
                if (File.Exists(Path_)) File.Move(Path_, Path_ + ".old", overwrite: true);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }

    public static void Write(string message)
    {
        try
        {
            lock (Gate)
            {
                File.AppendAllText(Path_, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{Environment.CurrentManagedThreadId}] {message}{Environment.NewLine}");
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }
}
