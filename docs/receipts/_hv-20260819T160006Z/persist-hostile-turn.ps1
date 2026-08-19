#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$pluginRoot = 'F:\GitHub\mcpserver-grok-plugin'
$workspace = 'F:\GitHub\McpServer'
$ev = Join-Path $workspace 'docs\receipts\_hv-20260819T160006Z'
$invoke = Join-Path $pluginRoot 'lib\Invoke-McpPlugin.ps1'
$hostileCache = Join-Path $ev 'hostile-cache'
New-Item -ItemType Directory -Path $hostileCache -Force | Out-Null

$env:MCP_PLUGIN_ROOT = $pluginRoot
$env:GROK_PLUGIN_ROOT = $pluginRoot
$env:PLUGIN_AGENT_NAME = 'GrokCode'
$env:MCP_AGENT_NAME = 'GrokCode'
$env:MCP_WORKSPACE_PATH = $workspace
$env:MCP_SUBAGENT_ID = 'hostile-validator'
Set-Location -LiteralPath $workspace

$utc = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
$sessionId = "GrokCode-$utc-hostile-triageplugin"
$requestId = "req-$utc-001-hostile-validate-isolation"
$receiptRel = "docs/receipts/hostile-validator-$utc.md"
$ids = [ordered]@{
    utc = $utc
    sessionId = $sessionId
    requestId = $requestId
    receiptRel = $receiptRel
}
$ids | ConvertTo-Json | Set-Content (Join-Path $ev 'hostile-ids.json') -Encoding utf8
Write-Output ($ids | ConvertTo-Json)

function Get-RootTurn {
    $p = Join-Path $workspace '.mcpServer\grok\current-turn.yaml'
    $raw = Get-Content -LiteralPath $p -Raw
    return [ordered]@{
        requestId = ([regex]::Match($raw, 'turnRequestId:\s*(.+)').Groups[1].Value.Trim())
        status = ([regex]::Match($raw, 'status:\s*(.+)').Groups[1].Value.Trim())
        lastWriteUtc = (Get-Item -LiteralPath $p).LastWriteTimeUtc.ToString('o')
        raw = $raw
    }
}

function Invoke-Iso {
    param(
        [Parameter(Mandatory)][string]$Method,
        [hashtable]$Params = @{},
        [int]$TimeoutSeconds = 120
    )
    $args = @{
        Command = 'Invoke'
        Method = $Method
        WorkspacePath = $workspace
        PluginRoot = $pluginRoot
        CacheRoot = $hostileCache
        TimeoutSeconds = $TimeoutSeconds
    }
    if ($Params.Count -gt 0) { $args['ParamsObject'] = $Params }
    $out = & $invoke @args
    $text = [string]$out
    $safe = ($Method -replace '[^a-zA-Z0-9]+', '-')
    Set-Content -LiteralPath (Join-Path $ev "iso-$safe.txt") -Value $text -Encoding utf8
    Write-Output "METHOD $Method len=$($text.Length)"
    Write-Output $text.Substring(0, [Math]::Min(1500, $text.Length))
    Write-Output '-----'
    return $text
}

$root0 = Get-RootTurn
$root0 | ConvertTo-Json | Set-Content (Join-Path $ev 'root-turn-before-hostile-session.json') -Encoding utf8
Write-Output "ROOT_BEFORE=$($root0.requestId) $($root0.status) $($root0.lastWriteUtc)"

$null = Invoke-Iso -Method 'workflow.sessionlog.bootstrap' -Params @{}
$root1 = Get-RootTurn
Write-Output "ROOT_AFTER_BOOTSTRAP=$($root1.requestId) $($root1.status) $($root1.lastWriteUtc)"

$null = Invoke-Iso -Method 'workflow.sessionlog.openSession' -Params @{
    agent = 'GrokCode'
    sessionId = $sessionId
    title = 'Hostile validate TRIAGEPLUGIN isolation remediATION'
    model = 'grok'
}
$root2 = Get-RootTurn
Write-Output "ROOT_AFTER_OPEN=$($root2.requestId) $($root2.status) $($root2.lastWriteUtc)"

$null = Invoke-Iso -Method 'workflow.sessionlog.beginTurn' -Params @{
    requestId = $requestId
    queryTitle = 'Hostile validate TRIAGEPLUGIN isolation remediATION'
    queryText = 'Hostile validation of FR-MCP-TRIAGEPLUGIN-001 root UserPromptSubmit isolation remediATION claims.'
    planFile = 'None'
    todoId = 'None'
}
$root3 = Get-RootTurn
Write-Output "ROOT_AFTER_BEGIN=$($root3.requestId) $($root3.status) $($root3.lastWriteUtc)"

$null = Invoke-Iso -Method 'workflow.sessionlog.updateTurn' -Params @{
    response = 'Independent hostile re-verify in progress.'
    interpretation = 'Attack implementer remediATION claims for FR-MCP-TRIAGEPLUGIN-001 isolation; class 1 project implementation of existing AC.'
    tags = @('hostile-validator', 'FR-MCP-TRIAGEPLUGIN-001', 'TEST-MCP-TRIAGEPLUGIN-001')
}

