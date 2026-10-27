$ErrorActionPreference = 'Stop'
$out = 'F:\GitHub\McpServer\.agent-scratch\p2-find-product.txt'
$lines = [System.Collections.Generic.List[string]]::new()
function Out([string]$s) { $lines.Add($s) }
$wt = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
$repl = Join-Path $wt 'plugins\core\lib-ps\repl-invoke.ps1'
$hook = Join-Path $wt 'plugins\core\lib-ps\plugin-hook.ps1'
$pester = Join-Path $wt 'plugins\core\test-fixtures\pester\SessionLogP2Contracts.Tests.ps1'
function Scan([string]$path, [string]$pattern) {
    Out ('## ' + (Split-Path $path -Leaf) + ' / ' + $pattern)
    $i = 0
    Get-Content -LiteralPath $path | ForEach-Object {
        $i++
        if ($_ -match $pattern) { Out ('{0,5}|{1}' -f $i, $_.TrimEnd()) }
    }
}
Scan $pester 'function Write-P2Turn|function New-P2Layout|auditActions|contextList|lastBuildStatus|codeEdits'
Scan $repl '404|session_dialog|Deny-ReplMissingCurrentTurn|queryText'
Scan $hook 'stop-gate|completeTurn|Close-PluginTurn'
Set-Content -LiteralPath $out -Value $lines -Encoding utf8
Write-Output ('WROTE ' + $lines.Count)
