#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p5-p6-20260822T012151Z'
New-Item -ItemType Directory -Force -Path $out | Out-Null
Set-Location -LiteralPath 'F:\GitHub\McpServer'

function Get-FileStamp([string]$Path) {
    if (Test-Path -LiteralPath $Path) {
        $item = Get-Item -LiteralPath $Path
        return [ordered]@{
            Path = $Path
            Exists = $true
            Length = $item.Length
            LastWriteTimeUtc = $item.LastWriteTimeUtc.ToString('o')
        }
    }
    return [ordered]@{ Path = $Path; Exists = $false }
}

$candidateDbs = @(
    'F:\GitHub\McpServer\mcp.db',
    'F:\GitHub\McpServer\mcp-data\mcp.db',
    'F:\GitHub\McpServer\src\McpServer.Support.Mcp\mcp.db',
    'C:\ProgramData\McpServer\mcp.db',
    'C:\ProgramData\McpServer\data\mcp.db'
)
$extra = Get-ChildItem -LiteralPath 'F:\GitHub\McpServer' -Filter '*.db' -File -ErrorAction SilentlyContinue
$extra2 = Get-ChildItem -LiteralPath 'C:\ProgramData\McpServer' -Filter '*.db' -Recurse -File -ErrorAction SilentlyContinue
$allDb = @($candidateDbs + @($extra | ForEach-Object { $_.FullName }) + @($extra2 | ForEach-Object { $_.FullName }) | Select-Object -Unique)

$beforeTemps = @(Get-ChildItem -LiteralPath $env:TEMP -Directory -Filter 'mcp-pluginint-*' -ErrorAction SilentlyContinue | Select-Object Name, FullName, LastWriteTimeUtc)
$before = [ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    DeveloperPidFromMarker = 16936
    Dbs = @($allDb | ForEach-Object { Get-FileStamp $_ })
    TempPluginIntDirs = @($beforeTemps | ForEach-Object {
        [ordered]@{ Name = $_.Name; FullName = $_.FullName; LastWriteTimeUtc = $_.LastWriteTimeUtc.ToString('o') }
    })
    Listeners7147 = @(Get-NetTCPConnection -LocalPort 7147 -State Listen -ErrorAction SilentlyContinue | Select-Object LocalAddress, LocalPort, OwningProcess)
}
$before | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $out 'isolation-before.json') -Encoding utf8
Write-Output ('BEFORE_DB_COUNT=' + @($before.Dbs).Count)
Write-Output ('BEFORE_TEMP_COUNT=' + @($beforeTemps).Count)

$filterLog = Join-Path $out 'dotnet-fixture-filter.log'
$filterTrxDir = Join-Path $out 'trx-filter'
New-Item -ItemType Directory -Force -Path $filterTrxDir | Out-Null
Write-Output 'START_FILTER_TESTS'
$filterSw = [System.Diagnostics.Stopwatch]::StartNew()
& dotnet test 'tests\McpServer.PluginIntegration.Tests' -c Debug --filter 'FullyQualifiedName~PluginIntegrationServerFixtureTests' --logger "trx;LogFileName=dotnet-fixture-filter.trx" --results-directory $filterTrxDir *>&1 |
    Tee-Object -FilePath $filterLog
$filterExit = $LASTEXITCODE
$filterSw.Stop()
[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    ExitCode = $filterExit
    DurationMs = $filterSw.ElapsedMilliseconds
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'dotnet-fixture-filter-exit.json') -Encoding utf8
Write-Output ("FILTER_EXIT=$filterExit DURATION_MS=$($filterSw.ElapsedMilliseconds)")

$allLog = Join-Path $out 'dotnet-pluginintegration-all.log'
$allTrxDir = Join-Path $out 'trx-all'
New-Item -ItemType Directory -Force -Path $allTrxDir | Out-Null
Write-Output 'START_ALL_TESTS'
$allSw = [System.Diagnostics.Stopwatch]::StartNew()
& dotnet test 'tests\McpServer.PluginIntegration.Tests' -c Debug --logger "trx;LogFileName=dotnet-pluginintegration-all.trx" --results-directory $allTrxDir *>&1 |
    Tee-Object -FilePath $allLog
$allExit = $LASTEXITCODE
$allSw.Stop()
[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    ExitCode = $allExit
    DurationMs = $allSw.ElapsedMilliseconds
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'dotnet-pluginintegration-all-exit.json') -Encoding utf8
Write-Output ("ALL_EXIT=$allExit DURATION_MS=$($allSw.ElapsedMilliseconds)")

$afterTemps = @(Get-ChildItem -LiteralPath $env:TEMP -Directory -Filter 'mcp-pluginint-*' -ErrorAction SilentlyContinue | Select-Object Name, FullName, LastWriteTimeUtc)
$after = [ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    Dbs = @($allDb | ForEach-Object { Get-FileStamp $_ })
    TempPluginIntDirs = @($afterTemps | ForEach-Object {
        [ordered]@{ Name = $_.Name; FullName = $_.FullName; LastWriteTimeUtc = $_.LastWriteTimeUtc.ToString('o') }
    })
    Listeners7147 = @(Get-NetTCPConnection -LocalPort 7147 -State Listen -ErrorAction SilentlyContinue | Select-Object LocalAddress, LocalPort, OwningProcess)
}
$after | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $out 'isolation-after.json') -Encoding utf8
Write-Output ('AFTER_TEMP_COUNT=' + @($afterTemps).Count)

try {
    $devHealth = Invoke-RestMethod -Uri 'http://127.0.0.1:7147/health' -Method Get -TimeoutSec 10
    [ordered]@{
        TimestampUtc = [DateTime]::UtcNow.ToString('o')
        status = $devHealth.status
        storage = $devHealth.storage
        version = $devHealth.version
    } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'developer-health-after.json') -Encoding utf8
    Write-Output ('DEV_HEALTH=' + $devHealth.status)
} catch {
    Set-Content -LiteralPath (Join-Path $out 'developer-health-after.json') -Value $_.Exception.ToString() -Encoding utf8
    Write-Output 'DEV_HEALTH_FAIL'
}

Write-Output 'TESTS_DONE'
exit 0
