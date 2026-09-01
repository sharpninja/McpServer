#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'
Set-Location -LiteralPath 'F:\GitHub\McpServer'
$log = 'F:\GitHub\McpServer\docs\receipts\_hv-testrun-20260817T020200Z.log'
if (Test-Path -LiteralPath $log) {
    Remove-Item -LiteralPath $log -Force
}

function Write-Log {
    param([string]$Message)
    $line = '[{0:yyyy-MM-ddTHH:mm:ss.fffZ}] {1}' -f (Get-Date).ToUniversalTime(), $Message
    Add-Content -LiteralPath $log -Value $line
    Write-Output $line
}

function Invoke-LoggedTest {
    param(
        [string]$Name,
        [string]$Project,
        [string]$Filter
    )
    Write-Log "BEGIN $Name"
    $args = @('test', $Project, '-c', 'Debug', '--nologo')
    if ($Filter) {
        $args += @('--filter', $Filter)
        Write-Log ("dotnet test {0} -c Debug --filter {1} --nologo" -f $Project, $Filter)
    }
    else {
        Write-Log ("dotnet test {0} -c Debug --nologo" -f $Project)
    }
    & dotnet @args 2>&1 | Tee-Object -FilePath $log -Append
    Write-Log ("END {0} EXIT={1}" -f $Name, $LASTEXITCODE)
}

Write-Log 'hostile independent rerun start'
Invoke-LoggedTest -Name 'handoff-migration-6' -Project 'tests/McpServer.Support.Mcp.IntegrationTests/McpServer.Support.Mcp.IntegrationTests.csproj' -Filter 'FullyQualifiedName~HandoffIngestionStorageMigrationTests|FullyQualifiedName~ProviderDatabaseIntegrationTests'
Invoke-LoggedTest -Name 'handoff-unit-66' -Project 'tests/McpServer.Support.Mcp.Tests/McpServer.Support.Mcp.Tests.csproj' -Filter 'FullyQualifiedName~Handoff'
Invoke-LoggedTest -Name 'client' -Project 'tests/McpServer.Client.Tests/McpServer.Client.Tests.csproj'
Invoke-LoggedTest -Name 'repl-core' -Project 'tests/McpServer.Repl.Core.Tests/McpServer.Repl.Core.Tests.csproj'
Invoke-LoggedTest -Name 'support-unit-no-integration' -Project 'tests/McpServer.Support.Mcp.Tests/McpServer.Support.Mcp.Tests.csproj' -Filter 'Category!=Integration'
Invoke-LoggedTest -Name 'support-integration' -Project 'tests/McpServer.Support.Mcp.IntegrationTests/McpServer.Support.Mcp.IntegrationTests.csproj'
Invoke-LoggedTest -Name 'repl-integration' -Project 'tests/McpServer.Repl.IntegrationTests/McpServer.Repl.IntegrationTests.csproj'
Write-Log 'hostile independent rerun complete'
exit 0
