$ErrorActionPreference = 'Continue'
$log = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\docs\receipts\hv\p2-unit-legion-20261002T193045Z-gate.log'
$hits = Select-String -LiteralPath $log -Pattern 'covers primary queued|codex exposes exactly one' -Context 18,2
foreach ($hit in $hits) {
    Write-Output '-----'
    $hit.Context.PreContext | ForEach-Object { $_ }
    Write-Output $hit.Line
    $hit.Context.PostContext | ForEach-Object { $_ }
}
$prior = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\docs\receipts\hv\p2-unit-legion-20261001T111937Z-gate.log'
if (Test-Path $prior) {
    $item = Get-Item $prior
    Write-Output ('PRIOR_BYTES=' + $item.Length)
    Write-Output '=== PRIOR TAIL ==='
    Get-Content -LiteralPath $prior -Tail 15
}
$nuke = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\TestResults\p2-unit-legion-20261002T193045Z\logs\nuke-test.log'
if (Test-Path $nuke) {
    $n = Get-Item $nuke
    Write-Output ('NUKE_LOG_BYTES=' + $n.Length + ' WRITE=' + $n.LastWriteTimeUtc.ToString('o'))
    Write-Output '=== NUKE TAIL ==='
    Get-Content -LiteralPath $nuke -Tail 8
}
