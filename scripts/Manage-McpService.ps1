<#
.SYNOPSIS
    Manages the MCP Server Windows service (install, uninstall, start, stop, restart, status).

.DESCRIPTION
    Publishes the MCP Server as a self-contained executable and manages it as a Windows service.
    Uses gsudo for elevation so the script can be run from a non-admin shell.

.PARAMETER Action
    The management action to perform: Install, Uninstall, Start, Stop, Restart, Status, Publish.

.PARAMETER ServiceName
    The Windows service name. Default: McpServer.

.PARAMETER DisplayName
    The display name shown in services.msc. Default: MCP Server.

.PARAMETER Description
    The service description. Default: MCP Model Context Protocol Server.

.PARAMETER InstallPath
    Where the published output is placed. Default: C:\ProgramData\McpServer.

.PARAMETER Instance
    Optional instance name passed as --instance to the executable.

.PARAMETER Port
    HTTP port for the server. Default: 7147.

.EXAMPLE
    .\Manage-McpService.ps1 -Action Install
    .\Manage-McpService.ps1 -Action Start
    .\Manage-McpService.ps1 -Action Status
    .\Manage-McpService.ps1 -Action Restart
    .\Manage-McpService.ps1 -Action Uninstall
    .\Manage-McpService.ps1 -Action Publish  # publish only, no service changes
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet('Install', 'Uninstall', 'Start', 'Stop', 'Restart', 'Status', 'Publish')]
    [string]$Action,

    [string]$ServiceName = 'McpServer',
    [string]$DisplayName = 'MCP Server',
    [string]$Description = 'MCP Model Context Protocol Server',
    [string]$InstallPath = 'C:\ProgramData\McpServer',
    [string]$Instance = '',
    [int]$Port = 7147
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$ProjectDir = Join-Path $PSScriptRoot '..\src\McpServer.Support.Mcp'
$ProjectFile = Join-Path $ProjectDir 'McpServer.Support.Mcp.csproj'
$ExeName = 'McpServer.Support.Mcp.exe'

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

function Assert-Gsudo {
    if (-not (Get-Command gsudo -ErrorAction SilentlyContinue)) {
        Write-Error "gsudo is required but not found. Install it: winget install gerardog.gsudo"
    }
}

function Get-ServiceExePath {
    $exe = Join-Path $InstallPath $ExeName
    $binPath = "`"$exe`" --urls `"http://+:$Port`""
    if ($Instance) {
        $binPath += " --instance `"$Instance`""
    }
    return $binPath
}

function Publish-App {
    Write-Host "Publishing MCP Server to $InstallPath ..." -ForegroundColor Cyan
    if (-not (Test-Path $ProjectFile)) {
        Write-Error "Project file not found: $ProjectFile"
    }

    dotnet publish $ProjectFile `
        -c Release `
        -r win-x64 `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -o $InstallPath

    if ($LASTEXITCODE -ne 0) {
        Write-Error "dotnet publish failed with exit code $LASTEXITCODE"
    }

    # Copy appsettings if not already present (don't overwrite user config)
    $sourceSettings = Join-Path $ProjectDir 'appsettings.json'
    $targetSettings = Join-Path $InstallPath 'appsettings.json'
    if (-not (Test-Path $targetSettings)) {
        Copy-Item $sourceSettings $targetSettings -Force
        Write-Host "  Copied default appsettings.json" -ForegroundColor DarkGray
    }
    else {
        Write-Host "  appsettings.json already exists — skipped (check for new config keys)" -ForegroundColor Yellow
    }

    Write-Host "Publish complete." -ForegroundColor Green
}

# ---------------------------------------------------------------------------
# Actions
# ---------------------------------------------------------------------------

function Install-McpService {
    Assert-Gsudo

    $svc = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    if ($svc) {
        Write-Host "Service '$ServiceName' already exists (Status: $($svc.Status)). Use -Action Uninstall first." -ForegroundColor Yellow
        return
    }

    # Publish first
    Publish-App

    $binPath = Get-ServiceExePath
    Write-Host "Installing service '$ServiceName' ..." -ForegroundColor Cyan
    Write-Host "  binPath = $binPath" -ForegroundColor DarkGray

    gsudo {
        param($svcName, $binPath, $dispName, $desc)
        sc.exe create $svcName binPath= $binPath start= auto DisplayName= $dispName
        if ($LASTEXITCODE -ne 0) { throw "sc.exe create failed with exit code $LASTEXITCODE" }
        sc.exe description $svcName $desc
        sc.exe failure $svcName reset= 86400 actions= restart/60000/restart/60000/restart/60000
    } -args $ServiceName, $binPath, $DisplayName, $Description

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Service installation failed with exit code $LASTEXITCODE"
    }

    Write-Host "Service '$ServiceName' installed successfully." -ForegroundColor Green
    Write-Host "Run: .\Manage-McpService.ps1 -Action Start" -ForegroundColor DarkGray
}

