using NAudio.Wave;
using NAudio.CoreAudioApi;
using pc.Models;

namespace pc.Services;

public sealed class WindowsMicrophoneRecorder : IAudioRecorder
{
    private WaveInEvent? waveIn;
    private WaveFileWriter? writer;
    private TaskCompletionSource<Exception?>? stopCompletion;
    private string? currentFilePath;

    public event Action<double>? AudioLevelChanged;

    public bool IsRecording => waveIn is not null;

    public IReadOnlyList<AudioDeviceInfo> GetInputDevices()
    {
        var devices = new List<AudioDeviceInfo>();
        for (var i = 0; i < WaveInEvent.DeviceCount; i++)
        {
            var capabilities = WaveInEvent.GetCapabilities(i);
            devices.Add(new AudioDeviceInfo(i, capabilities.ProductName));
        }

        return devices;
    }

    public string? GetDefaultInputDeviceName()
    {
        if (WaveInEvent.DeviceCount <= 0)
        {
            return null;
        }

        try
        {
            if (OperatingSystem.IsWindows())
            {
                using var enumerator = new MMDeviceEnumerator();
                var endpoint = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Multimedia);
                if (!string.IsNullOrWhiteSpace(endpoint.FriendlyName))
                {
                    return endpoint.FriendlyName;
                }
            }
        }
        catch
        {
            // Fall back to WinMM capabilities below.
        }

        try
        {
            var capabilities = WaveInEvent.GetCapabilities(AudioDeviceInfo.AutomaticDeviceNumber);
            return string.IsNullOrWhiteSpace(capabilities.ProductName)
                ? null
                : capabilities.ProductName;
        }
        catch
        {
            return GetInputDevices().FirstOrDefault()?.Name;
        }
    }

    public Task StartAsync(int deviceNumber)
    {
        if (IsRecording)
        {
            throw new InvalidOperationException("Recording is already in progress.");
        }

        currentFilePath = RecordingFilePaths.CreateRecordingFilePath();

        waveIn = new WaveInEvent
        {
            DeviceNumber = ResolveDeviceNumber(deviceNumber),
            WaveFormat = new WaveFormat(16000, 16, 1),
            BufferMilliseconds = 100
        };

        writer = new WaveFileWriter(currentFilePath, waveIn.WaveFormat);
        waveIn.DataAvailable += OnDataAvailable;
        waveIn.RecordingStopped += OnRecordingStopped;
        stopCompletion = new TaskCompletionSource<Exception?>(TaskCreationOptions.RunContinuationsAsynchronously);
        waveIn.StartRecording();

        return Task.CompletedTask;
    }

    public async Task<string> StopAsync()
    {
        if (!IsRecording || currentFilePath is null || stopCompletion is null)
        {
            throw new InvalidOperationException("Recording has not started.");
        }

        waveIn!.StopRecording();
        var exception = await stopCompletion.Task;
        if (exception is not null)
        {
            throw exception;
        }

        return currentFilePath;
    }

    public void Dispose()
    {
        Cleanup();
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        writer?.Write(e.Buffer, 0, e.BytesRecorded);
        writer?.Flush();
        AudioLevelChanged?.Invoke(AudioLevelCalculator.Calculate(e.Buffer, e.BytesRecorded));
    }

    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        var completion = stopCompletion;
        var exception = e.Exception;

        Cleanup();
        completion?.TrySetResult(exception);
    }

    private void Cleanup()
    {
        if (waveIn is not null)
        {
            waveIn.DataAvailable -= OnDataAvailable;
            waveIn.RecordingStopped -= OnRecordingStopped;
            waveIn.Dispose();
            waveIn = null;
        }

        writer?.Dispose();
        writer = null;
        stopCompletion = null;
        AudioLevelChanged?.Invoke(0);
    }

    private static int ResolveDeviceNumber(int deviceNumber)
    {
        if (deviceNumber == AudioDeviceInfo.AutomaticDeviceNumber)
        {
            return AudioDeviceInfo.AutomaticDeviceNumber;
        }

        return WaveInEvent.DeviceCount <= 0
            ? 0
            : Math.Clamp(deviceNumber, 0, WaveInEvent.DeviceCount - 1);
    }
}
