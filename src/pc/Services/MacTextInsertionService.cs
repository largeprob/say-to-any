namespace pc.Services;

public sealed record MacTextInsertionTarget(string? BundleIdentifier) : ITextInsertionTarget;

public sealed class MacTextInsertionService : ITextInsertionService
{
    private const string PbcopyPath = "/usr/bin/pbcopy";
    private const string PbpastePath = "/usr/bin/pbpaste";
    private const string OsascriptPath = "/usr/bin/osascript";

    private const string CaptureFrontmostBundleScript = """
        tell application "System Events"
            set frontApp to first application process whose frontmost is true
            return bundle identifier of frontApp
        end tell
        """;

    private const string PasteScript = """
        tell application "System Events"
            keystroke "v" using command down
        end tell
        """;

    public async Task CopyTextAsync(string text)
    {
        if (!OperatingSystem.IsMacOS())
        {
            throw new PlatformNotSupportedException("macOS clipboard insertion requires macOS.");
        }

        await WriteClipboardAsync(text);
    }

    public async Task PasteTextAsync(string text)
    {
        if (!await TryPasteTextToCurrentFocusAsync(text))
        {
            throw new InvalidOperationException("Cannot paste text on macOS. Grant Accessibility and Automation permissions to Say To Any.");
        }
    }

    public ITextInsertionTarget? CaptureCurrentTarget()
    {
        if (!OperatingSystem.IsMacOS())
        {
            return null;
        }

        try
        {
            var result = MacProcess.Run(
                OsascriptPath,
                ["-e", CaptureFrontmostBundleScript],
                timeoutMilliseconds: 1500);
            var bundleId = result.Succeeded ? result.StandardOutput.Trim() : string.Empty;
            return string.IsNullOrWhiteSpace(bundleId)
                ? new MacTextInsertionTarget(null)
                : new MacTextInsertionTarget(bundleId);
        }
        catch
        {
            return new MacTextInsertionTarget(null);
        }
    }

    public async Task<bool> TryPasteTextToCurrentFocusAsync(string text, ITextInsertionTarget? preferredTarget = null)
    {
        if (!OperatingSystem.IsMacOS() || string.IsNullOrEmpty(text))
        {
            return false;
        }

        var previousText = await ReadClipboardAsync();

        try
        {
            await WriteClipboardAsync(text);

            if (preferredTarget is MacTextInsertionTarget { BundleIdentifier: { Length: > 0 } bundleId })
            {
                await TryActivateApplicationAsync(bundleId);
                await Task.Delay(120);
            }

            var pasteResult = await MacProcess.RunAsync(
                OsascriptPath,
                ["-e", PasteScript],
                timeoutMilliseconds: 5000);
            await Task.Delay(160);
            return pasteResult.Succeeded;
        }
        catch
        {
            return false;
        }
        finally
        {
            if (previousText is not null)
            {
                try
                {
                    await WriteClipboardAsync(previousText);
                }
                catch
                {
                    // Clipboard restoration is best effort.
                }
            }
        }
    }

    private static async Task<string?> ReadClipboardAsync()
    {
        try
        {
            var result = await MacProcess.RunAsync(PbpastePath, [], timeoutMilliseconds: 3000);
            return result.Succeeded ? result.StandardOutput : string.Empty;
        }
        catch
        {
            return null;
        }
    }

    private static async Task WriteClipboardAsync(string text)
    {
        var result = await MacProcess.RunAsync(
            PbcopyPath,
            [],
            standardInput: text,
            timeoutMilliseconds: 3000);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException("Cannot write text to the macOS clipboard.");
        }
    }

    private static async Task TryActivateApplicationAsync(string bundleIdentifier)
    {
        try
        {
            await MacProcess.RunAsync(
                OsascriptPath,
                ["-e", $"tell application id {QuoteAppleScriptString(bundleIdentifier)} to activate"],
                timeoutMilliseconds: 3000);
        }
        catch
        {
            // If activation fails, paste still has a chance to work in the current app.
        }
    }

    private static string QuoteAppleScriptString(string value)
    {
        return $"\"{value.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";
    }
}
