using System.Text.RegularExpressions;

namespace BlahBlah3000.Rules;

/// <summary>Post-transcription rules and the Whisper prompt; everything is read live from the settings.</summary>
internal sealed class RuleEngine(Settings settings)
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);

    /// <summary>Whisper prompt: the hint text from the settings.</summary>
    public string Prompt => settings.Prompt.Trim();

    /// <summary>A part of a sentence; <c>Original</c> is set only for text produced by a replacement.</summary>
    public sealed record Segment(string Text, string? Original = null);

    public string Apply(string text) => string.Concat(ApplySegments(text).Select(s => s.Text));

    /// <summary>Creates the case-insensitive regex of a rule; null when the pattern is empty or not a valid regular expression.</summary>
    public static Regex? TryCreateRegex(string pattern)
    {
        if (pattern.Length == 0) return null;
        try
        {
            return new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, RegexTimeout);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    /// <summary>Applies the replacements and returns the sentence split into unchanged and replaced parts.</summary>
    public List<Segment> ApplySegments(string text)
    {
        var segments = new List<Segment> { new(text) };
        foreach (var rule in settings.Replacements)
        {
            if (rule.Pattern.Length == 0) continue;
            var regex = rule.IsRegex ? TryCreateRegex(rule.Pattern) : null;
            if (rule.IsRegex && regex == null) continue;
            var next = new List<Segment>();
            foreach (var segment in segments)
            {
                if (segment.Original != null)
                {
                    // An already replaced part can be changed by a later rule; the original text is kept.
                    next.Add(segment with { Text = ReplaceInside(segment.Text, rule, regex) });
                    continue;
                }
                if (regex != null) SplitRegex(segment.Text, rule, regex, next);
                else SplitPlain(segment.Text, rule, next);
            }
            segments = next;
        }
        return segments;
    }

    private static string ReplaceInside(string text, ReplacementRule rule, Regex? regex)
    {
        if (regex == null) return text.Replace(rule.Pattern, rule.Replacement, StringComparison.OrdinalIgnoreCase);
        try
        {
            return regex.Replace(text, rule.Replacement);
        }
        catch (RegexMatchTimeoutException)
        {
            return text;
        }
    }

    private static void SplitPlain(string text, ReplacementRule rule, List<Segment> output)
    {
        var start = 0;
        while (true)
        {
            var at = text.IndexOf(rule.Pattern, start, StringComparison.OrdinalIgnoreCase);
            if (at < 0) break;
            if (at > start) output.Add(new Segment(text[start..at]));
            var matched = text.Substring(at, rule.Pattern.Length);
            output.Add(matched == rule.Replacement ? new Segment(matched) : new Segment(rule.Replacement, matched));
            start = at + rule.Pattern.Length;
        }
        if (start < text.Length) output.Add(new Segment(text[start..]));
    }

    /// <summary>Like <see cref="SplitPlain"/>, but the pattern is a regular expression and the replacement may use groups ($1, ${name}).</summary>
    private static void SplitRegex(string text, ReplacementRule rule, Regex regex, List<Segment> output)
    {
        var start = 0;
        try
        {
            foreach (Match match in regex.Matches(text))
            {
                // An empty match would only insert text between characters; such patterns are ignored.
                if (match.Length == 0) continue;
                if (match.Index > start) output.Add(new Segment(text[start..match.Index]));
                var replaced = match.Result(rule.Replacement);
                output.Add(match.Value == replaced ? new Segment(replaced) : new Segment(replaced, match.Value));
                start = match.Index + match.Length;
            }
        }
        catch (RegexMatchTimeoutException)
        {
            Log.Write($"Regular expression timed out: {rule.Pattern}");
            output.Add(new Segment(text[start..]));
            return;
        }
        if (start < text.Length) output.Add(new Segment(text[start..]));
    }
}
