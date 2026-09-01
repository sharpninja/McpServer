#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$workspace = 'F:\GitHub\McpServer'
$testProject = Join-Path $workspace 'tests\McpServer.Support.Mcp.Tests\McpServer.Support.Mcp.Tests.csproj'
$outDir = Join-Path $workspace 'docs\receipts\_hv-g2-testout'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
$trx = Join-Path $outDir 'SessionLogSchemaGuardTests.trx'
$log = Join-Path $outDir 'dotnet-test.log'

$filter = 'FullyQualifiedName~SessionLogSchemaGuardTests'
$args = @(
    'test', $testProject,
    '-c', 'Debug',
    '--filter', $filter,
    '--nologo',
    '--logger', "trx;LogFileName=$trx",
    '--logger', 'console;verbosity=detailed'
)

Push-Location $workspace
try {
    & dotnet @args *>&1 | Tee-Object -FilePath $log | Out-Null
    $exit = $LASTEXITCODE
} finally {
    Pop-Location
}

$summary = [ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
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
    $summary.Tail = (($text -split "`n") | Select-Object -Last 40) -join "`n"
}

$summary | ConvertTo-Json -Depth 6
