#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p20'
$plugin = 'F:\GitHub\mcpserver-grok-plugin\lib\Invoke-McpPlugin.ps1'
$cacheRoot = Join-Path $out 'plugin-cache'
Set-Location -LiteralPath 'F:\GitHub\McpServer'
$env:MCP_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$env:MCPSERVER_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$env:GROK_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$env:PLUGIN_AGENT_NAME = 'GrokSubagentHostile'
$env:MCP_AGENT_NAME = 'GrokSubagentHostile'
$env:GROK_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:MCP_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:MCP_PLUGIN_HOST = 'grok'

$agent = 'GrokSubagentHostile'
$sessionId = (Get-Content -LiteralPath (Join-Path $out 'session-id.txt') -Raw).Trim()
$requestId = (Get-Content -LiteralPath (Join-Path $out 'request-id.txt') -Raw).Trim()

function Invoke-Plugin {
    param([string]$Method, [hashtable]$Params, [string]$Name)
    $outFile = Join-Path $out ($Name + '.txt')
    $errFile = Join-Path $out ($Name + '.err.txt')
    try {
        $result = & $plugin -Command Invoke -Method $Method -ParamsObject $Params -WorkspacePath 'F:\GitHub\McpServer' -CacheRoot $cacheRoot -TimeoutSeconds 120 2> $errFile
        if ($null -eq $result) { $result = '' }
        if ($result -isnot [string]) { $result = ($result | Out-String) }
        Set-Content -LiteralPath $outFile -Value $result -Encoding utf8
        Write-Output ('OK ' + $Name)
    } catch {
        Set-Content -LiteralPath $outFile -Value ('ERROR ' + $_.Exception.ToString()) -Encoding utf8
        Write-Output ('FAIL ' + $Name + ' ' + $_.Exception.Message)
    }
}

$now = [DateTime]::UtcNow.ToString('o')
$completeResponse = 'Hostile C-green-P20 review AGREE. Receipt docs/receipts/hostile-validator-20260822T134148Z.md. PluginUpdateServiceHarnessTests Failed 0 Passed 2 Skipped 0. Live pid 6220. Harness pluginint-p20-20260822T133039Z Development UpdateService sanitizedFixtures. P20Harness-20260822T133039Z-sanitized persisted. PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 remain done false. Phase D not started.'

Invoke-Plugin -Method 'client.SessionLog.AppendDialogAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    items = @(
        @{ timestamp = $now; role = 'model'; category = 'observation'; content = 'Independent named filter Failed 0 Passed 2 Skipped 0 Duration 135 ms --no-build. _build.csproj 0 Error(s). C-red-P20 AGREE 132413Z FailCount 0. Live pid 6220 only listener. pluginint-p20-20260822T133039Z Development UpdateService sanitizedFixtures true Failed 0 Skipped 0 no raw apiKey. Live P20Harness-20260822T133039Z-sanitized turn completed. PLAN/PLUGININT/P20/hygiene done false. Planted approval absent.' }
        @{ timestamp = $now; role = 'model'; category = 'decision'; content = 'Decision: AGREE C-green-P20. Consequence: parent may record this green gate; must not mark PLAN-PLUGINHANDOFF-001 or MCP-PLUGININT-001 done:true without a later closeout citing this AGREE in doneSummary. Alternatives rejected: treating implementer TRX as proof; requiring PLAN/PLUGININT done; FAIL for leftover Handoff dirty files timestamped before the red gate; FAIL B2 from FR createdAt query stamps.' }
    )
} -Name 'sl-dialog'

