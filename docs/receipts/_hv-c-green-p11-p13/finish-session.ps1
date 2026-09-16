#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p11-p13'
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
$sessionId = 'GrokSubagentHostile-20260822T032633Z-c-green-p11-p13'
$requestId = 'req-20260822T032633Z-001-hostile-c-green-p11-p13'
$now = [DateTime]::UtcNow.ToString('o')
$receiptMdPath = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T035559Z.md'
$receiptJsonPath = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T035559Z.json'

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
        throw
    }
}

$receiptMd = Get-Item -LiteralPath $receiptMdPath
$receiptJson = Get-Item -LiteralPath $receiptJsonPath
$mdText = Get-Content -LiteralPath $receiptMd.FullName -Raw
$jsonObj = Get-Content -LiteralPath $receiptJson.FullName -Raw | ConvertFrom-Json
$proof = [ordered]@{
    MdExists = $true
    MdLength = $receiptMd.Length
    MdLastWriteTimeUtc = $receiptMd.LastWriteTimeUtc.ToString('o')
    MdHasAgree = [bool]($mdText -match '(?m)^OverallVerdict:\s*AGREE\s*$')
    MdHasDisagree = [bool]($mdText -match '(?m)^OverallVerdict:\s*DISAGREE\s*$')
    MdEmDash = $mdText.Contains([char]0x2014)
    MdEnDash = $mdText.Contains([char]0x2013)
    JsonExists = $true
    JsonLength = $receiptJson.Length
    JsonLastWriteTimeUtc = $receiptJson.LastWriteTimeUtc.ToString('o')
    JsonOverallVerdict = [string]$jsonObj.OverallVerdict
    JsonFailCount = $jsonObj.FailCount
    JsonPassCount = $jsonObj.PassCount
    JsonClaimCount = @($jsonObj.Claims).Count
}
$proof | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'receipt-on-disk.json') -Encoding utf8

$todoYamlStatus = git -C 'F:\GitHub\McpServer' status --porcelain -- docs/Project/TODO.yaml
[ordered]@{ TodoYamlPorcelain = [string]$todoYamlStatus } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'todo-yaml-git-status.json') -Encoding utf8

$dialogItems = @(
    @{ timestamp = $now; role = 'model'; category = 'observation'; content = 'Independent full PluginIntegration.Tests Passed 54 Failed 0 Skipped 0 Total 54. Recovered adapter filter Passed 24 Failed 0 Skipped 0 Total 24 after first filter abort ExitCode -1 no TRX. ExecuteCanonicalTurnAsync no longer throws. Persist is fixture.CreateTrustedClient after discarded LaunchAsync. P14 named theory absent. Live todo_get PLAN-PLUGINHANDOFF-001 done false; MCP-PLUGININT-001 done false P11/P12/P13/P14 done false.' }
    @{ timestamp = $now; role = 'model'; category = 'decision'; content = 'Decision: DISAGREE C-green-P11-P13. Numeric test gate is green, but P12 persist is a typed-client shortcut that fails TR-MCP-PLUGININT-001 AC3 and TEST-MCP-PLUGININT-001 AC2 as real plugin workflow. Consequence: do not mark PLAN or PLUGININT done; do not start P14 on this AGREE because there is no AGREE. Alternatives rejected: AGREE because Passed 54/24 (suite green is not AC3 coverage); FAIL the filter because the first process aborted (recovered rerun ExitCode 0 Passed 24).' }
    @{ timestamp = $now; role = 'model'; category = 'observation'; content = 'C-red-P11 AGREE on disk. Adapter files LastWriteTime after 2026-08-22T02:38:08Z. Plugin signature true. Health nonce a6f4a99ac05b4938ad16c7415d2dc4db echoed. Receipt docs/receipts/hostile-validator-20260822T035559Z.md OverallVerdict DISAGREE FailCount 4 PassCount 18.' }
)

Invoke-Plugin -Method 'client.SessionLog.AppendDialogAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    items = $dialogItems
} -Name 'sl-dialog'

