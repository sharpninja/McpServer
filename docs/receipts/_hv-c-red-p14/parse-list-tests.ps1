#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p14'
$listLog = Join-Path $out 'dotnet-list-tests.log'
if (-not (Test-Path -LiteralPath $listLog)) {
    [ordered]@{ exists = $false } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'list-tests-summary.json') -Encoding utf8
    Write-Output 'LIST_LOG_MISSING'
    exit 1
}
$lt = Get-Content -LiteralPath $listLog
$names = @($lt | Where-Object { $_ -match '^\s+McpServer\.PluginIntegration\.Tests\.' } | ForEach-Object { $_.Trim() })
$p14 = @($names | Where-Object { $_ -match 'PluginRootOverride_WritesOnlyWorkspaceMcpServerAgent_PoisonEmpty' })
$hostKinds = @('Codex', 'ClaudeCode', 'ClaudeCowork', 'Copilot', 'Grok', 'Cline', 'ClineV2', 'OpenCode')
$p14ByHost = foreach ($h in $hostKinds) {
    $needle = "hostKind: $h)"
    [ordered]@{
        HostKind = $h
        Count = @($p14 | Where-Object { $_ -match [regex]::Escape($needle) }).Count
    }
}
[ordered]@{
    exists = $true
    totalListed = $names.Count
    p14Count = $p14.Count
    p14Names = $p14
    p14ByHost = @($p14ByHost)
    p15Success = @($names | Where-Object { $_ -match 'Success_NoPendingFailsafe' }).Count
    p15FailedSubmit = @($names | Where-Object { $_ -match 'FailedSubmit_RetainsRootIdPending' }).Count
    p15Retry = @($names | Where-Object { $_ -match 'RetrySuccess_DeletesOnlyMatchingPending' }).Count
    p16AiTheory = @($names | Where-Object { $_ -match 'AiTheory_' }).Count
    adapterClassCount = @($names | Where-Object { $_ -match 'PluginSessionLogWorkflowAdapterTests' }).Count
    skipListed = @($names | Where-Object { $_ -match '\[SKIP' -or $_ -match 'skipped' }).Count
} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'list-tests-summary.json') -Encoding utf8
Write-Output ('TOTAL_LISTED=' + $names.Count)
Write-Output ('P14=' + $p14.Count)
Write-Output ('P15_SUCCESS=' + @($names | Where-Object { $_ -match 'Success_NoPendingFailsafe' }).Count)
Write-Output ('P15_FAILSUBMIT=' + @($names | Where-Object { $_ -match 'FailedSubmit_RetainsRootIdPending' }).Count)
Write-Output ('P15_RETRY=' + @($names | Where-Object { $_ -match 'RetrySuccess_DeletesOnlyMatchingPending' }).Count)
Write-Output ('P16_AITHEORY=' + @($names | Where-Object { $_ -match 'AiTheory_' }).Count)
Write-Output 'PARSE_LIST_DONE'
