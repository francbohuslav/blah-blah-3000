using System.Windows;
using System.Windows.Controls;
using BlahBlah3000.Dictation;

namespace BlahBlah3000;

/// <summary>
/// The latest push-to-talk sentences with the text at each stage (heard, after the replacements, typed). Selecting a wrong part and clicking
/// the button opens the "new replacement" window, so a replacement can be made without the dictation window.
/// </summary>
public partial class HistoryWindow : Window
{
    private readonly Settings _settings;
    private string _selection = "";

    internal HistoryWindow(Settings settings)
    {
        _settings = settings;
        InitializeComponent();
        Reload();
        DictationHistory.Changed += OnHistoryChanged;
        Closed += (_, _) => DictationHistory.Changed -= OnHistoryChanged;
    }

    /// <summary>The entry is added from the transcription thread.</summary>
    private void OnHistoryChanged() => Dispatcher.BeginInvoke(Reload);

    private void Reload()
    {
        var selected = List.SelectedItem as HistoryEntry;
        List.ItemsSource = DictationHistory.Snapshot();
        // A new sentence must not take the entry the user is reading away from them.
        if (selected != null) List.SelectedItem = ((IEnumerable<HistoryEntry>)List.ItemsSource).FirstOrDefault(e => e == selected);
        if (List.SelectedItem == null && List.Items.Count > 0) List.SelectedIndex = 0;
        ClearButton.IsEnabled = List.Items.Count > 0;
    }

    private void OnEntryChanged(object sender, SelectionChangedEventArgs e)
    {
        var entry = List.SelectedItem as HistoryEntry;
        HeardBox.Text = entry?.Heard ?? "";
        ReplacedBox.Text = entry?.Replaced ?? "";
        TypedBox.Text = entry?.Typed ?? "";
        _selection = "";
        AddButton.IsEnabled = entry != null;
    }

    /// <summary>The selection of a text box stays visible after it loses the focus, but it is remembered here anyway for the button.</summary>
    private void OnTextSelectionChanged(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox box && box.SelectedText.Length > 0) _selection = box.SelectedText.Trim();
    }

    private void OnAdd(object sender, RoutedEventArgs e)
    {
        if (List.SelectedItem is not HistoryEntry entry) return;
        // Without a selection, the whole heard sentence is offered.
        var pattern = _selection.Length > 0 ? _selection : entry.Heard;
        var dialog = new AddReplacementWindow(pattern) { Owner = this };
        if (dialog.ShowDialog() == true) _settings.AddOrUpdateReplacement(dialog.Rule);
    }

    private void OnClear(object sender, RoutedEventArgs e) => DictationHistory.Clear();
}
