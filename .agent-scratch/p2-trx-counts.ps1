$ErrorActionPreference = 'Continue'
$wt = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
$run = 'p2-unit-legion-20261002T193045Z'
$unit = Join-Path $wt "TestResults\$run\unit"
Write-Output '=== SELECTED ==='
Get-Content -LiteralPath (Join-Path $unit 'selected-projects.json') -Raw
Write-Output '=== TRX COUNTERS ==='
$sumPassed = 0
$sumFailed = 0
$sumTotal = 0
Get-ChildItem -LiteralPath $unit -Recurse -Filter '*.trx' | ForEach-Object {
    [xml]$x = Get-Content -LiteralPath $_.FullName
    $c = $x.TestRun.ResultSummary.Counters
    $outcome = $x.TestRun.ResultSummary.outcome
    Write-Output ($_.Directory.Name + ' outcome=' + $outcome + ' total=' + $c.total + ' passed=' + $c.passed + ' failed=' + $c.failed + ' executed=' + $c.executed)
    $sumPassed += [int]$c.passed
    $sumFailed += [int]$c.failed
    $sumTotal += [int]$c.total
    if ([int]$c.failed -gt 0) {
        $nodes = $x.SelectNodes('//UnitTestResult[@outcome="Failed"]')
        $shown = 0
        foreach ($n in $nodes) {
            if ($shown -ge 12) { break }
            Write-Output ('  FAILED_TEST=' + $n.testName)
            $shown++
        }
        Write-Output ('  FAILED_NODE_COUNT=' + @($nodes).Count)
    }
}
Write-Output ('SUM_TOTAL=' + $sumTotal + ' SUM_PASSED=' + $sumPassed + ' SUM_FAILED=' + $sumFailed)
Write-Output '=== PLUGININTEGRATION TEST RUN ==='
$nuke = Join-Path $wt "TestResults\$run\logs\nuke-test.log"
$pi = Select-String -LiteralPath $nuke -Pattern 'Test run for .*PluginIntegration'
if ($pi) { $pi | ForEach-Object { $_.Line.Trim() } } else { Write-Output 'NO_PLUGININTEGRATION_TEST_RUN' }
