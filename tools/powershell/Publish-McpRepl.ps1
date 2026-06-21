<#
.SYNOPSIS
    Publishes the McpRepl module to the PowerShell Gallery.

.DESCRIPTION
    This script is provided for manual / emergency publishes.
    Normal publishing happens automatically via the "publish_shared_modules" job
    in the main azure-pipelines.yml (only on the main branch).

    The canonical source for the module is:
    tools/powershell/McpRepl (this directory)
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)]
    [string]$ApiKey,
    [switch]$WhatIf
)

$modulePath = Join-Path $PSScriptRoot 'McpRepl'

if ($WhatIf) {
    Publish-Module -Path $modulePath -NuGetApiKey $ApiKey -WhatIf -Verbose
} else {
    if ($PSCmdlet.ShouldProcess('McpRepl', 'Publish to PSGallery')) {
        Publish-Module -Path $modulePath -NuGetApiKey $ApiKey -Verbose
    }
}
