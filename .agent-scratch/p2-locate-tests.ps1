$ErrorActionPreference = 'Stop'
$out = 'F:\GitHub\McpServer\.agent-scratch\p2-locate-tests.txt'
$lines = [System.Collections.Generic.List[string]]::new()
function Out([string]$s) { $lines.Add($s) }
$wt = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
$pester = Join-Path $wt 'plugins\core\test-fixtures\pester\SessionLogP2Contracts.Tests.ps1'
$i = 0
Get-Content -LiteralPath $pester | ForEach-Object {
    $i++
    if ($_ -match 'function Assert-P2|function Initialize-P2|function Invoke-P2|function Get-P2|It [''"]|Describe |404|session_dialog|queryText|completeTurn|MCP_PLUGIN_PERSIST_LOG|recovery') {
        Out ('{0,5}|{1}' -f $i, $_)
    }
}
# TEST-002 disposition briefly
$jsonPath = Join-Path $wt 'docs\receipts\hv\hostile-validator-sessionlife-p2-20261002T234447Z.json'
$doc = Get-Content -LiteralPath $jsonPath -Raw | ConvertFrom-Json
foreach ($row in @($doc.CriterionDispositions)) {
    if ([string]$row.id -eq 'TEST-MCP-SESSIONLIFE-002-AC001') {
        Out '==== TEST-002-AC001'
        Out ('STATUS=' + $row.status)
        Out ('TEXT=' + $row.text)
        Out ('RATIONALE=' + $row.rationale)
        foreach ($m in @($row.exactExecutedMappings)) {
            if (-not $m.allPassed) {
                Out ('  FAILNAME=' + $m.name)
            }
        }
    }
}
Set-Content -LiteralPath $out -Value $lines -Encoding utf8
Write-Output ('WROTE ' + $lines.Count)
