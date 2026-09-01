#Requires -Version 7.0
[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$ws = 'F:\GitHub\McpServer'
$plugin = 'F:\GitHub\mcpserver-grok-plugin'
$out = 'F:\GitHub\McpServer\docs\receipts\_hv-g8-120-06.json'
$pluginExe = Join-Path $plugin 'lib\Invoke-McpPlugin.ps1'
Import-Module (Join-Path $ws 'tools\powershell\McpSession.psm1') -Force
$stamp = [datetime]::UtcNow
$sessionId = New-McpSessionLogSlug -Agent 'GrokCode' -Model 'hostile-g8-120' -TimestampUtc $stamp
$requestId = 'req-{0}-001-hostile-g8-120-closeout' -f $stamp.ToUniversalTime().ToString('yyyyMMddTHHmmssZ')

. (Join-Path $ws 'plugins\core\lib-ps\yaml-object-mutation.ps1')
Import-McpYamlSerializer

$openPath = 'F:\GitHub\McpServer\docs\receipts\_hv-g8-120-open.yaml'
$beginPath = 'F:\GitHub\McpServer\docs\receipts\_hv-g8-120-begin.yaml'
$openObj = [ordered]@{
    agent = 'GrokCode'
    sessionId = $sessionId
    title = 'Hostile G8 BUG-TRIAGE-120 closeout'
    model = 'grok-hostile-validator'
}
$beginObj = [ordered]@{
    requestId = $requestId
    queryTitle = 'Hostile G8 120 closeout review'
    queryText = 'Hostile validate leftover BUG-TRIAGE-120 closeout-first on develop'
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
        [int]$KillAfterSec = 45
    )
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $psi = [System.Diagnostics.ProcessStartInfo]::new()
    $psi.FileName = 'pwsh.exe'
    $args = [System.Collections.Generic.List[string]]::new()
    foreach ($a in @(
        '-NoProfile', '-NonInteractive', '-File', $pluginExe,
        '-Command', 'Invoke',
        '-Method', $Method,
        '-WorkspacePath', $ws,
        '-PluginRoot', $plugin,
        '-TimeoutSeconds', "$TimeoutSec"
    )) { $args.Add($a) }
    if ($ParamsPath) {
        $args.Add('-ParamsPath')
        $args.Add($ParamsPath)
    }
    foreach ($a in $args) { [void]$psi.ArgumentList.Add($a) }
    $psi.WorkingDirectory = $ws
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
    }
}

$boot = Invoke-PluginTimed -Method 'workflow.sessionlog.bootstrap' -TimeoutSec 25 -KillAfterSec 30
$open = Invoke-PluginTimed -Method 'workflow.sessionlog.openSession' -ParamsPath $openPath -TimeoutSec 35 -KillAfterSec 40
$begin = Invoke-PluginTimed -Method 'workflow.sessionlog.beginTurn' -ParamsPath $beginPath -TimeoutSec 40 -KillAfterSec 45

$combined = "$($begin.StdOut)`n$($begin.StdErr)"
$classified = 'unknown'
if ($begin.TimedOut) { $classified = 'hung-killed' }
elseif ($combined -match 'queued/degraded|degraded|queued') { $classified = 'degraded-or-queued' }
elseif ($combined -match 'retryable') { $classified = 'classified-retryable' }
elseif ($begin.ExitCode -eq 0) { $classified = 'completed-exit0' }
elseif ($combined -match 'backend_unavailable|503') { $classified = '503-or-backend_unavailable' }
else { $classified = 'failed-unclassified' }

$obj = [ordered]@{
    TimestampUtc = [datetime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    SessionId = $sessionId
    RequestId = $requestId
    PlanFile = 'docs/plans/triage-cluster-002.md'
    TodoId = 'BUG-TRIAGE-120'
    Bootstrap = $boot
    OpenSession = $open
    BeginTurn = $begin
    BeginClassified = $classified
    BeginExceeded30s = ([double]$begin.ElapsedSec -gt 30)
}
$obj | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $out -Encoding utf8
Write-Output ("WROTE {0} session={1} req={2} beginTimedOut={3} beginExit={4} elapsed={5} class={6}" -f $out, $sessionId, $requestId, $begin.TimedOut, $begin.ExitCode, $begin.ElapsedSec, $classified)
