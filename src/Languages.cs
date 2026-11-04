namespace BlahBlah3000;

internal sealed record LanguageOption(string Code, string Label);

/// <summary>Languages offered for transcription; "auto" lets Whisper detect the language itself.</summary>
internal static class Languages
{
    /// <summary>The list for the current interface language (only the "auto" entry is translated, the others use native names).</summary>
    public static LanguageOption[] All =>
    [
        new("auto", Loc.T("Lang.Auto")), new("en", "English"), new("cs", "Čeština"), new("sk", "Slovenčina"),
        new("de", "Deutsch"), new("pl", "Polski"), new("fr", "Français"), new("es", "Español"),
        new("it", "Italiano"), new("ru", "Русский"), new("uk", "Українська"),
    ];
}
