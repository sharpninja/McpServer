#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p16-p18-rerun-20260822T084835Z'
Set-Location -LiteralPath 'F:\GitHub\McpServer'
$dotnet = 'C:\Program Files\dotnet\dotnet.exe'
$utc = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
$fullDir = Join-Path $out ("results-full-" + $utc)
New-Item -ItemType Directory -Force -Path $fullDir | Out-Null
$DotnetArgs = @(
    'test', 'tests/McpServer.PluginIntegration.Tests', '-c', 'Debug',
    '--results-directory', $fullDir,
    '--logger', 'trx;LogFileName=pluginint-all.trx'
)
$sw = [System.Diagnostics.Stopwatch]::StartNew()
$output = & $dotnet @DotnetArgs 2>&1 | ForEach-Object { $_.ToString() }
$code = $LASTEXITCODE
$sw.Stop()
$text = $output -join [Environment]::NewLine
Set-Content -LiteralPath (Join-Path $out 'hv-dotnet-pluginint-all.log') -Value $text -Encoding utf8
$summary = $null
if ($text -match '(Passed!|Failed!).+Failed:\s+\d+.+Passed:\s+\d+.+Skipped:\s+\d+.+Total:\s+\d+') {
    $summary = $Matches[0].Trim()
}
[ordered]@{
    ExitCode = $code
    DurationMs = $sw.ElapsedMilliseconds
    SummaryLine = $summary
    ResultsDirectory = $fullDir
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'hv-dotnet-pluginint-all-exit.json') -Encoding utf8

$trx = Get-ChildItem -LiteralPath $fullDir -Filter '*.trx' -Recurse | Select-Object -First 1
if ($trx) {
    $trxText = Get-Content -LiteralPath $trx.FullName -Raw
    $total = $null; $executed = $null; $passed = $null; $failed = $null; $skipped = $null; $notExecuted = $null
    if ($trxText -match 'Counters\s+([^/]+)/') {
        $attrs = $Matches[1]
        if ($attrs -match 'total="(\d+)"') { $total = $Matches[1] }
        if ($attrs -match 'executed="(\d+)"') { $executed = $Matches[1] }
        if ($attrs -match 'passed="(\d+)"') { $passed = $Matches[1] }
        if ($attrs -match 'failed="(\d+)"') { $failed = $Matches[1] }
        if ($attrs -match 'skipped="(\d+)"') { $skipped = $Matches[1] }
        if ($attrs -match 'notExecuted="(\d+)"') { $notExecuted = $Matches[1] }
    }
    $unitNames = @([regex]::Matches($trxText, 'testName="([^"]+)"') | ForEach-Object { $_.Groups[1].Value })
    $failedNames = @([regex]::Matches($trxText, 'outcome="Failed"') )
    [ordered]@{
        exists = $true
        path = $trx.FullName
        lastWriteTimeUtc = $trx.LastWriteTimeUtc.ToString('o')
        total = $total
        executed = $executed
        passed = $passed
        failed = $failed
        skipped = $skipped
        notExecuted = $notExecuted
        p16Count = @($unitNames | Where-Object { $_ -match 'AiTheory_Agent_RequiresValidJsonFields' }).Count
        p17Count = @($unitNames | Where-Object { $_ -match 'AiTheory_RejectsInvalidJson' }).Count
        p19Count = @($unitNames | Where-Object { $_ -match 'PluginNativeSuite_|PluginInt_P19_' }).Count
        nameCount = $unitNames.Count
        failedNameHits = @($unitNames | Where-Object { $_ -match 'PluginNativeSuite_|PluginInt_P19_' })
    } | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'full-trx-text.json') -Encoding utf8
}

Write-Output 'FULL_SUITE_DONE'
Write-Output $code
