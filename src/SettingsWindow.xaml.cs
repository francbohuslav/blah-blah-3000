using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using BlahBlah3000.Whisper;

namespace BlahBlah3000;

public partial class SettingsWindow : Window
{
    private readonly Settings _settings;
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
        PttDelaySlider.Value = Math.Clamp(settings.PushToTalkDelayMs, PttDelaySlider.Minimum, PttDelaySlider.Maximum);
        UpdateLabels();

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
        if (ThresholdLabel == null || PauseLabel == null || PttDelayLabel == null) return;
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

    private void OnSave(object sender, RoutedEventArgs e)
    {
        AdvancedResult.Text = "";
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
            return;
        }

        if (HotkeyBox.Text.Length > 0 && !string.Equals(HotkeyBox.Text, _settings.Hotkey, StringComparison.OrdinalIgnoreCase)
            && !App.IsHotkeyAvailable(HotkeyBox.Text))
        {
            TestResult.Foreground = Brushes.Firebrick;
            TestResult.Text = Loc.T("Set.HotkeyTaken", HotkeyBox.Text);
            return;
        }

        // A row being edited in the table must be committed, otherwise it would not be saved.
        RulesGrid.CommitEdit(DataGridEditingUnit.Row, true);
        var badRule = _rules.FirstOrDefault(r => r.IsRegex && r.Pattern.Length > 0 && Rules.RuleEngine.TryCreateRegex(r.Pattern) == null);
        if (badRule != null)
        {
            TestResult.Foreground = Brushes.Firebrick;
            TestResult.Text = Loc.T("Set.RegexInvalid", badRule.Pattern);
            return;
        }

        _settings.Microphone = (MicCombo.SelectedItem as Audio.MicOption)?.Name;
        _settings.Hotkey = HotkeyBox.Text;
        _settings.PushToTalkEnabled = PttCheck.IsChecked == true;
        _settings.PushToTalkKeys = PttKeysCombo.SelectedValue as string ?? "Ctrl+Shift";
        _settings.PushToTalkDelayMs = (int)PttDelaySlider.Value;
        _settings.PushToTalkLatch = PttLatchCheck.IsChecked == true;
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
