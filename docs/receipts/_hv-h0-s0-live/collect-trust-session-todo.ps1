#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$pluginRoot = 'F:\GitHub\mcpserver-grok-plugin'
$workspace = 'F:\GitHub\McpServer'
$outDir = Join-Path $workspace 'docs\receipts\_hv-h0-s0-live'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$env:MCP_PLUGIN_ROOT = $pluginRoot
$env:GROK_PLUGIN_ROOT = $pluginRoot
$env:PLUGIN_AGENT_NAME = 'GrokCode'
$env:MCP_AGENT_NAME = 'GrokCode'
$env:MCP_WORKSPACE_PATH = $workspace
Set-Location -LiteralPath $workspace

. (Join-Path $pluginRoot 'lib\marker-resolver.ps1')
Import-Module (Join-Path $workspace 'tools\powershell\McpSession.psm1') -Force
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
        [int]$TimeoutSeconds = 40
    )
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    try {
        if ($Params.Count -gt 0) {
            $output = & $invoke -Command Invoke -Method $Method -ParamsObject $Params -WorkspacePath $workspace -TimeoutSeconds $TimeoutSeconds 2>&1 | Out-String
        } else {
            $output = & $invoke -Command Invoke -Method $Method -WorkspacePath $workspace -TimeoutSeconds $TimeoutSeconds 2>&1 | Out-String
        }
        return [ordered]@{ ok = $true; elapsedMs = $sw.ElapsedMilliseconds; output = $output }
    } catch {
        return [ordered]@{ ok = $false; elapsedMs = $sw.ElapsedMilliseconds; output = "INVOKE_EXCEPTION: $($_.Exception.Message)`n$($_.ScriptStackTrace)" }
    }
}

$utc = [datetime]::UtcNow
$utcStamp = $utc.ToString('yyyyMMddTHHmmssZ')
Write-Out '00-utc.txt' $utcStamp

$marker = Join-Path $workspace 'AGENTS-README-FIRST.yaml'
$sigOk = Test-MarkerSignature -MarkerFile $marker
$baseUrl = Get-MarkerField -MarkerFile $marker -FieldName 'baseUrl'
$nonce = -join ((1..16) | ForEach-Object { '{0:x}' -f (Get-Random -Maximum 16) })
$healthRaw = $null
$health = $null
$nonceOk = $false
try {
    $healthRaw = Invoke-WebRequest -Uri "$baseUrl/health?nonce=$nonce" -TimeoutSec 8 -UseBasicParsing
    $health = $healthRaw.Content | ConvertFrom-Json
    $nonceOk = ($health.nonce -eq $nonce)
} catch {
    $health = [ordered]@{ error = $_.Exception.Message }
}

$pluginJson = Get-Content -LiteralPath (Join-Path $pluginRoot '.grok-plugin\plugin.json') -Raw
$pluginVersionFile = (Get-Content -LiteralPath (Join-Path $pluginRoot '.version') -Raw).Trim()
$cwd = (Get-Location).Path

Write-Out '01-trust.json' ([ordered]@{
    timestampUtc = $utc.ToString('o')
    utcStamp = $utcStamp
    pluginVersionFile = $pluginVersionFile
    pluginJsonName = (($pluginJson | ConvertFrom-Json).name)
    pluginJsonVersion = (($pluginJson | ConvertFrom-Json).version)
    signatureOk = [bool]$sigOk
    nonce = $nonce
    nonceOk = [bool]$nonceOk
    healthStatusCode = $(if ($healthRaw) { [int]$healthRaw.StatusCode } else { $null })
    health = $health
    cwd = $cwd
    markerWrittenAtUtc = (Get-MarkerField -MarkerFile $marker -FieldName 'markerWrittenAtUtc')
})

$sessionId = New-McpSessionLogSlug -Agent 'GrokCode' -Model 'grok-hostile-validator' -TimestampUtc $utc
$requestId = "req-$utcStamp-001-hostile-h0-s0-sessionlog-remediate"
Write-Out '03-ids.json' ([ordered]@{
    sessionId = $sessionId
    requestId = $requestId
    agent = 'GrokCode'
})

