#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p20'
$temp = 'C:\Users\kingd\AppData\Local\Temp\grok-goal-678ba5b2f579\implementer\p20-green.trx'
$copied = 'F:\GitHub\McpServer\docs\receipts\pluginint-p20-20260822T133039Z\PluginUpdateServiceHarnessTests.trx'
function Get-Sha([string]$path) {
    if (-not (Test-Path -LiteralPath $path)) { return $null }
    return (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
}
[ordered]@{
    tempExists = Test-Path -LiteralPath $temp
    copiedExists = Test-Path -LiteralPath $copied
    tempSha256 = Get-Sha $temp
    copiedSha256 = Get-Sha $copied
    hashesEqual = ((Get-Sha $temp) -eq (Get-Sha $copied))
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'trx-hash.json') -Encoding utf8
Write-Output 'TRX_HASH_DONE'
