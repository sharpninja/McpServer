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

function Invoke-One {
    param([string]$Method, [hashtable]$Params, [string]$OutName)
    try {
        $output = & $invoke -Command Invoke -Method $Method -ParamsObject $Params -WorkspacePath $workspace -TimeoutSeconds 60 2>&1 | Out-String
    } catch {
        $output = "INVOKE_EXCEPTION: $($_.Exception.Message)"
    }
    Set-Content -LiteralPath (Join-Path $outDir $OutName) -Value $output -Encoding utf8
    Write-Output ("DONE " + $OutName + " chars=" + $output.Length)
}

Invoke-One workflow.requirements.getFr @{ id = 'FR-MCP-170' } '20-fr-FR-MCP-170.txt'
Invoke-One workflow.requirements.getFr @{ id = 'FR-MCP-171' } '20-fr-FR-MCP-171.txt'
Invoke-One workflow.requirements.getFr @{ id = 'FR-MCP-172' } '20-fr-FR-MCP-172.txt'
Invoke-One workflow.requirements.getTr @{ id = 'TR-MCP-PERSIST-001' } '21-tr-TR-MCP-PERSIST-001.txt'
Invoke-One workflow.requirements.getTr @{ id = 'TR-MCP-PERSIST-002' } '21-tr-TR-MCP-PERSIST-002.txt'
Invoke-One workflow.requirements.getTr @{ id = 'TR-MCP-PERSIST-003' } '21-tr-TR-MCP-PERSIST-003.txt'
Invoke-One workflow.requirements.getTr @{ id = 'TR-MCP-PERSIST-004' } '21-tr-TR-MCP-PERSIST-004.txt'
Invoke-One workflow.requirements.getTest @{ id = 'TEST-MCP-195' } '22-test-TEST-MCP-195.txt'
Invoke-One workflow.requirements.getTest @{ id = 'TEST-MCP-196' } '22-test-TEST-MCP-196.txt'
Invoke-One workflow.requirements.listMappings @{ frId = 'FR-MCP-170' } '23-map-FR-MCP-170.txt'
Invoke-One workflow.requirements.listMappings @{ frId = 'FR-MCP-171' } '23-map-FR-MCP-171.txt'
Invoke-One workflow.requirements.listMappings @{ frId = 'FR-MCP-172' } '23-map-FR-MCP-172.txt'
