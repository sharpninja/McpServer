#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p16-p18-r2'

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
    $p16 = @($outcomes | Where-Object { $_.name -match 'AiTheory_Agent_RequiresValidJsonFields' })
    $p17 = @($outcomes | Where-Object { $_.name -match 'AiTheory_RejectsInvalidJson_DoesNotOverrideDeterministicFailure' })
    $p18 = @($outcomes | Where-Object { $_.name -match 'NukeTarget_SkipIsFailure' })
    $p19 = @($outcomes | Where-Object { $_.name -match 'PluginNativeSuite_|PluginInt_P19_' })
    $ai = @($outcomes | Where-Object { $_.name -match 'PluginSessionLogAiTheoryTests|AiTheory_' })
    $failedLine = @($outcomes | Where-Object { $_.outcome -eq 'Failed' })
    $skippedLine = @($outcomes | Where-Object { $_.outcome -in @('NotExecuted','Skipped','Inconclusive') })
    $hostKinds = @('Codex', 'ClaudeCode', 'ClaudeCowork', 'Copilot', 'Grok', 'Cline', 'ClineV2', 'OpenCode')
    $p16ByHost = foreach ($h in $hostKinds) {
        $matched = @($p16 | Where-Object { $_.name -match [regex]::Escape($h) })
        [ordered]@{
            HostKind = $h
            Count = @($matched).Count
            Failed = @($matched | Where-Object { $_.outcome -eq 'Failed' }).Count
            Passed = @($matched | Where-Object { $_.outcome -eq 'Passed' }).Count
            Skipped = @($matched | Where-Object { $_.outcome -in @('NotExecuted', 'Skipped', 'Inconclusive') }).Count
        }
    }
    [ordered]@{
        exists = $true
        path = $TrxPath
        lastWriteTimeUtc = (Get-Item -LiteralPath $TrxPath).LastWriteTimeUtc.ToString('o')
        length = (Get-Item -LiteralPath $TrxPath).Length
        outcome = Get-Attr $summary 'outcome'
        total = Get-Attr $c 'total'
        executed = Get-Attr $c 'executed'
        passed = Get-Attr $c 'passed'
        failed = Get-Attr $c 'failed'
        skipped = Get-Attr $c 'skipped'
        notExecuted = Get-Attr $c 'notExecuted'
        inconclusive = Get-Attr $c 'inconclusive'
        unitCount = @($results).Count
        failedNames = @($failedLine | ForEach-Object { $_.name })
        passedCountNames = @($outcomes | Where-Object { $_.outcome -eq 'Passed' }).Count
        skippedNames = @($skippedLine | ForEach-Object { $_.name })
        failedMessages = @($failedLine | ForEach-Object { [ordered]@{ name = $_.name; message = $_.message } })
        p16Count = @($p16).Count
        p16Failed = @($p16 | Where-Object { $_.outcome -eq 'Failed' }).Count
        p16Passed = @($p16 | Where-Object { $_.outcome -eq 'Passed' }).Count
        p16Skipped = @($p16 | Where-Object { $_.outcome -in @('NotExecuted','Skipped','Inconclusive') }).Count
        p17Count = @($p17).Count
        p17Failed = @($p17 | Where-Object { $_.outcome -eq 'Failed' }).Count
        p17Passed = @($p17 | Where-Object { $_.outcome -eq 'Passed' }).Count
        p17Skipped = @($p17 | Where-Object { $_.outcome -in @('NotExecuted','Skipped','Inconclusive') }).Count
        p18Count = @($p18).Count
        p18Failed = @($p18 | Where-Object { $_.outcome -eq 'Failed' }).Count
        p18Passed = @($p18 | Where-Object { $_.outcome -eq 'Passed' }).Count
        p18Skipped = @($p18 | Where-Object { $_.outcome -in @('NotExecuted','Skipped','Inconclusive') }).Count
        p19Count = @($p19).Count
        aiCount = @($ai).Count
        aiFailed = @($ai | Where-Object { $_.outcome -eq 'Failed' }).Count
        aiPassed = @($ai | Where-Object { $_.outcome -eq 'Passed' }).Count
        aiSkipped = @($ai | Where-Object { $_.outcome -in @('NotExecuted','Skipped','Inconclusive') }).Count
        p16ByHost = @($p16ByHost)
        passedNames = @($outcomes | Where-Object { $_.outcome -eq 'Passed' } | ForEach-Object { $_.name })
    } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $dest -Encoding utf8
}

function Get-ConsoleCounts {
    param([string]$LogPath, [string]$Dest)
    $console = [ordered]@{ exists = (Test-Path -LiteralPath $LogPath); path = $LogPath }
    if (Test-Path -LiteralPath $LogPath) {
        $t = Get-Content -LiteralPath $LogPath -Raw
        $console.successful = [bool]($t -match 'Test Run Successful')
        $console.failedBang = [bool]($t -match 'Test Run Failed')
        $console.totalTests = if ($t -match '(?m)^Total tests:\s+(\d+)') { [int]$Matches[1] } else { $null }
        $console.passed = if ($t -match '(?m)^\s+Passed:\s+(\d+)') { [int]$Matches[1] } else { $null }
        $console.failed = if ($t -match '(?m)^\s+Failed:\s+(\d+)') { [int]$Matches[1] } else { 0 }
        $console.skipped = if ($t -match '(?m)^\s+Skipped:\s+(\d+)') { [int]$Matches[1] } else { 0 }
        $console.totalTime = if ($t -match '(?m)^Total time:\s+(.+)$') { $Matches[1].Trim() } else { $null }
        $console.failedResultLines = @([regex]::Matches($t, '(?m)^\s+Failed McpServer\.') | ForEach-Object { $_.Value }).Count
        $item = Get-Item -LiteralPath $LogPath
        $console.length = $item.Length
        $console.lastWriteTimeUtc = $item.LastWriteTimeUtc.ToString('o')
    }
    $console | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $Dest -Encoding utf8
}

$aiTrx = Get-ChildItem -LiteralPath $out -Recurse -Filter 'ai-theory.trx' -ErrorAction SilentlyContinue | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
$nukeTrx = Get-ChildItem -LiteralPath $out -Recurse -Filter 'nuke-skip.trx' -ErrorAction SilentlyContinue | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1

if ($aiTrx) { Get-TrxSummary -TrxPath $aiTrx.FullName -Name 'ai' } else { Get-TrxSummary -TrxPath (Join-Path $out 'missing-ai-theory.trx') -Name 'ai' }
if ($nukeTrx) { Get-TrxSummary -TrxPath $nukeTrx.FullName -Name 'nuke' } else { Get-TrxSummary -TrxPath (Join-Path $out 'missing-nuke-skip.trx') -Name 'nuke' }

Get-ConsoleCounts -LogPath (Join-Path $out 'hv-dotnet-ai-filter.log') -Dest (Join-Path $out 'hv-dotnet-ai-filter-console.json')
Get-ConsoleCounts -LogPath (Join-Path $out 'hv-dotnet-nuke-filter.log') -Dest (Join-Path $out 'hv-dotnet-nuke-filter-console.json')

Write-Output 'PARSE_DONE'
