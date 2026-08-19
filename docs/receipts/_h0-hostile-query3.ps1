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

function Invoke-PluginSafe {
    param(
        [Parameter(Mandatory)][string]$Method,
        [string]$ParamsYaml = '',
        [Parameter(Mandatory)][string]$OutName
    )
    $text = ''
    try {
        if ([string]::IsNullOrWhiteSpace($ParamsYaml)) {
            $text = [string](& $invoke -Command Invoke -Method $Method -WorkspacePath $workspace)
        } else {
            $text = [string](& $invoke -Command Invoke -Method $Method -Params $ParamsYaml -WorkspacePath $workspace)
        }
    } catch {
        $text = [string]$_.Exception.Message
        if ($_.ErrorDetails -and $_.ErrorDetails.Message) {
            $text = [string]$_.ErrorDetails.Message
        }
        if (-not $text) { $text = [string]$_ }
    }
    Set-Content -LiteralPath (Join-Path $outDir $OutName) -Value $text -Encoding utf8
    Write-Output "WROTE $OutName length=$($text.Length) preview=$($text.Substring(0, [Math]::Min(180, $text.Length)).Replace("`n",' '))"
}

$restTests = @(
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
foreach ($id in $restTests) {
    Invoke-PluginSafe -Method 'workflow.requirements.getTest' -ParamsYaml "id: $id" -OutName "12-test-$id.txt"
}

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
    Invoke-PluginSafe -Method 'workflow.requirements.listMappings' -ParamsYaml "frId: $id" -OutName "13b-map-$id.txt"
}

Invoke-PluginSafe -Method 'client.SessionLog.QueryAsync' -ParamsYaml @"
agent: GrokCode
text: req-20260818T191655Z-004-s0-triagecluster-reqs
limit: 20
"@ -OutName '20b-sessionlog-query-implementer-turn.txt'

Invoke-PluginSafe -Method 'client.SessionLog.QueryAsync' -ParamsYaml @"
agent: GrokCode
text: s0-triagecluster-reqs
limit: 20
"@ -OutName '20c-sessionlog-query-s0-slug.txt'

Write-Output 'QUERY3 DONE'
