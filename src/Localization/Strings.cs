namespace BlahBlah3000;

/// <summary>Translation table: key → (English, Czech). Texts with {0}, {1}… are formatted by <see cref="Loc.T(string, object[])"/>.</summary>
internal static class Strings
{
    public static readonly Dictionary<string, (string En, string Cs)> Table = new()
    {
        // Tray icon
        ["Tray.Dictate"] = ("Dictate (to clipboard)", "Diktovat (do schránky)"),
        ["Tray.Settings"] = ("Settings…", "Nastavení…"),
        ["Tray.Download"] = ("Download Whisper model…", "Stáhnout Whisper model…"),
        ["Tray.Autostart"] = ("Start with Windows", "Spouštět s Windows"),
        ["Tray.Exit"] = ("Exit", "Konec"),
        ["Tray.State.Ready"] = ("Whisper ready", "Whisper připraven"),
        ["Tray.State.Idle"] = ("Whisper unloaded from memory", "Whisper uvolněn z paměti"),
        ["Tray.State.Loading"] = ("loading Whisper model…", "načítám Whisper model…"),
        ["Tray.State.Downloading"] = ("downloading model {0} %", "stahuji model {0} %"),
        ["Tray.State.Missing"] = ("Whisper model missing", "chybí Whisper model"),
        ["Tray.State.Error"] = ("Whisper failed to load", "Whisper se nenačetl"),
        ["Tray.ApiDown"] = (" (API not running)", " (API nejede)"),
        ["Tray.HotkeyFailed"] = ("Cannot register the shortcut {0}; another application uses it. Change it in settings.",
            "Zkratku {0} nejde zaregistrovat, má ji jiná aplikace. Změň ji v nastavení."),
        ["Tray.Recording"] = ("BlahBlah3000 – recording", "BlahBlah3000 – nahrává"),
        ["Tray.PttHookFailed"] = ("Cannot start push to talk: the keyboard hook could not be installed.",
            "Push to talk nejde spustit: nepodařilo se nainstalovat klávesnicový hook."),
        ["Tray.PttModelUnavailable"] = ("Push to talk is not available: the Whisper model is missing or failed to load.",
            "Push to talk není dostupný: Whisper model chybí nebo se nenačetl."),
        ["Tray.ApiFailed"] = ("Cannot start the API on port {0}: {1}", "API nejde spustit na portu {0}: {1}"),

        // Dictation window
        ["Main.DragTip"] = ("Drag to move the window", "Tažením přesuneš okno"),
        ["Main.SettingsTip"] = ("Settings", "Nastavení"),
        ["Main.AddRuleTip"] = ("Add the selected text to replacements", "Přidat vybraný text do náhrad"),
        ["Main.Hint.Paste"] = ("Enter = paste · Shift+Enter = new line · Esc = cancel", "Enter = vložit · Shift+Enter = nový řádek · Esc = zrušit"),
        ["Main.Hint.Clipboard"] = ("Enter = copy to clipboard · Shift+Enter = new line · Esc = cancel",
            "Enter = zkopírovat do schránky · Shift+Enter = nový řádek · Esc = zrušit"),
        ["Main.OpeningMic"] = ("Opening microphone…", "Otevírám mikrofon…"),
        ["Main.Recording"] = ("● Recording", "● Nahrává"),
        ["Main.Processing"] = ("Processing…", "Zpracovávám…"),
        ["Main.NoAudio"] = ("No sound is coming from the microphone (check the privacy settings and the default microphone)",
            "Z mikrofonu nepřichází zvuk (zkontroluj soukromí a výchozí mikrofon)"),
        ["Main.MicOpenFailed"] = ("Cannot open the microphone: {0}", "Mikrofon nejde otevřít: {0}"),
        ["Main.ErrorEsc"] = ("{0} (Esc = close)", "{0} (Esc = zavřít)"),
        ["Main.ClipboardFailed"] = ("Could not set the clipboard (Esc = close)", "Schránku se nepodařilo nastavit (Esc = zavřít)"),
        ["Main.InClipboard"] = ("The text is in the clipboard (Ctrl+V)", "Text je ve schránce (Ctrl+V)"),
        ["Main.Transcribing"] = ("Whisper: ⟳ transcribing…", "Whisper: ⟳ přepisuji…"),
        ["Main.Done"] = ("Whisper: ✓ {0:F2} s", "Whisper: ✓ {0:F2} s"),
        ["Main.Failed"] = ("Whisper: ✗ error", "Whisper: ✗ chyba"),
        ["Main.Downloading"] = ("Whisper: downloading model {0}", "Whisper: stahuji model {0}"),
        ["Main.ModelMissing"] = ("Whisper: ✗ model missing – click to download", "Whisper: ✗ chybí model – klikni pro stažení"),
        ["Main.ModelError"] = ("Whisper: ✗ the model failed to load", "Whisper: ✗ model se nenačetl"),
        ["Main.ModelLoading"] = ("Whisper: loading model…", "Whisper: načítám model…"),
        ["Main.Original"] = ("Original: {0}", "Původně: {0}"),

        // Updates
        ["Update.Available"] = ("New version {0} is available – click for details", "Je dostupná nová verze {0} – klikni pro podrobnosti"),
        ["Update.ConfirmTitle"] = ("BlahBlah3000 update", "Aktualizace BlahBlah3000"),
        ["Update.Confirm"] = ("BlahBlah3000 will now close and the release notes of version {0} will open in your browser, where you can download the installer.\n\nContinue?",
            "BlahBlah3000 se nyní ukončí a v prohlížeči se otevřou poznámky k vydání verze {0}, odkud si můžeš stáhnout instalátor.\n\nPokračovat?"),

        // Settings window
        ["Set.Title"] = ("BlahBlah3000 – settings", "BlahBlah3000 – nastavení"),
        ["Set.Tab.General"] = ("General", "Obecné"),
        ["Set.Tab.Hint"] = ("Hint", "Nápověda"),
        ["Set.Tab.Replacements"] = ("Replacements", "Náhrady"),
        ["Set.Tab.Recording"] = ("Recording", "Nahrávání"),
        ["Set.Tab.Activation"] = ("Activation", "Aktivace"),
        ["Set.Tab.Advanced"] = ("Advanced", "Pokročilé"),
        ["Set.Debug.Title"] = ("Debugging", "Ladění"),
        ["Set.UiLanguage"] = ("Interface language", "Jazyk rozhraní"),
        ["Set.UiLanguage.Hint"] = ("Applies to windows opened from now on.", "Projeví se v nově otevřených oknech."),
        ["Set.UiLanguage.Auto"] = ("Automatic (system language)", "Automaticky (podle systému)"),
        ["Set.Language"] = ("Transcription language", "Jazyk přepisu"),
        ["Set.Language.Hint"] = ("Automatic = Whisper detects the language itself (it may be wrong on short sentences). A fixed language is more reliable.",
            "Automaticky = Whisper jazyk pozná sám (u krátkých vět může chybovat). Pevný jazyk je spolehlivější."),
        ["Set.Hotkey"] = ("Global shortcut for dictation", "Globální zkratka pro diktování"),
        ["Set.Clear"] = ("Clear", "Vymazat"),
        ["Set.Hotkey.Tip"] = ("Click here and press the desired combination (at least one of Ctrl, Alt, Shift, Win)",
            "Klikni sem a stiskni požadovanou kombinaci (alespoň jeden z Ctrl, Alt, Shift, Win)"),
        ["Set.Hotkey.Hint"] = ("Click the field and press a combination. The text is pasted into the window that was active when you pressed it.",
            "Klikni do pole a stiskni kombinaci. Text se vloží do okna, které bylo při stisku aktivní."),
        ["Set.Ptt"] = ("Push to talk (dictate without a window)", "Push to talk (diktování bez okna)"),
        ["Set.Ptt.Enable"] = ("Hold the keys below and speak: each sentence is typed into the active window at the cursor",
            "Drž klávesy níže a mluv: každá věta se napíše do aktivního okna na pozici kurzoru"),
        ["Set.Ptt.Keys.CtrlShift"] = ("Ctrl + Shift", "Ctrl + Shift"),
        ["Set.Ptt.Latch"] = ("Latch: after the delay the keys can be released; recording stops when you press them again",
            "Přepínání: po uplynutí doby můžeš klávesy pustit; nahrávání skončí, až je stiskneš znovu"),
        ["Set.Ptt.Keys.LeftCtrl"] = ("Left Ctrl", "Levý Ctrl"),
        ["Set.Ptt.Keys.LeftShift"] = ("Left Shift", "Levý Shift"),
        ["Set.Ptt.Delay"] = ("Hold the keys for: {0:F0} ms before recording starts", "Podržet klávesy: {0:F0} ms, než se začne nahrávat"),
        ["Set.Ptt.Keys.RightCtrl"] = ("Right Ctrl", "Pravý Ctrl"),
        ["Set.Ptt.Keys.RightShift"] = ("Right Shift", "Pravý Shift"),
        ["Set.Ptt.Hint"] = ("Recording starts after the keys are held for the time above and ends when you release them; the tray icon turns red. " +
            "A longer time lets you use ordinary shortcuts (such as Ctrl+C when Ctrl is a push-to-talk key) without starting a recording. " +
            "The text is typed like keyboard input, so it does not work in windows running as administrator.",
            "Nahrávání začne, když klávesy podržíš po dobu nastavenou výše, a skončí po jejich uvolnění; ikona v liště zčervená. " +
            "Delší doba umožní používat běžné zkratky (třeba Ctrl+C, když je Ctrl klávesou pro push to talk), aniž by se spustilo nahrávání. " +
            "Text se píše jako vstup z klávesnice, takže nefunguje v oknech spuštěných jako správce."),
        ["Set.Autostart"] = ("Start BlahBlah3000 with Windows (it runs in the tray)", "Spouštět BlahBlah3000 s Windows (poběží v liště)"),
        ["Set.CheckUpdates"] = ("Check for a new version on GitHub", "Kontrolovat novou verzi na GitHubu"),
        ["Set.Api"] = ("REST API for other applications", "REST API pro ostatní aplikace"),
        ["Set.Api.Enable"] = ("Enable the API (GET /health, POST /transcribe)", "Zapnout API (GET /health, POST /transcribe)"),
        ["Set.Port"] = ("Port:", "Port:"),
        ["Set.ListenAll"] = ("Listen on the local network too (otherwise localhost only)", "Poslouchat i z místní sítě (jinak jen localhost)"),
        ["Set.Unload.Before"] = ("Unload the model from memory after", "Uvolnit model z paměti po"),
        ["Set.Unload.After"] = ("min of inactivity (0 = never)", "min nečinnosti (0 = nikdy)"),
        ["Set.Debug"] = ("Debugging: save the recording (debug\\recording.wav) and the transcripts before and after replacements (debug\\transcripts.txt)",
            "Ladění: ukládat nahrávku (debug\\recording.wav) a přepisy před a po náhradách (debug\\transcripts.txt)"),
        ["Set.Hint.Intro"] = ("Free text passed to Whisper as a hint before every transcription. List terms and names it should spell correctly " +
            "and add a sample sentence in your style, e.g. \"Kubernetes, Grafana. Today I finished the first part and left the second for tomorrow.\" " +
            "Keep it short (a few dozen words); a sentence with a number written as a word makes Whisper write numbers as words. Leave it empty if you do not need it.",
            "Volný text, který Whisper dostane jako nápovědu před každým přepisem. Vypiš termíny a jména, která má psát správně, " +
            "a přidej ukázkovou větu ve svém stylu, např. „Kubernetes, Grafana. Dnes jsem dokončil první část a druhou nechám na zítřek.“ " +
            "Drž ho krátký (pár desítek slov); je-li ve větě číslo napsané slovem, Whisper píše čísla slovy. Nepotřebuješ-li ho, nech pole prázdné."),
        ["Set.Rep.Intro"] = ("After transcription, the text on the left is replaced with the text on the right. Matching is case-insensitive; by default the text is matched literally. Tick Regex to use a regular expression (the replacement may then use groups such as $1). Add a new row at the end of the table.",
            "Po přepisu se text hledaný vlevo nahradí textem vpravo. Hledá se bez ohledu na velikost písmen, ve výchozím stavu doslovně. Zaškrtnutím Regex použiješ regulární výraz (v náhradě pak jdou použít skupiny jako $1). Nový řádek přidáš na konci tabulky."),
        ["Set.Rep.Said"] = ("What is said", "Co se řekne"),
        ["Set.Rep.Written"] = ("What is written", "Co se napíše"),
        ["Set.Rep.Regex"] = ("Regex", "Regex"),
        ["Set.RegexInvalid"] = ("✗ invalid regular expression: {0}", "✗ neplatný regulární výraz: {0}"),
        ["Set.Mic"] = ("Microphone", "Mikrofon"),
        ["Set.Sens.Intro"] = ("Speak and watch the green bar. Set the silence threshold so that speech clearly exceeds it and the noise between pauses does not.",
            "Mluv a sleduj zelený pruh. Práh ticha nastav tak, aby ho řeč bezpečně přesahovala a šum v pauzách ne."),
        ["Set.Level"] = ("Current microphone level", "Aktuální hladina z mikrofonu"),
        ["Set.Threshold"] = ("Silence threshold: {0:F0} (quieter sound counts as a pause)", "Práh ticha: {0:F0} (tišší zvuk se bere jako pauza)"),
        ["Set.Pause.Title"] = ("Pause that ends a sentence", "Pauza, která ukončí větu"),
        ["Set.Pause"] = ("Pause: {0:F0} ms", "Pauza: {0:F0} ms"),
        ["Set.Save"] = ("Save", "Uložit"),
        ["Set.Cancel"] = ("Cancel", "Zrušit"),
        ["Set.ModelLoaded"] = ("● Model {0} is loaded", "● Model {0} je načtený"),
        ["Set.ModelFailed"] = ("✗ The model failed to load: {0}", "✗ Model se nenačetl: {0}"),
        ["Set.ModelPreparing"] = ("The model is being prepared ({0} {1})", "Model se připravuje ({0} {1})"),
        ["Set.PortInvalid"] = ("✗ the port must be a number from 1 to 65535", "✗ port musí být číslo 1 až 65535"),
        ["Set.UnloadInvalid"] = ("✗ the inactivity time must be 0 or a positive number", "✗ doba nečinnosti musí být číslo 0 nebo větší"),
        ["Set.HotkeyTaken"] = ("✗ the shortcut {0} is already used by another application, choose a different one",
            "✗ zkratku {0} už používá jiná aplikace, vyber jinou"),

        // First-run wizard
        ["FR.Title"] = ("BlahBlah3000 – first run", "BlahBlah3000 – první spuštění"),
        ["FR.Welcome"] = ("Welcome to BlahBlah3000", "Vítej v BlahBlah3000"),
        ["FR.Intro"] = ("Before you start dictating, set up three things. You can change everything later in settings.",
            "Než začneš diktovat, nastav tři věci. Všechno půjde později změnit v nastavení."),
        ["FR.Step1"] = ("1. Dictation language", "1. Jazyk diktování"),
        ["FR.Step1.Hint"] = ("If you dictate in several languages, leave Automatic. A fixed language is more reliable if you speak only one.",
            "Diktuješ-li ve více jazycích, nech Automaticky. Pevný jazyk je spolehlivější, pokud mluvíš jen jedním."),
        ["FR.Step2"] = ("2. Whisper model (speech recognition)", "2. Whisper model (rozpoznávání řeči)"),
        ["FR.Model.Downloading"] = ("Downloading: {0:F0} MB of {1:F0} MB", "Stahuje se: {0:F0} MB z {1:F0} MB"),
        ["FR.Model.Missing"] = ("The model {0} is not downloaded. It is about {1:F0} MB and will be downloaded from huggingface.co to the folder {2}. " +
            "Dictation does not work without it; you can also download it later from the tray icon menu.",
            "Model {0} není stažený. Má asi {1:F0} MB a stáhne se z huggingface.co do složky {2}. " +
            "Bez něj diktování nefunguje, stáhnout ho jde i později z nabídky ikony v liště."),
        ["FR.Model.Error"] = ("The model is downloaded but could not be loaded: {0}", "Model je stažený, ale nepodařilo se ho načíst: {0}"),
        ["FR.Model.OK"] = ("✓ The model is downloaded.", "✓ Model je stažený."),
        ["FR.Model.Cancel"] = ("Cancel", "Přerušit"),
        ["FR.Model.Download"] = ("Download ({0:F0} MB)", "Stáhnout ({0:F0} MB)"),
        ["FR.Step3"] = ("3. Keyboard shortcut for dictation", "3. Klávesová zkratka pro diktování"),
        ["FR.Hotkey.Tip"] = ("Click here and press the desired combination", "Klikni sem a stiskni požadovanou kombinaci"),
        ["FR.Hotkey.Free"] = ("✓ The shortcut {0} is free. Click the field and press another combination to change it.",
            "✓ Zkratka {0} je volná. Klikni do pole a stiskni jinou kombinaci, chceš-li ji změnit."),
        ["FR.Hotkey.Taken"] = ("✗ The shortcut {0} is already used by another application (or the system). Choose a different one: click the field and press a new combination.",
            "✗ Zkratku {0} už používá jiná aplikace (nebo systém). Vyber jinou: klikni do pole a stiskni novou kombinaci."),
        ["FR.Done"] = ("Done", "Hotovo"),

        // Model download window
        ["MD.Title"] = ("BlahBlah3000 – Whisper model", "BlahBlah3000 – Whisper model"),
        ["MD.Downloading.Heading"] = ("Downloading the Whisper model", "Stahuje se Whisper model"),
        ["MD.Downloading.Body"] = ("{0:F0} MB of {1:F0} MB", "{0:F0} MB z {1:F0} MB"),
        ["MD.Missing.Heading"] = ("The Whisper model is missing", "Chybí Whisper model"),
        ["MD.Missing.Body"] = ("The application needs a speech recognition model ({0}). It is about {1:F0} MB and will be downloaded from huggingface.co to the folder {2}. " +
            "Dictation does not work without it.",
            "Aplikace potřebuje model rozpoznávání řeči ({0}). Má asi {1:F0} MB a stáhne se z huggingface.co do složky {2}. Bez něj diktování nefunguje."),
        ["MD.Download"] = ("Download ({0:F0} MB)", "Stáhnout ({0:F0} MB)"),
        ["MD.NotNow"] = ("Not now", "Teď ne"),
        ["MD.Cancel"] = ("Cancel", "Přerušit"),

        // Add replacement window
        ["AR.Title"] = ("BlahBlah3000 – new replacement", "BlahBlah3000 – nová náhrada"),
        ["AR.Regex"] = ("Regular expression", "Regulární výraz"),
        ["AR.Add"] = ("Add", "Přidat"),

        // Transcriber and model store
        ["LT.Downloading"] = ("The Whisper model is being downloaded.", "Whisper model se právě stahuje."),
        ["LT.DownloadFailed"] = ("Download failed: {0}", "Stahování selhalo: {0}"),
        ["LT.Missing"] = ("The Whisper model is missing. Download it from the tray icon menu.", "Chybí Whisper model. Stáhni ho v nabídce ikony v liště."),
        ["LT.BadSize"] = ("The downloaded model has an unexpected size.", "Stažený model má jinou velikost, než se čekalo."),

        ["Mic.Default"] = ("Windows default microphone (automatic)", "Výchozí mikrofon Windows (automaticky)"),

        // Transcription languages
        ["Lang.Auto"] = ("Automatic (several languages)", "Automaticky (více jazyků)"),
    };
}
