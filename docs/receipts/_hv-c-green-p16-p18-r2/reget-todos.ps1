#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p16-p18-r2'
$plugin = 'F:\GitHub\mcpserver-grok-plugin\lib\Invoke-McpPlugin.ps1'
$cacheRoot = Join-Path $out 'plugin-cache'
Set-Location -LiteralPath 'F:\GitHub\McpServer'
$env:MCP_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$env:MCPSERVER_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$env:GROK_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:MCP_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:PLUGIN_AGENT_NAME = 'GrokSubagentHostile'
$env:MCP_AGENT_NAME = 'GrokSubagentHostile'
$env:MCP_PLUGIN_HOST = 'grok'

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

Invoke-Plugin -Method 'client.Todo.GetAsync' -Params @{ id = 'PLAN-PLUGINHANDOFF-001' } -Name 'todo-plan-client-final'
Invoke-Plugin -Method 'client.Todo.GetAsync' -Params @{ id = 'MCP-PLUGININT-001' } -Name 'todo-pluginint-client-final'
Invoke-Plugin -Method 'workflow.todo.get' -Params @{ id = 'PLAN-PLUGINHANDOFF-001' } -Name 'todo-plan-final'
Invoke-Plugin -Method 'workflow.todo.get' -Params @{ id = 'MCP-PLUGININT-001' } -Name 'todo-pluginint-final'

$paths = @(
    'F:\GitHub\McpServer\build\Build.PluginSessionLogIntegration.cs',
    'F:\GitHub\McpServer\tests\Build.Tests\PluginSessionLogIntegrationTargetTests.cs'
)
$items = foreach ($p in $paths) {
    $i = Get-Item -LiteralPath $p
    [ordered]@{ Name = $i.Name; LastWriteTimeUtc = $i.LastWriteTimeUtc.ToString('o'); Length = $i.Length }
}
$items | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'file-timestamps-after-nuke-reread.json') -Encoding utf8

$trx = Get-ChildItem -LiteralPath $out -Recurse -Filter 'nuke-skip-reread.trx' | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
if ($trx) {
    [xml]$x = Get-Content -LiteralPath $trx.FullName
    $ns = New-Object System.Xml.XmlNamespaceManager($x.NameTable)
    $ns.AddNamespace('t', 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010')
    $c = $x.SelectSingleNode('//t:ResultSummary/t:Counters', $ns)
    $u = $x.SelectSingleNode('//t:UnitTestResult', $ns)
    [ordered]@{
        path = $trx.FullName
        total = [string]$c.total
        executed = [string]$c.executed
        passed = [string]$c.passed
        failed = [string]$c.failed
        skipped = [string]$c.skipped
        notExecuted = [string]$c.notExecuted
        testName = [string]$u.testName
        outcome = [string]$u.outcome
        duration = [string]$u.duration
    } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'nuke-reread-trx-summary.json') -Encoding utf8
}

Write-Output 'REGET_DONE'
