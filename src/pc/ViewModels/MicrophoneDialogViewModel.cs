using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using pc.Models;
using pc.Services;

namespace pc.ViewModels;

public partial class MicrophoneDialogViewModel : ViewModelBase, IDisposable
{
    private readonly MainWindowViewModel main;
    private readonly Action close;
    private readonly MicrophoneLevelMonitor monitor = new();
    private readonly DispatcherTimer meterTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(33)
    };

    private MicrophoneOptionViewModel? selectedOption;
    private double targetMeterLevel;
    private double displayedMeterLevel;
    private int speechHoldTicks;
    private bool disposed;

    public MicrophoneDialogViewModel(MainWindowViewModel main, Action close)
    {
        this.main = main;
        this.close = close;

        IEnumerable<AudioDeviceInfo> devices = main.Microphones.Count == 0
            ? [AudioDeviceInfo.Automatic]
            : main.Microphones;

        foreach (var device in devices)
        {
            Options.Add(new MicrophoneOptionViewModel(device, SelectOption));
        }

        selectedOption = Options.FirstOrDefault(option => option.Device.DeviceNumber == main.SelectedMicrophone?.DeviceNumber)
            ?? Options.FirstOrDefault();

        if (selectedOption is not null)
        {
            ApplySelectedOption(selectedOption, updateMain: false);
        }

        monitor.AudioLevelChanged += OnMonitorAudioLevelChanged;
        meterTimer.Tick += OnMeterTimerTick;
        meterTimer.Start();
        RestartMonitor();
    }

    public ObservableCollection<MicrophoneOptionViewModel> Options { get; } = [];

    [RelayCommand]
    private void Close()
    {
        close();
    }

    private void SelectOption(MicrophoneOptionViewModel option)
    {
        if (disposed)
        {
            return;
        }

        ApplySelectedOption(option, updateMain: true);
        RestartMonitor();
    }

    private void ApplySelectedOption(MicrophoneOptionViewModel option, bool updateMain)
    {
        foreach (var item in Options)
        {
            item.IsSelected = ReferenceEquals(item, option);
            item.ActiveMeterBars = 0;
        }

        selectedOption = option;
        ResetMeterState();

        if (updateMain)
        {
            main.SelectedMicrophone = option.Device;
        }
    }

    private void RestartMonitor()
    {
        monitor.Stop();

        if (selectedOption is null)
        {
            return;
        }

        try
        {
            monitor.Start(selectedOption.Device.DeviceNumber);
        }
        catch
        {
            selectedOption.ActiveMeterBars = 0;
        }
    }

    private void OnMonitorAudioLevelChanged(double level)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (disposed)
            {
                return;
            }

            var normalizedLevel = Math.Clamp(level, 0, 1);
            if (normalizedLevel <= 0)
            {
                return;
            }

            speechHoldTicks = 8;
            targetMeterLevel = Math.Max(targetMeterLevel, 0.16 + normalizedLevel * 0.84);
        });
    }

    private void OnMeterTimerTick(object? sender, EventArgs e)
    {
        if (selectedOption is null)
        {
            return;
        }

        if (speechHoldTicks > 0)
        {
            speechHoldTicks--;
            targetMeterLevel = Math.Max(targetMeterLevel * 0.98, 0.16);
        }
        else
        {
            targetMeterLevel *= 0.82;
        }

        var smoothing = targetMeterLevel > displayedMeterLevel ? 0.36 : 0.18;
        displayedMeterLevel += (targetMeterLevel - displayedMeterLevel) * smoothing;

        var activeBars = displayedMeterLevel < 0.06
            ? 0
            : Math.Clamp((int)Math.Ceiling(displayedMeterLevel * 6), 0, 6);
        selectedOption.ActiveMeterBars = activeBars;
    }

    private void ResetMeterState()
    {
        targetMeterLevel = 0;
        displayedMeterLevel = 0;
        speechHoldTicks = 0;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        meterTimer.Stop();
        meterTimer.Tick -= OnMeterTimerTick;
        monitor.AudioLevelChanged -= OnMonitorAudioLevelChanged;
        monitor.Dispose();
    }
}
