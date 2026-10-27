$ErrorActionPreference = 'Continue'
$wt = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
Set-Location -LiteralPath $wt
Write-Output '=== DIFF STAT BEFORE RESET ==='
git diff --stat
$sha = '987cd8f39610c9fa301862e3b1d526d23e049469'
Write-Output '=== RESET HARD ==='
git reset --hard $sha
Write-Output ('RESET_EXIT=' + $LASTEXITCODE)
Write-Output ('HEAD=' + (git rev-parse HEAD))
Write-Output ('BRANCH=' + (git rev-parse --abbrev-ref HEAD))
Write-Output '=== STATUS SHORT ==='
git status --short --branch | Select-Object -First 30
Write-Output '=== PLUGIN HEALTH HELPERS ==='
Select-String -Path 'F:\github\mcpserver-grok-plugin\lib\*.ps1' -Pattern 'function (Test-Marker|Invoke-Mcp|Get-Health|Test-Nonce)' |
    ForEach-Object { $_.Filename + ':' + $_.LineNumber + ':' + $_.Line.Trim() }
Write-Output '=== SESSION START FILE ==='
Get-ChildItem -LiteralPath 'F:\github\mcpserver-grok-plugin' -Recurse -Filter 'session-start.ps1' -ErrorAction SilentlyContinue |
    ForEach-Object { $_.FullName }
