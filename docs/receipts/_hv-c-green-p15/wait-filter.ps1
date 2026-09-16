#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'
$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p15'
$deadline = [DateTime]::UtcNow.AddMinutes(12)
while ([DateTime]::UtcNow -lt $deadline) {
    $exitFile = Join-Path $out 'hv-dotnet-p15-filter-exit.json'
    $trx = Join-Path $out 'trx-p15-hv\p15-filter.trx'
    $log = Join-Path $out 'hv-dotnet-p15-filter.log'
    if ((Test-Path -LiteralPath $exitFile) -and (Test-Path -LiteralPath $trx)) {
        Write-Output ("FILTER_READY " + [DateTime]::UtcNow.ToString('o'))
        Get-Content -LiteralPath $exitFile
        break
    }
    $len = 0
    $lw = $null
    if (Test-Path -LiteralPath $log) {
        $i = Get-Item -LiteralPath $log
        $len = $i.Length
        $lw = $i.LastWriteTimeUtc.ToString('o')
    }
    Write-Output ("WAIT filter exit missing logLen=$len logUtc=$lw now=" + [DateTime]::UtcNow.ToString('o'))
    Start-Sleep -Seconds 20
}
if (-not (Test-Path -LiteralPath (Join-Path $out 'hv-dotnet-p15-filter-exit.json'))) {
    Write-Output 'FILTER_NOT_READY'
    exit 2
}
