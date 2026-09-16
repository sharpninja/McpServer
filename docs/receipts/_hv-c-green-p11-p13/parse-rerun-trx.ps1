#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p11-p13'
$trxPath = Join-Path $out 'dotnet-adapter-filter-rerun.trx'
$dest = Join-Path $out 'filter-rerun-trx-summary.json'

function Get-Attr {
    param($node, [string]$name)
    if ($null -eq $node) { return $null }
    $a = $node.Attributes[$name]
    if ($null -eq $a) { return $null }
    return [string]$a.Value
}

if (-not (Test-Path -LiteralPath $trxPath)) {
    [ordered]@{ exists = $false; path = $trxPath } | ConvertTo-Json | Set-Content -LiteralPath $dest -Encoding utf8
    Write-Output 'RERUN_TRX_MISSING'
    exit 1
}

[xml]$x = Get-Content -LiteralPath $trxPath
$ns = New-Object System.Xml.XmlNamespaceManager($x.NameTable)
$ns.AddNamespace('t', 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010')
$summary = $x.SelectSingleNode('//t:ResultSummary', $ns)
$c = $x.SelectSingleNode('//t:ResultSummary/t:Counters', $ns)
$results = $x.SelectNodes('//t:UnitTestResult', $ns)
$outcomes = @()
foreach ($u in @($results)) {
    $outcomes += [ordered]@{
        name = Get-Attr $u 'testName'
        outcome = Get-Attr $u 'outcome'
        duration = Get-Attr $u 'duration'
    }
}
$p11 = @($outcomes | Where-Object { $_.name -match 'Theory_EachScenario_FailsUntilAdapterOperational' })
$p12 = @($outcomes | Where-Object { $_.name -match 'BootstrapBeginAppendComplete_CapturesIdsCacheShaReceipt' })
$p13 = @($outcomes | Where-Object { $_.name -match 'ServerQuery_SourceTypeIdsActionDialogCompletedWorkspace' })
$p14 = @($outcomes | Where-Object { $_.name -match 'PluginRootOverride_WritesOnlyWorkspaceMcpServerAgent_PoisonEmpty' })
$hostKinds = @('Codex', 'ClaudeCode', 'ClaudeCowork', 'Copilot', 'Grok', 'ClineV2', 'Cline', 'OpenCode')
function Host-Rows($rows) {
    foreach ($h in $hostKinds) {
        $pat = if ($h -eq 'Cline') { 'hostKind: Cline\)' } else { [regex]::Escape($h) }
        $matched = @($rows | Where-Object { $_.name -match $pat })
        [ordered]@{ HostKind = $h; Count = $matched.Count; Passed = @($matched | Where-Object { $_.outcome -eq 'Passed' }).Count; Failed = @($matched | Where-Object { $_.outcome -eq 'Failed' }).Count }
    }
}
[ordered]@{
    exists = $true
    path = $trxPath
    outcome = Get-Attr $summary 'outcome'
    total = Get-Attr $c 'total'
    executed = Get-Attr $c 'executed'
    passed = Get-Attr $c 'passed'
    failed = Get-Attr $c 'failed'
    skipped = Get-Attr $c 'skipped'
    notExecuted = Get-Attr $c 'notExecuted'
    unitCount = @($results).Count
    failedNames = @($outcomes | Where-Object { $_.outcome -eq 'Failed' } | ForEach-Object { $_.name })
    skippedNames = @($outcomes | Where-Object { $_.outcome -in @('NotExecuted','Skipped','Inconclusive') } | ForEach-Object { $_.name })
    p11Count = $p11.Count
    p11Passed = @($p11 | Where-Object { $_.outcome -eq 'Passed' }).Count
    p12Count = $p12.Count
    p12Passed = @($p12 | Where-Object { $_.outcome -eq 'Passed' }).Count
    p13Count = $p13.Count
    p13Passed = @($p13 | Where-Object { $_.outcome -eq 'Passed' }).Count
    p14Count = $p14.Count
    p11ByHost = @(Host-Rows $p11)
    p12ByHost = @(Host-Rows $p12)
    p13ByHost = @(Host-Rows $p13)
} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $dest -Encoding utf8
Write-Output 'RERUN_PARSE_DONE'
