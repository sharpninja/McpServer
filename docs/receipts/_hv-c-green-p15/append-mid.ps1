#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p15'
$plugin = 'F:\GitHub\mcpserver-grok-plugin\lib\Invoke-McpPlugin.ps1'
$cacheRoot = Join-Path $out 'plugin-cache'
Set-Location -LiteralPath 'F:\GitHub\McpServer'
$env:MCP_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$env:MCPSERVER_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$env:PLUGIN_AGENT_NAME = 'GrokSubagentHostile'
$env:MCP_AGENT_NAME = 'GrokSubagentHostile'
$env:GROK_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:MCP_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:MCP_PLUGIN_HOST = 'grok'
$agent = 'GrokSubagentHostile'
$sessionId = (Get-Content -LiteralPath (Join-Path $out 'session-id.txt') -Raw).Trim()
$requestId = (Get-Content -LiteralPath (Join-Path $out 'request-id.txt') -Raw).Trim()

function Invoke-Named {
    param([string]$Name, [string]$Method, [hashtable]$Params)
    $outFile = Join-Path $out ($Name + '.txt')
    $errFile = Join-Path $out ($Name + '.err.txt')
    try {
        $result = & $plugin -Command Invoke -Method $Method -ParamsObject $Params -WorkspacePath 'F:\GitHub\McpServer' -CacheRoot $cacheRoot -TimeoutSeconds 120 2>&1 | ForEach-Object { $_.ToString() }
        Set-Content -LiteralPath $outFile -Value (($result | Out-String)) -Encoding utf8
        Write-Output ('OK ' + $Name)
    } catch {
        Set-Content -LiteralPath $outFile -Value ('ERROR ' + $_.Exception.ToString()) -Encoding utf8
        Set-Content -LiteralPath $errFile -Value ($_.ToString()) -Encoding utf8
        Write-Output ('FAIL ' + $Name + ' ' + $_.Exception.Message)
    }
}

$now = [DateTime]::UtcNow.ToString('o')
Invoke-Named -Name 'sl-dialog-mid' -Method 'client.SessionLog.AppendDialogAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    items = @(
        @{ timestamp = $now; role = 'model'; category = 'observation'; content = 'Independent P15 filter ExitCode 0. Console Passed! Failed 0 Passed 24 Skipped 0 Total 24 Duration 5 m 43 s. TRX executed 24 passed 24 failed 0 notExecuted 0 unitCount 24 p16Count 0. Success 8 FailedSubmit 8 Retry 8 all passed. list-tests P15 8+8+8 P16 0 Skip 0. Live todo_get PLAN-PLUGINHANDOFF-001 done false; MCP-PLUGININT-001 done false P15 done false. Adapter LastWriteTimeUtc 2026-08-22T05:40:18Z after C-red AGREE 05:35:39Z. Full PluginIntegration suite still running.' }
        @{ timestamp = $now; role = 'model'; category = 'decision'; content = 'Decision: do not AGREE until the full PluginIntegration.Tests run is Failed 0 Skipped 0. Consequence: filter green is necessary but not sufficient for C-green-P15. Alternatives: AGREE on filter only. Rejected: parent required the full suite and Byrd exit is current-plus-prior Failed 0 Skipped 0.' }
    )
}

Invoke-Named -Name 'sl-actions-mid' -Method 'client.SessionLog.AppendActionsAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    actions = @(
        @{ order = 1; description = 'add-profile: read 18 non-skill profile markdown files plus add-profile SKILL.md and hostile-validator SKILL.md'; type = 'edit'; status = 'completed'; filePath = 'C:\Users\kingd\.claude\profile\PROFILE.md' }
        @{ order = 2; description = 'Plugin Test-MarkerSignature true; health nonce nonce-hv-20260822062055-15239 echoed; tool search exact mcpserver-grok-plugin; OpenSession created true; BeginTurn retry turnId 42908'; type = 'edit'; status = 'completed'; filePath = 'F:\GitHub\mcpserver-grok-plugin\lib\Invoke-McpPlugin.ps1' }
        @{ order = 3; description = 'Live workflow.todo.get and client.Todo.GetAsync PLAN-PLUGINHANDOFF-001 done false; MCP-PLUGININT-001 done false; P15 task done false; combined C P14/P15 task done false'; type = 'edit'; status = 'completed'; filePath = 'docs/plans/PLAN-PLUGINHANDOFF-001.md' }
        @{ order = 4; description = 'Independent P15 filter Passed 24 Failed 0 Skipped 0 Total 24 Duration 5 m 43 s; TRX unitCount 24 p16Count 0'; type = 'test'; status = 'completed'; filePath = 'tests/McpServer.PluginIntegration.Tests/PluginSessionLogWorkflowAdapterTests.cs' }
        @{ order = 5; description = 'Adapter ExecuteCanonicalTurnAsync sets FailsafePathVerified true and throws on matching leftover; ExecuteFailedSubmitAsync writes sessionlog-{sessionId}-*-pending.yaml; RetryFailedSubmitAsync writes sessionlog-sibling-unrelated.yaml then deletes matching glob'; type = 'edit'; status = 'completed'; filePath = 'tests/McpServer.PluginIntegration.Tests/PluginSessionLogWorkflowAdapter.cs' }
        @{ order = 6; description = 'Decision: withhold OverallVerdict until full PluginIntegration.Tests Failed 0 Skipped 0'; type = 'design_decision'; status = 'completed'; filePath = 'docs/plans/PLAN-PLUGINHANDOFF-001.md' }
    )
}

Write-Output 'APPEND_MID_DONE'
