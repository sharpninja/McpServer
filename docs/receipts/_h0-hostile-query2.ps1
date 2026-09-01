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

$invoke = Join-Path $pluginRoot 'lib\Invoke-McpPlugin.ps1'

function Invoke-Plugin {
    param(
        [Parameter(Mandatory)][string]$Method,
        [string]$ParamsYaml = ''
    )
    if ([string]::IsNullOrWhiteSpace($ParamsYaml)) {
        return & $invoke -Command Invoke -Method $Method -WorkspacePath $workspace
    }
    return & $invoke -Command Invoke -Method $Method -Params $ParamsYaml -WorkspacePath $workspace
}

function Save-Result {
    param([string]$Name, [string]$Text)
    $path = Join-Path $outDir $Name
    Set-Content -LiteralPath $path -Value $Text -Encoding utf8
    Write-Output "WROTE $Name length=$($Text.Length)"
}

$todo = Invoke-Plugin -Method 'workflow.todo.get' -ParamsYaml 'id: PLAN-TRIAGECLUSTER-001'
Save-Result '03-todo-PLAN-TRIAGECLUSTER-001.txt' ([string]$todo)

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
    $r = Invoke-Plugin -Method 'workflow.requirements.getFr' -ParamsYaml "id: $id"
    Save-Result ("10-fr-$id.txt") ([string]$r)
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
    $r = Invoke-Plugin -Method 'workflow.requirements.getTr' -ParamsYaml "id: $id"
    Save-Result ("11-tr-$id.txt") ([string]$r)
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
    $r = Invoke-Plugin -Method 'workflow.requirements.getTest' -ParamsYaml "id: $id"
    Save-Result ("12-test-$id.txt") ([string]$r)
}

foreach ($id in $frIds) {
    $r = Invoke-Plugin -Method 'workflow.requirements.listMappings' -ParamsYaml "frId: $id"
    Save-Result ("13b-map-$id.txt") ([string]$r)
}

$qImpl = Invoke-Plugin -Method 'client.SessionLog.QueryAsync' -ParamsYaml @"
agent: GrokCode
text: req-20260818T191655Z-004-s0-triagecluster-reqs
limit: 20
"@
Save-Result '20b-sessionlog-query-implementer-turn.txt' ([string]$qImpl)

$qSelf = Invoke-Plugin -Method 'client.SessionLog.QueryAsync' -ParamsYaml @"
agent: GrokCode
text: req-20260818T192456Z-prompt-33a0
limit: 5
"@
Save-Result '23b-sessionlog-query-hostile-hook-turn.txt' ([string]$qSelf)

Write-Output 'QUERY2 DONE'
