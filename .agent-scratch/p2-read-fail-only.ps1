$ErrorActionPreference = 'Stop'
$out = 'F:\GitHub\McpServer\.agent-scratch\p2-fail-only.txt'
# Re-extract from json more tightly: status, text, rationale, and only failing exact names
$wt = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
$jsonPath = Join-Path $wt 'docs\receipts\hv\hostile-validator-sessionlife-p2-20261002T234447Z.json'
$doc = Get-Content -LiteralPath $jsonPath -Raw | ConvertFrom-Json
$lines = [System.Collections.Generic.List[string]]::new()
function Out([string]$s) { $lines.Add($s) }
$want = @(
    'FR-MCP-SESSIONLIFE-001-AC002',
    'TR-MCP-SESSIONLIFE-001-AC001',
    'TR-MCP-SESSIONLIFE-001-AC002',
    'TEST-MCP-SESSIONLIFE-001-AC001',
    'TEST-MCP-SESSIONLIFE-001-AC002'
)
foreach ($row in @($doc.CriterionDispositions)) {
    if ($want -notcontains [string]$row.id) { continue }
    Out ('==== ' + $row.id + ' STATUS=' + $row.status)
    Out ('TEXT=' + $row.text)
    Out ('RATIONALE=' + $row.rationale)
    foreach ($m in @($row.exactExecutedMappings)) {
        Out ('  MAP added=' + $m.added + ' allPassed=' + $m.allPassed)
        Out ('  NAME=' + $m.name)
    }
    Out ''
}
Out '==== NAME RECONCILIATIONS'
Out ($doc.NameReconciliations | ConvertTo-Json -Depth 6 -Compress)
Out '==== CACHE COUNTEREXAMPLE'
Out ($doc.CacheAssertionCounterexample | ConvertTo-Json -Depth 6)
Out '==== COMPLETENESS'
Out ($doc.CompletenessCalculation | ConvertTo-Json -Depth 5)
Set-Content -LiteralPath $out -Value $lines -Encoding utf8
Write-Output ('WROTE ' + $lines.Count)
