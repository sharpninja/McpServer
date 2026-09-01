#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$worktree = 'F:\GitHub\McpServer\.worktrees\triage-closeout'
$testProject = Join-Path $worktree 'tests\McpServer.Support.Mcp.Tests\McpServer.Support.Mcp.Tests.csproj'
$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-closeout-s1'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
$trxName = 'SessionLogCloseoutFilter.trx'
$trx = Join-Path $outDir $trxName
$log = Join-Path $outDir 'dotnet-test.log'

$filter = 'FullyQualifiedName~SessionLogSchemaGuardTests|FullyQualifiedName~SessionLogAgentSessionHeaderMigrationTests'
$dotnetArgs = @(
    'test', $testProject,
    '-c', 'Debug',
    '--filter', $filter,
    '--nologo',
    '--logger', "trx;LogFileName=$trx",
    '--logger', 'console;verbosity=detailed'
)

Push-Location $worktree
try {
    & dotnet @dotnetArgs *>&1 | Tee-Object -FilePath $log | Out-Null
    $exit = $LASTEXITCODE
} finally {
    Pop-Location
}

$summary = [ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    Worktree = $worktree
    TestProject = $testProject
    Filter = $filter
    ExitCode = $exit
    LogPath = $log
    TrxPath = $trx
    TrxExists = Test-Path -LiteralPath $trx
}

if (Test-Path -LiteralPath $log) {
    $text = Get-Content -LiteralPath $log -Raw
    $summary.Passed = if ($text -match 'Passed:\s+(\d+)') { [int]$Matches[1] } else { $null }
    $summary.Failed = if ($text -match 'Failed:\s+(\d+)') { [int]$Matches[1] } else { $null }
    $summary.Skipped = if ($text -match 'Skipped:\s+(\d+)') { [int]$Matches[1] } else { $null }
    $summary.Total = if ($text -match 'Total:\s+(\d+)') { [int]$Matches[1] } else { $null }
    $summary.Tail = (($text -split "`n") | Select-Object -Last 80) -join "`n"
}

if (Test-Path -LiteralPath $trx) {
    [xml]$trxXml = Get-Content -LiteralPath $trx -Raw
    $counters = $trxXml.TestRun.ResultSummary.Counters
    $summary.TrxTotal = [int]$counters.total
    $summary.TrxExecuted = [int]$counters.executed
    $summary.TrxPassed = [int]$counters.passed
    $summary.TrxFailed = [int]$counters.failed
    $summary.TrxNotExecuted = [int]$counters.notExecuted
    $units = @($trxXml.TestRun.Results.UnitTestResult)
    $summary.Methods = @(
        $units | ForEach-Object {
            [ordered]@{
                testName = $_.testName
                outcome = $_.outcome
            }
        }
    )
}

$jsonPath = Join-Path $outDir 'tests.json'
$summary | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $jsonPath -Encoding utf8
Write-Output $jsonPath
Write-Output ($summary | ConvertTo-Json -Depth 8)
exit $exit
