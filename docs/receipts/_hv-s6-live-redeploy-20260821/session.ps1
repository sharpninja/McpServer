#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$pluginRoot = 'C:\Users\kingd\.grok\installed-plugins\f--github-mcpserver-grok-plugin-67f1f31f'
$workspace = 'F:\GitHub\McpServer'
$outDir = Join-Path $workspace 'docs\receipts\_hv-s6-live-redeploy-20260821'
$cache = Join-Path $outDir 'plugin-cache'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
New-Item -ItemType Directory -Force -Path $cache | Out-Null

$env:MCP_PLUGIN_ROOT = $pluginRoot
$env:GROK_PLUGIN_ROOT = $pluginRoot
$env:PLUGIN_AGENT_NAME = 'GrokCode'
$env:MCP_AGENT_NAME = 'GrokCode'
$env:MCP_WORKSPACE_PATH = $workspace
Set-Location -LiteralPath $workspace

$invoke = Join-Path $pluginRoot 'lib\Invoke-McpPlugin.ps1'

function Write-Out {
    param([string]$Name, [object]$Value)
    $path = Join-Path $outDir $Name
    if ($Value -is [string]) {
        Set-Content -LiteralPath $path -Value $Value -Encoding utf8
    } else {
        ($Value | ConvertTo-Json -Depth 40) | Set-Content -LiteralPath $path -Encoding utf8
    }
    return $path
}

function Invoke-Plugin {
    param(
        [Parameter(Mandatory)][string]$Method,
        [hashtable]$Params = @{},
        [int]$TimeoutSeconds = 90
    )
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    try {
        if ($Params.Count -gt 0) {
            $output = & $invoke -Command Invoke -Method $Method -ParamsObject $Params -WorkspacePath $workspace -CacheRoot $cache -TimeoutSeconds $TimeoutSeconds 2>&1 | Out-String
        } else {
            $output = & $invoke -Command Invoke -Method $Method -WorkspacePath $workspace -CacheRoot $cache -TimeoutSeconds $TimeoutSeconds 2>&1 | Out-String
        }
        return [ordered]@{ ok = $true; elapsedMs = $sw.ElapsedMilliseconds; output = $output }
    } catch {
        return [ordered]@{ ok = $false; elapsedMs = $sw.ElapsedMilliseconds; output = "INVOKE_EXCEPTION: $($_.Exception.Message)" }
    }
}

$utc = [datetime]::UtcNow
$utcStamp = $utc.ToString('yyyyMMddTHHmmssZ')
$sessionId = "GrokCode-$utcStamp-hostile-s6-redeploy"
$requestId = "req-$utcStamp-001-hostile-s6-nuke-redeploy"
Write-Out '00-ids.json' ([ordered]@{
    utc = $utcStamp
    timestampO = $utc.ToString('o')
    sessionId = $sessionId
    requestId = $requestId
    cache = $cache
})

$boot = Invoke-Plugin -Method 'workflow.sessionlog.bootstrap'
Write-Out '11-bootstrap.txt' ([string]$boot.output)

$open = Invoke-Plugin -Method 'workflow.sessionlog.openSession' -Params @{
    agent = 'GrokCode'
    sessionId = $sessionId
    title = 'Hostile S6 Nuke UpdateService live redeploy'
    model = 'grok-hostile-validator'
}
Write-Out '12-open.txt' ([string]$open.output)

$begin = Invoke-Plugin -Method 'workflow.sessionlog.beginTurn' -Params @{
    requestId = $requestId
    queryTitle = 'Hostile S6 Nuke UpdateService live redeploy'
    queryText = 'Class 2 operator-directed Nuke UpdateService redeploy. Attack A1-A6 HMAC service version health gsudo GitVersion. Do not mark TODOs done. Do not commit.'
    planFile = 'docs/plans/sessionlog-remediate-001.md'
    todoId = 'PLAN-SESSIONLOGREMEDIATE-001'
}
Write-Out '13-begin.txt' ([string]$begin.output)

