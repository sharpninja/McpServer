$ErrorActionPreference = 'Stop'
$out = 'F:\GitHub\McpServer\docs\receipts\_hv-b5-20260821T224147Z'
$plugin = 'F:\GitHub\mcpserver-grok-plugin\lib\Invoke-McpPlugin.ps1'
Set-Location 'F:\GitHub\McpServer'
$env:MCP_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$env:PLUGIN_AGENT_NAME = 'GrokSubagentHostile'
$env:MCP_AGENT_NAME = 'GrokSubagentHostile'
$env:GROK_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:MCP_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'

$agent = 'GrokSubagentHostile'
$sessionId = 'GrokSubagentHostile-20260821T224147Z-b5-green'
$requestId = 'req-20260821T224147Z-001-phase-b5-green-gate'
$now = (Get-Date).ToUniversalTime().ToString('o')

function Invoke-Plugin {
    param([string]$Method, [hashtable]$Params, [string]$Name)
    $outFile = Join-Path $out ($Name + '.txt')
    $errFile = Join-Path $out ($Name + '.err.txt')
    try {
        $result = & $plugin -Command Invoke -Method $Method -ParamsObject $Params -WorkspacePath 'F:\GitHub\McpServer' -TimeoutSeconds 120 2> $errFile
        if ($null -eq $result) { $result = '' }
        if ($result -isnot [string]) { $result = ($result | Out-String) }
        Set-Content -LiteralPath $outFile -Value $result -Encoding utf8
        Write-Output ('OK ' + $Name)
    } catch {
        Set-Content -LiteralPath $outFile -Value ('ERROR ' + $_.Exception.ToString()) -Encoding utf8
        Write-Output ('FAIL ' + $Name + ' ' + $_.Exception.Message)
        throw
    }
}

Invoke-Plugin -Method 'workflow.sessionlog.bootstrap' -Params @{} -Name 'sl-bootstrap'

Invoke-Plugin -Method 'client.SessionLog.OpenSessionAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    title = 'Hostile B5 green gate PLAN-PLUGINHANDOFF-001 Phase B'
    model = 'grok-4-1-fast'
} -Name 'sl-open'

Invoke-Plugin -Method 'client.SessionLog.BeginTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    queryTitle = 'Hostile B5 green gate Phase B'
    queryText = 'Independent hostile validation of PLAN-PLUGINHANDOFF-001 Phase B5 green gate claims for plugin core dialog parse, REPL degraded persistence, drain timeout, V4 failsafe path, and SyncAgentPlugins checksums.'
    model = 'grok-4-1-fast'
    planFile = 'docs/plans/PLAN-PLUGINHANDOFF-001.md'
    todoId = 'PLAN-PLUGINHANDOFF-001'
} -Name 'sl-begin'

$dialogItems = @(
    @{ timestamp = $now; role = 'model'; category = 'observation'; content = 'add-profile executed. 18 non-skill profile files read. Work class 1. Surfaces A+B+C+D apply.' }
    @{ timestamp = $now; role = 'model'; category = 'decision'; content = 'Decision: score B5 on independently re-run tests and live store, not implementer chat. Consequence: AGREE only if A+B+C+D all PASS. Alternatives rejected: trusting cited logs without re-run; treating markdown projection as store.' }
    @{ timestamp = $now; role = 'model'; category = 'observation'; content = 'B2 receipt OverallVerdict AGREE still on disk. Drain return 2 absent. Get-McpFailsafeDir V4 pending path. Copy-CoreFile Copy-Item. Coordinator tests 7/0/0. Checksum 1/0/0. Persist reuse 13/0/0. Focused Pester 6/0/0. Full Pester 124/0/0. Repl.Core 847/0/0. TODOs PLAN and PLUGINCORE Done=false.' }
)

Invoke-Plugin -Method 'client.SessionLog.AppendDialogAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    items = $dialogItems
} -Name 'sl-dialog'

$actions = @(
    @{ order = 1; description = 'Executed add-profile; read 18 profile markdown files'; type = 'read'; status = 'completed'; filePath = 'C:\Users\kingd\.claude\profile\PROFILE.md' }
    @{ order = 2; description = 'Verified health nonce echo and marker signature True'; type = 'read'; status = 'completed'; filePath = 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml' }
    @{ order = 3; description = 'Confirmed B2 OverallVerdict AGREE on disk'; type = 'read'; status = 'completed'; filePath = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260821T220115Z.md' }
    @{ order = 4; description = 'Re-ran coordinator isolation tests 7/0/0'; type = 'test'; status = 'completed'; filePath = 'tests/McpServer.Repl.Core.Tests/SessionLogPersistenceCoordinatorIsolationTests.cs' }
    @{ order = 5; description = 'Re-ran checksum test 1/0/0 and hashed official plugin libs mismatchCount=0'; type = 'test'; status = 'completed'; filePath = 'tests/Build.Tests/SyncAgentPluginsChecksumTests.cs' }
    @{ order = 6; description = 'Chose to treat cited Test 165 as real and independently re-run named gates rather than trust chat'; type = 'design_decision'; status = 'completed'; filePath = '' }
)

Invoke-Plugin -Method 'client.SessionLog.PatchTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    turn = @{
        requestId = $requestId
        queryTitle = 'Hostile B5 green gate Phase B'
        actions = $actions
        designDecisions = @(
            'Score B5 from independently re-run tests and live MCP store, not implementer narrative.'
            'Do not mark PLAN-PLUGINHANDOFF-001 or MCP-PLUGINCORE-004 done from this review; that is parent duty after AGREE.'
        )
        tags = @('hostile-validator','PLAN-PLUGINHANDOFF-001','MCP-PLUGINCORE-004','B5')
        contextList = @('docs/plans/PLAN-PLUGINHANDOFF-001.md','plugins/core/lib-ps/repl-invoke.ps1','src/McpServer.Repl.Core/ReplCommandDispatcher.cs')
        interpretation = 'Validate Phase B3+B4 green claims for B5 AGREE so Phase C may start. Do not implement.'
    }
} -Name 'sl-patch'

Invoke-Plugin -Method 'client.SessionLog.CompleteTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    payload = @{
        requestId = $requestId
        status = 'completed'
        response = 'Hostile B5 review recorded. Receipt docs/receipts/hostile-validator-20260821T224147Z.md. Verdict written after remaining Compile finish and sessionlog_query proof.'
        queryTitle = 'Hostile B5 green gate Phase B'
    }
} -Name 'sl-complete'

Invoke-Plugin -Method 'client.SessionLog.QueryAsync' -Params @{
    agent = $agent
    todoId = 'PLAN-PLUGINHANDOFF-001'
    text = 'GrokSubagentHostile-20260821T224147Z-b5-green'
    limit = 5
} -Name 'sl-query'

Write-Output 'SESSION_PERSIST_DONE'
