using System.Windows;

namespace BlahBlah3000;

/// <summary>A small window for quickly adding a replacement from the selected text; the caret goes straight to the field for the new text.</summary>
public partial class AddReplacementWindow : Window
{
    internal AddReplacementWindow(string pattern)
    {
        InitializeComponent();
        PatternBox.Text = pattern;
        Loaded += (_, _) =>
        {
            ReplacementBox.Focus();
            ReplacementBox.SelectAll();
        };
    }

    internal ReplacementRule Rule => new() { Pattern = PatternBox.Text.Trim(), Replacement = ReplacementBox.Text, IsRegex = RegexCheck.IsChecked == true };

    private void OnAdd(object sender, RoutedEventArgs e)
    {
        if (PatternBox.Text.Trim().Length == 0 || RegexCheck.IsChecked == true && Rules.RuleEngine.TryCreateRegex(PatternBox.Text.Trim()) == null)
        {
            PatternBox.Focus();
            return;
        }
        DialogResult = true;
    }
}
