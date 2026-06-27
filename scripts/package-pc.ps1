param(
    [string]$Version = "",
    [string]$Runtime = "win-x64",
    [string]$Configuration = "Release",
    [string]$RepoUrl = "https://github.com/largeprob/say-to-any",
    [string]$GithubToken = $env:GITHUB_TOKEN,
    [switch]$Msi,
    [switch]$Upload,
    [switch]$PublishRelease
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot "src\pc\pc.csproj"
$publishDir = Join-Path $repoRoot "artifacts\publish\pc\$Runtime"
$releaseDir = Join-Path $repoRoot "artifacts\releases\pc\$Runtime"
$iconPath = Join-Path $repoRoot "src\pc\Assets\logo.ico"

if ([string]::IsNullOrWhiteSpace($Version)) {
    $Version = (& dotnet msbuild $projectPath -getProperty:Version).Trim()
}

if ([string]::IsNullOrWhiteSpace($Version)) {
    throw "Version is required. Pass -Version 1.0.0 or set <Version> in src\pc\pc.csproj."
}

New-Item -ItemType Directory -Force -Path $publishDir, $releaseDir | Out-Null

dotnet tool restore
dotnet publish $projectPath `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -o $publishDir `
    /p:PublishSingleFile=false

$packArgs = @(
    "pack",
    "--packId", "SayToAny",
    "--packVersion", $Version,
    "--packDir", $publishDir,
    "--outputDir", $releaseDir,
    "--packTitle", "Say-To-Any",
    "--packAuthors", "Say-To-Any",
    "--mainExe", "SayToAny.exe",
    "--runtime", $Runtime,
    "--icon", $iconPath
)

if ($Msi) {
    $packArgs += @("--msi", "true", "--instLocation", "Either")
}

dotnet tool run vpk -- @packArgs

if ($Upload) {
    if ([string]::IsNullOrWhiteSpace($GithubToken)) {
        throw "Github token is required for upload. Set GITHUB_TOKEN or pass -GithubToken."
    }

    $uploadArgs = @(
        "upload", "github",
        "--outputDir", $releaseDir,
        "--repoUrl", $RepoUrl,
        "--token", $GithubToken,
        "--merge", "true",
        "--publish", $PublishRelease.IsPresent.ToString().ToLowerInvariant()
    )

    dotnet tool run vpk -- @uploadArgs
}
