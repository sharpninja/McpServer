#requires -Version 7.4
<#
.SYNOPSIS
    Shared trust-handshake helper for the persona launchers.

.DESCRIPTION
    Wraps the repository marker helper at plugins/core/lib-ps/marker-resolver.ps1 instead of
    parsing the marker file directly. That helper owns the canonical marker-v1 payload
    construction and HMAC-SHA256 comparison, so any hand-rolled reader will drift from the
    server the moment the payload shape changes.

    Invoke-FullBootstrap performs all three handshake steps from the process doc: locate the
    marker by walking up from the start directory, verify its signature, then issue a nonce
    challenge against /health and confirm the nonce is echoed back.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Import-McpMarkerResolver {
    <#
    .SYNOPSIS
        Dot-sources the repository marker resolver.
    #>
    [CmdletBinding()]
    param(
        [Parameter()][string]$Workspace = (Get-Location).Path
    )

    $resolver = Join-Path $Workspace 'plugins/core/lib-ps/marker-resolver.ps1'
    if (-not (Test-Path -LiteralPath $resolver)) {
        throw "Marker resolver not found at '$resolver'. Run from the workspace root, or pass -Workspace."
    }
    . $resolver
}

function Assert-McpTrust {
    <#
    .SYNOPSIS
        Completes the trust handshake and exports the current API key.
    .DESCRIPTION
        On any failure this throws after the caller has logged MCP_UNTRUSTED intent. A failed
        handshake is terminal: do not launch a persona, and do not probe around it.
    .OUTPUTS
        Hashtable with MarkerFile, BaseUrl, and ApiKey.
    #>
    [CmdletBinding()]
    param(
        [Parameter()][string]$Workspace = (Get-Location).Path
    )

    Import-McpMarkerResolver -Workspace $Workspace

    # Find marker, verify marker-v1 HMAC signature, nonce-challenge /health.
    if (-not (Invoke-FullBootstrap -StartDir $Workspace)) {
        throw 'MCP_UNTRUSTED: trust handshake failed. Not launching a persona against an untrusted server.'
    }

    $markerFile = Find-MarkerFile -StartDir $Workspace
    $apiKey = Get-MarkerField -MarkerFile $markerFile -FieldName 'apiKey'
    $baseUrl = Get-MarkerField -MarkerFile $markerFile -FieldName 'baseUrl'

    if (-not $apiKey) { throw "MCP_UNTRUSTED: no apiKey field in '$markerFile'." }

    # The key rotates on every server start, so always re-read it rather than caching a value.
    $env:MCP_API_KEY = $apiKey

    return @{
        MarkerFile = $markerFile
        BaseUrl    = $baseUrl
        ApiKey     = $apiKey
    }
}
