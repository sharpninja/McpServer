$ErrorActionPreference = 'Stop'
$out = 'F:\GitHub\McpServer\.agent-scratch\p2-find-failsafe.txt'
$lines = [System.Collections.Generic.List[string]]::new()
function Out([string]$s) { $lines.Add($s) }
$repl = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\plugins\core\lib-ps\repl-invoke.ps1'
$i = 0
$capture = $false
Get-Content -LiteralPath $repl | ForEach-Object {
    $i++
    if ($_ -match 'function Write-ReplFailsafe') { $capture = $true }
    if ($capture) {
        Out ('{0,5}|{1}' -f $i, $_.TrimEnd())
        if ($i -gt 0 -and $_ -match '^function ' -and $_ -notmatch 'function Write-ReplFailsafe') { $capture = $false }
    }
}
Set-Content -LiteralPath $out -Value $lines -Encoding utf8
Write-Output ('WROTE ' + $lines.Count)
