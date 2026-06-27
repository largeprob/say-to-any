using System.Runtime.InteropServices;

namespace pc.Services;

public sealed class MacGlobalHotkeyService : IGlobalHotkeyService
{
    private const int EventTapSession = 1;
    private const int HeadInsertEventTap = 0;
    private const int EventTapOptionListenOnly = 1;
    private const int EventKeyDown = 10;
    private const int EventFlagsChanged = 12;
    private const int KeyboardEventKeycode = 9;
    private const int KeyCodeLeftOption = 58;
    private const int KeyCodeRightOption = 61;
    private const ulong EventFlagMaskAlternate = 0x00080000;
    private const uint Utf8Encoding = 0x08000100;

    private readonly CGEventTapCallBack callback;
    private readonly object sync = new();

    private Thread? eventTapThread;
    private IntPtr runLoop;
    private IntPtr eventTap;
    private IntPtr runLoopSource;
    private IntPtr runLoopMode;
    private Exception? startupException;
    private DateTimeOffset lastOptionTap = DateTimeOffset.MinValue;
    private DateTimeOffset lastFired = DateTimeOffset.MinValue;
    private bool optionDown;
    private bool optionChordInProgress;
    private volatile bool isRunning;

    public MacGlobalHotkeyService()
    {
        callback = OnEventTapCallback;
    }

    public event EventHandler? Pressed;

    public bool IsRunning => isRunning;

    public void Start()
    {
        if (!OperatingSystem.IsMacOS())
        {
            return;
        }

        lock (sync)
        {
            if (eventTapThread is not null)
            {
                return;
            }

            using var started = new ManualResetEventSlim();
            startupException = null;
            eventTapThread = new Thread(() => RunEventTapLoop(started))
            {
                IsBackground = true,
                Name = "Say To Any macOS hotkey"
            };
            eventTapThread.Start();

            if (!started.Wait(TimeSpan.FromSeconds(3)))
            {
                StopCore();
                throw new InvalidOperationException("macOS hotkey listener did not start.");
            }

            if (startupException is not null)
            {
                StopCore();
                throw startupException;
            }
        }
    }

    public void Dispose()
    {
        lock (sync)
        {
            StopCore();
        }
    }

    private void RunEventTapLoop(ManualResetEventSlim started)
    {
        try
        {
            var eventMask = (1UL << EventKeyDown) | (1UL << EventFlagsChanged);
            eventTap = CGEventTapCreate(
                EventTapSession,
                HeadInsertEventTap,
                EventTapOptionListenOnly,
                eventMask,
                callback,
                IntPtr.Zero);

            if (eventTap == IntPtr.Zero)
            {
                startupException = new InvalidOperationException(
                    "无法启用 macOS 双击 Option 快捷键，请在系统设置中授予辅助功能权限。");
                started.Set();
                return;
            }

            runLoopSource = CFMachPortCreateRunLoopSource(IntPtr.Zero, eventTap, 0);
            runLoop = CFRunLoopGetCurrent();
            runLoopMode = CFStringCreateWithCString(IntPtr.Zero, "kCFRunLoopDefaultMode", Utf8Encoding);
            CFRunLoopAddSource(runLoop, runLoopSource, runLoopMode);
            CGEventTapEnable(eventTap, true);
            isRunning = true;
            started.Set();
            CFRunLoopRun();
        }
        catch (Exception ex)
        {
            startupException = ex;
            started.Set();
        }
        finally
        {
            isRunning = false;
            ReleaseNativeHandles();
        }
    }

    private IntPtr OnEventTapCallback(IntPtr proxy, int type, IntPtr eventRef, IntPtr userInfo)
    {
        try
        {
            if (type == EventFlagsChanged)
            {
                HandleFlagsChanged(eventRef);
            }
            else if (type == EventKeyDown && optionDown)
            {
                optionChordInProgress = true;
            }
        }
        catch
        {
            // Never let a native callback exception escape into CoreGraphics.
        }

        return eventRef;
    }

    private void HandleFlagsChanged(IntPtr eventRef)
    {
        var keyCode = (int)CGEventGetIntegerValueField(eventRef, KeyboardEventKeycode);
        if (keyCode is not KeyCodeLeftOption and not KeyCodeRightOption)
        {
            return;
        }

        var optionDownNow = (CGEventGetFlags(eventRef) & EventFlagMaskAlternate) != 0;
        if (optionDownNow)
        {
            if (!optionDown)
            {
                optionDown = true;
                optionChordInProgress = false;
            }

            return;
        }

        var wasPlainOptionTap = optionDown && !optionChordInProgress;
        optionDown = false;
        optionChordInProgress = false;

        if (!wasPlainOptionTap)
        {
            lastOptionTap = DateTimeOffset.MinValue;
            return;
        }

        var now = DateTimeOffset.Now;
        if (now - lastOptionTap <= TimeSpan.FromMilliseconds(500) &&
            now - lastFired > TimeSpan.FromMilliseconds(650))
        {
            lastFired = now;
            lastOptionTap = DateTimeOffset.MinValue;
            ThreadPool.QueueUserWorkItem(_ => Pressed?.Invoke(this, EventArgs.Empty));
            return;
        }

        lastOptionTap = now;
    }

    private void StopCore()
    {
        if (runLoop != IntPtr.Zero)
        {
            CFRunLoopStop(runLoop);
        }

        if (eventTapThread is not null && eventTapThread.IsAlive)
        {
            eventTapThread.Join(TimeSpan.FromSeconds(2));
        }

        if (eventTapThread is not null && !eventTapThread.IsAlive)
        {
            eventTapThread = null;
        }
    }

    private void ReleaseNativeHandles()
    {
        if (runLoopMode != IntPtr.Zero)
        {
            CFRelease(runLoopMode);
            runLoopMode = IntPtr.Zero;
        }

        if (runLoopSource != IntPtr.Zero)
        {
            CFRelease(runLoopSource);
            runLoopSource = IntPtr.Zero;
        }

        if (eventTap != IntPtr.Zero)
        {
            CFRelease(eventTap);
            eventTap = IntPtr.Zero;
        }

        runLoop = IntPtr.Zero;
    }

    private delegate IntPtr CGEventTapCallBack(IntPtr proxy, int type, IntPtr eventRef, IntPtr userInfo);

    [DllImport("/System/Library/Frameworks/ApplicationServices.framework/ApplicationServices")]
    private static extern IntPtr CGEventTapCreate(
        int tap,
        int place,
        int options,
        ulong eventsOfInterest,
        CGEventTapCallBack callback,
        IntPtr userInfo);

    [DllImport("/System/Library/Frameworks/ApplicationServices.framework/ApplicationServices")]
    private static extern void CGEventTapEnable(IntPtr tap, bool enable);

    [DllImport("/System/Library/Frameworks/ApplicationServices.framework/ApplicationServices")]
    private static extern long CGEventGetIntegerValueField(IntPtr eventRef, int field);

    [DllImport("/System/Library/Frameworks/ApplicationServices.framework/ApplicationServices")]
    private static extern ulong CGEventGetFlags(IntPtr eventRef);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern IntPtr CFMachPortCreateRunLoopSource(IntPtr allocator, IntPtr port, nint order);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern IntPtr CFRunLoopGetCurrent();

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern void CFRunLoopRun();

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern void CFRunLoopStop(IntPtr runLoop);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern void CFRunLoopAddSource(IntPtr runLoop, IntPtr source, IntPtr mode);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern IntPtr CFStringCreateWithCString(IntPtr allocator, string value, uint encoding);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern void CFRelease(IntPtr handle);
}
