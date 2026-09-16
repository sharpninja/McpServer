#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p20'
$plugin = 'F:\GitHub\mcpserver-grok-plugin\lib\Invoke-McpPlugin.ps1'
$cacheRoot = Join-Path $out 'plugin-cache-own'
New-Item -ItemType Directory -Force -Path $cacheRoot | Out-Null
Set-Location -LiteralPath 'F:\GitHub\McpServer'
$env:MCP_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$env:MCPSERVER_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$env:GROK_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$env:PLUGIN_AGENT_NAME = 'GrokSubagentHostile'
$env:MCP_AGENT_NAME = 'GrokSubagentHostile'
$env:GROK_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:MCP_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:MCP_PLUGIN_HOST = 'grok'

$utc = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
$agent = 'GrokSubagentHostile'
$sessionId = "GrokSubagentHostile-$utc-c-red-p20-rebuild"
$requestId = "req-$utc-001-hostile-c-red-p20-rebuild"
Set-Content -LiteralPath (Join-Path $out 'own-session-id.txt') -Value $sessionId -Encoding utf8
Set-Content -LiteralPath (Join-Path $out 'own-request-id.txt') -Value $requestId -Encoding utf8
Write-Output ("OWN_SESSION=$sessionId")
Write-Output ("OWN_REQUEST=$requestId")

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

Invoke-Plugin -Method 'workflow.sessionlog.bootstrap' -Params @{} -Name 'own-sl-bootstrap'

Invoke-Plugin -Method 'client.SessionLog.OpenSessionAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    title = 'Hostile review PLAN-PLUGINHANDOFF-001 C-red-P20 rebuild gate'
    model = 'grok-build-subagent'
} -Name 'own-sl-open'

Invoke-Plugin -Method 'client.SessionLog.BeginTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    queryTitle = 'Hostile C-red-P20 rebuild named red tests'
    queryText = 'CLASS 1 hostile C-red-P20 rebuild review. Surfaces A+B+C+D. Review only. Independent rebuild of PluginUpdateServiceHarnessTests after sibling 131910Z turn completed citing 132413Z. Do not mark PLAN-PLUGINHANDOFF-001 or MCP-PLUGININT-001 done.'
    model = 'grok-build-subagent'
    planFile = 'docs/plans/PLAN-PLUGINHANDOFF-001.md'
    todoId = 'PLAN-PLUGINHANDOFF-001'
} -Name 'own-sl-begin'

$now = [DateTime]::UtcNow.ToString('o')
Invoke-Plugin -Method 'client.SessionLog.AppendDialogAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    items = @(
        @{ timestamp = $now; role = 'model'; category = 'observation'; content = 'add-profile 18 files. Independent rebuild PluginUpdateServiceHarnessTests Failed 2 Passed 0 Skipped 0 Duration 47 ms. Two FileNotFoundException reasons. 104054Z AGREE FailCount 0. Live todos done false. Receipt docs/receipts/hostile-validator-20260822T132620Z.md. Sibling session GrokSubagentHostile-20260822T131910Z-c-red-p20 already completed citing 132413Z; this session is the rebuild review.' }
        @{ timestamp = $now; role = 'model'; category = 'decision'; content = 'Decision: AGREE C-red-P20 on independently rebuilt named filter. Consequence: parent may start P20 green after this AGREE; must not mark PLAN or PLUGININT done. Alternatives rejected: treating sibling 132413Z --no-build as this rebuild receipt; requiring P20 green at this red gate.' }
    )
} -Name 'own-sl-dialog'

