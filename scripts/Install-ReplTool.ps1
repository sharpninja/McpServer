#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Installs or updates the mcpserver-repl global tool.

.DESCRIPTION
    This script installs or updates the SharpNinja.McpServer.Repl package as a dotnet global tool
    from the local-packages feed.

.PARAMETER Update
    Update an existing installation instead of installing fresh.

.PARAMETER Uninstall
    Uninstall the tool.

.EXAMPLE
    .\Install-ReplTool.ps1
    Install the REPL tool.

.EXAMPLE
    .\Install-ReplTool.ps1 -Update
    Update the REPL tool.

.EXAMPLE
    .\Install-ReplTool.ps1 -Uninstall
    Uninstall the REPL tool.
#>

[CmdletBinding()]
param(
    [switch]$Update,
    [switch]$Uninstall
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$solutionRoot = Join-Path $PSScriptRoot '..'
$packageSource = Join-Path $solutionRoot 'local-packages'

Push-Location $solutionRoot

try {
    if ($Uninstall) {
        Write-Host "Uninstalling SharpNinja.McpServer.Repl..." -ForegroundColor Yellow
        & dotnet tool uninstall --global SharpNinja.McpServer.Repl
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "Uninstall returned exit code $LASTEXITCODE (may not have been installed)"
        }
        else {
            Write-Host "Tool uninstalled successfully." -ForegroundColor Green
        }
    }
    elseif ($Update) {
        Write-Host "Updating SharpNinja.McpServer.Repl..." -ForegroundColor Yellow
        & dotnet tool update --global SharpNinja.McpServer.Repl --add-source $packageSource
        if ($LASTEXITCODE -ne 0) {
            throw "Update failed with exit code $LASTEXITCODE"
        }
        Write-Host "Tool updated successfully." -ForegroundColor Green
    }
    else {
        Write-Host "Installing SharpNinja.McpServer.Repl..." -ForegroundColor Yellow
        & dotnet tool install --global SharpNinja.McpServer.Repl --add-source $packageSource
        if ($LASTEXITCODE -ne 0) {
            throw "Install failed with exit code $LASTEXITCODE"
        }
        Write-Host "Tool installed successfully." -ForegroundColor Green
    }

    Write-Host ""
    Write-Host "Verifying installation..." -ForegroundColor Yellow
    & mcpserver-repl --version
    if ($LASTEXITCODE -ne 0) {
        throw "Verification failed with exit code $LASTEXITCODE"
    }

    Write-Host ""
    Write-Host "==== Available commands ====" -ForegroundColor Cyan
    Write-Host "  mcpserver-repl --version              Show version" -ForegroundColor White
    Write-Host "  mcpserver-repl --interactive          Run in interactive mode" -ForegroundColor White
    Write-Host "  mcpserver-repl --agent-stdio          Run in agent STDIO mode" -ForegroundColor White
    Write-Host ""
}
catch {
    Write-Error "Failed: $_"
    exit 1
}
finally {
    Pop-Location
}
