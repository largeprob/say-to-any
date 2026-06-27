using System.Diagnostics;
using System.Text;

namespace pc.Services;

internal sealed record MacProcessResult(int ExitCode, string StandardOutput, string StandardError)
{
    public bool Succeeded => ExitCode == 0;
}

internal static class MacProcess
{
    public static MacProcessResult Run(
        string fileName,
        IEnumerable<string> arguments,
        string? standardInput = null,
        int timeoutMilliseconds = 5000)
    {
        return RunAsync(fileName, arguments, standardInput, timeoutMilliseconds)
            .GetAwaiter()
            .GetResult();
    }

    public static async Task<MacProcessResult> RunAsync(
        string fileName,
        IEnumerable<string> arguments,
        string? standardInput = null,
        int timeoutMilliseconds = 5000)
    {
        using var timeout = new CancellationTokenSource(timeoutMilliseconds);
        using var process = new Process
        {
            StartInfo = CreateStartInfo(fileName, arguments, redirectInput: standardInput is not null)
        };

        process.Start();

        var outputTask = process.StandardOutput.ReadToEndAsync(timeout.Token);
        var errorTask = process.StandardError.ReadToEndAsync(timeout.Token);

        if (standardInput is not null)
        {
            await process.StandardInput.WriteAsync(standardInput);
            process.StandardInput.Close();
        }

        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            throw new TimeoutException($"{fileName} did not finish within {timeoutMilliseconds} ms.");
        }

        return new MacProcessResult(
            process.ExitCode,
            await outputTask,
            await errorTask);
    }

    private static ProcessStartInfo CreateStartInfo(
        string fileName,
        IEnumerable<string> arguments,
        bool redirectInput)
    {
        var startInfo = new ProcessStartInfo(fileName)
        {
            RedirectStandardInput = redirectInput,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        return startInfo;
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // Best effort cleanup for platform helper processes.
        }
    }
}
