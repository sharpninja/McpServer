#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$paths = @(
    'F:\GitHub\McpServer\src\McpServer.Support.Mcp\McpStdio\FwhMcpTools.AgentHelp.cs',
    'F:\GitHub\McpServer\src\McpServer.Support.Mcp\McpStdio\FwhMcpTools.SessionLog.cs',
    'F:\GitHub\McpServer\src\McpServer.Services\Options\AgentHelpOptions.cs',
    'F:\GitHub\McpServer\src\McpServer.Services\Services\GrokCliAgentExecutionStrategy.cs',
    'F:\GitHub\McpServer\appsettings.yaml'
)
foreach ($p in $paths) {
    $i = Get-Item -LiteralPath $p
    Write-Output ($i.FullName + ' lw=' + $i.LastWriteTimeUtc.ToString('o'))
}
Set-Location 'F:\GitHub\McpServer'
Write-Output '--- porcelain targeted ---'
git status --porcelain -- src/McpServer.Services/Options/AgentHelpOptions.cs src/McpServer.Services/Services/GrokCliAgentExecutionStrategy.cs appsettings.yaml src/McpServer.Support.Mcp/appsettings.yaml
Write-Output '--- utc now ---'
Write-Output ([datetime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ'))
