$ErrorActionPreference = 'Stop'
$env:MCP_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:GROK_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:PLUGIN_AGENT_NAME = 'GrokCode'
$env:MCP_AGENT_NAME = 'GrokCode'
$env:MCP_PLUGIN_HOST = 'grok'
$env:MCP_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$pwsh = 'C:\Program Files\WindowsApps\Microsoft.PowerShell_7.6.6.0_x64__8wekyb3d8bbwe\pwsh.exe'
$invoke = 'F:\GitHub\mcpserver-grok-plugin\lib\repl-invoke.ps1'
Set-Location 'F:\GitHub\McpServer'
$dialog = @'
dialogItems:
  - timestamp: 2026-10-02T23:05:00Z
    role: model
    content: |
      Mapped twelve previously failed P2 criteria to executed tests, then isolated the SQLite memory schema cache after the unit gate failed on a shared :memory: ready flag. Gate p2-unit-legion-20261002T222414Z accepted at 6e3215ff. One Astra round DISAGREE accuracy 99 completeness 77.5. No second round. PR 72 remains open draft unmerged.
    category: decision
'@
& $pwsh -NoProfile -NonInteractive -File $invoke -Method workflow.sessionlog.appendDialog -ParamsYaml $dialog
if ($LASTEXITCODE -ne 0) { Write-Output 'DIALOG_EXIT=' + $LASTEXITCODE }
$actions = @'
actions:
  - order: 1
    description: Mapped executed tests into the r3 acceptance manifest and added the never-degraded 404 assertion.
    type: edit
    status: completed
    filePath: docs/receipts/sessionlife-completion/20260928-p0-r3/acceptance-manifest.json
  - order: 2
    description: Probe each SQLite memory database instead of sharing one connection-string cache.
    type: design_decision
    status: completed
    filePath: src/McpServer.Storage/SessionLogSchemaGuard.cs
  - order: 3
    description: Accepted unit gate p2-unit-legion-20261002T222414Z on tip 6e3215ff.
    type: design_decision
    status: completed
    filePath: ""
  - order: 4
    description: Recorded the single Astra DISAGREE receipt and pushed it to PR 72.
    type: commit
    status: completed
    filePath: docs/receipts/hv/hostile-validator-sessionlife-p2-20261002T223800Z.md
'@
& $pwsh -NoProfile -NonInteractive -File $invoke -Method workflow.sessionlog.appendActions -ParamsYaml $actions
$complete = @'
response: |
  Tip 6e3215ffeab727c345739fb452aeaa0abdc416ab gated by p2-unit-legion-20261002T222414Z (Pester 242/242, Nuke 4298/4298, Build.Tests 321/321). Astra 20261002T223800Z DISAGREE accuracy 99 completeness 77.5 confidence 99. Receipts pushed as c4f3a1c5. PR 72 remains OPEN draft unmerged. No second Astra round.
'@
& $pwsh -NoProfile -NonInteractive -File $invoke -Method workflow.sessionlog.completeTurn -ParamsYaml $complete
Write-Output 'SESSION_LOG_DONE'
