#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$pluginRoot = 'F:\GitHub\mcpserver-grok-plugin'
$workspace = 'F:\GitHub\McpServer'
$invoke = Join-Path $pluginRoot 'lib\Invoke-McpPlugin.ps1'
$outDir = 'F:\GitHub\McpServer\docs\receipts'

$env:GROK_PLUGIN_ROOT = $pluginRoot
$env:MCP_PLUGIN_ROOT = $pluginRoot
$env:PLUGIN_AGENT_NAME = 'GrokCode'
$env:MCP_AGENT_NAME = 'GrokCode'
$env:MCP_WORKSPACE_PATH = $workspace
Set-Location $workspace

$utc = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
$sessionId = "GrokCode-$utc-hostile-wrapup"
$reqId = "req-$utc-001-hostile-wrap-up-review"

$idsPath = Join-Path $outDir '_hv-wrapup-ids-20260818.txt'
@(
    "UTC=$utc"
    "SESSION_ID=$sessionId"
    "REQUEST_ID=$reqId"
) | Set-Content -LiteralPath $idsPath -Encoding utf8

function Invoke-Plugin {
    param(
        [Parameter(Mandatory)][string]$Method,
        [Parameter(Mandatory)][hashtable]$Params,
        [string]$Tag
    )
    $tagSafe = if ($Tag) { $Tag } else { ($Method -replace '[^\w\.-]', '_') }
    $outPath = Join-Path $outDir "_hv-wrapup-$tagSafe.json"
    Write-Output "INVOKE $Method TAG=$tagSafe"
    & $invoke -Command Invoke -Method $Method -ParamsObject $Params -WorkspacePath $workspace -PluginRoot $pluginRoot -TimeoutSeconds 180 |
        Tee-Object -FilePath $outPath
    Write-Output "WROTE $outPath EXIT=$LASTEXITCODE"
}

# bootstrap
Invoke-Plugin -Method 'workflow.sessionlog.bootstrap' -Params @{} -Tag 'bootstrap'

# openSession
Invoke-Plugin -Method 'workflow.sessionlog.openSession' -Params @{
    agent     = 'GrokCode'
    sessionId = $sessionId
    title     = 'Hostile validation of refresh-docs wrap-up push'
    model     = 'grok-code'
} -Tag 'open'

# beginTurn - requestId in params is the turn request id
Invoke-Plugin -Method 'workflow.sessionlog.beginTurn' -Params @{
    requestId  = $reqId
    queryTitle = 'Hostile validate wrap-up refresh-docs push'
    queryText  = 'Adversarial review of wrap-up-20260818T183800Z claims: refresh-docs, generateDocument wiki, remotes, wiki publish, unit suite, TODOs not marked done.'
} -Tag 'begin'

Write-Output "SESSION_ID=$sessionId"
Write-Output "REQUEST_ID=$reqId"
