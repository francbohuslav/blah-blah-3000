using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BlahBlah3000;

/// <summary>One replacement: what is searched for in the transcript (plain text or a regular expression, case-insensitive) and what it is replaced with.</summary>
internal sealed class ReplacementRule
{
    public string Pattern { get; set; } = "";
    public string Replacement { get; set; } = "";

    /// <summary>true = <see cref="Pattern"/> is a regular expression (the replacement may use $1, ${name}); false = plain text.</summary>
    public bool IsRegex { get; set; }
}

/// <summary>User settings stored in %APPDATA%\Bogan\BlahBlah3000\settings.json.</summary>
internal sealed class Settings
{
    private static readonly string FilePath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Bogan", "BlahBlah3000", "settings.json");

    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    /// <summary>Name of the selected microphone; null means the Windows default microphone.</summary>
    public string? Microphone { get; set; }

    /// <summary>Interface language: "auto" (follows Windows), "en" or "cs".</summary>
    public string UiLanguage { get; set; } = Loc.Auto;

    /// <summary>Transcription language: "auto" (Whisper detects it) or a language code (en, cs…).</summary>
    public string Language { get; set; } = "auto";

    /// <summary>Free text passed to Whisper as a hint: terms and names to spell correctly, a style sample, etc.</summary>
    public string Prompt { get; set; } = "";

    public List<ReplacementRule> Replacements { get; set; } = [];

    /// <summary>Silence threshold as RMS (0 to 32768); quieter blocks count as a pause.</summary>
    public double SilenceThreshold { get; set; } = 400;

    /// <summary>How long a silence (ms) ends a sentence.</summary>
    public int PauseMs { get; set; } = 700;

    /// <summary>Save a debug recording and transcripts to the debug folder.</summary>
    public bool DebugRecording { get; set; }

    /// <summary>The first-run wizard has been completed.</summary>
    public bool FirstRunDone { get; set; }

    /// <summary>Global shortcut that opens dictation, e.g. "Win+Shift+D"; empty = no shortcut.</summary>
    public string Hotkey { get; set; } = "Win+Shift+D";

    /// <summary>Push to talk: hold <see cref="PushToTalkKeys"/> to dictate straight into the active window, without the dictation window.</summary>
    public bool PushToTalkEnabled { get; set; }

    /// <summary>Modifier-only combination for push to talk: "Ctrl+Shift", "LeftCtrl", "LeftShift", "RightCtrl" or "RightShift".</summary>
    public string PushToTalkKeys { get; set; } = "Ctrl+Shift";

    /// <summary>How long (ms) the push-to-talk keys must be held before recording starts; a longer delay leaves room for ordinary shortcuts.</summary>
    public int PushToTalkDelayMs { get; set; } = 300;

    /// <summary>Push to talk lowers the computer's sound to this percentage of the current volume while recording; 100 = off, 0 = mute.</summary>
    public int PushToTalkDuckPercent { get; set; } = 100;

    /// <summary>Play a short tone when push to talk starts and when it ends.</summary>
    public bool PushToTalkSounds { get; set; } = true;

    /// <summary>Latch mode only: the recording stops by itself after this many seconds without speech; 0 = never.</summary>
    public int PushToTalkAutoStopSeconds { get; set; } = 3;

    /// <summary>true = after the hold delay the push-to-talk keys can be released; the next press of them stops the recording.</summary>
    public bool PushToTalkLatch { get; set; }

    /// <summary>AI text correction in the dictation window (an OpenAI-compatible chat endpoint); off by default.</summary>
    public bool LlmEnabled { get; set; }

    /// <summary>Base URL of the endpoint, e.g. "https://api.openai.com/v1"; "/chat/completions" is appended.</summary>
    public string LlmBaseUrl { get; set; } = "https://api.openai.com/v1";

    public string LlmModel { get; set; } = "gpt-4o-mini";

    /// <summary>The user's own system prompt; empty = the built-in default in the current interface language.</summary>
    public string LlmPrompt { get; set; } = "";

    /// <summary>Correct the text automatically when Enter is pressed (the result is shown and Enter must be pressed again to paste it).</summary>
    public bool LlmAutoFix { get; set; }

    /// <summary>The API key encrypted for the current Windows user (DPAPI), as Base64; use <see cref="LlmApiKey"/>.</summary>
    public string LlmApiKeyProtected { get; set; } = "";

    /// <summary>The API key in plain text; stored only encrypted and unreadable for another Windows user or on another computer.</summary>
    [JsonIgnore]
    public string LlmApiKey
    {
        get
        {
            if (LlmApiKeyProtected.Length == 0) return "";
            try
            {
                return Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(LlmApiKeyProtected), null, DataProtectionScope.CurrentUser));
            }
            catch (Exception ex) when (ex is CryptographicException or FormatException)
            {
                Log.Write("The saved API key cannot be decrypted (another user or computer); enter it again");
                return "";
            }
        }
        set => LlmApiKeyProtected = value.Length == 0
            ? ""
            : Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(value), null, DataProtectionScope.CurrentUser));
    }

    /// <summary>Start with Windows (HKCU\...\Run entry, started with --tray).</summary>
    public bool Autostart { get; set; }

    /// <summary>Enable the REST API (/health, /transcribe) for other applications.</summary>
    public bool ApiEnabled { get; set; } = true;

    public int ApiPort { get; set; } = 8221;

    /// <summary>true = the API listens on all interfaces (including the LAN), false = localhost only.</summary>
    public bool ApiListenAll { get; set; } = false;

    /// <summary>After how many minutes of inactivity the model is unloaded from memory; 0 = never.</summary>
    public int UnloadAfterMinutes { get; set; }

    /// <summary>Look on GitHub for a newer version (at startup and once a day).</summary>
    public bool CheckForUpdates { get; set; } = true;

    public static Settings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                return JsonSerializer.Deserialize<Settings>(File.ReadAllText(FilePath), Json) ?? new Settings();
            }
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            Log.Write($"Cannot load settings: {ex.Message}");
        }
        return new Settings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, Json));
        }
        catch (IOException ex)
        {
            Log.Write($"Cannot save settings: {ex.Message}");
        }
    }
}
