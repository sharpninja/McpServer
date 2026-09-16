#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p14'
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
$sessionId = 'GrokSubagentHostile-20260822T042653Z-c-green-p14'
$requestId = 'req-20260822T042653Z-001-hostile-c-green-p14'
$now = [DateTime]::UtcNow.ToString('o')
$receiptMdPath = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T050748Z.md'
$receiptJsonPath = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T050748Z.json'

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

$todoYamlStatus = git -C 'F:\GitHub\McpServer' status --porcelain -- docs/Project/TODO.yaml docs/todo.yaml
[ordered]@{ TodoYamlPorcelain = [string]$todoYamlStatus } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'todo-yaml-git-status-after.json') -Encoding utf8

$dialogItems = @(
    @{ timestamp = $now; role = 'model'; category = 'observation'; content = 'Independent WMI PID 20172 P14 filter ExitCode 0 console Passed 8 Failed 0 Skipped 0 Total 8 Duration 2 m 10 s. Full PluginIntegration.Tests ExitCode 0 Passed 62 Failed 0 Skipped 0 Total 62 Duration 10 m 7 s. TRX matches. --list-tests FQN 62 P14 8 P15 0 P16 0. Live todo_get PLAN-PLUGINHANDOFF-001 done false; MCP-PLUGININT-001 done false P14 done false. Adapter sets PluginRootOverrideRejected, extraEnvironment PLUGIN_ROOT_OVERRIDE empty, runner Remove empty env. Prior C-red AGREE 041327Z Failed 8.' }
    @{ timestamp = $now; role = 'model'; category = 'decision'; content = 'Decision: AGREE C-green-P14. Named Theory eight rows pass with no Skip, maps TEST-MCP-PLUGININT-001 AC4, guard implemented, full suite Failed 0 Skipped 0. Consequence: parent may leave C-green-P14; do not mark PLAN or PLUGININT done; do not start from this review as P15 mixed in. Alternatives rejected: FAIL persist CreateTrustedClient (P14 DoD is PoisonEmpty plus TEST AC4; persist is later/P12 residual); FAIL missing sibling assert (C-red residual vs named theory).' }
)

Invoke-Plugin -Method 'client.SessionLog.AppendDialogAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    items = $dialogItems
} -Name 'sl-dialog'

$actions = @(
    @{ order = 1; description = 'Executed add-profile; read 18 profile markdown files'; type = 'read'; status = 'completed'; filePath = 'C:\Users\kingd\.claude\profile\PROFILE.md' }
    @{ order = 2; description = 'Health nonce nonce-hv-20260822044014-79279 echoed; plugin Test-MarkerSignature true; BeginTurnAsync turnId 42886'; type = 'read'; status = 'completed'; filePath = 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml' }
    @{ order = 3; description = 'Independently re-ran P14 filter via WMI PID 20172; ExitCode 0 Passed 8 Failed 0 Skipped 0 Duration 2 m 10 s'; type = 'test'; status = 'completed'; filePath = 'tests/McpServer.PluginIntegration.Tests/PluginSessionLogWorkflowAdapterTests.cs' }
    @{ order = 4; description = 'Independently re-ran full PluginIntegration.Tests; ExitCode 0 Passed 62 Failed 0 Skipped 0 Duration 10 m 7 s; list-tests FQN 62 P14 8 P15 0 P16 0'; type = 'test'; status = 'completed'; filePath = 'tests/McpServer.PluginIntegration.Tests/PluginSessionLogWorkflowAdapter.cs' }
    @{ order = 5; description = 'Live todo_get PLAN-PLUGINHANDOFF-001 done false; MCP-PLUGININT-001 done false P14 done false; getFr/getTr/getTest/listMappings for PLUGININT-001'; type = 'read'; status = 'completed'; filePath = 'docs/receipts/_hv-c-green-p14/hv-live-todo-plan.txt' }
    @{ order = 6; description = 'Wrote hostile receipt pair 20260822T050748Z OverallVerdict AGREE FailCount 0 PassCount 20'; type = 'create'; status = 'completed'; filePath = 'docs/receipts/hostile-validator-20260822T050748Z.md' }
    @{ order = 7; description = 'AGREE C-green-P14: named theory eight rows pass, guard implemented, P15/P16 absent, PLAN/PLUGININT remain done false. This AGREE is not plan closeout.'; type = 'design_decision'; status = 'completed'; filePath = '' }
)

Invoke-Plugin -Method 'client.SessionLog.PatchTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    turn = @{
        requestId = $requestId
        queryTitle = 'Hostile C-green-P14 PLUGIN_ROOT_OVERRIDE isolation'
        actions = $actions
        designDecisions = @(
            'Score C-green-P14 from independently re-run P14 filter plus full PluginIntegration.Tests, live MCP store, C-red-P14 AGREE, and adapter guard source, not implementer narrative.'
            'AGREE: named Theory eight InlineData PluginHostKind rows pass with no Skip, maps TEST-MCP-PLUGININT-001 AC4, PluginRootOverrideRejected implemented, extraEnvironment cleared, runner removes empty env, full suite Failed 0 Skipped 0. P15/P16 absent. PLAN and PLUGININT remain done false. Do not mark PLAN/PLUGININT done. This AGREE is not plan closeout.'
        )
        tags = @('hostile-validator','PLAN-PLUGINHANDOFF-001','MCP-PLUGININT-001','C-green-P14','FR-MCP-PLUGININT-001','TR-MCP-PLUGININT-001','TEST-MCP-PLUGININT-001')
        contextList = @('docs/plans/PLAN-PLUGINHANDOFF-001.md','tests/McpServer.PluginIntegration.Tests/PluginSessionLogWorkflowAdapterTests.cs','tests/McpServer.PluginIntegration.Tests/PluginSessionLogWorkflowAdapter.cs','docs/receipts/hostile-validator-20260822T041327Z.md','docs/receipts/hostile-validator-20260822T050748Z.md')
        interpretation = 'Hostile review Phase C-green-P14 ONLY after cited C-red-P14 AGREE. Review only. Do not implement P15. Do not mark TODOs done.'
        filesModified = @('docs/receipts/hostile-validator-20260822T050748Z.md','docs/receipts/hostile-validator-20260822T050748Z.json')
    }
} -Name 'sl-patch'

Invoke-Plugin -Method 'client.SessionLog.CompleteTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    payload = @{
        requestId = $requestId
        status = 'completed'
        response = 'Hostile C-green-P14 review AGREE. Receipt docs/receipts/hostile-validator-20260822T050748Z.md. P14 filter Passed 8 Failed 0 Skipped 0. Full PluginIntegration.Tests Passed 62 Failed 0 Skipped 0. P15/P16 absent. PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 remain done false. This AGREE is not plan closeout.'
        queryTitle = 'Hostile C-green-P14 PLUGIN_ROOT_OVERRIDE isolation'
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
