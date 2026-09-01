#Requires -Version 7.0
[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$wt = 'F:\GitHub\McpServer\.worktrees\triage-plugin-core'
$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-s2-resume'
$out = Join-Path $outDir '05c-live-flush-temp.json'
$lib = Join-Path $wt 'plugins\core\lib-ps'
$hook = Join-Path $lib 'plugin-hook.ps1'
$scratch = Join-Path ([System.IO.Path]::GetTempPath()) ('hv-s2-resume-flush-' + [guid]::NewGuid().ToString('N'))
[void][System.IO.Directory]::CreateDirectory($scratch)

function Invoke-HookChild {
    param(
        [string]$WorkingDirectory,
        [hashtable]$Environment
    )
    $psi = [System.Diagnostics.ProcessStartInfo]::new()
    $psi.FileName = (Get-Command pwsh.exe -ErrorAction Stop).Source
    $psi.ArgumentList.Add('-NoLogo')
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
    $psi.RedirectStandardInput = $true
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.CreateNoWindow = $true
    foreach ($key in $Environment.Keys) {
        $psi.Environment[$key] = [string]$Environment[$key]
    }
    $proc = [System.Diagnostics.Process]::Start($psi)
    $proc.StandardInput.Close()
    $stdoutTask = $proc.StandardOutput.ReadToEndAsync()
    $stderrTask = $proc.StandardError.ReadToEndAsync()
    if (-not $proc.WaitForExit(30000)) {
        try { $proc.Kill($true) } catch { }
        return [ordered]@{ ExitCode = -1; Stdout = ''; Stderr = 'killed-after-30s'; TimedOut = $true }
    }
    return [ordered]@{
        ExitCode = $proc.ExitCode
        Stdout = ([string]$stdoutTask.Result).Trim()
        Stderr = ([string]$stderrTask.Result).Trim()
        TimedOut = $false
    }
}

try {
    $okWs = Join-Path $scratch 'ok-workspace'
    $okCwd = Join-Path $scratch 'ok-cwd'
    $okPendingDir = Join-Path $okWs '.mcpServer\claude\pending'
    [void][System.IO.Directory]::CreateDirectory($okPendingDir)
    [void][System.IO.Directory]::CreateDirectory($okCwd)
    Set-Content -LiteralPath (Join-Path $okWs 'AGENTS-README-FIRST.yaml') -Value "workspace: sessionend`n" -Encoding utf8
    $okPending = Join-Path $okPendingDir '001-client-Health-GetAsync.yaml'
    Set-Content -LiteralPath $okPending -Value "id: `"001`"`ntimestamp: `"2026-08-19T00:00:00Z`"`nmethod: client.Health.GetAsync`nparams: {}`nretryCount: 0" -Encoding utf8
    $okStub = Join-Path $scratch 'flush-repl.ps1'
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
        MCP_WORKSPACE_START_DIR = ''
        CODEX_CWD = ''
        CODEX_WORKSPACE_PATH = ''
        CODEX_PROJECT_DIR = ''
    }

    $failWs = Join-Path $scratch 'fail-workspace'
    $failCwd = Join-Path $scratch 'fail-cwd'
    $failPendingDir = Join-Path $failWs '.mcpServer\claude\pending'
    [void][System.IO.Directory]::CreateDirectory($failPendingDir)
    [void][System.IO.Directory]::CreateDirectory($failCwd)
    Set-Content -LiteralPath (Join-Path $failWs 'AGENTS-README-FIRST.yaml') -Value "workspace: sessionend`n" -Encoding utf8
    $failPending = Join-Path $failPendingDir '001-client-DoesNotExist.yaml'
    Set-Content -LiteralPath $failPending -Value "id: `"001`"`ntimestamp: `"2026-08-19T00:00:00Z`"`nmethod: client.DoesNotExist.Nope`nparams: {}`nretryCount: 0" -Encoding utf8
    $failStub = Join-Path $scratch 'flush-repl-fail.ps1'
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
        MCP_WORKSPACE_START_DIR = ''
        CODEX_CWD = ''
        CODEX_WORKSPACE_PATH = ''
        CODEX_PROJECT_DIR = ''
    }

    $unresDir = Join-Path $scratch 'unresolved-cwd'
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

    $obj = [ordered]@{
        TimestampUtc = [datetime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
        Scratch = $scratch
        Unresolved = $unresolved
        UnresolvedExit0Empty = ($unresolved.ExitCode -eq 0 -and $unresolved.Stdout -eq '{}')
        OkFlush = $ok
        OkPendingGone = -not (Test-Path -LiteralPath $okPending)
        OkPendingCount = @(Get-ChildItem -LiteralPath $okPendingDir -Filter '*.yaml' -File -ErrorAction SilentlyContinue).Count
        OkExit0Empty = ($ok.ExitCode -eq 0 -and $ok.Stdout -eq '{}')
        FailFlush = $fail
        FailPendingStillThere = (Test-Path -LiteralPath $failPending)
        FailLooksEmptySuccess = ($fail.ExitCode -eq 0 -and $fail.Stdout -eq '{}')
        FailHasFlushFailed = ($fail.Stdout -match 'flush-failed')
        FailExitCode = $fail.ExitCode
        FailStdout = $fail.Stdout
        FailMatchesClaim = (($fail.ExitCode -eq 1) -and ($fail.Stdout -match 'flush-failed') -and ($fail.Stdout -ne '{}'))
    }
    $obj | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $out -Encoding utf8
    Write-Output ("WROTE {0} unres={1} okGone={2} okOut={3} failExit={4} failOut={5} failClaim={6}" -f $out, $obj.UnresolvedExit0Empty, $obj.OkPendingGone, $ok.Stdout, $fail.ExitCode, $fail.Stdout, $obj.FailMatchesClaim)
} finally {
    Remove-Item -LiteralPath $scratch -Recurse -Force -ErrorAction SilentlyContinue
}
