#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p19'
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
    @{ timestamp = $now; role = 'model'; category = 'observation'; content = 'Independent PluginNativeSuiteReceiptTests ExitCode 0 Passed 3 Failed 0 Skipped 0. C-red-P19 AGREE exists. P20 names absent. Claude-code omitted ReplFailsafe/HookTurnDedupe/CurrentTurnSessionRebind. Independent omitted Pester: ReplFailsafe 2/0/0, HookTurnDedupe 2/0/0, CurrentTurnSessionRebind 0/1/0. Live todo_get PLAN and PLUGININT done false. Receipt docs/receipts/hostile-validator-20260822T101615Z.md OverallVerdict DISAGREE FailCount 6 PassCount 16.' }
    @{ timestamp = $now; role = 'model'; category = 'decision'; content = 'Decision: DISAGREE C-green-P19. Named tests pass on a 7-file Claude-code Pester subset. Bats skip is as-applicable. Omitting two currently-passing native Pester files plus one failing rebind test is not a complete native suite and does not satisfy TEST-MCP-PLUGININT-001 AC5 or P19 green DoD. Consequence: parent must not mark PLAN-PLUGINHANDOFF-001 or MCP-PLUGININT-001 done:true and must not start P20 reds. Alternatives rejected: AGREE because named tests Passed 3; AGREE because Bats is inapplicable; FAIL Bats skip (PowerShell-only runtime deletes .sh).' }
)

Invoke-Plugin -Method 'client.SessionLog.AppendDialogAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    items = $dialogItems
} -Name 'sl-dialog'

$actions = @(
    @{ order = 1; description = 'Executed add-profile; read 18 profile markdown files'; type = 'read'; status = 'completed'; filePath = 'C:\Users\kingd\.claude\profile\PROFILE.md' }
    @{ order = 2; description = 'Health nonce nonce-hv-20260822101202-68040 echoed; plugin Test-MarkerSignature true; BeginTurnAsync turnId 42943'; type = 'read'; status = 'completed'; filePath = 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml' }
    @{ order = 3; description = 'Independent PluginNativeSuiteReceiptTests Passed 3 Failed 0 Skipped 0'; type = 'test'; status = 'completed'; filePath = 'tests/McpServer.PluginIntegration.Tests/PluginNativeSuiteReceiptTests.cs' }
    @{ order = 4; description = 'Inventoried official plugin native tests versus pluginint-p19-20260822T095842Z logs; Claude-code omitted 3 Pester files'; type = 'read'; status = 'completed'; filePath = 'docs/receipts/pluginint-p19-20260822T095842Z/summary.json' }
    @{ order = 5; description = 'Independently ran omitted Claude-code Pester: ReplFailsafe pass, HookTurnDedupe pass, CurrentTurnSessionRebind fail'; type = 'test'; status = 'completed'; filePath = 'F:\GitHub\mcpserver-claude-code-plugin\tests\CurrentTurnSessionRebind.Tests.ps1' }
    @{ order = 6; description = 'Live todo_get PLAN-PLUGINHANDOFF-001 done false; MCP-PLUGININT-001 done false P19/P20 done false; hygiene keep-open'; type = 'read'; status = 'completed'; filePath = 'docs/receipts/_hv-c-green-p19/todo-plan-client.txt' }
    @{ order = 7; description = 'Wrote hostile receipt pair 20260822T101615Z OverallVerdict DISAGREE FailCount 6 PassCount 16'; type = 'create'; status = 'completed'; filePath = 'docs/receipts/hostile-validator-20260822T101615Z.md' }
    @{ order = 8; description = 'DISAGREE C-green-P19: Claude-code native Pester incomplete. Do not mark TODOs done. Do not start P20.'; type = 'design_decision'; status = 'completed'; filePath = 'docs/plans/PLAN-PLUGINHANDOFF-001.md' }
)

Invoke-Plugin -Method 'client.SessionLog.PatchTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    turn = @{
        requestId = $requestId
        queryTitle = 'Hostile C-green-P19 native suite receipts'
        actions = $actions
        designDecisions = @(
            'Score C-green-P19 from independently re-run PluginNativeSuiteReceiptTests, omitted Claude-code Pester rerun, live MCP store, C-red-P19 AGREE, and plugin inventory versus receipt logs, not implementer narrative.'
            'DISAGREE: named tests pass on a Claude-code Pester subset. Two omitted files pass independently. TEST AC5 and P19 DoD require the applicable native suite complete. Do not mark PLAN or PLUGININT done. Do not start P20.'
        )
        tags = @('hostile-validator','PLAN-PLUGINHANDOFF-001','MCP-PLUGININT-001','C-green-P19','FR-MCP-PLUGININT-001','TR-MCP-PLUGININT-001','TEST-MCP-PLUGININT-001')
        contextList = @('docs/plans/PLAN-PLUGINHANDOFF-001.md','tests/McpServer.PluginIntegration.Tests/PluginNativeSuiteReceiptTests.cs','docs/receipts/pluginint-p19-20260822T095842Z/summary.json','docs/receipts/hostile-validator-20260822T092424Z.md','docs/receipts/hostile-validator-20260822T101615Z.md')
        interpretation = 'Hostile review Phase C-green-P19. Review only. Do not mark TODOs done. Do not write P20 tests.'
        filesModified = @('docs/receipts/hostile-validator-20260822T101615Z.md','docs/receipts/hostile-validator-20260822T101615Z.json')
        response = 'Hostile C-green-P19 review DISAGREE. Receipt docs/receipts/hostile-validator-20260822T101615Z.md. PluginNativeSuiteReceiptTests Passed 3 Failed 0 Skipped 0. Claude-code omitted 3 Pester files; two pass independently. PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 remain done false. Do not start P20.'
    }
} -Name 'sl-patch'

Invoke-Plugin -Method 'client.SessionLog.CompleteTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    payload = @{
        requestId = $requestId
        status = 'completed'
        response = 'Hostile C-green-P19 review DISAGREE. Receipt docs/receipts/hostile-validator-20260822T101615Z.md. PluginNativeSuiteReceiptTests Passed 3 Failed 0 Skipped 0. Claude-code omitted 3 Pester files; two pass independently. PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 remain done false. Do not start P20.'
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
