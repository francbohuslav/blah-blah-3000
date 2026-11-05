using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BlahBlah3000.Llm;

/// <summary>A failure that can be shown to the user as it is (wrong key, limit reached, no connection…).</summary>
internal sealed class LlmException(string message) : Exception(message);

/// <summary>A provider offered in the settings; the URL and the model are only a starting point the user can change.</summary>
internal sealed record LlmPreset(string Id, string Label, string Url, string Model);

/// <summary>
/// Text correction through an OpenAI-compatible chat endpoint. Only the most basic request is sent (model, a system message and the text,
/// no other parameters), because that is what every provider and local server understands.
/// </summary>
internal static partial class LlmClient
{
    private static readonly HttpClient Http = new() { Timeout = Timeout.InfiniteTimeSpan };
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(30);

    public static readonly LlmPreset[] Presets =
    [
        new("openai", "OpenAI", "https://api.openai.com/v1", "gpt-4o-mini"),
        new("groq", "Groq", "https://api.groq.com/openai/v1", "llama-3.3-70b-versatile"),
        new("gemini", "Google Gemini", "https://generativelanguage.googleapis.com/v1beta/openai", "gemini-2.5-flash"),
        new("xai", "xAI (Grok)", "https://api.x.ai/v1", "grok-3-mini"),
        new("openrouter", "OpenRouter", "https://openrouter.ai/api/v1", "openai/gpt-4o-mini"),
        new("ollama", "Ollama (local)", "http://localhost:11434/v1", "llama3.2"),
        new("custom", "", "", ""),
    ];

    /// <summary>The built-in prompt in the current interface language.</summary>
    public static string DefaultPrompt => Loc.T("Llm.DefaultPrompt");

    /// <summary>The user's prompt, or the built-in one when the user has not changed it.</summary>
    public static string EffectivePrompt(Settings settings) => string.IsNullOrWhiteSpace(settings.LlmPrompt) ? DefaultPrompt : settings.LlmPrompt;

    /// <summary>Is the text one of the built-in prompts (in any language)? Then it is not stored as the user's own and follows the interface language.</summary>
    public static bool IsDefaultPrompt(string text)
    {
        var (en, cs) = Strings.Table["Llm.DefaultPrompt"];
        var normalized = Normalize(text);
        return normalized.Length == 0 || normalized == Normalize(en) || normalized == Normalize(cs);
    }

    private static string Normalize(string text) => text.Replace("\r\n", "\n").Trim();

    /// <summary>Local servers (Ollama, LM Studio) keep the text on this computer.</summary>
    public static bool IsLocal(string baseUrl) =>
        Uri.TryCreate(baseUrl.Trim(), UriKind.Absolute, out var uri) && (uri.IsLoopback || uri.Host == "localhost");

