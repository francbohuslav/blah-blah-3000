using System.Threading.Channels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using BlahBlah3000.Audio;
using BlahBlah3000.Native;
using BlahBlah3000.Rules;
using BlahBlah3000.Update;
using BlahBlah3000.Whisper;

namespace BlahBlah3000;

public partial class MainWindow : Window
{
    private readonly IntPtr _previousWindow;
    private readonly Settings _settings = App.Settings;
    private readonly RuleEngine _rules;
    private readonly MicRecorder _recorder = new();
    private readonly SentenceSegmenter _segmenter = new();
    private DebugDump _debug;
    private readonly Channel<byte[]> _sentences = Channel.CreateUnbounded<byte[]>();
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _consumer;
    private readonly System.Windows.Threading.DispatcherTimer _watchdog = new() { Interval = TimeSpan.FromSeconds(3) };
    private bool _recording;
    private bool _finishing;
    private string? _error;
    private bool _errorIsMic;
    private int _pending;
    private bool _whisperUsed;
    private bool _paused;
    private bool _fixing;
    private CancellationTokenSource? _fixCts;
    private string? _original;
    // The text for which Enter pastes instead of correcting again (the corrected text, the original after "back", or text whose correction failed).
    private string _lastFixedText = "";

    internal MainWindow(IntPtr previousWindow)
    {
        InitializeComponent();
        _previousWindow = previousWindow;
        AppVersion.Text = $" {UpdateChecker.Current}";
        ApplyLlmSettings();
        _rules = new RuleEngine(_settings);
        _debug = new DebugDump(_settings.DebugRecording);
        _segmenter.RmsThreshold = _settings.SilenceThreshold;
        _segmenter.PauseMs = _settings.PauseMs;

        _segmenter.Sentence += pcm =>
        {
            _debug.LogSentence(pcm);
            _pending++;
            _whisperUsed = true;
            SetWhisperInfo(Loc.T("Main.Transcribing"), Color.FromRgb(0xfd, 0xd6, 0x63));
            _sentences.Writer.TryWrite(pcm);
        };
        _segmenter.Level += level => Level.Value = level;
        // NAudio raises the event on its own thread; the controls and the segmenter belong to the UI thread.
        _recorder.Data += chunk => Dispatcher.BeginInvoke(() => OnAudio(chunk));
        _consumer = Task.Run(ConsumeAsync);

        _watchdog.Tick += (_, _) =>
        {
            _watchdog.Stop();
            if (_recording) return;
            Log.Write("No audio data arrived within 3 s");
            if (_error == null) ShowError(Loc.T("Main.NoAudio"), mic: true);
        };

        Loaded += (_, _) =>
        {
            var handle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            var active = Win32.ForceForeground(handle);
            Activate();
            Transcript.Focus();
            Log.Write($"Window in foreground: {active}");
            Log.Write($"Whisper: built-in, language {_settings.Language}, prompt: \"{_rules.Prompt}\"");
            StartRecording();
            ShowTranscriberState();
        };
        App.Transcriber.StateChanged += OnTranscriberStateChanged;
        App.UpdateChanged += OnUpdateChanged;
        ShowUpdate();
    }

    private void OnUpdateChanged() => Dispatcher.BeginInvoke(ShowUpdate);

    /// <summary>A newer release is announced at the bottom of the window; clicking it offers to close the application and open the release notes.</summary>
    private void ShowUpdate()
    {
        var update = App.AvailableUpdate;
        UpdateLink.Visibility = update == null ? Visibility.Collapsed : Visibility.Visible;
        if (update != null) UpdateLink.Text = Loc.T("Update.Available", update.Tag);
    }

    private void OnUpdateClick(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        App.InstallUpdate(this);
    }

