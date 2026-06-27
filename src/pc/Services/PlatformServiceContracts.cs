using pc.Models;

namespace pc.Services;

public interface IAudioRecorder : IDisposable
{
    event Action<double>? AudioLevelChanged;

    bool IsRecording { get; }

    IReadOnlyList<AudioDeviceInfo> GetInputDevices();

    string? GetDefaultInputDeviceName();

    Task StartAsync(int deviceNumber);

    Task<string> StopAsync();
}

public interface IMicrophoneLevelMonitor : IDisposable
{
    event Action<double>? AudioLevelChanged;

    bool IsRunning { get; }

    void Start(int deviceNumber);

    void Stop();
}

public interface IGlobalHotkeyService : IDisposable
{
    event EventHandler? Pressed;

    bool IsRunning { get; }

    void Start();
}

public interface ITextInsertionTarget;

public interface ITextInsertionService
{
    Task CopyTextAsync(string text);

    Task PasteTextAsync(string text);

    ITextInsertionTarget? CaptureCurrentTarget();

    Task<bool> TryPasteTextToCurrentFocusAsync(string text, ITextInsertionTarget? preferredTarget = null);
}

public sealed class PlatformServiceSet
{
    public PlatformServiceSet(
        IAudioRecorder audioRecorder,
        ITextInsertionService textInsertion,
        IGlobalHotkeyService globalHotkey,
        Func<IMicrophoneLevelMonitor> microphoneLevelMonitorFactory)
    {
        AudioRecorder = audioRecorder;
        TextInsertion = textInsertion;
        GlobalHotkey = globalHotkey;
        MicrophoneLevelMonitorFactory = microphoneLevelMonitorFactory;
    }

    public IAudioRecorder AudioRecorder { get; }

    public ITextInsertionService TextInsertion { get; }

    public IGlobalHotkeyService GlobalHotkey { get; }

    public Func<IMicrophoneLevelMonitor> MicrophoneLevelMonitorFactory { get; }
}

public static class PlatformServices
{
    public static PlatformServiceSet Create()
    {
        if (OperatingSystem.IsMacOS())
        {
            return new PlatformServiceSet(
                new MacMicrophoneRecorder(),
                new MacTextInsertionService(),
                new MacGlobalHotkeyService(),
                () => new MacMicrophoneLevelMonitor());
        }

        return new PlatformServiceSet(
            new WindowsMicrophoneRecorder(),
            new WindowsTextInsertionService(),
            new WindowsGlobalHotkeyService(),
            () => new WindowsMicrophoneLevelMonitor());
    }
}
