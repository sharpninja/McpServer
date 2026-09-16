#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p16-p18-rerun-20260822T084835Z'

function Parse-TrxText {
    param([string]$TrxPath, [string]$Dest)
    if (-not (Test-Path -LiteralPath $TrxPath)) {
        [ordered]@{ exists = $false; path = $TrxPath } | ConvertTo-Json | Set-Content -LiteralPath $Dest -Encoding utf8
        return
    }
    $text = Get-Content -LiteralPath $TrxPath -Raw
    $total = $null; $executed = $null; $passed = $null; $failed = $null; $skipped = $null; $notExecuted = $null
    if ($text -match 'Counters\s+([^/]+)/') {
        $attrs = $Matches[1]
        if ($attrs -match 'total="(\d+)"') { $total = $Matches[1] }
        if ($attrs -match 'executed="(\d+)"') { $executed = $Matches[1] }
        if ($attrs -match 'passed="(\d+)"') { $passed = $Matches[1] }
        if ($attrs -match 'failed="(\d+)"') { $failed = $Matches[1] }
        if ($attrs -match 'skipped="(\d+)"') { $skipped = $Matches[1] }
        if ($attrs -match 'notExecuted="(\d+)"') { $notExecuted = $Matches[1] }
    }
    $unitNames = @([regex]::Matches($text, 'testName="([^"]+)"') | ForEach-Object { $_.Groups[1].Value })
    $failedNames = @([regex]::Matches($text, 'outcome="Failed"[^>]*testName="([^"]+)"|testName="([^"]+)"[^>]*outcome="Failed"') | ForEach-Object { if ($_.Groups[1].Success) { $_.Groups[1].Value } else { $_.Groups[2].Value } })
    $skippedNames = @([regex]::Matches($text, 'outcome="(Skipped|NotExecuted)"[^>]*testName="([^"]+)"|testName="([^"]+)"[^>]*outcome="(Skipped|NotExecuted)"') | ForEach-Object { if ($_.Groups[2].Success -and $_.Groups[2].Value) { $_.Groups[2].Value } else { $_.Groups[3].Value } })
    [ordered]@{
        exists = $true
        path = $TrxPath
        lastWriteTimeUtc = (Get-Item -LiteralPath $TrxPath).LastWriteTimeUtc.ToString('o')
        length = (Get-Item -LiteralPath $TrxPath).Length
        total = $total
        executed = $executed
        passed = $passed
        failed = $failed
        skipped = $skipped
        notExecuted = $notExecuted
        nameCount = $unitNames.Count
        p16Count = @($unitNames | Where-Object { $_ -match 'AiTheory_Agent_RequiresValidJsonFields' }).Count
        p17Count = @($unitNames | Where-Object { $_ -match 'AiTheory_RejectsInvalidJson' }).Count
        p19Count = @($unitNames | Where-Object { $_ -match 'PluginNativeSuite_|PluginInt_P19_' }).Count
        failedNames = @($failedNames | Where-Object { $_ })
        skippedNames = @($skippedNames | Where-Object { $_ })
        namesSample = @($unitNames | Select-Object -First 20)
    } | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $Dest -Encoding utf8
}

Parse-TrxText -TrxPath (Join-Path $out 'results-nuke-20260822T085418Z\nuke-skip.trx') -Dest (Join-Path $out 'nuke-trx-text.json')
Parse-TrxText -TrxPath (Join-Path $out 'results-ai-theory-20260822T085418Z\ai-theory.trx') -Dest (Join-Path $out 'ai-theory-trx-text.json')
$aiTrait = Get-ChildItem -LiteralPath $out -Recurse -Filter 'ai-trait.trx' | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
if ($aiTrait) {
    Parse-TrxText -TrxPath $aiTrait.FullName -Dest (Join-Path $out 'ai-trait-trx-text.json')
}

$p19Helper = Get-Item -LiteralPath 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginNativeSuiteReceipt.cs' -ErrorAction SilentlyContinue
if ($p19Helper) {
    [ordered]@{
        exists = $true
        lastWriteTimeUtc = $p19Helper.LastWriteTimeUtc.ToString('o')
        creationTimeUtc = $p19Helper.CreationTimeUtc.ToString('o')
        length = $p19Helper.Length
    } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'p19-helper-timestamp.json') -Encoding utf8
}

Write-Output 'PARSE_TRX_TEXT_DONE'
