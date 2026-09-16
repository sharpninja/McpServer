#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p14'
$script = Join-Path $out 'hv-tests-run.ps1'
$stdout = Join-Path $out 'hv-tests-run.stdout.log'
$stderr = Join-Path $out 'hv-tests-run.stderr.log'
$waitLog = Join-Path $out 'hv-wait-p14-hosts.json'

function Get-BusyCount {
    $n = 0
    $hints = [System.Collections.Generic.List[string]]::new()
    $pids = [System.Collections.Generic.List[int]]::new()
    foreach ($p in @(Get-CimInstance Win32_Process)) {
        $name = [string]$p.Name
        $cmd = [string]$p.CommandLine
        if ([string]::IsNullOrWhiteSpace($cmd)) { continue }
        $isTestHost = ($name -match 'testhost|vstest|dotnet') -and ($cmd -match 'PluginIntegration|McpServer.PluginIntegration')
        $isWaitScript = ($name -match 'pwsh') -and ($cmd -match 'wait-then-tests\.ps1' -or $cmd -match '\\tests\.ps1')
        if (-not ($isTestHost -or $isWaitScript)) { continue }
        $n++
        $pids.Add([int]$p.ProcessId)
        if ($cmd.Length -gt 180) { $cmd = $cmd.Substring(0, 180) }
        $hints.Add($name + ':' + $p.ProcessId + ':' + $cmd)
    }
    return [pscustomobject]@{ Count = $n; ProcessIds = @($pids); Hints = @($hints) }
}

$deadline = [DateTime]::UtcNow.AddMinutes(20)
$snapshots = [System.Collections.Generic.List[object]]::new()
Write-Output ("WAIT_START=" + [DateTime]::UtcNow.ToString('o'))
do {
    $busy = Get-BusyCount
    $snap = [ordered]@{
        TimestampUtc = [DateTime]::UtcNow.ToString('o')
        Count = [int]$busy.Count
        ProcessIds = @($busy.ProcessIds)
        CommandHints = @($busy.Hints)
    }
    $snapshots.Add($snap)
    Write-Output ("WAIT_COUNT=" + $busy.Count + " UTC=" + $snap.TimestampUtc)
    if ([int]$busy.Count -eq 0) { break }
    Start-Sleep -Seconds 15
} while ([DateTime]::UtcNow -lt $deadline)

$final = Get-BusyCount
[ordered]@{
    RemainingCount = [int]$final.Count
    RemainingPids = @($final.ProcessIds)
    SnapshotCount = $snapshots.Count
    Snapshots = @($snapshots)
    TimedOut = ([int]$final.Count -gt 0)
    FinishedUtc = [DateTime]::UtcNow.ToString('o')
} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $waitLog -Encoding utf8

if ([int]$final.Count -gt 0) {
    Write-Output 'WAIT_TIMEOUT_REMAINING_PROCS'
    [ordered]@{
        Aborted = $true
        Reason = 'busy PluginIntegration testhosts or tests.ps1 remain'
        RemainingCount = [int]$final.Count
    } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'hv-tests-aborted.json') -Encoding utf8
    Write-Output 'ABORTED'
    exit 2
}

if (Test-Path -LiteralPath $stdout) { Remove-Item -LiteralPath $stdout -Force }
if (Test-Path -LiteralPath $stderr) { Remove-Item -LiteralPath $stderr -Force }

$p = Start-Process -FilePath 'pwsh.exe' -ArgumentList @(
    '-NoProfile',
    '-NonInteractive',
    '-File',
    $script
) -WorkingDirectory 'F:\GitHub\McpServer' -RedirectStandardOutput $stdout -RedirectStandardError $stderr -PassThru -WindowStyle Hidden

[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    Pid = $p.Id
    Script = $script
    Stdout = $stdout
    Stderr = $stderr
    HasExited = [bool]$p.HasExited
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'hv-tests-run.pid.json') -Encoding utf8
Write-Output ('STARTED_PID=' + $p.Id)
Write-Output 'DETACHED_OK'
