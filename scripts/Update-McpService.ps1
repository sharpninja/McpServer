<#
.SYNOPSIS
    Updates the installed MCP Server Windows service in-place, preserving configuration and data.

.DESCRIPTION
    Stops the service, publishes the latest build, restores preserved files
    (appsettings, databases), restarts the service, and verifies health.
    Uses gsudo for elevation.

.PARAMETER ServiceName
    The Windows service name. Default: McpServer.

.PARAMETER InstallPath
    Service installation directory. Default: C:\ProgramData\McpServer.

.PARAMETER Port
    HTTP port for health check. Default: 7147.

.PARAMETER SkipBuild
    When set, copies from a pre-built publish folder instead of running dotnet publish.

.PARAMETER PublishSource
    Path to pre-built publish output. Only used with -SkipBuild.

.EXAMPLE
    .\Update-McpService.ps1
    .\Update-McpService.ps1 -SkipBuild -PublishSource E:\github\McpServer\_publish
#>
[CmdletBinding()]
param(
    [string]$ServiceName = 'McpServer',
    [string]$InstallPath = 'C:\ProgramData\McpServer',
    [int]$Port = 7147,
    [switch]$SkipBuild,
    [string]$PublishSource = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$ProjectDir  = Join-Path $PSScriptRoot '..\src\McpServer.Support.Mcp'
$ProjectFile = Join-Path $ProjectDir 'McpServer.Support.Mcp.csproj'
$ExeName     = 'McpServer.Support.Mcp.exe'
$BackupDir   = Join-Path $env:TEMP "McpServer-update-backup-$(Get-Date -Format 'yyyyMMdd-HHmmss')"

# Files to preserve across updates (glob patterns relative to InstallPath).
$PreservePatterns = @(
    'appsettings*.json',
    '*.db',
    '*.db-shm',
    '*.db-wal'
)

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

function Assert-Gsudo {
    if (-not (Get-Command gsudo -ErrorAction SilentlyContinue)) {
        Write-Error "gsudo is required but not found. Install it: winget install gerardog.gsudo"
    }
}

function Write-Step {
    param([string]$Message)
    Write-Host "`n>> $Message" -ForegroundColor Cyan
}

function Wait-ProcessExit {
    param([string]$Name, [int]$TimeoutSeconds = 30)
    $elapsed = 0
    while ($elapsed -lt $TimeoutSeconds) {
        $procs = Get-Process -Name $Name -ErrorAction SilentlyContinue
        if (-not $procs) { return $true }
        Start-Sleep -Seconds 1
        $elapsed++
    }
    return $false
}

# ---------------------------------------------------------------------------
# Pipeline
# ---------------------------------------------------------------------------

Assert-Gsudo

$svc = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if (-not $svc) {
    Write-Error "Service '$ServiceName' is not installed. Use Manage-McpService.ps1 -Action Install first."
}

$wasRunning = $svc.Status -eq 'Running'

# 1. Stop the service
Write-Step "1/7  Stopping service '$ServiceName' ..."
if ($wasRunning) {
    gsudo sc.exe stop $ServiceName | Out-Null
    if (-not (Wait-ProcessExit -Name $ExeName.Replace('.exe','') -TimeoutSeconds 30)) {
        Write-Warning "Process did not exit within 30 s — forcing termination"
        gsudo {
            param($name)
            Get-Process -Name $name -ErrorAction SilentlyContinue | Stop-Process -Force
        } -args $ExeName.Replace('.exe','')
        Start-Sleep -Seconds 2
    }
    Write-Host "  Service stopped." -ForegroundColor Green
}
else {
    Write-Host "  Service was not running." -ForegroundColor DarkGray
}

# 2. Backup preserved files
Write-Step "2/7  Backing up config & data files ..."
New-Item -ItemType Directory -Path $BackupDir -Force | Out-Null
$backedUp = @()
foreach ($pattern in $PreservePatterns) {
    $files = Get-ChildItem -Path $InstallPath -Filter $pattern -ErrorAction SilentlyContinue
    foreach ($f in $files) {
        Copy-Item $f.FullName (Join-Path $BackupDir $f.Name) -Force
        $backedUp += $f.Name
    }
}
if ($backedUp.Count -gt 0) {
    Write-Host "  Backed up: $($backedUp -join ', ')" -ForegroundColor DarkGray
}
else {
    Write-Host "  No files matched preserve patterns." -ForegroundColor Yellow
}

# 3. Build / Publish
Write-Step "3/7  Publishing new build ..."
if ($SkipBuild) {
    if (-not $PublishSource -or -not (Test-Path $PublishSource)) {
        Write-Error "PublishSource '$PublishSource' not found. Provide a valid path with -SkipBuild."
    }
    gsudo { param($src, $dst) Copy-Item -Path "$src\*" -Destination $dst -Recurse -Force } -args $PublishSource, $InstallPath
}
else {
    if (-not (Test-Path $ProjectFile)) {
        Write-Error "Project file not found: $ProjectFile"
    }
    # Publish to a temp staging directory first, then copy elevated.
    $stageDir = Join-Path $env:TEMP "McpServer-publish-stage"
    if (Test-Path $stageDir) { Remove-Item $stageDir -Recurse -Force }
    dotnet publish $ProjectFile -c Release -o $stageDir
    if ($LASTEXITCODE -ne 0) { Write-Error "dotnet publish failed (exit code $LASTEXITCODE)" }

    gsudo { param($src, $dst) Copy-Item -Path "$src\*" -Destination $dst -Recurse -Force } -args $stageDir, $InstallPath
    Remove-Item $stageDir -Recurse -Force -ErrorAction SilentlyContinue
}
Write-Host "  Publish complete." -ForegroundColor Green

# 4. Restore preserved files
Write-Step "4/7  Restoring config & data files ..."
$restored = @()
foreach ($f in (Get-ChildItem -Path $BackupDir -ErrorAction SilentlyContinue)) {
    $target = Join-Path $InstallPath $f.Name
    gsudo { param($src, $dst) Copy-Item -Path $src -Destination $dst -Force } -args $f.FullName, $target
    $restored += $f.Name
}
if ($restored.Count -gt 0) {
    Write-Host "  Restored: $($restored -join ', ')" -ForegroundColor DarkGray
}

# 5. Start the service
Write-Step "5/7  Starting service '$ServiceName' ..."
gsudo sc.exe start $ServiceName | Out-Null
Start-Sleep -Seconds 3
$svc = Get-Service -Name $ServiceName
Write-Host "  Service status: $($svc.Status)" -ForegroundColor $(if ($svc.Status -eq 'Running') { 'Green' } else { 'Red' })

# 6. Health check
Write-Step "6/7  Verifying health on port $Port ..."
$healthy = $false
for ($attempt = 1; $attempt -le 10; $attempt++) {
    try {
        $r = Invoke-WebRequest -Uri "http://localhost:$Port/health" -TimeoutSec 3 -UseBasicParsing -ErrorAction Stop
        Write-Host "  Health: HTTP $($r.StatusCode) — $($r.Content)" -ForegroundColor Green
        $healthy = $true
        break
    }
    catch {
        if ($attempt -lt 10) { Start-Sleep -Seconds 2 }
    }
}
if (-not $healthy) {
    Write-Warning "Service did not respond to health check after 20 seconds."
}

# 7. Cleanup
Write-Step "7/7  Cleanup ..."
Remove-Item $BackupDir -Recurse -Force -ErrorAction SilentlyContinue
Write-Host "  Backup directory removed." -ForegroundColor DarkGray

# Summary
Write-Host "`n=== Update complete ===" -ForegroundColor Green
Write-Host "  Service : $ServiceName ($($svc.Status))"
Write-Host "  Path    : $InstallPath"
Write-Host "  Health  : $(if ($healthy) { 'OK' } else { 'FAILED' })"
Write-Host "  Files   : $($restored.Count) preserved, $($backedUp.Count) backed up"
