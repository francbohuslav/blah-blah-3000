using System.Windows;
using System.Windows.Threading;
using BlahBlah3000.Api;
using BlahBlah3000.Dictation;
using BlahBlah3000.Native;
using BlahBlah3000.Update;
using BlahBlah3000.Whisper;

namespace BlahBlah3000;

public partial class App : Application
{
    /// <summary>The only Whisper model in the process; shared by the dictation window and the REST API.</summary>
    internal static LocalTranscriber Transcriber { get; } = new();

    internal static Settings Settings { get; private set; } = new();

    private static SingleInstance? _instance;
    private static TrayIcon? _tray;
    private static GlobalHotkey? _hotkey;
    private static readonly ApiServer Api = new();
    private static DispatcherTimer? _idleTimer;
    private static MainWindow? _session;
    private static ModelDownloadWindow? _modelWindow;
    private static DispatcherTimer? _updateTimer;
    private static string? _apiError;
    private static bool _hotkeyFailed;
    private static KeyboardHook? _pushToTalkHook;
    private static PushToTalkSession? _pushToTalk;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        // The window to paste into must be remembered before anything of ours is shown.
        var previous = Win32.GetForegroundWindow();
        var trayOnly = e.Args.Contains("--tray", StringComparer.OrdinalIgnoreCase);

        _instance = SingleInstance.TryAcquire();
        if (_instance == null)
        {
            // The application is already running (in the tray); autostart does nothing, otherwise the running instance is told to start dictation.
            if (!trayOnly) SingleInstance.SignalRunning(previous);
            Shutdown();
            return;
        }

        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        LegacyMigration.Run();
        Log.Write("=== start ===");
        DispatcherUnhandledException += (_, args) => Log.Write($"Unhandled exception (UI): {args.Exception}");
        AppDomain.CurrentDomain.UnhandledException += (_, args) => Log.Write($"Unhandled exception: {args.ExceptionObject}");
        TaskScheduler.UnobservedTaskException += (_, args) => Log.Write($"Unhandled exception (task): {args.Exception}");

        Settings = Settings.Load();
        Loc.Init(Settings.UiLanguage);
        _tray = new TrayIcon();
        _tray.Dictate += () => StartSession(IntPtr.Zero);
        _tray.OpenSettings += OpenSettings;
        _tray.DownloadModel += ShowModelDownload;
        _tray.ToggleAutostart += () =>
        {
            Settings.Autostart = !Settings.Autostart;
            Settings.Save();
            ApplySettings();
        };
        _tray.Exit += () => Shutdown();

        _instance.Activated += handle => Dispatcher.BeginInvoke(() => StartSession(handle));
        _instance.StartListening();

        _hotkey = new GlobalHotkey();
        _hotkey.Pressed += () => StartSession(Win32.GetForegroundWindow());

        _pushToTalkHook = new KeyboardHook();
        _pushToTalkHook.Engaged += StartPushToTalk;
        // The hook callback must return quickly, so ending the recording is left to the dispatcher.
        _pushToTalkHook.Released += () => Dispatcher.BeginInvoke(StopPushToTalk);

        Transcriber.StateChanged += () => Dispatcher.BeginInvoke(RefreshTray);
        _idleTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
        _idleTimer.Tick += (_, _) =>
        {
            if (Settings.UnloadAfterMinutes > 0 && _session == null)
            {
                Transcriber.UnloadIfIdle(TimeSpan.FromMinutes(Settings.UnloadAfterMinutes));
            }
        };
        _idleTimer.Start();

