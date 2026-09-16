#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'
$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p14'
$lt = Get-Content -LiteralPath (Join-Path $out 'hv-dotnet-list-tests.log')
$fqn = @($lt | Where-Object { $_ -match '^\s+McpServer\.PluginIntegration\.Tests\.' })
$obj = [ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    Stamp = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
    FqnCount = $fqn.Count
    P14 = @($fqn | Where-Object { $_ -match 'PluginRootOverride_WritesOnlyWorkspaceMcpServerAgent_PoisonEmpty' }).Count
    P15 = @($fqn | Where-Object { $_ -match 'Success_NoPendingFailsafe|FailedSubmit_RetainsRootIdPending|RetrySuccess_DeletesOnlyMatchingPending' }).Count
    P16 = @($fqn | Where-Object { $_ -match 'AiTheory_' }).Count
    SkipListed = @($lt | Where-Object { $_ -match '\[SKIP\]' }).Count
}
$obj | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'hv-list-fqn-count.json') -Encoding utf8
$obj | ConvertTo-Json
Write-Output ('STAMP=' + $obj.Stamp)
