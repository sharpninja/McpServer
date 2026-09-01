#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$pluginRoot = 'F:\GitHub\mcpserver-grok-plugin'
$workspace = 'F:\GitHub\McpServer'
$outDir = Join-Path $workspace 'docs\receipts\_h0-hostile-raw'
$env:MCP_PLUGIN_ROOT = $pluginRoot
$env:GROK_PLUGIN_ROOT = $pluginRoot
$env:PLUGIN_AGENT_NAME = 'GrokCode'
$env:MCP_AGENT_NAME = 'GrokCode'
$env:MCP_WORKSPACE_PATH = $workspace
Set-Location -LiteralPath $workspace

$invoke = Join-Path $pluginRoot 'lib\Invoke-McpPlugin.ps1'

# Window ends before this hostile turn opened, so the query text cannot contaminate.
$q = & $invoke -Command Invoke -Method 'client.SessionLog.QueryAsync' -Params @"
agent: GrokCode
from: 2026-08-18T18:27:00Z
to: 2026-08-18T19:18:00Z
limit: 50
"@ -WorkspacePath $workspace
Set-Content -LiteralPath (Join-Path $outDir '24-sessionlog-query-window.txt') -Value ([string]$q) -Encoding utf8

$raw = [string]$q
$needle = 'GrokCode-20260818T182741Z-plugin-session'
$hasSess = $raw.Contains($needle)
$hasTurn = $raw.Contains('req-20260818T191655Z-004-s0-triagecluster-reqs')
Write-Output "WINDOW length=$($raw.Length) hasSessionId=$hasSess hasTurnId=$hasTurn"

# Extract requestIds near the implementer session if present.
if ($hasSess) {
    $idx = $raw.IndexOf($needle)
    $slice = $raw.Substring($idx, [Math]::Min(8000, $raw.Length - $idx))
    Set-Content -LiteralPath (Join-Path $outDir '24b-implementer-session-slice.txt') -Value $slice -Encoding utf8
    Write-Output 'WROTE 24b-implementer-session-slice.txt'
}

# Also extract from the earlier unfiltered dump.
$dump = Join-Path $outDir '20-sessionlog-query-implementer-turn.txt'
if (Test-Path -LiteralPath $dump) {
    $big = Get-Content -LiteralPath $dump -Raw
    $hasSess2 = $big.Contains($needle)
    $hasTurn2 = $big.Contains('req-20260818T191655Z-004-s0-triagecluster-reqs')
    Write-Output "DUMP length=$($big.Length) hasSessionId=$hasSess2 hasTurnId=$hasTurn2"
    if ($hasSess2) {
        $idx2 = $big.IndexOf($needle)
        $slice2 = $big.Substring($idx2, [Math]::Min(8000, $big.Length - $idx2))
        Set-Content -LiteralPath (Join-Path $outDir '24c-dump-implementer-slice.txt') -Value $slice2 -Encoding utf8
        Write-Output 'WROTE 24c-dump-implementer-slice.txt'
    }
}

# Try GET-style client method names if they exist.
foreach ($method in @(
        'client.SessionLog.GetAsync'
        'client.SessionLog.GetSessionAsync'
        'workflow.sessionlog.getSession'
    )) {
    try {
        $r = & $invoke -Command Invoke -Method $method -Params @"
agent: GrokCode
sessionId: GrokCode-20260818T182741Z-plugin-session
"@ -WorkspacePath $workspace
        Set-Content -LiteralPath (Join-Path $outDir ("25-$($method.Replace('.','_')).txt")) -Value ([string]$r) -Encoding utf8
        Write-Output "METHOD $method length=$($r.ToString().Length)"
    } catch {
        Write-Output "METHOD $method FAIL $($_.Exception.Message.Substring(0, [Math]::Min(200, $_.Exception.Message.Length)))"
    }
}
