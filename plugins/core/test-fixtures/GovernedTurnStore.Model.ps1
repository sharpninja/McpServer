# SPDX-License-Identifier: Apache-2.0
<#
.SYNOPSIS
Independent test-only executable A8 lifecycle model.
.DESCRIPTION
The contract runner and child workers supply the same operations and captured
bindings to this disposable model and the production boundary. This model never
receives expected states or case names. Its simple file adapter lets the common
runner inject malformed storage, lock contention and interrupted publication.
It supplies assertion evidence, not proof of production concurrency/durability.
#>
function Invoke-MockGovernedTurnStore {
    [CmdletBinding()]
    param([string]$Directory,[Collections.IDictionary]$Context,[string]$Operation,
        [Collections.IDictionary]$InputObject,[scriptblock]$PublicationObserver={param($Phase) [void]$Phase},[string]$Mutation='')
    function Copy-ModelSnapshot {
        param([object]$Value)
        return ,(ConvertFrom-Json (ConvertTo-Json -InputObject $Value -Depth 60 -Compress) -AsHashtable -DateKind String -NoEnumerate)
    }
    function Get-ModelStoreResult {
        param([string]$Code,[bool]$Ok=$true,[object]$Binding=$null,[object[]]$Aliases=@(),[switch]$NoState,[bool]$Retryable=$false)
        return @{ok=$Ok;code=$Code;retryable=$Retryable;binding=$Binding;aliases=(Copy-ModelSnapshot $Aliases);state=$(if($NoState){$null}else{Copy-ModelSnapshot $state})}
    }
    function Test-ModelBinding {
        param([Collections.IDictionary]$Binding)
        if((@($Binding.Keys|Sort-Object) -join '|') -cne (@($fields|Sort-Object) -join '|')){return $false}
        foreach($field in $fields){if($Binding[$field] -isnot [string] -or [string]::IsNullOrWhiteSpace($Binding[$field])){return $false}}
        return $Binding.workspacePath -ceq $Context.workspacePath -and $Binding.agentIdentity -ceq $Context.agentIdentity -and ($Binding.requestId -cnotlike 'pending-*' -or $Binding.requestId -ceq ('pending-'+$Binding.ingressNonce))
    }
    function Resolve-ModelAliasId {
        param([string]$Id,[string]$HostKey)
        $binding=@($state.aliases|Where-Object {$_.hostSessionKey -ceq $HostKey -and $_.pendingId -ceq $Id})
        if($binding.Count -eq 1){return $binding[0].requestId}
        return $Id
    }
    function Resolve-ModelStoredBinding {
        $selection=@{Context=$state;HostSessionKey=$InputObject.hostSessionKey}
        if($InputObject.Contains('requestId')){$selection.RequestId=Resolve-ModelAliasId $InputObject.requestId $InputObject.hostSessionKey}
        return Resolve-MockGovernedTurnBinding @selection
    }
    function Save-ModelSnapshot {
        $staged=$statePath+'.'+[guid]::NewGuid().ToString('N')+'.tmp'
        [IO.File]::WriteAllText($staged,(ConvertTo-Json -InputObject $state -Depth 60))
        & $observer 'staged'
        [IO.File]::Copy($staged,$statePath,$true)
        & $observer 'published'
    }
    $observer=$PublicationObserver
    $fields=@('workspacePath','agentIdentity','mcpSessionId','hostSessionKey','keySource','ingressNonce','requestId','boundAtUtc')
    $statePath=Join-Path $Directory 'turn-store.json';$lockPath=Join-Path $Directory 'turn-store.lock';$handle=$null;$state=$null
    try{
        if([IO.DirectoryInfo]::new($Directory).LinkTarget -or [IO.FileInfo]::new($statePath).LinkTarget -or [IO.FileInfo]::new($lockPath).LinkTarget){return Get-ModelStoreResult store_corrupt $false -NoState}
        [void][IO.Directory]::CreateDirectory($Directory)
        for($attempt=0;$attempt -lt 100;$attempt++){
            try{$handle=[IO.File]::Open($lockPath,[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None);break}catch [IO.IOException]{Start-Sleep -Milliseconds 10}
        }
        if($null -eq $handle){return Get-ModelStoreResult store_unavailable $false -NoState -Retryable $true}
        if([IO.File]::Exists($statePath)){
            try{
                $state=ConvertFrom-Json ([IO.File]::ReadAllText($statePath)) -AsHashtable -DateKind String -NoEnumerate
                if($state.schemaVersion -ne 1 -or $state.revision -isnot [ValueType] -or $state.revision -lt 0 -or $state.hosts -isnot [array] -or $state.aliases -isnot [array]){throw 'Malformed model snapshot'}
            }catch{return Get-ModelStoreResult store_corrupt $false -NoState}
            if($state.workspacePath -cne $Context.workspacePath -or $state.agentIdentity -cne $Context.agentIdentity -or (ConvertTo-Json -InputObject $state.aliasConsumers -Compress) -cne (ConvertTo-Json -InputObject $Context.aliasConsumers -Compress)){return Get-ModelStoreResult context_mismatch $false -NoState}
        }else{
            $state=@{schemaVersion=1;workspacePath=$Context.workspacePath;agentIdentity=$Context.agentIdentity;aliasConsumers=@($Context.aliasConsumers);revision=0;hosts=@();aliases=@()}
        }
        if($Operation -ceq 'Register'){
            if(-not (Test-ModelBinding $InputObject)){return Get-ModelStoreResult ingress_nonce_invalid $false}
            $hostEntry=@($state.hosts|Where-Object hostSessionKey -CEQ $InputObject.hostSessionKey)|Select-Object -First 1
            if($null -ne $hostEntry){
                $same=@($hostEntry.turns|Where-Object ingressNonce -CEQ $InputObject.ingressNonce)
                if($same.Count){
                    foreach($field in $fields){
                        $candidate=if($field -ceq 'requestId'){Resolve-ModelAliasId $InputObject.requestId $InputObject.hostSessionKey}else{$InputObject[$field]}
                        if($same[0][$field] -cne $candidate){return Get-ModelStoreResult ingress_nonce_invalid $false}
                    }
                    return Get-ModelStoreResult unchanged
                }
                if(@($hostEntry.turns|Where-Object requestId -CEQ $InputObject.requestId).Count){return Get-ModelStoreResult ingress_nonce_invalid $false}
            }else{$hostEntry=@{hostSessionKey=$InputObject.hostSessionKey;turns=@()};$state.hosts+=@($hostEntry)}
            $turn=Copy-ModelSnapshot $InputObject;$turn.delivered=($Mutation -ceq 'deliver-on-register');$turn.status='open'
            if($Mutation -ceq 'drop-original-owner'){$turn.mcpSessionId='wrong-session'}
            $hostEntry.turns+=@($turn);$state.revision++;Save-ModelSnapshot
            return Get-ModelStoreResult registered
        }
        if($Operation -ceq 'Resolve'){
            $resolved=Resolve-ModelStoredBinding
            return Get-ModelStoreResult $resolved.code $resolved.ok $resolved.binding
        }
        if($Operation -ceq 'Alias'){
            foreach($field in @($fields|Where-Object {$_ -cne 'requestId'})){
                if(-not $InputObject.Contains($field) -or $InputObject[$field] -isnot [string]){return Get-ModelStoreResult alias_conflict $false}
            }
            if($InputObject.workspacePath -cne $Context.workspacePath -or $InputObject.agentIdentity -cne $Context.agentIdentity -or $InputObject.pendingId -cne ('pending-'+$InputObject.ingressNonce) -or [string]::IsNullOrWhiteSpace($InputObject.requestId) -or $InputObject.requestId.StartsWith('pending-') -or $InputObject.aliasedAtUtc -isnot [string]){return Get-ModelStoreResult alias_conflict $false}
            $existing=@($state.aliases|Where-Object {$_.hostSessionKey -ceq $InputObject.hostSessionKey -and ($_.pendingId -ceq $InputObject.pendingId -or $_.requestId -ceq $InputObject.requestId)})
            if($existing.Count){
                foreach($field in @($fields)+@('pendingId')){if($existing[0][$field] -cne $InputObject[$field]){return Get-ModelStoreResult alias_conflict $false}}
                return Get-ModelStoreResult unchanged
            }
            $hostEntry=@($state.hosts|Where-Object hostSessionKey -CEQ $InputObject.hostSessionKey)|Select-Object -First 1
            if($null -eq $hostEntry){return Get-ModelStoreResult alias_conflict $false}
            $old=@($hostEntry.turns|Where-Object requestId -CEQ $InputObject.pendingId)
            if($old.Count -ne 1 -or $old[0].status -cne 'open' -or @($hostEntry.turns|Where-Object requestId -CEQ $InputObject.requestId).Count){return Get-ModelStoreResult alias_conflict $false}
            foreach($field in @($fields|Where-Object {$_ -cne 'requestId'})){if($old[0][$field] -cne $InputObject[$field]){return Get-ModelStoreResult alias_conflict $false}}
            if($Mutation -ceq 'omit-alias'){return Get-ModelStoreResult aliased}
            $committed=Copy-ModelSnapshot $InputObject;$committed.acks=@();$state.aliases+=@($committed)
            $old[0].requestId=$InputObject.requestId;$state.revision++;Save-ModelSnapshot
            return Get-ModelStoreResult aliased
        }
        if($Operation -ceq 'ListAliases'){
            if($InputObject.Contains('consumerId') -and $InputObject.consumerId -cnotin $Context.aliasConsumers){return Get-ModelStoreResult consumer_unknown $false}
            $selected=@($state.aliases|Where-Object {$_.hostSessionKey -ceq $InputObject.hostSessionKey -and (-not $InputObject.Contains('consumerId') -or $InputObject.consumerId -cnotin $_.acks)})
            return Get-ModelStoreResult listed -Aliases $selected
        }
        if($Operation -ceq 'Acknowledge'){
            if($InputObject.consumerId -cnotin $Context.aliasConsumers){return Get-ModelStoreResult consumer_unknown $false}
            $selected=@($state.aliases|Where-Object {$_.hostSessionKey -ceq $InputObject.hostSessionKey -and $_.mcpSessionId -ceq $InputObject.mcpSessionId -and $_.pendingId -ceq $InputObject.pendingId -and $_.requestId -ceq $InputObject.requestId})
            if($selected.Count -ne 1){return Get-ModelStoreResult alias_conflict $false}
            if($InputObject.consumerId -cin $selected[0].acks){return Get-ModelStoreResult unchanged}
            if($Mutation -ceq 'omit-ack'){return Get-ModelStoreResult acknowledged}
            $selected[0].acks+=@($InputObject.consumerId)
            if($Mutation -ceq 'discard-acknowledged' -and $selected[0].acks.Count -eq $Context.aliasConsumers.Count){$state.aliases=@()}
            $state.revision++;Save-ModelSnapshot
            return Get-ModelStoreResult acknowledged
        }
        if($Operation -cin @('Deliver','Terminal')){
            $resolved=Resolve-ModelStoredBinding
            if(-not $resolved.ok -and -not ($Operation -ceq 'Terminal' -and $resolved.code -ceq 'turn_immutable')){return Get-ModelStoreResult $resolved.code $false}
            $request=Resolve-ModelAliasId $InputObject.requestId $InputObject.hostSessionKey
            $hostEntry=@($state.hosts|Where-Object hostSessionKey -CEQ $InputObject.hostSessionKey)[0]
            $selected=@($hostEntry.turns|Where-Object requestId -CEQ $request)[0]
            if($Operation -ceq 'Terminal'){
                if($selected.status -ceq 'terminal'){return Get-ModelStoreResult unchanged}
                $selected.status='terminal';$code='terminal'
                if($Mutation -ceq 'cancel-other-turns'){foreach($turn in $hostEntry.turns){$turn.status='terminal'}}
            }else{
                if($selected.delivered){return Get-ModelStoreResult unchanged}
                $selected.delivered=$true;$code='delivered'
            }
            $state.revision++;Save-ModelSnapshot
            return Get-ModelStoreResult $code
        }
        return Get-ModelStoreResult request_unknown $false
    }catch{
        return Get-ModelStoreResult store_unavailable $false -NoState -Retryable $true
    }finally{if($null -ne $handle){$handle.Dispose()}}
}
