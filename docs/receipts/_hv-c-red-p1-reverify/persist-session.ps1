#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p1-reverify'
Set-Location -LiteralPath 'F:\GitHub\McpServer'
$env:MCP_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$env:PLUGIN_AGENT_NAME = 'GrokSubagentHostile'
$env:MCP_AGENT_NAME = 'GrokSubagentHostile'
$env:GROK_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:MCP_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$plugin = 'F:\GitHub\mcpserver-grok-plugin\lib\Invoke-McpPlugin.ps1'
$agent = 'GrokSubagentHostile'
$sessionId = Get-Content -LiteralPath (Join-Path $out 'session-id.txt') -Raw
$sessionId = $sessionId.Trim()
$requestId = Get-Content -LiteralPath (Join-Path $out 'request-id.txt') -Raw
$requestId = $requestId.Trim()
$now = [DateTime]::UtcNow.ToString('o')

function Invoke-Plugin {
    param([string]$Method, [hashtable]$Params, [string]$Name, [int]$TimeoutSeconds = 180)
    $outFile = Join-Path $out ($Name + '.txt')
    $errFile = Join-Path $out ($Name + '.err.txt')
    try {
        $result = & $plugin -Command Invoke -Method $Method -ParamsObject $Params -WorkspacePath 'F:\GitHub\McpServer' -TimeoutSeconds $TimeoutSeconds 2> $errFile
        if ($null -eq $result) { $result = '' }
        if ($result -isnot [string]) { $result = ($result | Out-String) }
        Set-Content -LiteralPath $outFile -Value $result -Encoding utf8
        Write-Output ('OK ' + $Name)
    } catch {
        Set-Content -LiteralPath $outFile -Value ('ERROR ' + $_.Exception.ToString()) -Encoding utf8
        Write-Output ('FAIL ' + $Name + ' ' + $_.Exception.Message)
    }
}

Invoke-Plugin -Method 'workflow.todo.get' -Params @{ id = 'MCP-PLUGININT-001' } -Name 'todo-mcp-pluginint-001-after'
Invoke-Plugin -Method 'workflow.todo.get' -Params @{ id = 'PLAN-PLUGINHANDOFF-001' } -Name 'todo-plan-after'

$dialogItems = @(
    @{ timestamp = $now; role = 'model'; category = 'observation'; content = 'add-profile executed. 18 non-skill profile files read. Work class 1 C-red-P1. Marker signature True. Health nonce 64dd0f3df23a46858d4bec6b1c14db36 echoed HTTP 200.' }
    @{ timestamp = $now; role = 'model'; category = 'observation'; content = 'First independent run of PluginSessionLogIntegrationTargetTests: Failed 3 Passed 0 Skipped 0. Catalog missing, sln missing project, Nuke property null. existence.json at 232638Z had no PluginIntegration project and no Build.PluginSessionLogIntegration.cs.' }
    @{ timestamp = $now; role = 'model'; category = 'observation'; content = 'Later git status showed untracked build/Build.PluginSessionLogIntegration.cs and tests/McpServer.PluginIntegration.Tests/. File mtimes 23:29:05Z project, 23:30:57Z nuke target, 23:31:00Z sln. Catalog now 8 enabled rows including Cline v2. Rebuild then CS0103 DotNetTest. P2 test PluginIntegrationProject_HasXmlDocsAndNonparallelCollection exists.' }
    @{ timestamp = $now; role = 'model'; category = 'decision'; content = 'Decision: OverallVerdict DISAGREE. C-red-P1 AGREE requires currently failing P1 reds and no P2-P3 implementation. Consequence: parent must not start or continue P2-P3 until a clean red tree is re-established and this gate AGREE. Alternatives rejected: treating the first Failed-3 run as enough after green artifacts landed; treating PLUGINCORE close as C-red-P1 completion.' }
    @{ timestamp = $now; role = 'model'; category = 'decision'; content = 'Decision: MCP-PLUGINCORE-004 done=true is allowed by B5 AGREE docs/receipts/hostile-validator-20260821T230457Z.md with that path in doneSummary and cited Failed 0 Skipped 0 gates. Consequence: PLUGINCORE close is not a FAIL and is not C-red-P1 completion.' }
)

Invoke-Plugin -Method 'client.SessionLog.AppendDialogAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    items = $dialogItems
} -Name 'sl-dialog'

$actions = @(
    @{ order = 1; description = 'Executed add-profile; read 18 non-skill profile markdown files'; type = 'read'; status = 'completed'; filePath = 'C:\Users\kingd\.claude\profile\PROFILE.md' }
    @{ order = 2; description = 'Verified marker signature True and health nonce echo'; type = 'read'; status = 'completed'; filePath = 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml' }
    @{ order = 3; description = 'First independent run Failed 3 Skipped 0 Passed 0'; type = 'test'; status = 'completed'; filePath = 'tests/Build.Tests/PluginSessionLogIntegrationTargetTests.cs' }
    @{ order = 4; description = 'Caught untracked P2-P3 implementation after first scan'; type = 'read'; status = 'completed'; filePath = 'tests/McpServer.PluginIntegration.Tests/McpServer.PluginIntegration.Tests.csproj' }
    @{ order = 5; description = 'Rebuild after late files: CS0103 DotNetTest'; type = 'test'; status = 'failed'; filePath = 'build/Build.PluginSessionLogIntegration.cs' }
    @{ order = 6; description = 'PLUGINCORE close allowed by B5 AGREE; C-red-P1 DISAGREE because P2-P3 started before red AGREE'; type = 'design_decision'; status = 'completed'; filePath = 'docs/plans/PLAN-PLUGINHANDOFF-001.md' }
)

Invoke-Plugin -Method 'client.SessionLog.PatchTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    turn = @{
        requestId = $requestId
        queryTitle = 'Hostile C-red-P1 plugin sessionlog red tests'
        actions = $actions
        designDecisions = @(
            'DISAGREE C-red-P1 because P2-P3 product files landed during the red gate before AGREE.'
            'MCP-PLUGINCORE-004 done=true is allowed by B5 AGREE receipt path in doneSummary; it is not C-red-P1 completion.'
        )
        tags = @('hostile-validator','PLAN-PLUGINHANDOFF-001','MCP-PLUGININT-001','C-red-P1')
        contextList = @(
            'docs/plans/PLAN-PLUGINHANDOFF-001.md',
            'tests/Build.Tests/PluginSessionLogIntegrationTargetTests.cs',
            'docs/receipts/hostile-validator-20260821T230457Z.md'
        )
        interpretation = 'Inter-phase C-red-P1 gate. AGREE only if P1 reds currently fail and P2-P3 has not started.'
        filesModified = @(
            'docs/receipts/_hv-c-red-p1-reverify/collect.ps1',
            'docs/receipts/hostile-validator-20260821T233500Z.md'
        )
    }
} -Name 'sl-patch'

Invoke-Plugin -Method 'client.SessionLog.CompleteTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    payload = @{
        requestId = $requestId
        status = 'completed'
        response = 'Hostile C-red-P1 DISAGREE. P1 tests existed and first run Failed 3 Skipped 0. P2-P3 implementation started during the gate (csproj, catalog, Nuke target, sln). Receipt docs/receipts/hostile-validator-20260821T233500Z.md.'
        queryTitle = 'Hostile C-red-P1 plugin sessionlog red tests'
    }
} -Name 'sl-complete'

Invoke-Plugin -Method 'client.SessionLog.QueryAsync' -Params @{
    agent = $agent
    text = $sessionId
    limit = 5
} -Name 'sl-query'

Write-Output 'PERSIST_DONE'
