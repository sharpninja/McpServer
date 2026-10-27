$ErrorActionPreference = 'Stop'
$wt = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
$jsonPath = Join-Path $wt 'docs\receipts\hv\hostile-validator-sessionlife-p2-20261002T234447Z.json'
$raw = Get-Content -LiteralPath $jsonPath -Raw
$doc = $raw | ConvertFrom-Json
Write-Output ('TOP_KEYS=' + (($doc.PSObject.Properties.Name) -join ','))
$ids = @(
    'FR-MCP-SESSIONLIFE-001-AC002',
    'TR-MCP-SESSIONLIFE-001-AC001',
    'TR-MCP-SESSIONLIFE-001-AC002',
    'TEST-MCP-SESSIONLIFE-001-AC001',
    'TEST-MCP-SESSIONLIFE-001-AC002',
    'FR-MCP-SESSIONLIFE-002-AC002',
    'TR-MCP-SESSIONLIFE-002-AC003',
    'TEST-MCP-SESSIONLIFE-002-AC001',
    'AC-FR-MCP-SESSIONLOGCTX-001-003'
)
function Show-Node($node, [string]$label) {
    if (-not $node) { Write-Output ("MISSING " + $label); return }
    Write-Output ('---- ' + $label)
    $node | ConvertTo-Json -Depth 8
}
# try common shapes
$disp = $null
foreach ($name in @('CriterionDispositions','criterionDispositions','Dispositions','criteria')) {
    if ($doc.PSObject.Properties.Name -contains $name) { $disp = $doc.$name; Write-Output ('FOUND=' + $name); break }
}
if (-not $disp) {
    Write-Output 'NO_DISP_TOP'
    # search recursively one level
    foreach ($p in $doc.PSObject.Properties) {
        $v = $p.Value
        if ($v -is [System.Management.Automation.PSCustomObject]) {
            $sub = $v.PSObject.Properties.Name
            if ($sub -contains 'CriterionDispositions' -or $sub -contains 'criterionDispositions') {
                Write-Output ('NESTED_IN=' + $p.Name)
            }
        }
    }
}
if ($disp) {
    if ($disp -is [System.Array] -or $disp -is [System.Collections.IEnumerable]) {
        foreach ($row in @($disp)) {
            $id = [string]$row.id
            if (-not $id) { $id = [string]$row.criterionId }
            if (-not $id) { $id = [string]$row.acceptanceCriterionId }
            if ($ids -contains $id) {
                Show-Node $row $id
            }
        }
    }
}
