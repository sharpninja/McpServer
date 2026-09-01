#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$pluginRoot = 'F:\GitHub\mcpserver-grok-plugin'
$workspace = 'F:\GitHub\McpServer'
$ev = Join-Path $workspace 'docs\receipts\_hv-20260819T164408Z'
$invoke = Join-Path $pluginRoot 'lib\Invoke-McpPlugin.ps1'
$hostileCache = Join-Path $ev 'hostile-cache'
$sessionId = 'GrokCode-20260819T164408Z-hostile-wrapup'
$requestId = 'req-20260819T164408Z-001-hostile-validate-wrap-up'
$receiptRel = 'docs/receipts/hostile-validator-20260819T164408Z.md'
$utc = '2026-08-19T16:44:08Z'

$env:MCP_PLUGIN_ROOT = $pluginRoot
$env:GROK_PLUGIN_ROOT = $pluginRoot
$env:PLUGIN_AGENT_NAME = 'GrokCode'
$env:MCP_AGENT_NAME = 'GrokCode'
$env:MCP_WORKSPACE_PATH = $workspace
$env:MCP_SUBAGENT_ID = 'hostile-validator'
Set-Location -LiteralPath $workspace

. (Join-Path $pluginRoot 'lib\yaml-object-mutation.ps1')
$isoStatePath = Join-Path $hostileCache 'session-state.yaml'
$state = [ordered]@{
    status = 'verified'
    agent = 'GrokCode'
    sessionId = $sessionId
    lastUpdated = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    markerFilePath = 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml'
    markerLastWriteUtc = '2026-08-19T13:05:36.9456178Z'
    title = 'Hostile validate wrap-up push claims'
}
Write-McpYamlObject -Path $isoStatePath -Document $state

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
    try {
        $out = & $invoke @args 2>&1
        $text = ($out | Out-String)
        if ([string]::IsNullOrWhiteSpace($text)) { $text = '<empty>' }
    } catch {
        $text = "EXCEPTION: $($_.Exception.ToString())"
    }
    $safe = ($Method -replace '[^a-zA-Z0-9]+', '-')
    $path = Join-Path $ev ("iso-$safe.txt")
    Set-Content -LiteralPath $path -Value $text -Encoding utf8
    Write-Output "METHOD $Method file=$path chars=$($text.Length)"
    if ($text.Length -gt 1600) { Write-Output $text.Substring(0, 1600) } else { Write-Output $text }
    Write-Output '-----'
    return $text
}

$root0 = Get-RootTurn
Write-Output "ROOT_BEFORE=$($root0.requestId) $($root0.status) $($root0.lastWriteUtc)"

$null = Invoke-Iso -Method 'workflow.sessionlog.bootstrap' -Params @{}
$null = Invoke-Iso -Method 'workflow.sessionlog.openSession' -Params @{
    agent = 'GrokCode'
    sessionId = $sessionId
    title = 'Hostile validate wrap-up push claims'
    model = 'grok'
}

$null = Invoke-Iso -Method 'workflow.sessionlog.beginTurn' -Params @{
    requestId = $requestId
    queryTitle = 'Hostile validate wrap-up push claims'
    queryText = 'Hostile validation of operator-directed refresh-docs wrap-up and GitHub/wiki push claims. Class 2. planFile None. todoId None.'
    planFile = 'None'
    todoId = 'None'
}
$root1 = Get-RootTurn
Write-Output "ROOT_AFTER_BEGIN=$($root1.requestId) $($root1.status) $($root1.lastWriteUtc)"

$null = Invoke-Iso -Method 'workflow.sessionlog.updateTurn' -Params @{
    response = 'Independent hostile re-verify in progress.'
    interpretation = 'Attack wrap-up push claims A1-A8 plus workspace rules. Surface C N/A for class 2 ops. Do not flip TODOs.'
    tags = @('hostile-validator', 'wrap-up', 'refresh-docs', 'class-2')
}

$ts = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
$decision = 'Decision: OverallVerdict AGREE on wrap-up push claims. Rationale: marker HMAC True; health Healthy 1.4.28+f4060f03 storage reachable nonce match; wiki.yaml schema mcp-wiki-export/v1 documents 34 file-backed 28 missing 0; ZIP sha256 C666EBD12134F452C9722247F3343F82D01653DA06E2328CD2C6E47CEB438D2E length 954459 entries 79 including github/Byrd-Todo-Execution-Spec.md; git diff --check 0; ValidateTraceability Succeeded findings=0; ls-remote origin/develop cbae4dd matches HEAD (docs-receipts on top of c81abaf feat(triage)); wiki HEAD 763c838 includes Byrd-Todo-Execution-Spec.md and Agent-Plugin-Availability.md UserPromptSubmit background section; QueryAsync text refresh-docs wrap-up shows 020 completed planFile None todoId None; todo get+audit last done flip 2026-08-19T01:41:53Z; origin/main does not contain c81abaf/cbae4dd; Azure develop still f4060f03. Alternatives rejected: FAIL A3 because empty-tree vs HEAD has historical whitespace (wrap-up command was git diff --check, independently 0); FAIL A5 because the word isolat is absent (section is UserPromptSubmit and background agents); FAIL A4 because HEAD is not c81abaf (parent allowed subsequent docs-receipts commit; ls-remote matches HEAD).'
$null = Invoke-Iso -Method 'workflow.sessionlog.appendDialog' -Params @{
    dialogItems = @(
        @{
            timestamp = $ts
            role = 'model'
            content = 'add-profile executed: 18 non-skill profile markdown files. Independently verified marker signature, health nonce, ZIP hash, wiki.yaml parse, ls-remote origin and wiki, ValidateTraceability, sessionlog QueryAsync without exact-requestId filter, todo get and GetAuditAsync.'
            category = 'observation'
        }
        @{
            timestamp = $ts
            role = 'model'
            content = $decision
            category = 'decision'
        }
    )
}

