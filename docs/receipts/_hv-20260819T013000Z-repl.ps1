$ErrorActionPreference = 'Continue'
$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-20260819T013000Z'
$proj = 'F:\GitHub\McpServer\tests\McpServer.Repl.Core.Tests\McpServer.Repl.Core.Tests.csproj'
$filter = @(
  'FullyQualifiedName~GetTr',
  'FullyQualifiedName~ValidateTrId',
  'FullyQualifiedName~LegacyTr',
  'FullyQualifiedName~LooksLikeProgressOnly',
  'FullyQualifiedName~EchoHelper',
  'FullyQualifiedName~UseEchoHelper',
  'FullyQualifiedName~AgentHelp'
) -join '|'
$log = Join-Path $outDir 'named-repl.log'
Write-Output 'START_REPL'
& dotnet test $proj -c Debug --no-build --filter $filter --nologo --list-tests | Tee-Object -FilePath $log
Write-Output ('LIST_EXIT=' + $LASTEXITCODE)
& dotnet test $proj -c Debug --no-build --filter $filter --nologo | Tee-Object -FilePath (Join-Path $outDir 'named-repl-run.log')
Write-Output ('REPL_EXIT=' + $LASTEXITCODE)
