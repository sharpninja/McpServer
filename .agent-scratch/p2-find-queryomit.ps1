$ErrorActionPreference = 'Stop'
$out = 'F:\GitHub\McpServer\.agent-scratch\p2-find-queryomit.txt'
$lines = [System.Collections.Generic.List[string]]::new()
function Out([string]$s) { $lines.Add($s) }
$wt = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
$hits = Get-ChildItem -LiteralPath (Join-Path $wt 'plugins\core\lib-ps') -Filter '*.ps1' | Select-String -Pattern 'function New-McpPluginTurnUpsertRequest|QueryText' | Select-Object -First 40
foreach ($h in $hits) {
    Out ('{0}:{1}:{2}' -f $h.Filename, $h.LineNumber, $h.Line.Trim())
}
Set-Content -LiteralPath $out -Value $lines -Encoding utf8
Write-Output ('WROTE ' + $lines.Count)
