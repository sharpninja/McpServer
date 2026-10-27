$ErrorActionPreference = 'Stop'
$wt = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
$files = @(
    'tests\McpServer.Support.Mcp.Tests\Services\SessionLogServiceTurnContextTests.cs',
    'tests\McpServer.Support.Mcp.Tests\Services\SessionLogTurnContextValidatorTests.cs',
    'tests\Build.Tests\SessionLifeUnitGateValidatorTests.cs',
    'tests\Build.Tests\SessionLifeUnitGateConsumerTests.cs'
)
foreach ($rel in $files) {
    Write-Output ('==== ' + $rel)
    rg -n '^\s*(public|private).*\(|\[Fact\]|\[Theory\]' (Join-Path $wt $rel)
}
Write-Output '==== SEARCH LEGACY BEHAVIOR ===='
rg -n -g '*.cs' 'AppendDialogAsync|not_found|None' (Join-Path $wt 'tests') | Select-Object -First 80
