#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p19-r2'
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
    @{ timestamp = $now; role = 'model'; category = 'observation'; content = 'Independent PluginNativeSuiteReceiptTests ExitCode 0 Passed 3 Failed 0 Skipped 0 Duration 133 ms. Independent Claude-code Invoke-Pester -Path tests ExitCode 0 discovery 10 files / 25 tests Passed 25 Failed 0 Skipped 0. LoadLatest is pluginint-p19-20260822T102645Z. 095842Z remains 7-file subset. Live todo_get PLAN and PLUGININT done false. P20 names absent. Receipt docs/receipts/hostile-validator-20260822T104054Z.md OverallVerdict AGREE FailCount 0 PassCount 24.' }
    @{ timestamp = $now; role = 'model'; category = 'decision'; content = 'Decision: AGREE C-green-P19 r2. Prior 101615Z DISAGREE was valid. Claude-code native Pester is now the full 10-file tree Failed 0 Skipped 0 before, after-sync, and independent rerun. Named tests independently Passed 3. P20 remains unstarted. PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 remain done false. Consequence: parent may treat C-green-P19 as gated; must not mark PLAN or PLUGININT done:true; must not skip C-red-P20. Alternatives rejected: DISAGREE because named tests still omit discovery file-count assertions (current suite independently proven complete); AGREE as plan closeout (P20 open).' }
)

Invoke-Plugin -Method 'client.SessionLog.AppendDialogAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    items = $dialogItems
} -Name 'sl-dialog'

$actions = @(
    @{ order = 1; description = 'Executed add-profile; read 18 profile markdown files'; type = 'read'; status = 'completed'; filePath = 'C:\Users\kingd\.claude\profile\PROFILE.md' }
    @{ order = 2; description = 'Health nonce nonce-hv-20260822103729-66902 echoed; plugin Test-MarkerSignature true; BeginTurnAsync turnId 42949'; type = 'read'; status = 'completed'; filePath = 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml' }
    @{ order = 3; description = 'Independent PluginNativeSuiteReceiptTests Passed 3 Failed 0 Skipped 0'; type = 'test'; status = 'completed'; filePath = 'tests/McpServer.PluginIntegration.Tests/PluginNativeSuiteReceiptTests.cs' }
    @{ order = 4; description = 'Inventoried official plugin native tests versus pluginint-p19-20260822T102645Z logs; Claude-code discovery 10 files'; type = 'read'; status = 'completed'; filePath = 'docs/receipts/pluginint-p19-20260822T102645Z/summary.json' }
    @{ order = 5; description = 'Independently ran Invoke-Pester -Path tests: 10 files / 25 tests Passed 25 Failed 0 Skipped 0'; type = 'test'; status = 'completed'; filePath = 'F:\GitHub\mcpserver-claude-code-plugin\tests' }
    @{ order = 6; description = 'Live todo_get PLAN-PLUGINHANDOFF-001 done false; MCP-PLUGININT-001 done false P19/P20 done false; hygiene keep-open'; type = 'read'; status = 'completed'; filePath = 'docs/receipts/_hv-c-green-p19-r2/todo-plan-client.txt' }
    @{ order = 7; description = 'Wrote hostile receipt pair 20260822T104054Z OverallVerdict AGREE FailCount 0 PassCount 24'; type = 'create'; status = 'completed'; filePath = 'docs/receipts/hostile-validator-20260822T104054Z.md' }
    @{ order = 8; description = 'AGREE C-green-P19 r2: Claude-code native Pester complete. Do not mark TODOs done. Do not skip C-red-P20.'; type = 'design_decision'; status = 'completed'; filePath = 'docs/plans/PLAN-PLUGINHANDOFF-001.md' }
)

Invoke-Plugin -Method 'client.SessionLog.PatchTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    turn = @{
        requestId = $requestId
        queryTitle = 'Hostile C-green-P19 r2 native suite receipts'
        actions = $actions
        designDecisions = @(
            'Score C-green-P19 r2 from independently re-run PluginNativeSuiteReceiptTests, independent Claude-code Invoke-Pester -Path tests, live MCP store, C-red-P19 AGREE, prior DISAGREE 101615Z, and plugin inventory versus 102645Z logs, not implementer narrative.'
            'AGREE: native suites as applicable are complete Failed 0 Skipped 0. 095842Z subset remains a documented FAIL. Do not mark PLAN or PLUGININT done. Do not start P20 without C-red-P20.'
        )
        tags = @('hostile-validator','PLAN-PLUGINHANDOFF-001','MCP-PLUGININT-001','C-green-P19-r2','FR-MCP-PLUGININT-001','TR-MCP-PLUGININT-001','TEST-MCP-PLUGININT-001')
        contextList = @('docs/plans/PLAN-PLUGINHANDOFF-001.md','tests/McpServer.PluginIntegration.Tests/PluginNativeSuiteReceiptTests.cs','docs/receipts/pluginint-p19-20260822T102645Z/summary.json','docs/receipts/hostile-validator-20260822T101615Z.md','docs/receipts/hostile-validator-20260822T104054Z.md')
        interpretation = 'Hostile review Phase C-green-P19 r2. Review only. Do not mark TODOs done. Do not write P20 tests.'
        filesModified = @('docs/receipts/hostile-validator-20260822T104054Z.md','docs/receipts/hostile-validator-20260822T104054Z.json')
        response = 'Hostile C-green-P19 r2 review AGREE. Receipt docs/receipts/hostile-validator-20260822T104054Z.md. PluginNativeSuiteReceiptTests Passed 3 Failed 0 Skipped 0. Independent Claude-code Pester 10 files / 25 tests Failed 0 Skipped 0. PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 remain done false. Do not start P20 without C-red-P20.'
    }
} -Name 'sl-patch'

Invoke-Plugin -Method 'client.SessionLog.CompleteTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    payload = @{
        requestId = $requestId
        status = 'completed'
        response = 'Hostile C-green-P19 r2 review AGREE. Receipt docs/receipts/hostile-validator-20260822T104054Z.md. PluginNativeSuiteReceiptTests Passed 3 Failed 0 Skipped 0. Independent Claude-code Pester 10 files / 25 tests Failed 0 Skipped 0. PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 remain done false. Do not start P20 without C-red-P20.'
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