$actions = @(
    @{ order = 1; description = 'Executed add-profile; read 18 profile markdown files'; type = 'read'; status = 'completed'; filePath = 'C:\Users\kingd\.claude\profile\PROFILE.md' }
    @{ order = 2; description = 'Health nonce nonce-hv-ind-20260822132411-72537 echoed; plugin Test-MarkerSignature true'; type = 'read'; status = 'completed'; filePath = 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml' }
    @{ order = 3; description = 'Independent rebuild PluginUpdateServiceHarnessTests Failed 2 Passed 0 Skipped 0 Duration 47 ms'; type = 'test'; status = 'completed'; filePath = 'tests/McpServer.PluginIntegration.Tests/PluginUpdateServiceHarnessTests.cs' }
    @{ order = 4; description = 'Confirmed no pluginint-p20-* dirs and no Build.PluginPromotion.cs'; type = 'read'; status = 'completed'; filePath = 'docs/receipts/_hv-c-red-p20/independent-p20-green-absence.json' }
    @{ order = 5; description = 'Live todo_get PLAN/PLUGININT done false; C P20 combined task done false; hygiene keep-open'; type = 'read'; status = 'completed'; filePath = 'docs/receipts/_hv-c-red-p20/independent-todo-plan-client.txt' }
    @{ order = 6; description = 'Independently confirmed 104054Z OverallVerdict AGREE FailCount 0'; type = 'read'; status = 'completed'; filePath = 'docs/receipts/hostile-validator-20260822T104054Z.md' }
    @{ order = 7; description = 'Wrote hostile receipt pair 20260822T132620Z OverallVerdict AGREE FailCount 0 PassCount 19'; type = 'create'; status = 'completed'; filePath = 'docs/receipts/hostile-validator-20260822T132620Z.md' }
    @{ order = 8; description = 'AGREE C-red-P20 rebuild. Tests are new reds after 104054Z. Do not mark PLAN/PLUGININT done.'; type = 'design_decision'; status = 'completed'; filePath = 'docs/plans/PLAN-PLUGINHANDOFF-001.md' }
)

$completeResponse = 'Hostile C-red-P20 rebuild review AGREE. Receipt docs/receipts/hostile-validator-20260822T132620Z.md. PluginUpdateServiceHarnessTests Failed 2 Passed 0 Skipped 0 with FileNotFoundException pluginint-p20 directory and Build.PluginPromotion.cs missing. PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 remain done false. Combined C P20 task remains done false.'

Invoke-Plugin -Method 'client.SessionLog.PatchTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    turn = @{
        requestId = $requestId
        queryTitle = 'Hostile C-red-P20 rebuild named red tests'
        actions = $actions
        designDecisions = @(
            'Score C-red-P20 from independently rebuilt PluginUpdateServiceHarnessTests, live MCP store, C-green-P19 AGREE 104054Z, and on-disk absence of P20 green artifacts, not implementer TRX and not sibling --no-build collector alone.'
            'AGREE: reds exist as new files after 104054Z, currently Failed 2 Passed 0 Skipped 0, two FileNotFoundException messages. Do not mark PLAN or PLUGININT done. P20 execute/green may start after this AGREE.'
        )
        tags = @('hostile-validator','PLAN-PLUGINHANDOFF-001','MCP-PLUGININT-001','C-red-P20','FR-MCP-PLUGININT-001','TR-MCP-PLUGININT-001','TEST-MCP-PLUGININT-001')
        contextList = @('docs/plans/PLAN-PLUGINHANDOFF-001.md','tests/McpServer.PluginIntegration.Tests/PluginUpdateServiceHarnessTests.cs','docs/receipts/hostile-validator-20260822T104054Z.md','docs/receipts/hostile-validator-20260822T132620Z.md')
        interpretation = 'Hostile review Phase C-red-P20 rebuild. Review only. Do not mark TODOs done. Do not write P20 green artifacts.'
        filesModified = @('docs/receipts/hostile-validator-20260822T132620Z.md','docs/receipts/hostile-validator-20260822T132620Z.json')
        response = $completeResponse
    }
} -Name 'own-sl-patch'

Invoke-Plugin -Method 'client.SessionLog.CompleteTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    payload = @{
        requestId = $requestId
        status = 'completed'
        response = $completeResponse
    }
} -Name 'own-sl-complete'

Invoke-Plugin -Method 'client.SessionLog.QueryAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    limit = 5
} -Name 'own-sl-query-sid-after'

Write-Output 'OWN_SESSION_DONE'