$status = & $invoke -Command Status -WorkspacePath $workspace 2>&1 | Out-String
Write-Out '01b-plugin-status.txt' ([string]$status)

$bootstrap = Invoke-Plugin -Method 'workflow.sessionlog.bootstrap'
Write-Out '02-bootstrap.txt' ([string]$bootstrap.output)
Write-Out '02-bootstrap-meta.json' ([ordered]@{ ok = $bootstrap.ok; elapsedMs = $bootstrap.elapsedMs })

$open = Invoke-Plugin -Method 'workflow.sessionlog.openSession' -Params @{
    agent = 'GrokCode'
    sessionId = $sessionId
    title = 'Hostile H0 sessionlog-remediate-001 S0 requirements capture'
    model = 'grok-hostile-validator'
}
Write-Out '04-openSession.txt' ([string]$open.output)
Write-Out '04-openSession-meta.json' ([ordered]@{ ok = $open.ok; elapsedMs = $open.elapsedMs })

$begin = Invoke-Plugin -Method 'workflow.sessionlog.beginTurn' -Params @{
    requestId = $requestId
    queryTitle = 'Hostile H0 S0 sessionlog-remediate AC capture'
    queryText = 'H0 after S0 of docs/plans/sessionlog-remediate-001.md. Class 1 requirements capture. Attack PLAN TODO, live FR/TR/TEST, mappings, BUG 160-164 not done, no product persist this slice.'
    planFile = 'docs/plans/sessionlog-remediate-001.md'
    todoId = 'PLAN-SESSIONLOGREMEDIATE-001'
}
Write-Out '05-beginTurn.txt' ([string]$begin.output)
Write-Out '05-beginTurn-meta.json' ([ordered]@{ ok = $begin.ok; elapsedMs = $begin.elapsedMs })

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
    $got = Invoke-Plugin -Method 'workflow.todo.get' -Params @{ id = $tid } -TimeoutSeconds 50
    Write-Out "10-todo-$safe.txt" ([string]$got.output)
    Write-Out "10-todo-$safe-meta.json" ([ordered]@{ id = $tid; ok = $got.ok; elapsedMs = $got.elapsedMs; chars = $got.output.Length })
}

$gitStatusScoped = & git status --short --untracked-files=all -- docs/plans/sessionlog-remediate-001.md plugins/core/lib-ps/repl-invoke.ps1 src/McpServer.Support.Mcp tests 2>&1 | Out-String
Write-Out '30-git-status-scoped.txt' $gitStatusScoped
Write-Out '31-git-head.txt' ((& git rev-parse HEAD 2>&1 | Out-String).Trim())
Write-Out '32-git-diff-stat-product.txt' ((& git diff --stat HEAD -- plugins/core/lib-ps/repl-invoke.ps1 src/McpServer.Support.Mcp/Services src/McpServer.Support.Mcp/Controllers src/McpServer.Support.Mcp/Program.cs 2>&1 | Out-String))
Write-Out '32-git-untracked-src.txt' ((& git ls-files --others --exclude-standard -- plugins/core/lib-ps src/McpServer.Support.Mcp tests 2>&1 | Out-String))
Write-Out '32-git-status-short-top.txt' ((& git status --short --untracked-files=all 2>&1 | Select-Object -First 80 | Out-String))

$planPath = Join-Path $workspace 'docs\plans\sessionlog-remediate-001.md'
$planItem = Get-Item -LiteralPath $planPath -ErrorAction SilentlyContinue
Write-Out '40-plan-exists.json' ([ordered]@{
    exists = [bool]$planItem
    path = 'docs/plans/sessionlog-remediate-001.md'
    length = $(if ($planItem) { $planItem.Length } else { $null })
    lastWriteUtc = $(if ($planItem) { $planItem.LastWriteTimeUtc.ToString('o') } else { $null })
})

Write-Output "COLLECT1_DONE utc=$utcStamp sessionId=$sessionId requestId=$requestId sig=$sigOk nonceOk=$nonceOk"
