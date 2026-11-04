using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using BlahBlah3000.Native;
using BlahBlah3000.Whisper;

namespace BlahBlah3000;

/// <summary>First-run wizard: language, the Whisper model (detection and download after consent) and the keyboard shortcut.</summary>
public partial class FirstRunWindow : Window
{
    private const double SizeMb = ModelStore.ExpectedSize / 1024.0 / 1024.0;

    private readonly Settings _settings;
    private bool _hotkeyOk;

    internal FirstRunWindow(Settings settings)
    {
        InitializeComponent();
        _settings = settings;
        LanguageCombo.ItemsSource = Languages.All;
        LanguageCombo.SelectedValue = Languages.All.Any(l => l.Code == settings.Language) ? settings.Language : "auto";
        HotkeyBox.Text = settings.Hotkey;
        AutostartCheck.IsChecked = settings.Autostart;
        App.Transcriber.StateChanged += OnStateChanged;
        Closed += (_, _) => App.Transcriber.StateChanged -= OnStateChanged;
        RefreshModel();
        ValidateHotkey();
    }

    private void OnStateChanged() => Dispatcher.BeginInvoke(RefreshModel);

    private void RefreshModel()
    {
        var transcriber = App.Transcriber;
        ModelBar.Visibility = Visibility.Collapsed;
        ModelError.Visibility = Visibility.Collapsed;
        ModelButton.Visibility = Visibility.Collapsed;
        ModelText.Foreground = Brushes.Black;
        switch (transcriber.State)
        {
            case TranscriberState.Downloading:
                ModelText.Text = Loc.T("FR.Model.Downloading", transcriber.Progress * SizeMb, SizeMb);
                ModelBar.Visibility = Visibility.Visible;
                ModelBar.Value = transcriber.Progress;
                ModelButton.Visibility = Visibility.Visible;
                ModelButton.Content = Loc.T("FR.Model.Cancel");
                break;
            case TranscriberState.ModelMissing:
                ModelText.Text = Loc.T("FR.Model.Missing", ModelStore.FileName, SizeMb, ModelStore.Directory_);
                if (transcriber.Detail.Length > 0)
                {
                    ModelError.Text = transcriber.Detail;
                    ModelError.Visibility = Visibility.Visible;
                }
                ModelButton.Visibility = Visibility.Visible;
                ModelButton.Content = Loc.T("FR.Model.Download", SizeMb);
                break;
            case TranscriberState.Error:
                ModelText.Text = Loc.T("FR.Model.Error", transcriber.Detail);
                ModelText.Foreground = Brushes.Firebrick;
                break;
            default:
                ModelText.Text = Loc.T("FR.Model.OK");
                ModelText.Foreground = Brushes.SeaGreen;
                break;
        }
    }

    private void OnModelButton(object sender, RoutedEventArgs e)
    {
        if (App.Transcriber.State == TranscriberState.Downloading) App.Transcriber.CancelDownload();
        else _ = App.Transcriber.DownloadModelAsync();
    }

    private void OnHotkeyKeyDown(object sender, KeyEventArgs e)
    {
        e.Handled = true;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        var text = GlobalHotkey.Format(key, Keyboard.Modifiers);
        if (text == null) return;
        HotkeyBox.Text = text;
        ValidateHotkey();
    }

    /// <summary>Checks that no other application holds the shortcut; otherwise asks for a different one and blocks finishing.</summary>
    private void ValidateHotkey()
    {
        _hotkeyOk = App.IsHotkeyAvailable(HotkeyBox.Text);
        HotkeyHint.Text = _hotkeyOk
            ? Loc.T("FR.Hotkey.Free", HotkeyBox.Text)
            : Loc.T("FR.Hotkey.Taken", HotkeyBox.Text);
        HotkeyHint.Foreground = _hotkeyOk ? Brushes.SeaGreen : Brushes.Firebrick;
        FinishButton.IsEnabled = _hotkeyOk;
    }

    private void OnFinish(object sender, RoutedEventArgs e)
    {
        if (!_hotkeyOk) return;
        _settings.Language = LanguageCombo.SelectedValue as string ?? "auto";
        _settings.Hotkey = HotkeyBox.Text;
        _settings.Autostart = AutostartCheck.IsChecked == true;
        _settings.FirstRunDone = true;
        _settings.Save();
        DialogResult = true;
    }
}
