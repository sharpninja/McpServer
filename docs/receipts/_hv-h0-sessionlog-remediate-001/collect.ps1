#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$pluginRoot = 'F:\GitHub\mcpserver-grok-plugin'
$workspace = 'F:\GitHub\McpServer'
$outDir = Join-Path $workspace 'docs\receipts\_hv-h0-sessionlog-remediate-001'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$env:MCP_PLUGIN_ROOT = $pluginRoot
$env:GROK_PLUGIN_ROOT = $pluginRoot
$env:PLUGIN_AGENT_NAME = 'GrokCode'
$env:MCP_AGENT_NAME = 'GrokCode'
$env:MCP_WORKSPACE_PATH = $workspace
Set-Location -LiteralPath $workspace

. (Join-Path $pluginRoot 'lib\marker-resolver.ps1')
$invoke = Join-Path $pluginRoot 'lib\Invoke-McpPlugin.ps1'

function Write-Out {
    param([string]$Name, [object]$Value)
    $path = Join-Path $outDir $Name
    if ($Value -is [string]) {
        Set-Content -LiteralPath $path -Value $Value -Encoding utf8
    } else {
        ($Value | ConvertTo-Json -Depth 60) | Set-Content -LiteralPath $path -Encoding utf8
    }
    return $path
}

function Invoke-Plugin {
    param(
        [Parameter(Mandatory)][string]$Method,
        [hashtable]$Params = @{}
    )
    try {
        if ($Params.Count -gt 0) {
            $output = & $invoke -Command Invoke -Method $Method -ParamsObject $Params -WorkspacePath $workspace -TimeoutSeconds 25 2>&1 | Out-String
        } else {
            $output = & $invoke -Command Invoke -Method $Method -WorkspacePath $workspace -TimeoutSeconds 25 2>&1 | Out-String
        }
        return $output
    } catch {
        return "INVOKE_EXCEPTION: $($_.Exception.Message)`n$($_.ScriptStackTrace)"
    }
}

$utc = (Get-Date).ToUniversalTime()
$utcStamp = $utc.ToString('yyyyMMddTHHmmssZ')
Write-Out '00-utc.txt' $utcStamp

$marker = Join-Path $workspace 'AGENTS-README-FIRST.yaml'
$sigOk = Test-MarkerSignature -MarkerFile $marker
$baseUrl = Get-MarkerField -MarkerFile $marker -FieldName 'baseUrl'
$nonce = "nonce-$utcStamp-$PID"
$health = $null
$nonceOk = $false
$healthRaw = $null
try {
    $healthRaw = Invoke-WebRequest -Uri "$baseUrl/health?nonce=$nonce" -TimeoutSec 8 -UseBasicParsing
    $health = $healthRaw.Content | ConvertFrom-Json
    $nonceOk = ($health.nonce -eq $nonce)
} catch {
    $health = @{ error = $_.Exception.Message }
}

$pluginJson = Get-Content -LiteralPath (Join-Path $pluginRoot '.grok-plugin\plugin.json') -Raw
$pluginVersionFile = Get-Content -LiteralPath (Join-Path $pluginRoot '.version') -Raw

Write-Out '01-trust.json' ([ordered]@{
    timestampUtc = $utc.ToString('o')
    utcStamp = $utcStamp
    pluginJson = $pluginJson
    pluginVersionFile = $pluginVersionFile.Trim()
    signatureOk = [bool]$sigOk
    nonce = $nonce
    nonceOk = [bool]$nonceOk
    healthStatusCode = $(if ($healthRaw) { [int]$healthRaw.StatusCode } else { $null })
    health = $health
    cwd = (Get-Location).Path
    markerWrittenAtUtc = (Get-MarkerField -MarkerFile $marker -FieldName 'markerWrittenAtUtc')
})

$bootstrap = Invoke-Plugin -Method 'workflow.sessionlog.bootstrap'
Write-Out '02-bootstrap.txt' ([string]$bootstrap)

$sessionId = "GrokCode-$utcStamp-hostile-sessionlog-h0"
$requestId = "req-$utcStamp-001-hostile-h0-sessionlog-remediate"
Write-Out '03-ids.json' ([ordered]@{
    sessionId = $sessionId
    requestId = $requestId
    agent = 'GrokCode'
})

$open = Invoke-Plugin -Method 'workflow.sessionlog.openSession' -Params @{
    agent = 'GrokCode'
    sessionId = $sessionId
    title = 'Hostile H0 sessionlog-remediate-001 S0 requirements capture'
    model = 'grok-code'
}
Write-Out '04-openSession.txt' ([string]$open)

$begin = Invoke-Plugin -Method 'workflow.sessionlog.beginTurn' -Params @{
    requestId = $requestId
    queryTitle = 'Hostile H0 sessionlog-remediate-001 S0 AC capture'
    queryText = 'H0 after S0 of docs/plans/sessionlog-remediate-001.md. Class 1 requirements capture. Attack plan, PLAN TODO, live FR/TR/TEST, BUG links, no product persist this slice.'
}
Write-Out '05-beginTurn.txt' ([string]$begin)

