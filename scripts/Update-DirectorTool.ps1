<#
.SYNOPSIS
    Builds, packs, and installs the McpServer Director as a global dotnet tool.

.DESCRIPTION
    Bumps the patch level in GitVersion.yml next-version, computes the package
    version via dotnet-gitversion, kills any running director process, uninstalls
    the previous version, packs the new version, and installs it globally.

.PARAMETER SkipVersionBump
    When set, skips the GitVersion.yml next-version patch bump.

.EXAMPLE
    .\Update-DirectorTool.ps1
    .\Update-DirectorTool.ps1 -SkipVersionBump
#>
[CmdletBinding()]
param(
    [switch]$SkipVersionBump
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$ProgressPreference    = 'SilentlyContinue'

$RepoRoot    = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$ProjectDir  = Join-Path $RepoRoot 'src\McpServer.Director'
$NupkgDir    = Join-Path $RepoRoot 'nupkg'
$ToolId      = 'SharpNinja.McpServer.Director'
$ToolCommand = 'director'

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
Write-Step "2/6  Computing package version ..."
Push-Location $RepoRoot
try {
    $gitVersionJson = dotnet gitversion /output json 2>&1
    if ($LASTEXITCODE -ne 0) { Write-Error "dotnet gitversion failed: $gitVersionJson" }
    $versionInfo = $gitVersionJson | ConvertFrom-Json
    $packageVersion = $versionInfo.SemVer
}
finally { Pop-Location }
Write-Host "  Package version: $packageVersion" -ForegroundColor Green

# 3. Kill running director
Write-Step "3/6  Stopping running director ..."
$procs = Get-Process -Name $ToolCommand -ErrorAction SilentlyContinue
if ($procs) {
    foreach ($p in $procs) { Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue }
    Start-Sleep -Seconds 1
    Write-Host "  Killed $($procs.Count) process(es)." -ForegroundColor Green
}
else {
    Write-Host "  No running director found." -ForegroundColor DarkGray
}

# 4. Uninstall previous version
Write-Step "4/6  Uninstalling previous version ..."
dotnet tool uninstall --global $ToolId 2>&1 | Out-Null
Write-Host "  Uninstalled (or was not installed)." -ForegroundColor DarkGray

# 5. Pack
Write-Step "5/6  Packing $ToolId v$packageVersion ..."
dotnet pack $ProjectDir -c Release -o $NupkgDir /p:Version=$packageVersion
if ($LASTEXITCODE -ne 0) { Write-Error "dotnet pack failed (exit code $LASTEXITCODE)" }
Write-Host "  Pack complete." -ForegroundColor Green

# 6. Install globally
Write-Step "6/6  Installing globally ..."
dotnet tool install --global $ToolId --add-source $NupkgDir --version $packageVersion
if ($LASTEXITCODE -ne 0) { Write-Error "dotnet tool install failed (exit code $LASTEXITCODE)" }

Write-Host "`n=== Director tool updated ===" -ForegroundColor Green
Write-Host "  Version : $packageVersion"
Write-Host "  Command : $ToolCommand interactive"
