#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$out = 'F:\GitHub\McpServer\docs\receipts\_p19-green-bootstrap'
$plugin = 'F:\GitHub\mcpserver-grok-plugin\lib\Invoke-McpPlugin.ps1'
$cacheRoot = Join-Path $out 'plugin-cache'
New-Item -ItemType Directory -Force -Path $cacheRoot | Out-Null

$ids = Get-Content -LiteralPath (Join-Path $out 'ids.json') -Raw | ConvertFrom-Json
$agent = 'GrokCode'
$sessionId = [string]$ids.sessionId
$requestId = [string]$ids.requestId

$env:GROK_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:MCP_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:PLUGIN_AGENT_NAME = 'GrokCode'
$env:MCP_AGENT_NAME = 'GrokCode'
$env:MCP_WORKSPACE_PATH = 'F:\GitHub\McpServer'
Set-Location -LiteralPath 'F:\GitHub\McpServer'

function Invoke-Plugin {
    param([string]$Method, [hashtable]$Params, [string]$Name)
    $outFile = Join-Path $out ($Name + '.txt')
    $errFile = Join-Path $out ($Name + '.err.txt')
    try {
        $result = & $plugin -Command Invoke -Method $Method -ParamsObject $Params -WorkspacePath 'F:\GitHub\McpServer' -CacheRoot $cacheRoot -TimeoutSeconds 120 2> $errFile
        if ($null -eq $result) { $result = '' }
        if ($result -isnot [string]) { $result = ($result | Out-String) }
        Set-Content -LiteralPath $outFile -Value $result -Encoding utf8
        Write-Output ('OK ' + $Name)
    } catch {
        Set-Content -LiteralPath $outFile -Value ('ERROR ' + $_.Exception.ToString()) -Encoding utf8
        Write-Output ('FAIL ' + $Name + ' ' + $_.Exception.Message)
    }
}

Invoke-Plugin -Method 'client.SessionLog.OpenSessionAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    title = 'P19 complete native-suite green after C-green-P19 DISAGREE'
    model = 'grok'
} -Name 'sl-open-client'

Invoke-Plugin -Method 'client.SessionLog.BeginTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    queryTitle = 'P19 complete native-suite green after DISAGREE'
    queryText = 'Replace subset Pester receipt with a complete-run native suite receipt covering every tests/*.Tests.ps1 and Jest npm test, SyncAgentPlugins rebind, then PluginNativeSuiteReceiptTests and C-green-P19 hostile only.'
    model = 'grok'
    planFile = 'docs/plans/PLAN-PLUGINHANDOFF-001.md'
    todoId = 'PLAN-PLUGINHANDOFF-001'
} -Name 'sl-begin-client'

$now = [DateTime]::UtcNow.ToString('o')
Invoke-Plugin -Method 'client.SessionLog.AppendDialogAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    items = @(
        @{ timestamp = $now; role = 'model'; category = 'observation'; content = 'add-profile executed. 18 non-skill profile markdown files read. Plugin .version and plugin.json 1.104.0. Tool registry exact name mcpserver-grok-plugin present. commandTemplate git pull --ff-only exit 0 already up to date. Test-MarkerSignature true. Invoke-FullBootstrap true. PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 live get done false. P19 tasks remain done false.' }
        @{ timestamp = $now; role = 'model'; category = 'decision'; content = 'Decision: do not trust pluginint-p19-20260822T095842Z subset Pester. Produce a new complete-run receipt that discovers every tests/*.Tests.ps1 including Claude-code ReplFailsafe, HookTurnDedupe, CurrentTurnSessionRebind. Use Invoke-Pester -PassThru not -CI. After SyncAgentPlugins rerun. Do not mark TODOs done. Do not write P20 tests. Consequence: LoadLatest will use the new stamp after summary.json is written. Alternatives rejected: greening T095842Z; treating T102645Z as this agent''s independent run without re-executing.' }
    )
} -Name 'sl-dialog-start'

Invoke-Plugin -Method 'client.SessionLog.QueryAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
} -Name 'sl-query-sid'

Invoke-Plugin -Method 'workflow.sessionlog.queryHistory' -Params @{
    agent = $agent
    limit = 5
    offset = 0
} -Name 'sl-history-after-open'

Write-Output "SESSION=$sessionId"
Write-Output "REQUEST=$requestId"
