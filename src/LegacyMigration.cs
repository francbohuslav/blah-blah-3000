using System.IO;
using Microsoft.Win32;

namespace BlahBlah3000;

/// <summary>One-time move of data from the application's former name ("Kecalek") so settings and the downloaded model survive the rename.</summary>
internal static class LegacyMigration
{
    private const string LegacyName = "Kecalek";
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public static void Run()
    {
        try
        {
            MoveFolder(Environment.SpecialFolder.LocalApplicationData);
            MoveFolder(Environment.SpecialFolder.ApplicationData);
            MigrateAutostart();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            // Not fatal: the application simply starts with default settings.
            System.Diagnostics.Debug.WriteLine($"Legacy migration failed: {ex.Message}");
        }
    }

    private static void MoveFolder(Environment.SpecialFolder root)
    {
        var bogan = Path.Combine(Environment.GetFolderPath(root), "Bogan");
        var legacy = Path.Combine(bogan, LegacyName);
        var current = Path.Combine(bogan, "BlahBlah3000");
        if (Directory.Exists(legacy) && !Directory.Exists(current)) Directory.Move(legacy, current);
    }

    private static void MigrateAutostart()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
        if (key?.GetValue(LegacyName) == null) return;
        key.DeleteValue(LegacyName, throwOnMissingValue: false);
        Native.Autostart.Set(true);
    }
}
