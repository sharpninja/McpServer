# SPDX-License-Identifier: Apache-2.0
<#
.SYNOPSIS
FR-QB-003/005 TR-QB-AUDIT-001 AC/TEST-QB-008/009/069: same A8/outbox integration scenarios.
#>
[CmdletBinding(SupportsShouldProcess)]
param([ValidateSet('Mock','Production')][string]$Mode='Mock',[Parameter(Mandatory)][string]$ReceiptPath,[switch]$LegacySeed,[string]$FixtureDirectory=$PSScriptRoot)
$ErrorActionPreference='Stop'
if(-not $PSCmdlet.ShouldProcess($ReceiptPath,('Execute8original plus1legacy-applied alias-consumer cases and'+$(if($LegacySeed){8}else{5})+'interaction controls with retained storage'))){return}
Import-Module powershell-yaml
Import-Module PowerShell.MCP
. (Join-Path $FixtureDirectory 'GovernedTurnBinding.Model.ps1')
. (Join-Path $FixtureDirectory 'GovernedTurnStore.Model.ps1')
. (Join-Path $FixtureDirectory 'GovernedAuditOutbox.Durable.Model.ps1')
. (Join-Path $FixtureDirectory 'GovernedAuditAliasConsumer.Model.ps1')
if($Mode -ceq 'Production'){. (Join-Path $FixtureDirectory '../lib-ps/governed-turn-store.ps1');. (Join-Path $FixtureDirectory '../lib-ps/governed-audit-outbox.ps1');. (Join-Path $FixtureDirectory '../lib-ps/governed-audit-alias-consumer.ps1')}
. (Join-Path $PSScriptRoot 'GovernedAuditAliasConsumer.LegacySeed.ps1')
$base=Join-Path ([IO.Path]::GetFullPath((Join-Path $FixtureDirectory '../../../.mcpServer'))) ('a-'+[guid]::NewGuid().ToString('N').Substring(0,8))
function Copy-AliasFixtureValue {
    param($Value)
    return ,(ConvertFrom-Json (ConvertTo-Json -InputObject $Value -Depth 60 -Compress) -AsHashtable -DateKind String -NoEnumerate)
}
function Invoke-AliasScenario {
    param([string]$Name,[string]$Mutation='')
    $fixtureDirectory=Join-Path $base ($Name+'-'+[guid]::NewGuid().ToString('N').Substring(0,8))
    $fixtureStore=$fixtureDirectory+'/store';$fixtureOutbox=$fixtureDirectory+'/outbox'
    $fixtureContext=@{workspacePath='/fixture/alias';agentIdentity='Codex';aliasConsumers=@('qb-audit-outbox-v1','another-consumer')}
    $fixtureEffects=[Collections.Generic.List[object]]::new()
    $fixtureFault=@{value='';stopOnce=$false}
    $fixtureProvider=if($Mutation){'Mock'}else{$Mode}
    $store={
        param($Directory,$Context,$Operation,$InputObject)
        $inputCopy=Copy-AliasFixtureValue $InputObject
        $resolvedAtAck=$null
        if($Operation -ceq 'Acknowledge'){
            $outboxContext=@{workspacePath=$fixtureContext.workspacePath;agentIdentity=$fixtureContext.agentIdentity;mcpSessionId=$InputObject.mcpSessionId;hostSessionKey='host-alias'}
            $resolveArgs=@{RootDirectory=$fixtureOutbox;Context=$outboxContext;Operation='Resolve';InputObject=@{requestId=$InputObject.pendingId};PublicationObserver={param($Phase,$Path) [void]$Phase;[void]$Path}}
            $resolvedAtAck=if($fixtureProvider -ceq 'Mock'){Invoke-MockDurableAuditOutbox @resolveArgs}else{Invoke-McpGovernedAuditOutbox @resolveArgs}
            if($fixtureFault.value -ceq 'reject-ack'){$inputCopy.consumerId='unregistered-consumer'}
        }
        $fixtureEffects.Add(@{boundary='store';operation=$Operation;input=$inputCopy;resolvedAtAck=$resolvedAtAck})
        $arguments=@{Directory=$Directory;Context=$Context;Operation=$Operation;InputObject=$inputCopy}
        if($Mutation -ceq 'legacy-drop-acks' -and $Operation -ceq 'Acknowledge'){$arguments.Mutation='discard-acknowledged'}
        $reply=if($fixtureProvider -ceq 'Mock'){Invoke-MockGovernedTurnStore @arguments}else{Invoke-McpGovernedTurnStore @arguments}
        if($Mutation -ceq 'missing-list' -and $Operation -ceq 'ListAliases'){$reply.Remove('aliases')}
        return $reply
    }
    $outbox={
        param($RootDirectory,$Context,$Operation,$InputObject)
        $inputCopy=Copy-AliasFixtureValue $InputObject
        if($fixtureFault.value -ceq 'reject-apply' -and $Operation -ceq 'ApplyAlias'){$inputCopy.mcpSessionId='foreign-session'}
        $fixtureEffects.Add(@{boundary='outbox';operation=$Operation;input=$inputCopy})
        $arguments=@{RootDirectory=$RootDirectory;Context=$Context;Operation=$Operation;InputObject=$inputCopy;PublicationObserver={param($Phase,$Path) [void]$Phase;[void]$Path};Clock={1000};Sleep={param($Milliseconds) [void]$Milliseconds}}
        if($Operation -ceq 'Flush'){
            $arguments.Transport=@{submit={param($AuditRecord) $reply=@{code='persisted';revision=9};foreach($key in @('workspacePath','agentIdentity','mcpSessionId','requestId','eventId','order','payloadHash')){$reply[$key]=$AuditRecord[$key]};return $reply};read={param($AuditRecord) [void]$AuditRecord;throw 'Unexpected readback'}}
        }
        if($fixtureProvider -ceq 'Mock'){return Invoke-MockDurableAuditOutbox @arguments}
        return Invoke-McpGovernedAuditOutbox @arguments
    }
    $dependencies=@{Store=$store;Outbox=$outbox}
    $captures=[Collections.Generic.List[object]]::new()
    $bindings=@(foreach($number in @(1..$(if($Name -ceq 'multiple-sessions'){2}else{1}))){
        $binding=@{workspacePath=$fixtureContext.workspacePath;agentIdentity=$fixtureContext.agentIdentity;mcpSessionId=('session-'+$number);hostSessionKey='host-alias';keySource='native';ingressNonce=('nonce-'+$number);requestId=('pending-nonce-'+$number);boundAtUtc='2026-10-14T23:00:00.0000000Z'}
        if(-not $LegacySeed){
        $registration=& $store $fixtureStore $fixtureContext 'Register' $binding
        if(-not $registration.ok){throw 'Fixture register failed'}
        $outboxContext=@{workspacePath=$fixtureContext.workspacePath;agentIdentity=$fixtureContext.agentIdentity;mcpSessionId=$binding.mcpSessionId;hostSessionKey=$binding.hostSessionKey}
        foreach($order in 1..2){
            $capture=& $outbox $fixtureOutbox $outboxContext 'Enqueue' @{requestId=$binding.requestId;kind='dialog';payloadJson=(ConvertTo-Json -InputObject @{text=("immutable "+$order);unicode=([string][char]0x96EA)} -Compress)}
            if($capture.code -cne 'queued' -or $capture.event.order -ne $order){throw 'Fixture capture failed'}
            $captures.Add(@{context=$outboxContext;event=(Copy-AliasFixtureValue $capture.event)})
        }
        }
        if(-not $LegacySeed -and $Name -cne 'none'){
            $alias=Copy-AliasFixtureValue $binding
            $alias.pendingId=$binding.requestId;$alias.requestId='minted-'+$number;$alias.aliasedAtUtc='2026-10-14T23:01:00.0000000Z'
            $aliased=& $store $fixtureStore $fixtureContext 'Alias' $alias
            if(-not $aliased.ok){throw 'Fixture alias commit failed'}
        }
        $binding
    })
    if($LegacySeed){$journalMutation=switch($Mutation){'legacy-journal-wrong-owner'{'wrong-owner'} 'legacy-journal-wrong-target'{'wrong-target'} default{'none'}};Initialize-LegacyAuditAliasFixture -Context $fixtureContext -Bindings $bindings -Case $Name -Store $fixtureStore -Outbox $fixtureOutbox -AppliedJournal:($Name -ceq 'already-applied') -JournalMutation $journalMutation}
    $before=@(Get-ChildItem -LiteralPath $fixtureOutbox -File -Recurse|Where-Object Name -Like '*.event.yaml'|ForEach-Object {@{name=$_.Name;event=(ConvertFrom-Yaml ([IO.File]::ReadAllText($_.FullName))).event}})
    $fixtureEffects.Clear()
    $fixtureFault.value=switch($Name){'application-failure'{'reject-apply'} 'ack-failure'{'reject-ack'} default{''}}
    $fixtureFault.stopOnce=$Name -ceq 'interrupted'
    $observer={param($Phase,$Alias) [void]$Alias;if($Phase -ceq 'applied' -and $fixtureFault.stopOnce){$fixtureFault.stopOnce=$false;throw 'Fixture interrupted after durable application'}}
    $consumerContext=Copy-AliasFixtureValue $fixtureContext
    if($Name -ceq 'foreign-context'){$consumerContext.workspacePath='/foreign'}
    $consumerArgs=@{StoreDirectory=$fixtureStore;OutboxRoot=$fixtureOutbox;Context=$consumerContext;HostSessionKey='host-alias';Dependencies=$dependencies;StepObserver=$observer}
    $result=if($fixtureProvider -ceq 'Mock'){Invoke-MockAuditAliasConsumer @consumerArgs -Mutation $Mutation}else{Invoke-McpGovernedAuditAliasConsumer @consumerArgs}
    $recover=$Name -cin @('interrupted','ack-failure')
    if($recover){
        if($result.ok){throw 'Interrupted or refused acknowledgement reported success'}
        $unacked=& $store $fixtureStore $fixtureContext 'ListAliases' @{hostSessionKey='host-alias';consumerId='qb-audit-outbox-v1'}
        if($unacked.aliases.Count -ne 1){throw 'Unacknowledged durable alias disappeared before retry'}
        $fixtureFault.value=''
        $result=if($fixtureProvider -ceq 'Mock'){Invoke-MockAuditAliasConsumer @consumerArgs -Mutation $Mutation}else{Invoke-McpGovernedAuditAliasConsumer @consumerArgs}
    }
    $success=$Name -cnotin @('application-failure','foreign-context')
    if($result.ok -cne $success){throw 'Consumer outcome disagrees with actual scenario'}
    foreach($effect in @($fixtureEffects|Where-Object operation -CEQ 'Acknowledge')){
        if($effect.resolvedAtAck.code -cne 'resolved' -or $effect.resolvedAtAck.requestId -cne $effect.input.requestId){throw 'Acknowledgement preceded durable outbox application'}
    }
    $unacked=& $store $fixtureStore $fixtureContext 'ListAliases' @{hostSessionKey='host-alias';consumerId='qb-audit-outbox-v1'}
    if($unacked.aliases.Count -ne $(if($success){0}else{1})){throw 'Durable acknowledgement enumeration differs'}
    if($LegacySeed -and $success -and $Name -cne 'none'){
        $all=& $store $fixtureStore $fixtureContext 'ListAliases' @{hostSessionKey='host-alias'}
        if($all.aliases.Count -ne $bindings.Count){throw 'Legacy committed alias count differs'}
        foreach($legacyAlias in $all.aliases){if($legacyAlias.acks -cnotcontains 'another-consumer' -or $legacyAlias.acks -cnotcontains 'qb-audit-outbox-v1'){throw 'Original legacy acknowledgment lost'}}
    }
    $after=@(Get-ChildItem -LiteralPath $fixtureOutbox -File -Recurse|Where-Object Name -Like '*.event.yaml'|ForEach-Object {@{name=$_.Name;event=(ConvertFrom-Yaml ([IO.File]::ReadAllText($_.FullName))).event}})
    if($after.Count -ne $before.Count){throw 'Alias application duplicated or lost event'}
    foreach($original in $before){
        $current=@($after|Where-Object name -CEQ $original.name)
        if($current.Count -ne 1){throw 'Alias changed stable filename'}
        foreach($field in @('workspacePath','agentIdentity','mcpSessionId','eventId','order','kind','payloadJson','payloadHash')){
            if($current[0].event[$field] -cne $original.event[$field]){throw ('Immutable captured event changed:'+ $field)}
        }
        $expectedRequest=if($success -and $Name -cne 'none'){$original.event.requestId.Replace('pending-nonce-','minted-')}else{$original.event.requestId}
        if($current[0].event.requestId -cne $expectedRequest){throw 'Durable event owner did not reconcile'}
    }
    if($Name -ceq 'duplicate'){
        $storeHash=(Get-FileHash ($fixtureStore+'/turn-store.json')).Hash
        $again=if($fixtureProvider -ceq 'Mock'){Invoke-MockAuditAliasConsumer @consumerArgs -Mutation $Mutation}else{Invoke-McpGovernedAuditAliasConsumer @consumerArgs}
        if(-not $again.ok -or (Get-FileHash ($fixtureStore+'/turn-store.json')).Hash -cne $storeHash){throw 'Duplicate consumer changed durable acknowledgement'}
    }
    foreach($binding in $bindings){
        $context=@{workspacePath=$fixtureContext.workspacePath;agentIdentity=$fixtureContext.agentIdentity;mcpSessionId=$binding.mcpSessionId;hostSessionKey='host-alias'}
        if($success -and $Name -cne 'none'){
            $minted=$binding.requestId.Replace('pending-nonce-','minted-')
            $next=& $outbox $fixtureOutbox $context 'Enqueue' @{requestId=$minted;kind='dialog';payloadJson=(ConvertTo-Json -InputObject @{text='third'} -Compress)}
            if($next.event.order -ne 3){throw 'Alias reset logical turn order'}
            $flushed=& $outbox $fixtureOutbox $context 'Flush' @{}
            if($flushed.persisted -ne 3 -or $flushed.pending -ne 0 -or $flushed.quarantined -ne 0){throw 'Aliased events did not flush in captured ownership'}
        }
    }
    return @{case=$Name;result=$result;eventsBefore=$before;eventsAfter=$after;unacknowledged=$unacked.aliases;effects=@($fixtureEffects);directory=$fixtureDirectory}
}
$results=[Collections.Generic.List[object]]::new();$controls=[Collections.Generic.List[object]]::new();$failure=$null
try{
    foreach($case in @('none','apply','duplicate','interrupted','application-failure','ack-failure','foreign-context','multiple-sessions')+$(if($LegacySeed){@('already-applied')}else{@()})){$results.Add((Invoke-AliasScenario $case))}
    foreach($mutation in (@('ack-first','omit-apply','omit-ack','missing-list','ack-first')+$(if($LegacySeed){@('legacy-drop-acks')}else{@()}))){
        $case=if($controls.Count -eq 4){'interrupted'}else{'apply'}
        $rejection=''
        try{$null=Invoke-AliasScenario $case $mutation}catch{$rejection=$_.Exception.Message}
        if(-not $rejection){throw 'Wrong dependency interaction escaped common integration assertions'}
        if($mutation -ceq 'legacy-drop-acks' -and $rejection -cne 'Legacy committed alias count differs'){throw ('Wrong legacy negative-control rejection: '+$rejection)}
        $controls.Add(@{case=$case;mutation=$mutation;assertion=$rejection;rejected=$true})
    }
    if($LegacySeed){foreach($mutation in @('legacy-journal-wrong-owner','legacy-journal-wrong-target')){$rejection='';try{$null=Invoke-AliasScenario 'already-applied' $mutation}catch{$rejection=$_.Exception.Message};if($rejection -cne 'Consumer outcome disagrees with actual scenario'){throw ('Wrong applied-journal negative-control rejection: '+$rejection)};$controls.Add(@{case='already-applied';mutation=$mutation;assertion=$rejection;rejected=$true})}}
}catch{$failure=@{message=$_.Exception.Message;stack=$_.ScriptStackTrace}}
$receipt=[ordered]@{timestamp=[DateTimeOffset]::Now.ToString('o');mode=$Mode;cases=$results.Count;plannedCases=$(if($LegacySeed){9}else{8});legacySeed=[bool]$LegacySeed;originalRunnerSha256='111CCE6F1398CD292B7D2E7CE45BB9C99FED0DA5B590FF073F4D5D3E6EBE4378';negativeControls=$controls.Count;failed=$(if($failure){1}else{0});skipped=0;failure=$failure;results=@($results);controls=@($controls);fixtureSha256=(Get-FileHash $PSCommandPath).Hash;modelSha256=(Get-FileHash (Join-Path $FixtureDirectory 'GovernedAuditAliasConsumer.Model.ps1')).Hash;productionSha256=$(if($Mode -ceq 'Production'){(Get-FileHash (Join-Path $FixtureDirectory '../lib-ps/governed-audit-alias-consumer.ps1')).Hash}else{$null});scope='Consumer integration; host startup/mint/drain wiring, legacy migration, alias process-crash windows still required';productComplete=$false}
Add-LinesToFile -Path $ReceiptPath -Content ($receipt|ConvertTo-Json -Depth 80)|Out-Null
[pscustomobject]$receipt|Select-Object mode,cases,plannedCases,negativeControls,failed,skipped,failure|ConvertTo-Json -Depth 8
if($failure){throw $failure.message}
