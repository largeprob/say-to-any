using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SukiUI.Dialogs;

namespace pc.ViewModels;

public enum PreferencesSection
{
    Account,
    Settings,
    About
}

public partial class PreferencesDialogViewModel : ViewModelBase
{
    private static readonly IBrush ActiveNavigationForeground = new SolidColorBrush(Color.FromRgb(0x12, 0x6D, 0xFF));
    private static readonly IBrush InactiveNavigationForeground = new SolidColorBrush(Color.FromRgb(0x52, 0x66, 0x81));

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAccountVisible))]
    [NotifyPropertyChangedFor(nameof(IsSettingsVisible))]
    [NotifyPropertyChangedFor(nameof(IsAboutVisible))]
    [NotifyPropertyChangedFor(nameof(AccountNavigationForeground))]
    [NotifyPropertyChangedFor(nameof(SettingsNavigationForeground))]
    [NotifyPropertyChangedFor(nameof(AboutNavigationForeground))]
    private PreferencesSection selectedSection;

    private readonly ISukiDialog dialog;

    public PreferencesDialogViewModel(MainWindowViewModel main, PreferencesSection selectedSection, ISukiDialog dialog)
    {
        Main = main;
        this.dialog = dialog;
        this.selectedSection = selectedSection;
    }

    public MainWindowViewModel Main { get; }

    public string[] AppLanguages { get; } = ["简体中文", "English"];

    public string RecordingHotkeyText => "双击 Alt";

    public string TranslationHotkeyText => "未设置";

    public bool IsAccountVisible => SelectedSection == PreferencesSection.Account;

    public bool IsSettingsVisible => SelectedSection == PreferencesSection.Settings;

    public bool IsAboutVisible => SelectedSection == PreferencesSection.About;

    public IBrush AccountNavigationForeground => GetNavigationForeground(PreferencesSection.Account);

    public IBrush SettingsNavigationForeground => GetNavigationForeground(PreferencesSection.Settings);

    public IBrush AboutNavigationForeground => GetNavigationForeground(PreferencesSection.About);

    [RelayCommand]
    private void ShowAccount()
    {
        SelectedSection = PreferencesSection.Account;
    }

    [RelayCommand]
    private void ShowSettings()
    {
        SelectedSection = PreferencesSection.Settings;
    }

    [RelayCommand]
    private void ShowAbout()
    {
        SelectedSection = PreferencesSection.About;
    }

    [RelayCommand]
    private void CloseDialog()
    {
        dialog.Dismiss();
    }

    private IBrush GetNavigationForeground(PreferencesSection section)
    {
        return SelectedSection == section
            ? ActiveNavigationForeground
            : InactiveNavigationForeground;
    }
}
