#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$out = 'F:\GitHub\McpServer\docs\receipts\_p19-green-bootstrap'
$plugin = 'F:\GitHub\mcpserver-grok-plugin\lib\Invoke-McpPlugin.ps1'
$cacheRoot = Join-Path $out 'plugin-cache'
$ids = Get-Content -LiteralPath (Join-Path $out 'ids.json') -Raw | ConvertFrom-Json
$env:GROK_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:MCP_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:PLUGIN_AGENT_NAME = 'GrokCode'
$env:MCP_WORKSPACE_PATH = 'F:\GitHub\McpServer'
Set-Location -LiteralPath 'F:\GitHub\McpServer'
$now = [DateTime]::UtcNow.ToString('o')
$result = & $plugin -Command Invoke -Method 'client.SessionLog.AppendDialogAsync' -ParamsObject @{
    agent = 'GrokCode'
    sessionId = [string]$ids.sessionId
    requestId = [string]$ids.requestId
    items = @(
        @{ timestamp = $now; role = 'model'; category = 'observation'; content = 'First complete-run attempt _p19-work-20260822T103838Z discovered all Claude-code 10 *.Tests.ps1 including ReplFailsafe, HookTurnDedupe, CurrentTurnSessionRebind. All five PowerShell plugins Failed 2 on ReplFailsafe AfterEach: $script:OriginalFailsafeDir unset under leaked Set-StrictMode Latest. Jest plugins Failed 0. Isolated Codex ReplFailsafe with Set-StrictMode -Off: Passed 2 Failed 0 Skipped 0. Runner killed before promoting a failing pluginint-p19 dir. T102645Z remains LoadLatest until a later green complete-run is promoted.' }
        @{ timestamp = $now; role = 'model'; category = 'decision'; content = 'Decision: inner Pester process uses Set-StrictMode -Off so official plugin tests run as they do in host Pester. Do not leak runner StrictMode into plugin suites. Wait-NativeMachineClear now matches testhost.exe and dotnet test only, not idle MCP consoles whose command line mentions Pester. Do not promote pluginint-p19-* unless every row Failed 0 Skipped 0. Alternatives rejected: changing plugin tests in this slice; greening T095842Z subset; promoting the StrictMode-failed work dir.' }
    )
} -WorkspacePath 'F:\GitHub\McpServer' -CacheRoot $cacheRoot -TimeoutSeconds 120
$result | Set-Content -LiteralPath (Join-Path $out 'sl-dialog-strictmode.txt') -Encoding utf8
Write-Output 'APPENDED'
