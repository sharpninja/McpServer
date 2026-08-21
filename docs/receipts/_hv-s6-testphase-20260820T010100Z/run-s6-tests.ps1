$ErrorActionPreference = 'Stop'
$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-s6-testphase-20260820T010100Z'
New-Item -ItemType Directory -Path $outDir -Force | Out-Null

$worktree = 'F:\GitHub\McpServer\.worktrees\triage-stale-turns'

Write-Output '=== PESTER TriagePluginIdentity.Tests.ps1 ==='
$pesterXml = Join-Path $outDir 'pester-TriagePluginIdentity.nunit.xml'
$pesterOut = Join-Path $outDir 'pester-TriagePluginIdentity.txt'
$cfg = New-PesterConfiguration
$cfg.Run.Path = (Join-Path $worktree 'plugins\core\test-fixtures\pester\TriagePluginIdentity.Tests.ps1')
$cfg.Run.PassThru = $true
$cfg.Output.Verbosity = 'Detailed'
$cfg.TestResult.Enabled = $true
$cfg.TestResult.OutputPath = $pesterXml
$cfg.TestResult.OutputFormat = 'NUnitXml'
$pester = Invoke-Pester -Configuration $cfg
$pesterLines = @(
    ('Pester Result={0} Passed={1} Failed={2} Skipped={3} Total={4}' -f $pester.Result, $pester.PassedCount, $pester.FailedCount, $pester.SkippedCount, $pester.TotalCount)
)
foreach ($t in $pester.Tests) {
    $pesterLines += ('[{0}] {1}' -f $t.Result, $t.ExpandedName)
}
$pesterLines | Set-Content -LiteralPath $pesterOut -Encoding utf8
$pesterLines | ForEach-Object { Write-Output $_ }

Write-Output '=== DOTNET SessionLogTriageStoreTests ==='
$storeProj = Join-Path $worktree 'tests\McpServer.Support.Mcp.Tests\McpServer.Support.Mcp.Tests.csproj'
$storeLog = Join-Path $outDir 'SessionLogTriageStoreTests.txt'
& dotnet test $storeProj -c Debug --filter 'FullyQualifiedName~SessionLogTriageStoreTests' --logger 'trx;LogFileName=SessionLogTriageStoreTests.trx' --results-directory $outDir | Tee-Object -FilePath $storeLog
$storeExit = $LASTEXITCODE

Write-Output '=== DOTNET QueryAsync_RequestObjectPassesTurnStatusAndStaleOlderThanHours ==='
$clientProj = Join-Path $worktree 'tests\McpServer.Client.Tests\McpServer.Client.Tests.csproj'
$clientLog = Join-Path $outDir 'Client-QueryAsync-TurnStatus.txt'
& dotnet test $clientProj -c Debug --filter 'FullyQualifiedName~QueryAsync_RequestObjectPassesTurnStatusAndStaleOlderThanHours' --logger 'trx;LogFileName=Client-QueryAsync-TurnStatus.trx' --results-directory $outDir | Tee-Object -FilePath $clientLog
$clientExit = $LASTEXITCODE

$summary = [ordered]@{
    PesterPassed = $pester.PassedCount
    PesterFailed = $pester.FailedCount
    PesterSkipped = $pester.SkippedCount
    PesterTotal = $pester.TotalCount
    PesterResult = [string]$pester.Result
    StoreExit = $storeExit
    ClientExit = $clientExit
}
$summary | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $outDir 'summary.json') -Encoding utf8
Write-Output '=== SUMMARY ==='
Write-Output ($summary | ConvertTo-Json)
if ($pester.FailedCount -ne 0 -or $pester.SkippedCount -ne 0 -or $storeExit -ne 0 -or $clientExit -ne 0) {
    exit 1
}
exit 0
