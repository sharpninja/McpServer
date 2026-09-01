#Requires -Version 7.0
[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$u = [datetime]::UtcNow
$obj = [ordered]@{
    Stamp = $u.ToString('yyyyMMddTHHmmssZ')
    Iso = $u.ToString('yyyy-MM-ddTHH:mm:ssZ')
}
$obj | ConvertTo-Json | Set-Content -LiteralPath 'F:\GitHub\McpServer\docs\receipts\_hv-s2-resume\13-stamp.json' -Encoding utf8
Write-Output $obj.Stamp
