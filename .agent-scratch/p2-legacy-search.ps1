$ErrorActionPreference = 'Stop'
$wt = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
Write-Output '==== CONSUMER FACTS ===='
rg -n 'public (async )?(void|Task)' (Join-Path $wt 'tests\Build.Tests\SessionLifeUnitGateConsumerTests.cs')
Write-Output '==== APPEND DIALOG TESTS ===='
rg -n -g '*.cs' 'AppendDialog' (Join-Path $wt 'tests') | Select-Object -First 40
Write-Output '==== PESTER FAILSAFE / NOT_FOUND ===='
rg -n -g '*.ps1' 'not_found|failsafe|queued|AppendDialogAsync' (Join-Path $wt 'plugins\core\test-fixtures\pester') | Select-Object -First 40
