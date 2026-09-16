#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p19'
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
    @{ timestamp = $now; role = 'model'; category = 'observation'; content = 'Independent PluginNativeSuiteReceiptTests ExitCode 1 Total tests 3 Failed 3 Passed 0 Skipped 0. All three FileNotFoundException No docs/receipts/pluginint-p19-<utc> receipt directory exists. list-tests 3 named methods. P20 names absent. pluginint-p19 dirs 0 after tests. Live todo_get PLAN and PLUGININT done false. Receipt docs/receipts/hostile-validator-20260822T092424Z.md OverallVerdict AGREE FailCount 0 PassCount 19.' }
    @{ timestamp = $now; role = 'model'; category = 'decision'; content = 'Decision: AGREE C-red-P19. Named tests exist, currently red, pin AC5 native-suite half, P20 not mixed, no fake-green receipts, TODOs remain done false, prior C-green-P16-P18 AGREE exists. Consequence: parent may treat this red gate as AGREE and may proceed to P19 green execution after accepting it, but must not mark PLAN-PLUGINHANDOFF-001 or MCP-PLUGININT-001 done:true and must not treat this as C-green-P19. Alternatives rejected: DISAGREE because LoadLatest does not itself check Failed/Skipped (Facts do; empty folder still cannot green); DISAGREE because leftover full-suite testhost delayed the filter (independent TRX was still produced after MachineClear); AGREE and mark TODOs done (forbidden).' }
)

Invoke-Plugin -Method 'client.SessionLog.AppendDialogAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    items = $dialogItems
} -Name 'sl-dialog'

$actions = @(
    @{ order = 1; description = 'Executed add-profile; read 18 profile markdown files'; type = 'read'; status = 'completed'; filePath = 'C:\Users\kingd\.claude\profile\PROFILE.md' }
    @{ order = 2; description = 'Health nonce nonce-hv-20260822090258-672 echoed; plugin Test-MarkerSignature true; BeginTurnAsync turnId 42935'; type = 'read'; status = 'completed'; filePath = 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml' }
    @{ order = 3; description = 'Independent PluginNativeSuiteReceiptTests Failed 3 Passed 0 Skipped 0; all three FileNotFoundException pluginint-p19 receipt directory missing'; type = 'test'; status = 'completed'; filePath = 'tests/McpServer.PluginIntegration.Tests/PluginNativeSuiteReceiptTests.cs' }
    @{ order = 4; description = 'Re-read PluginNativeSuiteReceipt.cs LoadLatest stamp pattern, summary.json, nativeSuite allow-list, git.sha'; type = 'read'; status = 'completed'; filePath = 'tests/McpServer.PluginIntegration.Tests/PluginNativeSuiteReceipt.cs' }
    @{ order = 5; description = 'Live todo_get after tests PLAN-PLUGINHANDOFF-001 done false; MCP-PLUGININT-001 done false P19/P20 done false'; type = 'read'; status = 'completed'; filePath = 'docs/receipts/_hv-c-red-p19/todo-plan-client-final.txt' }
    @{ order = 6; description = 'Confirmed zero pluginint-p19 directories after tests; P20 named tests absent'; type = 'read'; status = 'completed'; filePath = 'docs/receipts/_hv-c-red-p19/pluginint-p19-dirs-after-tests.json' }
    @{ order = 7; description = 'Wrote hostile receipt pair 20260822T092424Z OverallVerdict AGREE FailCount 0 PassCount 19'; type = 'create'; status = 'completed'; filePath = 'docs/receipts/hostile-validator-20260822T092424Z.md' }
    @{ order = 8; description = 'AGREE C-red-P19: named tests red, P20 absent, TODOs remain done false, no fake-green pluginint-p19 receipts. Do not implement P19 green in this review. This AGREE is not plan closeout.'; type = 'design_decision'; status = 'completed'; filePath = 'docs/plans/PLAN-PLUGINHANDOFF-001.md' }
)

Invoke-Plugin -Method 'client.SessionLog.PatchTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    turn = @{
        requestId = $requestId
        queryTitle = 'Hostile C-red-P19 PluginNativeSuiteReceiptTests currently red'
        actions = $actions
        designDecisions = @(
            'Score C-red-P19 from independently re-run PluginNativeSuiteReceiptTests, live MCP store, C-green-P16-P18 AGREE, PluginNativeSuiteReceipt.cs pin analysis, and zero pluginint-p19 directories, not implementer narrative.'
            'AGREE: three named P19 tests exist, currently red Failed 3 Passed 0 Skipped 0, P20 absent, PLAN and PLUGININT remain done false. Do not mark them done. Do not treat this as C-green-P19.'
        )
        tags = @('hostile-validator','PLAN-PLUGINHANDOFF-001','MCP-PLUGININT-001','C-red-P19','FR-MCP-PLUGININT-001','TR-MCP-PLUGININT-001','TEST-MCP-PLUGININT-001')
        contextList = @('docs/plans/PLAN-PLUGINHANDOFF-001.md','tests/McpServer.PluginIntegration.Tests/PluginNativeSuiteReceiptTests.cs','tests/McpServer.PluginIntegration.Tests/PluginNativeSuiteReceipt.cs','docs/receipts/hostile-validator-20260822T084649Z.md','docs/receipts/hostile-validator-20260822T092424Z.md')
        interpretation = 'Hostile review Phase C-red-P19 only. Review only. Do not implement P19 green. Do not mark TODOs done.'
        filesModified = @('docs/receipts/hostile-validator-20260822T092424Z.md','docs/receipts/hostile-validator-20260822T092424Z.json')
        response = 'Hostile C-red-P19 review AGREE. Receipt docs/receipts/hostile-validator-20260822T092424Z.md. PluginNativeSuiteReceiptTests Failed 3 Passed 0 Skipped 0. P20 names absent. PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 remain done false. No pluginint-p19 fake-green receipts. Do not implement P19 green. This AGREE is not plan closeout.'
    }
} -Name 'sl-patch'

Invoke-Plugin -Method 'client.SessionLog.CompleteTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    payload = @{
        requestId = $requestId
        status = 'completed'
        response = 'Hostile C-red-P19 review AGREE. Receipt docs/receipts/hostile-validator-20260822T092424Z.md. PluginNativeSuiteReceiptTests Failed 3 Passed 0 Skipped 0. P20 names absent. PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 remain done false. No pluginint-p19 fake-green receipts. Do not implement P19 green. This AGREE is not plan closeout.'
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
