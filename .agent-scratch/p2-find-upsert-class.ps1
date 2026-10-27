$ErrorActionPreference = 'Stop'
$out = 'F:\GitHub\McpServer\.agent-scratch\p2-find-upsert-class.txt'
$lines = [System.Collections.Generic.List[string]]::new()
function Out([string]$s) { $lines.Add($s) }
$wt = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
$hits = Get-ChildItem -LiteralPath $wt -Recurse -Include '*.cs','*.ps1' -File -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notmatch '\\(bin|obj|TestResults|\.git)\\' } |
    Select-String -Pattern 'class McpPluginTurnUpsertRequest|QueryText' -List |
    Select-Object -First 30
foreach ($h in $hits) {
    Out ($h.Path.Substring($wt.Length) + ':' + $h.LineNumber)
}
Set-Content -LiteralPath $out -Value $lines -Encoding utf8
Write-Output ('WROTE ' + $lines.Count)
