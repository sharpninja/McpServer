#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p11'
$hits = @(Get-ChildItem -Path $out -Filter *.ps1 | Select-String -Pattern '\bpython(3)?\b|\bpy\.exe\b')
$obj = @(foreach ($h in $hits) {
    [pscustomobject]@{
        Path = $h.Path
        LineNumber = $h.LineNumber
        Line = $h.Line.Trim()
    }
})
$obj | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $out 'no-python-hits.json') -Encoding utf8

$todoYamlStatus = git -C 'F:\GitHub\McpServer' status --porcelain -- docs/Project/TODO.yaml
[ordered]@{ TodoYamlPorcelain = [string]$todoYamlStatus } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'todo-yaml-git-status.json') -Encoding utf8

$handoff = 'F:\GitHub\McpServer\src\McpServer.Services\Services\HandoffIngestionService.cs'
$handoffItem = Get-Item -LiteralPath $handoff
$gitHandoff = git -C 'F:\GitHub\McpServer' status --porcelain -- src/McpServer.Services/Services/HandoffIngestionService.cs
$gitLogHandoff = git -C 'F:\GitHub\McpServer' log -n 3 --format='%h %ci %s' -- src/McpServer.Services/Services/HandoffIngestionService.cs
$methodHits = @(Select-String -LiteralPath $handoff -Pattern 'WaitForHeartbeatIntervalAsync')
[ordered]@{
    Exists = $true
    LastWriteTimeUtc = $handoffItem.LastWriteTimeUtc.ToString('o')
    Length = $handoffItem.Length
    Porcelain = [string]$gitHandoff
    RecentLog = [string]$gitLogHandoff
    WaitForHeartbeatIntervalAsyncHitCount = $methodHits.Count
    HitLines = @($methodHits | ForEach-Object { $_.LineNumber.ToString() + ':' + $_.Line.Trim() })
} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $out 'handoff-compile-probe.json') -Encoding utf8

$ids = [ordered]@{
    stamp = if (Test-Path (Join-Path $out 'stamp.txt')) { (Get-Content (Join-Path $out 'stamp.txt') -Raw).Trim() } else { $null }
    sessionIdFile = if (Test-Path (Join-Path $out 'session-id.txt')) { (Get-Content (Join-Path $out 'session-id.txt') -Raw).Trim() } else { $null }
    requestIdFile = if (Test-Path (Join-Path $out 'request-id.txt')) { (Get-Content (Join-Path $out 'request-id.txt') -Raw).Trim() } else { $null }
}
$ids | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'session-id-files.json') -Encoding utf8

$gitStatus = git -C 'F:\GitHub\McpServer' status --porcelain -- tests/McpServer.PluginIntegration.Tests docs/plans/PLAN-PLUGINHANDOFF-001.md src/McpServer.Services/Services/HandoffIngestionService.cs
Set-Content -LiteralPath (Join-Path $out 'git-status-expanded.txt') -Value ([string]$gitStatus) -Encoding utf8

Write-Output ('PYTHON_HIT_COUNT=' + $hits.Count)
Write-Output ('TODO_YAML_PORCELAIN=[' + $todoYamlStatus + ']')
Write-Output ('HANDOFF_PORCELAIN=[' + $gitHandoff + ']')
Write-Output ('HANDOFF_HITS=' + $methodHits.Count)
Write-Output ('STAMP=' + $ids.stamp)
Write-Output ('SESSION_FILE=' + $ids.sessionIdFile)
Write-Output ('REQUEST_FILE=' + $ids.requestIdFile)
