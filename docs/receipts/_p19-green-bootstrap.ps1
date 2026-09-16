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
$utc | Set-Content -LiteralPath (Join-Path $outDir 'utc.txt') -Encoding utf8

. (Join-Path $pluginRoot 'lib\marker-resolver.ps1')
$marker = Join-Path $workspace 'AGENTS-README-FIRST.yaml'
$sig = Test-MarkerSignature -MarkerFile $marker
$boot = Invoke-FullBootstrap -StartDir $workspace
[pscustomobject]@{
    utc = $utc
    pluginVersionDot = (Get-Content -LiteralPath (Join-Path $pluginRoot '.version') -Raw).Trim()
    pluginJson = (Get-Content -LiteralPath (Join-Path $pluginRoot '.grok-plugin\plugin.json') -Raw | ConvertFrom-Json).version
    testMarkerSignature = [bool]$sig
    invokeFullBootstrap = [bool]$boot
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $outDir 'trust.json') -Encoding utf8

if (-not $sig -or -not $boot) {
    'MCP_UNTRUSTED' | Set-Content -LiteralPath (Join-Path $outDir 'untrusted.txt') -Encoding utf8
    throw 'MCP_UNTRUSTED'
}

$apiKey = (Select-String -LiteralPath $marker -Pattern '^apiKey:\s*(.+)$').Matches[0].Groups[1].Value.Trim()
$baseUrl = (Select-String -LiteralPath $marker -Pattern '^baseUrl:\s*(.+)$').Matches[0].Groups[1].Value.Trim()
$headers = @{ 'X-Api-Key' = $apiKey }
$search = Invoke-RestMethod -Uri ($baseUrl + '/mcpserver/tools/search?keyword=mcpserver-grok-plugin') -Headers $headers -TimeoutSec 30
$search | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $outDir 'tool-search.json') -Encoding utf8

$items = @()
if ($search -is [System.Array]) { $items = @($search) }
elseif ($search.items) { $items = @($search.items) }
elseif ($search.tools) { $items = @($search.tools) }
elseif ($search.results) { $items = @($search.results) }
else { $items = @($search) }

$exact = $items | Where-Object {
    $n = $null
    if ($_.name) { $n = [string]$_.name }
    elseif ($_.Name) { $n = [string]$_.Name }
    elseif ($_.toolName) { $n = [string]$_.toolName }
    $n -eq 'mcpserver-grok-plugin'
} | Select-Object -First 1

if (-not $exact) {
    throw 'tool search exact name mcpserver-grok-plugin not found'
}

$exact | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $outDir 'tool-exact.json') -Encoding utf8

$template = $null
if ($exact.commandTemplate) { $template = [string]$exact.commandTemplate }
elseif ($exact.CommandTemplate) { $template = [string]$exact.CommandTemplate }

$template | Set-Content -LiteralPath (Join-Path $outDir 'commandTemplate.txt') -Encoding utf8
$targetParent = 'F:\GitHub'
if ($template) {
    $cmd = $template.Replace('{targetParent}', $targetParent).Replace('{{targetParent}}', $targetParent)
    $cmd | Set-Content -LiteralPath (Join-Path $outDir 'commandTemplate-rendered.txt') -Encoding utf8
    try {
        $templateOut = Invoke-Expression $cmd 2>&1 | ForEach-Object { "$_" }
        $templateOut | Set-Content -LiteralPath (Join-Path $outDir 'commandTemplate-out.txt') -Encoding utf8
        "TEMPLATE_EXIT=$LASTEXITCODE" | Set-Content -LiteralPath (Join-Path $outDir 'commandTemplate-exit.txt') -Encoding utf8
    } catch {
        $_ | Out-String | Set-Content -LiteralPath (Join-Path $outDir 'commandTemplate-error.txt') -Encoding utf8
    }
}

$invoke = Join-Path $pluginRoot 'lib\Invoke-McpPlugin.ps1'
function Invoke-Plugin([string]$Method, [object]$Params) {
    $paramsPath = Join-Path $outDir ('params-' + [guid]::NewGuid().ToString('N') + '.yaml')
    Import-Module (Join-Path $pluginRoot 'lib\McpPluginShim.psm1') -Force
    . (Join-Path $pluginRoot 'lib\yaml-object-mutation.ps1')
    Import-McpYamlSerializer
    $yaml = ConvertTo-Yaml -Data $Params -Options WithIndentedSequences
    Set-Content -LiteralPath $paramsPath -Value $yaml -Encoding utf8
    & $invoke -Command Invoke -Method $Method -ParamsPath $paramsPath -WorkspacePath $workspace -PluginRoot $pluginRoot -TimeoutSeconds 120
}

$status = & $invoke -Command Status -WorkspacePath $workspace -PluginRoot $pluginRoot -TimeoutSeconds 90
$status | Set-Content -LiteralPath (Join-Path $outDir 'status.txt') -Encoding utf8

$sessionId = "GrokCode-$utc-pluginhandoff-p19green"
$requestId = "req-$utc-001-p19-complete-native-suites"

$bootResult = Invoke-Plugin 'workflow.sessionlog.bootstrap' (@{})
$bootResult | Set-Content -LiteralPath (Join-Path $outDir 'sl-bootstrap.txt') -Encoding utf8

$openParams = [ordered]@{
    agent = 'GrokCode'
    sessionId = $sessionId
    title = 'P19 complete native-suite green after C-green-P19 DISAGREE'
    model = 'grok'
    sourceType = 'GrokCode'
}
$openResult = Invoke-Plugin 'workflow.sessionlog.openSession' $openParams
$openResult | Set-Content -LiteralPath (Join-Path $outDir 'sl-open.txt') -Encoding utf8

$beginParams = [ordered]@{
    requestId = $requestId
    queryTitle = 'P19 complete native-suite green after DISAGREE'
    queryText = 'Replace subset Pester receipt with a complete-run native suite receipt covering every tests/*.Tests.ps1 and Jest npm test, SyncAgentPlugins rebind, then PluginNativeSuiteReceiptTests and C-green-P19 hostile only.'
}
$beginResult = Invoke-Plugin 'workflow.sessionlog.beginTurn' $beginParams
$beginResult | Set-Content -LiteralPath (Join-Path $outDir 'sl-begin.txt') -Encoding utf8

$hist = Invoke-Plugin 'workflow.sessionlog.queryHistory' ([ordered]@{ agent = 'GrokCode'; limit = 5; offset = 0 })
$hist | Set-Content -LiteralPath (Join-Path $outDir 'sl-history.txt') -Encoding utf8

$todoPlan = Invoke-Plugin 'client.Todo.GetAsync' ([ordered]@{ id = 'PLAN-PLUGINHANDOFF-001' })
$todoPlan | Set-Content -LiteralPath (Join-Path $outDir 'todo-plan.txt') -Encoding utf8
$todoInt = Invoke-Plugin 'client.Todo.GetAsync' ([ordered]@{ id = 'MCP-PLUGININT-001' })
$todoInt | Set-Content -LiteralPath (Join-Path $outDir 'todo-pluginint.txt') -Encoding utf8

[pscustomobject]@{
    sessionId = $sessionId
    requestId = $requestId
    pluginRoot = $pluginRoot
    workspace = $workspace
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $outDir 'ids.json') -Encoding utf8

Write-Output "SESSION=$sessionId"
Write-Output "REQUEST=$requestId"
Write-Output "SIG=$sig"
Write-Output "BOOT=$boot"
Write-Output "EXACT=$($exact.name)"
