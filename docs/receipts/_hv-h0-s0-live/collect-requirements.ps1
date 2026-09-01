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
        [int]$TimeoutSeconds = 50
    )
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    try {
        $output = & $invoke -Command Invoke -Method $Method -ParamsObject $Params -WorkspacePath $workspace -TimeoutSeconds $TimeoutSeconds 2>&1 | Out-String
        return [ordered]@{ ok = $true; elapsedMs = $sw.ElapsedMilliseconds; output = $output }
    } catch {
        return [ordered]@{ ok = $false; elapsedMs = $sw.ElapsedMilliseconds; output = "INVOKE_EXCEPTION: $($_.Exception.Message)" }
    }
}

$frIds = @(
    'FR-MCP-170'
    'FR-MCP-171'
    'FR-MCP-172'
    'FR-MCP-SESSIONPERSIST-001'
    'FR-MCP-SESSIONPERSIST-002'
    'FR-MCP-SESSIONPERSIST-003'
    'FR-MCP-TRIAGE-002'
    'FR-MCP-FAILSAFE-001'
    'FR-MCP-SESSIONLOGSAN-001'
    'FR-MCP-SESSIONLOGCTX-001'
)
foreach ($rid in $frIds) {
    $safe = $rid -replace '[^A-Za-z0-9-]', '_'
    $got = Invoke-Plugin -Method 'workflow.requirements.getFr' -Params @{ id = $rid }
    Write-Out "20-fr-$safe.txt" ([string]$got.output)
    Write-Out "20-fr-$safe-meta.json" ([ordered]@{ id = $rid; ok = $got.ok; elapsedMs = $got.elapsedMs; chars = $got.output.Length })
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
    Write-Out "21-tr-$safe.txt" ([string]$got.output)
    Write-Out "21-tr-$safe-meta.json" ([ordered]@{ id = $rid; ok = $got.ok; elapsedMs = $got.elapsedMs; chars = $got.output.Length })
}

$testIds = @(
    'TEST-MCP-195'
    'TEST-MCP-196'
    'TEST-MCP-SESSIONPERSIST-001'
    'TEST-MCP-SESSIONPERSIST-002'
    'TEST-MCP-FAILSAFE-001'
)
foreach ($rid in $testIds) {
    $safe = $rid -replace '[^A-Za-z0-9-]', '_'
    $got = Invoke-Plugin -Method 'workflow.requirements.getTest' -Params @{ id = $rid }
    Write-Out "22-test-$safe.txt" ([string]$got.output)
    Write-Out "22-test-$safe-meta.json" ([ordered]@{ id = $rid; ok = $got.ok; elapsedMs = $got.elapsedMs; chars = $got.output.Length })
}

$mapFrs = @(
    'FR-MCP-170'
    'FR-MCP-171'
    'FR-MCP-172'
    'FR-MCP-SESSIONPERSIST-001'
    'FR-MCP-FAILSAFE-001'
    'FR-MCP-TRIAGE-002'
)
foreach ($rid in $mapFrs) {
    $safe = $rid -replace '[^A-Za-z0-9-]', '_'
    $got = Invoke-Plugin -Method 'workflow.requirements.listMappings' -Params @{ frId = $rid }
    Write-Out "23-map-$safe.txt" ([string]$got.output)
    Write-Out "23-map-$safe-meta.json" ([ordered]@{ id = $rid; ok = $got.ok; elapsedMs = $got.elapsedMs; chars = $got.output.Length })
}

Write-Output 'COLLECT2_DONE'
