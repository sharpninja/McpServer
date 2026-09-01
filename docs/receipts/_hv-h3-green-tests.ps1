#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

Set-Location -LiteralPath 'F:\GitHub\McpServer'
$outDir = 'F:\GitHub\McpServer\docs\receipts'

function Invoke-NamedTest {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][string]$Project,
        [Parameter(Mandatory)][string]$Filter
    )
    $out = Join-Path $outDir ('_hv-h3-green-' + $Name + '.txt')
    $start = [datetime]::UtcNow
    @(
        'NAME=' + $Name
        'PROJECT=' + $Project
        'FILTER=' + $Filter
        'START=' + $start.ToString('o')
    ) | Set-Content -LiteralPath $out -Encoding utf8
    & dotnet test $Project -c Debug --filter $Filter --nologo *>&1 |
        ForEach-Object { $_.ToString() } |
        Tee-Object -FilePath $out -Append
    $code = $LASTEXITCODE
    $end = [datetime]::UtcNow
    @(
        'EXIT=' + $code
        'END=' + $end.ToString('o')
    ) | Add-Content -LiteralPath $out -Encoding utf8
    Write-Output ('RAN ' + $Name + ' EXIT=' + $code)
}

function Invoke-ListTests {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][string]$Project,
        [Parameter(Mandatory)][string]$Filter
    )
    $out = Join-Path $outDir ('_hv-h3-green-list-' + $Name + '.txt')
    $start = [datetime]::UtcNow
    @(
        'NAME=' + $Name
        'PROJECT=' + $Project
        'FILTER=' + $Filter
        'START=' + $start.ToString('o')
    ) | Set-Content -LiteralPath $out -Encoding utf8
    & dotnet test $Project -c Debug --filter $Filter --list-tests --nologo *>&1 |
        ForEach-Object { $_.ToString() } |
        Tee-Object -FilePath $out -Append
    $code = $LASTEXITCODE
    @(
        'EXIT=' + $code
        'END=' + [datetime]::UtcNow.ToString('o')
    ) | Add-Content -LiteralPath $out -Encoding utf8
    Write-Output ('LIST ' + $Name + ' EXIT=' + $code)
}

Invoke-ListTests -Name 'support' -Project 'tests/McpServer.Support.Mcp.Tests' -Filter 'ProductsControllerTests|Products|ProductEntityTests|ProductMigrationApplyTests'
Invoke-NamedTest -Name 'support' -Project 'tests/McpServer.Support.Mcp.Tests' -Filter 'ProductsControllerTests|Products|ProductEntityTests|ProductMigrationApplyTests'

Invoke-ListTests -Name 'client' -Project 'tests/McpServer.Client.Tests' -Filter 'FullyQualifiedName~ProductClientTests'
Invoke-NamedTest -Name 'client' -Project 'tests/McpServer.Client.Tests' -Filter 'FullyQualifiedName~ProductClientTests'

Invoke-ListTests -Name 'repl' -Project 'tests/McpServer.Repl.Core.Tests' -Filter 'FullyQualifiedName~GenericClientPassthroughValidClientNamesTests.InvokeAsync_UnknownClient_ErrorTextStillListsResolvableClients'
Invoke-NamedTest -Name 'repl' -Project 'tests/McpServer.Repl.Core.Tests' -Filter 'FullyQualifiedName~GenericClientPassthroughValidClientNamesTests.InvokeAsync_UnknownClient_ErrorTextStillListsResolvableClients'

Invoke-ListTests -Name 'reqclient' -Project 'tests/McpServer.Client.Tests' -Filter 'FullyQualifiedName~RequirementsClient'
Invoke-NamedTest -Name 'reqclient' -Project 'tests/McpServer.Client.Tests' -Filter 'FullyQualifiedName~RequirementsClient'

Write-Output 'H3_GREEN_TESTS_DONE'
