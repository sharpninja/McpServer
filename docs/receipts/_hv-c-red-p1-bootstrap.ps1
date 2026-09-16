#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$utc = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
$stampPath = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p1-stamp.txt'
Set-Content -LiteralPath $stampPath -Value $utc -Encoding utf8
Write-Output "UTC=$utc"

$env:MCP_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:PLUGIN_AGENT_NAME = 'GrokSubagentHostile'
$env:MCP_AGENT_NAME = 'GrokSubagentHostile'
$env:MCP_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$env:GROK_WORKSPACE_PATH = 'F:\GitHub\McpServer'
Set-Location -LiteralPath 'F:\GitHub\McpServer'

$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p1'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

# Session-start hook (signature + nonce via plugin)
$sessionStartOut = Join-Path $outDir 'session-start.txt'
try {
    $ss = & 'F:\GitHub\mcpserver-grok-plugin\hooks\scripts\session-start.ps1' 2>&1 | Out-String
    Set-Content -LiteralPath $sessionStartOut -Value $ss -Encoding utf8
    Write-Output 'SESSION_START_OK'
} catch {
    Set-Content -LiteralPath $sessionStartOut -Value $_.ToString() -Encoding utf8
    Write-Output "SESSION_START_FAIL=$($_.Exception.Message)"
}

# Health-check hook
$healthOut = Join-Path $outDir 'health-check.txt'
try {
    $hc = & 'F:\GitHub\mcpserver-grok-plugin\hooks\scripts\health-check.ps1' 2>&1 | Out-String
    Set-Content -LiteralPath $healthOut -Value $hc -Encoding utf8
    Write-Output 'HEALTH_CHECK_OK'
} catch {
    Set-Content -LiteralPath $healthOut -Value $_.ToString() -Encoding utf8
    Write-Output "HEALTH_CHECK_FAIL=$($_.Exception.Message)"
}

$invoke = 'F:\GitHub\mcpserver-grok-plugin\lib\repl-invoke.ps1'

function Invoke-Plugin {
    param([string]$Method, [string]$ParamsYaml, [string]$OutName)
    $outFile = Join-Path $outDir $OutName
    try {
        $result = & pwsh.exe -NoProfile -NonInteractive -File $invoke -Method $Method -ParamsYaml $ParamsYaml 2>&1 | Out-String
        Set-Content -LiteralPath $outFile -Value $result -Encoding utf8
        Write-Output "INVOKE_OK method=$Method file=$OutName"
    } catch {
        Set-Content -LiteralPath $outFile -Value $_.ToString() -Encoding utf8
        Write-Output "INVOKE_FAIL method=$Method err=$($_.Exception.Message)"
    }
}

$bootYaml = @'
{}
'@
Invoke-Plugin -Method 'workflow.sessionlog.bootstrap' -ParamsYaml $bootYaml -OutName 'bootstrap.txt'

$sessionId = "GrokSubagentHostile-$utc-c-red-p1"
Set-Content -LiteralPath (Join-Path $outDir 'session-id.txt') -Value $sessionId -Encoding utf8

$openYaml = @"
agent: GrokSubagentHostile
sessionId: $sessionId
title: Hostile validate PLAN-PLUGINHANDOFF-001 C-red-P1
model: grok-build-subagent
"@
Invoke-Plugin -Method 'workflow.sessionlog.openSession' -ParamsYaml $openYaml -OutName 'open-session.txt'

$requestId = "req-$utc-001-hostile-validate-c-red-p1"
Set-Content -LiteralPath (Join-Path $outDir 'request-id.txt') -Value $requestId -Encoding utf8

$beginYaml = @"
requestId: $requestId
queryTitle: Hostile validate C-red-P1 plugin sessionlog tests
queryText: Independently re-verify PLAN-PLUGINHANDOFF-001 Phase C-red-P1 claims for PluginSessionLogIntegrationTargetTests, TODO state, missing green implementation, and B5 AGREE receipt.
"@
Invoke-Plugin -Method 'workflow.sessionlog.beginTurn' -ParamsYaml $beginYaml -OutName 'begin-turn.txt'

Write-Output "SESSION_ID=$sessionId"
Write-Output "REQUEST_ID=$requestId"
Write-Output 'BOOTSTRAP_DONE'
