using System.Diagnostics;
using System.Runtime.InteropServices;

namespace pc.Services;

public sealed class WindowsGlobalHotkeyService : IGlobalHotkeyService
{
    private const int LowLevelKeyboardHook = 13;
    private const int KeyDown = 0x0100;
    private const int KeyUp = 0x0101;
    private const int SystemKeyDown = 0x0104;
    private const int SystemKeyUp = 0x0105;
    private const uint InputKeyboard = 1;
    private const int InjectedKeyFlag = 0x10;
    private const int VirtualKeyAlt = 0x12;
    private const int VirtualKeyLeftAlt = 0xA4;
    private const int VirtualKeyRightAlt = 0xA5;

    private readonly LowLevelKeyboardProc hookProc;
    private IntPtr hookHandle;
    private DateTimeOffset lastAltTap = DateTimeOffset.MinValue;
    private DateTimeOffset lastFired = DateTimeOffset.MinValue;
    private bool altDown;
    private bool altChordInProgress;
    private bool forwardedAltDown;
    private int activeAltKeyCode = VirtualKeyAlt;

    public WindowsGlobalHotkeyService()
    {
        hookProc = HookCallback;
    }

    public event EventHandler? Pressed;

    public bool IsRunning => hookHandle != IntPtr.Zero;

    public void Start()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        if (hookHandle != IntPtr.Zero)
        {
            return;
        }

        using var process = Process.GetCurrentProcess();
        using var module = process.MainModule;
        var moduleHandle = module?.ModuleName is null ? IntPtr.Zero : GetModuleHandle(module.ModuleName);
        hookHandle = SetWindowsHookEx(LowLevelKeyboardHook, hookProc, moduleHandle, 0);

        if (hookHandle == IntPtr.Zero)
        {
            throw new InvalidOperationException("Cannot install global Alt hook.");
        }
    }

    public void Dispose()
    {
        if (hookHandle != IntPtr.Zero)
        {
            UnhookWindowsHookEx(hookHandle);
            hookHandle = IntPtr.Zero;
        }
    }

    private IntPtr HookCallback(int code, IntPtr wordParam, IntPtr longParam)
    {
        var suppressKey = false;
        if (code >= 0)
        {
            var keyInfo = Marshal.PtrToStructure<KeyboardHookInfo>(longParam);
            if ((keyInfo.Flags & InjectedKeyFlag) != 0)
            {
                return CallNextHookEx(hookHandle, code, wordParam, longParam);
            }

            if (wordParam == KeyDown || wordParam == SystemKeyDown)
            {
                suppressKey = OnKeyDown(keyInfo.VirtualKey);
            }
            else if (wordParam == KeyUp || wordParam == SystemKeyUp)
            {
                suppressKey = OnKeyUp(keyInfo.VirtualKey);
            }
        }

        if (suppressKey)
        {
            return (IntPtr)1;
        }

        return CallNextHookEx(hookHandle, code, wordParam, longParam);
    }

    private bool OnKeyDown(int keyCode)
    {
        if (IsAltKey(keyCode))
        {
            if (!altDown)
            {
                altDown = true;
                altChordInProgress = false;
                forwardedAltDown = false;
                activeAltKeyCode = keyCode;
            }

            return true;
        }

        if (altDown)
        {
            altChordInProgress = true;
            if (!forwardedAltDown)
            {
                SendKeyDown(activeAltKeyCode);
                forwardedAltDown = true;
            }
        }

        return false;
    }

    private bool OnKeyUp(int keyCode)
    {
        if (!IsAltKey(keyCode))
        {
            return false;
        }

        var wasPlainAltTap = altDown && !altChordInProgress;
        altDown = false;
        altChordInProgress = false;
        forwardedAltDown = false;
        activeAltKeyCode = VirtualKeyAlt;

        if (!wasPlainAltTap)
        {
            lastAltTap = DateTimeOffset.MinValue;
            return false;
        }

        var now = DateTimeOffset.Now;
        if (now - lastAltTap <= TimeSpan.FromMilliseconds(500) &&
            now - lastFired > TimeSpan.FromMilliseconds(650))
        {
            lastFired = now;
            lastAltTap = DateTimeOffset.MinValue;
            Pressed?.Invoke(this, EventArgs.Empty);
            return true;
        }

        lastAltTap = now;
        return true;
    }

    private static bool IsAltKey(int keyCode)
    {
        return keyCode is VirtualKeyAlt or VirtualKeyLeftAlt or VirtualKeyRightAlt;
    }

    private static void SendKeyDown(int keyCode)
    {
        var inputs = new[]
        {
            new Input
            {
                Type = InputKeyboard,
                Union = new InputUnion
                {
                    Keyboard = new KeyboardInputData
                    {
                        VirtualKey = (ushort)keyCode
                    }
                }
            }
        };

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>());
    }

    private delegate IntPtr LowLevelKeyboardProc(int code, IntPtr wordParam, IntPtr longParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardHookInfo
    {
        public int VirtualKey;
        public int ScanCode;
        public int Flags;
        public int Time;
        public IntPtr ExtraInfo;
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

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int hookId, LowLevelKeyboardProc procedure, IntPtr module, uint threadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hook);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr wordParam, IntPtr longParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string moduleName);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint inputCount, Input[] inputs, int inputSize);
}
