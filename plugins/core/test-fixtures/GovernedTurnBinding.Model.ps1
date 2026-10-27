# SPDX-License-Identifier: Apache-2.0
<#
.SYNOPSIS
Independent test-only selection model for the frozen A8 snapshot seam.
.DESCRIPTION
Consumes actual captured snapshots and selection arguments, not case names or
expected results. Mutations deliberately violate individual dependency effects
so the shared contract assertions must reject them.
#>
function Resolve-MockGovernedTurnBinding {
    [CmdletBinding()]
    param([Collections.IDictionary]$Context,[object]$HostSessionKey,[object]$RequestId,[string]$Mutation='')
    $fields=@('workspacePath','agentIdentity','mcpSessionId','hostSessionKey','keySource','ingressNonce','requestId','boundAtUtc')
    $result=@{ok=$false;code='host_session_unknown';retryable=$false;binding=$null}
    if($HostSessionKey -isnot [string] -or [string]::IsNullOrWhiteSpace($HostSessionKey)){return $result}
    $hosts=@($Context.hosts|Where-Object {$_.hostSessionKey -ceq $HostSessionKey})
    if($hosts.Count -eq 0){return $result}
    if($hosts.Count -gt 1){$result.code='host_session_ambiguous';return $result}
    $result.code='request_unknown'
    $turns=@($hosts[0].turns)
    foreach($turn in $turns){
        foreach($field in $fields){
            if(-not $turn.Contains($field) -or $turn[$field] -isnot [string] -or [string]::IsNullOrWhiteSpace($turn[$field])){return $result}
        }
        if($turn.delivered -isnot [bool] -or $turn.status -cnotin @('open','terminal')){return $result}
        if($Mutation -cne 'ignore-identity' -and ($turn.workspacePath -cne $Context.workspacePath -or $turn.agentIdentity -cne $Context.agentIdentity -or $turn.hostSessionKey -cne $HostSessionKey)){return $result}
    }
    if($PSBoundParameters.ContainsKey('RequestId')){
        if($RequestId -isnot [string] -or [string]::IsNullOrWhiteSpace($RequestId)){return $result}
        $candidates=@($turns|Where-Object {if($Mutation -ceq 'case-fold-id'){$_.requestId -eq $RequestId}else{$_.requestId -ceq $RequestId}})
    }else{
        $candidates=@($turns|Where-Object {$_.status -ceq 'open' -and ($_.delivered -or $Mutation -ceq 'include-minted')})
    }
    if($candidates.Count -eq 0){return $result}
    if($candidates.Count -gt 1 -and $Mutation -cne 'first-implicit'){$result.code='request_ambiguous';return $result}
    if($candidates[0].status -ceq 'terminal'){$result.code='turn_immutable';return $result}
    $map=[Collections.Generic.Dictionary[string,string]]::new([StringComparer]::Ordinal)
    foreach($field in $fields){$map.Add($field,$candidates[0][$field])}
    $binding=if($Mutation -ceq 'mutable-binding'){$map}else{[Collections.ObjectModel.ReadOnlyDictionary[string,string]]::new($map)}
    if($Mutation -ceq 'mutate-input'){$Context.agentIdentity='mutated-agent'}
    return @{ok=$true;code='resolved';retryable=$false;binding=$binding}
}
