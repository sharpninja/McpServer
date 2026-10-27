$ErrorActionPreference = 'Stop'
Set-Location 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
git diff --stat -- docs/receipts/sessionlife-completion/20260928-p0-r3/acceptance-manifest.json plugins/core/test-fixtures/pester/SessionLogLifecycle.Tests.ps1
Write-Output '==== status ===='
git status --short -- docs/receipts/sessionlife-completion/20260928-p0-r3/acceptance-manifest.json plugins/core/test-fixtures/pester/SessionLogLifecycle.Tests.ps1
Write-Output '==== HEAD ===='
git rev-parse HEAD
Write-Output '==== pester invoke hint ===='
Select-String -Path 'tools\validation\Invoke-SessionLifeUnitGate.ps1' -Pattern 'Invoke-Pester|pester' | Select-Object -First 20 | ForEach-Object { $_.Line.Trim() }