$now = [datetime]::UtcNow.ToString('o')
$dialog = Invoke-Plugin -Method 'workflow.sessionlog.appendDialog' -Params @{
    dialogItems = @(
        @{
            timestamp = $now
            role = 'model'
            content = 'add-profile executed: 18 non-skill profile markdown files. Class 2 user-directed ops. Surface C N/A.'
            category = 'observation'
        }
        @{
            timestamp = $now
            role = 'model'
            content = 'Decision: score this as Class 2 operator-directed Nuke redeploy. Consequence: do not FAIL missing FR/TR or leftover-27/163. S6 extra persist proofs are out of scope unless implementer claimed plan-step complete. Alternatives rejected: treating this as Class 1 Byrd product work.'
            category = 'decision'
        }
        @{
            timestamp = $now
            role = 'model'
            content = 'Independent re-verify: Test-MarkerSignature True, Invoke-FullBootstrap True, Status available, Get-Service Running, ProductVersion 1.4.30+ee89cd63, health 200 Healthy storage reachable, GitVersion.yml staged 1.4.30, no new commit, live exe SHA matches manifest generatedBy Build.UpdateService.cs.'
            category = 'observation'
        }
    )
}
Write-Out '14-dialog.txt' ([string]$dialog.output)

$actions = Invoke-Plugin -Method 'workflow.sessionlog.appendActions' -Params @{
    actions = @(
        @{ order = 1; description = 'add-profile: 18 non-skill profile markdown files'; type = 'edit'; status = 'completed'; filePath = 'C:\Users\kingd\.claude\profile' }
        @{ order = 2; description = 'Plugin HMAC only: Test-MarkerSignature True, Invoke-FullBootstrap True, Status available. Did not construct HMACSHA256.'; type = 'edit'; status = 'completed'; filePath = 'C:\Users\kingd\.grok\installed-plugins\f--github-mcpserver-grok-plugin-67f1f31f\lib\marker-resolver.ps1' }
        @{ order = 3; description = 'Re-read UpdateService log EXIT=0 duration 3:19 WSHealth 38/38'; type = 'edit'; status = 'completed'; filePath = 'docs/receipts/_hv-s6-updateservice-20260821T101630Z/update-service.log' }
        @{ order = 4; description = 'FileVersionInfo live exe 1.4.30+ee89cd63; hash matches deployment manifest'; type = 'edit'; status = 'completed'; filePath = 'C:\ProgramData\McpServer\McpServer.Support.Mcp.exe' }
        @{ order = 5; description = 'Class 2 ops: score C N/A; D does not require leftover S6 product persist proofs'; type = 'design_decision'; status = 'completed'; filePath = 'docs/plans/sessionlog-remediate-001.md' }
    )
}
Write-Out '15-actions.txt' ([string]$actions.output)

$complete = Invoke-Plugin -Method 'workflow.sessionlog.completeTurn' -Params @{
    response = 'Hostile S6 live Nuke redeploy review. Independent re-verify of A1-A6. Receipt docs/receipts/hostile-validator-TIMESTAMP.md. Did not mark TODOs done. Did not commit.'
} -TimeoutSeconds 120
Write-Out '16-complete.txt' ([string]$complete.output)

$query = Invoke-Plugin -Method 'workflow.sessionlog.queryHistory' -Params @{
    agent = 'GrokCode'
    sessionId = $sessionId
    limit = 1
    offset = 0
} -TimeoutSeconds 90
Write-Out '17-query-proof.txt' ([string]$query.output)

Write-Output "SESSION_DONE utc=$utcStamp sessionId=$sessionId requestId=$requestId bootOk=$($boot.ok) openOk=$($open.ok) beginOk=$($begin.ok) completeOk=$($complete.ok) queryOk=$($query.ok)"
