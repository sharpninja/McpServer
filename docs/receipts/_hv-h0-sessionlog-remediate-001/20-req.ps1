#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'
$pluginRoot = 'F:\GitHub\mcpserver-grok-plugin'
$workspace = 'F:\GitHub\McpServer'
$outDir = Join-Path $workspace 'docs\receipts\_hv-h0-sessionlog-remediate-001'
$env:MCP_PLUGIN_ROOT = $pluginRoot
$env:GROK_PLUGIN_ROOT = $pluginRoot
$env:PLUGIN_AGENT_NAME = 'GrokCode'
$env:MCP_AGENT_NAME = 'GrokCode'
$env:MCP_WORKSPACE_PATH = $workspace
Set-Location -LiteralPath $workspace
$invoke = Join-Path $pluginRoot 'lib\Invoke-McpPlugin.ps1'
$method = $args[0]
$id = $args[1]
if ([string]::IsNullOrWhiteSpace($method) -or [string]::IsNullOrWhiteSpace($id)) { throw 'method and id required' }
$safe = $id -replace '[^A-Za-z0-9-]', '_'
$prefix = if ($method -match 'getFr') { '20-fr' } elseif ($method -match 'getTr') { '21-tr' } elseif ($method -match 'getTest') { '22-test' } elseif ($method -match 'listMappings') { '23-map' } else { '20-misc' }
$params = if ($method -match 'listMappings') { @{ frId = $id } } else { @{ id = $id } }
try {
    $output = & $invoke -Command Invoke -Method $method -ParamsObject $params -WorkspacePath $workspace -TimeoutSeconds 60 2>&1 | Out-String
} catch {
    $output = "INVOKE_EXCEPTION: $($_.Exception.Message)"
}
Set-Content -LiteralPath (Join-Path $outDir "$prefix-$safe.txt") -Value $output -Encoding utf8
Write-Output ("DONE method=" + $method + " id=" + $id + " chars=" + $output.Length)
