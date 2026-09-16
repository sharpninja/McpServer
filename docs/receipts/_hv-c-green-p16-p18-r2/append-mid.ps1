#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p16-p18-r2'
$plugin = 'F:\GitHub\mcpserver-grok-plugin\lib\Invoke-McpPlugin.ps1'
$cacheRoot = Join-Path $out 'plugin-cache'
Set-Location -LiteralPath 'F:\GitHub\McpServer'
$env:MCP_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$env:MCPSERVER_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$env:GROK_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:MCP_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:PLUGIN_AGENT_NAME = 'GrokSubagentHostile'
$env:MCP_AGENT_NAME = 'GrokSubagentHostile'
$env:MCP_PLUGIN_HOST = 'grok'

$agent = 'GrokSubagentHostile'
$sessionId = (Get-Content -LiteralPath (Join-Path $out 'session-id.txt') -Raw).Trim()
$requestId = (Get-Content -LiteralPath (Join-Path $out 'request-id.txt') -Raw).Trim()
$now = [DateTime]::UtcNow.ToString('o')

$result = & $plugin -Command Invoke -Method 'client.SessionLog.AppendDialogAsync' -ParamsObject @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    items = @(
        @{ timestamp = $now; role = 'model'; category = 'observation'; content = 'Source re-read: Build.PluginSessionLogIntegration.cs LastWriteTimeUtc 2026-08-22T08:27:01Z after prior DISAGREE 08:17:50Z. PreflightPluginSessionLogAiUnitStrategy checks SharpNinja.aiUnit, appsettings.aiunit.json ActiveStrategy/Strategies/grok-build. Target runs PluginInt=Deterministic TRX then FailIfSkipped then PluginInt=AI TRX then FailIfSkipped. NukeTarget_SkipIsFailure calls Preflight. P19 names 0 in *.cs. PLAN and MCP-PLUGININT-001 live done false. Independent PluginInt=AI deferred until leftover implementer testhost from docs/receipts/_p18-preflight-20260822T082521Z/ finishes. Did not kill that process.' }
        @{ timestamp = $now; role = 'model'; category = 'decision'; content = 'Decision: do not treat implementer _p18-preflight TRX as this review independent rerun. Wait for leftover PluginIntegration testhost, then run PluginInt=AI and list-tests in unique ResultsDirectory. Run NukeTarget_SkipIsFailure now because Build.Tests does not share that testhost. Consequence: A3 stays UNKNOWN until independent AI TRX exists. Alternatives rejected: kill implementer testhost; claim AGREE from source-only C4/D2; use implementer trx-ai as proof.' }
    )
} -WorkspacePath 'F:\GitHub\McpServer' -CacheRoot $cacheRoot -TimeoutSeconds 120
Set-Content -LiteralPath (Join-Path $out 'sl-dialog-mid.txt') -Value ($result | Out-String) -Encoding utf8
Write-Output 'APPEND_MID_DONE'
