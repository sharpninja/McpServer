#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p20'
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
    @{ timestamp = $now; role = 'model'; category = 'observation'; content = 'Independent rebuild PluginUpdateServiceHarnessTests Failed 2 Passed 0 Skipped 0 Duration 47 ms. Harness FileNotFoundException no pluginint-p20-<utc> directory. Promotion FileNotFoundException Build.PluginPromotion.cs missing. 104054Z independently AGREE FailCount 0. Live todo_get PLAN/PLUGININT/HYGIENE done false. Combined C P20 task done false. Receipt docs/receipts/hostile-validator-20260822T132620Z.md OverallVerdict AGREE FailCount 0 PassCount 19.' }
    @{ timestamp = $now; role = 'model'; category = 'decision'; content = 'Decision: AGREE C-red-P20. Named tests exist as new reds after 104054Z, currently fail for the two claimed FileNotFoundException reasons, product greens absent, TODOs remain done false. Consequence: parent may treat C-red-P20 as gated and start P20 green only after this AGREE; must not mark PLAN or PLUGININT done:true; must not skip C-green-P20 hostile. Alternatives rejected: DISAGREE because store AC5 text omits UpdateService (plan/parent locked deploy half; red gate does not require isSatisfied); requiring P20 green at this red gate; opening a second hostile session instead of completing turnId 42975.' }
)

Invoke-Plugin -Method 'client.SessionLog.AppendDialogAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    items = $dialogItems
} -Name 'sl-dialog'

Invoke-Plugin -Method 'client.SessionLog.AppendActionsAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    actions = @(
        @{ order = 1; description = 'Executed add-profile; read 18 profile markdown files'; type = 'read'; status = 'completed'; filePath = 'C:\Users\kingd\.claude\profile\PROFILE.md' }
        @{ order = 2; description = 'Health nonce nonce-hv-ind-20260822132411-72537 echoed; plugin Test-MarkerSignature true; BeginTurnAsync turnId 42975'; type = 'read'; status = 'completed'; filePath = 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml' }
        @{ order = 3; description = 'Independent rebuild PluginUpdateServiceHarnessTests Failed 2 Passed 0 Skipped 0'; type = 'test'; status = 'completed'; filePath = 'tests/McpServer.PluginIntegration.Tests/PluginUpdateServiceHarnessTests.cs' }
        @{ order = 4; description = 'Confirmed no pluginint-p20-* dirs and no Build.PluginPromotion.cs'; type = 'read'; status = 'completed'; filePath = 'docs/receipts/_hv-c-red-p20/independent-p20-green-absence.json' }
        @{ order = 5; description = 'Live todo_get PLAN/PLUGININT done false; C P20 combined task done false; hygiene keep-open'; type = 'read'; status = 'completed'; filePath = 'docs/receipts/_hv-c-red-p20/independent-todo-plan-client.txt' }
        @{ order = 6; description = 'Independently confirmed 104054Z OverallVerdict AGREE FailCount 0'; type = 'read'; status = 'completed'; filePath = 'docs/receipts/hostile-validator-20260822T104054Z.md' }
        @{ order = 7; description = 'Wrote hostile receipt pair 20260822T132620Z OverallVerdict AGREE FailCount 0 PassCount 19'; type = 'create'; status = 'completed'; filePath = 'docs/receipts/hostile-validator-20260822T132620Z.md' }
        @{ order = 8; description = 'AGREE C-red-P20. Tests are new reds after 104054Z. Do not mark PLAN/PLUGININT done. Do not skip C-green-P20.'; type = 'design_decision'; status = 'completed'; filePath = '' }
    )
} -Name 'sl-actions'

Invoke-Plugin -Method 'client.SessionLog.CompleteTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    response = 'Hostile C-red-P20 review AGREE. Receipt docs/receipts/hostile-validator-20260822T132620Z.md. PluginUpdateServiceHarnessTests Failed 2 Passed 0 Skipped 0 with FileNotFoundException pluginint-p20 directory and Build.PluginPromotion.cs missing. PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 remain done false. Combined C P20 task remains done false. Do not start P20 green without this AGREE; do not mark PLAN or PLUGININT done.'
    interpretation = 'Hostile review Phase C-red-P20. Review only. Do not mark TODOs done. Do not write P20 green artifacts.'
    status = 'completed'
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
