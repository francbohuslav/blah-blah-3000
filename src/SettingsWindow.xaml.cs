using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using BlahBlah3000.Llm;
using BlahBlah3000.Whisper;

namespace BlahBlah3000;

public partial class SettingsWindow : Window
{
    private readonly Settings _settings;
    private readonly bool _initializing = true;
    private readonly Audio.MicRecorder _recorder = new();
    private readonly ObservableCollection<ReplacementRule> _rules;

    internal SettingsWindow(Settings settings)
    {
        InitializeComponent();
        _settings = settings;

        UiLanguageCombo.ItemsSource = new[] { new LanguageOption(Loc.Auto, Loc.T("Set.UiLanguage.Auto")) }
            .Concat(Loc.Available.Select(l => new LanguageOption(l.Code, l.Name))).ToList();
        UiLanguageCombo.SelectedValue = Loc.Available.Any(l => l.Code == settings.UiLanguage) ? settings.UiLanguage : Loc.Auto;
        LanguageCombo.ItemsSource = Languages.All;
        LanguageCombo.SelectedValue = Languages.All.Any(l => l.Code == settings.Language) ? settings.Language : "auto";
        ShowModelState();
        HotkeyBox.Text = settings.Hotkey;
        PttCheck.IsChecked = settings.PushToTalkEnabled;
        PttKeysCombo.ItemsSource = new LanguageOption[]
        {
            new("Ctrl+Shift", Loc.T("Set.Ptt.Keys.CtrlShift")),
            new("LeftCtrl", Loc.T("Set.Ptt.Keys.LeftCtrl")),
            new("LeftShift", Loc.T("Set.Ptt.Keys.LeftShift")),
            new("RightCtrl", Loc.T("Set.Ptt.Keys.RightCtrl")),
            new("RightShift", Loc.T("Set.Ptt.Keys.RightShift")),
        };
        PttKeysCombo.SelectedValue = settings.PushToTalkKeys;
        if (PttKeysCombo.SelectedItem == null) PttKeysCombo.SelectedIndex = 0;
        AutostartCheck.IsChecked = settings.Autostart;
        UpdatesCheck.IsChecked = settings.CheckForUpdates;
        ApiCheck.IsChecked = settings.ApiEnabled;
        PortBox.Text = settings.ApiPort.ToString();
        ListenAllCheck.IsChecked = settings.ApiListenAll;
        UnloadBox.Text = settings.UnloadAfterMinutes.ToString();
        DebugCheck.IsChecked = settings.DebugRecording;
        PromptBox.Text = settings.Prompt;
        _rules = new ObservableCollection<ReplacementRule>(
            settings.Replacements.Select(r => new ReplacementRule { Pattern = r.Pattern, Replacement = r.Replacement, IsRegex = r.IsRegex }));
        RulesGrid.ItemsSource = _rules;
        ThresholdSlider.Value = settings.SilenceThreshold;
        PauseSlider.Value = settings.PauseMs;
        PttLatchCheck.IsChecked = settings.PushToTalkLatch;
        PttSoundsCheck.IsChecked = settings.PushToTalkSounds;
        PttDuckSlider.Value = Math.Clamp(settings.PushToTalkDuckPercent, PttDuckSlider.Minimum, PttDuckSlider.Maximum);
        PttAutoStopSlider.Value = Math.Clamp(settings.PushToTalkAutoStopSeconds, PttAutoStopSlider.Minimum, PttAutoStopSlider.Maximum);
        PttDelaySlider.Value = Math.Clamp(settings.PushToTalkDelayMs, PttDelaySlider.Minimum, PttDelaySlider.Maximum);
        UpdateLabels();

        LlmCheck.IsChecked = settings.LlmEnabled;
        var presets = LlmClient.Presets.Select(p => p.Id == "custom" ? p with { Label = Loc.T("Set.Llm.Provider.Custom") } : p).ToList();
        LlmProviderCombo.ItemsSource = presets;
        LlmProviderCombo.SelectedItem = presets.FirstOrDefault(p => p.Url.Length > 0
            && string.Equals(p.Url, settings.LlmBaseUrl.Trim().TrimEnd('/'), StringComparison.OrdinalIgnoreCase)) ?? presets[^1];
        LlmUrlBox.Text = settings.LlmBaseUrl;
        LlmModelBox.Text = settings.LlmModel;
        LlmKeyBox.Password = settings.LlmApiKey;
        LlmPromptBox.Text = LlmClient.EffectivePrompt(settings);
        LlmAutoCheck.IsChecked = settings.LlmAutoFix;
        _initializing = false;

        _recorder.Data += OnMicData;
        var options = Audio.MicRecorder.ListOptions();
        MicCombo.ItemsSource = options;
        MicCombo.SelectedItem = options.FirstOrDefault(o => o.Name != null && o.Name == settings.Microphone) ?? options[0];
        Closed += (_, _) => _recorder.Dispose();
    }