$null = Invoke-Iso -Method 'workflow.sessionlog.appendActions' -Params @{
    actions = @(
        @{ order = 1; description = 'Test-MarkerSignature True. GET /health nonce nonce-hv-20260819T164408Z-wrapup echoed. status Healthy version 1.4.28+f4060f037e62e64974026aff9d24e11b2f481952 storage reachable.'; type = 'tool_call'; status = 'completed'; filePath = 'AGENTS-README-FIRST.yaml' }
        @{ order = 2; description = 'ZIP sha256 C666EBD12134F452C9722247F3343F82D01653DA06E2328CD2C6E47CEB438D2E length 954459 entries 79 including github/Byrd-Todo-Execution-Spec.md. wiki.yaml schema mcp-wiki-export/v1 documents 34 file-backed 28 missing 0.'; type = 'tool_call'; status = 'completed'; filePath = 'docs/requirements/requirements-wiki-documents.zip' }
        @{ order = 3; description = 'ls-remote origin/develop cbae4dd6febf6cfab81b77a2578ff1b36a6a3499 matches HEAD. c81abaf feat(triage) is parent. Wiki HEAD 763c83803046a018107e06c9945e508551236d86. origin/main d14a2330 does not contain wrap-up SHAs. Azure develop still f4060f03.'; type = 'tool_call'; status = 'completed'; filePath = '' }
        @{ order = 4; description = 'client.SessionLog.QueryAsync agent=GrokCode text=refresh-docs wrap-up (no exact requestId filter) shows 020 completed on GrokCode-20260818T182741Z-plugin-session planFile None todoId None. workflow.todo.get plus client.Todo.GetAuditAsync last done true at 2026-08-19T01:41:53.1708697Z totalCount 7.'; type = 'tool_call'; status = 'completed'; filePath = 'docs/receipts/_hv-20260819T164408Z/06-turn-020-slice.txt' }
        @{ order = 5; description = 'AGREE: wrap-up push claims A1-A8 and applicable B/D re-verified. Surface C N/A class 2 ops. No PLAN TODO flipped this wrap-up.'; type = 'design_decision'; status = 'completed'; filePath = $receiptRel }
    )
}

$completeResponse = @"
Hostile validation of refresh-docs wrap-up push.
OverallVerdict: AGREE
Receipt: $receiptRel
add-profile: 18 non-skill profile files
PASS 15 FAIL 0 UNKNOWN 0
Surface C N/A class 2
020 completed on GrokCode-20260818T182741Z-plugin-session
PLAN-TRIAGECLUSTER-001 last done flip 2026-08-19T01:41:53Z
HEAD cbae4dd on origin/develop; wiki 763c838
"@

$null = Invoke-Iso -Method 'workflow.sessionlog.completeTurn' -Params @{
    response = $completeResponse
} -TimeoutSeconds 120

$root2 = Get-RootTurn
$root2 | ConvertTo-Json | Set-Content (Join-Path $ev 'root-turn-after-hostile-session.json') -Encoding utf8
Write-Output "ROOT_AFTER_COMPLETE=$($root2.requestId) $($root2.status) $($root2.lastWriteUtc)"
Set-Content (Join-Path $ev 'current-turn-after-hostile-session.yaml') -Value $root2.raw -Encoding utf8

Write-Output '=== QUERY PROOF ==='
$proof = Invoke-Iso -Method 'client.SessionLog.QueryAsync' -Params @{
    agent = 'GrokCode'
    text = 'Hostile validate wrap-up push claims'
    limit = 10
}
Set-Content (Join-Path $ev '10-hostile-query-proof.txt') -Value $proof -Encoding utf8
Write-Output "PROOF_HAS_SESSION=$($proof.Contains($sessionId))"
Write-Output "PROOF_HAS_TURN=$($proof.Contains($requestId))"
Write-Output "PROOF_HAS_AGREE=$($proof.Contains('OverallVerdict: AGREE'))"
Write-Output "PROOF_HAS_NONE=$($proof.Contains('planFile: None'))"
Write-Output "IDS session=$sessionId turn=$requestId receipt=$receiptRel utc=$utc"
