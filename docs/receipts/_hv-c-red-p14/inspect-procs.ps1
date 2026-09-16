#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p14'
$procs = @(Get-CimInstance Win32_Process | Where-Object {
    $_.Name -match 'testhost|dotnet' -and [string]$_.CommandLine -match 'PluginIntegration'
} | Select-Object ProcessId, Name, CommandLine)
[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    Count = @($procs).Count
    Processes = @($procs | ForEach-Object {
        [ordered]@{
            ProcessId = $_.ProcessId
            Name = $_.Name
            CommandLine = [string]$_.CommandLine
        }
    })
} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $out 'dotnet-procs-after-kill.json') -Encoding utf8
Write-Output ('HOST_COUNT=' + @($procs).Count)
Get-ChildItem -LiteralPath $out -Filter 'dotnet-p14*' -ErrorAction SilentlyContinue | ForEach-Object {
    Write-Output ($_.FullName + ' len=' + $_.Length + ' mtime=' + $_.LastWriteTimeUtc.ToString('o'))
}
$trxDir = Join-Path $out 'trx-p14'
if (Test-Path -LiteralPath $trxDir) {
    Get-ChildItem -LiteralPath $trxDir | ForEach-Object {
        Write-Output ($_.FullName + ' len=' + $_.Length)
    }
} else {
    Write-Output 'NO_TRX_DIR'
}
Write-Output ('LOG_EXISTS=' + (Test-Path (Join-Path $out 'dotnet-p14-filter.log')))
if (Test-Path (Join-Path $out 'dotnet-p14-filter.log')) {
    $t = Get-Content -LiteralPath (Join-Path $out 'dotnet-p14-filter.log') -Raw
    Write-Output ('LOG_LEN=' + $t.Length)
    Write-Output ('HAS_FAILED_BANG=' + ($t -match 'Failed!'))
    Write-Output ('HAS_PASSED_BANG=' + ($t -match 'Passed!'))
    if ($t -match 'Failed:\s+(\d+)') { Write-Output ('FAILED=' + $Matches[1]) }
    if ($t -match 'Passed:\s+(\d+)') { Write-Output ('PASSED=' + $Matches[1]) }
    if ($t -match 'Skipped:\s+(\d+)') { Write-Output ('SKIPPED=' + $Matches[1]) }
    if ($t -match 'Total:\s+(\d+)') { Write-Output ('TOTAL=' + $Matches[1]) }
}
Write-Output 'INSPECT_DONE'
