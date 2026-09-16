$ErrorActionPreference = 'Continue'
$stamp = (Get-Date).ToUniversalTime().ToString('yyyyMMddTHHmmssZ')
$out = Join-Path 'F:\GitHub\McpServer\docs\receipts' ('_hv-b5-' + $stamp)
New-Item -ItemType Directory -Force -Path $out | Out-Null
Set-Content -LiteralPath (Join-Path $out 'stamp.txt') -Value $stamp -Encoding utf8
$meta = [ordered]@{
    stamp = $stamp
    out = $out
    tz = [TimeZoneInfo]::Local.Id
    nowUtc = (Get-Date).ToUniversalTime().ToString('o')
    nowLocal = (Get-Date).ToString('o')
}
$meta | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'meta.json') -Encoding utf8
Write-Output ('STAMP=' + $stamp)
Write-Output ('OUT=' + $out)
Write-Output ('TZ=' + [TimeZoneInfo]::Local.Id)
Write-Output ('NOW_UTC=' + $meta.nowUtc)
