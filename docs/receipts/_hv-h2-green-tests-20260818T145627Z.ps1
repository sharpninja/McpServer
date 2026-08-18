#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

Set-Location 'F:\GitHub\McpServer'

$focused = 'FullyQualifiedName~McpServer.Support.Mcp.Tests.Products|FullyQualifiedName~McpServer.Support.Mcp.Tests.Storage.ProductEntityTests|FullyQualifiedName~McpServer.Support.Mcp.Tests.Storage.ProductMigrationApplyTests'
$official = 'FullyQualifiedName~Product|FullyQualifiedName~RequirementScopeLayerServiceTests'

Write-Output '=== LIST_FOCUSED ==='
$startList = [datetime]::UtcNow
Write-Output ("LIST_START=" + $startList.ToString('o'))
dotnet test tests/McpServer.Support.Mcp.Tests -c Debug --no-restore --list-tests --filter $focused
Write-Output ("LIST_END=" + [datetime]::UtcNow.ToString('o'))
Write-Output ("LIST_EXIT=" + $LASTEXITCODE)

Write-Output '=== RUN_FOCUSED ==='
$startFocused = [datetime]::UtcNow
Write-Output ("FOCUSED_START=" + $startFocused.ToString('o'))
dotnet test tests/McpServer.Support.Mcp.Tests -c Debug --filter $focused
$focusedExit = $LASTEXITCODE
Write-Output ("FOCUSED_END=" + [datetime]::UtcNow.ToString('o'))
Write-Output ("FOCUSED_EXIT=" + $focusedExit)

Write-Output '=== RUN_OFFICIAL_H2_GREEN ==='
$startOfficial = [datetime]::UtcNow
Write-Output ("OFFICIAL_START=" + $startOfficial.ToString('o'))
dotnet test tests/McpServer.Support.Mcp.Tests -c Debug --filter $official
$officialExit = $LASTEXITCODE
Write-Output ("OFFICIAL_END=" + [datetime]::UtcNow.ToString('o'))
Write-Output ("OFFICIAL_EXIT=" + $officialExit)
