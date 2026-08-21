#Requires -Version 7.0
[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$main = 'F:\GitHub\McpServer'
$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-s2-hgreen'
$out = Join-Path $outDir '17-live-120-force-timeout.json'
$tempPlugin = Join-Path $outDir 'temp-plugin-core'
$pluginExe = Join-Path $tempPlugin 'lib\Invoke-McpPlugin.ps1'
$cache = Join-Path $outDir 'cache-core-120-timeout'
[void][System.IO.Directory]::CreateDirectory($cache)

Import-Module (Join-Path $main 'tools\powershell\McpSession.psm1') -Force
$stamp = [datetime]::UtcNow
$sessionId = New-McpSessionLogSlug -Agent 'GrokCode' -Model 'hostile-s2-120-to' -TimestampUtc $stamp
$requestId = 'req-{0}-001-hostile-s2-120-to' -f $stamp.ToUniversalTime().ToString('yyyyMMddTHHmmssZ')

. (Join-Path $tempPlugin 'lib\yaml-object-mutation.ps1')
Import-McpYamlSerializer
$openPath = Join-Path $outDir '17-open.yaml'
$beginPath = Join-Path $outDir '17-begin.yaml'
Set-Content -LiteralPath $openPath -Value (ConvertTo-Yaml -Data ([ordered]@{
    agent = 'GrokCode'
    sessionId = $sessionId
    title = 'Hostile S2 forced wrapper timeout'
    model = 'grok-hostile-validator'
}) -Options WithIndentedSequences) -Encoding utf8
Set-Content -LiteralPath $beginPath -Value (ConvertTo-Yaml -Data ([ordered]@{
    requestId = $requestId
    queryTitle = 'Hostile S2 forced wrapper timeout'
    queryText = 'Force Invoke-McpPlugin TimeoutSeconds 1 on beginTurn to classify command_timeout'
    planFile = 'docs/plans/triage-cluster-002.md'
    todoId = 'BUG-TRIAGE-120'
}) -Options WithIndentedSequences) -Encoding utf8

function Invoke-PluginTimed {
    param(
        [Parameter(Mandatory)][string]$Method,
        [string]$ParamsPath,
        [int]$TimeoutSec = 1,
        [int]$KillAfterSec = 8
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

# Open first with a generous budget so beginTurn is the forced timeout.
$open = Invoke-PluginTimed -Method 'workflow.sessionlog.openSession' -ParamsPath $openPath -TimeoutSec 20 -KillAfterSec 25
$begin = Invoke-PluginTimed -Method 'workflow.sessionlog.beginTurn' -ParamsPath $beginPath -TimeoutSec 1 -KillAfterSec 8
$combined = "$($begin.StdOut)`n$($begin.StdErr)"
$classified = 'unknown'
if ($begin.TimedOut) { $classified = 'hung-killed' }
elseif ($combined -match 'command_timeout') { $classified = 'classified-command-timeout' }
elseif ($combined -match 'queued/degraded|degraded|queued') { $classified = 'degraded-or-queued' }
elseif ($combined -match 'retryable') { $classified = 'classified-retryable' }
elseif ($begin.ExitCode -eq 0) { $classified = 'completed-exit0' }
else { $classified = 'failed-unclassified' }

$obj = [ordered]@{
    TimestampUtc = [datetime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    SessionId = $sessionId
    RequestId = $requestId
    OpenSession = $open
    BeginTurn = $begin
    BeginClassified = $classified
    BeginHasCommandTimeout = ($combined -match 'command_timeout')
    BeginHasRetryableTrue = ($combined -match 'retryable:\s*true')
    BeginHasDegraded = ($combined -match 'degraded')
    BeginHasUnclassifiedThrow = ($combined -match 'throw' -and $combined -notmatch 'command_timeout')
}
$obj | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $out -Encoding utf8
Write-Output ("WROTE {0} class={1} exit={2} elapsed={3} cmdTimeout={4} retryable={5}" -f $out, $classified, $begin.ExitCode, $begin.ElapsedSec, $obj.BeginHasCommandTimeout, $obj.BeginHasRetryableTrue)
