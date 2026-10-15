#!/usr/bin/env pwsh
[CmdletBinding()]
param(
    [Parameter(Position = 0, ValueFromRemainingArguments = $true)]
    [string[]]$BuildArguments
)

$ErrorActionPreference = 'Stop'

function Resolve-NukePowerShellHost {
    # TR-MCP-NUKE-001 AC-001: the PowerShell executable of the running installation ($PSHOME), never the current process's executable,
    # which under WinRM remoting is wsmprovhost.exe and would exit 0 without building; a missing executable stops the build.
    param(
        [string]$PowerShellHome = $PSHOME,
        [string]$Edition = $PSVersionTable.PSEdition,
        [bool]$OnWindows = ([System.Environment]::OSVersion.Platform -eq [System.PlatformID]::Win32NT)
    )

    $name = if ($Edition -ne 'Core') { 'powershell.exe' } elseif ($OnWindows) { 'pwsh.exe' } else { 'pwsh' }
    $candidate = Join-Path $PowerShellHome $name
    if (-not (Test-Path -LiteralPath $candidate -PathType Leaf)) {
        throw "build.ps1: no PowerShell executable at '$candidate'; run build.ps1 from an installed pwsh."
    }
    return $candidate
}

if ($env:MCP_NUKE_POWERSHELL_BOOTSTRAPPED -ne '1') {
    $powerShellHost = Resolve-NukePowerShellHost

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
