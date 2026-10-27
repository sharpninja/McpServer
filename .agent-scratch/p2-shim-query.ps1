$ErrorActionPreference = 'Stop'
$out = 'F:\GitHub\McpServer\.agent-scratch\p2-shim-query.txt'
$lines = [System.Collections.Generic.List[string]]::new()
function Out([string]$s) { $lines.Add($s) }
$shim = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\plugins\core\lib-ps\McpPluginShim.psm1'
$i = 0
$start = 0
Get-Content -LiteralPath $shim | ForEach-Object {
    $i++
    if ($_ -match 'function New-McpPluginTurnUpsertRequest') { $start = $i; Out ('START ' + $i) }
    if ($start -gt 0 -and $i -ge $start -and $i -lt ($start + 180)) {
        if ($_ -match 'QueryText|queryText|function |Contains|status|sessionId|codeEdits|audit') {
            Out ('{0,5}|{1}' -f $i, $_.TrimEnd())
        }
    }
}
Set-Content -LiteralPath $out -Value $lines -Encoding utf8
Write-Output ('WROTE ' + $lines.Count)
