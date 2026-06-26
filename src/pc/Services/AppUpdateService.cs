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
                return;
            }

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
        catch
        {
            // Update failures should never prevent dictation from starting.
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
