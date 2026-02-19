<#
.SYNOPSIS
    Builds and starts the MCP Support server (FWH.Support.Mcp) for Cursor/Copilot context.
.DESCRIPTION
    Builds FWH.Support.Mcp with the specified configuration (default Staging), then runs the server.
    The MCP server is excluded from solution build configs and must be built/run via this script or
    directly: dotnet build src\FWH.Support.Mcp\FWH.Support.Mcp.csproj -c <Config>
    dotnet run --project src\FWH.Support.Mcp\FWH.Support.Mcp.csproj -c <Config>
    Listens on http://localhost:7147 by default (Development). See docs/api/mcp-client-config.md.
.PARAMETER Configuration
    Build configuration: Debug, Release, or Staging (default).
.PARAMETER NoBuild
    Skip build; run only (fails if not already built).
.PARAMETER Instance
    Optional MCP instance name from appsettings under Mcp:Instances:{name}.
    Also supported via environment variable MCP_INSTANCE.
.EXAMPLE
    .\Start-McpServer.ps1
    .\Start-McpServer.ps1 -Configuration Debug
    .\Start-McpServer.ps1 -NoBuild
    .\Start-McpServer.ps1 -Instance default
#>
[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release", "Staging")]
    [string]$Configuration = "Staging",
    [switch]$NoBuild,
    [string]$Instance
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path $PSScriptRoot -Parent
$projectPath = Join-Path $repoRoot "src\FWH.Support.Mcp\FWH.Support.Mcp.csproj"

if (-not (Test-Path $projectPath)) {
    Write-Error "Project not found: $projectPath"
    exit 1
}

if (-not $NoBuild) {
    Write-Host "Building FWH.Support.Mcp (-c $Configuration) ..."
    & dotnet build $projectPath -c $Configuration
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Build failed."
        exit $LASTEXITCODE
    }
}

Write-Host "Starting MCP server (port determined by PORT env var or Mcp:Port / selected instance). Press Ctrl+C to stop."
$runArgs = @("run", "--project", $projectPath, "-c", $Configuration, "--no-build")
if (-not [string]::IsNullOrWhiteSpace($Instance)) {
    $runArgs += "--"
    $runArgs += "--instance"
    $runArgs += $Instance
    Write-Host "Using MCP instance: $Instance"
}
& dotnet @runArgs
