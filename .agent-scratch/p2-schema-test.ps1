$ErrorActionPreference = 'Stop'
$wt = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
Set-Location -LiteralPath $wt
$filter = 'FullyQualifiedName~SessionLogSchemaGuardTests'
& dotnet test (Join-Path $wt 'tests\McpServer.Support.Mcp.Tests\McpServer.Support.Mcp.Tests.csproj') -c Debug --filter $filter --nologo --no-restore
if ($LASTEXITCODE -ne 0) {
  & dotnet test (Join-Path $wt 'tests\McpServer.Support.Mcp.Tests\McpServer.Support.Mcp.Tests.csproj') -c Debug --filter $filter --nologo
}
Write-Output ("TEST_EXIT=" + $LASTEXITCODE)
exit $LASTEXITCODE
