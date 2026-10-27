$ErrorActionPreference = 'Stop'
$env:MCP_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:GROK_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:PLUGIN_AGENT_NAME = 'GrokCode'
$env:MCP_AGENT_NAME = 'GrokCode'
$env:MCP_PLUGIN_HOST = 'grok'
$env:MCP_WORKSPACE_PATH = 'F:\GitHub\McpServer'
Set-Location 'F:\GitHub\McpServer'
$repl = 'F:\GitHub\mcpserver-grok-plugin\lib\repl-invoke.ps1'
$pwsh = 'C:\Program Files\WindowsApps\Microsoft.PowerShell_7.6.6.0_x64__8wekyb3d8bbwe\pwsh.exe'
$turn = (Get-Content -LiteralPath (Join-Path $env:USERPROFILE '.grok\p2-fix-turn.txt') -Raw).Trim()
Write-Output ('TURN=' + $turn)
$dialog = @"
requestId: $turn
dialogItems:
  - role: model
    category: decision
    content: |
      Probe on PAYTON-LEGION2: Process GROK_PLUGIN_ROOT is set. ProcessStartInfo.Remove clears it (BEFORE true, REMOVED true, AFTER false). A new ProcessStartInfo then copies keys and still ContainsKey GROK_PLUGIN_ROOT (COPY_HAS_PLUGIN true). Dropping inherited keys absent from the source clears it (DROPPED 1). Product DisableGrokPluginEnvironment already removes the key; RecordingSpawner.Clone was reintroducing it. Fix is in the recorder, not a silenced assertion.
      P2 primary stdout is the typed result envelope from Invoke-ReplMethod (code persisted). Empty stdout was stale against that contract and against HV14. setSessionTitle receipts omit requestId; the wrapper tests require requestId null or empty and sessionId bound. The outcomes test now expects requestId empty plus sessionId equal to the layout session, and the fake emits retitled for SetSessionTitleAsync.
      Codex wrapper tests resolve parent-of-worktree mcpserver-codex-plugin. Junction F:\GitHub\McpServer\.worktrees\mcpserver-codex-plugin -> F:\GitHub\mcpserver-codex-plugin. Focused Pester 11 passed, 0 failed. Isolated Grok cwd fact passed 1. Product commit e755378cdc5d7d3a73471bcebf6874801ed3be80 pushed to PR 72. lib-ps was not edited, so SyncAgentPlugins was not run. Full unit gate is in progress on that tip.
"@
& $pwsh -NoProfile -NonInteractive -File $repl -Method 'workflow.sessionlog.appendDialog' -ParamsYaml $dialog
if ($LASTEXITCODE -ne 0) { Write-Output ('DIALOG_EXIT=' + $LASTEXITCODE); exit $LASTEXITCODE }
$actions = @"
requestId: $turn
actions:
  - order: 1
    type: design_decision
    description: Align P2 primary stdout with the typed persisted envelope and setSessionTitle requestId empty plus sessionId binding. Drop inherited env keys in the Grok recorder clone. Restore the Codex plugin sibling path with a junction.
    status: completed
  - order: 2
    type: commit
    description: fix(sessionlife) match typed P2 receipts. SHA e755378cdc5d7d3a73471bcebf6874801ed3be80. Branch cursor/sessionlife-p2-contracts-5cb2. Pushed to origin for PR 72.
    status: completed
"@
& $pwsh -NoProfile -NonInteractive -File $repl -Method 'workflow.sessionlog.appendActions' -ParamsYaml $actions
Write-Output ('ACTIONS_EXIT=' + $LASTEXITCODE)
exit $LASTEXITCODE
