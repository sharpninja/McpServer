$ErrorActionPreference = 'Stop'
$out = 'F:\GitHub\McpServer\.agent-scratch\p2-find-raw.txt'
$lines = [System.Collections.Generic.List[string]]::new()
function Out([string]$s) { $lines.Add($s) }
$repl = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\plugins\core\lib-ps\repl-invoke.ps1'
$i = 0
$capture = $false
$end = 0
Get-Content -LiteralPath $repl | ForEach-Object {
    $i++
    if ($_ -match '^function Invoke-ReplRaw ') { $capture = $true; $end = $i + 70 }
    if ($capture -and $i -le $end) { Out ('{0,5}|{1}' -f $i, $_.TrimEnd()) }
}
Set-Content -LiteralPath $out -Value $lines -Encoding utf8
Write-Output ('WROTE ' + $lines.Count)
