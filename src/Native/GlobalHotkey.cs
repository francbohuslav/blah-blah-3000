using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace BlahBlah3000.Native;

/// <summary>Global keyboard shortcut (RegisterHotKey) on a hidden message-only window. A shortcut is written as, for example, "Ctrl+Alt+Space".</summary>
internal sealed class GlobalHotkey : IDisposable
{
    private const int WmHotkey = 0x0312;
    private const int HotkeyId = 0x4B43;
    private const uint ModAlt = 1, ModControl = 2, ModShift = 4, ModWin = 8, ModNoRepeat = 0x4000;

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint modifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private readonly HwndSource _source;
    private bool _registered;

    public event Action? Pressed;

    public GlobalHotkey()
    {
        // HWND_MESSAGE (-3): a message-only window that is never shown.
        _source = new HwndSource(new HwndSourceParameters("BlahBlah3000Hotkey") { ParentWindow = new IntPtr(-3) });
        _source.AddHook(Hook);
    }

    private IntPtr Hook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmHotkey && wParam.ToInt32() == HotkeyId)
        {
            handled = true;
            Pressed?.Invoke();
        }
        return IntPtr.Zero;
    }

    /// <summary>Registers the shortcut (replacing any previous one). Returns false when the shortcut is invalid or already held by another application.</summary>
    public bool Register(string text)
    {
        Unregister();
        if (string.IsNullOrWhiteSpace(text)) return true;
        if (!TryParse(text, out var modifiers, out var key)) return false;
        _registered = RegisterHotKey(_source.Handle, HotkeyId, modifiers | ModNoRepeat, (uint)KeyInterop.VirtualKeyFromKey(key));
        return _registered;
    }

    private void Unregister()
    {
        if (_registered) UnregisterHotKey(_source.Handle, HotkeyId);
        _registered = false;
    }

    public static bool TryParse(string text, out uint modifiers, out Key key)
    {
        modifiers = 0;
        key = Key.None;
        foreach (var part in text.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            switch (part.ToLowerInvariant())
            {
                case "ctrl": modifiers |= ModControl; break;
                case "alt": modifiers |= ModAlt; break;
                case "shift": modifiers |= ModShift; break;
                case "win": modifiers |= ModWin; break;
                default:
                    if (!Enum.TryParse(part, true, out key) || key == Key.None) return false;
                    break;
            }
        }
        return key != Key.None && modifiers != 0;
    }

    /// <summary>Builds the shortcut text from the pressed key and modifiers; null when it is only a modifier or no modifier is held.</summary>
    public static string? Format(Key key, ModifierKeys modifiers)
    {
        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt or Key.LeftShift or Key.RightShift
            or Key.LWin or Key.RWin or Key.System or Key.None or Key.ImeProcessed) return null;
        if (modifiers == ModifierKeys.None) return null;
        var parts = new List<string>();
        if (modifiers.HasFlag(ModifierKeys.Control)) parts.Add("Ctrl");
        if (modifiers.HasFlag(ModifierKeys.Alt)) parts.Add("Alt");
        if (modifiers.HasFlag(ModifierKeys.Shift)) parts.Add("Shift");
        if (modifiers.HasFlag(ModifierKeys.Windows)) parts.Add("Win");
        parts.Add(key.ToString());
        return string.Join("+", parts);
    }

    public void Dispose()
    {
        Unregister();
        _source.Dispose();
    }
}
