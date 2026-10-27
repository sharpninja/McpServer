#Requires -Version 7.5
# SPDX-License-Identifier: Apache-2.0
<#
.SYNOPSIS
Same scenario interactions/assertions for independent manifest model and canonical outbox.
#>
[CmdletBinding(SupportsShouldProcess)]
param([ValidateSet('Mock','Production')][string]$Mode='Mock',[Parameter(Mandatory)][string]$ReceiptPath,[scriptblock]$ProviderFactory)
$ErrorActionPreference='Stop'
if(Test-Path $ReceiptPath){throw 'Chronological receipt exists'}
Write-Output 'Counted effects: manifest capture/restart/alias/completion scenarios, independent negative controls and one full chronological receipt.'
if(-not $PSCmdlet.ShouldProcess($ReceiptPath,'Run manifest ownership and publication assertions')){return}
if($Mode -ceq 'Mock'){
 . (Join-Path $PSScriptRoot 'GovernedAuditCompletion.Model.ps1')
 . (Join-Path $PSScriptRoot 'GovernedAuditManifest.Model.ps1')
}elseif(-not $ProviderFactory){throw 'Canonical outbox provider required'}
$script:manifestAssertions=0
function Assert-ManifestContract {
 param([bool]$Condition,[string]$Message)
 $script:manifestAssertions++
 if(-not $Condition){throw ('MANIFEST_ASSERT: '+$Message)}
}
function Invoke-ManifestScenario {
 param([string]$Name,[string]$Mutation='')
 $context=@{workspacePath='F:/fixture/manifest';agentIdentity='Codex';mcpSessionId='Codex-20260101T000000Z-manifest';hostSessionKey='host-fixture'}
 $state=@{manifests=@{};orders=@{};aliases=@{};records=[Collections.Generic.List[object]]::new();effects=[Collections.Generic.List[object]]::new();server=@{};serial=0;crashAfterJournal=($Name -cin @('crash-after-journal','retry-interrupted-publication'))}
 if($Name -ceq 'legacy-reserved-order'){$state.orders['req-one']=1L}
 $modelCall=if($Mode -ceq 'Mock'){(Get-Command Invoke-MockAuditManifest).ScriptBlock}else{$null}
 $provider=if($Mode -ceq 'Production'){& $ProviderFactory $context $state}else{$null}
 $modeForInvocation=$Mode;$mutationForInvocation=$Mutation
 $completionForInvocation=if($Mode -ceq 'Mock'){(Get-Command Test-MockGovernedAuditCompletion).ScriptBlock}else{$null}
 $invoke={param($Operation,$InputData)
  if($modeForInvocation -ceq 'Mock'){return & $modelCall -State $state -Context $context -Operation $Operation -InputObject $InputData -Mutation $mutationForInvocation -CompletionChecker $completionForInvocation}
  return & $provider.Invoke $Operation $InputData
 }.GetNewClosure()
 $inspect={if($modeForInvocation -ceq 'Mock'){return $state}else{return & $provider.Inspect}}.GetNewClosure()
 $request=if($Name -cin @('pending-alias','pending-alias-restart')){'pending-fixture-nonce'}else{'req-one'}
 $text="  # Ingress **whole** $([char]0x03A9)`r`nline2  "
 $payload=@{text=$text}|ConvertTo-Json -Compress
 $firstInput=@{requestId=$request;kind='metadata';payloadJson=$payload;evidenceRole='ingress';captureKey='ingress:fixture-nonce'}
 if($Name -ceq 'unlabelled-ingress'){$firstInput.evidenceRole=''}
 if($Name -ceq 'invalid-role'){$firstInput.evidenceRole='invented'}
 if($Name -ceq 'empty-manifest'){
  $missing=& $invoke 'ReadManifest' @{requestId=$request}
  Assert-ManifestContract ($missing.code -ceq 'manifest_missing' -and $null -eq $missing.manifest) 'No invented manifest for empty storage'
  return @{name=$Name;observations=@($missing);effects=(& $inspect).effects}
 }
 $first=& $invoke 'Enqueue' $firstInput
 if($Name -ceq 'invalid-role'){
  Assert-ManifestContract ($first.code -ceq 'lost') 'Invalid evidence role refuses capture'
  Assert-ManifestContract (@((& $inspect).records).Count -eq 0) 'Invalid role publishes no event'
  return @{name=$Name;observations=@($first);effects=(& $inspect).effects}
 }
 if($Name -cin @('crash-after-journal','retry-interrupted-publication')){
  Assert-ManifestContract ($first.code -ceq 'lost') 'Interrupted capture is not queued success'
  $manifest=& $invoke 'ReadManifest' @{requestId=$request}
  Assert-ManifestContract (@($manifest.manifest.expectedEvents).Count -eq 1) 'Journal retains lost expected UUID'
  Assert-ManifestContract (@((& $inspect).records).Count -eq 0) 'Event was not published after journal crash'
  $check=& $invoke 'CheckCapturedCompletion' @{requestId=$request;requiresResult=$false}
  Assert-ManifestContract (-not $check.allowed) 'Zero pending records cannot hide expected missing event'
  if($Name -ceq 'retry-interrupted-publication'){
   $null=& $invoke 'Restart' @{}
   $retry=& $invoke 'Enqueue' $firstInput
   $after=& $invoke 'ReadManifest' @{requestId=$request}
   Assert-ManifestContract ($retry.code -ceq 'queued' -and $retry.event.eventId -ceq $manifest.manifest.expectedEvents[0].eventId -and $retry.event.order -eq 1 -and $retry.event.payloadJson -ceq $payload -and @($after.manifest.expectedEvents).Count -eq 1 -and @((& $inspect).records).Count -eq 1) 'Interrupted publication reuses durable UUID/order/fulltext and recreates one missing file'
  }
  return @{name=$Name;observations=@($first,$manifest,$check);effects=(& $inspect).effects}
 }
 Assert-ManifestContract ($first.code -ceq 'queued') 'Actual capture queued'
 Assert-ManifestContract ($first.event.payloadJson -ceq $payload -and $first.event.requestId -ceq $request) 'Complete payload and owning request preserved'
 $before=& $invoke 'ReadManifest' @{requestId=$request}
 Assert-ManifestContract ($before.code -ceq 'manifest' -and @($before.manifest.expectedEvents).Count -eq 1) 'Capture is in durable manifest'
 Assert-ManifestContract ($before.manifest.expectedEvents[0].eventId -ceq $first.event.eventId -and $before.manifest.expectedEvents[0].payloadJson -ceq $payload) 'UUID and exact payload derive from capture'
 Assert-ManifestContract (($before.manifest.ingressEventId -ceq $first.event.eventId) -eq ($Name -cne 'unlabelled-ingress')) 'Ingress role is explicit'
 $tape=@((& $inspect).effects)
 $journalIndex=[array]::FindIndex($tape,[Predicate[object]]{param($Item) $Item.operation -ceq 'PublishJournal'})
 $eventIndex=[array]::FindIndex($tape,[Predicate[object]]{param($Item) $Item.operation -ceq 'PublishEvent'})
 Assert-ManifestContract ($journalIndex -ge 0 -and $eventIndex -gt $journalIndex) 'Full journal publication precedes event publication'
 if($Name -ceq 'retry-identical'){
  $retry=& $invoke 'Enqueue' $firstInput
  $after=& $invoke 'ReadManifest' @{requestId=$request}
  Assert-ManifestContract ($retry.code -ceq 'queued' -and $retry.event.eventId -ceq $first.event.eventId -and @($after.manifest.expectedEvents).Count -eq 1 -and @((& $inspect).records).Count -eq 1) 'Identical capture key reuses one exact durable UUID/order/file'
 }elseif($Name -ceq 'retry-conflict'){
  $firstInput.payloadJson=@{text='changed'}|ConvertTo-Json -Compress
  $retry=& $invoke 'Enqueue' $firstInput
  $after=& $invoke 'ReadManifest' @{requestId=$request}
  Assert-ManifestContract ($retry.code -ceq 'lost' -and $after.manifest.expectedEvents[0].payloadJson -ceq $payload -and @($after.manifest.expectedEvents).Count -eq 1) 'Conflicting key never overwrites captured full text'
 }elseif($Name -ceq 'duplicate-ingress'){
  $firstInput.captureKey='another-ingress'
  $retry=& $invoke 'Enqueue' $firstInput
  Assert-ManifestContract ($retry.code -ceq 'lost' -and @((& $inspect).records).Count -eq 1) 'A second distinct ingress cannot replace first identity'
 }elseif($Name -ceq 'restart'){
  $null=& $invoke 'Restart' @{}
  $after=& $invoke 'ReadManifest' @{requestId=$request}
  Assert-ManifestContract ($after.code -ceq 'manifest' -and $after.manifest.expectedEvents[0].eventId -ceq $first.event.eventId -and $after.manifest.expectedEvents[0].payloadJson -ceq $payload) 'Restart reads original durable full envelope'
 }elseif($Name -cin @('pending-alias','pending-alias-restart')){
  $alias=@{workspacePath=$context.workspacePath;agent=$context.agentIdentity;mcpSessionId=$context.mcpSessionId;hostSessionKey=$context.hostSessionKey;ingressNonce='fixture-nonce';pendingId=$request;requestId='req-minted';aliasedAtUtc='2026-01-01T00:00:01Z';acks=@()}
  $applied=& $invoke 'ApplyAlias' $alias
  if($Name -ceq 'pending-alias-restart'){$null=& $invoke 'Restart' @{}}
  $old=& $invoke 'ReadManifest' @{requestId=$request};$new=& $invoke 'ReadManifest' @{requestId='req-minted'}
  Assert-ManifestContract ($applied.code -ceq 'applied' -and $old.code -ceq 'manifest' -and $new.code -ceq 'manifest') 'Both pending/minted IDs resolve after durable alias/restart'
  $rebound=$new.manifest.expectedEvents[0]
  Assert-ManifestContract ($rebound.requestId -ceq 'req-minted' -and $rebound.eventId -ceq $first.event.eventId -and $rebound.order -eq $first.event.order -and $rebound.payloadJson -ceq $payload -and $rebound.payloadHash -ceq $first.event.payloadHash) 'Only request ID changes in expected envelope'
 }elseif($Name -ceq 'request-isolation'){
  $other=@{requestId='req-other';kind='metadata';payloadJson=$payload;evidenceRole='ingress';captureKey='other'}
  $second=& $invoke 'Enqueue' $other
  $original=& $invoke 'ReadManifest' @{requestId=$request};$separate=& $invoke 'ReadManifest' @{requestId='req-other'}
  Assert-ManifestContract ($second.event.order -eq 1 -and @($original.manifest.expectedEvents).Count -eq 1 -and @($separate.manifest.expectedEvents).Count -eq 1 -and $second.event.eventId -cne $first.event.eventId) 'Each logical owning turn has independent sequence/manifest'
 }elseif($Name -cne 'capture-ingress'){
  $secondInput=@{requestId=$request;kind='metadata';payloadJson=(@{text="  # Full final`r`n**result**  "}|ConvertTo-Json -Compress);evidenceRole='result';captureKey='result:fixture'}
  if($Name -ceq 'equal-payload-distinct-captures'){$secondInput.payloadJson=$payload;$secondInput.evidenceRole=''}
  $second=& $invoke 'Enqueue' $secondInput
  $after=& $invoke 'ReadManifest' @{requestId=$request}
  Assert-ManifestContract ($second.code -ceq 'queued' -and $second.event.order -eq $first.event.order+1 -and $second.event.eventId -cne $first.event.eventId -and @($after.manifest.expectedEvents).Count -eq 2) 'Distinct capture reserves next order and independent UUID in manifest'
  Assert-ManifestContract ($after.manifest.expectedEvents[1].payloadJson -ceq $secondInput.payloadJson) 'Complete final payload retained'
  if($Name -ceq 'duplicate-result'){
   $secondInput.captureKey='other-result';$duplicate=& $invoke 'Enqueue' $secondInput
   Assert-ManifestContract ($duplicate.code -ceq 'lost') 'Second distinct final cannot replace first result identity'
  }elseif($Name -cne 'equal-payload-distinct-captures'){
   Assert-ManifestContract ($after.manifest.resultEventId -ceq $second.event.eventId) 'Result identity captured explicitly'
   if($Name -cne 'pending-blocks'){$null=& $invoke 'Flush' @{requestId=$request}}
   $checkInput=@{requestId=$request;requiresResult=$true}
   if($Name -ceq 'ignore-empty-caller-set'){$checkInput.expectedEvents=@()}
   if($Name -ceq 'ignore-forged-caller-set'){$checkInput.expectedEvents=@(@{text='forged summary'})}
   $check=& $invoke 'CheckCapturedCompletion' $checkInput
   $shouldAllow=$Name -cnotin @('pending-blocks','legacy-reserved-order','unlabelled-ingress')
   Assert-ManifestContract ($check.allowed -eq $shouldAllow) 'Completion uses captured authoritative manifest and actual retained evidence'
   if($shouldAllow){Assert-ManifestContract ((@($check.checkedEventIds) -join '|') -ceq ($first.event.eventId+'|'+$second.event.eventId)) 'Every actual captured identity independently verified'}
  }
 }
 return @{name=$Name;first=$first;manifest=$before;effects=(& $inspect).effects}
}
$names=@('empty-manifest','capture-ingress','capture-result','retry-identical','retry-conflict','duplicate-ingress','duplicate-result','equal-payload-distinct-captures','restart','crash-after-journal','legacy-reserved-order','pending-alias','pending-alias-restart','request-isolation','unlabelled-ingress','invalid-role','pending-blocks','ignore-empty-caller-set','ignore-forged-caller-set')
$names+=@('retry-interrupted-publication')
$results=[Collections.Generic.List[object]]::new();$failures=[Collections.Generic.List[object]]::new();$controls=[Collections.Generic.List[object]]::new()
foreach($name in $names){try{$results.Add((Invoke-ManifestScenario $name))}catch{$failures.Add(@{case=$name;error=$_.Exception.ToString()})}}
if($Mode -ceq 'Mock'){
 foreach($fault in @(@('retry-interrupted-publication','skip-recreate'),@('capture-ingress','drop-journal'),@('capture-ingress','omit-role'),@('capture-ingress','change-text'),@('capture-result','reset-order'),@('capture-result','duplicate-uuid'),@('capture-ingress','publication-before-journal'),@('retry-identical','retry-duplicates'),@('restart','forget-restart'),@('pending-alias','forget-alias'),@('ignore-empty-caller-set','use-caller-manifest'),@('ignore-forged-caller-set','use-caller-manifest'))){
  try{$null=Invoke-ManifestScenario $fault[0] $fault[1];$failures.Add(@{control=$fault;error='Wrong interaction escaped assertions'})}catch{if($_.Exception.Message.StartsWith('MANIFEST_ASSERT:',[StringComparison]::Ordinal)){$controls.Add(@{case=$fault[0];mutation=$fault[1];rejection=$_.Exception.Message})}else{$failures.Add(@{control=$fault;error=$_.Exception.ToString()})}}
 }
}
$receipt=[ordered]@{timestamp=Get-Date -Format o;mode=$Mode;plannedCases=$names.Count;cases=$results.Count;assertions=$script:manifestAssertions;rejectingControls=$controls.Count;failed=$failures.Count;skipped=0;results=$results.ToArray();controls=$controls.ToArray();failures=$failures.ToArray();sourceHashes=@(Get-FileHash $PSCommandPath,(Join-Path $PSScriptRoot 'GovernedAuditManifest.Model.ps1'),(Join-Path $PSScriptRoot 'GovernedAuditCompletion.Model.ps1')|Select-Object Path,Hash);productComplete=$false}
Import-Module PowerShell.MCP -ErrorAction Stop
$var1=ConvertTo-Json $receipt -Depth 100
Add-LinesToFile $ReceiptPath -Content $var1|Out-Null
[pscustomobject]@{cases=$results.Count;planned=$names.Count;assertions=$script:manifestAssertions;controls=$controls.Count;failed=$failures.Count;receipt=$ReceiptPath}
if($failures.Count){throw 'Manifest prerequisite failed; chronology retained'}
