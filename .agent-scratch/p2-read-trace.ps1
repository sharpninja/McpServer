$ErrorActionPreference = 'Stop'
$p = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\docs\receipts\hv\20261002T203600Z-trace-audit.json'
$raw = Get-Content -LiteralPath $p -Raw
Write-Output ('BYTES=' + $raw.Length)
$j = $raw | ConvertFrom-Json
Write-Output ('TYPE=' + $j.GetType().FullName)
if ($j -is [System.Collections.IDictionary] -or $j.PSObject) {
    $j.PSObject.Properties.Name | ForEach-Object { Write-Output ('PROP=' + $_) }
}
