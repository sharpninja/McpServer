#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p1'
New-Item -ItemType Directory -Force -Path $out | Out-Null
Set-Location -LiteralPath 'F:\GitHub\McpServer'

$env:MCP_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$env:MCPSERVER_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$env:GROK_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$env:PLUGIN_AGENT_NAME = 'GrokSubagentHostile'
$env:MCP_AGENT_NAME = 'GrokSubagentHostile'
$env:GROK_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:MCP_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'

$plugin = 'F:\GitHub\mcpserver-grok-plugin\lib\Invoke-McpPlugin.ps1'

function Invoke-Plugin {
    param([string]$Method, [hashtable]$Params, [string]$Name)
    $outFile = Join-Path $out ($Name + '.txt')
    $errFile = Join-Path $out ($Name + '.err.txt')
    try {
        $result = & $plugin -Command Invoke -Method $Method -ParamsObject $Params -WorkspacePath 'F:\GitHub\McpServer' -TimeoutSeconds 120 2> $errFile
        if ($null -eq $result) { $result = '' }
        if ($result -isnot [string]) { $result = ($result | Out-String) }
        Set-Content -LiteralPath $outFile -Value $result -Encoding utf8
        Write-Output ('OK ' + $Name)
    } catch {
        Set-Content -LiteralPath $outFile -Value ('ERROR ' + $_.Exception.ToString()) -Encoding utf8
        Write-Output ('FAIL ' + $Name + ' ' + $_.Exception.Message)
    }
}

# Receipt file existence
$receipts = Get-ChildItem -LiteralPath 'F:\GitHub\McpServer\docs\receipts' -Filter 'hostile-validator-20260821*.md' -File -ErrorAction SilentlyContinue |
    Select-Object Name, FullName, LastWriteTimeUtc, Length
$receipts | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $out 'receipt-glob.json') -Encoding utf8
Write-Output ('RECEIPT_COUNT=' + @($receipts).Count)

# Grep sln/build for PluginIntegration and PluginSessionLogIntegration
$slnHits = Select-String -Path 'F:\GitHub\McpServer\McpServer.sln' -Pattern 'PluginIntegration|PluginSessionLogIntegration' -ErrorAction SilentlyContinue
$buildHits = @()
Get-ChildItem -LiteralPath 'F:\GitHub\McpServer\build' -Recurse -Include *.cs,*.csx,*.json -ErrorAction SilentlyContinue |
    ForEach-Object {
        $m = Select-String -Path $_.FullName -Pattern 'PluginIntegration|PluginSessionLogIntegration' -ErrorAction SilentlyContinue
        if ($m) { $buildHits += $m }
    }
$rootBuildHits = Select-String -Path 'F:\GitHub\McpServer\build.ps1','F:\GitHub\McpServer\.nuke\build.schema.json' -Pattern 'PluginIntegration|PluginSessionLogIntegration' -ErrorAction SilentlyContinue
$grepText = @()
$grepText += '=== sln ==='
if ($slnHits) { $grepText += ($slnHits | ForEach-Object { $_.ToString() }) } else { $grepText += '(no matches)' }
$grepText += '=== build ==='
if ($buildHits) { $grepText += ($buildHits | ForEach-Object { $_.ToString() }) } else { $grepText += '(no matches in build/ recurse)' }
$grepText += '=== root build files ==='
if ($rootBuildHits) { $grepText += ($rootBuildHits | ForEach-Object { $_.ToString() }) } else { $grepText += '(no matches)' }
Set-Content -LiteralPath (Join-Path $out 'grep-pluginintegration.txt') -Value ($grepText -join "`n") -Encoding utf8

# Project / catalog existence
$proj = 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests'
$catalog = 'F:\GitHub\McpServer\scenarios\plugin-sessionlog-scenarios.json'
$exists = [ordered]@{
    pluginIntegrationDirExists = Test-Path -LiteralPath $proj
    pluginIntegrationCsprojExists = Test-Path -LiteralPath (Join-Path $proj 'McpServer.PluginIntegration.Tests.csproj')
    catalogExists = Test-Path -LiteralPath $catalog
    testFileExists = Test-Path -LiteralPath 'F:\GitHub\McpServer\tests\Build.Tests\PluginSessionLogIntegrationTargetTests.cs'
}
$exists | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'existence.json') -Encoding utf8
Write-Output ($exists | ConvertTo-Json -Compress)

# MCP TODOs
foreach ($id in @('MCP-PLUGINCORE-004','PLAN-PLUGINHANDOFF-001','MCP-PLUGININT-001','MCP-WORKSPACEHYGIENE-002')) {
    $name = 'todo-' + ($id.ToLowerInvariant())
    Invoke-Plugin -Method 'workflow.todo.get' -Params @{ id = $id } -Name $name
}

# Requirements
foreach ($pair in @(
    @{ Method = 'workflow.requirements.getFr'; Params = @{ id = 'FR-MCP-PLUGININT-001' }; Name = 'req-fr-pluginint-001' },
    @{ Method = 'workflow.requirements.getTr'; Params = @{ id = 'TR-MCP-PLUGININT-001' }; Name = 'req-tr-pluginint-001' },
    @{ Method = 'workflow.requirements.getTest'; Params = @{ id = 'TEST-MCP-PLUGININT-001' }; Name = 'req-test-pluginint-001' }
)) {
    Invoke-Plugin -Method $pair.Method -Params $pair.Params -Name $pair.Name
}

Write-Output 'COLLECT_DONE'
