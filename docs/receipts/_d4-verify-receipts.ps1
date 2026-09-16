#Requires -Version 7.0
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$outDir = 'F:\GitHub\McpServer\docs\receipts\_d4-20260822T141644Z'
$files = @(
    (Join-Path $outDir '01-client-tests.log')
    (Join-Path $outDir '01-client-tests.trx')
    (Join-Path $outDir '02-support-mcp-tests.log')
    (Join-Path $outDir '02-support-mcp-tests.trx')
    'F:\GitHub\McpServer\docs\receipts\d4-handoff-gate-20260822T141644Z.md'
    'F:\GitHub\McpServer\docs\receipts\d4-handoff-gate-20260822T141644Z.json'
    (Join-Path $outDir 'session-complete.json')
)
foreach ($f in $files) {
    $exists = Test-Path -LiteralPath $f
    $sha = if ($exists) { (Get-FileHash -LiteralPath $f -Algorithm SHA256).Hash } else { 'MISSING' }
    $len = if ($exists) { (Get-Item -LiteralPath $f).Length } else { 0 }
    Write-Output (($f) + ' exists=' + $exists + ' bytes=' + $len + ' sha256=' + $sha)
}

$complete = Get-Content -LiteralPath (Join-Path $outDir 'session-complete.json') -Raw | ConvertFrom-Json
Write-Output ('COMPLETE_PAYLOAD_SNIP=' + $complete.complete.Substring(0, [Math]::Min(300, $complete.complete.Length)))
Write-Output ('TODOS=' + ($complete.todosAfter | ConvertTo-Json -Compress))
