#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$workspace = 'F:\GitHub\McpServer'
$plugin = 'F:\GitHub\mcpserver-grok-plugin\lib\Invoke-McpPlugin.ps1'
$paramsPath = 'F:\GitHub\McpServer\docs\receipts\_hv-s0-mark-107-done.yaml'

@'
id: BUG-TRIAGE-107
done: true
doneSummary: >-
  Closeout AGREE docs/receipts/hostile-validator-20260819T184513Z.md.
  Build.SyncAgentPlugins.cs uses version-less sharpninja-mcpserver-plugin-core.tgz
  and asserts npm pack against package.json. SyncAgentPluginsVendorNameTests 3/0/0.
  Live npm pack --dry-run filename sharpninja-mcpserver-plugin-core-0.2.0.tgz.
  Session GrokCode-20260819T183701Z-hostile-g11-closeout.
'@ | Set-Content -LiteralPath $paramsPath -Encoding utf8

& $plugin -Command Invoke -Method 'workflow.todo.update' -ParamsPath $paramsPath -WorkspacePath $workspace
