# SPDX-License-Identifier: Apache-2.0
<#
.SYNOPSIS
Resolves ordinary governed audit writes against their captured host binding.
.DESCRIPTION
FR-QB-003/TR-QB-AUDIT-001 and FR-QB-005/TR-QB-RUNTIME-001 (A8).
This pure selector neither mints nor persists a turn. The lifecycle owner must
provide a trusted snapshot and serialize selection plus mutation together.
See ../contracts/governed-turn-binding.md for the consumer contract.
#>
function Resolve-McpGovernedTurnBinding {
    <#
    .SYNOPSIS
    Selects one captured request without mutable-current-session fallback.
    .PARAMETER Context
    Trusted workspace/agent snapshot with host-owned turn records.
    .PARAMETER HostSessionKey
    Captured native host key. Never taken from model-supplied tool arguments.
    .PARAMETER RequestId
    Optional explicit request. Omission alone enables unambiguous selection.
    .OUTPUTS
    Dictionary with ok, code, retryable and an independent read-only binding.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][System.Collections.IDictionary]$Context,
        [AllowNull()][AllowEmptyString()][object]$HostSessionKey,
        [AllowNull()][AllowEmptyString()][object]$RequestId
    )
    function Get-BindingFailure {
        param([string]$Code)
        return [ordered]@{ok=$false;code=$Code;retryable=$false;binding=$null}
    }
    if($HostSessionKey -isnot [string] -or [string]::IsNullOrWhiteSpace($HostSessionKey)){
        return (Get-BindingFailure host_session_unknown)
    }
    if(-not $Context.Contains('hosts') -or $null -eq $Context.hosts){
        return (Get-BindingFailure host_session_unknown)
    }
    $hosts=@($Context.hosts | Where-Object {
        $_ -is [System.Collections.IDictionary] -and $_.Contains('hostSessionKey') -and
        $_.hostSessionKey -is [string] -and [string]::Equals($_.hostSessionKey,$HostSessionKey,[StringComparison]::Ordinal)
    })
    if($hosts.Count -eq 0){return (Get-BindingFailure host_session_unknown)}
    if($hosts.Count -ne 1){return (Get-BindingFailure host_session_ambiguous)}
    if(-not $hosts[0].Contains('turns')){return (Get-BindingFailure request_unknown)}

    $fields=@('workspacePath','agentIdentity','mcpSessionId','hostSessionKey','keySource','ingressNonce','requestId','boundAtUtc')
    $turns=@($hosts[0].turns)
    foreach($turn in $turns){
        if($turn -isnot [System.Collections.IDictionary]){return (Get-BindingFailure request_unknown)}
        foreach($field in $fields){
            if(-not $turn.Contains($field) -or $turn[$field] -isnot [string] -or [string]::IsNullOrWhiteSpace($turn[$field])){
                return (Get-BindingFailure request_unknown)
            }
        }
        foreach($field in @('workspacePath','agentIdentity')){
            if(-not $Context.Contains($field) -or $Context[$field] -isnot [string] -or
                -not [string]::Equals($turn[$field],$Context[$field],[StringComparison]::Ordinal)){
                return (Get-BindingFailure request_unknown)
            }
        }
        if(-not [string]::Equals($turn.hostSessionKey,$HostSessionKey,[StringComparison]::Ordinal) -or
            -not $turn.Contains('delivered') -or $turn.delivered -isnot [bool] -or
            -not $turn.Contains('status') -or $turn.status -isnot [string] -or $turn.status -cnotin @('open','terminal')){
            return (Get-BindingFailure request_unknown)
        }
    }
    if($PSBoundParameters.ContainsKey('RequestId')){
        if($RequestId -isnot [string] -or [string]::IsNullOrWhiteSpace($RequestId)){
            return (Get-BindingFailure request_unknown)
        }
        $matchingTurns=@($turns | Where-Object {[string]::Equals($_.requestId,$RequestId,[StringComparison]::Ordinal)})
    }else{
        $matchingTurns=@($turns | Where-Object {$_.delivered -and $_.status -ceq 'open'})
    }
    if($matchingTurns.Count -eq 0){return (Get-BindingFailure request_unknown)}
    if($matchingTurns.Count -ne 1){return (Get-BindingFailure request_ambiguous)}
    if($matchingTurns[0].status -ceq 'terminal'){return (Get-BindingFailure turn_immutable)}
    $binding=[System.Collections.Generic.Dictionary[string,string]]::new([StringComparer]::Ordinal)
    foreach($field in $fields){$binding.Add($field,$matchingTurns[0][$field])}
    return [ordered]@{
        ok=$true;code='resolved';retryable=$false
        binding=[System.Collections.ObjectModel.ReadOnlyDictionary[string,string]]::new($binding)
    }
}
