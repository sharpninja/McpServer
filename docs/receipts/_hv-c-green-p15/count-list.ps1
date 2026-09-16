#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'
$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p15'
$listLog = Join-Path $out 'hv-dotnet-list-tests.log'
$lt = Get-Content -LiteralPath $listLog
[ordered]@{
    totalListed = @($lt | Where-Object { $_ -match '^\s{2,}' -and $_ -match '\.' }).Count
    uniqueNonEmpty = @($lt | Where-Object { $_.Trim() }).Count
    p14Count = @($lt | Where-Object { $_ -match 'PluginRootOverride_WritesOnlyWorkspaceMcpServerAgent_PoisonEmpty' }).Count
    p15Success = @($lt | Where-Object { $_ -match 'Success_NoPendingFailsafe' }).Count
    p15FailedSubmit = @($lt | Where-Object { $_ -match 'FailedSubmit_RetainsRootIdPending' }).Count
    p15Retry = @($lt | Where-Object { $_ -match 'RetrySuccess_DeletesOnlyMatchingPending' }).Count
    p16AiTheory = @($lt | Where-Object { $_ -match 'AiTheory_' }).Count
    skipListed = @($lt | Where-Object { $_ -match '\[SKIP\]|Skipped' }).Count
    p15Lines = @($lt | Where-Object { $_ -match 'Success_NoPendingFailsafe|FailedSubmit_RetainsRootIdPending|RetrySuccess_DeletesOnlyMatchingPending' } | ForEach-Object { $_.Trim() })
} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $out 'list-tests-summary.json') -Encoding utf8
Write-Output ((Get-Content -LiteralPath (Join-Path $out 'list-tests-summary.json') -Raw))
