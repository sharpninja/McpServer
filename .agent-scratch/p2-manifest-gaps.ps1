$ErrorActionPreference = 'Stop'
$manifestPath = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\docs\receipts\sessionlife-completion\20260928-p0-r3\acceptance-manifest.json'
$m = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
Write-Output 'ROOT PROPS:'
$m.PSObject.Properties.Name | ForEach-Object { Write-Output $_ }
$want = @(
    'FR-MCP-SESSIONLIFE-002-AC002',
    'TR-MCP-SESSIONLIFE-002-AC003',
    'TEST-MCP-SESSIONLIFE-002-AC001',
    'TEST-MCP-SESSIONLIFE-002-AC004',
    'FR-MCP-170-AC002',
    'FR-MCP-170-AC003',
    'AC-FR-MCP-SESSIONLOGCTX-001-002',
    'AC-FR-MCP-SESSIONLOGCTX-001-003',
    'FR-MCP-REPL-009-AC001',
    'FR-MCP-REPL-009-AC002',
    'FR-MCP-REPL-009-AC003',
    'FR-MCP-REPL-009-AC004'
)
function Walk($node, $path) {
    if ($null -eq $node) { return }
    if ($node -is [System.Management.Automation.PSCustomObject]) {
        $id = $null
        foreach ($n in @('acceptanceCriterionId','id','criterionId','acId')) {
            if ($node.PSObject.Properties.Name -contains $n) { $id = [string]$node.$n; break }
        }
        if ($id -and ($want -contains $id)) {
            Write-Output ('==== PATH ' + $path + ' ID ' + $id)
            foreach ($p in $node.PSObject.Properties) {
                $text = if ($p.Value -is [string]) { $p.Value } else { ($p.Value | ConvertTo-Json -Compress -Depth 8) }
                if ($null -eq $text) { $text = '' }
                if ($text.Length -gt 1200) { $text = $text.Substring(0, 1200) }
                Write-Output ($p.Name + '=' + $text)
            }
        }
        foreach ($p in $node.PSObject.Properties) { Walk $p.Value ($path + '/' + $p.Name) }
    } elseif ($node -is [System.Array] -or $node -is [System.Collections.IList]) {
        for ($i = 0; $i -lt $node.Count; $i++) { Walk $node[$i] ($path + "[$i]") }
    }
}
Walk $m 'root'
