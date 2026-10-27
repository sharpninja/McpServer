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

function Resolve-McpGovernedRecoveryBinding {
    <#
    .SYNOPSIS
    FR-QB-003/005 TR-QB-AUDIT-001: Select an explicit trusted original session/request pair.
    .DESCRIPTION
    Read-only recovery lookup in the existing context snapshot. The host supplies
    TrustedPair separately from tool content. No current-host key or latest-turn
    fallback is used, and a selected binding grants no turn mutation permission.
    .PARAMETER Context
    Existing workspace/agent snapshot, validated under the store's existing lock.
    .PARAMETER TrustedPair
    Captured original mcpSessionId and requestId. Either missing value refuses selection.
    .OUTPUTS
    Exact immutable captured binding, or request_unknown/request_ambiguous.
    #>
    [CmdletBinding()]
    param([Parameter(Mandatory)][Collections.IDictionary]$Context,
          [AllowNull()][Collections.IDictionary]$TrustedPair)
    $failure=[ordered]@{ok=$false;code='request_unknown';binding=$null}
    if(-not $TrustedPair){return $failure}
    foreach($key in @('mcpSessionId','requestId')){
        if(-not $TrustedPair.Contains($key) -or $TrustedPair[$key] -isnot [string] -or [string]::IsNullOrWhiteSpace($TrustedPair[$key])){return $failure}
    }
    $selectedAliases=@($Context.aliases|Where-Object {$_.mcpSessionId -ceq $TrustedPair.mcpSessionId -and $_.pendingId -ceq $TrustedPair.requestId})
    if($selectedAliases.Count -gt 1){return [ordered]@{ok=$false;code='request_ambiguous';binding=$null}}
    $request=if($selectedAliases.Count){$selectedAliases[0].requestId}else{$TrustedPair.requestId}
    $candidates=@($Context.hosts|ForEach-Object {$_.turns}|Where-Object {
        $_.mcpSessionId -ceq $TrustedPair.mcpSessionId -and $_.requestId -ceq $request -and
        $_.workspacePath -ceq $Context.workspacePath -and $_.agentIdentity -ceq $Context.agentIdentity
    })
    if(-not $candidates.Count){return $failure}
    if($candidates.Count -ne 1){return [ordered]@{ok=$false;code='request_ambiguous';binding=$null}}
    $selected=$candidates[0]
    if($selectedAliases.Count){
        $alias=$selectedAliases[0]
        if($alias.hostSessionKey -cne $selected.hostSessionKey -or $alias.ingressNonce -cne $selected.ingressNonce -or $alias.pendingId -cne ('pending-'+$selected.ingressNonce)){return $failure}
    }
    $binding=[Collections.Generic.Dictionary[string,string]]::new([StringComparer]::Ordinal)
    foreach($field in @('workspacePath','agentIdentity','mcpSessionId','hostSessionKey','keySource','ingressNonce','requestId','boundAtUtc')){
        if($selected[$field] -isnot [string] -or [string]::IsNullOrWhiteSpace($selected[$field])){return $failure}
        $binding.Add($field,$selected[$field])
    }
    return [ordered]@{ok=$true;code='resolved';binding=[Collections.ObjectModel.ReadOnlyDictionary[string,string]]::new($binding)}
}

