#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Packs and publishes McpServer.Repl.Host as a dotnet global tool.

.DESCRIPTION
    This script builds the McpServer.Repl.Host project in Release configuration,
    packs it as a NuGet package, and publishes it to the local NuGet feed.

.PARAMETER Clean
    Clean the project before building.

.PARAMETER SkipBuild
    Skip the build step and only pack.

.EXAMPLE
    .\Pack-ReplTool.ps1
    Build, pack, and publish the REPL tool.

.EXAMPLE
    .\Pack-ReplTool.ps1 -Clean
    Clean, build, pack, and publish the REPL tool.
#>

[CmdletBinding()]
param(
    [switch]$Clean,
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$projectPath = Join-Path $PSScriptRoot '..' 'src' 'McpServer.Repl.Host' 'McpServer.Repl.Host.csproj'
$outputPath = Join-Path $PSScriptRoot '..' 'local-packages'
$solutionRoot = Join-Path $PSScriptRoot '..'

Push-Location $solutionRoot

try {
    Write-Host "==== Packing McpServer.Repl.Host as dotnet tool ====" -ForegroundColor Cyan

    if ($Clean) {
        Write-Host "Cleaning project..." -ForegroundColor Yellow
        & dotnet clean $projectPath --configuration Release
        if ($LASTEXITCODE -ne 0) {
            throw "Clean failed with exit code $LASTEXITCODE"
        }
    }

    if (-not $SkipBuild) {
        Write-Host "Building project in Release configuration..." -ForegroundColor Yellow
        & dotnet build $projectPath --configuration Release
        if ($LASTEXITCODE -ne 0) {
            throw "Build failed with exit code $LASTEXITCODE"
        }
    }

    Write-Host "Packing NuGet package..." -ForegroundColor Yellow
    & dotnet pack $projectPath --configuration Release --output $outputPath --no-build
    if ($LASTEXITCODE -ne 0) {
        throw "Pack failed with exit code $LASTEXITCODE"
    }

    Write-Host ""
    Write-Host "==== Package created successfully ====" -ForegroundColor Green
    Write-Host "Package location: $outputPath" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "To install as a global tool, run:" -ForegroundColor Yellow
    Write-Host "  dotnet tool install --global SharpNinja.McpServer.Repl --add-source ./local-packages" -ForegroundColor White
    Write-Host ""
    Write-Host "To update an existing installation, run:" -ForegroundColor Yellow
    Write-Host "  dotnet tool update --global SharpNinja.McpServer.Repl --add-source ./local-packages" -ForegroundColor White
    Write-Host ""
    Write-Host "To verify installation, run:" -ForegroundColor Yellow
    Write-Host "  mcpserver-repl --version" -ForegroundColor White
    Write-Host ""
}
catch {
    Write-Error "Failed to pack REPL tool: $_"
    exit 1
}
finally {
    Pop-Location
}
