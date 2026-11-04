using System.Globalization;
using System.Windows.Markup;

namespace BlahBlah3000;

/// <summary>
/// User interface texts in English and Czech. English is the default; Czech is chosen when the setting says so
/// or when the setting is "auto" and Windows uses Czech.
/// </summary>
internal static class Loc
{
    public const string Auto = "auto";

    /// <summary>Languages the interface is translated to, as (code, name in that language).</summary>
    public static readonly (string Code, string Name)[] Available = [("en", "English"), ("cs", "Čeština")];

    /// <summary>Code of the language in use ("en" or "cs").</summary>
    public static string Current { get; private set; } = "en";

    /// <summary>Picks the interface language from the setting ("auto", "en" or "cs").</summary>
    public static void Init(string setting)
    {
        Current = Available.Any(l => l.Code == setting) ? setting : Detect();
    }

    private static string Detect()
    {
        var system = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        return Available.Any(l => l.Code == system) ? system : "en";
    }

    public static string T(string key)
    {
        if (!Strings.Table.TryGetValue(key, out var pair)) return key;
        return Current == "cs" ? pair.Cs : pair.En;
    }

    public static string T(string key, params object[] args) => string.Format(CultureInfo.CurrentCulture, T(key), args);
}

/// <summary>XAML access to <see cref="Loc"/>: <c>Text="{local:Loc Settings.Save}"</c>.</summary>
[MarkupExtensionReturnType(typeof(string))]
public sealed class LocExtension(string key) : MarkupExtension
{
    public override object ProvideValue(IServiceProvider serviceProvider) => Loc.T(key);
}
