#Requires -Version 7.5
# SPDX-License-Identifier: Apache-2.0
function Invoke-McpGovernedSessionHost {
    <#
    .SYNOPSIS
    FR-QB-002/003/005: Compose the existing A8 lifecycle and immutable audit outbox.
    .DESCRIPTION
    Trusted host injection supplies roots, captured ingress and existing typed
    SessionLog dependencies. Tool arguments supply content and an optional request
    selector only. This boundary owns no second turn map, cache or transport.
    .PARAMETER StoreDirectory
    Private A8 store directory selected by validated host bootstrap.
    .PARAMETER OutboxRoot
    Private existing audit outbox root selected by validated host bootstrap.
    .PARAMETER HostContext
    Actual captured workspace, agent, MCP session, native host key/source, nonce,
    ingress time/text, operation identity and request classification.
    .PARAMETER Operation
    Canonical lifecycle, explicit decision, review or ordinary audit operation.
    .PARAMETER InputObject
    Content and optional request ID. Cannot replace trusted captured roots.
    The dispatch adapter alone adds batchIndex for multi-item input; this internal
    index separates capture keys while retaining the original host operation ID.
    .PARAMETER Server
    Existing typed client adapter: Begin, ReadTurn, Submit, ReadAudit, Complete, Fail.
    .PARAMETER CompletionPolicy
    Trusted stateless obligation gate. It must return code allow, continue or blocked.
    .PARAMETER MintRequestId
    Internal deterministic fixture seam; production uses a fresh timestamp and UUID.
    .PARAMETER StepObserver
    Internal publication evidence seam, never enabled by model arguments.
    .OUTPUTS
    Rooted result. Success means verified persistence, never operator delivery.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$StoreDirectory,
        [Parameter(Mandatory)][string]$OutboxRoot,
        [Parameter(Mandatory)][Collections.IDictionary]$HostContext,
        [Parameter(Mandatory)][ValidateSet('mintTurn','beginTurn','resolve','appendAction','appendDialog','decision','recordReview','completeTurn','failTurn')][string]$Operation,
        [Collections.IDictionary]$InputObject=@{},
        [Parameter(Mandatory)][scriptblock]$Server,
        [Parameter(Mandatory)][scriptblock]$CompletionPolicy,
        [scriptblock]$MintRequestId={ 'req-'+[DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')+'-'+[guid]::NewGuid().ToString('N') },
        [scriptblock]$StepObserver={param($Stage,$Binding) [void]$Stage;[void]$Binding}
    )
    . (Join-Path $PSScriptRoot 'governed-turn-store.ps1')
    . (Join-Path $PSScriptRoot 'governed-audit-alias-consumer.ps1')
    . (Join-Path $PSScriptRoot 'governed-audit-outbox.ps1')
    . (Join-Path $PSScriptRoot 'governed-payload.ps1')
    $binding=$null
    function Get-SessionHostResult {
        param([string]$Code,[bool]$Ok=$false,[object]$AuditRecord=$null,[object]$Detail=$null)
        return @{ok=$Ok;code=$Code;binding=$binding;event=$AuditRecord;detail=$Detail}
    }
    function Get-SessionHostPublication {
        $publicationStep=$StepObserver;$publicationBinding=$binding
        return {
            param($Stage,$Path)
            if($Stage -ceq 'retired'){& $publicationStep 'Retired' $publicationBinding;return}
            if($Stage -cne 'published'){return}
            if([IO.Path]::GetFileName($Path) -ceq 'sequence.yaml'){& $publicationStep 'CaptureJournal' $publicationBinding;return}
            if($Path.EndsWith('.event.yaml',[StringComparison]::Ordinal)){
                $record=ConvertFrom-Yaml ([IO.File]::ReadAllText($Path))
                if($record.receipt){& $publicationStep 'ReceiptPublished' $publicationBinding}
                elseif($record.attempts -eq 0){& $publicationStep 'CaptureEvent' $publicationBinding}
            }
        }.GetNewClosure()
    }
    function Invoke-SessionHostAliasReconcile {
        $aliasStep=$StepObserver
        $aliasObserver={param($Stage,$Alias) if($Stage -ceq 'applied'){& $aliasStep 'AliasApplied' $Alias}}.GetNewClosure()
        return Invoke-McpGovernedAuditAliasConsumer -StoreDirectory $StoreDirectory -OutboxRoot $OutboxRoot -Context $owner -HostSessionKey $binding.hostSessionKey -StepObserver $aliasObserver
    }
    function Get-SessionHostCapture {
        param([string]$Kind,[string]$Payload,[string]$Key,[string]$Role='')
        $normalized=ConvertTo-McpGovernedPayloadJson -Json $Payload
        return Invoke-McpGovernedAuditOutbox -RootDirectory $OutboxRoot -Context $binding -Operation Enqueue -InputObject @{requestId=$binding.requestId;kind=$Kind;payloadJson=$normalized;captureKey=$Key;evidenceRole=$Role} -PublicationObserver (Get-SessionHostPublication)
    }
    function Get-SessionHostTransport {
        $serverForAudit=$Server;$bindingForAudit=$binding
        return @{
            submit={param($AuditRecord) & $serverForAudit 'Submit' $bindingForAudit $AuditRecord}.GetNewClosure()
            read={param($AuditRecord) & $serverForAudit 'ReadAudit' $bindingForAudit $AuditRecord}.GetNewClosure()
        }
    }
    function Test-SessionHostFlush {
        $flushed=Invoke-McpGovernedAuditOutbox -RootDirectory $OutboxRoot -Context $binding -Operation Flush -Transport (Get-SessionHostTransport) -PublicationObserver (Get-SessionHostPublication)
        return ($flushed.code -ceq 'flushed' -and $flushed.pending -eq 0 -and $flushed.quarantined -eq 0 -and $flushed.rejected -eq 0)
    }
    function Test-SessionHostTurnRoot {
        param([object]$Turn)
        if($Turn -isnot [Collections.IDictionary]){return $false}
        foreach($field in @('workspacePath','agentIdentity','mcpSessionId','requestId')){if($Turn[$field] -isnot [string] -or $Turn[$field] -cne $binding[$field]){return $false}}
        return $true
    }
    try {
        $owner=@{workspacePath=$HostContext.workspacePath;agentIdentity=$HostContext.agentIdentity;aliasConsumers=@('qb-audit-outbox-v1')}
        if($Operation -cin @('mintTurn','beginTurn')){
            if($HostContext.actualIngress -ne $true){return Get-SessionHostResult 'mint_requires_host_ingress'}
            foreach($field in @('workspacePath','agentIdentity','mcpSessionId','hostSessionKey','keySource','ingressNonce','boundAtUtc')){
                if($HostContext[$field] -isnot [string] -or [string]::IsNullOrWhiteSpace($HostContext[$field])){return Get-SessionHostResult 'nonce_source_missing'}
            }
            if($HostContext.ingressText -isnot [string]){return Get-SessionHostResult 'ingress_nonce_invalid'}
            $binding=@{};foreach($field in @('workspacePath','agentIdentity','mcpSessionId','hostSessionKey','keySource','ingressNonce','boundAtUtc')){$binding[$field]=$HostContext[$field]}
            $binding.requestId='pending-'+$binding.ingressNonce
            $registered=Invoke-McpGovernedTurnStore -Directory $StoreDirectory -Context $owner -Operation Register -InputObject $binding
            if(-not $registered.ok){return Get-SessionHostResult $registered.code}
            $stored=@($registered.state.hosts|Where-Object hostSessionKey -CEQ $binding.hostSessionKey|ForEach-Object {$_.turns}|Where-Object ingressNonce -CEQ $binding.ingressNonce)
            if($stored.Count -ne 1 -or $stored[0].status -cne 'open'){return Get-SessionHostResult 'turn_immutable'}
            $binding.requestId=$stored[0].requestId
            if(-not $binding.requestId.StartsWith('pending-',[StringComparison]::Ordinal)){
                $reconciled=Invoke-SessionHostAliasReconcile
                if(-not $reconciled.ok){return Get-SessionHostResult $reconciled.code -Detail $reconciled}
            }
            $ingress=@{queryText=$HostContext.ingressText;requestClass=$HostContext.requestClass}|ConvertTo-Json -Compress
            $capture=Get-SessionHostCapture 'metadata' $ingress ('ingress:'+$binding.ingressNonce) 'ingress'
            if($capture.code -cne 'queued'){return Get-SessionHostResult 'lost' -Detail $capture}
            if($binding.requestId.StartsWith('pending-',[StringComparison]::Ordinal)){
                $pending=$binding.requestId;$minted=& $MintRequestId
                if($minted -isnot [string] -or [string]::IsNullOrWhiteSpace($minted) -or $minted.StartsWith('pending-',[StringComparison]::Ordinal)){return Get-SessionHostResult 'ingress_nonce_invalid'}
                $alias=@{};foreach($field in $binding.Keys){$alias[$field]=$binding[$field]}
                $alias.requestId=$minted;$alias.pendingId=$pending;$alias.aliasedAtUtc=$binding.boundAtUtc
                $committed=Invoke-McpGovernedTurnStore -Directory $StoreDirectory -Context $owner -Operation Alias -InputObject $alias
                if(-not $committed.ok){return Get-SessionHostResult $committed.code}
                $binding.requestId=$minted
                & $StepObserver 'AliasCommitted' $binding | Out-Null
            }
            # Reconcile before every replay, including a restart after alias publication.
            $reconciled=Invoke-SessionHostAliasReconcile
            if(-not $reconciled.ok){return Get-SessionHostResult $reconciled.code -Detail $reconciled}
            $normalizedIngress=(ConvertFrom-Json (ConvertTo-McpGovernedPayloadJson $ingress) -AsHashtable).queryText
            $title=$InputObject.queryTitle
            if($title -isnot [string] -or [string]::IsNullOrWhiteSpace($title) -or $title -ceq 'User prompt'){$title='Request '+$binding.ingressNonce}
            # Read first: an ambiguous prior Begin must not overwrite richer persisted state.
            $read=& $Server 'ReadTurn' $binding @{}
            if($null -eq $read){
                try{$null=& $Server 'Begin' $binding @{queryText=$normalizedIngress;queryTitle=$title}}catch{[void]$_.Exception}
                $read=& $Server 'ReadTurn' $binding @{}
            }
            if(-not (Test-SessionHostTurnRoot $read) -or $read.queryText -cne $normalizedIngress -or $read.status -cne 'in_progress'){return Get-SessionHostResult 'ingress_readback_mismatch'}
            if(-not (Test-SessionHostFlush)){return Get-SessionHostResult 'session_turn_completion_gate'}
            if($Operation -ceq 'beginTurn'){
                $delivered=Invoke-McpGovernedTurnStore -Directory $StoreDirectory -Context $owner -Operation Deliver -InputObject @{hostSessionKey=$binding.hostSessionKey;requestId=$binding.requestId}
                if(-not $delivered.ok){return Get-SessionHostResult $delivered.code}
            }
            return Get-SessionHostResult 'bound' $true $capture.event
        }
        $selection=@{hostSessionKey=$HostContext.hostSessionKey}
        if($InputObject.Contains('requestId')){$selection.requestId=$InputObject.requestId}
        $resolved=Invoke-McpGovernedTurnStore -Directory $StoreDirectory -Context $owner -Operation Resolve -InputObject $selection
        if(-not $resolved.ok){return Get-SessionHostResult $resolved.code}
        $binding=$resolved.binding
        $selected=@($resolved.state.hosts|Where-Object hostSessionKey -CEQ $binding.hostSessionKey|ForEach-Object {$_.turns}|Where-Object requestId -CEQ $binding.requestId)
        if($selected.Count -ne 1 -or -not $selected[0].delivered){return Get-SessionHostResult 'request_unknown'}
        if($HostContext.operationId -isnot [string] -or [string]::IsNullOrWhiteSpace($HostContext.operationId)){return Get-SessionHostResult 'nonce_source_missing'}
        $reconciled=Invoke-McpGovernedAuditAliasConsumer -StoreDirectory $StoreDirectory -OutboxRoot $OutboxRoot -Context $owner -HostSessionKey $binding.hostSessionKey
        if(-not $reconciled.ok){return Get-SessionHostResult $reconciled.code -Detail $reconciled}
        if($Operation -ceq 'resolve'){return Get-SessionHostResult 'resolved' $true}
        if($Operation -cin @('appendAction','appendDialog','decision','recordReview')){
            $kind=switch -CaseSensitive ($Operation){'appendAction'{'action'};'appendDialog'{'dialog'};'decision'{'decision'};'recordReview'{'metadata'}}
            $payload=$InputObject.payloadJson
            if($Operation -ceq 'recordReview'){
                $document=[System.Text.Json.JsonDocument]::Parse($payload)
                try{
                    $body=$document.RootElement
                    if($body.GetProperty('reviewType').GetString() -ceq 'Other' -and [string]::IsNullOrWhiteSpace($body.GetProperty('otherNotes').GetString())){return Get-SessionHostResult 'review_invalid'}
                    if($body.GetProperty('OverallVerdict').GetString() -ceq 'AGREE'){
                        foreach($field in @('accuracy','completeness')){if(-not (Test-McpGovernedReviewScore $body.GetProperty($field))){return Get-SessionHostResult 'review_invalid'}}
                    }
                }catch{return Get-SessionHostResult 'review_invalid'}finally{$document.Dispose()}
            }
            $captureKey=$Operation+':'+$HostContext.operationId
            if($InputObject.Contains('batchIndex')){
                $index=$InputObject.batchIndex
                if($Operation -cnotin @('appendAction','appendDialog') -or ($index -isnot [int] -and $index -isnot [long]) -or $index -lt 0){return Get-SessionHostResult 'invalid_params'}
                # A disjoint kind prefix preserves legacy single keys and original operation identity.
                $captureKey=$Operation+'Batch:'+(ConvertTo-Json -InputObject @($HostContext.operationId,$index) -Compress)
            }
            $capture=Get-SessionHostCapture $kind $payload $captureKey
            if($capture.code -cne 'queued'){return Get-SessionHostResult 'lost' -Detail $capture}
            if(-not (Test-SessionHostFlush)){return Get-SessionHostResult 'session_turn_completion_gate'}
            return Get-SessionHostResult 'persisted' $true $capture.event
        }
        $policy=& $CompletionPolicy $binding $Operation $InputObject
        if($policy -isnot [Collections.IDictionary] -or $policy.code -cne 'allow'){
            $code=if($policy.code -cin @('continue','blocked')){$policy.code}else{'session_turn_completion_gate'}
            return Get-SessionHostResult $code -Detail $policy
        }
        $complete=$Operation -ceq 'completeTurn'
        $payload=if($complete){@{response=$InputObject.response}|ConvertTo-Json -Compress}else{@{failureNote=$InputObject.failureNote}|ConvertTo-Json -Compress}
        $capture=Get-SessionHostCapture 'metadata' $payload ('terminal:'+$HostContext.operationId) $(if($complete){'result'}else{''})
        if($capture.code -cne 'queued' -or -not (Test-SessionHostFlush)){return Get-SessionHostResult 'session_turn_completion_gate'}
        $gate=Invoke-McpGovernedAuditOutbox -RootDirectory $OutboxRoot -Context $binding -Operation CheckCapturedCompletion -InputObject @{requestId=$binding.requestId;requiresResult=$complete} -Transport (Get-SessionHostTransport)
        if(-not $gate.allowed){return Get-SessionHostResult 'session_turn_completion_gate' -Detail $gate}
        $authority=Invoke-McpGovernedAuditOutbox -RootDirectory $OutboxRoot -Context $binding -Operation ReadManifest -InputObject @{requestId=$binding.requestId}
        $ingressEvent=@($authority.manifest.expectedEvents|Where-Object eventId -CEQ $authority.manifest.ingressEventId)
        if($ingressEvent.Count -ne 1){return Get-SessionHostResult 'session_turn_completion_gate'}
        $originalIngress=(ConvertFrom-Json $ingressEvent[0].payloadJson -AsHashtable).queryText
        $normalized=ConvertFrom-Json $capture.event.payloadJson -AsHashtable
        $expectedStatus=if($complete){'completed'}else{'failed'}
        $read=& $Server 'ReadTurn' $binding @{}
        if(-not (Test-SessionHostTurnRoot $read) -or $read.queryText -cne $originalIngress){return Get-SessionHostResult 'final_readback_mismatch'}
        if($read.status -ceq 'in_progress'){
            try{$null=& $Server $(if($complete){'Complete'}else{'Fail'}) $binding $normalized}catch{[void]$_.Exception}
            # Any ambiguous terminal submission is reconciled by full readback,
            # never a blind retry or a different turn selected from a cache.
            $read=& $Server 'ReadTurn' $binding @{}
        }
        if(-not (Test-SessionHostTurnRoot $read) -or $read.queryText -cne $originalIngress -or $read.status -cne $expectedStatus -or
            ($complete -and $read.response -cne $normalized.response) -or (-not $complete -and $read.failureNote -cne $normalized.failureNote)){
            return Get-SessionHostResult 'final_readback_mismatch'
        }
        & $StepObserver 'TerminalRead' $binding | Out-Null
        & $StepObserver 'Terminal' $binding | Out-Null
        $terminal=Invoke-McpGovernedTurnStore -Directory $StoreDirectory -Context $owner -Operation Terminal -InputObject @{hostSessionKey=$binding.hostSessionKey;requestId=$binding.requestId}
        return Get-SessionHostResult $expectedStatus $terminal.ok $capture.event
    }catch{
        return Get-SessionHostResult 'session_turn_completion_gate' -Detail @{diagnostic=$_.Exception.ToString()}
    }
}
