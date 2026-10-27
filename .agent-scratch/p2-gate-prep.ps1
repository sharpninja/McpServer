$ErrorActionPreference = 'Continue'
$wt = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
Set-Location -LiteralPath $wt
Write-Output ('HEAD=' + (git rev-parse HEAD))
Write-Output '=== GITIGNORE HV ==='
Select-String -Path (Join-Path $wt '.gitignore') -Pattern 'receipts/hv|gate.log' | ForEach-Object { $_.Line }
Write-Output '=== HANDOFF GATE LINES ==='
$handoff = Join-Path $wt 'docs\handoffs\sessionlife-p2-contracts-pr72-20261001T113858Z.md'
if (Test-Path -LiteralPath $handoff) {
    Select-String -LiteralPath $handoff -Pattern 'ACCEPTED|GATE_EXIT|Pester|PluginIntegration|gate.log|987cd8f' |
        Select-Object -First 40 |
        ForEach-Object { $_.LineNumber.ToString() + ':' + $_.Line.Trim() }
} else {
    Write-Output 'HANDOFF_MISSING'
}
Write-Output '=== TODO METHOD ==='
Select-String -Path 'F:\GitHub\mcpserver-grok-plugin\skills\todo\SKILL.md' -Pattern 'method: workflow.todo' |
    Select-Object -First 15 |
    ForEach-Object { $_.Line.Trim() }
