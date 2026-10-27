$ErrorActionPreference = 'Stop'
$out = 'F:\GitHub\McpServer\.agent-scratch\p2-find-newmcp.txt'
$lines = [System.Collections.Generic.List[string]]::new()
function Out([string]$s) { $lines.Add($s) }
$wt = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
$root = Join-Path $wt 'plugins'
$hits = Get-ChildItem -LiteralPath $root -Recurse -Filter '*.ps1' -File -ErrorAction SilentlyContinue |
    Select-String -Pattern 'function New-McpPluginTurnUpsertRequest' |
    Select-Object -First 10
foreach ($h in $hits) { Out ($h.Path + ':' + $h.LineNumber) }
Set-Content -LiteralPath $out -Value $lines -Encoding utf8
Write-Output ('WROTE ' + $lines.Count)
