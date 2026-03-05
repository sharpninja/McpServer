<#
.SYNOPSIS
    Tests the ngrok tunnel for MCP Server.

.DESCRIPTION
    Validates that the ngrok tunnel provider can be enabled, started, and that
    the public URL is reachable. Tests health, tunnel list, and MCP transport
    endpoints through the tunnel. Cleans up by stopping the tunnel on exit.

.PARAMETER BaseUrl
    Local MCP Server base URL. Default: http://localhost:7147

.PARAMETER ApiKey
    API key for authenticated endpoints. If omitted, reads from AGENTS-README-FIRST.yaml
    in the repository root.

.PARAMETER SkipCleanup
    If set, leaves the tunnel running after tests complete.

.PARAMETER TimeoutSeconds
    Maximum seconds to wait for the tunnel to become active. Default: 15

.EXAMPLE
    .\scripts\Test-McpNgrokTunnel.ps1
    .\scripts\Test-McpNgrokTunnel.ps1 -SkipCleanup
    .\scripts\Test-McpNgrokTunnel.ps1 -ApiKey "my-key" -TimeoutSeconds 30
#>
[CmdletBinding()]
param(
    [string]$BaseUrl = "http://localhost:7147",
    [string]$ApiKey,
    [switch]$SkipCleanup,
    [int]$TimeoutSeconds = 15
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# ── Helpers ──────────────────────────────────────────────────────────────────

function Write-Step   { param([string]$Msg) Write-Host "  ► $Msg" -ForegroundColor Cyan }
function Write-Pass   { param([string]$Msg) Write-Host "  ✓ $Msg" -ForegroundColor Green }
function Write-Fail   { param([string]$Msg) Write-Host "  ✗ $Msg" -ForegroundColor Red }
function Write-Info   { param([string]$Msg) Write-Host "    $Msg" -ForegroundColor DarkGray }
function Write-Section { param([string]$Msg) Write-Host "`n═══ $Msg ═══" -ForegroundColor Yellow }

function Get-Headers {
    param([switch]$ViaTunnel)
    $h = @{}
    if ($script:ApiKey) { $h["X-Api-Key"] = $script:ApiKey }
    if ($ViaTunnel) { $h["ngrok-skip-browser-warning"] = "true" }
    return $h
}

function Invoke-McpApi {
    param(
        [string]$Method = "GET",
        [string]$Url,
        [switch]$AllowFailure,
        [switch]$ViaTunnel
    )
    try {
        $params = @{
            Uri             = $Url
            Method          = $Method
            Headers         = (Get-Headers -ViaTunnel:$ViaTunnel)
            TimeoutSec      = 10
            ErrorAction     = "Stop"
        }
        return Invoke-RestMethod @params
    }
    catch {
        if ($AllowFailure) { return $null }
        throw
    }
}

# ── Resolve API Key ──────────────────────────────────────────────────────────

if (-not $ApiKey) {
    $markerPath = Join-Path $PSScriptRoot "..\AGENTS-README-FIRST.yaml"
    if (Test-Path $markerPath) {
        $markerContent = Get-Content -Raw $markerPath
        if ($markerContent -match 'apiKey:\s*(.+)') {
            $ApiKey = $Matches[1].Trim()
            Write-Info "API key read from marker file."
        }
    }
    if (-not $ApiKey) {
        Write-Fail "No API key provided and AGENTS-README-FIRST.yaml not found."
        Write-Info "Pass -ApiKey or ensure the MCP server is running."
        exit 1
    }
}

$passed = 0
$failed = 0

# ── 1. Server Health ─────────────────────────────────────────────────────────

Write-Section "Server Health"
Write-Step "Checking $BaseUrl/health ..."
try {
    $health = Invoke-RestMethod -Uri "$BaseUrl/health" -TimeoutSec 5
    if ($health.status -eq "Healthy") {
        Write-Pass "Server is healthy."
        $passed++
    }
    else {
        Write-Fail "Unexpected health status: $($health.status)"
        $failed++
    }
}
catch {
    Write-Fail "Server not reachable at $BaseUrl — is it running?"
    Write-Info "$_"
    exit 1
}

# ── 2. Tunnel Provider List ──────────────────────────────────────────────────

Write-Section "Tunnel Providers"
Write-Step "Listing tunnel providers ..."
$providers = Invoke-McpApi -Url "$BaseUrl/mcpserver/tunnel/list"
$ngrok = $providers | Where-Object { $_.provider -eq "ngrok" }

if (-not $ngrok) {
    Write-Fail "ngrok provider not registered."
    exit 1
}

Write-Pass "ngrok provider found (enabled=$($ngrok.enabled), running=$($ngrok.isRunning))"
$passed++

foreach ($p in $providers) {
    Write-Info "$($p.provider): enabled=$($p.enabled) running=$($p.isRunning) url=$($p.publicUrl)"
}

# ── 3. Enable ngrok ──────────────────────────────────────────────────────────

Write-Section "Enable ngrok"
if (-not $ngrok.enabled) {
    Write-Step "Enabling ngrok provider ..."
    $result = Invoke-McpApi -Method POST -Url "$BaseUrl/mcpserver/tunnel/ngrok/enable"
    if ($result.enabled) {
        Write-Pass "ngrok enabled."
        $passed++
    }
    else {
        Write-Fail "Failed to enable ngrok."
        $failed++
    }
}
else {
    Write-Pass "ngrok already enabled."
    $passed++
}

# ── 4. Start ngrok ───────────────────────────────────────────────────────────

Write-Section "Start ngrok"
Write-Step "Starting ngrok tunnel ..."
$startResult = Invoke-McpApi -Method POST -Url "$BaseUrl/mcpserver/tunnel/ngrok/start"
if ($startResult.isRunning) {
    Write-Pass "ngrok is running."
    $passed++
}
else {
    Write-Fail "ngrok failed to start: $($startResult.error)"
    $failed++
}

# ── 5. Wait for Public URL ───────────────────────────────────────────────────

Write-Section "Public URL"
$publicUrl = $null
$deadline = (Get-Date).AddSeconds($TimeoutSeconds)

Write-Step "Waiting up to ${TimeoutSeconds}s for public URL ..."
while ((Get-Date) -lt $deadline) {
    $status = Invoke-McpApi -Url "$BaseUrl/mcpserver/tunnel/ngrok/status"
    if ($status.publicUrl) {
        $publicUrl = $status.publicUrl
        break
    }
    Start-Sleep -Milliseconds 1000
}

if ($publicUrl) {
    Write-Pass "Public URL: $publicUrl"
    $passed++
}
else {
    Write-Fail "Timed out waiting for public URL."
    Write-Info "Last status error: $($status.error)"
    $failed++

    # Cannot test remote endpoints without URL — skip to cleanup
    Write-Section "Results"
    Write-Host "`n  Passed: $passed  |  Failed: $failed" -ForegroundColor $(if ($failed -gt 0) { "Red" } else { "Green" })
    if (-not $SkipCleanup) {
        Write-Step "Stopping ngrok ..."
        Invoke-McpApi -Method POST -Url "$BaseUrl/mcpserver/tunnel/ngrok/stop" -AllowFailure | Out-Null
    }
    exit 1
}

# ── 6. Test Remote Endpoints ─────────────────────────────────────────────────

Write-Section "Remote Endpoint Tests"

# 6a. Health through tunnel
Write-Step "Testing /health through tunnel ..."
try {
    $remoteHealth = Invoke-RestMethod -Uri "$publicUrl/health" -Headers @{ "ngrok-skip-browser-warning" = "true" } -TimeoutSec 10
    if ($remoteHealth.status -eq "Healthy") {
        Write-Pass "/health OK through tunnel."
        $passed++
    }
    else {
        Write-Fail "/health returned unexpected status: $($remoteHealth.status)"
        $failed++
    }
}
catch {
    Write-Fail "/health not reachable through tunnel: $_"
    $failed++
}

# 6b. Tunnel list through tunnel (authenticated)
Write-Step "Testing /mcpserver/tunnel/list through tunnel ..."
try {
    $remoteTunnels = Invoke-McpApi -Url "$publicUrl/mcpserver/tunnel/list" -ViaTunnel
    $remoteNgrok = $remoteTunnels | Where-Object { $_.provider -eq "ngrok" }
    if ($remoteNgrok -and $remoteNgrok.isRunning) {
        Write-Pass "/mcpserver/tunnel/list OK (ngrok running)."
        $passed++
    }
    else {
        Write-Fail "/mcpserver/tunnel/list returned unexpected data."
        $failed++
    }
}
catch {
    Write-Fail "/mcpserver/tunnel/list failed through tunnel: $_"
    $failed++
}

# 6c. MCP transport endpoint (OPTIONS/POST check)
Write-Step "Testing /mcp-transport reachability ..."
try {
    # MCP Streamable HTTP requires Accept: application/json, text/event-stream
    $mcpHeaders = @{
        "ngrok-skip-browser-warning" = "true"
        "Accept" = "application/json, text/event-stream"
    }
    $resp = Invoke-WebRequest -Uri "$publicUrl/mcp-transport" -Method POST `
        -ContentType "application/json" `
        -Headers $mcpHeaders `
        -Body '{"jsonrpc":"2.0","method":"initialize","params":{"protocolVersion":"2025-03-26","capabilities":{},"clientInfo":{"name":"test","version":"0.1"}},"id":1}' `
        -TimeoutSec 10 -ErrorAction Stop
    if ($resp.StatusCode -ge 200 -and $resp.StatusCode -lt 500) {
        Write-Pass "/mcp-transport reachable (HTTP $($resp.StatusCode))."
        $passed++
    }
    else {
        Write-Fail "/mcp-transport returned HTTP $($resp.StatusCode)."
        $failed++
    }
}
catch [System.Net.WebException] {
    $webResp = $_.Exception.Response
    if ($webResp -and $webResp.StatusCode) {
        $code = [int]$webResp.StatusCode
        if ($code -lt 500) {
            Write-Pass "/mcp-transport reachable (HTTP $code — expected for bare POST)."
            $passed++
        }
        else {
            Write-Fail "/mcp-transport returned HTTP $code."
            $failed++
        }
    }
    else {
        Write-Fail "/mcp-transport not reachable: $_"
        $failed++
    }
}
catch {
    Write-Fail "/mcp-transport not reachable: $_"
    $failed++
}

# 6d. Latency measurement
Write-Step "Measuring round-trip latency ..."
try {
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    Invoke-RestMethod -Uri "$publicUrl/health" -Headers @{ "ngrok-skip-browser-warning" = "true" } -TimeoutSec 10 | Out-Null
    $sw.Stop()
    $latencyMs = $sw.ElapsedMilliseconds
    if ($latencyMs -lt 5000) {
        Write-Pass "Latency: ${latencyMs}ms"
        $passed++
    }
    else {
        Write-Fail "High latency: ${latencyMs}ms"
        $failed++
    }
}
catch {
    Write-Fail "Latency test failed: $_"
    $failed++
}

# ── 7. Cleanup ────────────────────────────────────────────────────────────────

Write-Section "Cleanup"
if ($SkipCleanup) {
    Write-Info "Skipping cleanup (-SkipCleanup). Tunnel remains active at: $publicUrl"
}
else {
    Write-Step "Stopping ngrok tunnel ..."
    $stopResult = Invoke-McpApi -Method POST -Url "$BaseUrl/mcpserver/tunnel/ngrok/stop" -AllowFailure
    if ($stopResult -and -not $stopResult.isRunning) {
        Write-Pass "ngrok stopped."
    }
    else {
        Write-Info "ngrok may still be running — check manually."
    }
}

# ── Results ───────────────────────────────────────────────────────────────────

Write-Section "Results"
$total = $passed + $failed
Write-Host "`n  Passed: $passed / $total  |  Failed: $failed" -ForegroundColor $(if ($failed -gt 0) { "Red" } else { "Green" })
Write-Host ""

if ($failed -gt 0) { exit 1 }
exit 0
