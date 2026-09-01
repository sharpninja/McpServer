#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'
Set-Location -LiteralPath 'F:\GitHub\McpServer'
$start = [datetime]::UtcNow
Write-Output ('RUN2_START=' + $start.ToString('o'))
& dotnet test 'tests/McpServer.Support.Mcp.Tests' -c Debug --filter 'FullyQualifiedName~ProductRequirementContextTests' --nologo --no-restore
Write-Output ('RUN2_EXIT=' + $LASTEXITCODE)
Write-Output ('RUN2_END=' + [datetime]::UtcNow.ToString('o'))
