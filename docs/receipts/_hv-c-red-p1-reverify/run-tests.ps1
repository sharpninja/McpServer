#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p1-reverify'
New-Item -ItemType Directory -Force -Path $out | Out-Null
Set-Location -LiteralPath 'F:\GitHub\McpServer'

$filter = 'FullyQualifiedName~NukeBuild.Tests.PluginSessionLogIntegrationTargetTests'
$trx = Join-Path $out 'hv-c-red-p1-reverify.trx'
$log = Join-Path $out 'dotnet-test.log'

$args = @(
    'test', 'tests/Build.Tests',
    '-c', 'Debug',
    '--filter', $filter,
    '--logger', ('trx;LogFileName=' + $trx),
    '--results-directory', $out,
    '--nologo'
)

Write-Output ('DOTNET_TEST_START filter=' + $filter)
& dotnet @args *>&1 | Tee-Object -FilePath $log
$code = $LASTEXITCODE
Set-Content -LiteralPath (Join-Path $out 'dotnet-test-exit.txt') -Value ([string]$code) -Encoding utf8
Write-Output ('DOTNET_TEST_EXIT=' + $code)

# Parse TRX outcomes
$trxFiles = Get-ChildItem -LiteralPath $out -Filter '*.trx' -File -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTimeUtc -Descending
$trxPath = if ($trxFiles) { $trxFiles[0].FullName } else { $null }
$summary = [ordered]@{
    trxPath = $trxPath
    exitCode = $code
    results = @()
    failed = 0
    skipped = 0
    passed = 0
    other = 0
}
if ($trxPath -and (Test-Path -LiteralPath $trxPath)) {
    [xml]$xml = Get-Content -LiteralPath $trxPath
    $ns = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
    $ns.AddNamespace('t', 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010')
    $nodes = $xml.SelectNodes('//t:UnitTestResult', $ns)
    foreach ($n in @($nodes)) {
        $outcome = [string]$n.outcome
        $name = [string]$n.testName
        $message = ''
        $err = $n.SelectSingleNode('t:Output/t:ErrorInfo/t:Message', $ns)
        if ($err) { $message = [string]$err.InnerText }
        $summary.results += [pscustomobject]@{
            testName = $name
            outcome = $outcome
            duration = [string]$n.duration
            message = $message
        }
        switch ($outcome) {
            'Failed' { $summary.failed++ }
            'Skipped' { $summary.skipped++ }
            'Passed' { $summary.passed++ }
            'NotExecuted' { $summary.skipped++ }
            default { $summary.other++ }
        }
    }
}
$summary | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'trx-summary.json') -Encoding utf8
Write-Output ('TRX_FAILED=' + $summary.failed)
Write-Output ('TRX_SKIPPED=' + $summary.skipped)
Write-Output ('TRX_PASSED=' + $summary.passed)
Write-Output 'TEST_RUN_DONE'
