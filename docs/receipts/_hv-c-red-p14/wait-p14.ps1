#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p14'
$deadline = [DateTime]::UtcNow.AddMinutes(15)
$target = 50072

function Get-PluginIntHosts {
    @(Get-CimInstance Win32_Process | Where-Object {
        $_.Name -match 'testhost|dotnet' -and [string]$_.CommandLine -match 'PluginIntegration|PluginRootOverride'
    })
}

Write-Output ("WAIT_START=" + [DateTime]::UtcNow.ToString('o'))
while ([DateTime]::UtcNow -lt $deadline) {
    $alive = Get-Process -Id $target -ErrorAction SilentlyContinue
    $hosts = Get-PluginIntHosts
    Write-Output ("UTC=" + [DateTime]::UtcNow.ToString('o') + " TARGET_ALIVE=" + [bool]$alive + " HOST_COUNT=" + @($hosts).Count)
    if (-not $alive) { break }
    Start-Sleep -Seconds 15
}

$hostsAfter = Get-PluginIntHosts
[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    TargetPid = $target
    TargetAlive = [bool](Get-Process -Id $target -ErrorAction SilentlyContinue)
    HostCount = @($hostsAfter).Count
    Hosts = @($hostsAfter | ForEach-Object {
        [ordered]@{
            ProcessId = $_.ProcessId
            Name = $_.Name
            CommandLine = [string]$_.CommandLine
        }
    })
} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'wait-p14-hosts.json') -Encoding utf8

$trx = Join-Path $out 'trx-p14\p14-filter.trx'
$log = Join-Path $out 'dotnet-p14-filter.log'
Write-Output ("TRX_EXISTS=" + (Test-Path -LiteralPath $trx))
if (Test-Path -LiteralPath $trx) {
    $item = Get-Item -LiteralPath $trx
    Write-Output ("TRX_LEN=" + $item.Length + " TRX_MTIME=" + $item.LastWriteTimeUtc.ToString('o'))
}
Write-Output ("LOG_EXISTS=" + (Test-Path -LiteralPath $log))
if (Test-Path -LiteralPath $log) {
    $item = Get-Item -LiteralPath $log
    Write-Output ("LOG_LEN=" + $item.Length + " LOG_MTIME=" + $item.LastWriteTimeUtc.ToString('o'))
    $t = Get-Content -LiteralPath $log -Raw
    Write-Output ("HAS_FAILED_BANG=" + ($t -match 'Failed!'))
    Write-Output ("HAS_PASSED_BANG=" + ($t -match 'Passed!'))
    if ($t -match 'Failed:\s+(\d+)') { Write-Output ('FAILED=' + $Matches[1]) }
    if ($t -match 'Passed:\s+(\d+)') { Write-Output ('PASSED=' + $Matches[1]) }
    if ($t -match 'Skipped:\s+(\d+)') { Write-Output ('SKIPPED=' + $Matches[1]) }
    if ($t -match 'Total:\s+(\d+)') { Write-Output ('TOTAL=' + $Matches[1]) }
    Write-Output ('REJECT_MSG_COUNT=' + ([regex]::Matches($t, 'must reject PLUGIN_ROOT_OVERRIDE')).Count)
}
Write-Output ("WAIT_END=" + [DateTime]::UtcNow.ToString('o'))
Write-Output 'WAIT_DONE'
