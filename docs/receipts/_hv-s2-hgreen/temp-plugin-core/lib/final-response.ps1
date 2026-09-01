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

if (-not (Get-Command ConvertTo-Yaml -ErrorAction SilentlyContinue)) {
    Import-Module powershell-yaml -ErrorAction Stop
}

$paramsYaml = ConvertTo-Yaml -Data ([ordered]@{ response = $Response }) -Options WithIndentedSequences

& (Join-Path $scriptDir 'repl-invoke.ps1') -Method 'workflow.sessionlog.completeTurn' -ParamsYaml $paramsYaml
