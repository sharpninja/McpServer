#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$pluginRoot = 'F:\GitHub\mcpserver-grok-plugin'
$workspace = 'F:\GitHub\McpServer'
$outDir = Join-Path $workspace 'docs\receipts\_p19-green-bootstrap'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$env:GROK_PLUGIN_ROOT = $pluginRoot
$env:PLUGIN_AGENT_NAME = 'GrokCode'
$env:MCP_PLUGIN_ROOT = $pluginRoot
$env:MCP_WORKSPACE_PATH = $workspace
$env:MCPSERVER_WORKSPACE_PATH = $workspace
Set-Location -LiteralPath $workspace

$utc = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
$sessionId = "GrokCode-$utc-pluginhandoff-p19green"
$requestId = "req-$utc-001-p19-complete-native-suites"

# Execute exact tool commandTemplate with targetParent=F:\GitHub
$parent = 'F:\GitHub'
$name = 'mcpserver-grok-plugin'
$repo = 'https://github.com/sharpninja/mcpserver-grok-plugin.git'
New-Item -ItemType Directory -Force -Path $parent | Out-Null
$path = Join-Path $parent $name
$templateLog = Join-Path $outDir 'commandTemplate-out.txt'
try {
    if (Test-Path -LiteralPath (Join-Path $path '.git')) {
        $pull = & git -C $path pull --ff-only 2>&1 | ForEach-Object { "$_" }
        $pull | Set-Content -LiteralPath $templateLog -Encoding utf8
        "TEMPLATE_EXIT=$LASTEXITCODE action=pull-ff-only" | Set-Content -LiteralPath (Join-Path $outDir 'commandTemplate-exit.txt') -Encoding utf8
    } elseif (Test-Path -LiteralPath $path) {
        throw ($path + ' exists but is not a git repository')
    } else {
        $clone = & git clone $repo $path 2>&1 | ForEach-Object { "$_" }
        $clone | Set-Content -LiteralPath $templateLog -Encoding utf8
        "TEMPLATE_EXIT=$LASTEXITCODE action=clone" | Set-Content -LiteralPath (Join-Path $outDir 'commandTemplate-exit.txt') -Encoding utf8
    }
} catch {
    ($_ | Out-String) | Set-Content -LiteralPath (Join-Path $outDir 'commandTemplate-error.txt') -Encoding utf8
}

$invoke = Join-Path $pluginRoot 'lib\Invoke-McpPlugin.ps1'
function Invoke-Plugin {
    param(
        [Parameter(Mandatory)][string]$Method,
        [Parameter(Mandatory)][hashtable]$Params,
        [Parameter(Mandatory)][string]$OutName
    )
    $paramsPath = Join-Path $outDir ($OutName + '-params.yaml')
    . (Join-Path $pluginRoot 'lib\yaml-object-mutation.ps1')
    Import-McpYamlSerializer
    $yaml = ConvertTo-Yaml -Data $Params -Options WithIndentedSequences
    Set-Content -LiteralPath $paramsPath -Value $yaml -Encoding utf8
    $result = & $invoke -Command Invoke -Method $Method -ParamsPath $paramsPath -WorkspacePath $workspace -PluginRoot $pluginRoot -TimeoutSeconds 180
    $text = if ($null -eq $result) { '' } else { ($result | Out-String) }
    Set-Content -LiteralPath (Join-Path $outDir ($OutName + '.txt')) -Value $text -Encoding utf8
    return $text
}

$status = & $invoke -Command Status -WorkspacePath $workspace -PluginRoot $pluginRoot -TimeoutSeconds 90
($status | Out-String) | Set-Content -LiteralPath (Join-Path $outDir 'status.txt') -Encoding utf8

$null = Invoke-Plugin -Method 'workflow.sessionlog.bootstrap' -Params @{} -OutName 'sl-bootstrap'

$open = @{
    agent = 'GrokCode'
    sessionId = $sessionId
    title = 'P19 complete native-suite green after C-green-P19 DISAGREE'
    model = 'grok'
    sourceType = 'GrokCode'
}
$null = Invoke-Plugin -Method 'workflow.sessionlog.openSession' -Params $open -OutName 'sl-open'

$begin = @{
    requestId = $requestId
    queryTitle = 'P19 complete native-suite green after DISAGREE'
    queryText = 'Replace subset Pester receipt with a complete-run native suite receipt covering every tests/*.Tests.ps1 and Jest npm test, SyncAgentPlugins rebind, then PluginNativeSuiteReceiptTests and C-green-P19 hostile only.'
}
$null = Invoke-Plugin -Method 'workflow.sessionlog.beginTurn' -Params $begin -OutName 'sl-begin'

$hist = @{ agent = 'GrokCode'; limit = 5; offset = 0 }
$null = Invoke-Plugin -Method 'workflow.sessionlog.queryHistory' -Params $hist -OutName 'sl-history'

$null = Invoke-Plugin -Method 'client.Todo.GetAsync' -Params @{ id = 'PLAN-PLUGINHANDOFF-001' } -OutName 'todo-plan'
$null = Invoke-Plugin -Method 'client.Todo.GetAsync' -Params @{ id = 'MCP-PLUGININT-001' } -OutName 'todo-pluginint'

[pscustomobject]@{
    utc = $utc
    sessionId = $sessionId
    requestId = $requestId
    pluginRoot = $pluginRoot
    workspace = $workspace
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $outDir 'ids.json') -Encoding utf8

Write-Output "SESSION=$sessionId"
Write-Output "REQUEST=$requestId"
