$ErrorActionPreference = 'Continue'
$out = 'F:\GitHub\McpServer\docs\receipts\_hv-b5-20260821T224147Z'
$canonicalDir = 'F:\GitHub\McpServer\plugins\core\lib-ps'
$githubRoot = 'F:\GitHub'
$official = @(
    'mcpserver-codex-plugin',
    'mcpserver-claude-code-plugin',
    'mcpserver-copilot-plugin',
    'mcpserver-cline-plugin',
    'mcpserver-grok-plugin'
)
$mismatches = @()
$comparisons = @()
foreach ($plugin in $official) {
    $pluginRoot = Join-Path $githubRoot $plugin
    $canonicalFiles = @(Get-ChildItem -LiteralPath $canonicalDir -File | Where-Object { $_.Name -ne 'GAPS.md' })
    foreach ($cf in $canonicalFiles) {
        $c1 = Join-Path $pluginRoot (Join-Path 'lib' $cf.Name)
        $c2 = Join-Path $pluginRoot (Join-Path 'lib-ps' $cf.Name)
        $copy = $null
        if (Test-Path -LiteralPath $c1) { $copy = $c1 }
        elseif (Test-Path -LiteralPath $c2) { $copy = $c2 }
        if ($null -eq $copy) {
            $mismatches += ($plugin + ': missing ' + $cf.Name)
            $comparisons += @{ plugin = $plugin; file = $cf.Name; status = 'missing' }
            continue
        }
        $h1 = (Get-FileHash -LiteralPath $cf.FullName -Algorithm SHA256).Hash
        $h2 = (Get-FileHash -LiteralPath $copy -Algorithm SHA256).Hash
        $match = $h1 -eq $h2
        if (-not $match) { $mismatches += ($plugin + ': drift ' + $cf.Name) }
        $comparisons += @{
            plugin = $plugin
            file = $cf.Name
            canonical = $h1
            copy = $h2
            path = $copy
            match = $match
        }
    }
}
$result = @{
    mismatchCount = $mismatches.Count
    mismatches = $mismatches
    comparisons = $comparisons
}
$result | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $out 'plugin-checksums.json') -Encoding utf8
Write-Output ('CHECKSUM_MISMATCHES=' + $mismatches.Count)
if ($mismatches.Count -gt 0) { $mismatches | ForEach-Object { Write-Output $_ } }
