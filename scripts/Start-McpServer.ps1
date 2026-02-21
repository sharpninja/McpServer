<#
.SYNOPSIS
    Builds and starts the MCP Support server (McpServer.Support.Mcp) for Cursor/Copilot context.
.DESCRIPTION
    Builds McpServer.Support.Mcp with the specified configuration (default Staging), then runs the server.
    The MCP server is excluded from solution build configs and must be built/run via this script or
    directly: dotnet build src\McpServer.Support.Mcp\McpServer.Support.Mcp.csproj -c <Config>
    dotnet run --project src\McpServer.Support.Mcp\McpServer.Support.Mcp.csproj -c <Config>
    Listens on http://localhost:7147 by default (Development). See docs/api/mcp-client-config.md.
.PARAMETER Configuration
    Build configuration: Debug, Release, or Staging (default).
.PARAMETER NoBuild
    Skip build; run only (fails if not already built).
.PARAMETER Instance
    Optional MCP instance name from appsettings under Mcp:Instances:{name}.
    Also supported via environment variable MCP_INSTANCE.
.PARAMETER Docker
    Run the MCP server as a Docker container instead of locally via dotnet run.
    Uses docker-compose.mcp.yml from the repo root.
.PARAMETER Stop
    Stop the running Docker container (requires -Docker).
.EXAMPLE
    .\Start-McpServer.ps1
    .\Start-McpServer.ps1 -Configuration Debug
    .\Start-McpServer.ps1 -NoBuild
    .\Start-McpServer.ps1 -Instance default
    .\Start-McpServer.ps1 -Docker
    .\Start-McpServer.ps1 -Docker -Stop
#>
[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release", "Staging")]
    [string]$Configuration = "Staging",
    [switch]$NoBuild,
    [string]$Instance,
    [switch]$Docker,
    [switch]$Stop
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path $PSScriptRoot -Parent

# Docker mode
if ($Docker) {
    $composeFile = Join-Path $repoRoot "docker-compose.mcp.yml"
    if (-not (Test-Path $composeFile)) {
        Write-Error "docker-compose.mcp.yml not found at: $composeFile"
        exit 1
    }
    if ($Stop) {
        Write-Host "Stopping MCP server Docker container..."
        docker compose -f $composeFile down mcp-server
        exit $LASTEXITCODE
    }
    Write-Host "Starting MCP server via Docker (workspace: $repoRoot)..."
    $env:MCP_WORKSPACE = $repoRoot
    docker compose -f $composeFile up --build -d
    if ($LASTEXITCODE -eq 0) {
        Write-Host "MCP server container started. Health: http://localhost:7147/health"
    }
    exit $LASTEXITCODE
}

$projectPath = Join-Path $repoRoot "src\McpServer.Support.Mcp\McpServer.Support.Mcp.csproj"

if (-not (Test-Path $projectPath)) {
    Write-Error "Project not found: $projectPath"
    exit 1
}

if (-not $NoBuild) {
    Write-Host "Building McpServer.Support.Mcp (-c $Configuration) ..."
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
