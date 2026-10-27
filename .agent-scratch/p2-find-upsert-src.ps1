$ErrorActionPreference = 'Stop'
$out = 'F:\GitHub\McpServer\.agent-scratch\p2-find-upsert-src.txt'
$lines = [System.Collections.Generic.List[string]]::new()
function Out([string]$s) { $lines.Add($s) }
$wt = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
foreach ($root in @('src','plugins\core\lib-ps','tests')) {
    $dir = Join-Path $wt $root
    if (-not (Test-Path $dir)) { continue }
    $hits = Get-ChildItem -LiteralPath $dir -Recurse -Include '*.cs','*.ps1' -File -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } |
        Select-String -Pattern 'class McpPluginTurnUpsertRequest|function New-McpPluginTurnUpsertRequest' |
        Select-Object -First 20
    foreach ($h in $hits) { Out ($h.Path + ':' + $h.LineNumber + ':' + $h.Line.Trim()) }
}
Set-Content -LiteralPath $out -Value $lines -Encoding utf8
Write-Output ('WROTE ' + $lines.Count)
