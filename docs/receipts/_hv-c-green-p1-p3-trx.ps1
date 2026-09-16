#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p1-p3'
Set-Location -LiteralPath 'F:\GitHub\McpServer'
$env:MCP_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$env:PLUGIN_AGENT_NAME = 'GrokSubagentHostile'
$env:MCP_AGENT_NAME = 'GrokSubagentHostile'
$env:GROK_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:MCP_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$plugin = 'F:\GitHub\mcpserver-grok-plugin\lib\Invoke-McpPlugin.ps1'

function Get-Attr {
    param($Node, [string]$Name)
    if ($null -eq $Node) { return 'missing-node' }
    $a = $Node.Attributes[$Name]
    if ($null -eq $a) { return 'missing-attr' }
    return [string]$a.Value
}

function Get-TrxSummary {
    param([string]$TrxPath)
    if (-not (Test-Path -LiteralPath $TrxPath)) {
        return [ordered]@{ exists = $false; path = $TrxPath }
    }
    [xml]$x = Get-Content -LiteralPath $TrxPath
    $c = $x.TestRun.ResultSummary.Counters
    $unitOutcomes = @()
    if ($null -ne $x.TestRun.Results -and $null -ne $x.TestRun.Results.UnitTestResult) {
        foreach ($u in @($x.TestRun.Results.UnitTestResult)) {
            $unitOutcomes += [ordered]@{
                name = [string]$u.testName
                outcome = [string]$u.outcome
            }
        }
    }
    return [ordered]@{
        exists = $true
        path = $TrxPath
        outcome = [string]$x.TestRun.ResultSummary.outcome
        total = Get-Attr $c 'total'
        executed = Get-Attr $c 'executed'
        passed = Get-Attr $c 'passed'
        failed = Get-Attr $c 'failed'
        skipped = Get-Attr $c 'skipped'
        notExecuted = Get-Attr $c 'notExecuted'
        inconclusive = Get-Attr $c 'inconclusive'
        unitOutcomes = $unitOutcomes
    }
}

$buildTrx = Get-TrxSummary -TrxPath (Join-Path $out 'build-pluginsessionlog.trx')
$pluginTrx = Get-TrxSummary -TrxPath (Join-Path $out 'pluginintegration.trx')
[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    Build = $buildTrx
    Plugin = $pluginTrx
} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $out 'trx-parsed.json') -Encoding utf8
Write-Output ('BUILD outcome=' + $buildTrx.outcome + ' passed=' + $buildTrx.passed + ' failed=' + $buildTrx.failed + ' skipped=' + $buildTrx.skipped + ' notExecuted=' + $buildTrx.notExecuted)
Write-Output ('PLUGIN outcome=' + $pluginTrx.outcome + ' passed=' + $pluginTrx.passed + ' failed=' + $pluginTrx.failed + ' skipped=' + $pluginTrx.skipped + ' notExecuted=' + $pluginTrx.notExecuted)

$sln = Get-Item -LiteralPath 'F:\GitHub\McpServer\McpServer.sln'
$agreeCutoff = [DateTime]::Parse('2026-08-21T23:25:45Z').ToUniversalTime()
[ordered]@{
    Path = $sln.FullName
    LastWriteTimeUtc = $sln.LastWriteTimeUtc.ToString('o')
    Length = $sln.Length
    AfterCRedP1Agree = ($sln.LastWriteTimeUtc -gt $agreeCutoff)
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'sln-timestamp.json') -Encoding utf8

$raw = Get-Content -LiteralPath 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml' -Raw
$apiKey = if ($raw -match '(?m)^apiKey:\s*(.+)$') { $Matches[1].Trim() } else { '' }
$headers = @{ 'X-Api-Key' = $apiKey }
try {
    $resp = Invoke-WebRequest -Uri 'http://PAYTON-LEGION2:7147/mcpserver/tools/search?keyword=mcpserver-grok-plugin' -Headers $headers -Method Get -TimeoutSec 30
    Set-Content -LiteralPath (Join-Path $out 'tool-search-raw.json') -Value $resp.Content -Encoding utf8
    Write-Output ("TOOL_SEARCH_STATUS=" + [int]$resp.StatusCode + ' LEN=' + $resp.Content.Length)
} catch {
    Set-Content -LiteralPath (Join-Path $out 'tool-search-raw.json') -Value $_.Exception.ToString() -Encoding utf8
    Write-Output 'TOOL_SEARCH_FAIL'
}

try {
    $fr = & $plugin -Command Invoke -Method 'client.Requirements.GetFrAsync' -ParamsObject @{ id = 'FR-MCP-PLUGININT-001' } -WorkspacePath 'F:\GitHub\McpServer' -TimeoutSeconds 120
    if ($fr -isnot [string]) { $fr = ($fr | Out-String) }
    Set-Content -LiteralPath (Join-Path $out 'req-fr-client.txt') -Value $fr -Encoding utf8
    Write-Output 'OK req-fr-client'
} catch {
    Set-Content -LiteralPath (Join-Path $out 'req-fr-client.txt') -Value $_.Exception.ToString() -Encoding utf8
    Write-Output 'FAIL req-fr-client'
}

$slnDiff = git diff --unified=3 -- 'McpServer.sln' 2>&1 | Out-String
Set-Content -LiteralPath (Join-Path $out 'git-diff-sln.txt') -Value $slnDiff -Encoding utf8

Write-Output 'TRX_FOLLOWUP_DONE'
