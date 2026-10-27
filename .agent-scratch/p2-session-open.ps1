$ErrorActionPreference = 'Stop'
$env:MCP_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:GROK_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:PLUGIN_AGENT_NAME = 'GrokCode'
$env:MCP_AGENT_NAME = 'GrokCode'
$env:MCP_PLUGIN_HOST = 'grok'
$env:MCP_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$env:GROK_WORKSPACE_PATH = 'F:\GitHub\McpServer'

Set-Location -LiteralPath 'F:\GitHub\McpServer'
. 'F:\GitHub\mcpserver-grok-plugin\lib\marker-resolver.ps1'
$bootstrapOk = Invoke-FullBootstrap -StartDir 'F:\GitHub\McpServer'
Write-Output ('FULL_BOOTSTRAP=' + $bootstrapOk)
if (-not $bootstrapOk) { exit 2 }

$repl = 'F:\GitHub\mcpserver-grok-plugin\lib\repl-invoke.ps1'
$utc = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
$script:SessionId = "GrokCode-$utc-sessionlife-p2-unit-hv"
$script:TurnRequestId = "req-$utc-sessionlife-p2-unit-hv"
Write-Output ('SESSION_ID=' + $script:SessionId)
Write-Output ('TURN_REQUEST_ID=' + $script:TurnRequestId)

function Invoke-PluginMethod {
    param(
        [Parameter(Mandatory)][string]$Method,
        [string]$ParamsYaml = ''
    )
    Write-Output ('--- METHOD ' + $Method + ' ---')
    & pwsh.exe -NoProfile -NonInteractive -File $repl -Method $Method -ParamsYaml $ParamsYaml
    Write-Output ('METHOD_EXIT=' + $LASTEXITCODE)
}

Invoke-PluginMethod -Method 'workflow.sessionlog.bootstrap' -ParamsYaml ''

$openYaml = @"
agent: GrokCode
sessionId: $($script:SessionId)
title: SessionLife P2 unit gate and one Astra HV on PR 72 tip
model: grok-4.7
"@
Invoke-PluginMethod -Method 'workflow.sessionlog.openSession' -ParamsYaml $openYaml

$beginYaml = @"
requestId: $($script:TurnRequestId)
queryTitle: SessionLife P2 unit gate and one Astra HV
queryText: Run the SessionLife P2 unit gate and one Astra HV round on PR 72 tip 987cd8f39610c9fa301862e3b1d526d23e049469. Do not merge. Do not remediate HV FAILs. Do not bulk-close SessionLife TODOs.
"@
Invoke-PluginMethod -Method 'workflow.sessionlog.beginTurn' -ParamsYaml $beginYaml

$historyYaml = @"
agent: GrokCode
limit: 5
offset: 0
"@
Invoke-PluginMethod -Method 'workflow.sessionlog.queryHistory' -ParamsYaml $historyYaml

$statePath = Join-Path $env:USERPROFILE '.grok\p2-session-state.json'
$state = [ordered]@{
    sessionId = $script:SessionId
    turnRequestId = $script:TurnRequestId
    utc = $utc
    head = '987cd8f39610c9fa301862e3b1d526d23e049469'
}
$state | ConvertTo-Json | Set-Content -LiteralPath $statePath -Encoding utf8
Write-Output ('STATE_PATH=' + $statePath)
