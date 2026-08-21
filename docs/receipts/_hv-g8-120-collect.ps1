#Requires -Version 7.0
# Hostile G8/120 evidence collector. Review-only. No product edits.
[CmdletBinding()]
param(
    [string]$WorkspacePath = 'F:\GitHub\McpServer',
    [string]$PluginRoot = 'F:\GitHub\mcpserver-grok-plugin',
    [string]$OutJson = 'F:\GitHub\McpServer\docs\receipts\_hv-g8-120-out.json'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$utc = [datetime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
$stampCompact = [datetime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
$outDir = Split-Path -Parent $OutJson
if (-not (Test-Path -LiteralPath $outDir)) {
    [void][System.IO.Directory]::CreateDirectory($outDir)
}

$result = [ordered]@{
    TimestampUtc = $utc
    StampCompact = $stampCompact
    WorkspacePath = $WorkspacePath
    PluginRoot = $PluginRoot
    Git = $null
    Marker = $null
    Health = $null
    Failsafe = $null
    NamedTests = $null
    Pester = $null
    Dotnet = $null
    PluginStatus = $null
    SessionIds = $null
    PluginBeginTurn = $null
    Errors = [System.Collections.Generic.List[string]]::new()
}

function Invoke-External {
    param(
        [Parameter(Mandatory)][string]$FilePath,
        [string[]]$ArgumentList = @(),
        [string]$WorkingDirectory = $WorkspacePath,
        [int]$TimeoutSec = 120
    )
    $psi = [System.Diagnostics.ProcessStartInfo]::new()
    $psi.FileName = $FilePath
    foreach ($a in $ArgumentList) { [void]$psi.ArgumentList.Add($a) }
    $psi.WorkingDirectory = $WorkingDirectory
    $psi.UseShellExecute = $false
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.CreateNoWindow = $true
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $p = [System.Diagnostics.Process]::Start($psi)
    $exited = $p.WaitForExit($TimeoutSec * 1000)
    if (-not $exited) {
        try { $p.Kill($true) } catch { }
        $sw.Stop()
        return [ordered]@{
            TimedOut = $true
            ExitCode = $null
            ElapsedSec = [math]::Round($sw.Elapsed.TotalSeconds, 3)
            StdOut = ''
            StdErr = "KILLED after ${TimeoutSec}s"
        }
    }
    $stdout = $p.StandardOutput.ReadToEnd()
    $stderr = $p.StandardError.ReadToEnd()
    $sw.Stop()
    return [ordered]@{
        TimedOut = $false
        ExitCode = $p.ExitCode
        ElapsedSec = [math]::Round($sw.Elapsed.TotalSeconds, 3)
        StdOut = $stdout
        StdErr = $stderr
    }
}

try {
    Push-Location $WorkspacePath
    $branch = (git rev-parse --abbrev-ref HEAD 2>$null)
    $sha = (git rev-parse HEAD 2>$null)
    $dirty = (git status --porcelain 2>$null)
    $result.Git = [ordered]@{
        Branch = [string]$branch
        Sha = [string]$sha
        DirtyLineCount = @($dirty).Count
        DirtyPreview = @($dirty | Select-Object -First 20)
    }
} catch {
    $result.Errors.Add("git: $_")
} finally {
    Pop-Location
}

try {
    . (Join-Path $WorkspacePath 'plugins\core\lib-ps\marker-resolver.ps1')
    $markerPath = Join-Path $WorkspacePath 'AGENTS-README-FIRST.yaml'
    $sigOk = [bool](Test-MarkerSignature -MarkerFile $markerPath)
    $item = Get-Item -LiteralPath $markerPath
    $result.Marker = [ordered]@{
        Path = $markerPath
        SignatureOk = $sigOk
        LastWriteTimeUtc = $item.LastWriteTimeUtc.ToString('o')
    }
} catch {
    $result.Errors.Add("marker: $_")
    $result.Marker = [ordered]@{ Path = (Join-Path $WorkspacePath 'AGENTS-README-FIRST.yaml'); SignatureOk = $false; Error = "$_" }
}

try {
    $nonce = "hv-$stampCompact-$PID"
    $uri = "http://PAYTON-LEGION2:7147/health?nonce=$nonce"
    $healthSw = [System.Diagnostics.Stopwatch]::StartNew()
    $health = Invoke-RestMethod -Uri $uri -TimeoutSec 10
    $healthSw.Stop()
    $echo = $null
    if ($health -is [string]) { $echo = $health }
    elseif ($health.PSObject.Properties.Name -contains 'nonce') { $echo = [string]$health.nonce }
    $result.Health = [ordered]@{
        ElapsedSec = [math]::Round($healthSw.Elapsed.TotalSeconds, 3)
        NonceSent = $nonce
        NonceEcho = $echo
        NonceMatch = ($echo -eq $nonce)
        RawType = $health.GetType().FullName
        Raw = $health
    }
} catch {
    $result.Errors.Add("health: $_")
    $result.Health = [ordered]@{ NonceMatch = $false; Error = "$_" }
}

function Get-FailsafeInventory {
    param([string[]]$Roots)
    $files = @()
    foreach ($root in $Roots) {
        if (-not $root) { continue }
        if (-not (Test-Path -LiteralPath $root)) { continue }
        $files += @(Get-ChildItem -LiteralPath $root -Recurse -File -ErrorAction SilentlyContinue |
            Where-Object { $_.Extension -in '.yaml', '.yml' })
    }
    $items = @()
    foreach ($f in $files) {
        $head = ''
        try { $head = (Get-Content -LiteralPath $f.FullName -TotalCount 40 -ErrorAction Stop) -join "`n" } catch { $head = "$_" }
        $kind = 'unknown'
        if ($head -match 'backend_unavailable|HTTP\s*503|\b503\b') { $kind = '503-or-backend_unavailable' }
        elseif ($head -match 'timeout|timed out|command_timeout') { $kind = 'timeout' }
        elseif ($head -match 'session_submit|SessionLog.SubmitAsync') { $kind = 'session_submit' }
        $items += [ordered]@{
            Path = $f.FullName
            Length = $f.Length
            LastWriteTimeUtc = $f.LastWriteTimeUtc.ToString('o')
            KindGuess = $kind
            Head = $head.Substring(0, [Math]::Min(500, $head.Length))
        }
    }
    return [ordered]@{
        Count = $items.Count
        Items = $items
    }
}

try {
    . (Join-Path $PluginRoot 'lib\resolve-cache-dir.ps1')
    $env:MCP_WORKSPACE_PATH = $WorkspacePath
    $env:MCP_PLUGIN_ROOT = $PluginRoot
    $env:GROK_PLUGIN_ROOT = $PluginRoot
    $env:PLUGIN_AGENT_NAME = 'GrokCode'
    $cacheDir = $null
    try { $cacheDir = Resolve-McpCacheDir -StartPath $WorkspacePath } catch { $cacheDir = Join-Path $PluginRoot 'cache' }
    $failsafeDir = $null
    $quarantineDir = $null
    try { $failsafeDir = Get-McpFailsafeDir } catch { $failsafeDir = Join-Path $cacheDir 'failsafe' }
    try { $quarantineDir = Get-McpFailsafeQuarantineDir } catch { $quarantineDir = Join-Path $failsafeDir 'quarantine' }
    $pendingDir = Join-Path $cacheDir 'pending'
    $inv = Get-FailsafeInventory -Roots @(
        $failsafeDir
        $pendingDir
        (Join-Path $PluginRoot 'cache\failsafe')
        (Join-Path $WorkspacePath 'plugins\core\cache')
    )
    $result.Failsafe = [ordered]@{
        CacheDir = $cacheDir
        FailsafeDir = $failsafeDir
        QuarantineDir = $quarantineDir
        PendingDir = $pendingDir
        PendingCount = $inv.Count
        Inventory = $inv
    }
} catch {
    $result.Errors.Add("failsafe: $_")
}

$pesterPath = Join-Path $WorkspacePath 'plugins\core\test-fixtures\pester\TriagePluginIdentity.Tests.ps1'
$result.NamedTests = [ordered]@{
    PesterFile = $pesterPath
    PesterExists = (Test-Path -LiteralPath $pesterPath)
    PesterIts = @(
        'BeginTurn.SubmitTimeoutAfterFailsafe_ReturnsDegradedQueued'
        'plugin shim preserves classified retryable instead of collapsing to internal_server_error'
        'CacheScope.BackgroundOpenSession_DoesNotRebindRootActiveSession'
        'CompleteTurn.SessionIdRebind_PersistsAndClearsFailsafe'
    )
    DotnetFilters = @(
        'FullyQualifiedName~ReplMcpErrorClassifierTests'
        'FullyQualifiedName~SessionLogControllerErrorTests'
        'FullyQualifiedName~McpToolErrorEnvelopeTests'
        'FullyQualifiedName~McpToolBackendUnavailableErrorTests'
        'FullyQualifiedName~McpErrorClassifierTests'
        'FullyQualifiedName~GlobalExceptionHandlerBackendUnavailableTests'
        'FullyQualifiedName~SessionLogPersistenceDispatcherTests'
        'FullyQualifiedName~SessionLogPersistenceStrategyTests'
    )
}

try {
    $pesterOut = Join-Path $outDir '_hv-g8-120-pester.xml'
    $pesterCmd = @"
`$cfg = New-PesterConfiguration
`$cfg.Run.Path = '$pesterPath'
`$cfg.Run.PassThru = `$true
`$cfg.Output.Verbosity = 'Detailed'
`$cfg.TestResult.Enabled = `$true
`$cfg.TestResult.OutputPath = '$pesterOut'
`$r = Invoke-Pester -Configuration `$cfg
[pscustomobject]@{
    TotalCount = `$r.TotalCount
    PassedCount = `$r.PassedCount
    FailedCount = `$r.FailedCount
    SkippedCount = `$r.SkippedCount
    NotRunCount = `$r.NotRunCount
    Result = [string]`$r.Result
    Tests = @(`$r.Tests | ForEach-Object { [pscustomobject]@{ Name = `$_.Name; Result = [string]`$_.Result; Executed = `$_.Executed } })
} | ConvertTo-Json -Depth 6
"@
    $pesterRun = Invoke-External -FilePath 'pwsh.exe' -ArgumentList @('-NoProfile', '-NonInteractive', '-Command', $pesterCmd) -TimeoutSec 180
    $pesterObj = $null
    if ($pesterRun.StdOut) {
        try { $pesterObj = $pesterRun.StdOut | ConvertFrom-Json } catch { }
    }
    $result.Pester = [ordered]@{
        TimedOut = $pesterRun.TimedOut
        ExitCode = $pesterRun.ExitCode
        ElapsedSec = $pesterRun.ElapsedSec
        Parsed = $pesterObj
        StdErrTail = if ($pesterRun.StdErr) { $pesterRun.StdErr.Substring([Math]::Max(0, $pesterRun.StdErr.Length - 2000)) } else { '' }
        StdOutTail = if ($pesterRun.StdOut) { $pesterRun.StdOut.Substring([Math]::Max(0, $pesterRun.StdOut.Length - 4000)) } else { '' }
    }
} catch {
    $result.Errors.Add("pester: $_")
}

try {
    $filter = ($result.NamedTests.DotnetFilters -join '|')
    $trx = Join-Path $outDir '_hv-g8-120-dotnet.trx'
    $dotnetArgs = @(
        'test'
        (Join-Path $WorkspacePath 'McpServer.sln')
        '-c', 'Debug'
        '--filter', $filter
        '--nologo'
        '--logger', "trx;LogFileName=$trx"
    )
    $dotnetRun = Invoke-External -FilePath 'dotnet' -ArgumentList $dotnetArgs -TimeoutSec 300
    $summary = $null
    if ($dotnetRun.StdOut -match 'Passed!\s+-\s+Failed:\s+(\d+),\s+Passed:\s+(\d+),\s+Skipped:\s+(\d+),\s+Total:\s+(\d+)') {
        $summary = [ordered]@{ Failed = [int]$Matches[1]; Passed = [int]$Matches[2]; Skipped = [int]$Matches[3]; Total = [int]$Matches[4] }
    } elseif ($dotnetRun.StdOut -match 'Failed!\s+-\s+Failed:\s+(\d+),\s+Passed:\s+(\d+),\s+Skipped:\s+(\d+),\s+Total:\s+(\d+)') {
        $summary = [ordered]@{ Failed = [int]$Matches[1]; Passed = [int]$Matches[2]; Skipped = [int]$Matches[3]; Total = [int]$Matches[4] }
    }
    $result.Dotnet = [ordered]@{
        TimedOut = $dotnetRun.TimedOut
        ExitCode = $dotnetRun.ExitCode
        ElapsedSec = $dotnetRun.ElapsedSec
        Filter = $filter
        Summary = $summary
        StdOutTail = if ($dotnetRun.StdOut) { $dotnetRun.StdOut.Substring([Math]::Max(0, $dotnetRun.StdOut.Length - 6000)) } else { '' }
        StdErrTail = if ($dotnetRun.StdErr) { $dotnetRun.StdErr.Substring([Math]::Max(0, $dotnetRun.StdErr.Length - 2000)) } else { '' }
    }
} catch {
    $result.Errors.Add("dotnet: $_")
}

$pluginExe = Join-Path $PluginRoot 'lib\Invoke-McpPlugin.ps1'
try {
    $statusRun = Invoke-External -FilePath 'pwsh.exe' -ArgumentList @(
        '-NoProfile', '-NonInteractive', '-File', $pluginExe,
        '-Command', 'Status',
        '-WorkspacePath', $WorkspacePath,
        '-PluginRoot', $PluginRoot,
        '-TimeoutSeconds', '45'
    ) -TimeoutSec 50
    $result.PluginStatus = [ordered]@{
        TimedOut = $statusRun.TimedOut
        ExitCode = $statusRun.ExitCode
        ElapsedSec = $statusRun.ElapsedSec
        StdOut = $statusRun.StdOut
        StdErr = $statusRun.StdErr
    }
} catch {
    $result.Errors.Add("plugin-status: $_")
}

try {
    Import-Module (Join-Path $WorkspacePath 'tools\powershell\McpSession.psm1') -Force
    $sessionId = New-McpSessionLogSlug -Agent 'GrokCode' -Model 'grok-hostile-validator' -TimestampUtc ([datetime]::UtcNow)
    $requestId = 'req-{0}-001-hostile-g8-120-closeout' -f $stampCompact
    $result.SessionIds = [ordered]@{
        SessionId = $sessionId
        RequestId = $requestId
        PlanFile = 'docs/plans/triage-cluster-002.md'
        TodoId = 'BUG-TRIAGE-120'
    }

    $boot = Invoke-External -FilePath 'pwsh.exe' -ArgumentList @(
        '-NoProfile', '-NonInteractive', '-File', $pluginExe,
        '-Command', 'Invoke',
        '-Method', 'workflow.sessionlog.bootstrap',
        '-WorkspacePath', $WorkspacePath,
        '-PluginRoot', $PluginRoot,
        '-TimeoutSeconds', '40'
    ) -TimeoutSec 45
    $result.SessionIds.Bootstrap = [ordered]@{
        TimedOut = $boot.TimedOut; ExitCode = $boot.ExitCode; ElapsedSec = $boot.ElapsedSec
        StdOutTail = if ($boot.StdOut) { $boot.StdOut.Substring([Math]::Max(0, $boot.StdOut.Length - 1500)) } else { '' }
        StdErrTail = if ($boot.StdErr) { $boot.StdErr.Substring([Math]::Max(0, $boot.StdErr.Length - 1500)) } else { '' }
    }

    $openObj = [ordered]@{
        agent = 'GrokCode'
        sessionId = $sessionId
        title = 'Hostile G8 BUG-TRIAGE-120 closeout'
        model = 'grok-hostile-validator'
    }
    $openJson = Join-Path $outDir '_hv-g8-120-open.json'
    $openObj | ConvertTo-Json -Compress | Set-Content -LiteralPath $openJson -Encoding utf8
    $openYamlCmd = @"
Import-Module '$PluginRoot\lib\McpPluginShim.psm1' -ErrorAction SilentlyContinue
. '$WorkspacePath\plugins\core\lib-ps\yaml-object-mutation.ps1'
Import-McpYamlSerializer
`$o = Get-Content -LiteralPath '$openJson' -Raw | ConvertFrom-Json
`$yaml = ConvertTo-Yaml -Data `$o -Options WithIndentedSequences
Set-Content -LiteralPath '$outDir\_hv-g8-120-open.yaml' -Value `$yaml -Encoding utf8
"@
    [void](Invoke-External -FilePath 'pwsh.exe' -ArgumentList @('-NoProfile', '-NonInteractive', '-Command', $openYamlCmd) -TimeoutSec 30)

    $open = Invoke-External -FilePath 'pwsh.exe' -ArgumentList @(
        '-NoProfile', '-NonInteractive', '-File', $pluginExe,
        '-Command', 'Invoke',
        '-Method', 'workflow.sessionlog.openSession',
        '-ParamsPath', (Join-Path $outDir '_hv-g8-120-open.yaml'),
        '-WorkspacePath', $WorkspacePath,
        '-PluginRoot', $PluginRoot,
        '-TimeoutSeconds', '40'
    ) -TimeoutSec 45
    $result.SessionIds.OpenSession = [ordered]@{
        TimedOut = $open.TimedOut; ExitCode = $open.ExitCode; ElapsedSec = $open.ElapsedSec
        StdOutTail = if ($open.StdOut) { $open.StdOut.Substring([Math]::Max(0, $open.StdOut.Length - 1500)) } else { '' }
        StdErrTail = if ($open.StdErr) { $open.StdErr.Substring([Math]::Max(0, $open.StdErr.Length - 1500)) } else { '' }
    }

    $beginObj = [ordered]@{
        requestId = $requestId
        queryTitle = 'Hostile G8 120 closeout review'
        queryText = 'Hostile validate leftover BUG-TRIAGE-120 closeout-first on develop'
        planFile = 'docs/plans/triage-cluster-002.md'
        todoId = 'BUG-TRIAGE-120'
    }
    $beginJson = Join-Path $outDir '_hv-g8-120-begin.json'
    $beginObj | ConvertTo-Json -Compress | Set-Content -LiteralPath $beginJson -Encoding utf8
    $beginYamlCmd = @"
. '$WorkspacePath\plugins\core\lib-ps\yaml-object-mutation.ps1'
Import-McpYamlSerializer
`$o = Get-Content -LiteralPath '$beginJson' -Raw | ConvertFrom-Json
`$yaml = ConvertTo-Yaml -Data `$o -Options WithIndentedSequences
Set-Content -LiteralPath '$outDir\_hv-g8-120-begin.yaml' -Value `$yaml -Encoding utf8
"@
    [void](Invoke-External -FilePath 'pwsh.exe' -ArgumentList @('-NoProfile', '-NonInteractive', '-Command', $beginYamlCmd) -TimeoutSec 30)

    $begin = Invoke-External -FilePath 'pwsh.exe' -ArgumentList @(
        '-NoProfile', '-NonInteractive', '-File', $pluginExe,
        '-Command', 'Invoke',
        '-Method', 'workflow.sessionlog.beginTurn',
        '-ParamsPath', (Join-Path $outDir '_hv-g8-120-begin.yaml'),
        '-WorkspacePath', $WorkspacePath,
        '-PluginRoot', $PluginRoot,
        '-TimeoutSeconds', '40'
    ) -TimeoutSec 45
    $classified = 'unknown'
    $combined = "$($begin.StdOut)`n$($begin.StdErr)"
    if ($begin.TimedOut) { $classified = 'hung-killed' }
    elseif ($combined -match 'queued/degraded|degraded|queued') { $classified = 'degraded-or-queued' }
    elseif ($combined -match 'retryable') { $classified = 'classified-retryable' }
    elseif ($begin.ExitCode -eq 0) { $classified = 'completed-exit0' }
    elseif ($combined -match 'backend_unavailable|503') { $classified = '503-or-backend_unavailable' }
    else { $classified = 'failed-unclassified' }

    $result.PluginBeginTurn = [ordered]@{
        TimedOut = $begin.TimedOut
        ExitCode = $begin.ExitCode
        ElapsedSec = $begin.ElapsedSec
        Classified = $classified
        Exceeded30s = ($begin.ElapsedSec -gt 30)
        StdOut = $begin.StdOut
        StdErr = $begin.StdErr
    }
} catch {
    $result.Errors.Add("beginTurn: $_")
}

$result | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $OutJson -Encoding utf8
Write-Output "WROTE $OutJson"
if ($result.SessionIds) {
    Write-Output ("SESSION " + $result.SessionIds.SessionId)
    Write-Output ("REQUEST " + $result.SessionIds.RequestId)
}
