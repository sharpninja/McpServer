#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'
Set-Location 'F:\GitHub\McpServer'
$sha = 'bd8a8d9e8cc3221bd25e7ce29479b460bc21b19e'
Write-Output ('SHA=' + $sha)
git cat-file -t $sha
Write-Output '--- show strategy ---'
git show "${sha}:src/McpServer.Services/Services/GrokCliAgentExecutionStrategy.cs" | Select-String -Pattern 'HighestEffort|--effort|--reasoning-effort'
Write-Output '--- log ---'
git log -1 --format='%H %cI %s' $sha
Write-Output ('HEAD=' + (git rev-parse HEAD))
git merge-base --is-ancestor $sha HEAD
Write-Output ('ancestor_exit=' + $LASTEXITCODE)
