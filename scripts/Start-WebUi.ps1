[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release", "Staging")]
    [string]$Configuration = "Debug",
    [int]$Port = 5200,
    [int]$TimeoutSeconds = 60,
    [switch]$NoBuild,
    [switch]$KillExisting
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-WebUiProcessSnapshot {
    $dotnet = Get-CimInstance Win32_Process | Where-Object { $_.Name -eq 'dotnet.exe' -and $_.CommandLine -match 'McpServer.Web' }
    $web = Get-CimInstance Win32_Process | Where-Object { $_.Name -eq 'McpServer.Web.exe' -and $_.CommandLine -match 'McpServer.Web' }

    [PSCustomObject]@{
        Dotnet = $dotnet | Select-Object ProcessId, ParentProcessId, Name, CommandLine
        WebExe = $web | Select-Object ProcessId, ParentProcessId, Name, CommandLine
    }
}

function Stop-WebUiProcesses {
    $snapshot = Get-WebUiProcessSnapshot
    $targetIds = @(
        $snapshot.WebExe | Select-Object -ExpandProperty ProcessId
        $snapshot.Dotnet | Select-Object -ExpandProperty ProcessId
    ) | Where-Object { $_ } | Select-Object -Unique

    foreach ($targetId in $targetIds) {
        Stop-Process -Id $targetId -Force -ErrorAction SilentlyContinue
    }

    return $targetIds
}

function Get-TailOrEmpty {
    param(
        [string]$Path,
        [int]$LineCount = 60
    )

    if (Test-Path -LiteralPath $Path) {
        return (Get-Content -LiteralPath $Path -Tail $LineCount)
    }

    return @()
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot "src\McpServer.Web\McpServer.Web.csproj"
if (-not (Test-Path -LiteralPath $projectPath)) {
    throw "McpServer.Web project not found at '$projectPath'."
}

if ($KillExisting) {
    $killedIds = Stop-WebUiProcesses
    if (@($killedIds).Count -gt 0) {
        Write-Host "Killed existing web-ui processes: $($killedIds -join ', ')"
    }
}

if (-not $NoBuild) {
    Write-Host "Building McpServer.Web ($Configuration)..."
    dotnet build $projectPath -c $Configuration
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed for McpServer.Web."
    }
}

$logRoot = Join-Path $repoRoot "logs\web-ui-startup"
$null = New-Item -ItemType Directory -Path $logRoot -Force
$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$outLog = Join-Path $logRoot "web-ui-$stamp.out.log"
$errLog = Join-Path $logRoot "web-ui-$stamp.err.log"
$diagLog = Join-Path $logRoot "web-ui-$stamp.diag.json"
$baseUrl = "http://localhost:$Port"

Write-Host "Starting McpServer.Web on $baseUrl with timeout ${TimeoutSeconds}s..."
$runArgs = @(
    "run",
    "--project", $projectPath,
    "-c", $Configuration,
    "--no-build",
    "--urls", $baseUrl
)

$process = Start-Process -FilePath "dotnet" `
    -ArgumentList $runArgs `
    -WorkingDirectory $repoRoot `
    -RedirectStandardOutput $outLog `
    -RedirectStandardError $errLog `
    -PassThru

$deadline = (Get-Date).AddSeconds($TimeoutSeconds)
$started = $false
while ((Get-Date) -lt $deadline) {
    if ($process.HasExited) {
        $stderrTail = (Get-TailOrEmpty -Path $errLog) -join [Environment]::NewLine
        throw "McpServer.Web exited early (PID $($process.Id), code $($process.ExitCode)).`n$stderrTail"
    }

    try {
        $resp = Invoke-WebRequest -Uri $baseUrl -UseBasicParsing -TimeoutSec 3
        if ($resp.StatusCode -ge 200 -and $resp.StatusCode -lt 500) {
            $started = $true
            break
        }
    }
    catch {
        Start-Sleep -Milliseconds 500
    }
}

if (-not $started) {
    $snapshot = Get-WebUiProcessSnapshot
    $allIds = @(
        $snapshot.WebExe | Select-Object -ExpandProperty ProcessId
        $snapshot.Dotnet | Select-Object -ExpandProperty ProcessId
    ) | Where-Object { $_ } | Select-Object -Unique

    $listeners = Get-NetTCPConnection -State Listen -ErrorAction SilentlyContinue |
        Where-Object { $allIds -contains $_.OwningProcess } |
        Select-Object LocalAddress, LocalPort, OwningProcess, State

    $diag = [PSCustomObject]@{
        TimestampUtc = (Get-Date).ToUniversalTime().ToString("o")
        BaseUrl = $baseUrl
        TimeoutSeconds = $TimeoutSeconds
        RootProcessId = $process.Id
        ProcessSnapshot = $snapshot
        ListenerSnapshot = $listeners
        StdErrTail = Get-TailOrEmpty -Path $errLog
        StdOutTail = Get-TailOrEmpty -Path $outLog
    }
    $diag | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $diagLog -Encoding UTF8

    foreach ($targetId in $allIds) {
        Stop-Process -Id $targetId -Force -ErrorAction SilentlyContinue
    }

    throw "Startup timed out after ${TimeoutSeconds}s. Process tree killed. Diagnostics: $diagLog"
}

$result = [PSCustomObject]@{
    Success = $true
    Url = $baseUrl
    ProcessId = $process.Id
    OutLog = $outLog
    ErrLog = $errLog
    DiagLog = $diagLog
}

$result | ConvertTo-Json -Compress
