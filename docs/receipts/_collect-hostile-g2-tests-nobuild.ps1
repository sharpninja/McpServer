#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$workspace = 'F:\GitHub\McpServer'
$testProject = Join-Path $workspace 'tests\McpServer.Support.Mcp.Tests\McpServer.Support.Mcp.Tests.csproj'
$outDir = Join-Path $workspace 'docs\receipts\_hv-g2-testout'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
$trxName = 'SessionLogSchemaGuardTests-nobuild.trx'
$log = Join-Path $outDir 'dotnet-test-nobuild.log'
$resultsDir = Join-Path $outDir 'results-nobuild'
New-Item -ItemType Directory -Force -Path $resultsDir | Out-Null

$dll = Join-Path $workspace 'tests\McpServer.Support.Mcp.Tests\bin\Debug\net10.0\McpServer.Support.Mcp.Tests.dll'
$filter = 'FullyQualifiedName~SessionLogSchemaGuardTests'

$summary = [ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    Filter = $filter
    DllExists = Test-Path -LiteralPath $dll
    DllPath = $dll
}

Push-Location $workspace
try {
    if (Test-Path -LiteralPath $dll) {
        & dotnet test $dll --no-build --nologo --filter $filter --results-directory $resultsDir --logger "trx;LogFileName=$trxName" --logger 'console;verbosity=detailed' *>&1 | Tee-Object -FilePath $log | Out-Null
        $summary.Mode = 'dll-no-build'
    } else {
        & dotnet test $testProject -c Debug --no-build --nologo --filter $filter --results-directory $resultsDir --logger "trx;LogFileName=$trxName" --logger 'console;verbosity=detailed' *>&1 | Tee-Object -FilePath $log | Out-Null
        $summary.Mode = 'csproj-no-build'
    }
    $summary.ExitCode = $LASTEXITCODE
} finally {
    Pop-Location
}

$summary.LogPath = $log
$trx = Join-Path $resultsDir $trxName
$summary.TrxPath = $trx
$summary.TrxExists = Test-Path -LiteralPath $trx

if (Test-Path -LiteralPath $log) {
    $text = Get-Content -LiteralPath $log -Raw
    $summary.Passed = if ($text -match 'Passed:\s+(\d+)') { [int]$Matches[1] } else { $null }
    $summary.Failed = if ($text -match 'Failed:\s+(\d+)') { [int]$Matches[1] } else { $null }
    $summary.Skipped = if ($text -match 'Skipped:\s+(\d+)') { [int]$Matches[1] } else { $null }
    $summary.Total = if ($text -match 'Total:\s+(\d+)') { [int]$Matches[1] } else { $null }
    $summary.Tail = (($text -split "`n") | Select-Object -Last 60) -join "`n"
}

$summary | ConvertTo-Json -Depth 6
