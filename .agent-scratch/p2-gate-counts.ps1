$ErrorActionPreference = 'Stop'
$out = 'F:\GitHub\McpServer\.agent-scratch\p2-gate-counts.txt'
$log = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\docs\receipts\hv\p2-unit-legion-20261003T010420Z-gate.log'
$lines = [System.Collections.Generic.List[string]]::new()
function Out([string]$s) { $lines.Add($s) }
$i = 0
Get-Content -LiteralPath $log | ForEach-Object {
    $i++
    if ($_ -match 'Passed!|Failed!|Total tests|accepted|CheckSessionLifeUnitGate|GATE_EXIT|Pester|native-run|PluginIntegration|Build.Tests|outcome') {
        if ($i -gt ($lines.Count)) { }
        Out ('{0}|{1}' -f $i, $_.Trim())
    }
}
# keep the file smaller: only last matching-ish. rewrite with a tighter pass
$keep = [System.Collections.Generic.List[string]]::new()
$all = Get-Content -LiteralPath $log
$keep.Add('LINES=' + $all.Count)
foreach ($n in 0..($all.Count-1)) {
    $t = $all[$n]
    if ($t -match 'Passed!|Failed!|Total tests:|GATE_EXIT=|CheckSessionLifeUnitGate|accepted run|PluginIntegration|pester tests|Tests Passed') {
        $keep.Add(('{0}|{1}' -f ($n+1), $t.Trim()))
    }
}
Set-Content -LiteralPath $out -Value $keep -Encoding utf8
Write-Output ('WROTE ' + $keep.Count)
