#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$pluginRoot = 'F:\GitHub\mcpserver-grok-plugin'
$workspace = 'F:\GitHub\McpServer'
$outDir = Join-Path $workspace 'docs\receipts\_h0-hostile-raw'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$env:MCP_PLUGIN_ROOT = $pluginRoot
$env:GROK_PLUGIN_ROOT = $pluginRoot
$env:PLUGIN_AGENT_NAME = 'GrokCode'
$env:MCP_AGENT_NAME = 'GrokCode'
$env:MCP_WORKSPACE_PATH = $workspace
Set-Location -LiteralPath $workspace

. (Join-Path $pluginRoot 'lib\marker-resolver.ps1')

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
        [hashtable]$Params = @{}
    )
    $invoke = Join-Path $pluginRoot 'lib\Invoke-McpPlugin.ps1'
    if ($Params.Count -gt 0) {
        return & pwsh.exe -NoProfile -NonInteractive -File $invoke -Command Invoke -Method $Method -ParamsObject $Params -WorkspacePath $workspace
    }
    return & pwsh.exe -NoProfile -NonInteractive -File $invoke -Command Invoke -Method $Method -WorkspacePath $workspace
}

$marker = Join-Path $workspace 'AGENTS-README-FIRST.yaml'
$sigOk = Test-MarkerSignature -MarkerFile $marker
$baseUrl = Get-MarkerField -MarkerFile $marker -FieldName 'baseUrl'
$nonce = "nonce-$(Get-Date -Format 'yyyyMMddHHmmss')-$PID"
$health = $null
$nonceOk = $false
try {
    $health = Invoke-RestMethod -Uri "$baseUrl/health?nonce=$nonce" -TimeoutSec 8
    $nonceOk = ($health.nonce -eq $nonce)
} catch {
    $health = @{ error = $_.Exception.Message }
}

Write-Out '00-trust.json' ([ordered]@{
    timestampUtc = (Get-Date).ToUniversalTime().ToString('o')
    pluginVersionFile = (Get-Content (Join-Path $pluginRoot '.grok-plugin\plugin.json') -Raw)
    signatureOk = [bool]$sigOk
    nonce = $nonce
    nonceOk = [bool]$nonceOk
    health = $health
    cwd = (Get-Location).Path
})

$bootstrap = Invoke-Plugin -Method 'workflow.sessionlog.bootstrap'
Write-Out '01-bootstrap.txt' ([string]$bootstrap)

$current = Invoke-Plugin -Method 'workflow.sessionlog.currentSession'
Write-Out '02-currentSession.txt' ([string]$current)

$todo = Invoke-Plugin -Method 'workflow.todo.get' -Params @{ id = 'PLAN-TRIAGECLUSTER-001' }
Write-Out '03-todo-PLAN-TRIAGECLUSTER-001.txt' ([string]$todo)

$frIds = @(
    'FR-MCP-TRIAGEERR-001'
    'FR-MCP-TRIAGESTORE-001'
    'FR-MCP-TRIAGESTORE-002'
    'FR-MCP-TRIAGESCHEMA-001'
    'FR-MCP-TRIAGEPLUGIN-001'
    'FR-MCP-TRIAGETODO-001'
    'FR-MCP-TRIAGEREQ-001'
    'FR-MCP-TRIAGEHELP-001'
)
foreach ($id in $frIds) {
    $r = Invoke-Plugin -Method 'workflow.requirements.getFr' -Params @{ id = $id }
    Write-Out ("10-fr-$id.txt") ([string]$r)
}

$trIds = @(
    'TR-MCP-TRIAGEERR-001'
    'TR-MCP-TRIAGESTORE-001'
    'TR-MCP-TRIAGESTORE-002'
    'TR-MCP-TRIAGESCHEMA-001'
    'TR-MCP-TRIAGEPLUGIN-001'
    'TR-MCP-TRIAGETODO-001'
    'TR-MCP-TRIAGEREQ-001'
    'TR-MCP-TRIAGEHELP-001'
)
foreach ($id in $trIds) {
    $r = Invoke-Plugin -Method 'workflow.requirements.getTr' -Params @{ id = $id }
    Write-Out ("11-tr-$id.txt") ([string]$r)
}

$testIds = @(
    'TEST-MCP-TRIAGEERR-001'
    'TEST-MCP-TRIAGESTORE-001'
    'TEST-MCP-TRIAGESTORE-002'
    'TEST-MCP-TRIAGESTORE-003'
    'TEST-MCP-TRIAGESTORE-004'
    'TEST-MCP-TRIAGESTORE-005'
    'TEST-MCP-TRIAGESTORE-006'
    'TEST-MCP-TRIAGESTORE-007'
    'TEST-MCP-TRIAGESCHEMA-001'
    'TEST-MCP-TRIAGEPLUGIN-001'
    'TEST-MCP-TRIAGEPLUGIN-002'
    'TEST-MCP-TRIAGEPLUGIN-003'
    'TEST-MCP-TRIAGEPLUGIN-004'
    'TEST-MCP-TRIAGEPLUGIN-005'
    'TEST-MCP-TRIAGETODO-001'
    'TEST-MCP-TRIAGETODO-002'
    'TEST-MCP-TRIAGEREQ-001'
    'TEST-MCP-TRIAGEHELP-001'
)
foreach ($id in $testIds) {
    $r = Invoke-Plugin -Method 'workflow.requirements.getTest' -Params @{ id = $id }
    Write-Out ("12-test-$id.txt") ([string]$r)
}

foreach ($id in $frIds) {
    $r = Invoke-Plugin -Method 'workflow.requirements.listMappings' -Params @{ frId = $id }
    Write-Out ("13-map-$id.txt") ([string]$r)
}

$qImpl = Invoke-Plugin -Method 'client.SessionLog.QueryAsync' -Params @{
    agent = 'GrokCode'
    text = 'req-20260818T191655Z-004-s0-triagecluster-reqs'
    limit = 20
}
Write-Out '20-sessionlog-query-implementer-turn.txt' ([string]$qImpl)

$qImplSess = Invoke-Plugin -Method 'client.SessionLog.QueryAsync' -Params @{
    agent = 'GrokCode'
    text = 'GrokCode-20260818T182741Z-plugin-session'
    limit = 20
}
Write-Out '21-sessionlog-query-implementer-session.txt' ([string]$qImplSess)

$qHist = Invoke-Plugin -Method 'workflow.sessionlog.queryHistory' -Params @{
    agent = 'GrokCode'
    limit = 25
    offset = 0
}
Write-Out '22-queryHistory-GrokCode.txt' ([string]$qHist)

$qSelf = Invoke-Plugin -Method 'client.SessionLog.QueryAsync' -Params @{
    agent = 'GrokCode'
    text = 'req-20260818T192456Z-prompt-33a0'
    limit = 10
}
Write-Out '23-sessionlog-query-hostile-hook-turn.txt' ([string]$qSelf)

Write-Output "DONE outDir=$outDir"
Get-ChildItem -LiteralPath $outDir | Select-Object Name, Length | Format-Table -AutoSize | Out-String