function Uninstall-McpService {
    Assert-Gsudo

    $svc = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    if (-not $svc) {
        Write-Host "Service '$ServiceName' does not exist." -ForegroundColor Yellow
        return
    }

    if ($svc.Status -eq 'Running') {
        Write-Host "Stopping service '$ServiceName' ..." -ForegroundColor Cyan
        gsudo sc.exe stop $ServiceName
        Start-Sleep -Seconds 3
    }

    Write-Host "Removing service '$ServiceName' ..." -ForegroundColor Cyan
    gsudo sc.exe delete $ServiceName

    if ($LASTEXITCODE -ne 0) {
        Write-Error "sc.exe delete failed with exit code $LASTEXITCODE"
    }

    Write-Host "Service '$ServiceName' removed." -ForegroundColor Green
    Write-Host "Published files remain at: $InstallPath" -ForegroundColor DarkGray
}

function Start-McpService {
    Assert-Gsudo

    $svc = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    if (-not $svc) {
        Write-Error "Service '$ServiceName' is not installed. Run -Action Install first."
    }

    if ($svc.Status -eq 'Running') {
        Write-Host "Service '$ServiceName' is already running." -ForegroundColor Yellow
        return
    }

    Write-Host "Starting service '$ServiceName' ..." -ForegroundColor Cyan
    gsudo sc.exe start $ServiceName

    if ($LASTEXITCODE -ne 0) {
        Write-Error "sc.exe start failed with exit code $LASTEXITCODE"
    }

    Start-Sleep -Seconds 2
    $svc = Get-Service -Name $ServiceName
    Write-Host "Service status: $($svc.Status)" -ForegroundColor Green
}

function Stop-McpService {
    Assert-Gsudo

    $svc = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    if (-not $svc) {
        Write-Error "Service '$ServiceName' is not installed."
    }

    if ($svc.Status -ne 'Running') {
        Write-Host "Service '$ServiceName' is not running (Status: $($svc.Status))." -ForegroundColor Yellow
        return
    }

    Write-Host "Stopping service '$ServiceName' ..." -ForegroundColor Cyan
    gsudo sc.exe stop $ServiceName

    if ($LASTEXITCODE -ne 0) {
        Write-Error "sc.exe stop failed with exit code $LASTEXITCODE"
    }

    Start-Sleep -Seconds 2
    $svc = Get-Service -Name $ServiceName
    Write-Host "Service status: $($svc.Status)" -ForegroundColor Green
}

function Restart-McpService {
    Stop-McpService
    Start-Sleep -Seconds 1
    Start-McpService
}

function Get-McpServiceStatus {
    $svc = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    if (-not $svc) {
        Write-Host "Service '$ServiceName' is not installed." -ForegroundColor Yellow
        return
    }

    Write-Host "Service: $($svc.DisplayName) ($ServiceName)" -ForegroundColor Cyan
    Write-Host "  Status  : $($svc.Status)"
    Write-Host "  Startup : $($svc.StartType)"

    # Show the executable path
    $regPath = "HKLM:\SYSTEM\CurrentControlSet\Services\$ServiceName"
    if (Test-Path $regPath) {
        $imagePath = (Get-ItemProperty $regPath).ImagePath
        Write-Host "  BinPath : $imagePath" -ForegroundColor DarkGray
    }

    # Check if responding on configured port
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:$Port/health" -TimeoutSec 3 -UseBasicParsing -ErrorAction Stop
        Write-Host "  Health  : $($response.StatusCode) OK" -ForegroundColor Green
    }
    catch {
        if ($svc.Status -eq 'Running') {
            Write-Host "  Health  : Not responding on port $Port" -ForegroundColor Red
        }
        else {
            Write-Host "  Health  : (service not running)" -ForegroundColor DarkGray
        }
    }
}

# ---------------------------------------------------------------------------
# Dispatch
# ---------------------------------------------------------------------------

switch ($Action) {
    'Install'   { Install-McpService }
    'Uninstall' { Uninstall-McpService }
    'Start'     { Start-McpService }
    'Stop'      { Stop-McpService }
    'Restart'   { Restart-McpService }
    'Status'    { Get-McpServiceStatus }
    'Publish'   { Publish-App }
}
