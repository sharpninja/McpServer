$ErrorActionPreference = 'Continue'
$wt = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
$run = 'p2-unit-legion-20261002T193045Z'
$root = Join-Path $wt (Join-Path 'TestResults' $run)
$log = Join-Path $wt "docs\receipts\hv\$run-gate.log"
Set-Location $wt
Write-Output ('HEAD=' + (git rev-parse HEAD).Trim())
Write-Output ('SHIM_EXISTS=' + (Test-Path 'C:\Users\kingd\.dotnet\tools\pwsh.exe'))
Write-Output ('ASIDE_EXISTS=' + (Test-Path 'C:\Users\kingd\.dotnet\tools\pwsh.exe.p2gate-aside'))
$item = Get-Item -LiteralPath $log
$hash = Get-FileHash -LiteralPath $log -Algorithm SHA256
Write-Output ('LOG_BYTES=' + $item.Length)
Write-Output ('LOG_SHA256=' + $hash.Hash)
Write-Output '=== KEY LINES ==='
Select-String -LiteralPath $log -Pattern 'ACCEPTED|GATE_EXIT|PluginIntegration|Failed!|Passed!|Total tests|CheckSessionLife|not accepted|Rejected|Build.Tests' |
    Select-Object -Last 40 |
    ForEach-Object { $_.Line.Trim().Substring(0, [Math]::Min(220, $_.Line.Trim().Length)) }
Write-Output '=== COMMAND RESULTS ==='
Get-Content -LiteralPath (Join-Path $root 'command-results.json') -Raw
Write-Output '=== NATIVE ==='
Get-Content -LiteralPath (Join-Path $root 'pester\native-run.json') -Raw
Write-Output '=== TRX COUNTS ==='
$trx = Get-ChildItem -LiteralPath (Join-Path $root 'build-tests') -Filter '*.trx' -Recurse -File | Select-Object -First 1
if ($trx) {
    Write-Output ('TRX=' + $trx.FullName + ' BYTES=' + $trx.Length)
    [xml]$x = Get-Content -LiteralPath $trx.FullName
    $c = $x.TestRun.ResultSummary.Counters
    Write-Output ('BUILD_TOTAL=' + $c.total + ' PASSED=' + $c.passed + ' FAILED=' + $c.failed + ' EXECUTED=' + $c.executed)
} else {
    Write-Output 'TRX_MISSING'
}
Write-Output '=== NUKE SUMMARY LINES ==='
$nuke = Join-Path $root 'logs\nuke-test.log'
Select-String -LiteralPath $nuke -Pattern 'PluginIntegration|Passed!|Failed!|Total tests|test run' |
    Select-Object -Last 30 |
    ForEach-Object { $_.Line.Trim().Substring(0, [Math]::Min(240, $_.Line.Trim().Length)) }
