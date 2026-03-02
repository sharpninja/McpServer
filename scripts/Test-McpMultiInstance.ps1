[CmdletBinding()]
param(
    [string]$Configuration = "Staging",
    [string]$FirstInstance = "default",
    [string]$SecondInstance = "alt-local",
    [int]$TimeoutSeconds = 180
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-PortForInstance {
    param(
        [string]$SettingsPath,
        [string]$InstanceName
    )

    $settings = Get-Content -Raw -Path $SettingsPath | ConvertFrom-Json
    $instance = $settings.Mcp.Instances.$InstanceName
    if ($null -eq $instance) {
        throw "Instance '$InstanceName' not found in '$SettingsPath'."
    }

    return [int]$instance.Port
}

function Wait-Healthy {
    param(
        [string]$BaseUrl,
        [System.Diagnostics.Process]$Process,
        [string]$ErrorLogPath,
        [int]$TimeoutSeconds
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        if ($Process.HasExited) {
            $errorTail = ""
            if (Test-Path $ErrorLogPath) {
                $errorTail = (Get-Content -Path $ErrorLogPath -Tail 40) -join [Environment]::NewLine
            }

            throw "Process $($Process.Id) exited before health check passed at $BaseUrl/health.$([Environment]::NewLine)$errorTail"
        }

        try {
            $response = Invoke-WebRequest -Uri "$BaseUrl/health" -UseBasicParsing -TimeoutSec 5
            if ($response.StatusCode -eq 200) {
                return
            }
        }
        catch {
            Start-Sleep -Milliseconds 500
        }
    }

    $timeoutTail = ""
    if (Test-Path $ErrorLogPath) {
        $timeoutTail = (Get-Content -Path $ErrorLogPath -Tail 40) -join [Environment]::NewLine
    }

    throw "Timed out waiting for health endpoint at $BaseUrl/health.$([Environment]::NewLine)$timeoutTail"
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot "src\McpServer.Support.Mcp\McpServer.Support.Mcp.csproj"
$dllPath = Join-Path $repoRoot "src\McpServer.Support.Mcp\bin\$Configuration\net9.0\McpServer.Support.Mcp.dll"
$settingsPath = Join-Path $repoRoot "src\McpServer.Support.Mcp\appsettings.$Configuration.json"

if (-not (Test-Path $settingsPath)) {
    throw "Settings file '$settingsPath' does not exist."
}

if (-not (Test-Path $dllPath)) {
    Write-Host "Building MCP server ($Configuration) for smoke test..."
    dotnet build $projectPath -c $Configuration | Out-Host
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to build MCP server for smoke test."
    }
}

$firstPort = Get-PortForInstance -SettingsPath $settingsPath -InstanceName $FirstInstance
$secondPort = Get-PortForInstance -SettingsPath $settingsPath -InstanceName $SecondInstance

$firstProcess = $null
$secondProcess = $null
$firstOutLog = Join-Path $env:TEMP "mcp-$FirstInstance-$([Guid]::NewGuid().ToString('N')).out.log"
$firstErrLog = Join-Path $env:TEMP "mcp-$FirstInstance-$([Guid]::NewGuid().ToString('N')).err.log"
$secondOutLog = Join-Path $env:TEMP "mcp-$SecondInstance-$([Guid]::NewGuid().ToString('N')).out.log"
$secondErrLog = Join-Path $env:TEMP "mcp-$SecondInstance-$([Guid]::NewGuid().ToString('N')).err.log"

try {
    Write-Host "Starting instance '$FirstInstance' on port $firstPort..."
    $firstProcess = Start-Process -FilePath "dotnet" `
        -ArgumentList @($dllPath, "--instance", $FirstInstance) `
        -WorkingDirectory $repoRoot `
        -Environment @{
            "ASPNETCORE_ENVIRONMENT" = $Configuration
            "PORT" = "$firstPort"
        } `
        -RedirectStandardOutput $firstOutLog `
        -RedirectStandardError $firstErrLog `
        -PassThru

    Write-Host "Starting instance '$SecondInstance' on port $secondPort..."
    $secondProcess = Start-Process -FilePath "dotnet" `
        -ArgumentList @($dllPath, "--instance", $SecondInstance) `
        -WorkingDirectory $repoRoot `
        -Environment @{
            "ASPNETCORE_ENVIRONMENT" = $Configuration
            "PORT" = "$secondPort"
        } `
        -RedirectStandardOutput $secondOutLog `
        -RedirectStandardError $secondErrLog `
        -PassThru

    $firstUrl = "http://localhost:$firstPort"
    $secondUrl = "http://localhost:$secondPort"

    Wait-Healthy -BaseUrl $firstUrl -Process $firstProcess -ErrorLogPath $firstErrLog -TimeoutSeconds $TimeoutSeconds
    Wait-Healthy -BaseUrl $secondUrl -Process $secondProcess -ErrorLogPath $secondErrLog -TimeoutSeconds $TimeoutSeconds

    $firstTodo = Invoke-RestMethod -Uri "$firstUrl/mcpserver/todo" -Method Get
    $secondTodo = Invoke-RestMethod -Uri "$secondUrl/mcpserver/todo" -Method Get

    $firstCount = [int]$firstTodo.totalCount
    $secondCount = [int]$secondTodo.totalCount

    Write-Host "Instance '$FirstInstance' healthy at $firstUrl with todo count: $firstCount"
    Write-Host "Instance '$SecondInstance' healthy at $secondUrl with todo count: $secondCount"
    Write-Host "Logs: $firstOutLog, $firstErrLog, $secondOutLog, $secondErrLog"
    Write-Host "Multi-instance smoke test passed."
}
finally {
    if ($null -ne $firstProcess -and -not $firstProcess.HasExited) {
        Stop-Process -Id $firstProcess.Id -Force
    }

    if ($null -ne $secondProcess -and -not $secondProcess.HasExited) {
        Stop-Process -Id $secondProcess.Id -Force
    }
}
