namespace pc.Services;

public sealed class UnsupportedGlobalHotkeyService : IGlobalHotkeyService
{
    public event EventHandler? Pressed
    {
        add { }
        remove { }
    }

    public bool IsRunning => false;

    public void Start()
    {
    }

    public void Dispose()
    {
    }
}
