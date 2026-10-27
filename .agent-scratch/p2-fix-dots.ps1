$ErrorActionPreference = 'Stop'
$out = 'F:\GitHub\McpServer\.agent-scratch\p2-fix-dots.txt'
$lines = [System.Collections.Generic.List[string]]::new()
function Out([string]$s) { $lines.Add($s) }
$manifest = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\docs\receipts\sessionlife-completion\20260928-p0-r3\acceptance-manifest.json'
$raw = [System.IO.File]::ReadAllText($manifest)
$before = ([regex]::Matches($raw, 'outcomes\.\.')).Count
Out ("BEFORE=$before")
if ($before -lt 1) { throw 'no double-dot names found' }
$updated = $raw.Replace('outcomes..', 'outcomes.')
$after = ([regex]::Matches($updated, 'outcomes\.\.')).Count
Out ("AFTER=$after")
if ($after -ne 0) { throw 'double-dot names remain' }
# JSON must still parse and acceptance states stay not-accepted
$doc = $updated | ConvertFrom-Json
$accepted = 0
$rows = @($doc.requirementAcceptanceCoverage)
if ($rows.Count -eq 0 -and $doc.PSObject.Properties.Name -contains 'requirements') {
    $rows = @($doc.requirements)
}
Out ("TOP=" + (($doc.PSObject.Properties.Name) -join ','))
function Walk($node) {
    if ($null -eq $node) { return }
    if ($node -is [System.Collections.IEnumerable] -and $node -isnot [string]) {
        foreach ($item in $node) { Walk $item }
        return
    }
    if ($node -is [System.Management.Automation.PSCustomObject]) {
        if ($node.PSObject.Properties.Name -contains 'acceptanceState') {
            $script:stateCount++
            if ([string]$node.acceptanceState -eq 'accepted') { $script:accepted++ }
        }
        foreach ($p in $node.PSObject.Properties) { Walk $p.Value }
    }
}
$script:stateCount = 0
$script:accepted = 0
Walk $doc
Out ("STATE_COUNT=$script:stateCount")
Out ("ACCEPTED=$script:accepted")
if ($script:accepted -ne 0) { throw 'accepted state appeared' }
$utf8 = New-Object System.Text.UTF8Encoding $false
[System.IO.File]::WriteAllText($manifest, $updated, $utf8)
Out 'WROTE_MANIFEST'
Set-Content -LiteralPath $out -Value $lines -Encoding utf8
Write-Output ($lines -join [Environment]::NewLine)
