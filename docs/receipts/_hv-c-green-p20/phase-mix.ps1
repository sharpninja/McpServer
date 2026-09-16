#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'
$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p20'
$root = 'F:\GitHub\McpServer'
$paths = @(
    'src/McpServer.Repl.Core/ReplCommandDispatcher.cs',
    'src/McpServer.Services/Services/HandoffIngestionService.cs',
    'tests/McpServer.Support.Mcp.Tests/Services/HandoffDurabilityTests.cs',
    'tests/Build.Tests/PluginSessionLogIntegrationTargetTests.cs',
    'tests/Build.Tests/SyncAgentPluginsChecksumTests.cs',
    'tests/McpServer.Repl.Core.Tests/SessionLogPersistenceCoordinatorIsolationTests.cs',
    'build/Build.PluginPromotion.cs',
    'build/plugin-promotion-policy.json',
    'build/Build.SyncAgentPlugins.cs',
    'docs/receipts/pluginint-p20-20260822T133039Z/summary.json'
)
$rows = foreach ($rel in $paths) {
    $full = Join-Path $root $rel
    $item = if (Test-Path -LiteralPath $full) { Get-Item -LiteralPath $full } else { $null }
    [ordered]@{
        path = $rel
        exists = [bool]$item
        lastWriteTimeUtc = if ($item) { $item.LastWriteTimeUtc.ToString('o') } else { $null }
        afterRedGate = if ($item) { $item.LastWriteTimeUtc -gt [datetime]'2026-08-22T13:24:13Z' } else { $null }
    }
}
$rows | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $out 'phase-mix-mtimes.json') -Encoding utf8
Write-Output 'PHASE_MIX_DONE'
