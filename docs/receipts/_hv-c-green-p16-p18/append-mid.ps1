#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p16-p18'
$plugin = 'F:\GitHub\mcpserver-grok-plugin\lib\Invoke-McpPlugin.ps1'
$cacheRoot = Join-Path $out 'plugin-cache'
Set-Location -LiteralPath 'F:\GitHub\McpServer'
$env:MCP_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$env:MCPSERVER_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$env:PLUGIN_AGENT_NAME = 'GrokSubagentHostile'
$env:MCP_AGENT_NAME = 'GrokSubagentHostile'
$env:GROK_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:MCP_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:MCP_PLUGIN_HOST = 'grok'

$agent = 'GrokSubagentHostile'
$sessionId = (Get-Content -LiteralPath (Join-Path $out 'session-id.txt') -Raw).Trim()
$requestId = (Get-Content -LiteralPath (Join-Path $out 'request-id.txt') -Raw).Trim()
$now = [DateTime]::UtcNow.ToString('o')

function Invoke-Plugin {
    param([string]$Method, [hashtable]$Params, [string]$Name)
    $outFile = Join-Path $out ($Name + '.txt')
    $errFile = Join-Path $out ($Name + '.err.txt')
    try {
        $result = & $plugin -Command Invoke -Method $Method -ParamsObject $Params -WorkspacePath 'F:\GitHub\McpServer' -CacheRoot $cacheRoot -TimeoutSeconds 120 2> $errFile
        if ($null -eq $result) { $result = '' }
        if ($result -isnot [string]) { $result = ($result | Out-String) }
        Set-Content -LiteralPath $outFile -Value $result -Encoding utf8
        Write-Output ('OK ' + $Name)
    } catch {
        Set-Content -LiteralPath $outFile -Value ('ERROR ' + $_.Exception.ToString()) -Encoding utf8
        Write-Output ('FAIL ' + $Name + ' ' + $_.Exception.Message)
    }
}

Invoke-Plugin -Method 'client.SessionLog.AppendDialogAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    items = @(
        @{ timestamp = $now; role = 'model'; category = 'observation'; content = 'Independent tests-run restarted after first attempt failed because PowerShell $Args automatic variable emptied the dotnet splat (exit -2147450751, Args []). Fixed tests-run.ps1 to use $DotnetArgs and C:\Program Files\dotnet\dotnet.exe. Live todo_get PLAN-PLUGINHANDOFF-001 done false; MCP-PLUGININT-001 done false; P16/P17/P18 tasks done false. EvaluateAsync implemented 2026-08-22T07:44:14Z after C-red-P16 AGREE 07:37:37Z. Build.PluginSessionLogIntegration.cs still 2026-08-21T23:33:11Z with no aiUnit preflight and no Trait split. P19 names absent. Skip hits 0.' }
        @{ timestamp = $now; role = 'model'; category = 'decision'; content = 'Decision: score P18 plan/TR ac-6 separately from named NukeTarget_SkipIsFailure. Missing aiUnit strategy preflight in Build.PluginSessionLogIntegration.cs is a Surface D/C candidate even if skip-fail helper tests pass. Consequence: do not AGREE solely on named-test green. Alternatives rejected: treating whole-project DotNetTest as aiUnit preflight; requiring P19 native suites in this gate.' }
    )
} -Name 'sl-dialog-mid'

Write-Output 'APPEND_MID_DONE'
