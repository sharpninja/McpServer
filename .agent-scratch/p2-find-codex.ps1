$ErrorActionPreference = 'Continue'
Write-Output '=== CODEX COMMAND ==='
$codex = Get-Command codex -ErrorAction SilentlyContinue
if ($codex) { Write-Output ($codex.Source) } else { Write-Output 'CODEX_MISSING' }
$hv = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\docs\receipts\hv'
Write-Output '=== PS1 CODEX HITS ==='
Get-ChildItem -LiteralPath $hv -Filter '*.ps1' -File |
    Select-String -Pattern 'codex exec|gpt-6-astra' |
    Select-Object -First 20 |
    ForEach-Object { $_.Filename + ':' + $_.LineNumber + ':' + $_.Line.Trim().Substring(0, [Math]::Min(180, $_.Line.Trim().Length)) }
Write-Output '=== SCRATCH CODEX ==='
$scratch = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\.agent-scratch'
if (Test-Path $scratch) {
    Get-ChildItem -LiteralPath $scratch -File -ErrorAction SilentlyContinue |
        Select-Object -First 40 |
        ForEach-Object { $_.Name }
}
