#!/usr/bin/env pwsh
[CmdletBinding()]
param(
    [Parameter(Position = 0, ValueFromRemainingArguments = $true)]
    [string[]]$BuildArguments
)

$ErrorActionPreference = 'Stop'

if ($env:MCP_NUKE_POWERSHELL_BOOTSTRAPPED -ne '1') {
    $powerShellHost = (Get-Process -Id $PID).Path
    if ([string]::IsNullOrWhiteSpace($powerShellHost)) {
        $powerShellHost = if ($PSVersionTable.PSEdition -eq 'Core') { 'pwsh' } else { 'powershell.exe' }
    }

    $env:MCP_NUKE_POWERSHELL_BOOTSTRAPPED = '1'
    try {
        & $powerShellHost -NoLogo -NoProfile -NonInteractive -File $PSCommandPath @BuildArguments
        exit $LASTEXITCODE
    }
    finally {
        Remove-Item Env:\MCP_NUKE_POWERSHELL_BOOTSTRAPPED -ErrorAction SilentlyContinue
    }
}

$buildProject = Join-Path $PSScriptRoot 'build' '_build.csproj'
& dotnet run --project $buildProject -- @BuildArguments
exit $LASTEXITCODE
