#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p15'
$listLog = Join-Path $out 'hv-dotnet-list-tests.log'
$lines = Get-Content -LiteralPath $listLog
$fqn = @($lines | Where-Object { $_ -match 'McpServer\.PluginIntegration\.Tests\.' } | ForEach-Object { $_.Trim() } | Sort-Object -Unique)
$key = 'F:\GitHub\McpServer'
$full = [IO.Path]::GetFullPath($key)
$b64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($full))
$url = (($b64 -replace '\+', '-') -replace '/', '_').TrimEnd('=')
[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    UniqueFqnCount = $fqn.Count
    P14 = @($fqn | Where-Object { $_ -match 'PluginRootOverride_WritesOnlyWorkspaceMcpServerAgent_PoisonEmpty' }).Count
    P15Success = @($fqn | Where-Object { $_ -match 'Success_NoPendingFailsafe' }).Count
    P15FailedSubmit = @($fqn | Where-Object { $_ -match 'FailedSubmit_RetainsRootIdPending' }).Count
    P15Retry = @($fqn | Where-Object { $_ -match 'RetrySuccess_DeletesOnlyMatchingPending' }).Count
    P16 = @($fqn | Where-Object { $_ -match 'AiTheory_' }).Count
    SkipListed = @($fqn | Where-Object { $_ -match '(?i)skip' }).Count
    SampleFqn = @($fqn | Select-Object -First 5)
    WorkspaceFullPath = $full
    WorkspaceKeyUtf8Base64Url = $url
    PluginStatusFailsafeKey = 'RjpcR2l0SHViXE1jcFNlcnZlcg'
    PluginStatusKeyMatches = ($url -eq 'RjpcR2l0SHViXE1jcFNlcnZlcg')
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'hv-list-fqn-count.json') -Encoding utf8
Write-Output ('UNIQUE_FQN=' + $fqn.Count)
