#Requires -Version 7.0
[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$main = 'F:\GitHub\McpServer'
$wt = 'F:\GitHub\McpServer\.worktrees\triage-plugin-core'
$plugin = 'F:\GitHub\mcpserver-grok-plugin'
$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-s2-hgreen'
$out = Join-Path $outDir '16-live-120-core.json'
$tempPlugin = Join-Path $outDir 'temp-plugin-core'
$tempLib = Join-Path $tempPlugin 'lib'
$cache = Join-Path $outDir 'cache-core-120'

if (Test-Path -LiteralPath $tempPlugin) {
    Remove-Item -LiteralPath $tempPlugin -Recurse -Force
}
[void][System.IO.Directory]::CreateDirectory($tempLib)
[void][System.IO.Directory]::CreateDirectory($cache)
Copy-Item -Path (Join-Path $plugin 'lib\*') -Destination $tempLib -Recurse -Force
Copy-Item -Path (Join-Path $wt 'plugins\core\lib-ps\*') -Destination $tempLib -Force
$pluginExe = Join-Path $tempLib 'Invoke-McpPlugin.ps1'

Import-Module (Join-Path $main 'tools\powershell\McpSession.psm1') -Force
$stamp = [datetime]::UtcNow
$sessionId = New-McpSessionLogSlug -Agent 'GrokCode' -Model 'hostile-s2-120-core' -TimestampUtc $stamp
$requestId = 'req-{0}-001-hostile-s2-120-core' -f $stamp.ToUniversalTime().ToString('yyyyMMddTHHmmssZ')

. (Join-Path $wt 'plugins\core\lib-ps\yaml-object-mutation.ps1')
Import-McpYamlSerializer

$openPath = Join-Path $outDir '16-open.yaml'
$beginPath = Join-Path $outDir '16-begin.yaml'
Set-Content -LiteralPath $openPath -Value (ConvertTo-Yaml -Data ([ordered]@{
    agent = 'GrokCode'
    sessionId = $sessionId
    title = 'Hostile S2 worktree-core live 120'
    model = 'grok-hostile-validator'
}) -Options WithIndentedSequences) -Encoding utf8
Set-Content -LiteralPath $beginPath -Value (ConvertTo-Yaml -Data ([ordered]@{
    requestId = $requestId
    queryTitle = 'Hostile S2 worktree-core live 120'
    queryText = 'Attack BUG-TRIAGE-120 persist timeout on worktree plugin core copied into plugin lib layout'
    planFile = 'docs/plans/triage-cluster-002.md'
    todoId = 'BUG-TRIAGE-120'
}) -Options WithIndentedSequences) -Encoding utf8

function Invoke-PluginTimed {
    param(
        [Parameter(Mandatory)][string]$Method,
        [string]$ParamsPath,
        [int]$TimeoutSec = 12,
        [int]$KillAfterSec = 20
    )
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $psi = [System.Diagnostics.ProcessStartInfo]::new()
    $psi.FileName = 'pwsh.exe'
    foreach ($a in @(
        '-NoProfile', '-NonInteractive', '-File', $pluginExe,
        '-Command', 'Invoke',
        '-Method', $Method,
        '-WorkspacePath', $main,
        '-PluginRoot', $tempPlugin,
        '-CacheRoot', $cache,
        '-TimeoutSeconds', "$TimeoutSec"
    )) { [void]$psi.ArgumentList.Add($a) }
    if ($ParamsPath) {
        [void]$psi.ArgumentList.Add('-ParamsPath')
        [void]$psi.ArgumentList.Add($ParamsPath)
    }
    $psi.WorkingDirectory = $main
    $psi.UseShellExecute = $false
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.CreateNoWindow = $true
    $p = [System.Diagnostics.Process]::Start($psi)
    $exited = $p.WaitForExit($KillAfterSec * 1000)
    if (-not $exited) { try { $p.Kill($true) } catch { } }
    $stdout = ''
    $stderr = ''
    try { $stdout = $p.StandardOutput.ReadToEnd() } catch { }
    try { $stderr = $p.StandardError.ReadToEnd() } catch { }
    $sw.Stop()
    return [ordered]@{
        Method = $Method
        TimedOut = (-not $exited)
        ExitCode = if ($exited) { $p.ExitCode } else { $null }
        ElapsedSec = [math]::Round($sw.Elapsed.TotalSeconds, 3)
        StdOut = $stdout
        StdErr = $stderr
        TimeoutSecondsPassed = $TimeoutSec
    }
}

$boot = Invoke-PluginTimed -Method 'workflow.sessionlog.bootstrap' -TimeoutSec 20 -KillAfterSec 25
$open = Invoke-PluginTimed -Method 'workflow.sessionlog.openSession' -ParamsPath $openPath -TimeoutSec 20 -KillAfterSec 25
$begin = Invoke-PluginTimed -Method 'workflow.sessionlog.beginTurn' -ParamsPath $beginPath -TimeoutSec 12 -KillAfterSec 20
$combined = "$($begin.StdOut)`n$($begin.StdErr)"
$classified = 'unknown'
if ($begin.TimedOut) { $classified = 'hung-killed' }
elseif ($combined -match 'command_timeout') { $classified = 'classified-command-timeout' }
elseif ($combined -match 'queued/degraded|degraded|queued') { $classified = 'degraded-or-queued' }
elseif ($combined -match 'retryable') { $classified = 'classified-retryable' }
elseif ($begin.ExitCode -eq 0) { $classified = 'completed-exit0' }
elseif ($combined -match 'backend_unavailable|503') { $classified = '503-or-backend_unavailable' }
else { $classified = 'failed-unclassified' }

$obj = [ordered]@{
    TimestampUtc = [datetime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    PluginExe = $pluginExe
    TempPlugin = $tempPlugin
    SessionId = $sessionId
    RequestId = $requestId
    Bootstrap = $boot
    OpenSession = $open
    BeginTurn = $begin
    BeginClassified = $classified
    BeginHasCommandTimeout = ($combined -match 'command_timeout')
    BeginHasRetryableTrue = ($combined -match 'retryable:\s*true')
    BeginExceeded30s = ([double]$begin.ElapsedSec -gt 30)
}
$obj | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $out -Encoding utf8
Write-Output ("WROTE {0} class={1} exit={2} elapsed={3} cmdTimeout={4} retryable={5}" -f $out, $classified, $begin.ExitCode, $begin.ElapsedSec, $obj.BeginHasCommandTimeout, $obj.BeginHasRetryableTrue)