        Transcriber.Start();
        ApplySettings();
        StartUpdateChecks();
        if (!Settings.FirstRunDone)
        {
            // The wizard also replaces the model download offer; closing it with the X shows it again at the next start.
            if (new FirstRunWindow(Settings).ShowDialog() == true) ApplySettings();
        }
        else if (Transcriber.State == TranscriberState.ModelMissing) ShowModelDownload();
        if (!trayOnly) StartSession(previous);
    }

    /// <summary>A newer release found on GitHub, or null; the dictation window shows it at the bottom.</summary>
    internal static UpdateInfo? AvailableUpdate { get; private set; }

    internal static event Action? UpdateChanged;

    /// <summary>First check shortly after startup (so it does not compete with loading the model), then once a day.</summary>
    private static void StartUpdateChecks()
    {
        _updateTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _updateTimer.Tick += async (_, _) =>
        {
            _updateTimer.Interval = TimeSpan.FromHours(24);
            if (!Settings.CheckForUpdates) return;
            var update = await UpdateChecker.CheckAsync();
            if (update?.Version == AvailableUpdate?.Version) return;
            AvailableUpdate = update;
            UpdateChanged?.Invoke();
        };
        _updateTimer.Start();
    }

    /// <summary>Asks for confirmation, then closes the application and opens the release notes in the browser.</summary>
    internal static void InstallUpdate(Window owner)
    {
        var update = AvailableUpdate;
        if (update == null) return;
        var answer = MessageBox.Show(owner, Loc.T("Update.Confirm", update.Tag), Loc.T("Update.ConfirmTitle"),
            MessageBoxButton.OKCancel, MessageBoxImage.Information);
        if (answer != MessageBoxResult.OK) return;
        Log.Write($"Update: opening {update.Url} and exiting");
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(update.Url) { UseShellExecute = true });
        Current.Shutdown();
    }

    /// <summary>Opens the dictation window. <paramref name="target"/> is the window to paste into; Zero = the result goes to the clipboard only.</summary>
    internal static void StartSession(IntPtr target)
    {
        if (_session != null)
        {
            _session.Activate();
            return;
        }
        if (!Win32.IsPasteTarget(target)) target = IntPtr.Zero;
        Log.Write($"Dictation, paste target: {(target == IntPtr.Zero ? "clipboard only" : $"0x{target:X}")}");
        var window = new MainWindow(target);
        window.Closed += (_, _) => _session = null;
        _session = window;
        window.Show();
    }

    internal static void OpenSettings()
    {
        // From the tray the settings open without an owner; from the dictation window MainWindow opens them with an owner.
        if (_session != null)
        {
            _session.OpenSettings();
            return;
        }
        var dialog = new SettingsWindow(Settings) { WindowStartupLocation = WindowStartupLocation.CenterScreen, ShowInTaskbar = true };
        if (dialog.ShowDialog() == true) ApplySettings();
    }

    internal static void ShowModelDownload()
    {
        if (_modelWindow != null)
        {
            _modelWindow.Activate();
            return;
        }
        _modelWindow = new ModelDownloadWindow();
        _modelWindow.Closed += (_, _) => _modelWindow = null;
        _modelWindow.Show();
    }

    /// <summary>Applies the settings that live in the whole application: language, shortcut, autostart, API and the icon state.</summary>
    internal static void ApplySettings()
    {
        Loc.Init(Settings.UiLanguage);
        _tray?.ApplyLanguage();
        Autostart.Set(Settings.Autostart);

        _hotkeyFailed = _hotkey != null && !_hotkey.Register(Settings.Hotkey);
        if (_hotkeyFailed)
        {
            Log.Write($"Cannot register the shortcut {Settings.Hotkey} (invalid or used by another application)");
            _tray?.Notify(Loc.T("Tray.HotkeyFailed", Settings.Hotkey), warning: true);
        }

        if (_pushToTalkHook != null)
        {
            if (!Settings.PushToTalkEnabled) _pushToTalkHook.Disable();
            else if (!_pushToTalkHook.Enable(Settings.PushToTalkKeys, TimeSpan.FromMilliseconds(Settings.PushToTalkDelayMs)))
            {
                Log.Write($"Cannot enable push to talk ({Settings.PushToTalkKeys})");
                _tray?.Notify(Loc.T("Tray.PttHookFailed"), warning: true);
            }
        }

        _ = ApplyApiAsync();
        RefreshTray();
    }

    /// <summary>The push-to-talk keys were held: records and types the transcript into the active window until they are released.</summary>
    private static void StartPushToTalk()
    {
        // The dictation window owns the microphone and the keyboard focus while it is open.
        if (_pushToTalk != null || _session != null) return;
        if (Transcriber.State is TranscriberState.ModelMissing or TranscriberState.Downloading or TranscriberState.Error)
        {
            _tray?.Notify(Loc.T("Tray.PttModelUnavailable"), warning: true);
            return;
        }
        var session = new PushToTalkSession(Settings, message => Current.Dispatcher.BeginInvoke(() => _tray?.Notify(message, warning: true)));
        try
        {
            session.Start();
        }
        catch (Exception ex)
        {
            Log.Write($"Push to talk failed to start: {ex}");
            session.Dispose();
            _tray?.Notify(Loc.T("Main.MicOpenFailed", ex.Message), warning: true);
            return;
        }
        _pushToTalk = session;
        _tray?.SetRecording(true);
    }

    private static async void StopPushToTalk()
    {
        var session = _pushToTalk;
        if (session == null) return;
        _pushToTalk = null;
        try
        {
            await session.StopAsync();
        }
        catch (Exception ex)
        {
            Log.Write($"Push to talk failed to finish: {ex}");
            session.Dispose();
        }
        // The icon stays red until the last sentence is typed, unless a new recording has started meanwhile.
        if (_pushToTalk == null) _tray?.SetRecording(false);
    }

    private static async Task ApplyApiAsync()
    {
        if (!Settings.ApiEnabled)
        {
            _apiError = null;
            await Api.StopAsync();
        }
        else if (!Api.Running || Api.Port != Settings.ApiPort || Api.ListenAll != Settings.ApiListenAll)
        {
            _apiError = await Api.StartAsync(Settings.ApiPort, Settings.ApiListenAll, Transcriber, () => Settings.Language);
            if (_apiError != null)
            {
                _tray?.Notify(Loc.T("Tray.ApiFailed", Settings.ApiPort, _apiError), warning: true);
            }
        }
        await Current.Dispatcher.InvokeAsync(RefreshTray);
    }

    private static void RefreshTray()
    {
        if (_tray == null) return;
        var text = Transcriber.State switch
        {
            TranscriberState.Ready => Loc.T("Tray.State.Ready"),
            TranscriberState.Idle => Loc.T("Tray.State.Idle"),
            TranscriberState.Loading => Loc.T("Tray.State.Loading"),
            TranscriberState.Downloading => Loc.T("Tray.State.Downloading", (Transcriber.Progress * 100).ToString("F0")),
            TranscriberState.ModelMissing => Loc.T("Tray.State.Missing"),
            _ => Loc.T("Tray.State.Error"),
        };
        if (_apiError != null) text += Loc.T("Tray.ApiDown");
        _tray.Refresh(text, Transcriber.State == TranscriberState.ModelMissing, Settings.Autostart, _hotkeyFailed ? "" : Settings.Hotkey);
    }

    /// <summary>Is the shortcut valid and free? The shortcut currently registered by this application counts as free.</summary>
    internal static bool IsHotkeyAvailable(string text)
    {
        if (!GlobalHotkey.TryParse(text, out _, out _)) return false;
        if (!_hotkeyFailed && string.Equals(text, Settings.Hotkey, StringComparison.OrdinalIgnoreCase)) return true;
        using var probe = new GlobalHotkey();
        return probe.Register(text);
    }

    internal static void NotifyTray(string text) => _tray?.Notify(text);

    protected override void OnExit(ExitEventArgs e)
    {
        _idleTimer?.Stop();
        _updateTimer?.Stop();
        _hotkey?.Dispose();
        _pushToTalkHook?.Dispose();
        _pushToTalk?.Dispose();
        _tray?.Dispose();
        // Off the UI thread, so the wait does not block the return to the synchronization context.
        Task.Run(() => Api.StopAsync()).Wait(TimeSpan.FromSeconds(3));
        _instance?.Dispose();
        if (_instance != null) Log.Write("=== end ===");
        base.OnExit(e);
    }
}
