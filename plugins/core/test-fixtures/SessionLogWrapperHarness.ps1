#Requires -Version 7.0
<#
.SYNOPSIS
Runs the shipped dispatcher behind real public wrappers with only external boundaries stubbed.
#>
param([string]$Method, [string]$ParamsYaml)
$requestedMethod = $Method
$requestedParams = $ParamsYaml
. (Join-Path $PSScriptRoot 'repl-implementation.ps1')

function Get-ReplSessionMeta { @{ SourceType = 'Codex'; SessionId = 'Codex-20261002T004900Z-review' } }
function Get-ReplSessionStateValue { param($Key) if ($Key -eq 'sessionId') { 'Codex-20261002T004900Z-review' } }
function Set-ReplSessionStateValue { $true }
function Assert-ReplCurrentTurnFresh { $true }
function Invoke-ReplRaw {
    if ($env:MCP_REVIEW_DISPOSITION -eq 'queued') {
        return @{ Success = $false; Output = ''; Error = 'HTTP 503 backend_unavailable' }
    }
    $output = if ($env:MCP_REVIEW_DISPOSITION -eq 'rejected') { '' } else {
        @{ type = 'result'; payload = @{ result = @{ sessionId = 'Codex-20261002T004900Z-review'; retitled = $true } } } | ConvertTo-Yaml
    }
    return @{ Success = $true; Output = $output; Error = '' }
}
if ($env:MCP_REVIEW_DISPOSITION -eq 'lost') {
    function Write-ReplFailsafe { '' }
}
Invoke-ReplMethod -Method $requestedMethod -ParamsYaml $requestedParams
if (-not $script:LastInvokeReplMethodSuccess) { exit 1 }
exit 0
