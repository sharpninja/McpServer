#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p5'

function Get-Attr {
    param($node, [string]$name)
    if ($null -eq $node) { return $null }
    $a = $node.Attributes[$name]
    if ($null -eq $a) { return $null }
    return [string]$a.Value
}

function Get-TrxSummary {
    param([string]$TrxPath, [string]$Name)
    $dest = Join-Path $out ($Name + '-trx-summary.json')
    if (-not (Test-Path -LiteralPath $TrxPath)) {
        [ordered]@{ exists = $false; path = $TrxPath } | ConvertTo-Json | Set-Content -LiteralPath $dest -Encoding utf8
        return
    }
    [xml]$x = Get-Content -LiteralPath $TrxPath
    $ns = New-Object System.Xml.XmlNamespaceManager($x.NameTable)
    $ns.AddNamespace('t', 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010')
    $summary = $x.SelectSingleNode('//t:ResultSummary', $ns)
    $c = $x.SelectSingleNode('//t:ResultSummary/t:Counters', $ns)
    $results = $x.SelectNodes('//t:UnitTestResult', $ns)
    $outcomes = @()
    foreach ($u in @($results)) {
        $msgNode = $u.SelectSingleNode('t:Output/t:ErrorInfo/t:Message', $ns)
        $outcomes += [ordered]@{
            name = Get-Attr $u 'testName'
            outcome = Get-Attr $u 'outcome'
            duration = Get-Attr $u 'duration'
            message = $(if ($msgNode) { [string]$msgNode.InnerText } else { $null })
        }
    }
    [ordered]@{
        exists = $true
        path = $TrxPath
        outcome = Get-Attr $summary 'outcome'
        total = Get-Attr $c 'total'
        executed = Get-Attr $c 'executed'
        passed = Get-Attr $c 'passed'
        failed = Get-Attr $c 'failed'
        skipped = Get-Attr $c 'skipped'
        notExecuted = Get-Attr $c 'notExecuted'
        inconclusive = Get-Attr $c 'inconclusive'
        unitCount = @($results).Count
        failedNames = @($outcomes | Where-Object { $_.outcome -eq 'Failed' } | ForEach-Object { $_.name })
        passedNames = @($outcomes | Where-Object { $_.outcome -eq 'Passed' } | ForEach-Object { $_.name })
        skippedNames = @($outcomes | Where-Object { $_.outcome -in @('NotExecuted','Skipped','Inconclusive') } | ForEach-Object { $_.name })
        unitOutcomes = $outcomes
    } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $dest -Encoding utf8
}

Get-TrxSummary -TrxPath (Join-Path $out 'dotnet-fixture-filter.trx') -Name 'filter'
Get-TrxSummary -TrxPath (Join-Path $out 'dotnet-pluginintegration-all.trx') -Name 'all'

git -C 'F:\GitHub\McpServer' log --format='%H %cI %s' -n 5 -- tests/McpServer.PluginIntegration.Tests/PluginIntegrationServerFixture.cs | Out-File -FilePath (Join-Path $out 'git-log-fixture-cs.txt') -Encoding utf8
git -C 'F:\GitHub\McpServer' log --format='%H %cI %s' -n 5 -- tests/McpServer.PluginIntegration.Tests/PluginIntegrationServerFixtureTests.cs | Out-File -FilePath (Join-Path $out 'git-log-fixture-tests.txt') -Encoding utf8

$plugin = 'F:\GitHub\mcpserver-grok-plugin\lib\Invoke-McpPlugin.ps1'
$env:MCP_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$env:GROK_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:PLUGIN_AGENT_NAME = 'GrokSubagentHostile'
try {
    $map = & $plugin -Command Invoke -Method 'workflow.requirements.listMappings' -ParamsObject @{ frId = 'FR-MCP-PLUGININT-001' } -WorkspacePath 'F:\GitHub\McpServer' -TimeoutSeconds 120
    Set-Content -LiteralPath (Join-Path $out 'req-map-pluginint-001.txt') -Value ([string]$map) -Encoding utf8
    Write-Output 'OK req-map'
} catch {
    Set-Content -LiteralPath (Join-Path $out 'req-map-pluginint-001.txt') -Value ('ERROR ' + $_.Exception.ToString()) -Encoding utf8
    Write-Output 'FAIL req-map'
}

$verPath = 'F:\GitHub\mcpserver-grok-plugin\.version'
$pjPath = 'F:\GitHub\mcpserver-grok-plugin\.grok-plugin\plugin.json'
$verObj = [ordered]@{
    VersionFile = if (Test-Path $verPath) { (Get-Content -LiteralPath $verPath -Raw).Trim() } else { $null }
    PluginJsonVersion = $null
}
if (Test-Path $pjPath) {
    $pj = Get-Content -LiteralPath $pjPath -Raw | ConvertFrom-Json
    $verObj.PluginJsonVersion = $pj.version
}
$verObj | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'plugin-version.json') -Encoding utf8

Write-Output 'PARSE_DONE'