$todoIds = @(
    'PLAN-SESSIONLOGREMEDIATE-001'
    'BUG-TRIAGE-160'
    'BUG-TRIAGE-161'
    'BUG-TRIAGE-162'
    'BUG-TRIAGE-163'
    'BUG-TRIAGE-164'
    'MCP-SESSIONLOG-001'
    'MCP-SESSIONLOG-002'
)
foreach ($tid in $todoIds) {
    $safe = $tid -replace '[^A-Za-z0-9-]', '_'
    $got = Invoke-Plugin -Method 'workflow.todo.get' -Params @{ id = $tid }
    Write-Out "10-todo-$safe.txt" ([string]$got)
}

$frIds = @(
    'FR-MCP-170'
    'FR-MCP-171'
    'FR-MCP-172'
    'FR-MCP-SESSIONPERSIST-001'
    'FR-MCP-SESSIONPERSIST-002'
    'FR-MCP-SESSIONPERSIST-003'
    'FR-MCP-TRIAGE-002'
)
foreach ($rid in $frIds) {
    $safe = $rid -replace '[^A-Za-z0-9-]', '_'
    $got = Invoke-Plugin -Method 'workflow.requirements.getFr' -Params @{ id = $rid }
    Write-Out "20-fr-$safe.txt" ([string]$got)
}

$trIds = @(
    'TR-MCP-PERSIST-001'
    'TR-MCP-PERSIST-002'
    'TR-MCP-PERSIST-003'
    'TR-MCP-PERSIST-004'
    'TR-MCP-SESSIONPERSIST-001'
    'TR-MCP-SESSIONPERSIST-002'
    'TR-MCP-SESSIONPERSIST-003'
    'TR-MCP-SESSIONPERSIST-004'
    'TR-MCP-TRIAGE-004'
)
foreach ($rid in $trIds) {
    $safe = $rid -replace '[^A-Za-z0-9-]', '_'
    $got = Invoke-Plugin -Method 'workflow.requirements.getTr' -Params @{ id = $rid }
    Write-Out "21-tr-$safe.txt" ([string]$got)
}

$testIds = @(
    'TEST-MCP-195'
    'TEST-MCP-196'
    'TEST-MCP-SESSIONPERSIST-001'
    'TEST-MCP-SESSIONPERSIST-002'
)
foreach ($rid in $testIds) {
    $safe = $rid -replace '[^A-Za-z0-9-]', '_'
    $got = Invoke-Plugin -Method 'workflow.requirements.getTest' -Params @{ id = $rid }
    Write-Out "22-test-$safe.txt" ([string]$got)
}

$mapFrs = @(
    'FR-MCP-170'
    'FR-MCP-171'
    'FR-MCP-172'
    'FR-MCP-SESSIONPERSIST-001'
    'FR-MCP-SESSIONPERSIST-002'
    'FR-MCP-SESSIONPERSIST-003'
)
foreach ($rid in $mapFrs) {
    $safe = $rid -replace '[^A-Za-z0-9-]', '_'
    $got = Invoke-Plugin -Method 'workflow.requirements.listMappings' -Params @{ frId = $rid }
    Write-Out "23-map-$safe.txt" ([string]$got)
}

$gitStatus = & git status --short --untracked-files=all -- docs/plans/sessionlog-remediate-001.md plugins/core/lib-ps/repl-invoke.ps1 src/McpServer.Support.Mcp 2>&1 | Out-String
Write-Out '30-git-status-scoped.txt' $gitStatus

$gitHead = & git rev-parse HEAD 2>&1 | Out-String
Write-Out '31-git-head.txt' $gitHead.Trim()

$diffRepl = & git diff --stat HEAD -- plugins/core/lib-ps/repl-invoke.ps1 src/McpServer.Support.Mcp/Services src/McpServer.Support.Mcp/Controllers src/McpServer.Support.Mcp/Program.cs 2>&1 | Out-String
Write-Out '32-git-diff-stat-product.txt' $diffRepl

$healthPlain = $null
try {
    $healthPlain = Invoke-WebRequest -Uri "$baseUrl/health" -TimeoutSec 8 -UseBasicParsing
    Write-Out '33-health-no-nonce.json' ([ordered]@{
        statusCode = [int]$healthPlain.StatusCode
        content = $healthPlain.Content
    })
} catch {
    Write-Out '33-health-no-nonce.json' ([ordered]@{ error = $_.Exception.Message })
}

$planPath = Join-Path $workspace 'docs\plans\sessionlog-remediate-001.md'
$planExists = Test-Path -LiteralPath $planPath
$planHead = if ($planExists) { (Get-Content -LiteralPath $planPath -TotalCount 50 | Out-String) } else { 'MISSING' }
Write-Out '40-plan-head.txt' $planHead
Write-Out '40-plan-exists.json' ([ordered]@{ exists = $planExists; path = 'docs/plans/sessionlog-remediate-001.md' })

$rcHits = Select-String -LiteralPath $planPath -Pattern 'RC[1-6]' -ErrorAction SilentlyContinue | ForEach-Object { $_.Line.Trim() }
Write-Out '41-plan-rc-lines.json' @($rcHits)

Write-Out '99-done.txt' "collector finished $utcStamp"
Write-Output "COLLECT_DONE utc=$utcStamp sessionId=$sessionId requestId=$requestId sig=$sigOk nonceOk=$nonceOk"
