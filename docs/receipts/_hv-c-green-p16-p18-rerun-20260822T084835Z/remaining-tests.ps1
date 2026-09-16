#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p16-p18-rerun-20260822T084835Z'
Set-Location -LiteralPath 'F:\GitHub\McpServer'
$dotnet = 'C:\Program Files\dotnet\dotnet.exe'
$utc = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')

function Get-Attr {
    param($Node, [string]$Name)
    if ($null -eq $Node) { return $null }
    $attr = $Node.Attributes[$Name]
    if ($null -eq $attr) { return $null }
    return [string]$attr.Value
}

function Get-TrxSummary {
    param([string]$TrxPath, [string]$Dest)
    if (-not (Test-Path -LiteralPath $TrxPath)) {
        [ordered]@{ exists = $false; path = $TrxPath } | ConvertTo-Json | Set-Content -LiteralPath $Dest -Encoding utf8
        return
    }
    $xml = New-Object System.Xml.XmlDocument
    $xml.Load($TrxPath)
    $ns = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
    $ns.AddNamespace('t', 'http://microsoft.com/schemas/visualstudio/2010/test')
    $counters = $xml.SelectSingleNode('//t:Counters', $ns)
    $unitResults = @($xml.SelectNodes('//t:UnitTestResult', $ns))
    $outcomes = @()
    foreach ($r in $unitResults) {
        $outcomes += [pscustomobject]@{
            name = Get-Attr $r 'testName'
            outcome = Get-Attr $r 'outcome'
            duration = Get-Attr $r 'duration'
        }
    }
    $names = @($outcomes | ForEach-Object { $_.name })
    $skippedNames = @($outcomes | Where-Object { $_.outcome -match 'Skipped|NotExecuted' } | ForEach-Object { $_.name })
    $failedNames = @($outcomes | Where-Object { $_.outcome -eq 'Failed' } | ForEach-Object { $_.name })
    [ordered]@{
        exists = $true
        path = $TrxPath
        lastWriteTimeUtc = (Get-Item -LiteralPath $TrxPath).LastWriteTimeUtc.ToString('o')
        total = Get-Attr $counters 'total'
        executed = Get-Attr $counters 'executed'
        passed = Get-Attr $counters 'passed'
        failed = Get-Attr $counters 'failed'
        skipped = Get-Attr $counters 'skipped'
        notExecuted = Get-Attr $counters 'notExecuted'
        resultCount = $unitResults.Count
        skippedNames = @($skippedNames)
        failedNames = @($failedNames)
        p16Count = @($names | Where-Object { $_ -match 'AiTheory_Agent_RequiresValidJsonFields' }).Count
        p16Passed = @($outcomes | Where-Object { $_.name -match 'AiTheory_Agent_RequiresValidJsonFields' -and $_.outcome -eq 'Passed' }).Count
        p16Failed = @($outcomes | Where-Object { $_.name -match 'AiTheory_Agent_RequiresValidJsonFields' -and $_.outcome -eq 'Failed' }).Count
        p17Count = @($names | Where-Object { $_ -match 'AiTheory_RejectsInvalidJson_DoesNotOverrideDeterministicFailure' }).Count
        p19Count = @($names | Where-Object { $_ -match 'PluginNativeSuite_|PluginInt_P19_' }).Count
        names = $names
    } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $Dest -Encoding utf8
}

