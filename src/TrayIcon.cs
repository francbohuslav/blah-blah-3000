using System.Windows;
using Drawing = System.Drawing;
using Forms = System.Windows.Forms;

namespace BlahBlah3000;

/// <summary>Notification area (tray) icon: a click opens dictation, the right button opens the menu.</summary>
internal sealed class TrayIcon : IDisposable
{
    private readonly Forms.NotifyIcon _icon;
    private readonly Forms.ToolStripMenuItem _state = new() { Enabled = false };
    private readonly Forms.ToolStripMenuItem _download = new();
    private readonly Forms.ToolStripMenuItem _autostart = new() { CheckOnClick = false };
    private readonly Forms.ToolStripMenuItem _dictate = new();
    private readonly Forms.ToolStripMenuItem _settings = new();
    private readonly Forms.ToolStripMenuItem _exit = new();
    private readonly Drawing.Icon _normalIcon;
    private readonly Drawing.Icon _recordingIcon;
    private string _tip = "BlahBlah3000";
    private bool _recording;

    public event Action? Dictate;
    public event Action? OpenSettings;
    public event Action? DownloadModel;
    public event Action? ToggleAutostart;
    public event Action? Exit;

    public TrayIcon()
    {
        var resource = Application.GetResourceStream(new Uri("pack://application:,,,/assets/BlahBlah3000.ico"));
        var menu = new Forms.ContextMenuStrip();
        _dictate.Click += (_, _) => Dictate?.Invoke();
        _settings.Click += (_, _) => OpenSettings?.Invoke();
        _download.Click += (_, _) => DownloadModel?.Invoke();
        _autostart.Click += (_, _) => ToggleAutostart?.Invoke();
        _exit.Click += (_, _) => Exit?.Invoke();
        menu.Items.AddRange([_dictate, new Forms.ToolStripSeparator(), _state, _download, new Forms.ToolStripSeparator(), _settings, _autostart,
            new Forms.ToolStripSeparator(), _exit]);
        ApplyLanguage();

        _normalIcon = new Drawing.Icon(resource.Stream, Forms.SystemInformation.SmallIconSize);
        _recordingIcon = CreateRecordingIcon(_normalIcon);
        _icon = new Forms.NotifyIcon
        {
            Icon = _normalIcon,
            Text = "BlahBlah3000",
            ContextMenuStrip = menu,
            Visible = true,
        };
        _icon.MouseClick += (_, e) =>
        {
            if (e.Button == Forms.MouseButtons.Left) Dictate?.Invoke();
        };
    }

    /// <summary>Sets the menu texts in the current interface language.</summary>
    public void ApplyLanguage()
    {
        _dictate.Text = Loc.T("Tray.Dictate");
        _settings.Text = Loc.T("Tray.Settings");
        _download.Text = Loc.T("Tray.Download");
        _autostart.Text = Loc.T("Tray.Autostart");
        _exit.Text = Loc.T("Tray.Exit");
    }

    /// <summary>Updates the model state in the menu and the tooltip text.</summary>
    public void Refresh(string stateText, bool modelMissing, bool autostart, string hotkey)
    {
        _state.Text = stateText;
        _download.Visible = modelMissing;
        _autostart.Checked = autostart;
        var tip = $"BlahBlah3000 – {stateText}" + (hotkey.Length > 0 ? $" ({hotkey})" : "");
        _tip = tip.Length > 63 ? tip[..63] : tip;
        if (!_recording) _icon.Text = _tip;
    }

    /// <summary>Push to talk is recording: the icon gets a red background (and a tooltip) until it is switched back.</summary>
    public void SetRecording(bool recording)
    {
        _recording = recording;
        _icon.Icon = recording ? _recordingIcon : _normalIcon;
        _icon.Text = recording ? Loc.T("Tray.Recording") : _tip;
    }

    /// <summary>
    /// The application icon with its dark background (the rounded square behind the smile) turned red. The white teeth and the colored
    /// mouth keep their colors; pixels in between (anti-aliased edges) are blended by how close they are to the dark gray.
    /// </summary>
    private static Drawing.Icon CreateRecordingIcon(Drawing.Icon original)
    {
        const int red = 0xe5, green = 0x39, blue = 0x35;
        using var bitmap = original.ToBitmap();
        for (var y = 0; y < bitmap.Height; y++)
        {
            for (var x = 0; x < bitmap.Width; x++)
            {
                var pixel = bitmap.GetPixel(x, y);
                if (pixel.A == 0) continue;
                var max = Math.Max(pixel.R, Math.Max(pixel.G, pixel.B));
                var min = Math.Min(pixel.R, Math.Min(pixel.G, pixel.B));
                // Only neutral grays are recolored (the mouth interior is dark red and stays as it is).
                if (max - min > 24) continue;
                // 1 for the dark background (gray up to about 70), falling to 0 for light grays and white.
                var weight = Math.Clamp((150 - max) / 80.0, 0, 1);
                if (weight == 0) continue;
                bitmap.SetPixel(x, y, Drawing.Color.FromArgb(pixel.A,
                    (int)(pixel.R + (red - pixel.R) * weight),
                    (int)(pixel.G + (green - pixel.G) * weight),
                    (int)(pixel.B + (blue - pixel.B) * weight)));
            }
        }
        var handle = bitmap.GetHicon();
        try
        {
            using var temporary = Drawing.Icon.FromHandle(handle);
            return (Drawing.Icon)temporary.Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr handle);

    public void Notify(string text, bool warning = false) =>
        _icon.ShowBalloonTip(3000, "BlahBlah3000", text, warning ? Forms.ToolTipIcon.Warning : Forms.ToolTipIcon.Info);

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
        _normalIcon.Dispose();
        _recordingIcon.Dispose();
    }
}