    public static bool IsValidUrl(string baseUrl) =>
        Uri.TryCreate(baseUrl.Trim(), UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https";

    /// <summary>Corrects the text with the configured provider and prompt.</summary>
    public static async Task<string> FixAsync(Settings settings, string text, CancellationToken ct)
    {
        var started = System.Diagnostics.Stopwatch.StartNew();
        var result = await CompleteAsync(settings.LlmBaseUrl, settings.LlmApiKey, settings.LlmModel, EffectivePrompt(settings), text, ct);
        Log.Write($"Text correction: {text.Length} → {result.Length} chars in {started.ElapsedMilliseconds} ms ({settings.LlmModel})");
        // A much shorter or longer result is not a correction (the model commented, answered or dropped text): the original is kept.
        if (text.Length >= 40 && (result.Length < text.Length / 2 || result.Length > text.Length * 2))
        {
            throw new LlmException(Loc.T("Llm.Unexpected"));
        }
        return result;
    }

    /// <summary>Sends one system and one user message to <c>{baseUrl}/chat/completions</c> and returns the reply.</summary>
    public static async Task<string> CompleteAsync(string baseUrl, string apiKey, string model, string system, string user, CancellationToken ct)
    {
        if (!IsValidUrl(baseUrl)) throw new LlmException(Loc.T("Llm.BadUrl"));
        var uri = new Uri(baseUrl.Trim().TrimEnd('/') + "/chat/completions");
        var body = JsonSerializer.Serialize(new
        {
            model,
            messages = new object[] { new { role = "system", content = system }, new { role = "user", content = user } },
        });
        using var request = new HttpRequestMessage(HttpMethod.Post, uri) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        if (apiKey.Length > 0) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(RequestTimeout);
        var started = System.Diagnostics.Stopwatch.StartNew();
        // The log gets the address, the model and what the service answered, never the API key or the text that was sent.
        var what = $"AI request POST {uri} (model {model})";
        try
        {
            using var response = await Http.SendAsync(request, timeout.Token);
            var json = await response.Content.ReadAsStringAsync(timeout.Token);
            if (!response.IsSuccessStatusCode)
            {
                Log.Write($"{what} failed: HTTP {(int)response.StatusCode} {response.ReasonPhrase} after {started.ElapsedMilliseconds} ms, answer: {Excerpt(json)}");
                throw new LlmException(Describe(response.StatusCode, json));
            }
            try
            {
                var reply = Extract(json);
                Log.Write($"{what} OK in {started.ElapsedMilliseconds} ms");
                return reply;
            }
            catch (LlmException)
            {
                Log.Write($"{what} gave an unusable answer after {started.ElapsedMilliseconds} ms: {Excerpt(json)}");
                throw;
            }
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            Log.Write($"{what} timed out after {RequestTimeout.TotalSeconds:F0} s");
            throw new LlmException(Loc.T("Llm.Timeout"));
        }
        catch (HttpRequestException ex)
        {
            Log.Write($"{what} could not connect: {ex.GetType().Name}: {ex.Message}{(ex.InnerException != null ? " / " + ex.InnerException.Message : "")}");
            throw new LlmException(Loc.T("Llm.Network", ex.Message));
        }
    }

    /// <summary>The beginning of a response body on one line.</summary>
    private static string Excerpt(string body)
    {
        var line = Regex.Replace(body, @"\s+", " ").Trim();
        return line.Length > 600 ? line[..600] + "…" : line;
    }

    private static string Describe(HttpStatusCode status, string json) => status switch
    {
        HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => Loc.T("Llm.Auth"),
        HttpStatusCode.NotFound => Loc.T("Llm.NotFound"),
        HttpStatusCode.TooManyRequests => Loc.T("Llm.RateLimit"),
        _ => Loc.T("Llm.Http", (int)status, ProviderMessage(json)),
    };

    /// <summary>The error text most providers return as <c>{"error": {"message": "…"}}</c>.</summary>
    private static string ProviderMessage(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("error", out var error))
            {
                var message = error.ValueKind == JsonValueKind.Object && error.TryGetProperty("message", out var m) ? m.GetString() : error.ToString();
                if (!string.IsNullOrWhiteSpace(message)) return message.Length > 200 ? message[..200] + "…" : message;
            }
        }
        catch (JsonException)
        {
        }
        return "";
    }

    private static string Extract(string json)
    {
        string? content = null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            var message = doc.RootElement.GetProperty("choices")[0].GetProperty("message");
            if (message.TryGetProperty("content", out var value))
            {
                // Some providers return the content as a list of parts.
                content = value.ValueKind == JsonValueKind.Array
                    ? string.Concat(value.EnumerateArray().Select(part => part.TryGetProperty("text", out var t) ? t.GetString() : ""))
                    : value.GetString();
            }
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException or IndexOutOfRangeException)
        {
            throw new LlmException(Loc.T("Llm.BadReply"));
        }
        // Reasoning models may put their thoughts into the reply.
        var text = ThinkBlock().Replace(content ?? "", "").Trim();
        if (text.Length == 0) throw new LlmException(Loc.T("Llm.Empty"));
        return text;
    }

    [GeneratedRegex("<think>.*?</think>", RegexOptions.Singleline)]
    private static partial Regex ThinkBlock();
}
