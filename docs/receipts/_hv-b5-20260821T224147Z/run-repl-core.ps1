$ErrorActionPreference = 'Continue'
$out = 'F:\GitHub\McpServer\docs\receipts\_hv-b5-20260821T224147Z'
Set-Location 'F:\GitHub\McpServer'
dotnet test tests/McpServer.Repl.Core.Tests -c Debug --logger "trx;LogFileName=hv-b5-repl-core.trx" --results-directory $out | Tee-Object -FilePath (Join-Path $out 'dotnet-repl-core.txt')
Write-Output ('REPL_CORE_EXIT=' + $LASTEXITCODE)
