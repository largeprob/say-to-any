using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using pc.Models;

namespace pc.ViewModels;

public partial class MicrophoneOptionViewModel : ViewModelBase
{
    private static readonly IBrush AccentBrush = new SolidColorBrush(Color.FromRgb(0x12, 0x6D, 0xFF));
    private static readonly IBrush AccentSoftBrush = new SolidColorBrush(Color.FromRgb(0xF3, 0xF8, 0xFF));
    private static readonly IBrush BorderBrush = new SolidColorBrush(Color.FromRgb(0xE1, 0xEB, 0xF8));
    private static readonly IBrush WhiteBrush = new SolidColorBrush(Colors.White);
    private static readonly IBrush TitleBrush = new SolidColorBrush(Color.FromRgb(0x10, 0x2A, 0x56));

    private readonly Action<MicrophoneOptionViewModel> select;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RowBackground))]
    [NotifyPropertyChangedFor(nameof(RowBorderBrush))]
    [NotifyPropertyChangedFor(nameof(SelectionBackground))]
    [NotifyPropertyChangedFor(nameof(SelectionBorderBrush))]
    [NotifyPropertyChangedFor(nameof(SelectionMark))]
    [NotifyPropertyChangedFor(nameof(TitleForeground))]
    private bool isSelected;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Bar1Brush))]
    [NotifyPropertyChangedFor(nameof(Bar2Brush))]
    [NotifyPropertyChangedFor(nameof(Bar3Brush))]
    [NotifyPropertyChangedFor(nameof(Bar4Brush))]
    [NotifyPropertyChangedFor(nameof(Bar5Brush))]
    [NotifyPropertyChangedFor(nameof(Bar6Brush))]
    private int activeMeterBars;

    public MicrophoneOptionViewModel(AudioDeviceInfo device, Action<MicrophoneOptionViewModel> select)
    {
        Device = device;
        this.select = select;
    }

    public AudioDeviceInfo Device { get; }

    public string Title => Device.DisplayName;

    public string Description => Device.Description;

    public IBrush RowBackground => IsSelected ? AccentSoftBrush : WhiteBrush;

    public IBrush RowBorderBrush => IsSelected ? AccentBrush : BorderBrush;

    public IBrush SelectionBackground => IsSelected ? AccentBrush : WhiteBrush;

    public IBrush SelectionBorderBrush => IsSelected ? AccentBrush : BorderBrush;

    public string SelectionMark => IsSelected ? "✓" : string.Empty;

    public IBrush TitleForeground => IsSelected ? AccentBrush : TitleBrush;

    public IBrush Bar1Brush => GetBarBrush(1);

    public IBrush Bar2Brush => GetBarBrush(2);

    public IBrush Bar3Brush => GetBarBrush(3);

    public IBrush Bar4Brush => GetBarBrush(4);

    public IBrush Bar5Brush => GetBarBrush(5);

    public IBrush Bar6Brush => GetBarBrush(6);

    [RelayCommand]
    private void Select()
    {
        select(this);
    }

    private IBrush GetBarBrush(int index)
    {
        return ActiveMeterBars >= index ? AccentBrush : WhiteBrush;
    }
}
