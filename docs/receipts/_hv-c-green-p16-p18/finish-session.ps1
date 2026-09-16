#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p16-p18'
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
$now = [DateTime]::UtcNow.ToString('o')
$receiptMdPath = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T081750Z.md'
$receiptJsonPath = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T081750Z.json'

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

$dialogItems = @(
    @{ timestamp = $now; role = 'model'; category = 'observation'; content = 'Independent AI filter ExitCode 0 Total tests 9 Passed 9 Failed 0 Skipped 0. NukeTarget_SkipIsFailure ExitCode 0 Total tests 1 Passed 1. Full PluginIntegration.Tests ExitCode 0 Total tests 95 Passed 95 Failed 0 Skipped 0 DurationMs 1224065. P19 names absent. Live todo_get PLAN and PLUGININT done false. EvaluateAsync implemented after C-red-P16 AGREE. Build.PluginSessionLogIntegration.cs has no aiUnit preflight. Receipt docs/receipts/hostile-validator-20260822T081750Z.md OverallVerdict DISAGREE FailCount 2 PassCount 18.' }
    @{ timestamp = $now; role = 'model'; category = 'decision'; content = 'Decision: DISAGREE C-green-P16-P18. Named AI and skip-fail tests independently Failed 0 Skipped 0 and full suite Passed 95, but P18 plan/TODO/TR ac-6 require aiUnit strategy preflight on Build.PluginSessionLogIntegration.cs, which is still P1-era skip-fail plus whole-project DotNetTest. Consequence: parent must not mark PLAN or PLUGININT done and must not start P19 as if this green gate AGREE. Alternatives rejected: AGREE because named tests passed; FAIL A3 because first splat attempt failed (second rerun is the evidence); require native-suite P19 in this gate.' }
)

Invoke-Plugin -Method 'client.SessionLog.AppendDialogAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    items = $dialogItems
} -Name 'sl-dialog'

$actions = @(
    @{ order = 1; description = 'Executed add-profile; read 18 profile markdown files'; type = 'read'; status = 'completed'; filePath = 'C:\Users\kingd\.claude\profile\PROFILE.md' }
    @{ order = 2; description = 'Health nonce nonce-hv-20260822075054-40006 echoed; plugin Test-MarkerSignature true; BeginTurnAsync turnId 42920'; type = 'read'; status = 'completed'; filePath = 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml' }
    @{ order = 3; description = 'Independent PluginSessionLogAiTheoryTests Failed 0 Passed 9 Skipped 0; all eight host kinds Passed plus P17 Fact'; type = 'test'; status = 'completed'; filePath = 'tests/McpServer.PluginIntegration.Tests/PluginSessionLogAiTheoryTests.cs' }
    @{ order = 4; description = 'Independent NukeTarget_SkipIsFailure Failed 0 Passed 1 Skipped 0; calls FailIfPluginSessionLogSkipped'; type = 'test'; status = 'completed'; filePath = 'tests/Build.Tests/PluginSessionLogIntegrationTargetTests.cs' }
    @{ order = 5; description = 'Independent full PluginIntegration.Tests Failed 0 Passed 95 Skipped 0 DurationMs 1224065; P19 count 0'; type = 'test'; status = 'completed'; filePath = 'tests/McpServer.PluginIntegration.Tests/AiStrategyFixture.cs' }
    @{ order = 6; description = 'Live todo_get PLAN-PLUGINHANDOFF-001 done false; MCP-PLUGININT-001 done false P16/P17/P18 done false; getFr/getTr/getTest/listMappings for PLUGININT-001'; type = 'read'; status = 'completed'; filePath = 'docs/receipts/_hv-c-green-p16-p18/todo-plan-client-final.txt' }
    @{ order = 7; description = 'Wrote hostile receipt pair 20260822T081750Z OverallVerdict DISAGREE FailCount 2 PassCount 18'; type = 'create'; status = 'completed'; filePath = 'docs/receipts/hostile-validator-20260822T081750Z.md' }
    @{ order = 8; description = 'DISAGREE C-green-P16-P18: named tests green, P19 absent, TODOs remain done false, but P18 aiUnit strategy preflight is missing from Build.PluginSessionLogIntegration.cs (TR ac-6 and plan P18 DoD). Do not implement P19. This DISAGREE is not plan closeout.'; type = 'design_decision'; status = 'completed'; filePath = 'build/Build.PluginSessionLogIntegration.cs' }
)

Invoke-Plugin -Method 'client.SessionLog.PatchTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    turn = @{
        requestId = $requestId
        queryTitle = 'Hostile C-green-P16-P18 AiTheory evaluator and skip-fail gate'
        actions = $actions
        designDecisions = @(
            'Score C-green-P16-P18 from independently re-run AiTheory filter, NukeTarget_SkipIsFailure filter, full PluginIntegration.Tests, live MCP store, C-red-P16 AGREE, EvaluateAsync source, and Build.PluginSessionLogIntegration.cs, not implementer narrative.'
            'DISAGREE: named tests Failed 0 Skipped 0 and full suite Passed 95, but TR-MCP-PLUGININT-001 ac-6 and plan P18 require aiUnit strategy preflight that is still absent. PLAN and PLUGININT remain done false. Do not mark them done. Do not implement P19 in this review.'
        )
        tags = @('hostile-validator','PLAN-PLUGINHANDOFF-001','MCP-PLUGININT-001','C-green-P16-P18','FR-MCP-PLUGININT-001','TR-MCP-PLUGININT-001','TEST-MCP-PLUGININT-001')
        contextList = @('docs/plans/PLAN-PLUGINHANDOFF-001.md','tests/McpServer.PluginIntegration.Tests/AiStrategyFixture.cs','tests/McpServer.PluginIntegration.Tests/PluginSessionLogAiTheoryTests.cs','tests/Build.Tests/PluginSessionLogIntegrationTargetTests.cs','build/Build.PluginSessionLogIntegration.cs','docs/receipts/hostile-validator-20260822T073737Z.md','docs/receipts/hostile-validator-20260822T081750Z.md')
        interpretation = 'Hostile review Phase C-green-P16-P18 ONLY after cited C-red-P16 AGREE. Review only. Do not implement P19-P20. Do not mark TODOs done.'
        filesModified = @('docs/receipts/hostile-validator-20260822T081750Z.md','docs/receipts/hostile-validator-20260822T081750Z.json')
        response = 'Hostile C-green-P16-P18 review DISAGREE. Receipt docs/receipts/hostile-validator-20260822T081750Z.md. AiTheory Passed 9 Failed 0 Skipped 0. NukeTarget_SkipIsFailure Passed 1 Failed 0 Skipped 0. Full PluginIntegration.Tests Passed 95 Failed 0 Skipped 0. P19 names absent. PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 remain done false. P18 aiUnit strategy preflight is missing. Do not implement P19. This DISAGREE is not plan closeout.'
    }
} -Name 'sl-patch'

Invoke-Plugin -Method 'client.SessionLog.CompleteTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    payload = @{
        requestId = $requestId
        status = 'completed'
        response = 'Hostile C-green-P16-P18 review DISAGREE. Receipt docs/receipts/hostile-validator-20260822T081750Z.md. AiTheory Passed 9 Failed 0 Skipped 0. NukeTarget_SkipIsFailure Passed 1 Failed 0 Skipped 0. Full PluginIntegration.Tests Passed 95 Failed 0 Skipped 0. P19 names absent. PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 remain done false. P18 aiUnit strategy preflight is missing. Do not implement P19. This DISAGREE is not plan closeout.'
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
