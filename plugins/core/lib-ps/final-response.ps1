#Requires -Version 7.0
[CmdletBinding()]
param(
    [string]$Response
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
. (Join-Path $scriptDir 'plugin-env.ps1')

if (-not $Response -and [Console]::IsInputRedirected) {
    $Response = [Console]::In.ReadToEnd()
}

if (-not $Response) {
    $Response = 'Turn completed.'
}

$indented = (($Response -replace "`r`n", "`n" -replace "`r", "`n") -split "`n" | ForEach-Object { "    $_" }) -join "`n"
$paramsYaml = "response: |`n$indented"

& (Join-Path $scriptDir 'repl-invoke.ps1') -Method 'workflow.sessionlog.completeTurn' -ParamsYaml $paramsYaml
