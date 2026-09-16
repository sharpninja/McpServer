#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p16-p18-rerun-20260822T084835Z'
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
$sessionId = 'GrokSubagentHostile-20260822T084835Z-c-green-p16-p18-rerun'
$requestId = 'req-20260822T084835Z-001-hostile-c-green-p16-p18-rerun'
$now = [DateTime]::UtcNow.ToString('o')
$receiptMdPath = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T092415Z.md'
$receiptJsonPath = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T092415Z.json'

function Invoke-Plugin {
    param([string]$Method, [hashtable]$Params, [string]$Name)
    $outFile = Join-Path $out ($Name + '.txt')
    $errFile = Join-Path $out ($Name + '.err.txt')
    try {
        $result = & $plugin -Command Invoke -Method $Method -ParamsObject $Params -WorkspacePath 'F:\GitHub\McpServer' -CacheRoot $cacheRoot -TimeoutSeconds 180 2> $errFile
        if ($null -eq $result) { $result = '' }
        if ($result -isnot [string]) { $result = ($result | Out-String) }
        Set-Content -LiteralPath $outFile -Value $result -Encoding utf8
        Write-Output ('OK ' + $Name)
    } catch {
        Set-Content -LiteralPath $outFile -Value ('ERROR ' + $_.Exception.ToString()) -Encoding utf8
        Write-Output ('FAIL ' + $Name + ' ' + $_.Exception.Message)
    }
}

$mdItem = Get-Item -LiteralPath $receiptMdPath
$jsonItem = Get-Item -LiteralPath $receiptJsonPath
[ordered]@{
    mdExists = $true
    mdLastWriteTimeUtc = $mdItem.LastWriteTimeUtc.ToString('o')
    mdLength = $mdItem.Length
    jsonExists = $true
    jsonLastWriteTimeUtc = $jsonItem.LastWriteTimeUtc.ToString('o')
    jsonLength = $jsonItem.Length
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'receipt-on-disk-finish.json') -Encoding utf8

Invoke-Plugin -Method 'client.SessionLog.AppendDialogAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    items = @(
        @{ timestamp = $now; role = 'model'; category = 'observation'; content = 'Independent named filters: AiTheory Passed 9 Failed 0 Skipped 0; NukeTarget_SkipIsFailure Passed 1 Failed 0 Skipped 0; PluginInt=AI Passed 9 Failed 0 Skipped 0. Full PluginIntegration.Tests ExitCode 1 Failed 3 Passed 95 Skipped 0 Total 98 DurationMs 1199229. Failures are the three P19 named tests. P19 files created 2026-08-22T08:52:54Z. Live todo_get PLAN and PLUGININT done false. Receipt docs/receipts/hostile-validator-20260822T092415Z.md OverallVerdict DISAGREE FailCount 4 PassCount 18.' }
        @{ timestamp = $now; role = 'model'; category = 'decision'; content = 'Decision: DISAGREE C-green-P16-P18 rerun. P18 preflight and PluginInt trait split are in source and named tests Failed 0 Skipped 0, but P19 named tests mixed into PluginIntegration.Tests (Trait PluginInt=Deterministic) and the full suite is Failed 3. Consequence: parent must not mark PLAN or PLUGININT done and must not treat sibling r2 AGREE as current. Alternatives rejected: AGREE because named AI/Nuke filters passed; treat P19 mix as residual instead of FAIL; require running the Nuke target as an extra FAIL.' }
    )
} -Name 'sl-dialog'

