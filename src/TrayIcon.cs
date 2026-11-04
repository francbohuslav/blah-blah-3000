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

        _icon = new Forms.NotifyIcon
        {
            Icon = new Drawing.Icon(resource.Stream, Forms.SystemInformation.SmallIconSize),
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
        _icon.Text = tip.Length > 63 ? tip[..63] : tip;
    }

    public void Notify(string text, bool warning = false) =>
        _icon.ShowBalloonTip(3000, "BlahBlah3000", text, warning ? Forms.ToolTipIcon.Warning : Forms.ToolTipIcon.Info);

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
    }
}
