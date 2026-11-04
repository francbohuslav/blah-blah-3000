using System.IO;

namespace BlahBlah3000;

/// <summary>Application data folder (log, debug output, model) in %LOCALAPPDATA%, so the exe can be installed anywhere.</summary>
internal static class AppPaths
{
    public static string Data { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Bogan", "BlahBlah3000");
}
