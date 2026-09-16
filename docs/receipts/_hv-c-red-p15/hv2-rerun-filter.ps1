#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p15\hv2'
New-Item -ItemType Directory -Force -Path $out | Out-Null
Set-Location -LiteralPath 'F:\GitHub\McpServer'
$transcript = Join-Path $out 'rerun-filter.transcript.log'
try { Stop-Transcript | Out-Null } catch { }
Start-Transcript -Path $transcript -Force | Out-Null
Write-Output ("RERUN_START=" + [DateTime]::UtcNow.ToString('o'))

$leftover = @(Get-CimInstance Win32_Process | Where-Object {
    $_.CommandLine -and (
        ($_.Name -match 'testhost|vstest') -or
        ($_.Name -eq 'dotnet.exe' -and $_.CommandLine -match 'McpServer.Support.Mcp.dll') -or
        ($_.CommandLine -match 'mcp-pluginint-')
    )
})
$leftItems = foreach ($p in $leftover) {
    $cmd = [string]$p.CommandLine
    if ($cmd.Length -gt 300) { $cmd = $cmd.Substring(0, 300) }
    [ordered]@{ ProcessId = $p.ProcessId; Name = $p.Name; CommandLine = $cmd }
}
[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    Count = $leftItems.Count
    Items = $leftItems
} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'rerun-leftover-procs.json') -Encoding utf8
Write-Output ("LEFTOVER=" + $leftItems.Count)

$deadline = [DateTime]::UtcNow.AddMinutes(3)
do {
    $busy = @(Get-CimInstance Win32_Process | Where-Object {
        $_.CommandLine -and $_.CommandLine -match 'PluginIntegration' -and
        ($_.Name -match 'testhost|vstest' -or ($_.Name -eq 'dotnet.exe' -and $_.CommandLine -match 'dotnet.exe\" test '))
    })
    Write-Output ("BUSY_PLUGININT=" + $busy.Count)
    if ($busy.Count -eq 0) { break }
    Start-Sleep -Seconds 10
} while ([DateTime]::UtcNow -lt $deadline)

$filterTrxDir = Join-Path $out 'trx-p15-rerun'
if (Test-Path -LiteralPath $filterTrxDir) { Remove-Item -LiteralPath $filterTrxDir -Recurse -Force }
New-Item -ItemType Directory -Force -Path $filterTrxDir | Out-Null
$filterLog = Join-Path $out 'dotnet-p15-filter-rerun.log'
$filterExpr = 'FullyQualifiedName~Theory_Agent_Success_NoPendingFailsafe|FullyQualifiedName~Theory_Agent_FailedSubmit_RetainsRootIdPending|FullyQualifiedName~Theory_Agent_RetrySuccess_DeletesOnlyMatchingPending'
$swFilter = [System.Diagnostics.Stopwatch]::StartNew()
$filterArgs = @(
    'test', 'tests/McpServer.PluginIntegration.Tests', '-c', 'Debug',
    '--filter', $filterExpr,
    '--logger', 'trx',
    '--logger', 'console',
    '--results-directory', $filterTrxDir
)
& dotnet @filterArgs *>&1 | Tee-Object -FilePath $filterLog
$filterExit = $LASTEXITCODE
$swFilter.Stop()
[ordered]@{
    Command = "dotnet test tests/McpServer.PluginIntegration.Tests -c Debug --filter `"$filterExpr`""
    ExitCode = $filterExit
    DurationMs = $swFilter.ElapsedMilliseconds
    Log = $filterLog
    ResultsDirectory = $filterTrxDir
    FinishedUtc = [DateTime]::UtcNow.ToString('o')
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'dotnet-p15-filter-rerun-exit.json') -Encoding utf8
Write-Output ("RERUN_FILTER_EXIT=$filterExit")
Write-Output ("RERUN_END=" + [DateTime]::UtcNow.ToString('o'))
try { Stop-Transcript | Out-Null } catch { }
