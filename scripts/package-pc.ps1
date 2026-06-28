param(
    [string]$Version = "",
    [string]$Runtime = "win-x64",
    [string]$Channel = "win",
    [string]$Configuration = "Release",
    [string]$RepoUrl = "https://github.com/largeprob/say-to-any",
    [string]$GithubToken = $env:GITHUB_TOKEN,
    [switch]$Msi,
    [switch]$Upload,
    [switch]$PublishRelease,
    [switch]$SkipRemoteDownload
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot "src\pc\pc.csproj"
$publishDir = Join-Path $repoRoot "artifacts\publish\pc\$Runtime"
$releaseDir = Join-Path $repoRoot "artifacts\releases\pc\$Runtime"
$iconPath = Join-Path $repoRoot "src\pc\Assets\logo.ico"

function Invoke-DotNet {
    param([string[]]$Arguments)

    dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }
}

function Invoke-Vpk {
    param([string[]]$Arguments)

    dotnet tool run vpk -- @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "vpk $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }
}

if (-not $Runtime.StartsWith("win-", [StringComparison]::OrdinalIgnoreCase)) {
    throw "This script creates Windows Velopack packages. Use Velopack's [osx] bundle flow for macOS packages."
}

if ([string]::IsNullOrWhiteSpace($Version)) {
    $Version = (& dotnet msbuild $projectPath -getProperty:Version).Trim()
}

if ([string]::IsNullOrWhiteSpace($Version)) {
    throw "Version is required. Pass -Version 1.0.0 or set <Version> in src\pc\pc.csproj."
}

if (Test-Path -LiteralPath $publishDir) {
    Remove-Item -LiteralPath $publishDir -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $publishDir, $releaseDir | Out-Null

Invoke-DotNet @("tool", "restore")

if (-not $SkipRemoteDownload) {
    $downloadArgs = @(
        "download", "github",
        "--outputDir", $releaseDir,
        "--channel", $Channel,
        "--repoUrl", $RepoUrl
    )

    if (-not [string]::IsNullOrWhiteSpace($GithubToken)) {
        $downloadArgs += @("--token", $GithubToken)
    }

    try {
        Invoke-Vpk $downloadArgs
    }
    catch {
        Write-Warning "Could not download existing Velopack release feed. A full package will still be generated. $($_.Exception.Message)"
    }
}

Invoke-DotNet @(
    "publish", $projectPath,
    "-c", $Configuration,
    "-r", $Runtime,
    "--self-contained", "true",
    "-o", $publishDir,
    "/p:Version=$Version",
    "/p:PublishSingleFile=false"
)

$packArgs = @(
    "pack",
    "--packId", "SayToAny",
    "--packVersion", $Version,
    "--packDir", $publishDir,
    "--outputDir", $releaseDir,
    "--channel", $Channel,
    "--packTitle", "Say-To-Any",
    "--packAuthors", "Say-To-Any",
    "--mainExe", "SayToAny.exe",
    "--runtime", $Runtime,
    "--icon", $iconPath
)

if ($Msi) {
    $packArgs += @("--msi", "true", "--instLocation", "Either")
}

Invoke-Vpk $packArgs

if ($Upload) {
    if ([string]::IsNullOrWhiteSpace($GithubToken)) {
        throw "Github token is required for upload. Set GITHUB_TOKEN or pass -GithubToken."
    }

    $uploadArgs = @(
        "upload", "github",
        "--outputDir", $releaseDir,
        "--channel", $Channel,
        "--repoUrl", $RepoUrl,
        "--token", $GithubToken,
        "--merge", "true",
        "--publish", $PublishRelease.IsPresent.ToString().ToLowerInvariant(),
        "--tag", "v$Version",
        "--releaseName", "v$Version"
    )

    Invoke-Vpk $uploadArgs
}