    /// <summary>The settings window listens to the microphone itself for tuning the threshold, so it works even when opened from the tray icon.</summary>
    private void OnMicChanged(object sender, SelectionChangedEventArgs e)
    {
        if (MicCombo.SelectedItem is not Audio.MicOption option) return;
        try
        {
            _recorder.Start(option.Number);
        }
        catch (Exception ex)
        {
            Log.Write($"Cannot open the microphone for settings: {ex.Message}");
        }
    }

    private void OnMicData(byte[] chunk)
    {
        var sum = 0.0;
        var count = chunk.Length / 2;
        for (var i = 0; i < count; i++)
        {
            var sample = BitConverter.ToInt16(chunk, i * 2);
            sum += (double)sample * sample;
        }
        var rms = count > 0 ? Math.Sqrt(sum / count) : 0;
        Dispatcher.BeginInvoke(() => LevelBar.Value = Math.Min(rms, LevelBar.Maximum));
    }


    private void UpdateLabels()
    {
        // The sliders raise ValueChanged while the window is still being built, before all labels exist.
        if (ThresholdLabel == null || PauseLabel == null || PttDelayLabel == null || PttAutoStopLabel == null || PttDuckLabel == null) return;
        PttDuckLabel.Text = Loc.T(PttDuckSlider.Value switch { 0 => "Set.Ptt.Duck.Mute", 100 => "Set.Ptt.Duck.Off", _ => "Set.Ptt.Duck" },
            PttDuckSlider.Value);
        PttAutoStopLabel.Text = Loc.T(PttAutoStopSlider.Value == 0 ? "Set.Ptt.AutoStop.Off" : "Set.Ptt.AutoStop", PttAutoStopSlider.Value);
        ThresholdLabel.Text = Loc.T("Set.Threshold", ThresholdSlider.Value);
        PauseLabel.Text = Loc.T("Set.Pause", PauseSlider.Value);
        PttDelayLabel.Text = Loc.T("Set.Ptt.Delay", PttDelaySlider.Value);
    }

    private void OnSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => UpdateLabels();

    private void ShowModelState()
    {
        var transcriber = App.Transcriber;
        var ready = transcriber.State == TranscriberState.Ready;
        TestResult.Foreground = ready ? Brushes.SeaGreen : transcriber.State == TranscriberState.Error ? Brushes.Firebrick : Brushes.Gray;
        TestResult.Text = ready
            ? Loc.T("Set.ModelLoaded", ModelStore.FileName)
            : transcriber.State == TranscriberState.Error
                ? Loc.T("Set.ModelFailed", transcriber.Detail)
                : Loc.T("Set.ModelPreparing", transcriber.State, transcriber.Detail);
    }

    private void OnHotkeyKeyDown(object sender, KeyEventArgs e)
    {
        e.Handled = true;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        var text = Native.GlobalHotkey.Format(key, Keyboard.Modifiers);
        if (text != null) HotkeyBox.Text = text;
    }

    private void OnClearHotkey(object sender, RoutedEventArgs e) => HotkeyBox.Text = "";

