#Requires -Version 7.0
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$worktree = 'C:\Users\kingd\.grok\worktrees\github-mcpserver\subagent-01a029d1-24e5-7603-879b-832cfd4fe7f7'
$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-e-red-20260822T151657Z'
[void][System.IO.Directory]::CreateDirectory($outDir)
Set-Location -LiteralPath $worktree

$csproj = 'tests\McpServer.Support.Mcp.Tests\McpServer.Support.Mcp.Tests.csproj'
$listLog = Join-Path $outDir 'hostile-review-list.log'
$runLog = Join-Path $outDir 'hostile-review-run.log'
$trxName = 'hostile-review-hv-e-red.trx'

Write-Output 'LIST_START'
$listOutput = & dotnet test $csproj -c Debug --filter 'FullyQualifiedName~HostileReview' --list-tests 2>&1 | Out-String
Set-Content -LiteralPath $listLog -Value $listOutput -Encoding utf8
Write-Output $listOutput
Write-Output ('LIST_EXIT=' + $LASTEXITCODE)

Write-Output 'RUN_START'
$runOutput = & dotnet test $csproj -c Debug --filter 'FullyQualifiedName~HostileReview' --logger ('trx;LogFileName=' + $trxName) --results-directory $outDir 2>&1 | Out-String
Set-Content -LiteralPath $runLog -Value $runOutput -Encoding utf8
Write-Output $runOutput
Write-Output ('RUN_EXIT=' + $LASTEXITCODE)
Write-Output ('RUN_LOG=' + $runLog)
