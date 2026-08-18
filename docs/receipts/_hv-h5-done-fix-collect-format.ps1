#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. 'F:\GitHub\McpServer\plugins\core\lib-ps\yaml-object-mutation.ps1'
# Not YAML. Mutate the collect script as text via object-safe replace of the known format string only.
$path = 'F:\GitHub\McpServer\docs\receipts\_hv-h5-done-collect.ps1'
$text = [System.IO.File]::ReadAllText($path)
$old = "[datetime]::UtcNow.ToString('yyyyMMddTHHMMSSZ')"
$new = "[datetime]::UtcNow.ToString('yyyyMMddTHHmmssZ')"
if (-not $text.Contains($old)) { throw 'format string not found' }
$text = $text.Replace($old, $new)
[System.IO.File]::WriteAllText($path, $text)
Write-Output 'COLLECT_FORMAT_FIXED'
