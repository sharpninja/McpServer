<#
.SYNOPSIS
    Builds, packs, and installs a .NET executable project as a global dotnet tool.

.DESCRIPTION
    Generalized tool redeploy script with Director defaults. It can:
    - optionally bump GitVersion next-version patch
    - compute package version via dotnet-gitversion (or accept an explicit version)
    - stop a running process by command name
    - uninstall previous global tool package
    - pack a target project into a nupkg
    - install the global tool from a local package source

.PARAMETER SkipVersionBump
    When set, skips the GitVersion.yml next-version patch bump.

.PARAMETER ProjectPath
    Path to the target .csproj. Defaults to McpServer.Director.

.PARAMETER ToolId
    Dotnet tool package id to uninstall/install.

.PARAMETER ToolCommand
    Command/process name to stop before reinstall.

.PARAMETER NupkgDir
    Output directory for generated .nupkg packages.

.PARAMETER PackageVersion
    Explicit package version. If omitted, version is computed from dotnet-gitversion SemVer.

.PARAMETER SkipProcessStop
    When set, skips stopping running processes for ToolCommand.

.PARAMETER PackProperty
    Additional pack MSBuild property values in KEY=VALUE format.
    Example: -PackProperty "IsPackable=true","PackAsTool=true","ToolCommandName=mcp-web"

.EXAMPLE
    .\Update-DirectorTool.ps1
    .\Update-DirectorTool.ps1 -SkipVersionBump
    .\Update-DirectorTool.ps1 -ProjectPath src\McpServer.Web\McpServer.Web.csproj -ToolId SharpNinja.McpServer.Web -ToolCommand mcp-web -SkipVersionBump -PackProperty "IsPackable=true","PackAsTool=true","ToolCommandName=mcp-web","PackageId=SharpNinja.McpServer.Web"
#>
[CmdletBinding()]
param(
    [switch]$SkipVersionBump,
    [string]$ProjectPath = 'src\McpServer.Director\McpServer.Director.csproj',
    [string]$ToolId = 'SharpNinja.McpServer.Director',
    [string]$ToolCommand = 'director',
    [string]$NupkgDir = 'nupkg',
    [string]$PackageVersion,
    [switch]$SkipProcessStop,
    [string[]]$PackProperty = @()
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$ProgressPreference    = 'SilentlyContinue'

$RepoRoot          = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$ResolvedProject   = Join-Path $RepoRoot $ProjectPath
$ResolvedNupkgDir  = Join-Path $RepoRoot $NupkgDir

if (-not (Test-Path $ResolvedProject)) {
    throw "Project not found: $ResolvedProject"
}

if (-not (Test-Path $ResolvedNupkgDir)) {
    New-Item -ItemType Directory -Path $ResolvedNupkgDir -Force | Out-Null
}

# ---------------------------------------------------------------------------
# Shared: Bump-GitVersionPatch (also used by Update-McpService.ps1)
# ---------------------------------------------------------------------------

. (Join-Path $PSScriptRoot 'Bump-GitVersionPatch.ps1')

# ---------------------------------------------------------------------------
# Pipeline
# ---------------------------------------------------------------------------

function Write-Step {
    param([string]$Message)
    Write-Host "`n>> $Message" -ForegroundColor Cyan
}

# 1. Bump version
if (-not $SkipVersionBump) {
    Write-Step "1/6  Bumping GitVersion next-version patch ..."
    $bumpResult = Bump-GitVersionPatch -RepoRoot $RepoRoot
    Write-Host "  $($bumpResult.OldVersion) -> $($bumpResult.NewVersion)" -ForegroundColor Green
}
else {
    Write-Step "1/6  Skipping version bump."
}

# 2. Compute package version
if (-not $PackageVersion) {
    Write-Step "2/6  Computing package version ..."
    Push-Location $RepoRoot
    try {
        $gitVersionJson = dotnet gitversion /output json 2>&1
        if ($LASTEXITCODE -ne 0) { Write-Error "dotnet gitversion failed: $gitVersionJson" }
        $versionInfo = $gitVersionJson | ConvertFrom-Json
        $PackageVersion = $versionInfo.SemVer
    }
    finally { Pop-Location }
}
else {
    Write-Step "2/6  Using provided package version."
}
Write-Host "  Package version: $packageVersion" -ForegroundColor Green

# 3. Stop running command process
if (-not $SkipProcessStop) {
    Write-Step "3/6  Stopping running process '$ToolCommand' ..."
    $procs = @(Get-Process -Name $ToolCommand -ErrorAction SilentlyContinue)
    if ($procs.Count -gt 0) {
        foreach ($p in $procs) { Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue }
        Start-Sleep -Seconds 1
        Write-Host "  Killed $($procs.Count) process(es)." -ForegroundColor Green
    }
    else {
        Write-Host "  No running '$ToolCommand' process found." -ForegroundColor DarkGray
    }
}
else {
    Write-Step "3/6  Skipping process stop."
}

# 4. Uninstall previous version
Write-Step "4/6  Uninstalling previous version ..."
dotnet tool uninstall --global $ToolId 2>&1 | Out-Null
Write-Host "  Uninstalled (or was not installed)." -ForegroundColor DarkGray

# 5. Pack
Write-Step "5/6  Packing $ToolId v$packageVersion ..."
$packArgs = @(
    'pack',
    $ResolvedProject,
    '-c', 'Release',
    '-o', $ResolvedNupkgDir,
    "/p:PackageVersion=$PackageVersion"
)
foreach ($prop in $PackProperty) {
    if ([string]::IsNullOrWhiteSpace($prop)) { continue }
    $packArgs += "/p:$prop"
}
& dotnet @packArgs
if ($LASTEXITCODE -ne 0) { Write-Error "dotnet pack failed (exit code $LASTEXITCODE)" }
Write-Host "  Pack complete." -ForegroundColor Green

# 6. Install globally
Write-Step "6/6  Installing globally ..."
dotnet tool install --global $ToolId --add-source $ResolvedNupkgDir --version $PackageVersion
if ($LASTEXITCODE -ne 0) { Write-Error "dotnet tool install failed (exit code $LASTEXITCODE)" }

Write-Host "`n=== Tool updated ===" -ForegroundColor Green
Write-Host "  Version : $packageVersion"
Write-Host "  Command : $ToolCommand interactive"