function Invoke-DotnetCapture {
    param([string[]]$DotnetArgs, [string]$LogName, [string]$ExitName)
    $logPath = Join-Path $out $LogName
    $exitPath = Join-Path $out $ExitName
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $output = & $dotnet @DotnetArgs 2>&1 | ForEach-Object { $_.ToString() }
    $code = $LASTEXITCODE
    $sw.Stop()
    $text = $output -join [Environment]::NewLine
    Set-Content -LiteralPath $logPath -Value $text -Encoding utf8
    $console = [ordered]@{
        ExitCode = $code
        DurationMs = $sw.ElapsedMilliseconds
        SummaryLine = $null
    }
    if ($text -match 'Passed!.*Failed:\s+\d+.*Passed:\s+\d+.*Skipped:\s+\d+.*Total:\s+\d+') {
        $console.SummaryLine = $Matches[0].Trim()
    } elseif ($text -match 'Failed!.*Failed:\s+\d+.*Passed:\s+\d+.*Skipped:\s+\d+.*Total:\s+\d+') {
        $console.SummaryLine = $Matches[0].Trim()
    }
    $console | ConvertTo-Json | Set-Content -LiteralPath $exitPath -Encoding utf8
    Write-Output ("DONE " + $LogName + " exit=" + $code + " ms=" + $sw.ElapsedMilliseconds)
    return $code
}

$p19 = Get-Item -LiteralPath 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginNativeSuiteReceiptTests.cs' -ErrorAction SilentlyContinue
if ($p19) {
    [ordered]@{
        exists = $true
        path = $p19.FullName
        length = $p19.Length
        lastWriteTimeUtc = $p19.LastWriteTimeUtc.ToString('o')
        creationTimeUtc = $p19.CreationTimeUtc.ToString('o')
    } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'p19-file-timestamp.json') -Encoding utf8
} else {
    [ordered]@{ exists = $false } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'p19-file-timestamp.json') -Encoding utf8
}

Get-TrxSummary -TrxPath (Join-Path $out 'results-nuke-20260822T085418Z\nuke-skip.trx') -Dest (Join-Path $out 'nuke-trx-summary.json')
Get-TrxSummary -TrxPath (Join-Path $out 'results-ai-theory-20260822T085418Z\ai-theory.trx') -Dest (Join-Path $out 'ai-theory-trx-summary.json')
$oldAiTrait = Join-Path $out 'results-ai-trait-20260822T085418Z\ai-trait.trx'
if (Test-Path -LiteralPath $oldAiTrait) {
    Get-TrxSummary -TrxPath $oldAiTrait -Dest (Join-Path $out 'ai-trait-partial-trx-summary.json')
}

$aiTraitDir = Join-Path $out ("results-ai-trait-rerun-" + $utc)
New-Item -ItemType Directory -Force -Path $aiTraitDir | Out-Null
$aiTraitArgs = @(
    'test', 'tests/McpServer.PluginIntegration.Tests', '-c', 'Debug',
    '--filter', 'PluginInt=AI',
    '--results-directory', $aiTraitDir,
    '--logger', 'trx;LogFileName=ai-trait.trx'
)
Invoke-DotnetCapture -DotnetArgs $aiTraitArgs -LogName 'hv-dotnet-ai-trait-filter.log' -ExitName 'hv-dotnet-ai-trait-filter-exit.json'
$aiTraitTrx = Get-ChildItem -LiteralPath $aiTraitDir -Filter '*.trx' -Recurse | Select-Object -First 1
if ($aiTraitTrx) { Get-TrxSummary -TrxPath $aiTraitTrx.FullName -Dest (Join-Path $out 'ai-trait-trx-summary.json') }

$fullDir = Join-Path $out ("results-full-" + $utc)
New-Item -ItemType Directory -Force -Path $fullDir | Out-Null
$fullArgs = @(
    'test', 'tests/McpServer.PluginIntegration.Tests', '-c', 'Debug',
    '--results-directory', $fullDir,
    '--logger', 'trx;LogFileName=pluginint-all.trx'
)
Invoke-DotnetCapture -DotnetArgs $fullArgs -LogName 'hv-dotnet-pluginint-all.log' -ExitName 'hv-dotnet-pluginint-all-exit.json'
$fullTrx = Get-ChildItem -LiteralPath $fullDir -Filter '*.trx' -Recurse | Select-Object -First 1
if ($fullTrx) { Get-TrxSummary -TrxPath $fullTrx.FullName -Dest (Join-Path $out 'full-trx-summary.json') }

Write-Output 'REMAINING_TESTS_DONE'