$actions = @(
    @{ order = 1; description = 'Executed add-profile; read 18 profile markdown files'; type = 'read'; status = 'completed'; filePath = 'C:\Users\kingd\.claude\profile\PROFILE.md' }
    @{ order = 2; description = 'Health nonce nonce-hv-retry-20260822133855-10725 echoed; plugin Test-MarkerSignature true; service pid 6220'; type = 'read'; status = 'completed'; filePath = 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml' }
    @{ order = 3; description = 'Independent PluginUpdateServiceHarnessTests Failed 0 Passed 2 Skipped 0 Duration 135 ms'; type = 'test'; status = 'completed'; filePath = 'tests/McpServer.PluginIntegration.Tests/PluginUpdateServiceHarnessTests.cs' }
    @{ order = 4; description = 'Confirmed pluginint-p20-20260822T133039Z Development UpdateService sanitizedFixtures and no raw apiKey'; type = 'read'; status = 'completed'; filePath = 'docs/receipts/pluginint-p20-20260822T133039Z/summary.json' }
    @{ order = 5; description = 'Confirmed RequiresOperatorApproval AllowPluginPromotion AssertPluginPromotionAllowed and policy requireOperatorApproval true'; type = 'read'; status = 'completed'; filePath = 'build/Build.PluginPromotion.cs' }
    @{ order = 6; description = 'dotnet build build/_build.csproj 0 Error(s)'; type = 'test'; status = 'completed'; filePath = 'build/_build.csproj' }
    @{ order = 7; description = 'Live todo_get PLAN/PLUGININT done false; C P20 combined task done false; hygiene keep-open'; type = 'read'; status = 'completed'; filePath = 'docs/receipts/_hv-c-green-p20/todo-plan-client.txt' }
    @{ order = 8; description = 'Independently confirmed 132413Z OverallVerdict AGREE FailCount 0 and red Failed 2'; type = 'read'; status = 'completed'; filePath = 'docs/receipts/hostile-validator-20260822T132413Z.md' }
    @{ order = 9; description = 'Live P20Harness-20260822T133039Z-sanitized turn completed'; type = 'read'; status = 'completed'; filePath = 'docs/receipts/_hv-c-green-p20/sl-p20harness-query.txt' }
    @{ order = 10; description = 'Wrote hostile receipt pair 20260822T134148Z OverallVerdict AGREE FailCount 0 PassCount 17'; type = 'create'; status = 'completed'; filePath = 'docs/receipts/hostile-validator-20260822T134148Z.md' }
    @{ order = 11; description = 'AGREE C-green-P20. Do not mark PLAN/PLUGININT done. Do not start Phase D.'; type = 'design_decision'; status = 'completed'; filePath = 'docs/plans/PLAN-PLUGINHANDOFF-001.md' }
)

Invoke-Plugin -Method 'client.SessionLog.PatchTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    turn = @{
        requestId = $requestId
        queryTitle = 'Hostile C-green-P20 UpdateService harness'
        actions = $actions
        designDecisions = @(
            'Score C-green-P20 from independently re-run PluginUpdateServiceHarnessTests, live MCP store, C-red-P20 AGREE 132413Z, live P20Harness session, and on-disk promotion/harness artifacts, not implementer TRX alone.'
            'AGREE: named filter Failed 0 Passed 2 Skipped 0 after red Failed 2. Do not mark PLAN or PLUGININT done. Phase D remains unstarted.'
        )
        tags = @('hostile-validator','PLAN-PLUGINHANDOFF-001','MCP-PLUGININT-001','C-green-P20','FR-MCP-PLUGININT-001','TR-MCP-PLUGININT-001','TEST-MCP-PLUGININT-001')
        contextList = @('docs/plans/PLAN-PLUGINHANDOFF-001.md','tests/McpServer.PluginIntegration.Tests/PluginUpdateServiceHarnessTests.cs','docs/receipts/hostile-validator-20260822T132413Z.md','docs/receipts/pluginint-p20-20260822T133039Z/summary.json','docs/receipts/hostile-validator-20260822T134148Z.md')
        interpretation = 'Hostile review Phase C-green-P20. Review only. Do not mark TODOs done. Do not start Phase D.'
        filesModified = @('docs/receipts/hostile-validator-20260822T134148Z.md','docs/receipts/hostile-validator-20260822T134148Z.json')
        response = $completeResponse
    }
} -Name 'sl-patch'

Invoke-Plugin -Method 'client.SessionLog.CompleteTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    payload = @{
        requestId = $requestId
        status = 'completed'
        response = $completeResponse
    }
} -Name 'sl-complete'

Invoke-Plugin -Method 'client.SessionLog.QueryAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    limit = 5
} -Name 'sl-query-sid-after'

Invoke-Plugin -Method 'workflow.sessionlog.queryHistory' -Params @{
    sessionId = $sessionId
    limit = 5
} -Name 'sl-query-history-after'

Write-Output 'FINISH_SESSION_DONE'
