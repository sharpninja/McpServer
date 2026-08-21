#Requires -Version 7.0
Set-StrictMode -Version Latest
$workspace = 'F:\GitHub\McpServer'
$outDir = Join-Path $workspace 'docs\receipts\_hv-h0-sessionlog-remediate-001'
$paths = @(
    'docs\Project\Functional-Requirements.md'
    'docs\Project\Technical-Requirements.md'
    'docs\Project\Testing-Requirements.md'
    'docs\Project\TR-per-FR-Mapping.md'
    'docs\Project\Requirements-Matrix.md'
    'docs\plans\sessionlog-remediate-001.md'
    'src\McpServer.Support.Mcp\Program.cs'
    'plugins\core\lib-ps\repl-invoke.ps1'
)
$rows = foreach ($rel in $paths) {
    $p = Join-Path $workspace $rel
    $i = Get-Item -LiteralPath $p
    [ordered]@{
        path = $rel
        lastWriteUtc = $i.LastWriteTimeUtc.ToString('o')
        length = $i.Length
    }
}
($rows | ConvertTo-Json -Depth 5) | Set-Content -LiteralPath (Join-Path $outDir '34-filetimes.json') -Encoding utf8
$diffStat = & git -C $workspace diff --stat -- docs/Project/Functional-Requirements.md docs/Project/Technical-Requirements.md docs/Project/Testing-Requirements.md docs/Project/TR-per-FR-Mapping.md docs/Project/Requirements-Matrix.md
Set-Content -LiteralPath (Join-Path $outDir '34-req-docs-diffstat.txt') -Value ($diffStat | Out-String) -Encoding utf8
Write-Output 'FILETIMES_DONE'