    /// <summary>Choosing a service fills in its address and a model; "other" leaves the fields as they are.</summary>
    private void OnLlmProviderChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initializing || LlmProviderCombo.SelectedItem is not LlmPreset { Id: not "custom" } preset) return;
        LlmUrlBox.Text = preset.Url;
        LlmModelBox.Text = preset.Model;
    }

    private void OnLlmPromptReset(object sender, RoutedEventArgs e) => LlmPromptBox.Text = LlmClient.DefaultPrompt;

    /// <summary>Sends a short sample with the values from the form (not yet saved) and shows the result or the error.</summary>
    private async void OnLlmTest(object sender, RoutedEventArgs e)
    {
        LlmTestButton.IsEnabled = false;
        LlmResult.Foreground = Brushes.Gray;
        LlmResult.Text = Loc.T("Llm.Testing");
        try
        {
            var started = System.Diagnostics.Stopwatch.StartNew();
            var reply = await LlmClient.CompleteAsync(LlmUrlBox.Text, LlmKeyBox.Password, LlmModelBox.Text.Trim(),
                LlmClient.IsDefaultPrompt(LlmPromptBox.Text) ? LlmClient.DefaultPrompt : LlmPromptBox.Text.Trim(),
                Loc.T("Llm.TestSample"), CancellationToken.None);
            LlmResult.Foreground = Brushes.SeaGreen;
            LlmResult.Text = Loc.T("Llm.TestOk", started.Elapsed.TotalSeconds, reply);
        }
        catch (LlmException ex)
        {
            LlmResult.Foreground = Brushes.Firebrick;
            LlmResult.Text = "✗ " + ex.Message + "\n" + Loc.T("Llm.SeeLog", Log.FilePath);
        }
        finally
        {
            LlmTestButton.IsEnabled = true;
        }
    }

    private static string HostOf(string url) => Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri) ? uri.Host : "";

    private void OnSave(object sender, RoutedEventArgs e)
    {
        AdvancedResult.Text = "";
        ActivationResult.Text = "";
        ReplacementsResult.Text = "";
        if (!int.TryParse(PortBox.Text.Trim(), out var port) || port is < 1 or > 65535)
        {
            // The port lives on the Advanced tab, so the message is shown there and the tab is brought forward.
            AdvancedResult.Foreground = Brushes.Firebrick;
            AdvancedResult.Text = Loc.T("Set.PortInvalid");
            SettingsTabs.SelectedItem = AdvancedTab;
            return;
        }
        if (!int.TryParse(UnloadBox.Text.Trim(), out var unload) || unload < 0)
        {
            TestResult.Foreground = Brushes.Firebrick;
            TestResult.Text = Loc.T("Set.UnloadInvalid");
            SettingsTabs.SelectedIndex = 0;
            return;
        }

        if (HotkeyBox.Text.Length > 0 && !string.Equals(HotkeyBox.Text, _settings.Hotkey, StringComparison.OrdinalIgnoreCase)
            && !App.IsHotkeyAvailable(HotkeyBox.Text))
        {
            // The shortcut lives on the Activation tab, so the message is shown there and the tab is brought forward.
            ActivationResult.Foreground = Brushes.Firebrick;
            ActivationResult.Text = Loc.T("Set.HotkeyTaken", HotkeyBox.Text);
            SettingsTabs.SelectedItem = ActivationTab;
            return;
        }

        // A row being edited in the table must be committed, otherwise it would not be saved.
        RulesGrid.CommitEdit(DataGridEditingUnit.Row, true);
        var badRule = _rules.FirstOrDefault(r => r.IsRegex && r.Pattern.Length > 0 && Rules.RuleEngine.TryCreateRegex(r.Pattern) == null);
        if (badRule != null)
        {
            // The table lives on the Replacements tab, so the message is shown there and the tab is brought forward.
            ReplacementsResult.Foreground = Brushes.Firebrick;
            ReplacementsResult.Text = Loc.T("Set.RegexInvalid", badRule.Pattern);
            SettingsTabs.SelectedItem = ReplacementsTab;
            return;
        }

        var llmEnabled = LlmCheck.IsChecked == true;
        if (llmEnabled && !LlmClient.IsValidUrl(LlmUrlBox.Text))
        {
            LlmResult.Foreground = Brushes.Firebrick;
            LlmResult.Text = Loc.T("Set.Llm.UrlInvalid");
            SettingsTabs.SelectedItem = LlmTab;
            return;
        }
        // The text leaves the computer only after the user has agreed: when the correction is turned on or sent to another host.
        if (llmEnabled && !LlmClient.IsLocal(LlmUrlBox.Text)
            && (!_settings.LlmEnabled || !string.Equals(HostOf(LlmUrlBox.Text), HostOf(_settings.LlmBaseUrl), StringComparison.OrdinalIgnoreCase))
            && MessageBox.Show(this, Loc.T("Llm.Consent", HostOf(LlmUrlBox.Text)), Loc.T("Llm.ConsentTitle"),
                MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK)
        {
            SettingsTabs.SelectedItem = LlmTab;
            return;
        }

        _settings.LlmEnabled = llmEnabled;
        _settings.LlmBaseUrl = LlmUrlBox.Text.Trim();
        _settings.LlmModel = LlmModelBox.Text.Trim();
        _settings.LlmApiKey = LlmKeyBox.Password;
        _settings.LlmAutoFix = LlmAutoCheck.IsChecked == true;
        // The built-in text is not stored: it keeps following the interface language until the user changes it.
        _settings.LlmPrompt = LlmClient.IsDefaultPrompt(LlmPromptBox.Text) ? "" : LlmPromptBox.Text.Trim();
        _settings.Microphone = (MicCombo.SelectedItem as Audio.MicOption)?.Name;
        _settings.Hotkey = HotkeyBox.Text;
        _settings.PushToTalkEnabled = PttCheck.IsChecked == true;
        _settings.PushToTalkKeys = PttKeysCombo.SelectedValue as string ?? "Ctrl+Shift";
        _settings.PushToTalkDelayMs = (int)PttDelaySlider.Value;
        _settings.PushToTalkLatch = PttLatchCheck.IsChecked == true;
        _settings.PushToTalkSounds = PttSoundsCheck.IsChecked == true;
        _settings.PushToTalkDuckPercent = (int)PttDuckSlider.Value;
        _settings.PushToTalkAutoStopSeconds = (int)PttAutoStopSlider.Value;
        _settings.Autostart = AutostartCheck.IsChecked == true;
        _settings.CheckForUpdates = UpdatesCheck.IsChecked == true;
        _settings.ApiEnabled = ApiCheck.IsChecked == true;
        _settings.ApiPort = port;
        _settings.ApiListenAll = ListenAllCheck.IsChecked == true;
        _settings.UnloadAfterMinutes = unload;

        _settings.UiLanguage = UiLanguageCombo.SelectedValue as string ?? Loc.Auto;
        _settings.Language = LanguageCombo.SelectedValue as string ?? "auto";
        _settings.DebugRecording = DebugCheck.IsChecked == true;
        _settings.Prompt = PromptBox.Text.Trim();
        _settings.Replacements = _rules.Where(r => !string.IsNullOrEmpty(r.Pattern)).ToList();
        _settings.SilenceThreshold = ThresholdSlider.Value;
        _settings.PauseMs = (int)PauseSlider.Value;
        _settings.Save();
        DialogResult = true;
    }
}
