using System.Collections.ObjectModel;
using Avalonia.Controls.Notifications;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using pc.Models;
using pc.Services;
using SukiUI.Dialogs;

namespace pc.ViewModels;

public enum MainSection
{
    Home,
    History,
    Settings
}

public sealed class HistoryRetentionOption
{
    public HistoryRetentionOption(string value, string displayName)
    {
        Value = value;
        DisplayName = displayName;
    }

    public string Value { get; }

    public string DisplayName { get; }
}

public partial class MainWindowViewModel : ViewModelBase, IDisposable
{
    private const int ProcessingDotsBlinkFrames = 14;
    private const double ProcessingShimmerStep = 0.018;
    private const double ProcessingShimmerBandWidth = 0.18;

    private static readonly IBrush ActiveNavigationForeground = new SolidColorBrush(Color.FromRgb(0x12, 0x6D, 0xFF));
    private static readonly IBrush InactiveNavigationForeground = new SolidColorBrush(Color.FromRgb(0x52, 0x66, 0x81));

    private readonly SettingsStore settingsStore = new();
    private readonly OpenAiCompatibleClient apiClient = new();
    private readonly IAudioRecorder recorder;
    private readonly ITextInsertionService textInsertion;
    private readonly IGlobalHotkeyService hotkeyService;
    private readonly Func<IMicrophoneLevelMonitor> microphoneLevelMonitorFactory;
    private readonly CancellationTokenSource shutdown = new();
    private readonly DispatcherTimer waveformTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(33)
    };
    private readonly DispatcherTimer dictationProcessingTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(33)
    };

    private ApplicationDataFile appData = new();
    private CancellationTokenSource? recordingTimeout;
    private CancellationTokenSource? promptTimeout;
    private ITextInsertionTarget? dictationInsertionTarget;
    private bool isUpdatingHistoryRetentionSelection;
    private double targetAudioLevel;
    private double displayedAudioLevel;
    private double dictationProcessingProgressTarget;
    private int speechHoldTicks;
    private int dictationProcessingAnimationFrame;

    [ObservableProperty]
    private AppSettings settings = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PrimaryActionLabel))]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    [NotifyPropertyChangedFor(nameof(IsNotRecording))]
    [NotifyPropertyChangedFor(nameof(IsDictationOverlayVisible))]
    [NotifyPropertyChangedFor(nameof(IsDictationControlVisible))]
    private bool isRecording;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PrimaryActionLabel))]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    private bool isBusy;

    [ObservableProperty]
    private string statusMessage = "待机";

    [ObservableProperty]
    private string rawTranscript = string.Empty;

    [ObservableProperty]
    private string finalText = string.Empty;

    [ObservableProperty]
    private string dictationResultText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDictationOverlayVisible))]
    private bool isDictationResultVisible;

    [ObservableProperty]
    private string pasteFallbackText = string.Empty;

    [ObservableProperty]
    private bool isPasteFallbackVisible;

    [ObservableProperty]
    private string promptMessage = string.Empty;

    [ObservableProperty]
    private bool isPromptVisible;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDictationOverlayVisible))]
    [NotifyPropertyChangedFor(nameof(IsDictationControlVisible))]
    private bool isDictationProcessing;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DictationProcessingProgressScale))]
    private double dictationProcessingProgress;

    [ObservableProperty]
    private double dictationProcessingDotsOpacity = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DictationProcessingShimmerTailOffset))]
    [NotifyPropertyChangedFor(nameof(DictationProcessingShimmerPeakOffset))]
    [NotifyPropertyChangedFor(nameof(DictationProcessingShimmerLeadOffset))]
    private double dictationProcessingShimmerPosition;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedMicrophoneDisplayName))]
    private AudioDeviceInfo? selectedMicrophone;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMicrophoneDialogVisible))]
    private MicrophoneDialogViewModel? microphoneDialog;

    [ObservableProperty]
    private HistoryRetentionOption? selectedHistoryRetentionOption;

    [ObservableProperty]
    private double waveBar1Height = 6;

    [ObservableProperty]
    private double waveBar2Height = 6;

    [ObservableProperty]
    private double waveBar3Height = 6;

    [ObservableProperty]
    private double waveBar4Height = 6;

    [ObservableProperty]
    private double waveBar5Height = 6;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsHomeVisible))]
    [NotifyPropertyChangedFor(nameof(IsHistoryVisible))]
    [NotifyPropertyChangedFor(nameof(IsSettingsVisible))]
    [NotifyPropertyChangedFor(nameof(HomeNavigationForeground))]
    [NotifyPropertyChangedFor(nameof(HistoryNavigationForeground))]
    private MainSection selectedSection = MainSection.Home;

    private int waveformFrame;

    public MainWindowViewModel()
        : this(PlatformServices.Create())
    {
    }

    internal MainWindowViewModel(PlatformServiceSet platformServices)
    {
        recorder = platformServices.AudioRecorder;
        textInsertion = platformServices.TextInsertion;
        hotkeyService = platformServices.GlobalHotkey;
        microphoneLevelMonitorFactory = platformServices.MicrophoneLevelMonitorFactory;

        appData = settingsStore.Load();
        Settings = appData.Settings;
        SelectHistoryRetentionOption(Settings.HistoryRetention);

        foreach (var item in appData.History.OrderByDescending(item => item.CreatedAt))
        {
            History.Add(item);
        }

        var startupCleanup = ApplyHistoryRetention(Settings.HistoryRetention);
        if (startupCleanup.RemovedCount > 0)
        {
            SaveAppData();
        }

        RefreshHistoryView();
        RefreshMicrophones();
        waveformTimer.Tick += OnWaveformTimerTick;
        dictationProcessingTimer.Tick += OnDictationProcessingTimerTick;
        recorder.AudioLevelChanged += OnAudioLevelChanged;
        hotkeyService.Pressed += OnHotkeyPressed;
        TryStartHotkey();
        AppUpdateService.CheckForUpdatesOnStartup(
            message => Dispatcher.UIThread.Post(() => StatusMessage = message),
            shutdown.Token);
    }

    public ObservableCollection<AudioDeviceInfo> Microphones { get; } = [];

    public ObservableCollection<DictationHistoryItem> History { get; } = [];

    public ObservableCollection<DictationHistoryGroup> HistoryGroups { get; } = [];

    public ISukiDialogManager DialogManager { get; } = new SukiDialogManager();

    public IReadOnlyList<HistoryRetentionOption> HistoryRetentionOptions { get; } =
    [
        new("Never", "从不"),
        new("24Hours", "24小时"),
        new("OneWeek", "一周"),
        new("OneMonth", "一个月"),
        new("Forever", "永远")
    ];

    public string PrimaryActionLabel => IsRecording ? "停止并识别" : "开始听写";

    public bool IsIdle => !IsRecording && !IsBusy;

    public bool IsNotRecording => !IsRecording;

    public bool IsDictationOverlayVisible => IsRecording || IsDictationProcessing || IsDictationResultVisible;

    public bool IsDictationControlVisible => IsRecording || IsDictationProcessing;

    public double DictationProcessingProgressScale => Math.Clamp(DictationProcessingProgress / 100d, 0, 1);

    public double DictationProcessingShimmerTailOffset => GetDictationProcessingShimmerOffset(-ProcessingShimmerBandWidth);

    public double DictationProcessingShimmerPeakOffset => GetDictationProcessingShimmerOffset(0);

    public double DictationProcessingShimmerLeadOffset => GetDictationProcessingShimmerOffset(ProcessingShimmerBandWidth);

    public string SelectedMicrophoneDisplayName => SelectedMicrophone?.DisplayName ?? "未发现麦克风";

    public bool IsMicrophoneDialogVisible => MicrophoneDialog is not null;

    public bool IsHomeVisible => SelectedSection == MainSection.Home;

    public bool IsHistoryVisible => SelectedSection == MainSection.History;

    public bool IsSettingsVisible => SelectedSection == MainSection.Settings;

    public IBrush HomeNavigationForeground => GetNavigationForeground(MainSection.Home);

    public IBrush HistoryNavigationForeground => GetNavigationForeground(MainSection.History);

    public string WordUsageText => $"{History.Sum(item => CountWords(item.FinalText)):N0} 字";

    public bool HasHistory => History.Count > 0;

    public bool IsHistoryEmpty => History.Count == 0;

    public string HistorySummaryText => History.Count == 0
        ? "暂无听写记录。"
        : $"{History.Count} 条听写结果，按日期分组。";

    [RelayCommand]
    private async Task ToggleDictationAsync()
    {
        if (IsBusy)
        {
            return;
        }

        if (IsRecording)
        {
            await StopAndProcessAsync(pasteAfterRecognition: true);
            return;
        }

        await StartRecordingAsync();
    }

    [RelayCommand]
    private async Task FinishDictationAsync()
    {
        if (IsBusy || !IsRecording)
        {
            return;
        }

        await StopAndProcessAsync(pasteAfterRecognition: true);
    }

    [RelayCommand]
    private async Task CancelDictationAsync()
    {
        if (IsBusy || !IsRecording)
        {
            return;
        }

        try
        {
            IsBusy = true;
            StopDictationProcessing();
            IsRecording = false;
            dictationInsertionTarget = null;
            StopWaveformAnimation();
            ResetWaveform();
            recordingTimeout?.Cancel();
            StatusMessage = "取消录音中...";

            var audioFile = await recorder.StopAsync();
            TryDeleteFile(audioFile);
            StatusMessage = "录音已取消";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "录音已取消";
        }
        catch (Exception ex)
        {
            StatusMessage = $"取消失败：{ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void RefreshMicrophones()
    {
        var requestedDeviceNumber = Settings.MicrophoneDeviceNumber;
        var automaticMicrophone = AudioDeviceInfo.CreateAutomatic(recorder.GetDefaultInputDeviceName());
        var inputDevices = recorder.GetInputDevices();

        Microphones.Clear();
        Microphones.Add(automaticMicrophone);

        foreach (var device in inputDevices)
        {
            Microphones.Add(device);
        }

        SelectedMicrophone = Microphones.FirstOrDefault(device => device.DeviceNumber == requestedDeviceNumber)
            ?? automaticMicrophone;

        if (inputDevices.Count > 0)
        {
            Settings.MicrophoneDeviceNumber = SelectedMicrophone.DeviceNumber;
            StatusMessage = $"麦克风：{SelectedMicrophone.DisplayName}";
        }
        else
        {
            StatusMessage = "未发现麦克风";
        }
    }

    [RelayCommand]
    private void ShowMicrophoneDialog()
    {
        RefreshMicrophones();
        CloseMicrophoneDialog();
        MicrophoneDialog = new MicrophoneDialogViewModel(this, CloseMicrophoneDialog, microphoneLevelMonitorFactory);
    }

    [RelayCommand]
    private void CloseMicrophoneDialog()
    {
        var dialog = MicrophoneDialog;
        MicrophoneDialog = null;
        dialog?.Dispose();
    }

    [RelayCommand]
    private void ShowHome()
    {
        SelectedSection = MainSection.Home;
    }

    [RelayCommand]
    private void ShowHistory()
    {
        SelectedSection = MainSection.History;
    }

    [RelayCommand]
    private void ShowSettings()
    {
        SelectedSection = MainSection.Settings;
    }

    [RelayCommand]
    private void SaveSettings()
    {
        SaveAppData();
        StatusMessage = "设置已保存";
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            StatusMessage = "测试连接中...";
            SaveAppData();
            StatusMessage = await apiClient.TestConnectionAsync(Settings, shutdown.Token);
        }
        catch (Exception ex)
        {
            StatusMessage = $"连接失败：{ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CopyFinalTextAsync()
    {
        if (string.IsNullOrWhiteSpace(FinalText))
        {
            StatusMessage = "没有可复制的文本";
            return;
        }

        try
        {
            await textInsertion.CopyTextAsync(FinalText);
            StatusMessage = "已复制";
        }
        catch (Exception ex)
        {
            StatusMessage = $"复制失败：{ex.Message}";
        }
    }

    [RelayCommand]
    private async Task PasteFinalTextAsync()
    {
        if (string.IsNullOrWhiteSpace(FinalText))
        {
            StatusMessage = "没有可粘贴的文本";
            return;
        }

        try
        {
            await textInsertion.PasteTextAsync(FinalText);
            StatusMessage = "已粘贴";
        }
        catch (Exception ex)
        {
            StatusMessage = $"粘贴失败：{ex.Message}";
        }
    }

    [RelayCommand]
    private async Task CopyPasteFallbackTextAsync()
    {
        if (string.IsNullOrWhiteSpace(PasteFallbackText))
        {
            IsPasteFallbackVisible = false;
            return;
        }

        try
        {
            await textInsertion.CopyTextAsync(PasteFallbackText);
            IsPasteFallbackVisible = false;
            StatusMessage = "已复制";
        }
        catch (Exception ex)
        {
            StatusMessage = $"复制失败：{ex.Message}";
        }
    }

    [RelayCommand]
    private void DismissPasteFallback()
    {
        IsPasteFallbackVisible = false;
    }

    [RelayCommand]
    private async Task CopyDictationResultAsync()
    {
        if (string.IsNullOrWhiteSpace(DictationResultText))
        {
            HideDictationResult();
            return;
        }

        try
        {
            await textInsertion.CopyTextAsync(DictationResultText);
            HideDictationResult();
            StatusMessage = "已复制";
        }
        catch (Exception ex)
        {
            StatusMessage = $"复制失败：{ex.Message}";
        }
    }

    [RelayCommand]
    private void DismissDictationResult()
    {
        HideDictationResult();
    }

    public async Task<bool> CopyHistoryTextAsync(DictationHistoryItem? item)
    {
        if (item is null || string.IsNullOrWhiteSpace(item.FinalText))
        {
            StatusMessage = "没有可复制的文本";
            return false;
        }

        try
        {
            await textInsertion.CopyTextAsync(item.FinalText);
            StatusMessage = "已复制历史文本";
            return true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"复制失败：{ex.Message}";
            return false;
        }
    }

    [RelayCommand]
    public void DeleteHistoryItem(DictationHistoryItem? item)
    {
        if (item is null)
        {
            return;
        }

        if (!History.Remove(item))
        {
            return;
        }

        var audioDeleteFailed = !TryDeleteFile(item.AudioFilePath);
        RefreshHistoryView();
        SaveAppData();
        StatusMessage = audioDeleteFailed
            ? "记录已删除，音频文件删除失败"
            : "记录已删除";
    }

    public async Task DownloadHistoryAudioAsync(DictationHistoryItem? item, string destinationPath)
    {
        if (item is null || string.IsNullOrWhiteSpace(item.AudioFilePath) || !File.Exists(item.AudioFilePath))
        {
            StatusMessage = "音频文件不存在";
            return;
        }

        if (string.IsNullOrWhiteSpace(destinationPath))
        {
            return;
        }

        try
        {
            var sourcePath = Path.GetFullPath(item.AudioFilePath);
            var targetPath = Path.GetFullPath(destinationPath);

            if (string.Equals(sourcePath, targetPath, StringComparison.OrdinalIgnoreCase))
            {
                StatusMessage = "音频已在该位置";
                return;
            }

            var targetFolder = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrWhiteSpace(targetFolder))
            {
                Directory.CreateDirectory(targetFolder);
            }

            await using var source = File.OpenRead(sourcePath);
            await using var destination = File.Create(targetPath);
            await source.CopyToAsync(destination, shutdown.Token);
            StatusMessage = "音频已下载";
        }
        catch (Exception ex)
        {
            StatusMessage = $"下载失败：{ex.Message}";
        }
    }

    public void Dispose()
    {
        recordingTimeout?.Cancel();
        promptTimeout?.Cancel();
        shutdown.Cancel();
        SaveAppData();
        waveformTimer.Stop();
        dictationProcessingTimer.Stop();
        waveformTimer.Tick -= OnWaveformTimerTick;
        dictationProcessingTimer.Tick -= OnDictationProcessingTimerTick;
        hotkeyService.Pressed -= OnHotkeyPressed;
        recorder.AudioLevelChanged -= OnAudioLevelChanged;
        MicrophoneDialog?.Dispose();
        hotkeyService.Dispose();
        recorder.Dispose();
        shutdown.Dispose();
        recordingTimeout?.Dispose();
        promptTimeout?.Dispose();
    }

    partial void OnSelectedMicrophoneChanged(AudioDeviceInfo? value)
    {
        if (value is not null)
        {
            Settings.MicrophoneDeviceNumber = value.DeviceNumber;
            StatusMessage = $"麦克风：{value.DisplayName}";
        }
    }

    partial void OnSelectedHistoryRetentionOptionChanged(HistoryRetentionOption? oldValue, HistoryRetentionOption? newValue)
    {
        if (isUpdatingHistoryRetentionSelection ||
            oldValue is null ||
            newValue is null ||
            string.Equals(oldValue.Value, newValue.Value, StringComparison.Ordinal))
        {
            return;
        }

        _ = ChangeHistoryRetentionAsync(oldValue, newValue);
    }

    private async Task StartRecordingAsync()
    {
        IsPasteFallbackVisible = false;
        IsPromptVisible = false;
        HideDictationResult();
        StopDictationProcessing();
        dictationInsertionTarget = textInsertion.CaptureCurrentTarget();

        if (SelectedMicrophone is null)
        {
            RefreshMicrophones();
        }

        if (SelectedMicrophone is null)
        {
            dictationInsertionTarget = null;
            StatusMessage = "无法开始：未发现麦克风";
            return;
        }

        try
        {
            SaveAppData();
            await recorder.StartAsync(SelectedMicrophone.DeviceNumber);
            ResetWaveform();
            IsRecording = true;
            StartWaveformAnimation();
            RawTranscript = string.Empty;
            FinalText = string.Empty;
            StatusMessage = "录音中...";
            StartRecordingTimeout();
        }
        catch (Exception ex)
        {
            dictationInsertionTarget = null;
            StatusMessage = $"录音失败：{ex.Message}";
            StopWaveformAnimation();
            IsRecording = false;
            ResetWaveform();
        }
    }

    private async Task StopAndProcessAsync(bool pasteAfterRecognition)
    {
        if (!IsRecording)
        {
            return;
        }

        try
        {
            IsBusy = true;
            StartDictationProcessing();
            IsRecording = false;
            StopWaveformAnimation();
            ResetWaveform();
            recordingTimeout?.Cancel();
            StatusMessage = "结束录音中...";
            var insertionTarget = dictationInsertionTarget;

            var audioFile = await recorder.StopAsync();
            SetDictationProcessingTarget(28);
            StatusMessage = "语音识别中...";
            RawTranscript = await apiClient.TranscribeAsync(audioFile, Settings, shutdown.Token);
            SetDictationProcessingTarget(72);

            if (string.IsNullOrWhiteSpace(RawTranscript))
            {
                StopDictationProcessing();
                StatusMessage = "未识别到文本";
                return;
            }

            StatusMessage = Settings.EnableTextCleanup ? "整理文本中..." : "识别完成";
            SetDictationProcessingTarget(Settings.EnableTextCleanup ? 88 : 94);
            FinalText = await apiClient.CleanupTextAsync(RawTranscript, Settings, shutdown.Token);
            await CompleteDictationProcessingAsync();
            AddHistory(RawTranscript, FinalText, audioFile);
            SaveAppData();

            if (pasteAfterRecognition && Settings.AutoPasteAfterDictation)
            {
                await DeliverFinalTextAsync(FinalText, insertionTarget);
            }
            else
            {
                StatusMessage = "完成";
            }
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "已取消";
        }
        catch (Exception ex)
        {
            StatusMessage = $"处理失败：{ex.Message}";
        }
        finally
        {
            StopDictationProcessing();
            dictationInsertionTarget = null;
            IsBusy = false;
        }
    }

    private async Task DeliverFinalTextAsync(string text, ITextInsertionTarget? insertionTarget)
    {
        StatusMessage = "插入文本中...";

        var pasted = false;
        try
        {
            pasted = await textInsertion.TryPasteTextToCurrentFocusAsync(text, insertionTarget);
        }
        catch
        {
            pasted = false;
        }

        IsPasteFallbackVisible = false;
        if (pasted)
        {
            HideDictationResult();
            StatusMessage = "已粘贴";
            return;
        }

        ShowDictationResult(text);
        StatusMessage = "已转换，可复制";
    }

    private void ShowDictationResult(string text)
    {
        DictationResultText = text;
        IsPromptVisible = false;
        IsDictationResultVisible = true;
    }

    private void HideDictationResult()
    {
        IsDictationResultVisible = false;
        DictationResultText = string.Empty;
    }

    private void ShowPrompt(string message)
    {
        promptTimeout?.Cancel();
        promptTimeout?.Dispose();
        promptTimeout = CancellationTokenSource.CreateLinkedTokenSource(shutdown.Token);
        var token = promptTimeout.Token;

        PromptMessage = message;
        IsPromptVisible = true;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(2), token);
                if (!token.IsCancellationRequested)
                {
                    await Dispatcher.UIThread.InvokeAsync(() => IsPromptVisible = false);
                }
            }
            catch (OperationCanceledException)
            {
                // Normal path when a newer prompt replaces this one.
            }
        }, token);
    }

    private void StartRecordingTimeout()
    {
        recordingTimeout?.Cancel();
        recordingTimeout?.Dispose();
        recordingTimeout = CancellationTokenSource.CreateLinkedTokenSource(shutdown.Token);
        var token = recordingTimeout.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Clamp(Settings.MaxRecordingSeconds, 5, 600)), token);
                if (!token.IsCancellationRequested && IsRecording)
                {
                    await Dispatcher.UIThread.InvokeAsync(async () => await StopAndProcessAsync(pasteAfterRecognition: true));
                }
            }
            catch (OperationCanceledException)
            {
                // Normal path when the user stops recording manually.
            }
        }, token);
    }

    private void OnHotkeyPressed(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(async () => await ToggleDictationAsync());
    }

    private void OnWaveformTimerTick(object? sender, EventArgs e)
    {
        AnimateWaveform();
    }

    private void OnDictationProcessingTimerTick(object? sender, EventArgs e)
    {
        AnimateDictationProcessing();
    }

    private void OnAudioLevelChanged(double level)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var normalizedLevel = Math.Clamp(level, 0, 1);
            if (normalizedLevel > 0)
            {
                speechHoldTicks = 9;
                targetAudioLevel = Math.Max(targetAudioLevel, 0.34 + normalizedLevel * 0.66);
            }
        });
    }

    private void TryStartHotkey()
    {
        try
        {
            hotkeyService.Start();
        }
        catch (Exception ex)
        {
            StatusMessage = $"双击 Alt 未启用：{ex.Message}";
        }
    }

    private async Task ChangeHistoryRetentionAsync(HistoryRetentionOption oldValue, HistoryRetentionOption newValue)
    {
        if (!string.Equals(newValue.Value, "Forever", StringComparison.Ordinal) &&
            !await ConfirmHistoryRetentionChangeAsync(newValue))
        {
            SelectHistoryRetentionOption(oldValue.Value);
            return;
        }

        Settings.HistoryRetention = newValue.Value;
        var cleanup = ApplyHistoryRetention(newValue.Value);
        SaveAppData();
        StatusMessage = CreateHistoryRetentionStatus(newValue, cleanup);
    }

    private async Task<bool> ConfirmHistoryRetentionChangeAsync(HistoryRetentionOption option)
    {
        try
        {
            return await DialogManager
                .CreateDialog()
                .OfType(NotificationType.Warning)
                .WithTitle("确认更改保存历史")
                .WithContent(CreateHistoryRetentionConfirmationText(option))
                .WithYesNoResult("确认", "取消")
                .Dismiss().ByClickingBackground()
                .TryShowAsync(shutdown.Token);
        }
        catch
        {
            return false;
        }
    }

    private void SelectHistoryRetentionOption(string value)
    {
        isUpdatingHistoryRetentionSelection = true;
        SelectedHistoryRetentionOption = FindHistoryRetentionOption(value);
        isUpdatingHistoryRetentionSelection = false;
    }

    private HistoryRetentionOption FindHistoryRetentionOption(string? value)
    {
        return HistoryRetentionOptions.FirstOrDefault(option => string.Equals(option.Value, value, StringComparison.Ordinal))
            ?? HistoryRetentionOptions[^1];
    }

    private (int RemovedCount, int AudioDeleteFailures) ApplyHistoryRetention(string retentionValue)
    {
        var now = DateTimeOffset.Now;

        return retentionValue switch
        {
            "Never" => DeleteHistoryItems(History),
            "24Hours" => DeleteHistoryItems(History.Where(item => item.CreatedAt < now.AddHours(-24))),
            "OneWeek" => DeleteHistoryItems(History.Where(item => item.CreatedAt < now.AddDays(-7))),
            "OneMonth" => DeleteHistoryItems(History.Where(item => item.CreatedAt < now.AddMonths(-1))),
            _ => (0, 0)
        };
    }

    private (int RemovedCount, int AudioDeleteFailures) DeleteHistoryItems(IEnumerable<DictationHistoryItem> items)
    {
        var removedCount = 0;
        var audioDeleteFailures = 0;

        foreach (var item in items.ToList())
        {
            if (!History.Remove(item))
            {
                continue;
            }

            removedCount++;
            if (!TryDeleteFile(item.AudioFilePath))
            {
                audioDeleteFailures++;
            }
        }

        if (removedCount > 0)
        {
            RefreshHistoryView();
        }

        return (removedCount, audioDeleteFailures);
    }

    private static string CreateHistoryRetentionConfirmationText(HistoryRetentionOption option)
    {
        return option.Value switch
        {
            "Never" => "选择“从不”后，系统将删除所有历史记录，并停止保存新的口述历史。此操作不可恢复。",
            "24Hours" => "选择“24小时”后，系统将删除 24 小时之前的口述历史。此操作不可恢复。",
            "OneWeek" => "选择“一周”后，系统将删除一周之前的口述历史。此操作不可恢复。",
            "OneMonth" => "选择“一个月”后，系统将删除一个月之前的口述历史。此操作不可恢复。",
            _ => string.Empty
        };
    }

    private static string CreateHistoryRetentionStatus(
        HistoryRetentionOption option,
        (int RemovedCount, int AudioDeleteFailures) cleanup)
    {
        var failureSuffix = cleanup.AudioDeleteFailures > 0
            ? "，部分音频文件删除失败"
            : string.Empty;

        return option.Value switch
        {
            "Never" => cleanup.RemovedCount == 0
                ? "已停止保存历史"
                : $"已停止保存历史，删除 {cleanup.RemovedCount} 条记录{failureSuffix}",
            "Forever" => "历史将永久保存在此设备上",
            _ => cleanup.RemovedCount == 0
                ? $"历史保存期限已设为{option.DisplayName}，没有需要删除的过期记录"
                : $"历史保存期限已设为{option.DisplayName}，删除 {cleanup.RemovedCount} 条过期记录{failureSuffix}"
        };
    }

    private void AddHistory(string rawText, string finalText, string audioFilePath)
    {
        if (string.Equals(Settings.HistoryRetention, "Never", StringComparison.Ordinal))
        {
            TryDeleteFile(audioFilePath);
            RefreshHistoryView();
            return;
        }

        History.Insert(0, new DictationHistoryItem
        {
            CreatedAt = DateTimeOffset.Now,
            RawText = rawText,
            FinalText = finalText,
            AudioFilePath = audioFilePath
        });

        var cleanup = ApplyHistoryRetention(Settings.HistoryRetention);
        if (cleanup.RemovedCount == 0)
        {
            RefreshHistoryView();
        }
    }

    private void SaveAppData()
    {
        appData.Settings = Settings;
        appData.History = History.ToList();
        settingsStore.Save(appData);
    }

    private void RefreshHistoryView()
    {
        HistoryGroups.Clear();

        var today = DateTimeOffset.Now.LocalDateTime.Date;
        var yesterday = today.AddDays(-1);

        AddHistoryGroup("今天的录音", History.Where(item => item.CreatedAt.LocalDateTime.Date == today));
        AddHistoryGroup("昨天的录音", History.Where(item => item.CreatedAt.LocalDateTime.Date == yesterday));
        AddHistoryGroup("最近的录音", History.Where(item => item.CreatedAt.LocalDateTime.Date < yesterday));

        OnPropertyChanged(nameof(WordUsageText));
        OnPropertyChanged(nameof(HasHistory));
        OnPropertyChanged(nameof(IsHistoryEmpty));
        OnPropertyChanged(nameof(HistorySummaryText));
    }

    private void AddHistoryGroup(string title, IEnumerable<DictationHistoryItem> items)
    {
        var groupItems = items.ToList();
        if (groupItems.Count > 0)
        {
            HistoryGroups.Add(new DictationHistoryGroup(title, groupItems));
        }
    }

    private static bool TryDeleteFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return true;
        }

        try
        {
            File.Delete(filePath);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static int CountWords(string text)
    {
        return string.IsNullOrWhiteSpace(text)
            ? 0
            : text.Count(character => !char.IsWhiteSpace(character));
    }

    private double GetDictationProcessingShimmerOffset(double offset)
    {
        return Math.Clamp(DictationProcessingShimmerPosition + offset, 0, 1);
    }

    private IBrush GetNavigationForeground(MainSection section)
    {
        return SelectedSection == section
            ? ActiveNavigationForeground
            : InactiveNavigationForeground;
    }

    private void StartWaveformAnimation()
    {
        targetAudioLevel = 0;
        displayedAudioLevel = 0;

        if (!waveformTimer.IsEnabled)
        {
            waveformTimer.Start();
        }
    }

    private void StopWaveformAnimation()
    {
        waveformTimer.Stop();
        targetAudioLevel = 0;
        displayedAudioLevel = 0;
        speechHoldTicks = 0;
    }

    private void StartDictationProcessing()
    {
        DictationProcessingProgress = 4;
        dictationProcessingProgressTarget = 18;
        ResetDictationProcessingLabelAnimation();
        IsDictationProcessing = true;

        if (!dictationProcessingTimer.IsEnabled)
        {
            dictationProcessingTimer.Start();
        }
    }

    private void SetDictationProcessingTarget(double target)
    {
        if (!IsDictationProcessing)
        {
            return;
        }

        dictationProcessingProgressTarget = Math.Max(
            dictationProcessingProgressTarget,
            Math.Clamp(target, 0, 99));
    }

    private async Task CompleteDictationProcessingAsync()
    {
        if (!IsDictationProcessing)
        {
            return;
        }

        dictationProcessingProgressTarget = 100;
        DictationProcessingProgress = 100;
        await Task.Delay(120);
        StopDictationProcessing();
    }

    private void StopDictationProcessing()
    {
        dictationProcessingTimer.Stop();
        dictationProcessingProgressTarget = 0;
        DictationProcessingProgress = 0;
        ResetDictationProcessingLabelAnimation();
        IsDictationProcessing = false;
    }

    private void AnimateDictationProcessing()
    {
        if (!IsDictationProcessing)
        {
            dictationProcessingTimer.Stop();
            return;
        }

        if (dictationProcessingProgressTarget < 92 &&
            DictationProcessingProgress >= dictationProcessingProgressTarget - 0.8)
        {
            dictationProcessingProgressTarget = Math.Min(92, dictationProcessingProgressTarget + 0.12);
        }

        var delta = dictationProcessingProgressTarget - DictationProcessingProgress;
        if (delta > 0)
        {
            var step = Math.Max(0.24, delta * 0.06);
            DictationProcessingProgress = Math.Min(dictationProcessingProgressTarget, DictationProcessingProgress + step);
        }

        AnimateDictationProcessingLabel();
    }

    private void ResetDictationProcessingLabelAnimation()
    {
        dictationProcessingAnimationFrame = 0;
        DictationProcessingDotsOpacity = 1;
        DictationProcessingShimmerPosition = 0;
    }

    private void AnimateDictationProcessingLabel()
    {
        dictationProcessingAnimationFrame++;
        DictationProcessingDotsOpacity = (dictationProcessingAnimationFrame / ProcessingDotsBlinkFrames) % 2 == 0
            ? 1
            : 0;

        var nextShimmerPosition = DictationProcessingShimmerPosition + ProcessingShimmerStep;
        DictationProcessingShimmerPosition = nextShimmerPosition >= 1
            ? 0
            : nextShimmerPosition;
    }

    private void AnimateWaveform()
    {
        if (!IsRecording)
        {
            StopWaveformAnimation();
            ResetWaveform();
            return;
        }

        if (speechHoldTicks > 0)
        {
            speechHoldTicks--;
            targetAudioLevel = Math.Max(targetAudioLevel * 0.98, 0.34);
        }
        else
        {
            targetAudioLevel *= 0.84;
        }

        var smoothing = targetAudioLevel > displayedAudioLevel ? 0.34 : 0.16;
        displayedAudioLevel += (targetAudioLevel - displayedAudioLevel) * smoothing;

        waveformFrame++;
        UpdateWaveform(displayedAudioLevel);
    }

    private void UpdateWaveform(double level)
    {
        WaveBar1Height = CalculateWaveBarHeight(level, 0);
        WaveBar2Height = CalculateWaveBarHeight(level, 1);
        WaveBar3Height = CalculateWaveBarHeight(level, 2);
        WaveBar4Height = CalculateWaveBarHeight(level, 3);
        WaveBar5Height = CalculateWaveBarHeight(level, 4);
    }

    private void ResetWaveform()
    {
        waveformFrame = 0;
        targetAudioLevel = 0;
        displayedAudioLevel = 0;
        speechHoldTicks = 0;
        WaveBar1Height = 10;
        WaveBar2Height = 10;
        WaveBar3Height = 10;
        WaveBar4Height = 10;
        WaveBar5Height = 10;
    }

    private double CalculateWaveBarHeight(double level, int index)
    {
        var phase = waveformFrame * 1.05 + index * 1.2;
        var motion = (Math.Sin(phase) + 1) / 2;
        var weightedMotion = 0.18 + motion * 0.82;
        var emphasizedLevel = Math.Pow(Math.Clamp(level, 0, 1), 0.7);
        return Math.Round(10 + weightedMotion * emphasizedLevel * 34, 1);
    }
}
