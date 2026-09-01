#Requires -Version 7.0
[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$ws = 'F:\GitHub\McpServer'
$out = 'F:\GitHub\McpServer\docs\receipts\_hv-g8-120-02.json'
$trx = 'F:\GitHub\McpServer\docs\receipts\_hv-g8-120-02.trx'
$filter = 'FullyQualifiedName~ReplMcpErrorClassifierTests|FullyQualifiedName~SessionLogPersistenceDispatcherTests|FullyQualifiedName~SessionLogPersistenceStrategyTests|FullyQualifiedName~ContractCorrectnessTests'
$sw = [System.Diagnostics.Stopwatch]::StartNew()
$psi = [System.Diagnostics.ProcessStartInfo]::new()
$psi.FileName = 'dotnet'
foreach ($a in @(
    'test', (Join-Path $ws 'tests\McpServer.Repl.Core.Tests\McpServer.Repl.Core.Tests.csproj'),
    '-c', 'Debug', '--nologo', '--filter', $filter,
    '--logger', "trx;LogFileName=$trx"
)) { [void]$psi.ArgumentList.Add($a) }
$psi.WorkingDirectory = $ws
$psi.UseShellExecute = $false
$psi.RedirectStandardOutput = $true
$psi.RedirectStandardError = $true
$p = [System.Diagnostics.Process]::Start($psi)
if (-not $p.WaitForExit(180000)) {
    try { $p.Kill($true) } catch { }
    throw 'dotnet test Repl.Core timed out at 180s'
}
$stdout = $p.StandardOutput.ReadToEnd()
$stderr = $p.StandardError.ReadToEnd()
$sw.Stop()
$summary = $null
if ($stdout -match 'Passed!\s+-\s+Failed:\s+(\d+),\s+Passed:\s+(\d+),\s+Skipped:\s+(\d+),\s+Total:\s+(\d+)') {
    $summary = [ordered]@{ Outcome = 'Passed'; Failed = [int]$Matches[1]; Passed = [int]$Matches[2]; Skipped = [int]$Matches[3]; Total = [int]$Matches[4] }
} elseif ($stdout -match 'Failed!\s+-\s+Failed:\s+(\d+),\s+Passed:\s+(\d+),\s+Skipped:\s+(\d+),\s+Total:\s+(\d+)') {
    $summary = [ordered]@{ Outcome = 'Failed'; Failed = [int]$Matches[1]; Passed = [int]$Matches[2]; Skipped = [int]$Matches[3]; Total = [int]$Matches[4] }
}
$obj = [ordered]@{
    TimestampUtc = [datetime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    ExitCode = $p.ExitCode
    ElapsedSec = [math]::Round($sw.Elapsed.TotalSeconds, 3)
    Filter = $filter
    Summary = $summary
    StdOut = $stdout
    StdErr = $stderr
}
$obj | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $out -Encoding utf8
Write-Output ("WROTE {0} exit={1} summary={2}" -f $out, $p.ExitCode, ($summary | ConvertTo-Json -Compress))
