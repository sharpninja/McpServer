#Requires -Version 7.0
[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$main = 'F:\GitHub\McpServer'
$plugin = 'F:\GitHub\mcpserver-grok-plugin'
$pluginExe = Join-Path $plugin 'lib\Invoke-McpPlugin.ps1'
$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-s2-hgreen'
$out = Join-Path $outDir '06-live-120-grokplugin.json'

Import-Module (Join-Path $main 'tools\powershell\McpSession.psm1') -Force
$stamp = [datetime]::UtcNow
$sessionId = New-McpSessionLogSlug -Agent 'GrokCode' -Model 'hostile-s2-120-live' -TimestampUtc $stamp
$requestId = 'req-{0}-001-hostile-s2-120-live' -f $stamp.ToUniversalTime().ToString('yyyyMMddTHHmmssZ')

. (Join-Path $plugin 'lib\yaml-object-mutation.ps1')
Import-McpYamlSerializer

$openPath = Join-Path $outDir '06-open.yaml'
$beginPath = Join-Path $outDir '06-begin.yaml'
$openObj = [ordered]@{
    agent = 'GrokCode'
    sessionId = $sessionId
    title = 'Hostile S2 live grok plugin 120 beginTurn'
    model = 'grok-hostile-validator'
}
$beginObj = [ordered]@{
    requestId = $requestId
    queryTitle = 'Hostile S2 live grok plugin 120'
    queryText = 'Attack BUG-TRIAGE-120 on installed grok plugin Invoke-McpPlugin'
    planFile = 'docs/plans/triage-cluster-002.md'
    todoId = 'BUG-TRIAGE-120'
}
Set-Content -LiteralPath $openPath -Value (ConvertTo-Yaml -Data $openObj -Options WithIndentedSequences) -Encoding utf8
Set-Content -LiteralPath $beginPath -Value (ConvertTo-Yaml -Data $beginObj -Options WithIndentedSequences) -Encoding utf8

function Invoke-PluginTimed {
    param(
        [Parameter(Mandatory)][string]$Method,
        [string]$ParamsPath,
        [int]$TimeoutSec = 40,
        [int]$KillAfterSec = 50
    )
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $psi = [System.Diagnostics.ProcessStartInfo]::new()
    $psi.FileName = 'pwsh.exe'
    foreach ($a in @(
        '-NoProfile', '-NonInteractive', '-File', $pluginExe,
        '-Command', 'Invoke',
        '-Method', $Method,
        '-WorkspacePath', $main,
        '-PluginRoot', $plugin,
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
    if (-not $exited) {
        try { $p.Kill($true) } catch { }
    }
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

$boot = Invoke-PluginTimed -Method 'workflow.sessionlog.bootstrap' -TimeoutSec 25 -KillAfterSec 30
$open = Invoke-PluginTimed -Method 'workflow.sessionlog.openSession' -ParamsPath $openPath -TimeoutSec 35 -KillAfterSec 40
$begin = Invoke-PluginTimed -Method 'workflow.sessionlog.beginTurn' -ParamsPath $beginPath -TimeoutSec 40 -KillAfterSec 50

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
Write-Output ("WROTE {0} class={1} exit={2} elapsed={3} cmdTimeout={4}" -f $out, $classified, $begin.ExitCode, $begin.ElapsedSec, $obj.BeginHasCommandTimeout)
