using System.ComponentModel;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using pc.Models;
using pc.ViewModels;
using SukiUI.Dialogs;

namespace pc.Views;

public partial class MainWindow : Window
{
    private const string HistoryRecordClass = "history-record";
    private const string ActionsOpenClass = "actions-open";
    private const string MenuOpenClass = "menu-open";
    private const string HistoryMoreMenuClass = "history-more-menu";
    private const string DangerMenuItemClass = "danger-menu-item";
    private const int WindowsBorderColorAttribute = 34;
    private const int WindowsCaptionColorAttribute = 35;
    private const int WindowsCaptionTextColorAttribute = 36;
    private const int AppChromeColor = 0x00FBF9F8;
    private const int AppChromeTextColor = 0x00562A10;

    private static readonly IBrush HistoryMenuTextBrush = new SolidColorBrush(Color.FromRgb(0x10, 0x2A, 0x56));
    private static readonly IBrush HistoryMenuIconBrush = new SolidColorBrush(Color.FromRgb(0x52, 0x66, 0x81));
    private static readonly IBrush DangerBrush = new SolidColorBrush(Color.FromRgb(0xDC, 0x26, 0x26));

    private static readonly Geometry DownloadIconGeometry = StreamGeometry.Parse("M12 3v11m0 0 4-4m-4 4-4-4M5 17v2a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2v-2");
    private static readonly Geometry DeleteIconGeometry = StreamGeometry.Parse("M4 7h16M10 11v6M14 11v6M6 7l1 14h10l1-14M9 7V4h6v3");

    private RecordingOverlayWindow? recordingOverlay;
    private PasteFallbackOverlayWindow? pasteFallbackOverlay;
    private PromptOverlayWindow? promptOverlay;
    private MainWindowViewModel? overlayViewModel;

    public MainWindow()
    {
        InitializeComponent();
        Opened += (_, _) => ApplyWindowsChromeColors();
        DataContextChanged += (_, _) => SyncRecordingOverlaySubscription();
        Closed += (_, _) => CloseOverlayWindows();
    }

    private void SyncRecordingOverlaySubscription()
    {
        if (overlayViewModel is not null)
        {
            overlayViewModel.PropertyChanged -= OnOverlayViewModelPropertyChanged;
        }

        overlayViewModel = DataContext as MainWindowViewModel;
        if (overlayViewModel is not null)
        {
            overlayViewModel.PropertyChanged += OnOverlayViewModelPropertyChanged;
        }

        UpdateRecordingOverlayVisibility();
        UpdatePasteFallbackOverlayVisibility();
        UpdatePromptOverlayVisibility();
    }

