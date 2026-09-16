#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p1'
$plugin = 'F:\GitHub\mcpserver-grok-plugin\lib\Invoke-McpPlugin.ps1'
Set-Location -LiteralPath 'F:\GitHub\McpServer'
$env:MCP_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$env:PLUGIN_AGENT_NAME = 'GrokSubagentHostile'
$env:MCP_AGENT_NAME = 'GrokSubagentHostile'
$env:GROK_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:MCP_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'

$agent = 'GrokSubagentHostile'
$sessionId = 'GrokSubagentHostile-20260821T231421Z-c-red-p1'
$requestId = 'req-20260821T231421Z-001-hostile-validate-c-red-p1'
$now = [DateTime]::UtcNow.ToString('o')

function Invoke-Plugin {
    param([string]$Method, [hashtable]$Params, [string]$Name)
    $outFile = Join-Path $out ($Name + '.txt')
    $errFile = Join-Path $out ($Name + '.err.txt')
    try {
        $result = & $plugin -Command Invoke -Method $Method -ParamsObject $Params -WorkspacePath 'F:\GitHub\McpServer' -TimeoutSeconds 120 2> $errFile
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

$dialogItems = @(
    @{ timestamp = $now; role = 'model'; category = 'observation'; content = 'add-profile executed. 18 non-skill profile files read. Work class 1. Surfaces A+B+C+D apply. Independent re-run Failed 3 Passed 0 Skipped 0.' }
    @{ timestamp = $now; role = 'model'; category = 'decision'; content = 'Decision: score C-red-P1 from independently re-run tests, live todo_get, and on-disk B5 receipt, not implementer chat. Consequence: AGREE only if A+B+C+D all PASS. Alternatives rejected: trusting chat; treating PLAN remaining as store; requiring P2-P20 or C green.' }
    @{ timestamp = $now; role = 'model'; category = 'observation'; content = 'B5 receipt OverallVerdict AGREE on disk. PLUGINCORE done true citing that receipt. PLAN and HYGIENE done false. PluginIntegration project absent. Catalog absent. Nuke target absent. Tests untracked new file after B5 timestamp.' }
)

Invoke-Plugin -Method 'client.SessionLog.AppendDialogAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    items = $dialogItems
} -Name 'sl-dialog'

$actions = @(
    @{ order = 1; description = 'Executed add-profile; read 18 profile markdown files'; type = 'read'; status = 'completed'; filePath = 'C:\Users\kingd\.claude\profile\PROFILE.md' }
    @{ order = 2; description = 'Health nonce 2650adc7c44445df8c85e7efb337f6bc echoed; plugin session turnId 42797'; type = 'read'; status = 'completed'; filePath = 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml' }
    @{ order = 3; description = 'Re-ran PluginSessionLogIntegrationTargetTests Failed 3 Passed 0 Skipped 0'; type = 'test'; status = 'completed'; filePath = 'tests/Build.Tests/PluginSessionLogIntegrationTargetTests.cs' }
    @{ order = 4; description = 'Live todo_get MCP-PLUGINCORE-004 done true; PLAN-PLUGINHANDOFF-001 and MCP-WORKSPACEHYGIENE-002 done false'; type = 'read'; status = 'completed'; filePath = 'docs/receipts/_hv-c-red-p1/todo-mcp-plugincore-004.txt' }
    @{ order = 5; description = 'Chose to treat C-red-P1 as harness-existence reds only and not demand P11 eight-agent theory'; type = 'design_decision'; status = 'completed'; filePath = '' }
)

Invoke-Plugin -Method 'client.SessionLog.PatchTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    turn = @{
        requestId = $requestId
        queryTitle = 'Hostile validate C-red-P1 plugin sessionlog tests'
        actions = $actions
        designDecisions = @(
            'Score C-red-P1 from independently re-run tests and live MCP store, not implementer narrative.'
            'Do not require Phase C green or PLAN done; residual catalog Cline v2 string gap is P4, not this red gate.'
        )
        tags = @('hostile-validator','PLAN-PLUGINHANDOFF-001','MCP-PLUGININT-001','C-red-P1','FR-MCP-PLUGININT-001','TR-MCP-PLUGININT-001','TEST-MCP-PLUGININT-001')
        contextList = @('docs/plans/PLAN-PLUGINHANDOFF-001.md','tests/Build.Tests/PluginSessionLogIntegrationTargetTests.cs','docs/receipts/hostile-validator-20260821T230457Z.md')
        interpretation = 'Validate Phase C-red-P1 claims. Review only. Do not implement.'
    }
} -Name 'sl-patch'

Invoke-Plugin -Method 'client.SessionLog.CompleteTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    payload = @{
        requestId = $requestId
        status = 'completed'
        response = 'Hostile C-red-P1 review recorded. Receipt docs/receipts/hostile-validator-20260821T232500Z.md pending write after query proof.'
        queryTitle = 'Hostile validate C-red-P1 plugin sessionlog tests'
    }
} -Name 'sl-complete'

Invoke-Plugin -Method 'client.SessionLog.QueryAsync' -Params @{
    agent = $agent
    limit = 10
} -Name 'sl-query-agent'

Invoke-Plugin -Method 'client.SessionLog.QueryAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    limit = 5
} -Name 'sl-query-sid'

# TRX counters without skipped property assumption
$trx = Join-Path $out 'hv-c-red-p1.trx'
if (Test-Path -LiteralPath $trx) {
    [xml]$x = Get-Content -LiteralPath $trx
    $c = $x.TestRun.ResultSummary.Counters
    $summary = [ordered]@{
        outcome = [string]$x.TestRun.ResultSummary.outcome
        total = [string]$c.total
        executed = [string]$c.executed
        passed = [string]$c.passed
        failed = [string]$c.failed
        notExecuted = [string]$c.notExecuted
    }
    $summary | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'trx-summary.json') -Encoding utf8
    Write-Output ('TRX ' + ($summary | ConvertTo-Json -Compress))
}

Write-Output 'FINISH_SESSION_DONE'
