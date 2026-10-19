<#
.SYNOPSIS
    Hands repl-invoke.ps1 its params through a file instead of the child command line.
.DESCRIPTION
    A Windows child command line is limited to 32,767 characters. Passing the params YAML as a -ParamsYaml
    argument made large calls (for example a 194 KB workflow.requirements.updateBatch) fail with
    "The filename or extension is too long" (triage-report-821fe2da58284b4ea21604cb2da2fccb). Every launcher
    that starts repl-invoke.ps1 in a new process writes the params to a private temporary file with
    New-McpReplParamsTransfer, passes the returned -ParamsPath arguments, and removes the file with
    Remove-McpReplParamsTransfer in a finally block. In-process calls (& repl-invoke.ps1, dot-sourced
    functions) keep passing -ParamsYaml; no command line is involved there.

    The file is UTF-8 without BOM and carries the text byte for byte (line endings and non-ASCII preserved).
    Empty params produce no file and no arguments.
#>

function New-McpReplParamsTransfer {
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions', '', Justification = 'Writes only a private temporary params file that the caller removes.')]
    [CmdletBinding()]
    param([AllowEmptyString()][AllowNull()][string]$ParamsText)

    if ([string]::IsNullOrEmpty($ParamsText)) {
        return [pscustomobject]@{ Arguments = @(); Path = $null }
    }
    $path = Join-Path ([System.IO.Path]::GetTempPath()) ('mcp-repl-params-{0}.yaml' -f [guid]::NewGuid().ToString('N'))
    [System.IO.File]::WriteAllText($path, $ParamsText, [System.Text.UTF8Encoding]::new($false))
    return [pscustomobject]@{ Arguments = @('-ParamsPath', $path); Path = $path }
}

function Remove-McpReplParamsTransfer {
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions', '', Justification = 'Removes only the temporary params file created by New-McpReplParamsTransfer.')]
    [CmdletBinding()]
    param($Transfer)

    if ($null -eq $Transfer -or [string]::IsNullOrEmpty([string]$Transfer.Path)) { return }
    if (Test-Path -LiteralPath $Transfer.Path -PathType Leaf) {
        Remove-Item -LiteralPath $Transfer.Path -Force -ErrorAction SilentlyContinue
    }
}

function Read-McpReplParamsFile {
    <# Reads a -ParamsPath file as UTF-8, tolerating a BOM, and fails clearly when it is missing. #>
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "-ParamsPath file not found: $Path"
    }
    return [System.IO.File]::ReadAllText($Path, [System.Text.UTF8Encoding]::new($false))
}
