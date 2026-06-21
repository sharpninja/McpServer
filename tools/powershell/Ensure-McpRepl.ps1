<#
.SYNOPSIS
    Canonical bootstrap helper for the McpRepl module.

.DESCRIPTION
    Used by all PowerShell-based McpServer agent plugins (Grok, Claude, Codex, Copilot, etc.)
    to ensure the shared McpRepl module is available.

    Canonical location: tools/powershell/Ensure-McpRepl.ps1 in the main McpServer repository.
#>
<#
.SYNOPSIS
    Ensures the McpRepl module is installed and imported.
    Safe to call from any plugin shim.
#>
[CmdletBinding()]
param(
    [version]$MinimumVersion = '1.0.0'
)

if (-not (Get-Module McpRepl -ListAvailable | Where-Object Version -ge $MinimumVersion)) {
    Write-Host "Installing McpRepl module from PowerShell Gallery..." -ForegroundColor Cyan
    Install-Module McpRepl -MinimumVersion $MinimumVersion -Scope CurrentUser -Force -AllowClobber
}

Import-Module McpRepl -MinimumVersion $MinimumVersion -ErrorAction Stop
Write-Verbose "McpRepl module ready."

