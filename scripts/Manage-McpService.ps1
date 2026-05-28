<#
.SYNOPSIS
    Manages the MCP Server Windows service (install, uninstall, start, stop, restart, status).

.DESCRIPTION
    Manages an already-deployed MCP Server Windows service.
    Deployment and installation must go through Update-McpService.ps1 so
    configuration restore and deployment verification always run.

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
    .\Manage-McpService.ps1 -Action Install  # intentionally blocked; use Update-McpService.ps1
    .\Manage-McpService.ps1 -Action Start
    .\Manage-McpService.ps1 -Action Status
    .\Manage-McpService.ps1 -Action Restart
    .\Manage-McpService.ps1 -Action Uninstall
    .\Manage-McpService.ps1 -Action Publish  # intentionally blocked; use Update-McpService.ps1
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
    Write-Error "Direct service publishing is disabled. Use: gsudo pwsh.exe -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File .\scripts\Update-McpService.ps1"
}

# ---------------------------------------------------------------------------
# Actions
# ---------------------------------------------------------------------------

function Install-McpService {
    Write-Error "Direct service installation is disabled. Use: gsudo pwsh.exe -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File .\scripts\Update-McpService.ps1"
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
