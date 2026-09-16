#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p1-p3'
Set-Location -LiteralPath 'F:\GitHub\McpServer'
$env:MCP_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$env:PLUGIN_AGENT_NAME = 'GrokSubagentHostile'
$env:MCP_AGENT_NAME = 'GrokSubagentHostile'
$env:GROK_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:MCP_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$plugin = 'F:\GitHub\mcpserver-grok-plugin\lib\Invoke-McpPlugin.ps1'

function Save-Text {
    param([string]$Path, $Value)
    if ($null -eq $Value) { $Value = '' }
    if ($Value -isnot [string]) { $Value = ($Value | Out-String) }
    Set-Content -LiteralPath $Path -Value $Value -Encoding utf8
}

# Plugin Test-MarkerSignature
. 'F:\GitHub\mcpserver-grok-plugin\lib\marker-resolver.ps1'
$markerFile = 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml'
$sigOk = Test-MarkerSignature -MarkerFile $markerFile
$bootOk = $false
try {
    $bootOk = Invoke-FullBootstrap -StartDir 'F:\GitHub\McpServer'
} catch {
    $bootOk = $false
    Save-Text (Join-Path $out 'plugin-bootstrap.err.txt') $_.Exception.ToString()
}
[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    TestMarkerSignature = [bool]$sigOk
    InvokeFullBootstrap = [bool]$bootOk
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'plugin-marker-sig.json') -Encoding utf8
Write-Output ("PLUGIN_SIG=$sigOk BOOTSTRAP=$bootOk")

# Session-start hook
try {
    $ss = & 'F:\GitHub\mcpserver-grok-plugin\hooks\scripts\session-start.ps1' 2>&1 | Out-String
    Save-Text (Join-Path $out 'session-start.txt') $ss
    Write-Output 'SESSION_START_OK'
} catch {
    Save-Text (Join-Path $out 'session-start.txt') $_.ToString()
    Write-Output 'SESSION_START_FAIL'
}

# TRX parse without assuming skipped
function Get-TrxSummary {
    param([string]$TrxPath)
    if (-not (Test-Path -LiteralPath $TrxPath)) {
        return [ordered]@{ exists = $false; path = $TrxPath }
    }
    [xml]$x = Get-Content -LiteralPath $TrxPath
    $c = $x.TestRun.ResultSummary.Counters
    $unitNames = @()
    $outcomes = @()
    foreach ($u in $x.TestRun.Results.UnitTestResult) {
        $unitNames += [string]$u.testName
        $outcomes += [ordered]@{ name = [string]$u.testName; outcome = [string]$u.outcome }
    }
    return [ordered]@{
        exists = $true
        path = $TrxPath
        outcome = [string]$x.TestRun.ResultSummary.outcome
        total = [string]$c.total
        executed = [string]$c.executed
        passed = [string]$c.passed
        failed = [string]$c.failed
        skipped = $(if ($c.skipped) { [string]$c.skipped } else { '0-missing-attr' })
        notExecuted = $(if ($c.notExecuted) { [string]$c.notExecuted } else { '0-missing-attr' })
        unitTestNames = $unitNames
        unitOutcomes = $outcomes
    }
}
$buildTrx = Get-TrxSummary -TrxPath (Join-Path $out 'build-pluginsessionlog.trx')
$pluginTrx = Get-TrxSummary -TrxPath (Join-Path $out 'pluginintegration.trx')
[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    Build = $buildTrx
    Plugin = $pluginTrx
} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $out 'trx-parsed.json') -Encoding utf8
Write-Output ('TRX_BUILD_PASSED=' + $buildTrx.passed + ' FAILED=' + $buildTrx.failed + ' TOTAL=' + $buildTrx.total)
Write-Output ('TRX_PLUGIN_PASSED=' + $pluginTrx.passed + ' FAILED=' + $pluginTrx.failed + ' TOTAL=' + $pluginTrx.total)

# sln timestamp
$sln = Get-Item -LiteralPath 'F:\GitHub\McpServer\McpServer.sln'
[ordered]@{
    Path = $sln.FullName
    LastWriteTimeUtc = $sln.LastWriteTimeUtc.ToString('o')
    Length = $sln.Length
    AfterCRedP1Agree = ($sln.LastWriteTimeUtc -gt [DateTime]::Parse('2026-08-21T23:25:45Z').ToUniversalTime())
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'sln-timestamp.json') -Encoding utf8

# Tool search without StrictMode items trap
$raw = Get-Content -LiteralPath 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml' -Raw
$apiKey = if ($raw -match '(?m)^apiKey:\s*(.+)$') { $Matches[1].Trim() } else { '' }
$headers = @{ 'X-Api-Key' = $apiKey }
try {
    $resp = Invoke-WebRequest -Uri 'http://PAYTON-LEGION2:7147/mcpserver/tools/search?keyword=mcpserver-grok-plugin' -Headers $headers -Method Get -TimeoutSec 30
    Save-Text (Join-Path $out 'tool-search-raw.json') $resp.Content
    Write-Output ("TOOL_SEARCH_STATUS=" + [int]$resp.StatusCode)
} catch {
    Save-Text (Join-Path $out 'tool-search-raw.json') $_.Exception.ToString()
    Write-Output 'TOOL_SEARCH_FAIL'
}

# Client getFr createdAt compare
try {
    $fr = & $plugin -Command Invoke -Method 'client.Requirements.GetFrAsync' -ParamsObject @{ id = 'FR-MCP-PLUGININT-001' } -WorkspacePath 'F:\GitHub\McpServer' -TimeoutSeconds 120
    Save-Text (Join-Path $out 'req-fr-client.txt') $fr
    Write-Output 'OK req-fr-client'
} catch {
    Save-Text (Join-Path $out 'req-fr-client.txt') $_.Exception.ToString()
}

# git sln diff excerpt
$slnDiff = git diff -- 'McpServer.sln' 2>&1
Save-Text (Join-Path $out 'git-diff-sln.txt') $slnDiff

Write-Output 'FOLLOWUP_DONE'
