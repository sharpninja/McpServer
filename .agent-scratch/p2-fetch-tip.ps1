$ErrorActionPreference = 'Continue'
Set-Location -LiteralPath 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
Write-Output '=== FETCH ==='
git fetch origin cursor/sessionlife-p2-contracts-5cb2
Write-Output ('FETCH_EXIT=' + $LASTEXITCODE)
$sha = '987cd8f39610c9fa301862e3b1d526d23e049469'
Write-Output '=== CAT-FILE ==='
git cat-file -t $sha
Write-Output ('CAT_EXIT=' + $LASTEXITCODE)
Write-Output '=== REV-PARSE ORIGIN ==='
git rev-parse origin/cursor/sessionlife-p2-contracts-5cb2
Write-Output '=== LOG -5 ORIGIN ==='
git log -5 --oneline origin/cursor/sessionlife-p2-contracts-5cb2
Write-Output '=== CONTAINS ==='
git branch -a --contains $sha
Write-Output '=== MARKER LENGTHS ==='
$wtMarker = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\AGENTS-README-FIRST.yaml'
$rootMarker = 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml'
Write-Output ('WT_MARKER_BYTES=' + (Get-Item -LiteralPath $wtMarker).Length)
Write-Output ('ROOT_MARKER_BYTES=' + (Get-Item -LiteralPath $rootMarker).Length)
Write-Output '=== PLUGIN LIB ==='
Get-ChildItem -LiteralPath 'F:\github\mcpserver-grok-plugin\lib' -Name
