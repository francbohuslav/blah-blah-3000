using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace BlahBlah3000.Native;

/// <summary>
/// Low-level keyboard hook that detects a modifier-only combination being held (push to talk), e.g. "Ctrl+Shift".
/// It never swallows a key. Must be created and used on the UI thread (the hook callback runs on the thread that installed it).
/// </summary>
internal sealed class KeyboardHook : IDisposable
{
    private const int WhKeyboardLl = 13;
    private const int WmKeyDown = 0x0100, WmKeyUp = 0x0101, WmSysKeyDown = 0x0104, WmSysKeyUp = 0x0105;
    private const uint LlkhfInjected = 0x10;

    private delegate IntPtr HookProc(int code, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int hookId, HookProc proc, IntPtr module, uint threadId);

    [DllImport("user32.dll")]
    private static extern bool UnhookWindowsHookEx(IntPtr hook);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string? name);

    [StructLayout(LayoutKind.Sequential)]
    private struct KbdLlHookStruct
    {
        public uint VkCode;
        public uint ScanCode;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;
    }

    // Virtual key codes reported by the low-level hook are always the left/right variants.
    private static readonly Dictionary<string, int[]> Modifiers = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ctrl"] = [0xA2, 0xA3],
        ["shift"] = [0xA0, 0xA1],
        ["leftctrl"] = [0xA2],
        ["rightctrl"] = [0xA3],
        ["leftshift"] = [0xA0],
        ["rightshift"] = [0xA1],
    };

    // The interval is the hold delay: how long the combination must be held before it counts; shorter holds are ordinary shortcuts.
    private readonly DispatcherTimer _holdTimer = new() { Interval = TimeSpan.FromMilliseconds(300) };
    private readonly HookProc _proc;
    private readonly HashSet<int> _held = [];
    private IntPtr _hook;
    private int[][] _groups = [];
    private bool _cancelled;
    private bool _engaged;
    private bool _latched;

    /// <summary>See the <c>latch</c> parameter of <see cref="Enable"/>.</summary>
    public bool Latch { get; private set; }

    /// <summary>The keys were let go and the recording goes on until they are pressed again.</summary>
    public bool IsLatched => _latched;

    /// <summary>
    /// Leaves the latched state without raising <see cref="Released"/> (the caller ends the recording itself, e.g. after a silence),
    /// so the next press of the keys starts a new recording instead of being taken as a stop.
    /// </summary>
    public void EndLatch() => _latched = false;

    /// <summary>The combination has been held for the hold delay without any other key.</summary>
    public event Action? Engaged;

    /// <summary>Latch mode: the engaged keys were let go and the recording goes on (the owner may still decide to end it, see <see cref="EndLatch"/>).</summary>
    public event Action? Latched;

    /// <summary>An engaged combination ended: a modifier was released (or, when latched, the keys were pressed again) or another key was pressed.</summary>
    public event Action? Released;

    public KeyboardHook()
    {
        // The delegate lives in a field so the garbage collector cannot free it while Windows still calls it.
        _proc = Callback;
        _holdTimer.Tick += (_, _) =>
        {
            _holdTimer.Stop();
            if (_cancelled || _engaged || !AllHeld()) return;
            _engaged = true;
            // The held keys are hidden from other applications from now on: the target sees them released and gets no further events of
            // them (auto-repeat, the final key-up), so the typed text never arrives as Ctrl+letter shortcuts.
            Win32.SendKeyUps(_held);
            Engaged?.Invoke();
        };
    }

    /// <summary>Parses a combination such as "Ctrl+Shift" or "RightCtrl"; false when it is not made of known modifiers.</summary>
    public static bool TryParse(string text, out int[][] groups)
    {
        var parsed = new List<int[]>();
        foreach (var part in text.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (!Modifiers.TryGetValue(part, out var keys))
            {
                groups = [];
                return false;
            }
            parsed.Add(keys);
        }
        groups = parsed.ToArray();
        return groups.Length > 0;
    }

    /// <summary>Starts listening for the combination (replacing any previous one). Returns false when it is invalid or the hook cannot be installed.</summary>
    /// <param name="latch">true = after the hold delay the keys may be released and the recording goes on until they are pressed again</param>
    public bool Enable(string combination, TimeSpan holdDelay, bool latch)
    {
        Disable();
        if (!TryParse(combination, out _groups)) return false;
        _holdTimer.Interval = holdDelay;
        Latch = latch;
        _hook = SetWindowsHookEx(WhKeyboardLl, _proc, GetModuleHandle(null), 0);
        if (_hook == IntPtr.Zero)
        {
            Log.Write($"Cannot install the keyboard hook (error {Marshal.GetLastWin32Error()})");
            return false;
        }
        return true;
    }

    public void Disable()
    {
        if (_hook != IntPtr.Zero) UnhookWindowsHookEx(_hook);
        _hook = IntPtr.Zero;
        _holdTimer.Stop();
        _held.Clear();
        _cancelled = false;
        if (_engaged || _latched)
        {
            _engaged = false;
            _latched = false;
            Released?.Invoke();
        }
    }

    private bool AllHeld() => _groups.Length > 0 && _groups.All(group => group.Any(_held.Contains));

    private bool IsTracked(int vk) => _groups.Any(group => Array.IndexOf(group, vk) >= 0);

    private IntPtr Callback(int code, IntPtr wParam, IntPtr lParam)
    {
        var swallow = false;
        if (code >= 0)
        {
            var info = Marshal.PtrToStructure<KbdLlHookStruct>(lParam);
            // Keys we inject ourselves (typing the transcript) must not influence the state.
            if ((info.Flags & LlkhfInjected) == 0)
            {
                var vk = (int)info.VkCode;
                // Decided before the handlers run: the key-up that ends the push to talk is hidden too.
                swallow = _engaged && IsTracked(vk);
                var message = wParam.ToInt32();
                if (message is WmKeyDown or WmSysKeyDown) OnKeyDown(vk);
                else if (message is WmKeyUp or WmSysKeyUp) OnKeyUp(vk);
            }
        }
        return swallow ? 1 : CallNextHookEx(_hook, code, wParam, lParam);
    }

    private void OnKeyDown(int vk)
    {
        if (IsTracked(vk))
        {
            _held.Add(vk);
            if (_latched)
            {
                // Pressing the keys again stops a latched push to talk. They are not hidden, so e.g. Ctrl+C still works as a shortcut.
                if (AllHeld())
                {
                    _latched = false;
                    _cancelled = true;
                    Released?.Invoke();
                }
                return;
            }
            if (!_cancelled && !_engaged && AllHeld() && !_holdTimer.IsEnabled) _holdTimer.Start();
            return;
        }
        // Another key while no tracked modifier is held is just typing; the flag would otherwise stay set until the next release.
        if (_held.Count == 0) return;
        // Another key while a modifier is held means an ordinary shortcut (or the user went back to typing).
        _cancelled = true;
        _holdTimer.Stop();
        EndEngaged();
    }

    private void OnKeyUp(int vk)
    {
        if (!IsTracked(vk)) return;
        _held.Remove(vk);
        if (!AllHeld())
        {
            _holdTimer.Stop();
            if (_engaged && Latch)
            {
                // The keys may be let go; the recording goes on until they are pressed again.
                _engaged = false;
                _latched = true;
                Latched?.Invoke();
            }
            else EndEngaged();
        }
        if (_held.Count == 0) _cancelled = false;
    }

    private void EndEngaged()
    {
        if (!_engaged) return;
        _engaged = false;
        Released?.Invoke();
    }

    public void Dispose() => Disable();
}
