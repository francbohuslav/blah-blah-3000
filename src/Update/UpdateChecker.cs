using System.Net.Http;
using System.Reflection;
using System.Text.Json;

namespace BlahBlah3000.Update;

/// <summary>A newer release found on GitHub: its version and the page with the release notes and the installer.</summary>
internal sealed record UpdateInfo(Version Version, string Tag, string Url);

/// <summary>
/// Looks for a newer version through the GitHub API (<c>releases/latest</c>, which ignores drafts and pre-releases).
/// The release tag must be a version such as <c>v1.2.0</c>; the release page holds the notes and the Windows installer.
/// </summary>
internal static class UpdateChecker
{
    /// <summary>GitHub repository as "owner/name". While it still starts with "OWNER/" the check is skipped.</summary>
    private const string Repository = "francbohuslav/blah-blah-3000";

    private static readonly HttpClient Http = CreateClient();

    /// <summary>Version of the running application (major.minor.build).</summary>
    public static Version Current { get; } = Normalize(Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0));

    private static bool IsConfigured => !Repository.StartsWith("OWNER/", StringComparison.Ordinal);

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        // GitHub rejects API requests without a User-Agent.
        client.DefaultRequestHeaders.UserAgent.ParseAdd($"BlahBlah3000/{Current}");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        return client;
    }

    /// <summary>Returns the newer release, or null when the application is up to date or the check failed (failures are only logged).</summary>
    public static async Task<UpdateInfo?> CheckAsync(CancellationToken cancellation = default)
    {
        if (!IsConfigured) return null;
        try
        {
            using var response = await Http.GetAsync($"https://api.github.com/repos/{Repository}/releases/latest", cancellation);
            if (!response.IsSuccessStatusCode)
            {
                Log.Write($"Update check: GitHub answered {(int)response.StatusCode}");
                return null;
            }
            using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellation), cancellationToken: cancellation);
            var tag = json.RootElement.GetProperty("tag_name").GetString() ?? "";
            var url = json.RootElement.GetProperty("html_url").GetString() ?? "";
            if (!TryParseTag(tag, out var version) || !IsReleasePage(url))
            {
                Log.Write($"Update check: unusable release \"{tag}\" ({url})");
                return null;
            }
            Log.Write($"Update check: latest release {tag}, running {Current}");
            return version > Current ? new UpdateInfo(version, tag, url) : null;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or KeyNotFoundException or InvalidOperationException)
        {
            Log.Write($"Update check failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>Accepts "v1.2.0", "1.2" and "1.2.0-beta"; anything else is not a version.</summary>
    private static bool TryParseTag(string tag, out Version version)
    {
        var text = tag.TrimStart('v', 'V');
        var dash = text.IndexOfAny(['-', '+']);
        if (dash >= 0) text = text[..dash];
        if (Version.TryParse(text, out var parsed))
        {
            version = Normalize(parsed);
            return true;
        }
        version = new Version(0, 0, 0);
        return false;
    }

    /// <summary>The URL comes from the network and is opened in a browser, so only https on github.com is trusted.</summary>
    private static bool IsReleasePage(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps && uri.Host == "github.com";

    /// <summary>Version.Revision/Build default to -1 when missing, which would break the comparison; always use three parts.</summary>
    private static Version Normalize(Version v) => new(v.Major, v.Minor, Math.Max(v.Build, 0));
}
