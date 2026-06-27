namespace pc.Services;

public sealed class MacMicrophoneLevelMonitor : IMicrophoneLevelMonitor
{
    public event Action<double>? AudioLevelChanged;

    public bool IsRunning { get; private set; }

    public void Start(int deviceNumber)
    {
        IsRunning = true;
        AudioLevelChanged?.Invoke(0);
    }

    public void Stop()
    {
        IsRunning = false;
        AudioLevelChanged?.Invoke(0);
    }

    public void Dispose()
    {
        Stop();
    }
}
