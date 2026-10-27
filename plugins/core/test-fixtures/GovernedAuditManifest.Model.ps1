# SPDX-License-Identifier: Apache-2.0
<#
.SYNOPSIS
Independent stateful audit manifest model for the canonical outbox test seam.
.DESCRIPTION
Consumes operation inputs, scripted identity/clock dependencies and persisted
model state. It never reads scenario names, assertions or production source.
#>
function Invoke-MockAuditManifest {
 [CmdletBinding()]
 param([hashtable]$State,[hashtable]$Context,[string]$Operation,[hashtable]$InputObject,[string]$Mutation='',[scriptblock]$CompletionChecker)
 function Copy-ManifestModelValue {
  param([object]$Value)
  return ,(ConvertFrom-Json (ConvertTo-Json -InputObject $Value -Depth 90 -Compress) -AsHashtable -DateKind String -NoEnumerate)
 }
 $id=$InputObject.requestId
 if($id -is [string] -and $State.aliases.ContainsKey($id)){$id=$State.aliases[$id]}
 if($Operation -ceq 'ApplyAlias'){
  if($Mutation -ceq 'forget-alias'){return @{code='applied'}}
  $old=$InputObject.pendingId;$new=$InputObject.requestId
  $State.aliases[$old]=$new
  if($State.manifests.ContainsKey($old)){
   $State.manifests[$new]=$State.manifests[$old];$null=$State.manifests.Remove($old)
   foreach($entry in $State.manifests[$new].expectedEvents){$entry.requestId=$new}
  }
  if($State.orders.ContainsKey($old)){$State.orders[$new]=$State.orders[$old];$null=$State.orders.Remove($old)}
  foreach($record in $State.records){if($record.event.requestId -ceq $old){$record.event.requestId=$new}}
  $State.effects.Add(@{operation='AliasJournal';pendingId=$old;requestId=$new})
  return @{code='applied'}
 }
 if($Operation -ceq 'ReadManifest'){
  if(-not $State.manifests.ContainsKey($id)){return @{code='manifest_missing';manifest=$null}}
  return @{code='manifest';manifest=(Copy-ManifestModelValue $State.manifests[$id])}
 
 }
 if($Operation -ceq 'Restart'){
  $State.crashAfterJournal=$false
  if($Mutation -ceq 'forget-restart'){$State.manifests.Clear()}
  $State.effects.Add(@{operation='Restart'})
  return @{code='restarted'}
 }
 if($Operation -ceq 'Enqueue'){
  $role=if($InputObject.ContainsKey('evidenceRole')){$InputObject.evidenceRole}else{''}
  if($role -cnotin @('','ingress','result')){return @{code='lost'}}
  $capture=if($InputObject.ContainsKey('captureKey')){$InputObject.captureKey}else{''}
  if(-not $State.manifests.ContainsKey($id)){
   $State.manifests[$id]=@{expectedEvents=@();ingressEventId='';resultEventId='';captureKeys=@{};legacyIncomplete=$State.orders.ContainsKey($id)}
  }
  $manifest=$State.manifests[$id]
 
  if($capture -and $manifest.captureKeys.ContainsKey($capture) -and $Mutation -cne 'retry-duplicates'){
   $prior=$manifest.captureKeys[$capture];$original=@($manifest.expectedEvents|Where-Object eventId -CEQ $prior.eventId)[0]
   if($original.kind -cne $InputObject.kind -or $original.payloadJson -cne $InputObject.payloadJson -or $prior.role -cne $role){return @{code='lost'}}
   if($Mutation -cne 'skip-recreate' -and @($State.records|Where-Object {$_.event.eventId -ceq $original.eventId}).Count -eq 0){$State.records.Add(@{event=(Copy-ManifestModelValue $original);state='pending'});$State.effects.Add(@{operation='PublishEvent';eventId=$original.eventId})}
   return @{code='queued';event=(Copy-ManifestModelValue $original)}
  }
  if(($role -ceq 'ingress' -and $manifest.ingressEventId) -or ($role -ceq 'result' -and $manifest.resultEventId)){return @{code='lost'}}
  $next=if($State.orders.ContainsKey($id)){[long]$State.orders[$id]+1}else{1L}
  if($Mutation -ceq 'reset-order'){$next=1L}
  $State.serial++
  $eventId='00000000-0000-4000-8000-'+$State.serial.ToString('000000000000')
  if($Mutation -ceq 'duplicate-uuid'){$eventId='00000000-0000-4000-8000-000000000001'}
  $payload=$InputObject.payloadJson
  if($Mutation -ceq 'change-text'){$payload='{}'}
  $audit=@{workspacePath=$Context.workspacePath;agentIdentity=$Context.agentIdentity;mcpSessionId=$Context.mcpSessionId;requestId=$id;eventId=$eventId;order=$next;kind=$InputObject.kind;payloadJson=$payload;payloadHash=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($payload))).ToLowerInvariant()}
  $State.orders[$id]=$next
  if($Mutation -cne 'drop-journal'){$manifest.expectedEvents+=@(Copy-ManifestModelValue $audit)}
  if($role -and $Mutation -cne 'omit-role'){$manifest[$role+'EventId']=$eventId}
  if($capture){$manifest.captureKeys[$capture]=@{eventId=$eventId;role=$role}}
  if($Mutation -ceq 'publication-before-journal'){$State.effects.Add(@{operation='PublishEvent';eventId=$eventId})}
  $State.effects.Add(@{operation='PublishJournal';eventId=$eventId;manifest=(Copy-ManifestModelValue $manifest)})
  if($State.crashAfterJournal){return @{code='lost'}}
  $State.records.Add(@{event=(Copy-ManifestModelValue $audit);state='pending'})
  if($Mutation -cne 'publication-before-journal'){$State.effects.Add(@{operation='PublishEvent';eventId=$eventId})}
  return @{code='queued';event=(Copy-ManifestModelValue $audit)}
 }
 if($Operation -ceq 'Flush'){
  foreach($record in $State.records){
   if($record.state -cne 'pending'){continue}
   $receipt=Copy-ManifestModelValue $record.event
   $null=$receipt.Remove('kind');$null=$receipt.Remove('payloadJson');$receipt.code='persisted';$receipt.revision=[long]$receipt.order
   $record.receipt=$receipt;$record.state='archive';$State.server[$record.event.eventId]=Copy-ManifestModelValue $receipt
   $State.effects.Add(@{operation='Submit';eventId=$record.event.eventId})
  }
  return @{code='flushed'}
 }
 if($Operation -ceq 'CheckCapturedCompletion'){
  if(-not $State.manifests.ContainsKey($id)){return @{allowed=$false;code='session_turn_completion_gate';detailCode='expected_set_missing';checkedEventIds=@()}}
  $manifest=$State.manifests[$id]
  if($manifest.legacyIncomplete -or @($manifest.expectedEvents).Count -ne $State.orders[$id]){return @{allowed=$false;code='session_turn_completion_gate';detailCode='expected_set_missing';checkedEventIds=@()}}
  $authority=@{requestId=$id;expectedEvents=$manifest.expectedEvents;ingressEventId=$manifest.ingressEventId;resultEventId=$manifest.resultEventId;requiresResult=$InputObject.requiresResult}
  if($Mutation -ceq 'use-caller-manifest' -and $InputObject.ContainsKey('expectedEvents')){$authority.expectedEvents=$InputObject.expectedEvents}
  $stateForCompletion=$State
  $dependencies=@{
   ListRecord={param($RequestId) return @($stateForCompletion.records|Where-Object {$_.event.requestId -ceq $RequestId}|ForEach-Object {$_.event.eventId})}.GetNewClosure()
   ReadRecord={param($Path) return ConvertTo-Json -InputObject @($stateForCompletion.records|Where-Object {$_.event.eventId -ceq $Path})[0] -Depth 90 -Compress}.GetNewClosure()
   ReadServer={param($AuditRecord) return $stateForCompletion.server[$AuditRecord.eventId]}.GetNewClosure()
  }
  return & $CompletionChecker -Context $Context -InputObject $authority -Dependencies $dependencies
 }

 throw 'Unknown independent manifest operation'
}
