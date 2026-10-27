$ErrorActionPreference = 'Stop'
$out = 'F:\GitHub\McpServer\.agent-scratch\p2-find-persist.txt'
$lines = [System.Collections.Generic.List[string]]::new()
function Out([string]$s) { $lines.Add($s) }
$wt = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
$repl = Join-Path $wt 'plugins\core\lib-ps\repl-invoke.ps1'
$i = 0
Get-Content -LiteralPath $repl | ForEach-Object {
    $i++
    if ($_ -match 'function Invoke-ReplPersistTurn|function Update-ReplTurnCacheStatus|function Test-ReplSessionVerbQueued|function New-McpPluginTurnUpsertRequest|QueryText|degraded') {
        if ($i -gt 1350 -and $i -lt 1750) { Out ('{0,5}|{1}' -f $i, $_.TrimEnd()) }
        elseif ($_ -match 'function ') { Out ('{0,5}|{1}' -f $i, $_.TrimEnd()) }
    }
}
Set-Content -LiteralPath $out -Value $lines -Encoding utf8
Write-Output ('WROTE ' + $lines.Count)
