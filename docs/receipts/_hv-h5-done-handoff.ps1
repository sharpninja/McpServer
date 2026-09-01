#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'
Set-Location -LiteralPath 'F:\GitHub\McpServer'
Write-Output ('UTC_START=' + [datetime]::UtcNow.ToString('o'))
$log = 'F:\GitHub\McpServer\docs\receipts\_hv-h5-done-handoff-isolated.txt'
$dotnetArgs = @(
    'test',
    'tests/McpServer.Support.Mcp.Tests',
    '-c', 'Debug',
    '--no-build',
    '--filter', 'FullyQualifiedName~HandoffDurabilityTests.ApproveAsync_LeaseExpiresDuringLiveCreate_SecondInstanceWins',
    '--nologo'
)
& dotnet @dotnetArgs 2>&1 | Tee-Object -FilePath $log
Write-Output ('HANDOFF_EXIT=' + $LASTEXITCODE)
Write-Output ('UTC_END=' + [datetime]::UtcNow.ToString('o'))
