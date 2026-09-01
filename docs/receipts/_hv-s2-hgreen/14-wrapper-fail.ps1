#Requires -Version 7.0
[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$wt = 'F:\GitHub\McpServer\.worktrees\triage-plugin-core'
$out = 'F:\GitHub\McpServer\docs\receipts\_hv-s2-hgreen\14-wrapper-fail.json'
$staged = Join-Path $wt 'plugins\core\.staged-plugin'
$sessionEnd = Join-Path $staged 'hooks\scripts\session-end.ps1'
$integrity = Join-Path $wt 'plugins\core\test-fixtures\check-core-integrity.ps1'
if (-not (Test-Path -LiteralPath $integrity)) {
    $integrity = @(Get-ChildItem -LiteralPath (Join-Path $wt 'plugins\core') -Recurse -Filter '*integrity*' -File | Select-Object -First 5 | ForEach-Object { $_.FullName })
}

$stagedExists = Test-Path -LiteralPath $staged
$sessionEndExists = Test-Path -LiteralPath $sessionEnd
$stderr = ''
$stdout = ''
$exit = $null
$elapsed = $null
if ($sessionEndExists) {
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $psi = [System.Diagnostics.ProcessStartInfo]::new()
    $psi.FileName = 'pwsh.exe'
    foreach ($a in @('-NoProfile','-NonInteractive','-File',$sessionEnd)) { [void]$psi.ArgumentList.Add($a) }
    $psi.WorkingDirectory = $wt
    $psi.UseShellExecute = $false
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.CreateNoWindow = $true
    $p = [System.Diagnostics.Process]::Start($psi)
    [void]$p.WaitForExit(15000)
    $stdout = $p.StandardOutput.ReadToEnd()
    $stderr = $p.StandardError.ReadToEnd()
    $exit = $p.ExitCode
    $sw.Stop()
    $elapsed = [math]::Round($sw.Elapsed.TotalSeconds, 3)
}

$obj = [ordered]@{
    TimestampUtc = [datetime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    StagedExists = $stagedExists
    SessionEndExists = $sessionEndExists
    SessionEndPath = $sessionEnd
    ExitCode = $exit
    ElapsedSec = $elapsed
    StdOut = $stdout
    StdErr = $stderr
    IntegrityCandidates = $integrity
}
$obj | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $out -Encoding utf8
Write-Output ("WROTE {0} staged={1} sessionEnd={2} exit={3}" -f $out, $stagedExists, $sessionEndExists, $exit)
