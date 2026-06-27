using System.Diagnostics;
using pc.Models;

namespace pc.Services;

public sealed class MacMicrophoneRecorder : IAudioRecorder
{
    private const string AfrecordPath = "/usr/bin/afrecord";
    private const string KillPath = "/bin/kill";
    private const int SigIntWaitMilliseconds = 4000;

    private Process? process;
    private Task<string>? standardErrorTask;
    private string? currentFilePath;

    public event Action<double>? AudioLevelChanged;

    public bool IsRecording => process is { HasExited: false };

    public IReadOnlyList<AudioDeviceInfo> GetInputDevices()
    {
        return [new AudioDeviceInfo(0, "系统默认麦克风")];
    }

    public string? GetDefaultInputDeviceName()
    {
        return "系统默认麦克风";
    }

    public async Task StartAsync(int deviceNumber)
    {
        if (!OperatingSystem.IsMacOS())
        {
            throw new PlatformNotSupportedException("macOS recording requires macOS.");
        }

        if (IsRecording)
        {
            throw new InvalidOperationException("Recording is already in progress.");
        }

        currentFilePath = RecordingFilePaths.CreateRecordingFilePath();
        process = new Process
        {
            StartInfo = CreateAfrecordStartInfo(currentFilePath)
        };

        process.Start();
        standardErrorTask = process.StandardError.ReadToEndAsync();

        await Task.Delay(250);
        if (process.HasExited)
        {
            var error = await ReadStandardErrorAsync();
            CleanupProcess();
            throw new InvalidOperationException(CreateAfrecordErrorMessage(error));
        }

        AudioLevelChanged?.Invoke(0);
    }

    public async Task<string> StopAsync()
    {
        var activeProcess = process;
        var filePath = currentFilePath;
        if (activeProcess is null || filePath is null)
        {
            throw new InvalidOperationException("Recording has not started.");
        }

        if (!activeProcess.HasExited)
        {
            await SendInterruptAsync(activeProcess.Id);
            await WaitOrKillAsync(activeProcess);
        }

        var error = await ReadStandardErrorAsync();
        CleanupProcess();
        AudioLevelChanged?.Invoke(0);

        if (!File.Exists(filePath) || new FileInfo(filePath).Length <= 44)
        {
            throw new InvalidOperationException(CreateAfrecordErrorMessage(error));
        }

        return filePath;
    }

    public void Dispose()
    {
        var activeProcess = process;
        if (activeProcess is { HasExited: false })
        {
            try
            {
                activeProcess.Kill(entireProcessTree: true);
            }
            catch
            {
                // Best effort cleanup while shutting down.
            }
        }

        CleanupProcess();
        AudioLevelChanged?.Invoke(0);
    }

    private static ProcessStartInfo CreateAfrecordStartInfo(string filePath)
    {
        var startInfo = new ProcessStartInfo(AfrecordPath)
        {
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        startInfo.ArgumentList.Add("-f");
        startInfo.ArgumentList.Add("WAVE");
        startInfo.ArgumentList.Add("-r");
        startInfo.ArgumentList.Add("16000");
        startInfo.ArgumentList.Add("-c");
        startInfo.ArgumentList.Add("1");
        startInfo.ArgumentList.Add(filePath);
        return startInfo;
    }

    private static async Task SendInterruptAsync(int processId)
    {
        try
        {
            await MacProcess.RunAsync(KillPath, ["-INT", processId.ToString()], timeoutMilliseconds: 1000);
        }
        catch
        {
            // Falling back to Kill below is acceptable if SIGINT cannot be delivered.
        }
    }

    private static async Task WaitOrKillAsync(Process activeProcess)
    {
        using var timeout = new CancellationTokenSource(SigIntWaitMilliseconds);
        try
        {
            await activeProcess.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            if (!activeProcess.HasExited)
            {
                activeProcess.Kill(entireProcessTree: true);
                await activeProcess.WaitForExitAsync();
            }
        }
    }

    private async Task<string> ReadStandardErrorAsync()
    {
        if (standardErrorTask is null)
        {
            return string.Empty;
        }

        try
        {
            return await standardErrorTask;
        }
        catch
        {
            return string.Empty;
        }
    }

    private void CleanupProcess()
    {
        process?.Dispose();
        process = null;
        standardErrorTask = null;
        currentFilePath = null;
    }

    private static string CreateAfrecordErrorMessage(string error)
    {
        return string.IsNullOrWhiteSpace(error)
            ? "macOS 录音失败，请确认已授予麦克风权限。"
            : $"macOS 录音失败：{error.Trim()}";
    }
}