    /// <summary>The borderless window is moved by dragging the top bar with the status.</summary>
    private void OnHeaderMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left) return;
        DragMove();
        Transcript.Focus();
    }

    private string SelectedText() =>
        string.Join(" ", Transcript.Selection.Text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)).Trim();

    private void OnTranscriptSelectionChanged(object sender, RoutedEventArgs e)
    {
        AddRuleButton.IsEnabled = SelectedText().Length > 0;
    }

    /// <summary>The selected text can be saved as a new replacement with one click and by filling in the target text.</summary>
    private void OnAddRuleClick(object sender, RoutedEventArgs e)
    {
        var dialog = new AddReplacementWindow(SelectedText()) { Owner = this };
        if (dialog.ShowDialog() == true)
        {
            var rule = dialog.Rule;
            var existing = _settings.Replacements.FirstOrDefault(
                r => string.Equals(r.Pattern, rule.Pattern, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                existing.Replacement = rule.Replacement;
                existing.IsRegex = rule.IsRegex;
            }
            else _settings.Replacements.Add(rule);
            _settings.Save();
            Log.Write($"Replacement added: \"{rule.Pattern}\" → \"{rule.Replacement}\"");
        }
        Activate();
        Transcript.Focus();
    }

    private void OnSettingsClick(object sender, RoutedEventArgs e) => OpenSettings();

    internal void OpenSettings()
    {
        var previousDebug = _settings.DebugRecording;
        var previousMic = _settings.Microphone;
        var dialog = new SettingsWindow(_settings) { Owner = this };
        var saved = dialog.ShowDialog() == true;
        if (saved)
        {
            App.ApplySettings();
            if (_settings.Microphone != previousMic)
            {
                // The sentence in progress from the old microphone is finished; the new microphone starts with a clean state.
                _segmenter.Flush();
                StartRecording();
            }
            ApplyLlmSettings();
            _segmenter.RmsThreshold = _settings.SilenceThreshold;
            _segmenter.PauseMs = _settings.PauseMs;
            Log.Write($"Settings saved (threshold {_settings.SilenceThreshold:F0}, pause {_settings.PauseMs} ms, " +
                      $"{_settings.Replacements.Count} replacements)");
            if (_settings.DebugRecording != previousDebug)
            {
                // Enabled debugging starts with clean files from this moment; disabling closes the files.
                _debug.Dispose();
                _debug = new DebugDump(_settings.DebugRecording);
                Log.Write($"Debugging {(_settings.DebugRecording ? "enabled" : "disabled")}");
            }
        }
        // After the dialog closes, focus returns to the text so Enter and Esc keep working.
        Activate();
        Transcript.Focus();
    }

    private void StartRecording()
    {
        var option = MicRecorder.Resolve(_settings.Microphone);
        _recording = false;
        if (_errorIsMic) _error = null;
        Status.Text = Loc.T("Main.OpeningMic");
        Status.Foreground = new SolidColorBrush(Color.FromRgb(0x9a, 0xa0, 0xa6));
        try
        {
            _recorder.Start(option.Number);
            Log.Write($"Recording started: {option.Label} (device {option.Number})");
        }
        catch (Exception ex)
        {
            Log.Write($"StartRecording failed: {ex}");
            ShowError(Loc.T("Main.MicOpenFailed", ex.Message), mic: true);
        }
        _watchdog.Stop();
        _watchdog.Start();
    }

    private void OnAudio(byte[] chunk)
    {
        if (!_recording)
        {
            _recording = true;
            Log.Write($"First audio block: {chunk.Length} B");
            if (_error == null)
            {
                Status.Text = Loc.T("Main.Recording");
                Status.Foreground = new SolidColorBrush(Color.FromRgb(0xf2, 0x8b, 0x82));
            }
        }
        // While the AI corrects the text, nothing new is dictated: it could not be merged with the corrected text.
        if (_finishing || _paused) return;
        _debug.AddChunk(chunk);
        _segmenter.Feed(chunk);
    }

    private async Task ConsumeAsync()
    {
        await foreach (var pcm in _sentences.Reader.ReadAllAsync())
        {
            var started = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                var text = await App.Transcriber.TranscribeAsync(pcm, _rules.Prompt, _settings.Language, _cts.Token);
                Log.Write($"Transcript ({started.ElapsedMilliseconds} ms): \"{text}\"");
                await Dispatcher.InvokeAsync(() =>
                {
                    if (text.Length > 0)
                    {
                        var segments = _rules.ApplySegments(text);
                        _debug.LogTranscript(text, string.Concat(segments.Select(s => s.Text)));
                        InsertSentence(segments);
                    }
                    OnRequestDone(Loc.T("Main.Done", started.Elapsed.TotalSeconds), Color.FromRgb(0x81, 0xc9, 0x95));
                });
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    ShowError(ex.Message);
                    OnRequestDone(Loc.T("Main.Failed"), Color.FromRgb(0xfd, 0xd6, 0x63));
                });
            }
        }
    }

    private void SetWhisperInfo(string text, Color color)
    {
        WhisperInfo.Text = text;
        WhisperInfo.Foreground = new SolidColorBrush(color);
    }

    /// <summary>A finished Whisper request; the indicator switches only when nothing else is running.</summary>
    private void OnRequestDone(string text, Color color)
    {
        _pending--;
        if (_pending == 0) SetWhisperInfo(text, color);
    }

    private void OnTranscriberStateChanged() => Dispatcher.BeginInvoke(ShowTranscriberState);

    /// <summary>Clicking the status indicator for a missing model offers the download (it is never downloaded on its own).</summary>
    private void OnWhisperInfoClick(object sender, MouseButtonEventArgs e)
    {
        if (App.Transcriber.State != TranscriberState.ModelMissing) return;
        e.Handled = true;
        App.ShowModelDownload();
    }

    /// <summary>Model status indicator; if a transcription is already running, its status takes precedence.</summary>
    private void ShowTranscriberState()
    {
        if (_pending > 0 || _whisperUsed) return;
        var transcriber = App.Transcriber;
        var gray = Color.FromRgb(0x9a, 0xa0, 0xa6);
        var yellow = Color.FromRgb(0xfd, 0xd6, 0x63);
        switch (transcriber.State)
        {
            case TranscriberState.Ready:
                // A working built-in model has nothing to report; the indicator appears only while loading, transcribing or on a problem.
                SetWhisperInfo("", gray);
                break;
            case TranscriberState.Downloading:
                SetWhisperInfo(Loc.T("Main.Downloading", transcriber.Detail), yellow);
                break;
            case TranscriberState.ModelMissing:
                SetWhisperInfo(Loc.T("Main.ModelMissing"), yellow);
                WhisperInfo.Cursor = Cursors.Hand;
                break;
            case TranscriberState.Error:
                SetWhisperInfo(Loc.T("Main.ModelError"), yellow);
                break;
            default:
                SetWhisperInfo(Loc.T("Main.ModelLoading"), gray);
                break;
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        App.Transcriber.StateChanged -= OnTranscriberStateChanged;
        App.UpdateChanged -= OnUpdateChanged;
        _debug.Dispose();
        base.OnClosed(e);
    }

    /// <summary>Inserts a sentence at the caret (the user can move it by clicking) and puts the caret after it.</summary>
    private void InsertSentence(List<RuleEngine.Segment> sentence)
    {
        var caret = Transcript.CaretPosition.GetInsertionPosition(LogicalDirection.Forward);
        var textBefore = new TextRange(Transcript.Document.ContentStart, caret).Text;
        var textAfter = new TextRange(caret, Transcript.Document.ContentEnd).Text;
        var before = textBefore.Length > 0 && !char.IsWhiteSpace(textBefore[^1]);
        var after = textAfter.Length > 0 && !char.IsWhiteSpace(textAfter[0]);

        var pointer = caret;
        if (before) pointer = new Run(" ", pointer).ElementEnd;
        foreach (var segment in sentence)
        {
            if (segment.Text.Length == 0) continue;
            var run = new Run(segment.Text, pointer);
            if (segment.Original != null)
            {
                run.Background = ReplacedBrush;
                run.ToolTip = Loc.T("Main.Original", segment.Original);
            }
            pointer = run.ElementEnd;
        }
        var end = pointer.GetInsertionPosition(LogicalDirection.Backward);
        if (after) new Run(" ", pointer);
        Transcript.CaretPosition = end;
    }

    private static readonly Brush ReplacedBrush = new SolidColorBrush(Color.FromRgb(0x6b, 0x5a, 0x1f));

    private void ShowError(string message, bool mic = false)
    {
        Log.Write($"Error: {message}");
        _error = message;
        _errorIsMic = mic;
        Status.Text = message;
        Status.Foreground = new SolidColorBrush(Color.FromRgb(0xfd, 0xd6, 0x63));
    }

    private async void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            // While the AI is working, Esc only cancels the correction.
            if (_fixing) _fixCts?.Cancel();
            else Cancel();
        }
        else if (e.Key == Key.K && Keyboard.Modifiers == ModifierKeys.Control && _settings.LlmEnabled)
        {
            e.Handled = true;
            await FixTextAsync();
        }
        else if (e.Key == Key.Enter && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
        {
            // Shift+Enter breaks the line at the caret; plain Enter submits the text.
            e.Handled = true;
            if (!Transcript.IsKeyboardFocused) return;
            Transcript.Selection.Text = "";
            Transcript.CaretPosition = Transcript.CaretPosition.InsertLineBreak();
        }
        else if (e.Key == Key.Enter && !_finishing)
        {
            e.Handled = true;
            if (_fixing) return;
            // With automatic correction the first Enter corrects and shows the result; the next one pastes it.
            if (_settings.LlmEnabled && _settings.LlmAutoFix && TranscriptText().Length > 0 && TranscriptText() != _lastFixedText)
            {
                await FixTextAsync();
                return;
            }
            await FinishAsync();
        }
    }

    private string TranscriptText() => new TextRange(Transcript.Document.ContentStart, Transcript.Document.ContentEnd).Text.Trim();

    private void SetTranscriptText(string text)
    {
        new TextRange(Transcript.Document.ContentStart, Transcript.Document.ContentEnd).Text = text;
        Transcript.CaretPosition = Transcript.Document.ContentEnd;
    }

    /// <summary>The correction button and its hint depend on the settings, which can change while the window is open.</summary>
    private void ApplyLlmSettings()
    {
        var on = _settings.LlmEnabled;
        FixButton.Visibility = on ? Visibility.Visible : Visibility.Collapsed;
        if (!on) RevertButton.Visibility = Visibility.Collapsed;
        var clipboard = _previousWindow == IntPtr.Zero;
        HintText.Text = Loc.T(on && _settings.LlmAutoFix
            ? (clipboard ? "Main.Hint.ClipboardFix" : "Main.Hint.PasteFix")
            : (clipboard ? "Main.Hint.Clipboard" : "Main.Hint.Paste"));
    }

    private void OnFixClick(object sender, RoutedEventArgs e) => _ = FixTextAsync();

    private void OnTranscriptTextChanged(object sender, TextChangedEventArgs e) => UpdateFixButton();

    /// <summary>There is nothing to correct in an empty window, and only one correction runs at a time.</summary>
    private void UpdateFixButton()
    {
        if (FixButton != null) FixButton.IsEnabled = !_fixing && TranscriptText().Length > 0;
    }

    /// <summary>
    /// Sends the whole text to the AI and shows the corrected text in the window, where it can be checked and edited; the original is kept
    /// so the user can go back. Recording is paused meanwhile, and Esc cancels.
    /// </summary>
    private async Task FixTextAsync()
    {
        if (_fixing || _finishing || !_settings.LlmEnabled || TranscriptText().Length == 0) return;
        _fixing = true;
        UpdateFixButton();
        _fixCts = new CancellationTokenSource();
        var token = _fixCts.Token;
        // What is being said now would arrive after the correction and be mixed up with it.
        _segmenter.Flush();
        _paused = true;
        SetWhisperInfo(Loc.T("Main.Fixing"), Color.FromRgb(0xfd, 0xd6, 0x63));
        try
        {
            // Sentences that are still being transcribed belong to the text.
            while (_pending > 0) await Task.Delay(50, token);
            var text = TranscriptText();
            if (text.Length == 0)
            {
                SetWhisperInfo("", Color.FromRgb(0x9a, 0xa0, 0xa6));
                return;
            }
            var started = System.Diagnostics.Stopwatch.StartNew();
            var corrected = await Llm.LlmClient.FixAsync(_settings, text, token);
            _original = text;
            SetTranscriptText(corrected);
            _lastFixedText = TranscriptText();
            RevertButton.Visibility = Visibility.Visible;
            SetWhisperInfo(Loc.T("Main.Fixed", started.Elapsed.TotalSeconds,
                Loc.T(_previousWindow == IntPtr.Zero ? "Main.Fixed.Copy" : "Main.Fixed.Paste")), Color.FromRgb(0x81, 0xc9, 0x95));
        }
        catch (OperationCanceledException)
        {
            _lastFixedText = TranscriptText();
            SetWhisperInfo(Loc.T("Main.FixCancelled"), Color.FromRgb(0xfd, 0xd6, 0x63));
        }
        catch (Llm.LlmException ex)
        {
            // The original text stays; the next Enter pastes it instead of trying again.
            Log.Write($"Text correction failed: {ex.Message}");
            _lastFixedText = TranscriptText();
            SetWhisperInfo(Loc.T("Main.FixFailed", ex.Message), Color.FromRgb(0xfd, 0xd6, 0x63));
        }
        finally
        {
            _paused = false;
            _fixing = false;
            UpdateFixButton();
            _fixCts.Dispose();
            _fixCts = null;
            Transcript.Focus();
        }
    }

    private void OnRevertClick(object sender, RoutedEventArgs e)
    {
        if (_original == null || _fixing) return;
        SetTranscriptText(_original);
        _lastFixedText = TranscriptText();
        _original = null;
        RevertButton.Visibility = Visibility.Collapsed;
        SetWhisperInfo("", Color.FromRgb(0x9a, 0xa0, 0xa6));
        Transcript.Focus();
    }

    private void Cancel()
    {
        _finishing = true;
        _cts.Cancel();
        _recorder.Dispose();
        Close();
    }

    private async Task FinishAsync()
    {
        _finishing = true;
        _recorder.Stop();
        _segmenter.Flush();
        _sentences.Writer.Complete();
        Status.Text = Loc.T("Main.Processing");
        Status.Foreground = new SolidColorBrush(Color.FromRgb(0x9a, 0xa0, 0xa6));
        await _consumer;

        if (_error != null)
        {
            // Nothing is copied; the user sees the error and can cancel with Esc.
            _finishing = false;
            _recorder.Dispose();
            Status.Text = Loc.T("Main.ErrorEsc", _error);
            return;
        }

        // The rules were already applied when each sentence was inserted; manually edited text is not rewritten.
        var text = new TextRange(Transcript.Document.ContentStart, Transcript.Document.ContentEnd).Text.Trim();
        _recorder.Dispose();
        if (text.Length == 0)
        {
            Close();
            return;
        }

        if (!await TrySetClipboardAsync(text))
        {
            Status.Text = Loc.T("Main.ClipboardFailed");
            return;
        }

        if (_previousWindow == IntPtr.Zero)
        {
            // Dictation from the tray icon: there is nowhere to paste, the result stays in the clipboard.
            Close();
            App.NotifyTray(Loc.T("Main.InClipboard"));
            return;
        }

        Hide();
        Win32.SetForegroundWindow(_previousWindow);
        await Task.Delay(200);
        Win32.SendCtrlV();
        await Task.Delay(100);
        Close();
    }

    private static async Task<bool> TrySetClipboardAsync(string text)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                Clipboard.SetDataObject(text, true);
                return true;
            }
            catch (System.Runtime.InteropServices.COMException)
            {
                await Task.Delay(50);
            }
        }
        return false;
    }
}
