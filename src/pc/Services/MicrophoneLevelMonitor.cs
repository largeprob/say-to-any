using NAudio.Wave;
using pc.Models;

namespace pc.Services;

public sealed class MicrophoneLevelMonitor : IDisposable
{
    private WaveInEvent? waveIn;

    public event Action<double>? AudioLevelChanged;

    public bool IsRunning => waveIn is not null;

    public void Start(int deviceNumber)
    {
        Stop();

        if (WaveInEvent.DeviceCount <= 0)
        {
            AudioLevelChanged?.Invoke(0);
            return;
        }

        var resolvedDeviceNumber = ResolveDeviceNumber(deviceNumber);
        try
        {
            StartCore(resolvedDeviceNumber);
        }
        catch
        {
            if (resolvedDeviceNumber != AudioDeviceInfo.AutomaticDeviceNumber)
            {
                throw;
            }

            Stop();
            StartCore(0);
        }
    }

    public void Stop()
    {
        Cleanup();
    }

    public void Dispose()
    {
        Cleanup();
    }

    private void StartCore(int deviceNumber)
    {
        waveIn = new WaveInEvent
        {
            DeviceNumber = deviceNumber,
            WaveFormat = new WaveFormat(16000, 16, 1),
            BufferMilliseconds = 80
        };

        waveIn.DataAvailable += OnDataAvailable;

        try
        {
            waveIn.StartRecording();
        }
        catch
        {
            Cleanup();
            throw;
        }
    }

    private static int ResolveDeviceNumber(int deviceNumber)
    {
        if (deviceNumber == AudioDeviceInfo.AutomaticDeviceNumber)
        {
            return AudioDeviceInfo.AutomaticDeviceNumber;
        }

        return Math.Clamp(deviceNumber, 0, WaveInEvent.DeviceCount - 1);
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        AudioLevelChanged?.Invoke(AudioLevelCalculator.Calculate(e.Buffer, e.BytesRecorded));
    }

    private void Cleanup()
    {
        var current = waveIn;
        waveIn = null;

        if (current is not null)
        {
            current.DataAvailable -= OnDataAvailable;
            try
            {
                current.StopRecording();
            }
            catch
            {
                // The capture device can already be stopped when the dialog closes.
            }

            current.Dispose();
        }

        AudioLevelChanged?.Invoke(0);
    }
}
