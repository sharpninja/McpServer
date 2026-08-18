#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$plugin = 'F:\GitHub\mcpserver-grok-plugin\lib\Invoke-McpPlugin.ps1'
$pluginRoot = 'F:\GitHub\mcpserver-grok-plugin'
$workspace = 'F:\GitHub\McpServer'

function Invoke-Hv {
    param([string]$Method, $Params)
    Write-Output ("==== {0} ====" -f $Method)
    try {
        & $plugin -Command Invoke -Method $Method -ParamsObject $Params -WorkspacePath $workspace -PluginRoot $pluginRoot -TimeoutSeconds 120
        Write-Output ("EXIT_OK {0}" -f $Method)
    } catch {
        Write-Output ("EXIT_FAIL {0}: {1}" -f $Method, $_.Exception.Message)
    }
}

Invoke-Hv -Method 'client.SessionLog.QueryAsync' -Params @{
    agent = 'GrokCode'
    text = 'GrokCode-20260817T232250Z-hostile-effort'
    limit = 5
}

Invoke-Hv -Method 'client.SessionLog.QueryAsync' -Params @{
    agent = 'GrokCode'
    text = 'Hostile validate Agent Help effort-high claims'
    limit = 5
}

Invoke-Hv -Method 'client.SessionLog.QueryAsync' -Params @{
    agent = 'GrokCode'
    text = 'req-20260817T232250Z-001-hostile-validate-effort'
    limit = 5
}
