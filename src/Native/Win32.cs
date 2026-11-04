using System.Runtime.InteropServices;

namespace BlahBlah3000.Native;

internal static class Win32
{
    private const ushort VkControl = 0x11;
    private const ushort VkV = 0x56;
    private const uint InputKeyboard = 1;
    private const uint KeyUp = 0x0002;

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern uint SendInput(uint count, Input[] inputs, int size);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr processId);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool attach);

    [DllImport("user32.dll")]
    private static extern bool BringWindowToTop(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte vk, byte scan, uint flags, UIntPtr extraInfo);

    private const byte VkF24 = 0x87;

    [DllImport("user32.dll")]
    private static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder name, int max);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    /// <summary>
    /// Tells whether pasting with Ctrl+V into the window makes sense: it exists, it is neither the desktop nor the taskbar,
    /// and it is not ours. Otherwise the text is only copied to the clipboard.
    /// </summary>
    public static bool IsPasteTarget(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero || !IsWindow(hWnd)) return false;
        GetWindowThreadProcessId(hWnd, out var pid);
        if (pid == Environment.ProcessId) return false;
        var name = new System.Text.StringBuilder(64);
        GetClassName(hWnd, name, name.Capacity);
        return name.ToString() is not ("Shell_TrayWnd" or "Shell_SecondaryTrayWnd" or "Progman" or "WorkerW" or "NotifyIconOverflowWindow"
            or "TopLevelWindowForOverflowXamlIsland");
    }

    /// <summary>
    /// Forces a window to the foreground. A process started from a shortcut is not allowed by Windows to take focus, so it attaches
    /// to the input thread of the currently active window and taps a harmless key before switching.
    /// </summary>
    /// <returns>true when the window is really active after the call</returns>
    public static bool ForceForeground(IntPtr hWnd)
    {
        var foreground = GetForegroundWindow();
        if (foreground == hWnd) return true;
        var foregroundThread = foreground == IntPtr.Zero ? 0 : GetWindowThreadProcessId(foreground, IntPtr.Zero);
        var currentThread = GetCurrentThreadId();
        var attached = foregroundThread != 0 && foregroundThread != currentThread
            && AttachThreadInput(currentThread, foregroundThread, true);
        try
        {
            // A key tap unlocks SetForegroundWindow; unlike Alt, F24 does not activate the window menu.
            keybd_event(VkF24, 0, 0, UIntPtr.Zero);
            keybd_event(VkF24, 0, KeyUp, UIntPtr.Zero);
            BringWindowToTop(hWnd);
            SetForegroundWindow(hWnd);
        }
        finally
        {
            if (attached) AttachThreadInput(currentThread, foregroundThread, false);
        }
        return GetForegroundWindow() == hWnd;
    }

    /// <summary>Sends Ctrl+V to the active window.</summary>
    public static void SendCtrlV()
    {
        var inputs = new[]
        {
            Key(VkControl, 0), Key(VkV, 0), Key(VkV, KeyUp), Key(VkControl, KeyUp),
        };
        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>());
    }

    private static Input Key(ushort vk, uint flags) => new()
    {
        Type = InputKeyboard,
        Union = new InputUnion { Keyboard = new KeyboardInput { VirtualKey = vk, Flags = flags } },
    };

    [StructLayout(LayoutKind.Sequential)]
    private struct Input
    {
        public uint Type;
        public InputUnion Union;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public KeyboardInput Keyboard;
        [FieldOffset(0)] public MouseInput Mouse;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardInput
    {
        public ushort VirtualKey;
        public ushort ScanCode;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;
    }

    // Only for the correct union size (MOUSEINPUT is the largest member).
    [StructLayout(LayoutKind.Sequential)]
    private struct MouseInput
    {
        public int Dx;
        public int Dy;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;
    }
}
