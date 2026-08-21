#Requires -Version 7.0
[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$wt = 'F:\GitHub\McpServer\.worktrees\triage-plugin-core'
$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-s2-resume'
$out = Join-Path $outDir '05-live-flush.json'
$lib = Join-Path $wt 'plugins\core\lib-ps'
$hook = Join-Path $lib 'plugin-hook.ps1'
$scratch = Join-Path $outDir 'scratch-flush'
if (Test-Path -LiteralPath $scratch) { Remove-Item -LiteralPath $scratch -Recurse -Force }
[void][System.IO.Directory]::CreateDirectory($scratch)

function Invoke-HookChild {
    param(
        [string]$WorkingDirectory,
        [hashtable]$Environment
    )
    $psi = [System.Diagnostics.ProcessStartInfo]::new()
    $psi.FileName = (Get-Command pwsh.exe -ErrorAction Stop).Source
    $psi.ArgumentList.Add('-NoProfile')
    $psi.ArgumentList.Add('-NonInteractive')
    $psi.ArgumentList.Add('-File')
    $psi.ArgumentList.Add($hook)
    $psi.ArgumentList.Add('-HookName')
    $psi.ArgumentList.Add('session-end')
    $psi.ArgumentList.Add('-HostName')
    $psi.ArgumentList.Add('claude-code')
    $psi.WorkingDirectory = $WorkingDirectory
    $psi.UseShellExecute = $false
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.CreateNoWindow = $true
    foreach ($key in $Environment.Keys) {
        $psi.Environment[$key] = [string]$Environment[$key]
    }
    $proc = [System.Diagnostics.Process]::Start($psi)
    $stdoutTask = $proc.StandardOutput.ReadToEndAsync()
    $stderrTask = $proc.StandardError.ReadToEndAsync()
    if (-not $proc.WaitForExit(30000)) {
        try { $proc.Kill($true) } catch { }
        return [ordered]@{ ExitCode = -1; Stdout = ''; Stderr = 'killed-after-30s'; TimedOut = $true }
    }
    return [ordered]@{
        ExitCode = $proc.ExitCode
        Stdout = [string]$stdoutTask.Result
        Stderr = [string]$stderrTask.Result
        TimedOut = $false
    }
}

# Unresolved cache
$unresDir = Join-Path $scratch 'unresolved'
[void][System.IO.Directory]::CreateDirectory($unresDir)
$unresolved = Invoke-HookChild -WorkingDirectory $unresDir -Environment @{
    MCP_PLUGIN_ROOT = $lib
    MCP_PLUGIN_HOST = 'claude-code'
    MCP_AGENT_NAME = 'ClaudeCode'
    MCP_CACHE_DIR_OVERRIDE = ''
    MCP_WORKSPACE_PATH = ''
    MCPSERVER_WORKSPACE_PATH = ''
    MCP_WORKSPACE_START_DIR = ''
    CLAUDE_PROJECT_DIR = ''
    CODEX_CWD = ''
    CODEX_WORKSPACE_PATH = ''
    CODEX_PROJECT_DIR = ''
    PLUGIN_ROOT_OVERRIDE = ''
}

# Success flush via CLAUDE_PROJECT_DIR
$okRoot = Join-Path $scratch 'ok'
$okWs = Join-Path $okRoot 'workspace'
$okCwd = Join-Path $okRoot 'cwd'
$okCache = Join-Path $okWs '.mcpServer\claude'
$okPendingDir = Join-Path $okCache 'pending'
$okFailsafeDir = Join-Path $okCache 'failsafe'
[void][System.IO.Directory]::CreateDirectory($okPendingDir)
[void][System.IO.Directory]::CreateDirectory($okFailsafeDir)
[void][System.IO.Directory]::CreateDirectory($okCwd)
Set-Content -LiteralPath (Join-Path $okWs 'AGENTS-README-FIRST.yaml') -Value "workspace: sessionend`n" -Encoding utf8
$okPending = Join-Path $okPendingDir '001-client-Health-GetAsync.yaml'
Set-Content -LiteralPath $okPending -Value "id: `"001`"`ntimestamp: `"2026-08-19T00:00:00Z`"`nmethod: client.Health.GetAsync`nparams: {}`nretryCount: 0" -Encoding utf8
$okStub = Join-Path $okRoot 'flush-repl.ps1'
Set-Content -LiteralPath $okStub -Value "param([string]`$Method,[string]`$ParamsYaml='')`nexit 0`n" -Encoding utf8
$ok = Invoke-HookChild -WorkingDirectory $okCwd -Environment @{
    MCP_PLUGIN_ROOT = $lib
    MCP_PLUGIN_HOST = 'claude-code'
    MCP_AGENT_NAME = 'ClaudeCode'
    CLAUDE_PROJECT_DIR = $okWs
    MCP_WORKSPACE_PATH = ''
    MCPSERVER_WORKSPACE_PATH = ''
    MCP_CACHE_DIR_OVERRIDE = ''
    PLUGIN_ROOT_OVERRIDE = ''
    MCP_CACHE_FLUSH_REPL = $okStub
    MCP_FAILSAFE_DRAIN_DISABLED = '1'
}
$okPendingGone = -not (Test-Path -LiteralPath $okPending)
$okPendingCount = @(Get-ChildItem -LiteralPath $okPendingDir -Filter '*.yaml' -File -ErrorAction SilentlyContinue).Count

# Failure flush
$failRoot = Join-Path $scratch 'fail'
$failWs = Join-Path $failRoot 'workspace'
$failCwd = Join-Path $failRoot 'cwd'
$failPendingDir = Join-Path $failWs '.mcpServer\claude\pending'
[void][System.IO.Directory]::CreateDirectory($failPendingDir)
[void][System.IO.Directory]::CreateDirectory($failCwd)
Set-Content -LiteralPath (Join-Path $failWs 'AGENTS-README-FIRST.yaml') -Value "workspace: sessionend`n" -Encoding utf8
$failPending = Join-Path $failPendingDir '001-client-DoesNotExist.yaml'
Set-Content -LiteralPath $failPending -Value "id: `"001`"`ntimestamp: `"2026-08-19T00:00:00Z`"`nmethod: client.DoesNotExist.Nope`nparams: {}`nretryCount: 0" -Encoding utf8
$failStub = Join-Path $failRoot 'flush-repl-fail.ps1'
Set-Content -LiteralPath $failStub -Value "param([string]`$Method,[string]`$ParamsYaml='')`nthrow 'flush-replay-failed'`n" -Encoding utf8
$fail = Invoke-HookChild -WorkingDirectory $failCwd -Environment @{
    MCP_PLUGIN_ROOT = $lib
    MCP_PLUGIN_HOST = 'claude-code'
    MCP_AGENT_NAME = 'ClaudeCode'
    CLAUDE_PROJECT_DIR = $failWs
    MCP_WORKSPACE_PATH = ''
    MCPSERVER_WORKSPACE_PATH = ''
    MCP_CACHE_DIR_OVERRIDE = ''
    PLUGIN_ROOT_OVERRIDE = ''
    MCP_CACHE_FLUSH_REPL = $failStub
    MCP_FAILSAFE_DRAIN_DISABLED = '1'
}
$failPendingStillThere = Test-Path -LiteralPath $failPending
$failStdout = [string]$fail.Stdout
$failLooksEmptySuccess = ($fail.ExitCode -eq 0 -and $failStdout.Trim() -eq '{}')
$failHasFlushFailed = ($failStdout -match 'flush-failed')

$obj = [ordered]@{
    TimestampUtc = [datetime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    Unresolved = $unresolved
    UnresolvedExit0Empty = ($unresolved.ExitCode -eq 0 -and ([string]$unresolved.Stdout).Trim() -eq '{}')
    OkFlush = $ok
    OkPendingGone = $okPendingGone
    OkPendingCount = $okPendingCount
    OkExit0Empty = ($ok.ExitCode -eq 0 -and ([string]$ok.Stdout).Trim() -eq '{}')
    FailFlush = $fail
    FailPendingStillThere = $failPendingStillThere
    FailLooksEmptySuccess = $failLooksEmptySuccess
    FailHasFlushFailed = $failHasFlushFailed
    FailExitCode = $fail.ExitCode
    FailStdoutTrim = $failStdout.Trim()
}
$obj | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $out -Encoding utf8
Write-Output ("WROTE {0} unresEmpty={1} okGone={2} failExit={3} failFlushFailed={4} failEmptySuccess={5}" -f $out, $obj.UnresolvedExit0Empty, $okPendingGone, $fail.ExitCode, $failHasFlushFailed, $failLooksEmptySuccess)
