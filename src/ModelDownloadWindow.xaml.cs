using System.Windows;
using BlahBlah3000.Whisper;

namespace BlahBlah3000;

/// <summary>Announces that the Whisper model is missing and downloads it only after consent; the download can be cancelled at any time.</summary>
public partial class ModelDownloadWindow : Window
{
    private const double SizeMb = ModelStore.ExpectedSize / 1024.0 / 1024.0;

    internal ModelDownloadWindow()
    {
        InitializeComponent();
        App.Transcriber.StateChanged += OnStateChanged;
        Closed += (_, _) => App.Transcriber.StateChanged -= OnStateChanged;
        Refresh();
    }

    private void OnStateChanged() => Dispatcher.BeginInvoke(Refresh);

    private void Refresh()
    {
        var transcriber = App.Transcriber;
        switch (transcriber.State)
        {
            case TranscriberState.Downloading:
                Heading.Text = Loc.T("MD.Downloading.Heading");
                Body.Text = Loc.T("MD.Downloading.Body", transcriber.Progress * SizeMb, SizeMb);
                Bar.Visibility = Visibility.Visible;
                Bar.Value = transcriber.Progress;
                ErrorText.Visibility = Visibility.Collapsed;
                PrimaryButton.Visibility = Visibility.Collapsed;
                SecondaryButton.Content = Loc.T("MD.Cancel");
                break;
            case TranscriberState.ModelMissing:
                Heading.Text = Loc.T("MD.Missing.Heading");
                Body.Text = Loc.T("MD.Missing.Body", ModelStore.FileName, SizeMb, ModelStore.Directory_);
                Bar.Visibility = Visibility.Collapsed;
                ErrorText.Text = transcriber.Detail;
                ErrorText.Visibility = transcriber.Detail.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
                PrimaryButton.Visibility = Visibility.Visible;
                PrimaryButton.Content = Loc.T("MD.Download", SizeMb);
                SecondaryButton.Content = Loc.T("MD.NotNow");
                break;
            default:
                // The model is downloaded (loading or ready); the window has nothing left to offer.
                Close();
                break;
        }
    }

    private void OnPrimary(object sender, RoutedEventArgs e) => _ = App.Transcriber.DownloadModelAsync();

    private void OnSecondary(object sender, RoutedEventArgs e)
    {
        if (App.Transcriber.State == TranscriberState.Downloading) App.Transcriber.CancelDownload();
        else Close();
    }
}
