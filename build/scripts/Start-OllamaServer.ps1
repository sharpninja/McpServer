#Requires -Version 7.0
<#
.SYNOPSIS
Runs the lane's long-lived Ollama server with its output owned by a detached logging worker.
.DESCRIPTION
The build isolates all of this worker's standard handles. Server output and startup failures go only to LogPath,
so the server can outlive the build without retaining its caller's output pipes. TR-MCP-NUKE-019.
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)][string]$OllamaPath,
    [Parameter(Mandatory)][string]$LogPath
)
$ErrorActionPreference='Stop'
if($PSCmdlet.ShouldProcess($OllamaPath,'Run Ollama server with detached file logging')) {
    $null=New-Item -ItemType Directory -Path (Split-Path $LogPath -Parent) -Force
    try {
        & $OllamaPath serve *>> $LogPath
        exit $LASTEXITCODE
    }
    catch {
        $_ | Out-String | Add-Content -LiteralPath $LogPath
        exit 1
    }
}
