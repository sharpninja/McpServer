#Requires -Version 7.0
[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$wt = 'F:\GitHub\McpServer\.worktrees\triage-plugin-core'
$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-s2-resume'
$out = Join-Path $outDir '06-live-persist.json'
$lib = Join-Path $wt 'plugins\core\lib-ps'
$scratch = Join-Path $outDir 'scratch-persist'
if (Test-Path -LiteralPath $scratch) { Remove-Item -LiteralPath $scratch -Recurse -Force }

$cache = Join-Path $scratch 'cache'
$failsafe = Join-Path $scratch 'failsafe'
$bin = Join-Path $scratch 'bin'
[void][System.IO.Directory]::CreateDirectory($cache)
[void][System.IO.Directory]::CreateDirectory($failsafe)
[void][System.IO.Directory]::CreateDirectory($bin)
$hang = Join-Path $bin 'mcpserver-repl.cmd'
Set-Content -LiteralPath $hang -Value "@echo off`r`nping -n 21 127.0.0.1 >nul`r`n" -Encoding ascii

. (Join-Path $lib 'yaml-object-mutation.ps1')
Import-McpYamlSerializer
Write-McpYamlObject -Path (Join-Path $cache 'session-state.yaml') -Document ([ordered]@{
    status = 'verified'
    sessionId = 'GrokCode-20260819T000000Z-plugin-session'
    agent = 'GrokCode'
    markerFilePath = (Join-Path $scratch 'AGENTS-README-FIRST.yaml')
    markerLastWriteUtc = '2026-08-19T00:00:00Z'
})
Write-McpYamlObject -Path (Join-Path $cache 'current-turn.yaml') -Document ([ordered]@{
    turnRequestId = 'req-20260819T000000Z-001-submit-timeout'
    queryTitle = 'submit timeout'
    queryText = 'submit timeout'
    openedAt = '2026-08-19T00:00:01Z'
    status = 'in_progress'
    sessionId = 'GrokCode-20260819T000000Z-plugin-session'
    planFile = 'docs/plans/triage-cluster-002.md'
    todoId = 'BUG-TRIAGE-120'
})

$savedPath = $env:PATH
$savedTimeout = $env:REPL_TIMEOUT
$savedCache = $env:MCP_CACHE_DIR_OVERRIDE
$savedFailsafe = $env:MCPSERVER_FAILSAFE_DIR
$savedPersist = $env:MCP_PLUGIN_PERSIST_LOG
$savedRoot = $env:MCP_PLUGIN_ROOT
$savedHost = $env:MCP_PLUGIN_HOST
$savedAgent = $env:MCP_AGENT_NAME

$threw = $false
$throwText = ''
$persisted = $null
$elapsed = $null
$details = $null
$rawDef = $null
$rawSource = $null
$whichRepl = $null
try {
    $env:PATH = "$bin;$savedPath"
    $env:REPL_TIMEOUT = '1'
    $env:MCP_CACHE_DIR_OVERRIDE = $cache
    $env:MCPSERVER_FAILSAFE_DIR = $failsafe
    $env:MCP_PLUGIN_ROOT = $lib
    $env:MCP_PLUGIN_HOST = 'grok'
    $env:MCP_AGENT_NAME = 'GrokCode'
    Remove-Item Env:\MCP_PLUGIN_PERSIST_LOG -ErrorAction SilentlyContinue
    . (Join-Path $lib 'repl-invoke.ps1')
    function Assert-ReplMarkerFresh { return $true }

    $raw = Get-Command Invoke-ReplRaw -CommandType Function -ErrorAction Stop
    $rawDef = $raw.Definition
    $rawSource = [string]$raw.Source
    $whichRepl = Get-Command mcpserver-repl -ErrorAction SilentlyContinue
    $started = [DateTime]::UtcNow
    try {
        $persisted = Invoke-ReplPersistTurn -RequestId 'req-20260819T000000Z-001-submit-timeout' -Title 'submit timeout' -Status 'in_progress' -ResponseText '(turn opened)' -PlanFile 'docs/plans/triage-cluster-002.md' -TodoId 'BUG-TRIAGE-120'
    } catch {
        $threw = $true
        $throwText = [string]$_
    }
    $elapsed = ([DateTime]::UtcNow - $started).TotalSeconds
    if ($script:LastReplPersistenceDetails) {
        $details = $script:LastReplPersistenceDetails
    }
} finally {
    $env:PATH = $savedPath
    if ($null -ne $savedTimeout) { $env:REPL_TIMEOUT = $savedTimeout } else { Remove-Item Env:\REPL_TIMEOUT -ErrorAction SilentlyContinue }
    if ($null -ne $savedCache) { $env:MCP_CACHE_DIR_OVERRIDE = $savedCache } else { Remove-Item Env:\MCP_CACHE_DIR_OVERRIDE -ErrorAction SilentlyContinue }
    if ($null -ne $savedFailsafe) { $env:MCPSERVER_FAILSAFE_DIR = $savedFailsafe } else { Remove-Item Env:\MCPSERVER_FAILSAFE_DIR -ErrorAction SilentlyContinue }
    if ($null -ne $savedPersist) { $env:MCP_PLUGIN_PERSIST_LOG = $savedPersist }
    if ($null -ne $savedRoot) { $env:MCP_PLUGIN_ROOT = $savedRoot } else { Remove-Item Env:\MCP_PLUGIN_ROOT -ErrorAction SilentlyContinue }
    if ($null -ne $savedHost) { $env:MCP_PLUGIN_HOST = $savedHost } else { Remove-Item Env:\MCP_PLUGIN_HOST -ErrorAction SilentlyContinue }
    if ($null -ne $savedAgent) { $env:MCP_AGENT_NAME = $savedAgent } else { Remove-Item Env:\MCP_AGENT_NAME -ErrorAction SilentlyContinue }
}

$failsafePath = $null
if ($details -and $details.Contains('failsafePath')) { $failsafePath = [string]$details['failsafePath'] }
$failsafeExists = $false
if ($failsafePath) { $failsafeExists = Test-Path -LiteralPath $failsafePath }

$obj = [ordered]@{
    TimestampUtc = [datetime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    Threw = $threw
    ThrowText = $throwText
    Persisted = $persisted
    ElapsedSec = [math]::Round([double]$elapsed, 3)
    Details = $details
    FailsafePath = $failsafePath
    FailsafeExists = $failsafeExists
    RawSource = $rawSource
    RawDefHasSubmitAsync = ($rawDef -match 'client\.SessionLog\.SubmitAsync')
    RawDefHasGetCommandRepl = ($rawDef -match 'Get-Command mcpserver-repl')
    RawDefHasStartSleep = ($rawDef -match 'Start-Sleep')
    RawDefHasProcessStart = ($rawDef -match 'ProcessStartInfo' -and $rawDef -match 'Wait\(')
    WhichReplSource = $(if ($whichRepl) { [string]$whichRepl.Source } else { $null })
    WhichReplIsHangCmd = ($(if ($whichRepl) { [string]$whichRepl.Source } else { '' }) -eq $hang)
    HangCmdPath = $hang
}
$obj | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $out -Encoding utf8
Write-Output ("WROTE {0} threw={1} persisted={2} elapsed={3} hangCmd={4} degraded={5}" -f $out, $threw, $persisted, $obj.ElapsedSec, $obj.WhichReplIsHangCmd, $(if ($details) { $details['degraded'] } else { $null }))
