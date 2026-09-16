#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p15\run2'
$utc = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
Set-Content -LiteralPath (Join-Path $out 'receipt-stamp.txt') -Value $utc -Encoding utf8
Write-Output ('RECEIPT_STAMP=' + $utc)
Write-Output ('NOW_O=' + [DateTime]::UtcNow.ToString('o'))

$py = @(Get-CimInstance Win32_Process | Where-Object { [string]$_.Name -match 'python' } | ForEach-Object {
    $cmd = [string]$_.CommandLine
    if ($cmd.Length -gt 180) { $cmd = $cmd.Substring(0, 180) }
    [ordered]@{ ProcessId = $_.ProcessId; Name = $_.Name; CommandLine = $cmd }
})
[ordered]@{ TimestampUtc = [DateTime]::UtcNow.ToString('o'); Count = $py.Count; Processes = $py } |
    ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $out 'python-procs-final.json') -Encoding utf8
Write-Output ('PYTHON_COUNT=' + $py.Count)

$git = git -C 'F:\GitHub\McpServer' status --porcelain -- 'docs/Project/TODO.yaml' 'docs/todo.yaml' 'tests/McpServer.PluginIntegration.Tests' 'docs/receipts'
[ordered]@{ TimestampUtc = [DateTime]::UtcNow.ToString('o'); Porcelain = @($git) } |
    ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'git-status-final.json') -Encoding utf8
Write-Output 'GIT_STATUS_SAVED'

$hosts = @('Codex','ClaudeCode','ClaudeCowork','Copilot','Grok','Cline','ClineV2','OpenCode')
$sum = Get-Content -LiteralPath (Join-Path $out 'filter-trx-summary.json') -Raw | ConvertFrom-Json
$names = @($sum.passedNames)
$hostCheck = foreach ($h in $hosts) {
    $success = @($names | Where-Object { $_ -match ('Success_NoPendingFailsafe\(hostKind: ' + $h + '\)') }).Count
    $failed = @($names | Where-Object { $_ -match ('FailedSubmit_RetainsRootIdPending\(hostKind: ' + $h + '\)') }).Count
    $retry = @($names | Where-Object { $_ -match ('RetrySuccess_DeletesOnlyMatchingPending\(hostKind: ' + $h + '\)') }).Count
    [ordered]@{ Host = $h; Success = $success; FailedSubmit = $failed; Retry = $retry }
}
$hostCheck | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'p15-host-matrix.json') -Encoding utf8
Write-Output 'HOST_MATRIX_SAVED'
