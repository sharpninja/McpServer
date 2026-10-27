$ErrorActionPreference = 'Stop'
Write-Output ('HOSTNAME=' + $env:COMPUTERNAME)
Write-Output ('UTC=' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ'))
$wt = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
$root = 'F:\GitHub\McpServer'
$plugin = 'F:\github\mcpserver-grok-plugin'
Write-Output ('WORKTREE_EXISTS=' + (Test-Path -LiteralPath $wt))
Write-Output ('ROOT_EXISTS=' + (Test-Path -LiteralPath $root))
Write-Output ('PLUGIN_EXISTS=' + (Test-Path -LiteralPath $plugin))
if (Test-Path -LiteralPath $wt) {
    Set-Location -LiteralPath $wt
    Write-Output ('WT_HEAD=' + (git rev-parse HEAD))
    Write-Output ('WT_BRANCH=' + (git rev-parse --abbrev-ref HEAD))
    git status --short --branch
}
if (Test-Path -LiteralPath $root) {
    Push-Location -LiteralPath $root
    Write-Output ('ROOT_HEAD=' + (git rev-parse HEAD))
    Write-Output ('ROOT_BRANCH=' + (git rev-parse --abbrev-ref HEAD))
    Pop-Location
}
if (Test-Path -LiteralPath $plugin) {
    Write-Output 'PLUGIN_TOP='
    Get-ChildItem -LiteralPath $plugin -Name | Select-Object -First 50
}
$markerCandidates = @(
    (Join-Path $wt 'AGENTS-README-FIRST.yaml'),
    (Join-Path $root 'AGENTS-README-FIRST.yaml')
)
foreach ($m in $markerCandidates) {
    Write-Output ('MARKER=' + $m + ' EXISTS=' + (Test-Path -LiteralPath $m))
}
