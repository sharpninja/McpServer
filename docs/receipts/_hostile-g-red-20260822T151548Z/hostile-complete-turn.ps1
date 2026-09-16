#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$pluginRoot = 'F:\GitHub\mcpserver-grok-plugin'
$workspace = 'F:\GitHub\McpServer'
$outDir = 'F:\GitHub\McpServer\docs\receipts\_hostile-g-red-20260822T151548Z'
$cacheRoot = 'F:\GitHub\McpServer\.mcpServer\grok-subagent-hostile-g-red'
$invoke = Join-Path $pluginRoot 'lib\Invoke-McpPlugin.ps1'
$now = [DateTime]::UtcNow.ToString('o')
$receiptRel = 'docs/receipts/hostile-validator-20260822T152545Z.md'

function Invoke-HostilePlugin {
    param(
        [Parameter(Mandatory)][string]$Method,
        [hashtable]$Params = @{},
        [int]$TimeoutSeconds = 120
    )
    $splat = @{
        Command = 'Invoke'
        Method = $Method
        WorkspacePath = $workspace
        PluginRoot = $pluginRoot
        CacheRoot = $cacheRoot
        TimeoutSeconds = $TimeoutSeconds
    }
    if ($Params.Count -gt 0) {
        $splat['ParamsObject'] = $Params
    }
    & $invoke @splat
}

$update = Invoke-HostilePlugin -Method 'workflow.sessionlog.updateTurn' -Params @{
    response = 'Hostile G-red independent rerun: Failed 12 Passed 0 Skipped 0. PLAN-PLUGINHANDOFF-001 and MCP-WIKIEXPORT-001 remain done:false. No dump/import product code. OverallVerdict AGREE.'
    interpretation = 'Class 1 G-RED only review of PLAN-PLUGINHANDOFF-001 Phase G named tests. Attack A1-A5 plus workspace rules, requirements, and plan DoD. No product implementation.'
    tags = @(
        'GrokSubagentHostile',
        'G-RED',
        'PLAN-PLUGINHANDOFF-001',
        'MCP-WIKIEXPORT-001',
        'FR-MCP-WIKIEXPORT-003',
        'FR-MCP-WIKIEXPORT-004',
        'FR-MCP-WIKIEXPORT-005'
    )
    contextList = @(
        'docs/plans/PLAN-PLUGINHANDOFF-001.md',
        'tests/McpServer.Support.Mcp.Tests/Services/WikiDumpPhaseGTests.cs',
        $receiptRel
    )
}

$dialog = Invoke-HostilePlugin -Method 'workflow.sessionlog.appendDialog' -Params @{
    dialogItems = @(
        @{
            timestamp = $now
            role = 'model'
            content = 'Decision: classify this request as class 1 project implementation (G-RED phase gate), not class 2 ops. Consequence: surfaces C and Byrd v4 apply. Rejected treating this as ops-only because named tests and FR-MCP-WIKIEXPORT-003/004/005 are product requirements. Affected: PLAN-PLUGINHANDOFF-001 Phase G, MCP-WIKIEXPORT-001.'
            category = 'decision'
        },
        @{
            timestamp = $now
            role = 'model'
            content = 'Decision: bind hydration attack to WorkspaceClient.CreateAsync / POST /mcpserver/workspace, not FederationClient.RegisterWorkspaceAsync. Consequence: A5 PASS if RegisterWorkspaceAsync still has no Dump and tests call WorkspaceService.CreateAsync. Rejected federation as hydration bind per plan decision 20 and G0 bound=no. Affected: TR-MCP-WIKIEXPORT-004.'
            category = 'decision'
        },
        @{
            timestamp = $now
            role = 'model'
            content = 'Observation: independent worktree rebuild+rerun TRX counters total=12 executed=12 passed=0 failed=12 notExecuted=0. Failures are unbound includeDump or unbound Dump. SrcHits for dump product tokens empty.'
            category = 'observation'
        }
    )
}

$actions = Invoke-HostilePlugin -Method 'workflow.sessionlog.appendActions' -Params @{
    actions = @(
        @{
            order = 1
            description = 'Class 1 G-RED: requirements 003-005 exist with AC; named tests must fail; no dump/import product code.'
            type = 'design_decision'
            status = 'completed'
            filePath = 'docs/plans/PLAN-PLUGINHANDOFF-001.md'
        },
        @{
            order = 2
            description = 'Independent rebuild+rerun of 12 named tests in worktree. TRX Failed 12 Passed 0 Skipped 0.'
            type = 'edit'
            status = 'completed'
            filePath = 'docs/receipts/_hostile-g-red-20260822T151548Z/hostile-g-red.trx'
        },
        @{
            order = 3
            description = 'Hostile receipt written on MAIN workspace after independent verification.'
            type = 'create'
            status = 'completed'
            filePath = $receiptRel
        }
    )
}

$complete = Invoke-HostilePlugin -Method 'workflow.sessionlog.completeTurn' -Params @{
    response = @'
Hostile validator GrokSubagentHostile finished G-RED review.
Independent worktree rebuild+rerun: Total 12 Failed 12 Passed 0 Skipped 0.
PLAN-PLUGINHANDOFF-001 done:false. MCP-WIKIEXPORT-001 done:false.
No dump/import product code under src/. FederationClient.RegisterWorkspaceAsync is not the hydration bind.
OverallVerdict: AGREE
'@
}

$history = Invoke-HostilePlugin -Method 'workflow.sessionlog.queryHistory' -Params @{
    agent = 'GrokCode'
    limit = 5
    offset = 0
}

$out = [ordered]@{
    PluginSessionId = 'GrokCode-20260822T152016Z-plugin-session'
    TurnRequestId = 'req-20260822T152013Z-001-g-red-hostile-validate'
    Update = [string]$update
    Dialog = [string]$dialog
    Actions = [string]$actions
    Complete = [string]$complete
    History = [string]$history
}
$out | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $outDir 'session-complete.json') -Encoding utf8
Write-Output ($out | ConvertTo-Json -Depth 6)
