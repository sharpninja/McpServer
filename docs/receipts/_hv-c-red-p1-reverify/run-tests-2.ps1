#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'
$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p1-reverify'
Set-Location -LiteralPath 'F:\GitHub\McpServer'
$trx = Join-Path $out 'hv-c-red-p1-rerun2.trx'
$log = Join-Path $out 'dotnet-test-rerun2.log'
$filter = 'FullyQualifiedName~NukeBuild.Tests.PluginSessionLogIntegrationTargetTests'
$dotnetArgs = @(
    'test', 'tests/Build.Tests',
    '-c', 'Debug',
    '--filter', $filter,
    '--logger', ('trx;LogFileName=' + $trx),
    '--results-directory', $out,
    '--nologo'
)
& dotnet @dotnetArgs *>&1 | Tee-Object -FilePath $log
$code = $LASTEXITCODE
Set-Content -LiteralPath (Join-Path $out 'dotnet-test-rerun2-exit.txt') -Value ([string]$code) -Encoding utf8
Write-Output ('RERUN2_EXIT=' + $code)
