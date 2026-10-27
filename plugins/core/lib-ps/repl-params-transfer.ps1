#Requires -Version 7.0
# SPDX-License-Identifier: Apache-2.0
<#
.SYNOPSIS
    Transfers exact invocation text through the existing REPL ParamsPath boundary.
.DESCRIPTION
    The caller owns this private UTF-8 file and removes it in finally. Empty text
    creates no file. This follows the installed plugin file-handoff pattern and
    avoids the Windows process command-line length limit without another transport.
#>
function New-McpReplParamsTransfer {
    [CmdletBinding(SupportsShouldProcess)]
    param([AllowEmptyString()][AllowNull()][string]$ParamsText)

    if ([string]::IsNullOrEmpty($ParamsText)) {
        return [pscustomobject]@{ Arguments = @(); Path = $null }
    }
    $path = Join-Path ([IO.Path]::GetTempPath()) ('mcp-repl-params-{0}.yaml' -f [guid]::NewGuid().ToString('N'))
    if (-not $PSCmdlet.ShouldProcess($path, 'Create one private exact UTF-8 params transfer file')) {
        return [pscustomobject]@{ Arguments = @(); Path = $null }
    }
    [IO.File]::WriteAllText($path, $ParamsText, [Text.UTF8Encoding]::new($false))
    return [pscustomobject]@{ Arguments = @('-ParamsPath', $path); Path = $path }
}

function Remove-McpReplParamsTransfer {
    <# Removes only the caller-owned private transfer after invocation, including failure. #>
    [CmdletBinding(SupportsShouldProcess)]
    param($Transfer)

    if ($null -eq $Transfer -or [string]::IsNullOrEmpty([string]$Transfer.Path)) { return }
    if ((Test-Path -LiteralPath $Transfer.Path -PathType Leaf) -and
        $PSCmdlet.ShouldProcess($Transfer.Path, 'Remove one private params transfer file')) {
        Remove-Item -LiteralPath $Transfer.Path -Force -ErrorAction Stop
    }
}

function Read-McpReplParamsFile {
    <# Reads exact UTF-8 text, tolerates a BOM, and rejects a missing dependency file. #>
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "-ParamsPath file not found: $Path"
    }
    return [IO.File]::ReadAllText($Path, [Text.UTF8Encoding]::new($false))
}
