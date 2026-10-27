#Requires -Version 7.5
# SPDX-License-Identifier: Apache-2.0
function Invoke-ModelSerializedRecovery {
    [CmdletBinding()]
    param([hashtable]$Scenario,[string]$Mutation='')
    $effects=[Collections.Generic.List[string]]::new();$effects.Add('Acquire')
    $bytes=$Scenario.serialized;$after=$bytes;$state=$null;$binding=$null;$code='request_unknown';$ok=$false;$retry=$false
    if(-not $Scenario.lockAvailable -and $Mutation -cne 'bypass-lock'){$code='store_unavailable';$retry=$true}
    else{
        try{
            $effects.Add('Read')
            if($null -eq $bytes){$state=@{schemaVersion=1;workspacePath=$Scenario.context.workspacePath;agentIdentity=$Scenario.context.agentIdentity;aliasConsumers=@($Scenario.context.aliasConsumers);revision=0;hosts=@();aliases=@()}}
            else{try{$state=ConvertFrom-Json $bytes -AsHashtable -DateKind String -NoEnumerate}catch{$code='store_corrupt';return @{result=@{ok=$false;code=$code;retryable=$false;binding=$null;aliases=@();state=$null};before=$bytes;after=$after;writes=0;effects=@($effects)+@('Release')}}}
            $foreign=$state.workspacePath -cne $Scenario.context.workspacePath -or $state.agentIdentity -cne $Scenario.context.agentIdentity -or (ConvertTo-Json -InputObject $state.aliasConsumers -Compress) -cne (ConvertTo-Json -InputObject $Scenario.context.aliasConsumers -Compress)
            if($foreign -and $Mutation -cne 'ignore-context'){$code='context_mismatch';$state=$null}
            else{
                $valid=$state.schemaVersion -eq 1 -and $state.revision -ge 0 -and $state.hosts -is [array] -and $state.aliases -is [array]
                $fields=@('workspacePath','agentIdentity','mcpSessionId','hostSessionKey','keySource','ingressNonce','requestId','boundAtUtc')
                foreach($hostEntry in $state.hosts){foreach($turn in $hostEntry.turns){
                    foreach($field in $fields){if($turn[$field] -isnot [string] -or [string]::IsNullOrWhiteSpace($turn[$field])){$valid=$false}}
                    if($turn.hostSessionKey -cne $hostEntry.hostSessionKey -or $turn.workspacePath -cne $state.workspacePath -or $turn.agentIdentity -cne $state.agentIdentity -or $turn.status -cnotin @('open','terminal') -or $turn.delivered -isnot [bool]){$valid=$false}
                }}
                foreach($alias in $state.aliases){
                    $owners=@($state.hosts|Where-Object hostSessionKey -CEQ $alias.hostSessionKey|ForEach-Object{$_.turns}|Where-Object requestId -CEQ $alias.requestId)
                    if($owners.Count -ne 1){$valid=$false;continue}
                    foreach($field in $fields){if($owners[0][$field] -cne $alias[$field]){$valid=$false}}
                    if($alias.pendingId -cne ('pending-'+$alias.ingressNonce) -or $alias.acks -isnot [array]){$valid=$false}
                    foreach($ack in $alias.acks){if($ack -cnotin $state.aliasConsumers){$valid=$false}}
                }
                if(-not $valid -and $Mutation -cne 'ignore-corrupt'){$code='store_corrupt';$state=$null}
                else{
                    $pair=$Scenario.trustedPair
                    if($Mutation -ceq 'tool-pair-authority'){$pair=$Scenario.toolParams}
                    if($Mutation -ceq 'latest-pair'){$newest=$state.hosts[0].turns[0];$pair=@{mcpSessionId=$newest.mcpSessionId;requestId=$newest.requestId}}
                    $resolved=Invoke-MockRecoveryBinding $state $pair
                    $ok=$resolved.ok;$code=$resolved.code;$binding=$resolved.binding
                    if($Mutation -ceq 'first-duplicate' -and $code -ceq 'request_ambiguous'){$first=$state.hosts[0].turns[0];$binding=@{};foreach($field in $fields){$binding[$field]=$first[$field]};$ok=$true;$code='resolved'}
                }
            }
        }finally{$effects.Add('Release')}
    }
    $writes=0
    if($Mutation -ceq 'write-selection'){$after=$bytes+' ';$writes=1;$effects.Add('Publish')}
    if($Mutation -ceq 'drop-release'){$effects.Remove('Release')|Out-Null}
    if($Mutation -ceq 'read-before-lock'){$effects.Insert(0,'Read')}
    if($Mutation -ceq 'drop-empty-state'){$state=$null}
    if($Mutation -ceq 'mutate-ack'){$state.aliases[0].acks=@('foreign')}
    if($Mutation -ceq 'mutable-binding' -and $binding){$mutable=@{};foreach($key in $binding.Keys){$mutable[$key]=$binding[$key]};$binding=$mutable}
    return @{result=@{ok=$ok;code=$code;retryable=$retry;binding=$binding;aliases=@();state=$state};before=$bytes;after=$after;writes=$writes;effects=@($effects)}
}