$actions = @(
    @{ order = 1; description = 'Executed add-profile; read 18 profile markdown files'; type = 'read'; status = 'completed'; filePath = 'C:\Users\kingd\.claude\profile\PROFILE.md' }
    @{ order = 2; description = 'Health nonce a6f4a99ac05b4938ad16c7415d2dc4db echoed; plugin Test-MarkerSignature true; BeginTurnAsync turnId 42874'; type = 'read'; status = 'completed'; filePath = 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml' }
    @{ order = 3; description = 'Re-ran full PluginIntegration.Tests Passed 54 Failed 0 Skipped 0; adapter persist is CreateTrustedClient after discarded LaunchAsync'; type = 'test'; status = 'completed'; filePath = 'tests/McpServer.PluginIntegration.Tests/PluginSessionLogWorkflowAdapter.cs' }
    @{ order = 4; description = 'Recovered filter FullyQualifiedName~PluginSessionLogWorkflowAdapterTests Passed 24 Failed 0 Skipped 0; P12/P13 present; P14 absent'; type = 'test'; status = 'completed'; filePath = 'tests/McpServer.PluginIntegration.Tests/PluginSessionLogWorkflowAdapterTests.cs' }
    @{ order = 5; description = 'Live todo_get PLAN-PLUGINHANDOFF-001 done false; MCP-PLUGININT-001 done false P11 P12 P13 P14 done false'; type = 'read'; status = 'completed'; filePath = 'docs/receipts/_hv-c-green-p11-p13/todo-plan.txt' }
    @{ order = 6; description = 'Wrote hostile receipt pair 20260822T035559Z OverallVerdict DISAGREE FailCount 4 PassCount 18'; type = 'create'; status = 'completed'; filePath = 'docs/receipts/hostile-validator-20260822T035559Z.md' }
    @{ order = 7; description = 'DISAGREE because persist is typed-client shortcut; TR AC3 and TEST AC2 plugin workflow not proven; P12 plan DoD not met. Tests are 54/0/0 and 24/0/0. Do not mark PLAN/PLUGININT done. Do not implement P14.'; type = 'design_decision'; status = 'completed'; filePath = '' }
)

Invoke-Plugin -Method 'client.SessionLog.PatchTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    turn = @{
        requestId = $requestId
        queryTitle = 'Hostile C-green-P11-P13 plugin Session Log workflow adapter greens'
        actions = $actions
        designDecisions = @(
            'Score C-green-P11-P13 from independently re-run PluginIntegration.Tests, recovered adapter filter, live MCP store, C-red-P11 AGREE, and adapter persist path, not implementer narrative.'
            'DISAGREE: numeric gate Passed 54/24 Failed 0 Skipped 0, but ExecuteCanonicalTurnAsync persists via fixture.CreateTrustedClient after discarding PluginHostProcessAdapter.LaunchAsync. TR-MCP-PLUGININT-001 AC3 and TEST-MCP-PLUGININT-001 AC2 as real plugin workflow fail. P14 absent. PLAN and PLUGININT remain done false. Do not mark PLAN/PLUGININT done. Do not implement P14.'
        )
        tags = @('hostile-validator','PLAN-PLUGINHANDOFF-001','MCP-PLUGININT-001','C-green-P11-P13','FR-MCP-PLUGININT-001','TR-MCP-PLUGININT-001','TEST-MCP-PLUGININT-001')
        contextList = @('docs/plans/PLAN-PLUGINHANDOFF-001.md','tests/McpServer.PluginIntegration.Tests/PluginSessionLogWorkflowAdapterTests.cs','tests/McpServer.PluginIntegration.Tests/PluginSessionLogWorkflowAdapter.cs','docs/receipts/hostile-validator-20260822T023808Z.md','docs/receipts/hostile-validator-20260822T035559Z.md')
        interpretation = 'Hostile review Phase C-green-P11-P13 after C-red-P11 AGREE. Review only. Do not implement P14. Do not mark TODOs done.'
        filesModified = @('docs/receipts/hostile-validator-20260822T035559Z.md','docs/receipts/hostile-validator-20260822T035559Z.json')
    }
} -Name 'sl-patch'

Invoke-Plugin -Method 'client.SessionLog.CompleteTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    payload = @{
        requestId = $requestId
        status = 'completed'
        response = 'Hostile C-green-P11-P13 review DISAGREE. Receipt docs/receipts/hostile-validator-20260822T035559Z.md. Full PluginIntegration.Tests Passed 54 Failed 0 Skipped 0. Adapter filter Passed 24 Failed 0 Skipped 0. Persist is typed-client shortcut after discarded production launch. P14 absent. PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 remain done false. Do not implement P14. This DISAGREE is not plan closeout.'
        queryTitle = 'Hostile C-green-P11-P13 plugin Session Log workflow adapter greens'
    }
} -Name 'sl-complete'

Invoke-Plugin -Method 'client.SessionLog.QueryAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    limit = 20
} -Name 'sl-query-sid-after'

Invoke-Plugin -Method 'workflow.sessionlog.queryHistory' -Params @{
    agent = $agent
    limit = 10
} -Name 'sl-query-history-after'

Write-Output 'FINISH_SESSION_DONE'
