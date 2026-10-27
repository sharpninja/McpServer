# SPDX-License-Identifier: Apache-2.0
<#
.SYNOPSIS
Independent executable A8 alias-consumer model; no production imports or case names.
#>
function Invoke-MockAuditAliasConsumer {
    [CmdletBinding()]
    param([string]$StoreDirectory,[string]$OutboxRoot,[hashtable]$Context,
        [string]$HostSessionKey,[hashtable]$Dependencies,[scriptblock]$StepObserver,
        [string]$Mutation='')
    $applied=0;$acknowledged=0
    try{
        $list=& $Dependencies.Store $StoreDirectory $Context 'ListAliases' @{hostSessionKey=$HostSessionKey;consumerId='qb-audit-outbox-v1'}
        if(-not $list.ok -or $list.code -cne 'listed' -or $list.aliases -isnot [Collections.IList]){throw 'alias_list_invalid'}
        foreach($alias in $list.aliases){
            if($alias.workspacePath -cne $Context.workspacePath -or $alias.agentIdentity -cne $Context.agentIdentity -or $alias.hostSessionKey -cne $HostSessionKey -or [string]::IsNullOrWhiteSpace($alias.mcpSessionId)){throw 'alias_context_invalid'}
            $ack=@{hostSessionKey=$HostSessionKey;mcpSessionId=$alias.mcpSessionId;pendingId=$alias.pendingId;requestId=$alias.requestId;consumerId='qb-audit-outbox-v1'}
            if($Mutation -ceq 'ack-first'){$null=& $Dependencies.Store $StoreDirectory $Context 'Acknowledge' $ack}
            $outboxContext=@{workspacePath=$Context.workspacePath;agentIdentity=$Context.agentIdentity;mcpSessionId=$alias.mcpSessionId;hostSessionKey=$HostSessionKey}
            if($Mutation -cne 'omit-apply'){
                $outboxAlias=@{agent=$alias.agentIdentity;acks=@($alias.acks)}
                foreach($field in @('workspacePath','mcpSessionId','hostSessionKey','ingressNonce','pendingId','requestId','aliasedAtUtc')){$outboxAlias[$field]=$alias[$field]}
                $result=& $Dependencies.Outbox $OutboxRoot $outboxContext 'ApplyAlias' $outboxAlias
                if($result.code -cnotin @('applied','unchanged')){throw 'alias_application_failed'}
                $applied++
            }
            & $StepObserver 'applied' $alias
            if($Mutation -cne 'omit-ack'){
                $result=& $Dependencies.Store $StoreDirectory $Context 'Acknowledge' $ack
                if(-not $result.ok -or $result.code -cnotin @('acknowledged','unchanged')){throw 'alias_acknowledgement_failed'}
                $acknowledged++
            }
        }
        return @{ok=$true;code='reconciled';applied=$applied;acknowledged=$acknowledged;detailCode=''}
    }catch{return @{ok=$false;code='alias_reconciliation_failed';applied=$applied;acknowledged=$acknowledged;detailCode=$_.Exception.Message}}
}
