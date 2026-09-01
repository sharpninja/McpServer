<#
.SYNOPSIS
    Preserve classified MCP error envelopes in the plugin shim.
.DESCRIPTION
    TR-MCP-TRIAGEERR-001: do not collapse a server/REPL envelope that already
    has code/message/retryable into opaque internal_server_error.
#>

function ConvertTo-McpPluginClassifiedError {
    [CmdletBinding()]
    param(
        [string]$Output = '',
        [string]$ErrorText = ''
    )

    $code = $null
    $message = $null
    $retryable = $false

    if ($Output -match '(?m)^\s*code:\s*(\S+)') { $code = $Matches[1].Trim("'`"") }
    if ($Output -match '(?m)^\s*message:\s*(.+)$') { $message = $Matches[1].Trim().Trim("'`"") }
    if ($Output -match '(?m)^\s*retryable:\s*(true|false)') {
        $retryable = $Matches[1] -eq 'true'
    }

    if (-not $code -and $ErrorText -match '(?m)^\s*code:\s*(\S+)') { $code = $Matches[1].Trim("'`"") }
    if (-not $message -and $ErrorText) { $message = $ErrorText }

    if (-not $code) {
        return [pscustomobject]@{
            code      = 'internal_server_error'
            message   = $(if ($message) { $message } else { 'Plugin invocation failed.' })
            retryable = $false
            preserved = $false
        }
    }

    if ($code -eq 'internal_server_error' -and $Output -match '(?m)^\s*code:\s*(?!internal_server_error)(\S+)') {
        $code = $Matches[1].Trim("'`"")
    }

    return [pscustomobject]@{
        code      = $code
        message   = $(if ($message) { $message } else { $ErrorText })
        retryable = [bool]$retryable
        preserved = $true
    }
}
