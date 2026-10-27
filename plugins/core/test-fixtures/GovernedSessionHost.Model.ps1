# SPDX-License-Identifier: Apache-2.0
<#
.SYNOPSIS
Independent host composition model using test-only A8/outbox models and scripted transport.
.DESCRIPTION
Accepts actual operation inputs and native model state, never scenario names or
expected summaries. Production lifecycle/transport code is never imported.
#>
function Invoke-MockGovernedSessionHost {
 [CmdletBinding()]
 param([hashtable]$State,[hashtable]$HostContext,[string]$Operation,[hashtable]$InputObject,[scriptblock]$Server,[string]$Mutation='')
 . (Join-Path $PSScriptRoot 'GovernedTurnBinding.Model.ps1')
 . (Join-Path $PSScriptRoot 'GovernedTurnStore.Model.ps1')
 . (Join-Path $PSScriptRoot 'GovernedAuditManifest.Model.ps1')
 . (Join-Path $PSScriptRoot 'GovernedAuditCompletion.Model.ps1')
 function Get-HostModelResult {
  param([string]$Code,[bool]$Ok=$false,[object]$Binding=$null,[object]$AuditRecord=$null)
  return @{ok=$Ok;code=$Code;binding=$Binding;event=$AuditRecord}
 }
 function Copy-HostModelValue {
  param([object]$Value)
  return ,(ConvertFrom-Json (ConvertTo-Json -InputObject $Value -Depth 90 -Compress) -AsHashtable -DateKind String -NoEnumerate)
 }
 function Get-HostModelNormalizedJson {
  param([string]$Json)
  # Scripted mock normalizer covers only the fixed-key host scenario inputs;
  # the separate S6 fidelity contract owns arbitrary property/number coverage.
  return $Json.Replace([char]0x2013,[char]0x002D).Replace([char]0x2014,[char]0x002D).Replace('\u2013','-').Replace('\u2014','-')
 }
 function Get-HostModelOutbox {
  param([Collections.IDictionary]$Binding)
  $key=ConvertTo-Json -InputObject @($Binding.mcpSessionId,$Binding.hostSessionKey) -Compress
  if(-not $State.outboxes.ContainsKey($key)){$State.outboxes[$key]=@{manifests=@{};orders=@{};aliases=@{};records=[Collections.Generic.List[object]]::new();effects=[Collections.Generic.List[object]]::new();server=@{};serial=0;crashAfterJournal=($State.ContainsKey('crashAfterJournal') -and $State.crashAfterJournal)}}
  return $State.outboxes[$key]
 }
 function Invoke-HostModelCapture {
  param([Collections.IDictionary]$Binding,[string]$Kind,[string]$Payload,[string]$Key,[string]$Role='')
  if($Mutation -ceq 'duplicate-decision' -and $Kind -ceq 'decision'){$Key+=':'+[guid]::NewGuid().ToString('N')}
  $box=Get-HostModelOutbox $Binding
  $captured=Invoke-MockAuditManifest -State $box -Context $Binding -Operation Enqueue -InputObject @{requestId=$Binding.requestId;kind=$Kind;payloadJson=(Get-HostModelNormalizedJson $Payload);captureKey=$Key;evidenceRole=$Role}
  if($captured.code -ceq 'queued' -and $State.ContainsKey('observer')){& $State.observer 'CaptureEvent' $Binding}
  return $captured
 }
 function Test-HostModelReceipt {
  param([object]$Receipt,[Collections.IDictionary]$AuditRecord)
  if($Receipt -isnot [Collections.IDictionary] -or $Receipt.code -cne 'persisted' -or $Receipt.revision -isnot [ValueType] -or $Receipt.revision -lt 1){return $false}
  foreach($field in @('workspacePath','agentIdentity','mcpSessionId','requestId','eventId','order','payloadHash')){if($Receipt[$field] -cne $AuditRecord[$field]){return $false}}
  return $true
 }
 function Test-HostModelFlush {
  param([Collections.IDictionary]$Binding)
  $box=Get-HostModelOutbox $Binding
  foreach($record in $box.records){
   if($record.event.requestId -cne $Binding.requestId -or $record.state -cne 'pending'){continue}
   $reply=if($record.receipt){& $Server 'ReadAudit' $Binding $record.event}else{& $Server 'Submit' $Binding $record.event}
   if($Mutation -cne 'ignore-submit-receipt' -and -not (Test-HostModelReceipt $reply $record.event)){return $false}
   $record.receipt=$reply;$box.server[$record.event.eventId]=$reply
   if($State.ContainsKey('observer')){& $State.observer 'ReceiptPublished' $Binding}
   $record.state='archive'
   if($State.ContainsKey('observer')){& $State.observer 'Retired' $Binding}
  }
  return $true
 }
 function Test-HostModelAudit {
  param([Collections.IDictionary]$Binding,[bool]$RequiresResult)
  $box=Get-HostModelOutbox $Binding
  $manifest=(Invoke-MockAuditManifest -State $box -Context $Binding -Operation ReadManifest -InputObject @{requestId=$Binding.requestId}).manifest
  if(-not $manifest){return $false}
  $boxForRead=$box;$serverForRead=$Server;$bindingForRead=$Binding
  $dependencies=@{
   ListRecord={param($RequestId) return @($boxForRead.records|Where-Object {$_.event.requestId -ceq $RequestId}|ForEach-Object {$_.event.eventId})}.GetNewClosure()
   ReadRecord={param($Id) return ConvertTo-Json -InputObject @($boxForRead.records|Where-Object {$_.event.eventId -ceq $Id})[0] -Depth 90 -Compress}.GetNewClosure()
   ReadServer={param($AuditRecord) return & $serverForRead 'ReadAudit' $bindingForRead $AuditRecord}.GetNewClosure()
  }
  $result=Test-MockGovernedAuditCompletion -Context $Binding -InputObject @{requestId=$Binding.requestId;expectedEvents=$manifest.expectedEvents;ingressEventId=$manifest.ingressEventId;resultEventId=$manifest.resultEventId;requiresResult=$RequiresResult} -Dependencies $dependencies
  return $result.allowed
 }
 function Invoke-HostModelAliasReconcile {
  if($Mutation -ceq 'omit-alias-consume'){return}
  $listing=Invoke-MockGovernedTurnStore -Directory $State.storeDirectory -Context $owner -Operation ListAliases -InputObject @{hostSessionKey=$binding.hostSessionKey;consumerId='qb-audit-outbox-v1'}
  if(-not $listing.ok){throw $listing.code}
  foreach($pendingAlias in $listing.aliases){
   $outboxAlias=@{workspacePath=$pendingAlias.workspacePath;agent=$pendingAlias.agentIdentity;mcpSessionId=$pendingAlias.mcpSessionId;hostSessionKey=$pendingAlias.hostSessionKey;ingressNonce=$pendingAlias.ingressNonce;pendingId=$pendingAlias.pendingId;requestId=$pendingAlias.requestId;aliasedAtUtc=$pendingAlias.aliasedAtUtc;acks=@()}
   $applied=Invoke-MockAuditManifest -State (Get-HostModelOutbox $pendingAlias) -Context $pendingAlias -Operation ApplyAlias -InputObject $outboxAlias
   if($applied.code -cne 'applied'){throw 'alias_reconciliation_failed'}
   if($State.ContainsKey('observer')){& $State.observer 'AliasApplied' $pendingAlias}
   $null=Invoke-MockGovernedTurnStore -Directory $State.storeDirectory -Context $owner -Operation Acknowledge -InputObject @{hostSessionKey=$pendingAlias.hostSessionKey;mcpSessionId=$pendingAlias.mcpSessionId;pendingId=$pendingAlias.pendingId;requestId=$pendingAlias.requestId;consumerId='qb-audit-outbox-v1'}
  }
 }
 $owner=@{workspacePath=$HostContext.workspacePath;agentIdentity=$HostContext.agentIdentity;aliasConsumers=@('qb-audit-outbox-v1')}
 if($Operation -cin @('mintTurn','beginTurn')){
  if($HostContext.actualIngress -ne $true){return Get-HostModelResult 'mint_requires_host_ingress'}
  foreach($field in @('workspacePath','agentIdentity','mcpSessionId','hostSessionKey','keySource','ingressNonce','boundAtUtc')){if([string]::IsNullOrWhiteSpace($HostContext[$field])){return Get-HostModelResult 'nonce_source_missing'}}
  if($HostContext.ingressText -isnot [string]){return Get-HostModelResult 'ingress_nonce_invalid'}
  $binding=@{};foreach($field in @('workspacePath','agentIdentity','mcpSessionId','hostSessionKey','keySource','ingressNonce','boundAtUtc')){$binding[$field]=$HostContext[$field]};$binding.requestId='pending-'+$HostContext.ingressNonce
  $registered=Invoke-MockGovernedTurnStore -Directory $State.storeDirectory -Context $owner -Operation Register -InputObject $binding
  if(-not $registered.ok){return Get-HostModelResult $registered.code}
  $stored=@($registered.state.hosts|Where-Object hostSessionKey -CEQ $binding.hostSessionKey|ForEach-Object {$_.turns}|Where-Object ingressNonce -CEQ $binding.ingressNonce)[0]
  $binding.requestId=$stored.requestId
  if(-not $binding.requestId.StartsWith('pending-',[StringComparison]::Ordinal)){Invoke-HostModelAliasReconcile}
  $ingressJson=@{queryText=$HostContext.ingressText;requestClass=$HostContext.requestClass}|ConvertTo-Json -Compress
  $capture=Invoke-HostModelCapture $binding 'metadata' $ingressJson ('ingress:'+$binding.ingressNonce) 'ingress'
  if($capture.code -cne 'queued'){return Get-HostModelResult 'lost'}
  if($binding.requestId.StartsWith('pending-',[StringComparison]::Ordinal)){
   $pending=$binding.requestId;$State.nextRequest++
   $binding.requestId='req-20260101T000000Z-host-'+$State.nextRequest
   $alias=Copy-HostModelValue $binding;$alias.pendingId=$pending;$alias.aliasedAtUtc=$HostContext.boundAtUtc
   $committed=Invoke-MockGovernedTurnStore -Directory $State.storeDirectory -Context $owner -Operation Alias -InputObject $alias
   if(-not $committed.ok){return Get-HostModelResult $committed.code}
   if($State.ContainsKey('observer')){& $State.observer 'AliasCommitted' $binding}
  }
  Invoke-HostModelAliasReconcile
  $normalizedIngress=(ConvertFrom-Json (Get-HostModelNormalizedJson $ingressJson) -AsHashtable).queryText
  $title=$InputObject.queryTitle
  if([string]::IsNullOrWhiteSpace($title) -or $title -ceq 'User prompt'){$title='Request '+$binding.ingressNonce}
  $null=& $Server 'Begin' $binding @{queryText=$normalizedIngress;queryTitle=$title}
  $read=& $Server 'ReadTurn' $binding @{}
  if($Mutation -cne 'ignore-ingress-readback' -and ($null -eq $read -or $read.queryText -cne $normalizedIngress)){return Get-HostModelResult 'ingress_readback_mismatch'}
  if(-not (Test-HostModelFlush $binding)){return Get-HostModelResult 'session_turn_completion_gate'}
  if($Operation -ceq 'beginTurn'){$delivered=Invoke-MockGovernedTurnStore -Directory $State.storeDirectory -Context $owner -Operation Deliver -InputObject @{hostSessionKey=$binding.hostSessionKey;requestId=$binding.requestId};if(-not $delivered.ok){return Get-HostModelResult $delivered.code}}
  return Get-HostModelResult 'bound' $true $binding $capture.event
 }
 $selection=@{hostSessionKey=$HostContext.hostSessionKey}
 if($InputObject.ContainsKey('requestId') -and $Mutation -cne 'target-latest'){$selection.requestId=$InputObject.requestId}
 $resolved=Invoke-MockGovernedTurnStore -Directory $State.storeDirectory -Context $owner -Operation Resolve -InputObject $selection
 if(-not $resolved.ok){return Get-HostModelResult $resolved.code}
 $binding=Copy-HostModelValue $resolved.binding
 $turn=@($resolved.state.hosts|Where-Object hostSessionKey -CEQ $binding.hostSessionKey|ForEach-Object {$_.turns}|Where-Object requestId -CEQ $binding.requestId)[0]
 if(-not $turn.delivered -and $Mutation -cne 'skip-delivery-guard'){return Get-HostModelResult 'request_unknown'}
 if($Operation -ceq 'resolve'){return Get-HostModelResult 'resolved' $true $binding}
 if($Operation -cin @('appendAction','appendDialog','decision','recordReview')){
  $kind=switch -CaseSensitive ($Operation){'appendAction'{'action'};'appendDialog'{'dialog'};'decision'{'decision'};'recordReview'{'metadata'}}
  $payload=$InputObject.payloadJson
  if($Operation -ceq 'recordReview'){
   $document=[System.Text.Json.JsonDocument]::Parse($payload)
   try{
    $body=$document.RootElement
    if($body.GetProperty('reviewType').GetString() -ceq 'Other' -and [string]::IsNullOrWhiteSpace($body.GetProperty('otherNotes').GetString())){return Get-HostModelResult 'review_invalid'}
    if($body.GetProperty('OverallVerdict').GetString() -ceq 'AGREE'){
     foreach($field in @('accuracy','completeness')){
      $score=$body.GetProperty($field);$raw=$score.GetRawText();$integer=$raw.Split('.')[0]
      if($score.ValueKind -ne [System.Text.Json.JsonValueKind]::Number -or $raw -cnotmatch '^(0|[1-9][0-9]*)(?:\.[0-9]+)?$' -or $integer.Length -lt 2 -or ($integer.Length -eq 2 -and [string]::CompareOrdinal($integer,'98') -lt 0)){return Get-HostModelResult 'review_invalid'}
     }
    }
   }catch{return Get-HostModelResult 'review_invalid'}finally{$document.Dispose()}
  }
  $key=$Operation+':'+$HostContext.operationId
  if($InputObject.ContainsKey('batchIndex')){
   # The independent model uses a different representation from production.
   $slot=ConvertTo-Json @{call=$HostContext.operationId;slot=$InputObject.batchIndex} -Compress
   $key='BatchCapture/'+$Operation+'/'+[Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($slot))
  }
  $capture=Invoke-HostModelCapture $binding $kind $payload $key
  if($capture.code -cne 'queued'){return Get-HostModelResult 'lost'}
  if(-not (Test-HostModelFlush $binding)){return Get-HostModelResult 'session_turn_completion_gate'}
  return Get-HostModelResult 'persisted' $true $binding $capture.event
 }
 if($Operation -cin @('completeTurn','failTurn')){
  if($State.policy -cne 'allow' -and $Mutation -cne 'omit-completion-gate'){return Get-HostModelResult $State.policy}
  $complete=$Operation -ceq 'completeTurn'
  $payload=if($complete){@{response=$InputObject.response}|ConvertTo-Json -Compress}else{@{failureNote=$InputObject.failureNote}|ConvertTo-Json -Compress}
  $capture=Invoke-HostModelCapture $binding 'metadata' $payload ('terminal:'+$HostContext.operationId) $(if($complete){'result'}else{''})
  if($capture.code -cne 'queued' -or -not (Test-HostModelFlush $binding) -or -not (Test-HostModelAudit $binding $complete)){return Get-HostModelResult 'session_turn_completion_gate'}
  $normalized=ConvertFrom-Json (Get-HostModelNormalizedJson $payload) -AsHashtable
  $read=& $Server 'ReadTurn' $binding @{}
  if($read.status -ceq 'in_progress' -or $Mutation -ceq 'repeat-terminal'){
   try{$null=& $Server $(if($complete){'Complete'}else{'Fail'}) $binding $normalized}catch{[void]$_.Exception}
   $read=& $Server 'ReadTurn' $binding @{}
  }
  $expectedStatus=if($complete){'completed'}else{'failed'}
  if($Mutation -cne 'skip-final-readback'){
   if($read.status -cne $expectedStatus -or ($complete -and $read.response -cne $normalized.response) -or (-not $complete -and $read.failureNote -cne $normalized.failureNote)){return Get-HostModelResult 'final_readback_mismatch'}
   foreach($field in @('workspacePath','agentIdentity','mcpSessionId','requestId')){if($read[$field] -cne $binding[$field]){return Get-HostModelResult 'final_readback_mismatch'}}
  }
  if($State.ContainsKey('observer')){& $State.observer 'TerminalRead' $binding}
  $State.effects.Add(@{operation='Terminal';requestId=$binding.requestId})
  $terminal=Invoke-MockGovernedTurnStore -Directory $State.storeDirectory -Context $owner -Operation Terminal -InputObject @{hostSessionKey=$binding.hostSessionKey;requestId=$binding.requestId}
  return Get-HostModelResult $expectedStatus $terminal.ok $binding $capture.event
 }
 return Get-HostModelResult 'turn_logging_unsupported'
}
