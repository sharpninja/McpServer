# SPDX-License-Identifier: Apache-2.0
<#
.SYNOPSIS
Reconcile A8 durable aliases into the governed audit outbox before acknowledging.
.DESCRIPTION
FR-QB-003/005; TR-QB-AUDIT-001; AC/TEST-QB-008/009/069.
Uses the canonical turn store and outbox; it never mints, delivers or completes a
turn. The caller supplies trusted captured context and private storage roots.
Dependencies and StepObserver are internal test seams, never model-controlled
settings, routing hints or transport alternatives.
#>
function Invoke-McpGovernedAuditAliasConsumer {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$StoreDirectory,
        [Parameter(Mandatory)][string]$OutboxRoot,
        [Parameter(Mandatory)][Collections.IDictionary]$Context,
        [Parameter(Mandatory)][string]$HostSessionKey,
        [Collections.IDictionary]$Dependencies=@{},
        [scriptblock]$StepObserver={param($Stage,$Binding) [void]$Stage;[void]$Binding}
    )
    $applied=0;$acknowledged=0
    try{
        . (Join-Path $PSScriptRoot 'governed-turn-store.ps1')
        . (Join-Path $PSScriptRoot 'governed-audit-outbox.ps1')
        $storeCall=if($Dependencies.Contains('Store')){$Dependencies.Store}else{
            {param($Directory,$Owner,$Operation,$InputData) Invoke-McpGovernedTurnStore -Directory $Directory -Context $Owner -Operation $Operation -InputObject $InputData}
        }
        $outboxCall=if($Dependencies.Contains('Outbox')){$Dependencies.Outbox}else{
            {param($Root,$Owner,$Operation,$InputData) Invoke-McpGovernedAuditOutbox -RootDirectory $Root -Context $Owner -Operation $Operation -InputObject $InputData}
        }
        if($storeCall -isnot [scriptblock] -or $outboxCall -isnot [scriptblock] -or [string]::IsNullOrWhiteSpace($HostSessionKey)){throw 'alias_dependency_invalid'}
        $consumerId='qb-audit-outbox-v1'
        $listing=& $storeCall $StoreDirectory $Context 'ListAliases' @{hostSessionKey=$HostSessionKey;consumerId=$consumerId}
        if($listing -isnot [Collections.IDictionary] -or $listing.ok -ne $true -or $listing.code -cne 'listed' -or $listing.aliases -isnot [Collections.IList]){throw 'alias_list_invalid'}
        foreach($binding in @($listing.aliases)){
            if($binding -isnot [Collections.IDictionary]){throw 'alias_context_invalid'}
            foreach($field in @('workspacePath','agentIdentity')){
                if($binding[$field] -isnot [string] -or -not [string]::Equals($binding[$field],$Context[$field],[StringComparison]::Ordinal)){throw 'alias_context_invalid'}
            }
            if($binding.hostSessionKey -isnot [string] -or $binding.hostSessionKey -cne $HostSessionKey -or $binding.mcpSessionId -isnot [string] -or [string]::IsNullOrWhiteSpace($binding.mcpSessionId)){throw 'alias_context_invalid'}
            $owner=@{workspacePath=$binding.workspacePath;agentIdentity=$binding.agentIdentity;mcpSessionId=$binding.mcpSessionId;hostSessionKey=$binding.hostSessionKey}
            $outboxAlias=@{workspacePath=$binding.workspacePath;agent=$binding.agentIdentity;mcpSessionId=$binding.mcpSessionId;hostSessionKey=$binding.hostSessionKey;ingressNonce=$binding.ingressNonce;pendingId=$binding.pendingId;requestId=$binding.requestId;aliasedAtUtc=$binding.aliasedAtUtc;acks=@($binding.acks)}
            $appliedResult=& $outboxCall $OutboxRoot $owner 'ApplyAlias' $outboxAlias
            if($appliedResult -isnot [Collections.IDictionary] -or $appliedResult.code -cnotin @('applied','unchanged')){throw 'alias_application_failed'}
            $applied++
            & $StepObserver 'applied' $binding
            $ack=@{hostSessionKey=$binding.hostSessionKey;mcpSessionId=$binding.mcpSessionId;pendingId=$binding.pendingId;requestId=$binding.requestId;consumerId=$consumerId}
            $ackResult=& $storeCall $StoreDirectory $Context 'Acknowledge' $ack
            if($ackResult -isnot [Collections.IDictionary] -or $ackResult.ok -ne $true -or $ackResult.code -cnotin @('acknowledged','unchanged')){throw 'alias_acknowledgement_failed'}
            $acknowledged++
        }
        return @{ok=$true;code='reconciled';applied=$applied;acknowledged=$acknowledged;detailCode=''}
    }catch{
        return @{ok=$false;code='alias_reconciliation_failed';applied=$applied;acknowledged=$acknowledged;detailCode=$_.Exception.Message}
    }
}
