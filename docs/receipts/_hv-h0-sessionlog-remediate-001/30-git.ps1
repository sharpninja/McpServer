#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'
$workspace = 'F:\GitHub\McpServer'
$outDir = Join-Path $workspace 'docs\receipts\_hv-h0-sessionlog-remediate-001'
Set-Location -LiteralPath $workspace

$gitStatus = & git status --short --untracked-files=all -- docs/plans/sessionlog-remediate-001.md plugins/core/lib-ps/repl-invoke.ps1 src/McpServer.Support.Mcp 2>&1 | Out-String
Set-Content -LiteralPath (Join-Path $outDir '30-git-status-scoped.txt') -Value $gitStatus -Encoding utf8

$gitHead = (& git rev-parse HEAD 2>&1 | Out-String).Trim()
Set-Content -LiteralPath (Join-Path $outDir '31-git-head.txt') -Value $gitHead -Encoding utf8

$diffRepl = & git diff --stat HEAD -- plugins/core/lib-ps/repl-invoke.ps1 src/McpServer.Support.Mcp/Services src/McpServer.Support.Mcp/Controllers src/McpServer.Support.Mcp/Program.cs 2>&1 | Out-String
Set-Content -LiteralPath (Join-Path $outDir '32-git-diff-stat-product.txt') -Value $diffRepl -Encoding utf8

$diffFull = & git diff --name-only HEAD 2>&1 | Out-String
Set-Content -LiteralPath (Join-Path $outDir '32-git-diff-name-only.txt') -Value $diffFull -Encoding utf8

$untracked = & git ls-files --others --exclude-standard -- plugins/core/lib-ps src/McpServer.Support.Mcp tests 2>&1 | Out-String
Set-Content -LiteralPath (Join-Path $outDir '32-git-untracked-src.txt') -Value $untracked -Encoding utf8

$planPath = Join-Path $workspace 'docs\plans\sessionlog-remediate-001.md'
$planExists = Test-Path -LiteralPath $planPath
$rcHits = @()
if ($planExists) {
    $rcHits = @(Select-String -LiteralPath $planPath -Pattern 'RC[1-6]' | ForEach-Object { $_.LineNumber.ToString() + ':' + $_.Line.Trim() })
}
($rcHits | ConvertTo-Json -Depth 5) | Set-Content -LiteralPath (Join-Path $outDir '41-plan-rc-lines.json') -Encoding utf8
@{ exists = $planExists; length = (Get-Item $planPath).Length; lastWriteUtc = (Get-Item $planPath).LastWriteTimeUtc.ToString('o') } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $outDir '40-plan-exists.json') -Encoding utf8

Write-Output "GIT_DONE head=$gitHead exists=$planExists rcCount=$($rcHits.Count)"
