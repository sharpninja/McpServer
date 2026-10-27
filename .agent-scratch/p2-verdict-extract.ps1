$ErrorActionPreference = 'Stop'
$md = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\docs\receipts\hv\hostile-validator-sessionlife-p2-20261002T203600Z.md'
$json = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\docs\receipts\hv\hostile-validator-sessionlife-p2-20261002T203600Z.json'
$text = [System.IO.File]::ReadAllText($md)
$idx = $text.LastIndexOf('=== VERDICT JSON ===')
Write-Output ('VERDICT_INDEX=' + $idx)
if ($idx -ge 0) {
    $tail = $text.Substring($idx)
    if ($tail.Length -gt 2500) { $tail = $tail.Substring(0, 2500) }
    Write-Output $tail
}
Write-Output '--- JSON HEAD ---'
$j = Get-Content -LiteralPath $json -Raw
if ($j.Length -gt 1500) { $j = $j.Substring(0, 1500) }
Write-Output $j