$ts = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
$null = Invoke-Iso -Method 'workflow.sessionlog.appendDialog' -Params @{
    dialogItems = @(
        @{
            timestamp = $ts
            role = 'model'
            content = 'add-profile executed: 18 non-skill profile markdown files. Independently re-ran Pester, hashed both plugin-hook.ps1 files, queried sessionlog without exact-requestId filter, read current-turn.yaml, verified git HEAD.'
            category = 'observation'
        }
        @{
            timestamp = $ts
            role = 'model'
            content = @'
Decision: OverallVerdict AGREE on the remediATION claims.
Rationale: Pester 10/0/0 including UserPromptSubmit.BackgroundPrompt_DoesNotSupersedeRootInProgressTurn; both hook files define Test-PluginPromptIsBackgroundAgent and Get-PluginRootTurnIsolationDecision with reuse/isolate-skip/open-new; live 019 remains in_progress and current-turn.yaml still names 019 after this hostile UserPromptSubmit; PLAN-TRIAGECLUSTER-001 doneSummary still cites 013000Z; HEAD still f4060f037e62e64974026aff9d24e11b2f481952 with no commit/push.
Alternatives rejected: DISAGREE because TEST-MCP-TRIAGEPLUGIN-001 markdown does not name the new It block (FR AC is covered by the new test); FAIL tests-first because red output was not independently captured (pester mtime 15:50:19Z precedes hook 15:51:14Z and 019 logged a red step).
'@
            category = 'decision'
        }
    )
}

$null = Invoke-Iso -Method 'workflow.sessionlog.appendActions' -Params @{
    actions = @(
        @{
            order = 1
            description = 'Independent Pester TriagePluginIdentity.Tests.ps1: Passed 10 Failed 0 Skipped 0 including UserPromptSubmit.BackgroundPrompt_DoesNotSupersedeRootInProgressTurn'
            type = 'tool_call'
            status = 'completed'
            filePath = 'plugins/core/test-fixtures/pester/TriagePluginIdentity.Tests.ps1'
        }
        @{
            order = 2
            description = 'SHA256 equal for plugins/core/lib-ps/plugin-hook.ps1 and mcpserver-grok-plugin/lib/plugin-hook.ps1; isolation eval reuse/isolate-skip/open-new'
            type = 'edit'
            status = 'completed'
            filePath = 'plugins/core/lib-ps/plugin-hook.ps1'
        }
        @{
            order = 3
            description = 'client.SessionLog.QueryAsync text=Remediate hook cache isolation shows 019 in_progress planFile None todoId None; current-turn.yaml still 019'
            type = 'tool_call'
            status = 'completed'
            filePath = 'F:\GitHub\McpServer\.mcpServer\grok\current-turn.yaml'
        }
        @{
            order = 4
            description = 'HEAD f4060f037e62e64974026aff9d24e11b2f481952 equals origin/develop; staged empty; no commit/push this remediATION'
            type = 'tool_call'
            status = 'completed'
            filePath = ''
        }
        @{
            order = 5
            description = 'AGREE: remediATION claims A1-A5 and applicable B/C/D re-verified. Isolation is existing FR-MCP-TRIAGEPLUGIN-001 AC, not a PLAN done flip.'
            type = 'design_decision'
            status = 'completed'
            filePath = $receiptRel
        }
    )
}

$completeResponse = @"
Hostile validation of TRIAGEPLUGIN isolation remediATION.
OverallVerdict: AGREE
Receipt: $receiptRel
add-profile: 18 non-skill profile files
Pester: 10 passed, 0 failed, 0 skipped
019 status in_progress on GrokCode-20260818T182741Z-plugin-session
current-turn.yaml still names req-20260819T153500Z-019-remediate-hook-cache-isolation
PLAN-TRIAGECLUSTER-001 done remains the 013000Z closeout (not flipped this remediATION)
HEAD f4060f037e62e64974026aff9d24e11b2f481952
"@

$null = Invoke-Iso -Method 'workflow.sessionlog.completeTurn' -Params @{
    response = $completeResponse
} -TimeoutSeconds 120

$root4 = Get-RootTurn
$root4 | ConvertTo-Json | Set-Content (Join-Path $ev 'root-turn-after-hostile-session.json') -Encoding utf8
Write-Output "ROOT_AFTER_COMPLETE=$($root4.requestId) $($root4.status) $($root4.lastWriteUtc)"
Set-Content (Join-Path $ev 'current-turn-after-hostile-session.yaml') -Value $root4.raw -Encoding utf8

if (Test-Path (Join-Path $hostileCache 'current-turn.yaml')) {
    Copy-Item (Join-Path $hostileCache 'current-turn.yaml') (Join-Path $ev 'isolated-current-turn.yaml') -Force
    Write-Output 'ISOLATED_TURN:'
    Get-Content (Join-Path $hostileCache 'current-turn.yaml') -Raw
}
if (Test-Path (Join-Path $hostileCache 'session-state.yaml')) {
    Copy-Item (Join-Path $hostileCache 'session-state.yaml') (Join-Path $ev 'isolated-session-state.yaml') -Force
    Write-Output 'ISOLATED_SESSION:'
    Get-Content (Join-Path $hostileCache 'session-state.yaml') -Raw
}

Write-Output '=== QUERY PROOF ==='
$proof = Invoke-Iso -Method 'client.SessionLog.QueryAsync' -Params @{
    agent = 'GrokCode'
    text = 'Hostile validate TRIAGEPLUGIN isolation remediATION'
    limit = 10
}
Set-Content (Join-Path $ev '10-hostile-query-proof.txt') -Value $proof -Encoding utf8
Write-Output "PROOF_HAS_SESSION=$($proof.Contains($sessionId))"
Write-Output "PROOF_HAS_TURN=$($proof.Contains($requestId))"
Write-Output "PROOF_HAS_AGREE=$($proof.Contains('OverallVerdict: AGREE'))"
