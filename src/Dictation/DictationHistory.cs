namespace BlahBlah3000.Dictation;

/// <summary>One dictated sentence at each stage: what Whisper heard, after the replacements, and what was finally typed.</summary>
internal sealed record HistoryEntry(DateTime Time, string Heard, string Replaced, string Typed)
{
    /// <summary>One line for the list: time and the typed text.</summary>
    public string Title => $"{Time:HH:mm:ss}  {Typed}";
}

/// <summary>
/// The latest sentences of push to talk, kept in memory only (never written to disk, gone when the application exits). The user can look
/// there for what went wrong in a sentence typed into another application and add a replacement for it.
/// </summary>
internal static class DictationHistory
{
    private const int Capacity = 50;
    private static readonly object Lock = new();
    private static readonly List<HistoryEntry> Items = [];

    /// <summary>Raised on the thread that added the entry.</summary>
    public static event Action? Changed;

    /// <summary>The entries, the newest first.</summary>
    public static IReadOnlyList<HistoryEntry> Snapshot()
    {
        lock (Lock) return [.. Items.AsEnumerable().Reverse()];
    }

    public static void Add(string heard, string replaced, string typed)
    {
        lock (Lock)
        {
            Items.Add(new HistoryEntry(DateTime.Now, heard, replaced, typed));
            if (Items.Count > Capacity) Items.RemoveAt(0);
        }
        Changed?.Invoke();
    }

    public static void Clear()
    {
        lock (Lock) Items.Clear();
        Changed?.Invoke();
    }
}
