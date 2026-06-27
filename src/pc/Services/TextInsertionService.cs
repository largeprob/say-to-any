using System.Runtime.InteropServices;
using System.Text;

namespace pc.Services;

public readonly record struct WindowsTextInsertionTarget(IntPtr ForegroundWindow, IntPtr FocusWindow) : ITextInsertionTarget;

public sealed class WindowsTextInsertionService : ITextInsertionService
{
    public async Task CopyTextAsync(string text)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("Clipboard insertion is currently implemented for Windows.");
        }

        await Task.Run(() => SetClipboardText(text));
    }

    public async Task PasteTextAsync(string text)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("Text insertion is currently implemented for Windows.");
        }

        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        var target = CaptureCurrentTarget();
        if (target is WindowsTextInsertionTarget windowsTarget &&
            await Task.Run(() => TrySendTextToTarget(text, windowsTarget)))
        {
            return;
        }

        var clipboardTarget = target is WindowsTextInsertionTarget capturedTarget
            ? capturedTarget
            : (WindowsTextInsertionTarget?)null;
        if (!await PasteViaClipboardAsync(text, clipboardTarget))
        {
            throw new InvalidOperationException("Cannot send text to the current focus.");
        }
    }

    public ITextInsertionTarget? CaptureCurrentTarget()
    {
        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        var foregroundWindow = GetForegroundWindow();
        return CreateTarget(foregroundWindow);
    }

    public async Task<bool> TryPasteTextToCurrentFocusAsync(string text, ITextInsertionTarget? preferredTarget = null)
    {
        if (!OperatingSystem.IsWindows() || string.IsNullOrEmpty(text))
        {
            return false;
        }

        if (preferredTarget is WindowsTextInsertionTarget preferredWindowsTarget &&
            await Task.Run(() => TrySendTextToTarget(text, preferredWindowsTarget)))
        {
            return true;
        }

        var currentTarget = CaptureCurrentTarget();
        if (currentTarget is WindowsTextInsertionTarget currentWindowsTarget &&
            !Equals(currentTarget, preferredTarget) &&
            await Task.Run(() => TrySendTextToTarget(text, currentWindowsTarget)))
        {
            return true;
        }

        WindowsTextInsertionTarget? clipboardTarget = preferredTarget is WindowsTextInsertionTarget preferredClipboardTarget
            ? preferredClipboardTarget
            : currentTarget is WindowsTextInsertionTarget currentClipboardTarget
                ? currentClipboardTarget
                : null;
        if (clipboardTarget is null)
        {
            return false;
        }

        return await PasteViaClipboardAsync(text, clipboardTarget.Value);
    }

    private static async Task<bool> PasteViaClipboardAsync(string text, WindowsTextInsertionTarget? target)
    {
        var previousText = await Task.Run(GetClipboardText);

        try
        {
            await Task.Run(() => SetClipboardText(text));
            await Task.Delay(100);
            if (target is not null)
            {
                await Task.Run(() => ActivateTarget(target.Value));
                await Task.Delay(60);
            }

            var sent = await Task.Run(SendCtrlV);
            await Task.Delay(160);
            return sent;
        }
        finally
        {
            if (previousText is not null)
            {
                await Task.Run(() => SetClipboardText(previousText));
            }
        }
    }

    private static string? GetClipboardText()
    {
        if (!OpenClipboardWithRetry())
        {
            return null;
        }

        try
        {
            if (!IsClipboardFormatAvailable(ClipboardFormatUnicodeText))
            {
                return null;
            }

            var handle = GetClipboardData(ClipboardFormatUnicodeText);
            if (handle == IntPtr.Zero)
            {
                return null;
            }

            var pointer = GlobalLock(handle);
            if (pointer == IntPtr.Zero)
            {
                return null;
            }

            try
            {
                return Marshal.PtrToStringUni(pointer);
            }
            finally
            {
                GlobalUnlock(handle);
            }
        }
        finally
        {
            CloseClipboard();
        }
    }

    private static void SetClipboardText(string text)
    {
        if (!OpenClipboardWithRetry())
        {
            throw new InvalidOperationException("Cannot open the clipboard.");
        }

        IntPtr handle = IntPtr.Zero;
        try
        {
            EmptyClipboard();

            var bytes = Encoding.Unicode.GetBytes(text + '\0');
            handle = GlobalAlloc(GlobalMoveable, (UIntPtr)bytes.Length);
            if (handle == IntPtr.Zero)
            {
                throw new InvalidOperationException("Cannot allocate clipboard memory.");
            }

            var pointer = GlobalLock(handle);
            if (pointer == IntPtr.Zero)
            {
                throw new InvalidOperationException("Cannot lock clipboard memory.");
            }

            try
            {
                Marshal.Copy(bytes, 0, pointer, bytes.Length);
            }
            finally
            {
                GlobalUnlock(handle);
            }

            if (SetClipboardData(ClipboardFormatUnicodeText, handle) == IntPtr.Zero)
            {
                throw new InvalidOperationException("Cannot set clipboard data.");
            }

            handle = IntPtr.Zero;
        }
        finally
        {
            if (handle != IntPtr.Zero)
            {
                GlobalFree(handle);
            }

            CloseClipboard();
        }
    }

    private static bool OpenClipboardWithRetry()
    {
        for (var i = 0; i < 10; i++)
        {
            if (OpenClipboard(IntPtr.Zero))
            {
                return true;
            }

            Thread.Sleep(20);
        }

        return false;
    }

    private static bool TrySendTextToTarget(string text, WindowsTextInsertionTarget target)
    {
        if (!TryResolveFocusWindow(target, out _))
        {
            return false;
        }

        ActivateTarget(target);
        return SendUnicodeText(text);
    }

    private static WindowsTextInsertionTarget? CreateTarget(IntPtr foregroundWindow)
    {
        if (foregroundWindow == IntPtr.Zero || !IsWindow(foregroundWindow))
        {
            return null;
        }

        var threadId = GetWindowThreadProcessId(foregroundWindow, out var processId);
        if (threadId == 0 || processId == Environment.ProcessId)
        {
            return null;
        }

        var focusWindow = GetFocusWindow(threadId);
        return new WindowsTextInsertionTarget(foregroundWindow, focusWindow);
    }

    private static bool TryResolveFocusWindow(WindowsTextInsertionTarget target, out IntPtr focusWindow)
    {
        focusWindow = target.FocusWindow;
        if (focusWindow != IntPtr.Zero && IsWindow(focusWindow))
        {
            return true;
        }

        if (target.ForegroundWindow == IntPtr.Zero || !IsWindow(target.ForegroundWindow))
        {
            focusWindow = IntPtr.Zero;
            return false;
        }

        focusWindow = GetFocusWindow(target.ForegroundWindow);
        return focusWindow != IntPtr.Zero && IsWindow(focusWindow);
    }

    private static IntPtr GetFocusWindow(IntPtr foregroundWindow)
    {
        var threadId = GetWindowThreadProcessId(foregroundWindow, out _);
        if (threadId == 0)
        {
            return IntPtr.Zero;
        }

        return GetFocusWindow(threadId);
    }

    private static IntPtr GetFocusWindow(uint threadId)
    {
        var info = new GuiThreadInfo
        {
            Size = Marshal.SizeOf<GuiThreadInfo>()
        };

        return GetGUIThreadInfo(threadId, ref info)
            ? info.FocusWindow
            : IntPtr.Zero;
    }

    private static void ActivateTarget(WindowsTextInsertionTarget target)
    {
        if (target.ForegroundWindow == IntPtr.Zero || !IsWindow(target.ForegroundWindow))
        {
            return;
        }

        SetForegroundWindow(target.ForegroundWindow);
        Thread.Sleep(35);
    }

    private static bool SendUnicodeText(string text)
    {
        var inputs = new Input[text.Length * 2];
        var inputIndex = 0;
        foreach (var character in text)
        {
            inputs[inputIndex++] = UnicodeKeyboardInput(character, KeyEventUnicode);
            inputs[inputIndex++] = UnicodeKeyboardInput(character, KeyEventUnicode | KeyEventKeyUp);
        }

        return SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>()) == inputs.Length;
    }

    private static bool SendCtrlV()
    {
        var inputs = new[]
        {
            KeyboardInput(VirtualKeyControl, 0),
            KeyboardInput(VirtualKeyV, 0),
            KeyboardInput(VirtualKeyV, KeyEventKeyUp),
            KeyboardInput(VirtualKeyControl, KeyEventKeyUp)
        };

        return SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>()) == inputs.Length;
    }

    private static Input KeyboardInput(ushort virtualKey, uint flags) => new()
    {
        Type = InputKeyboard,
        Union = new InputUnion
        {
            Keyboard = new KeyboardInputData
            {
                VirtualKey = virtualKey,
                Flags = flags
            }
        }
    };

    private static Input UnicodeKeyboardInput(char character, uint flags) => new()
    {
        Type = InputKeyboard,
        Union = new InputUnion
        {
            Keyboard = new KeyboardInputData
            {
                Scan = character,
                Flags = flags
            }
        }
    };

    private const uint ClipboardFormatUnicodeText = 13;
    private const uint GlobalMoveable = 0x0002;
    private const uint InputKeyboard = 1;
    private const uint KeyEventKeyUp = 0x0002;
    private const uint KeyEventUnicode = 0x0004;
    private const ushort VirtualKeyControl = 0x11;
    private const ushort VirtualKeyV = 0x56;

    [StructLayout(LayoutKind.Sequential)]
    private struct GuiThreadInfo
    {
        public int Size;
        public int Flags;
        public IntPtr ActiveWindow;
        public IntPtr FocusWindow;
        public IntPtr CaptureWindow;
        public IntPtr MenuOwnerWindow;
        public IntPtr MoveSizeWindow;
        public IntPtr CaretWindow;
        public NativeRect CaretRectangle;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Input
    {
        public uint Type;
        public InputUnion Union;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public KeyboardInputData Keyboard;

        [FieldOffset(0)]
        public MouseInputData Mouse;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardInputData
    {
        public ushort VirtualKey;
        public ushort Scan;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MouseInputData
    {
        public int X;
        public int Y;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetGUIThreadInfo(uint threadId, ref GuiThreadInfo info);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool IsWindow(IntPtr window);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetForegroundWindow(IntPtr window);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool OpenClipboard(IntPtr newOwner);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool CloseClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EmptyClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool IsClipboardFormatAvailable(uint format);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetClipboardData(uint format);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetClipboardData(uint format, IntPtr memory);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalAlloc(uint flags, UIntPtr bytes);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalLock(IntPtr memory);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalUnlock(IntPtr memory);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalFree(IntPtr memory);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint inputCount, Input[] inputs, int inputSize);
}