    private void OnOverlayViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.IsDictationOverlayVisible))
        {
            UpdateRecordingOverlayVisibility();
        }

        if (e.PropertyName == nameof(MainWindowViewModel.IsPasteFallbackVisible) ||
            e.PropertyName == nameof(MainWindowViewModel.PasteFallbackText))
        {
            UpdatePasteFallbackOverlayVisibility();
        }

        if (e.PropertyName == nameof(MainWindowViewModel.IsPromptVisible) ||
            e.PropertyName == nameof(MainWindowViewModel.PromptMessage))
        {
            UpdatePromptOverlayVisibility();
        }
    }

    private void UpdateRecordingOverlayVisibility()
    {
        if (overlayViewModel?.IsDictationOverlayVisible == true)
        {
            ShowRecordingOverlay(overlayViewModel);
            return;
        }

        HideRecordingOverlay();
    }

    private void ShowRecordingOverlay(MainWindowViewModel viewModel)
    {
        recordingOverlay ??= new RecordingOverlayWindow();
        recordingOverlay.DataContext = viewModel;
        recordingOverlay.Topmost = true;

        if (!recordingOverlay.IsVisible)
        {
            recordingOverlay.Show();
        }

        recordingOverlay.PositionNearBottomCenter(this);
    }

    private void HideRecordingOverlay()
    {
        if (recordingOverlay?.IsVisible == true)
        {
            recordingOverlay.Hide();
        }
    }

    private void UpdatePasteFallbackOverlayVisibility()
    {
        if (overlayViewModel?.IsPasteFallbackVisible == true)
        {
            ShowPasteFallbackOverlay(overlayViewModel);
            return;
        }

        HidePasteFallbackOverlay();
    }

    private void ShowPasteFallbackOverlay(MainWindowViewModel viewModel)
    {
        pasteFallbackOverlay ??= new PasteFallbackOverlayWindow();
        pasteFallbackOverlay.DataContext = viewModel;
        pasteFallbackOverlay.Topmost = true;

        if (!pasteFallbackOverlay.IsVisible)
        {
            pasteFallbackOverlay.Show();
        }

        pasteFallbackOverlay.PositionNearBottomCenter(this);
    }

    private void HidePasteFallbackOverlay()
    {
        if (pasteFallbackOverlay?.IsVisible == true)
        {
            pasteFallbackOverlay.Hide();
        }
    }

    private void UpdatePromptOverlayVisibility()
    {
        if (overlayViewModel?.IsPromptVisible == true)
        {
            ShowPromptOverlay(overlayViewModel);
            return;
        }

        HidePromptOverlay();
    }

    private void ShowPromptOverlay(MainWindowViewModel viewModel)
    {
        promptOverlay ??= new PromptOverlayWindow();
        promptOverlay.DataContext = viewModel;
        promptOverlay.Topmost = true;

        if (!promptOverlay.IsVisible)
        {
            promptOverlay.Show();
        }

        promptOverlay.PositionNearBottomCenter(this);
    }

    private void HidePromptOverlay()
    {
        if (promptOverlay?.IsVisible == true)
        {
            promptOverlay.Hide();
        }
    }

    private void CloseRecordingOverlay()
    {
        if (overlayViewModel is not null)
        {
            overlayViewModel.PropertyChanged -= OnOverlayViewModelPropertyChanged;
            overlayViewModel = null;
        }

        recordingOverlay?.Close();
        recordingOverlay = null;
    }

    private void CloseOverlayWindows()
    {
        CloseRecordingOverlay();
        pasteFallbackOverlay?.Close();
        pasteFallbackOverlay = null;
        promptOverlay?.Close();
        promptOverlay = null;
    }

    private void ApplyWindowsChromeColors()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var platformHandle = TryGetPlatformHandle();
        if (platformHandle is null || platformHandle.Handle == IntPtr.Zero)
        {
            return;
        }

        SetDwmWindowAttribute(platformHandle.Handle, WindowsBorderColorAttribute, AppChromeColor);
        SetDwmWindowAttribute(platformHandle.Handle, WindowsCaptionColorAttribute, AppChromeColor);
        SetDwmWindowAttribute(platformHandle.Handle, WindowsCaptionTextColorAttribute, AppChromeTextColor);
    }

    private static void SetDwmWindowAttribute(IntPtr windowHandle, int attribute, int color)
    {
        _ = DwmSetWindowAttribute(windowHandle, attribute, ref color, Marshal.SizeOf<int>());
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);

    private void OpenAccountDialog(object? sender, RoutedEventArgs e)
    {
        ShowPreferencesDialog(PreferencesSection.Account);
    }

    private void OpenSettingsDialog(object? sender, RoutedEventArgs e)
    {
        ShowPreferencesDialog(PreferencesSection.Settings);
    }

    private async void CopyHistoryItem(object? sender, RoutedEventArgs e)
    {
        if (sender is not Control control ||
            control.DataContext is not DictationHistoryItem item ||
            DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        var historyRecord = FindVisualAncestorWithClass<Border>(control, HistoryRecordClass);
        KeepHistoryActionsOpen(historyRecord);

        var copied = await viewModel.CopyHistoryTextAsync(item);
        if (copied)
        {
            await ShowCopiedToolTipAsync(control);
        }

        ReleaseHistoryActions(historyRecord);
    }

    private void OpenHistoryMoreMenu(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button ||
            button.DataContext is not DictationHistoryItem item ||
            DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        var historyRecord = FindVisualAncestorWithClass<Border>(button, HistoryRecordClass);
        KeepHistoryActionsOpen(historyRecord);
        KeepMenuButtonOpen(button);

        var downloadAudio = CreateHistoryMenuItem("下载音频", DownloadIconGeometry, HistoryMenuIconBrush, HistoryMenuTextBrush);
        downloadAudio.IsEnabled = item.HasAudioFile;
        downloadAudio.Click += async (_, _) => await DownloadHistoryAudioAsync(item);

        var deleteRecord = CreateHistoryMenuItem("删除记录", DeleteIconGeometry, DangerBrush, DangerBrush);
        deleteRecord.Classes.Add(DangerMenuItemClass);
        deleteRecord.Click += (_, _) => viewModel.DeleteHistoryItem(item);

        var menu = new ContextMenu
        {
            Placement = PlacementMode.BottomEdgeAlignedRight,
            Items =
            {
                downloadAudio,
                deleteRecord
            }
        };
        menu.Classes.Add(HistoryMoreMenuClass);
        menu.Closed += (_, _) =>
        {
            ReleaseHistoryActions(historyRecord);
            ReleaseMenuButton(button);
        };

        button.ContextMenu = menu;
        menu.Open(button);
    }

    private async Task DownloadHistoryAudioAsync(DictationHistoryItem item)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        if (!item.HasAudioFile)
        {
            viewModel.StatusMessage = "音频文件不存在";
            return;
        }

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "下载音频",
            SuggestedFileName = item.AudioFileName,
            FileTypeChoices =
            [
                new FilePickerFileType("WAV 音频")
                {
                    Patterns = ["*.wav"]
                }
            ]
        });

        if (file is null)
        {
            return;
        }

        await viewModel.DownloadHistoryAudioAsync(item, file.Path.LocalPath);
    }

    private static MenuItem CreateHistoryMenuItem(string header, Geometry iconGeometry, IBrush iconBrush, IBrush foreground)
    {
        return new MenuItem
        {
            Header = header,
            Icon = CreateMenuIcon(iconGeometry, iconBrush),
            Foreground = foreground
        };
    }

    private static Viewbox CreateMenuIcon(Geometry geometry, IBrush brush)
    {
        return new Viewbox
        {
            Width = 16,
            Height = 16,
            Stretch = Stretch.Uniform,
            Child = new Canvas
            {
                Width = 24,
                Height = 24,
                Children =
                {
                    new Avalonia.Controls.Shapes.Path
                    {
                        Data = geometry,
                        Stroke = brush,
                        StrokeThickness = 2,
                        StrokeLineCap = PenLineCap.Round,
                        StrokeJoin = PenLineJoin.Round
                    }
                }
            }
        };
    }

    private static async Task ShowCopiedToolTipAsync(Control control)
    {
        ToolTip.SetTip(control, "已复制");
        ToolTip.SetIsOpen(control, true);

        await Task.Delay(1200);

        ToolTip.SetIsOpen(control, false);
        ToolTip.SetTip(control, "复制");
    }

    private static void KeepHistoryActionsOpen(Control? historyRecord)
    {
        if (historyRecord is not null && !historyRecord.Classes.Contains(ActionsOpenClass))
        {
            historyRecord.Classes.Add(ActionsOpenClass);
        }
    }

    private static void KeepMenuButtonOpen(Control button)
    {
        if (!button.Classes.Contains(MenuOpenClass))
        {
            button.Classes.Add(MenuOpenClass);
        }
    }

    private static void ReleaseHistoryActions(Control? historyRecord)
    {
        historyRecord?.Classes.Remove(ActionsOpenClass);
    }

    private static void ReleaseMenuButton(Control button)
    {
        button.Classes.Remove(MenuOpenClass);
    }

    private static T? FindVisualAncestorWithClass<T>(Control control, string className)
        where T : Control
    {
        for (Control? current = control; current is not null; current = current.GetVisualParent() as Control)
        {
            if (current is T typed && current.Classes.Contains(className))
            {
                return typed;
            }
        }

        return null;
    }

    private void ShowPreferencesDialog(PreferencesSection selectedSection)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        if (!AppShell.Classes.Contains("modal-open"))
        {
            AppShell.Classes.Add("modal-open");
        }

        var shown = viewModel.DialogManager
            .CreateDialog()
            .WithViewModel(dialog => new PreferencesDialogViewModel(viewModel, selectedSection, dialog))
            .ShowCardBackground(false)
            .Dismiss().ByClickingBackground()
            .OnDismissed(_ => AppShell.Classes.Remove("modal-open"))
            .TryShow();

        if (!shown)
        {
            AppShell.Classes.Remove("modal-open");
        }
    }
}
