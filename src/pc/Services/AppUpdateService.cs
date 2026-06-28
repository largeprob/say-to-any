using System.Diagnostics;
using Velopack;
using Velopack.Sources;

namespace pc.Services;

public static class AppUpdateService
{
    private const string DefaultGithubRepositoryUrl = "https://github.com/largeprob/say-to-any";
    private const string RepositoryUrlEnvironmentVariable = "SAY_TO_ANY_UPDATE_REPOSITORY_URL";
    private const string GithubTokenEnvironmentVariable = "SAY_TO_ANY_UPDATE_GITHUB_TOKEN";
    private const string IncludePrereleasesEnvironmentVariable = "SAY_TO_ANY_UPDATE_PRERELEASE";
    private const string AutoRestartEnvironmentVariable = "SAY_TO_ANY_UPDATE_AUTO_RESTART";
    private const string DiagnosticsEnvironmentVariable = "SAY_TO_ANY_UPDATE_DIAGNOSTICS";

    public static void CheckForUpdatesOnStartup(Action<string> reportStatus, CancellationToken cancellationToken)
    {
        var repositoryUrl = GetSetting(RepositoryUrlEnvironmentVariable, DefaultGithubRepositoryUrl);
        if (string.IsNullOrWhiteSpace(repositoryUrl))
        {
            return;
        }

        _ = Task.Run(
            async () => await CheckForUpdatesAsync(repositoryUrl, reportStatus, cancellationToken),
            CancellationToken.None);
    }

    private static async Task CheckForUpdatesAsync(
        string repositoryUrl,
        Action<string> reportStatus,
        CancellationToken cancellationToken)
    {
        try
        {
            var source = new GithubSource(
                repositoryUrl,
                GetSetting(GithubTokenEnvironmentVariable),
                GetBooleanSetting(IncludePrereleasesEnvironmentVariable),
                downloader: null);
            var updateManager = new UpdateManager(source);

            if (!updateManager.IsInstalled)
            {
                ReportDiagnostic(reportStatus, "自动更新未启用：当前应用不是通过 Velopack 安装包或便携包启动的。");
                return;
            }

            Trace.TraceInformation(
                $"Checking for updates from {repositoryUrl} with current version {updateManager.CurrentVersion}.");

            var pendingUpdate = updateManager.UpdatePendingRestart;
            if (pendingUpdate is not null)
            {
                ApplyOrReportPendingUpdate(updateManager, pendingUpdate, reportStatus);
                return;
            }

            var update = await updateManager.CheckForUpdatesAsync();
            if (update is null)
            {
                return;
            }

            reportStatus($"发现新版本 {update.TargetFullRelease.Version}，正在下载...");
            await updateManager.DownloadUpdatesAsync(
                update,
                progress => reportStatus($"下载更新 {progress}%"),
                cancellationToken);

            if (ShouldRestartAfterDownload())
            {
                reportStatus("更新下载完成，正在重启安装...");
                updateManager.ApplyUpdatesAndRestart(update.TargetFullRelease, Array.Empty<string>());
                return;
            }

            reportStatus("更新已下载，将在下次启动时自动安装");
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown path.
        }
        catch (Exception ex)
        {
            // Update failures should never prevent dictation from starting.
            ReportDiagnostic(reportStatus, CreateFailureDiagnostic(ex));
        }
    }

    private static void ApplyOrReportPendingUpdate(
        UpdateManager updateManager,
        VelopackAsset pendingUpdate,
        Action<string> reportStatus)
    {
        if (!ShouldRestartAfterDownload())
        {
            reportStatus("更新已准备，将在下次启动时自动安装");
            return;
        }

        reportStatus("更新已准备，正在重启安装...");
        updateManager.ApplyUpdatesAndRestart(pendingUpdate, Array.Empty<string>());
    }

    private static bool ShouldRestartAfterDownload()
    {
        return GetBooleanSetting(AutoRestartEnvironmentVariable, defaultValue: true);
    }

    private static string CreateFailureDiagnostic(Exception exception)
    {
        return exception.Message.Contains("matching assets", StringComparison.OrdinalIgnoreCase) ||
            exception.Message.Contains("RELEASES", StringComparison.OrdinalIgnoreCase) ||
            exception.Message.Contains("releases.", StringComparison.OrdinalIgnoreCase)
            ? "更新检查失败：GitHub Release 缺少 Velopack 更新清单或更新包。"
            : $"更新检查失败：{exception.Message}";
    }

    private static void ReportDiagnostic(Action<string> reportStatus, string message)
    {
        Trace.TraceWarning(message);

        if (GetBooleanSetting(DiagnosticsEnvironmentVariable))
        {
            reportStatus(message);
        }
    }

    private static string GetSetting(string name, string defaultValue = "")
    {
        var value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? defaultValue : value.Trim();
    }

    private static bool GetBooleanSetting(string name, bool defaultValue = false)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        return value.Equals("1", StringComparison.OrdinalIgnoreCase)
            || value.Equals("true", StringComparison.OrdinalIgnoreCase)
            || value.Equals("yes", StringComparison.OrdinalIgnoreCase);
    }
}