$actions = @(
    @{ order = 1; description = 'Executed add-profile; read 18 profile markdown files'; type = 'read'; status = 'completed'; filePath = 'C:\Users\kingd\.claude\profile\PROFILE.md' }
    @{ order = 2; description = 'Health nonce nonce-hv-20260822T084835Z-74968 echoed; plugin Test-MarkerSignature true; BeginTurnAsync turnId 42932'; type = 'read'; status = 'completed'; filePath = 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml' }
    @{ order = 3; description = 'Independent PluginSessionLogAiTheoryTests Failed 0 Passed 9 Skipped 0; PluginInt=AI Failed 0 Passed 9 Skipped 0'; type = 'test'; status = 'completed'; filePath = 'tests/McpServer.PluginIntegration.Tests/PluginSessionLogAiTheoryTests.cs' }
    @{ order = 4; description = 'Independent NukeTarget_SkipIsFailure Failed 0 Passed 1 Skipped 0; asserts preflight and PluginInt filters'; type = 'test'; status = 'completed'; filePath = 'tests/Build.Tests/PluginSessionLogIntegrationTargetTests.cs' }
    @{ order = 5; description = 'Independent full PluginIntegration.Tests Failed 3 Passed 95 Skipped 0 Total 98 DurationMs 1199229; P19 count 3'; type = 'test'; status = 'completed'; filePath = 'tests/McpServer.PluginIntegration.Tests/PluginNativeSuiteReceiptTests.cs' }
    @{ order = 6; description = 'Live todo_get after tests PLAN-PLUGINHANDOFF-001 done false; MCP-PLUGININT-001 done false P16/P17/P18/P19 done false'; type = 'read'; status = 'completed'; filePath = 'docs/receipts/_hv-c-green-p16-p18-rerun-20260822T084835Z/todo-plan-client-final.txt' }
    @{ order = 7; description = 'Wrote hostile receipt pair 20260822T092415Z OverallVerdict DISAGREE FailCount 4 PassCount 18'; type = 'create'; status = 'completed'; filePath = 'docs/receipts/hostile-validator-20260822T092415Z.md' }
    @{ order = 8; description = 'DISAGREE C-green-P16-P18 rerun: named AI/Nuke green and P18 preflight exists, but P19 named tests mixed in and full suite Failed 3. Do not implement P19. This DISAGREE is not plan closeout.'; type = 'design_decision'; status = 'completed'; filePath = 'tests/McpServer.PluginIntegration.Tests/PluginNativeSuiteReceiptTests.cs' }
)

Invoke-Plugin -Method 'client.SessionLog.PatchTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    turn = @{
        requestId = $requestId
        queryTitle = 'Hostile C-green-P16-P18 rerun validation'
        actions = $actions
        designDecisions = @(
            'Score this unique-collector rerun from independently re-run filters, full PluginIntegration.Tests, live MCP store, C-red-P16 AGREE, EvaluateAsync source, Build.PluginSessionLogIntegration.cs, and on-disk P19 files, not implementer narrative or sibling r2 AGREE.'
            'DISAGREE: P18 preflight/trait-split source is present and named AI/Nuke tests Failed 0 Skipped 0, but P19 named tests mixed into this C-green gate and full PluginIntegration.Tests Failed 3 Passed 95 Skipped 0. PLAN and PLUGININT remain done false. Do not mark them done. Do not implement P19 in this review.'
        )
        tags = @('hostile-validator','PLAN-PLUGINHANDOFF-001','MCP-PLUGININT-001','C-green-P16-P18','FR-MCP-PLUGININT-001','TR-MCP-PLUGININT-001','TEST-MCP-PLUGININT-001')
        contextList = @('docs/plans/PLAN-PLUGINHANDOFF-001.md','tests/McpServer.PluginIntegration.Tests/AiStrategyFixture.cs','tests/McpServer.PluginIntegration.Tests/PluginSessionLogAiTheoryTests.cs','tests/Build.Tests/PluginSessionLogIntegrationTargetTests.cs','build/Build.PluginSessionLogIntegration.cs','tests/McpServer.PluginIntegration.Tests/PluginNativeSuiteReceiptTests.cs','docs/receipts/hostile-validator-20260822T073737Z.md','docs/receipts/hostile-validator-20260822T081750Z.md','docs/receipts/hostile-validator-20260822T092415Z.md')
        interpretation = 'Hostile review Phase C-green-P16-P18 ONLY after cited C-red-P16 AGREE and prior C4/D2 DISAGREE. Unique collector rerun. Review only. Do not implement P19-P20. Do not mark TODOs done.'
        filesModified = @('docs/receipts/hostile-validator-20260822T092415Z.md','docs/receipts/hostile-validator-20260822T092415Z.json')
        response = 'Hostile C-green-P16-P18 rerun DISAGREE. Receipt docs/receipts/hostile-validator-20260822T092415Z.md. AiTheory Passed 9 Failed 0 Skipped 0. NukeTarget_SkipIsFailure Passed 1 Failed 0 Skipped 0. PluginInt=AI Passed 9 Failed 0 Skipped 0. Full PluginIntegration.Tests Failed 3 Passed 95 Skipped 0 Total 98. P19 names mixed in. PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 remain done false. Do not implement P19. This DISAGREE is not plan closeout.'
        status = 'in_progress'
    }
} -Name 'sl-patch'

Invoke-Plugin -Method 'client.SessionLog.CompleteTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    response = 'Hostile C-green-P16-P18 rerun DISAGREE. Receipt docs/receipts/hostile-validator-20260822T092415Z.md. OverallVerdict DISAGREE FailCount 4 PassCount 18. Full PluginIntegration.Tests Failed 3 Passed 95 Skipped 0 because P19 named tests mixed in. PLAN and PLUGININT remain done false. Do not mark them done. Do not implement P19.'
} -Name 'sl-complete'

Invoke-Plugin -Method 'client.SessionLog.QueryAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    limit = 5
} -Name 'sl-query-sid-after'

Write-Output 'FINISH_DONE'
