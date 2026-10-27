#Requires -Version 7.5
# SPDX-License-Identifier: Apache-2.0
[CmdletBinding(SupportsShouldProcess)]
param([ValidateSet('Mock','Production')][string]$Mode='Mock',[Parameter(Mandatory)][string]$ReceiptPath,[scriptblock]$ProviderFactory)
$ErrorActionPreference='Stop'
if(Test-Path $ReceiptPath){throw 'Chronological receipt exists'}
Write-Output ('Counted effects: '+$Mode+' expected-set scenarios and dependency interaction evidence, mock rejecting controls, one full receipt. No phase acceptance.')
if(-not $PSCmdlet.ShouldProcess($ReceiptPath,'Run requirement-derived expected-set assertions')){return}
if($Mode -ceq 'Mock'){. (Join-Path $PSScriptRoot 'GovernedAuditCompletion.Model.ps1')}
elseif(-not $ProviderFactory){throw 'Actual canonical outbox provider required; no production fallback'}
function Copy-CompletionValue {
 param([object]$Value)
 return ,(ConvertFrom-Json (ConvertTo-Json -InputObject $Value -Depth 90 -Compress) -AsHashtable -DateKind String -NoEnumerate)
}
function Get-CompletionSeed {
 $context=@{workspacePath='F:/fixture/qb';agentIdentity='Codex';mcpSessionId='Codex-20260101T000000Z-completion'}
 $request='req-20260101T000001Z-completion'
 $events=@(for($index=1;$index -le 2;$index++){
  $payload=@{text="  # Full **text** $([char]0x03A9)`r`nline2  ";section=if($index -eq 1){'ingress'}else{'result'}}|ConvertTo-Json -Compress
  @{workspacePath=$context.workspacePath;agentIdentity=$context.agentIdentity;mcpSessionId=$context.mcpSessionId;requestId=$request;eventId=('00000000-0000-4000-8000-'+$index.ToString('000000000000'));order=$index;kind='metadata';payloadJson=$payload;payloadHash=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($payload))).ToLowerInvariant()}
 })
 $records=@{};$server=@{}
 for($index=0;$index -lt $events.Count;$index++){
  $receipt=Copy-CompletionValue $events[$index];$null=$receipt.Remove('kind');$null=$receipt.Remove('payloadJson');$receipt.code='persisted';$receipt.revision=$index+1
  $records['record'+$index]=@{state='archive';event=(Copy-CompletionValue $events[$index]);receipt=$receipt}
  $server[$events[$index].eventId]=Copy-CompletionValue $receipt
 }
 return @{context=$context;input=@{requestId=$request;expectedEvents=$events;ingressEventId=$events[0].eventId;resultEventId=$events[1].eventId;requiresResult=$true};recordIds=@('record0','record1');records=$records;server=$server;throwServer=$false}
}
$cases=[Collections.Generic.List[object]]::new()
$cases.Add(@{name='complete-full-text';allowed=$true;detail='';mutate={param($Seed) [void]$Seed}})
$cases.Add(@{name='cancel-without-invented-result';allowed=$true;detail='';mutate={param($Seed) $Seed.input.requiresResult=$false;$Seed.input.resultEventId='';$Seed.input.expectedEvents=@($Seed.input.expectedEvents[0]);$Seed.recordIds=@('record0')}})
$cases.Add(@{name='empty-expected-zero-queue';allowed=$false;detail='expected_set_missing';mutate={param($Seed) $Seed.input.expectedEvents=@();$Seed.recordIds=@()}})
$cases.Add(@{name='missing-ingress-identity';allowed=$false;detail='expected_set_missing';mutate={param($Seed) $Seed.input.ingressEventId=''}})
$cases.Add(@{name='missing-final-identity';allowed=$false;detail='expected_set_missing';mutate={param($Seed) $Seed.input.resultEventId=''}})
$cases.Add(@{name='same-id-ingress-result';allowed=$false;detail='expected_set_invalid';mutate={param($Seed) $Seed.input.resultEventId=$Seed.input.ingressEventId}})
$cases.Add(@{name='reordered-expected';allowed=$false;detail='expected_set_invalid';mutate={param($Seed) $Seed.input.expectedEvents=@($Seed.input.expectedEvents[1],$Seed.input.expectedEvents[0])}})
$cases.Add(@{name='duplicate-expected-uuid';allowed=$false;detail='expected_set_invalid';mutate={param($Seed) $Seed.input.expectedEvents[1].eventId=$Seed.input.expectedEvents[0].eventId}})
$cases.Add(@{name='missing-file-zero-pending';allowed=$false;detail='retained_set_mismatch';mutate={param($Seed) $Seed.recordIds=@('record0')}})
$cases.Add(@{name='same-count-wrong-uuid';allowed=$false;detail='retained_set_mismatch';mutate={param($Seed) $Seed.records.record1.event.eventId='00000000-0000-4000-8000-999999999999'}})
$cases.Add(@{name='duplicate-retained-event';allowed=$false;detail='retained_set_mismatch';mutate={param($Seed) $Seed.records.record1=Copy-CompletionValue $Seed.records.record0}})
$cases.Add(@{name='extra-retained-event';allowed=$false;detail='retained_set_mismatch';mutate={param($Seed) $Seed.records.record2=Copy-CompletionValue $Seed.records.record1;$Seed.recordIds+=@('record2')}})
$cases.Add(@{name='summary-replaces-full-text';allowed=$false;detail='retained_event_mismatch';mutate={param($Seed) $Seed.records.record1.event.payloadJson=@{text='summary'}|ConvertTo-Json -Compress;$Seed.records.record1.event.payloadHash=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($Seed.records.record1.event.payloadJson))).ToLowerInvariant()}})
$cases.Add(@{name='wrong-retained-order';allowed=$false;detail='retained_event_mismatch';mutate={param($Seed) $Seed.records.record1.event.order=1}})
$cases.Add(@{name='wrong-retained-root';allowed=$false;detail='retained_event_mismatch';mutate={param($Seed) $Seed.records.record1.event.workspacePath='F:/fixture/another'}})
$cases.Add(@{name='invalid-payload-hash';allowed=$false;detail='retained_event_mismatch';mutate={param($Seed) $Seed.input.expectedEvents[1].payloadHash=('0'*64);$Seed.records.record1.event.payloadHash=('0'*64);$Seed.records.record1.receipt.payloadHash=('0'*64);$Seed.server[$Seed.input.expectedEvents[1].eventId].payloadHash=('0'*64)}})
foreach($folder in @('pending','quarantine','rejected')){
 $folderForCase=$folder
 $cases.Add(@{name=('mandatory-'+$folder);allowed=$false;detail='mandatory_evidence_blocked';mutate={param($Seed) $Seed.records.record1.state=$folderForCase}.GetNewClosure()})
}
$cases.Add(@{name='missing-retained-receipt';allowed=$false;detail='retained_receipt_mismatch';mutate={param($Seed) $Seed.records.record1.receipt=$null}})
$cases.Add(@{name='zero-retained-revision';allowed=$false;detail='retained_receipt_mismatch';mutate={param($Seed) $Seed.records.record1.receipt.revision=0}})
foreach($field in @('workspacePath','agentIdentity','mcpSessionId','requestId','eventId','payloadHash','order','revision','code')){
 $fieldForCase=$field
 $cases.Add(@{name=('wrong-server-'+$field);allowed=$false;detail='server_readback_mismatch';mutate={param($Seed) $reply=$Seed.server[$Seed.input.expectedEvents[1].eventId];$reply[$fieldForCase]=if($fieldForCase -cin @('order','revision')){99}else{'wrong'}}.GetNewClosure()})
}
$cases.Add(@{name='missing-server-receipt';allowed=$false;detail='server_readback_mismatch';mutate={param($Seed) $Seed.server[$Seed.input.expectedEvents[1].eventId]=$null}})
$cases.Add(@{name='server-read-exception';allowed=$false;detail='evidence_unavailable';mutate={param($Seed) $Seed.throwServer=$true}})
function Invoke-CompletionMockProvider {
 param([hashtable]$Seed,[string]$Mutation)
 $effects=[Collections.Generic.List[object]]::new()
 $seedForIo=$Seed
  $copyForIo=(Get-Command Copy-CompletionValue).ScriptBlock
 $dependencies=@{
  ListRecord={param($RequestId) $effects.Add(@{operation='ListRecord';requestId=$RequestId});if($RequestId -cne $seedForIo.input.requestId){throw 'Wrong enumeration owner'};return @($seedForIo.recordIds)}.GetNewClosure()
  ReadRecord={param($Path) $effects.Add(@{operation='ReadRecord';path=$Path});return ConvertTo-Json $seedForIo.records[$Path] -Depth 90 -Compress}.GetNewClosure()
  ReadServer={param($AuditRecord) $effects.Add(@{operation='ReadServer';event=(& $copyForIo $AuditRecord)});if($seedForIo.throwServer){throw 'scripted independent read failure'};return & $copyForIo $seedForIo.server[$AuditRecord.eventId]}.GetNewClosure()
 }
 $result=Test-MockGovernedAuditCompletion -Context $Seed.context -InputObject $Seed.input -Dependencies $dependencies -Mutation $Mutation
 return @{result=$result;effects=$effects.ToArray()}
}
$assertions=0
function Assert-CompletionContract {
 param([bool]$Condition,[string]$Message)
 $script:assertions++
 if(-not $Condition){throw ('COMPLETION_ASSERT: '+$Message)}
}
function Invoke-CompletionScenario {
 param([Collections.IDictionary]$Scenario,[string]$Mutation='')
 $seed=Get-CompletionSeed
 & $Scenario.mutate $seed
 $before=ConvertTo-Json $seed.input -Depth 90 -Compress
 $observed=if($Mode -ceq 'Mock'){Invoke-CompletionMockProvider $seed $Mutation}else{& $ProviderFactory $seed}
 $script:lastCompletionObservation=$observed
 $result=$observed.result
 Assert-CompletionContract ($result.allowed -is [bool] -and $result.allowed -eq $Scenario.allowed) ($Scenario.name+': required allowed/refused result')
 Assert-CompletionContract ($result.code -ceq $(if($Scenario.allowed){'verified'}else{'session_turn_completion_gate'})) ($Scenario.name+': final classification')
 Assert-CompletionContract ($result.detailCode -ceq $Scenario.detail) ($Scenario.name+': exact refusal path')
 Assert-CompletionContract ((ConvertTo-Json $seed.input -Depth 90 -Compress) -ceq $before) ($Scenario.name+': captured manifest immutable')
 if($Scenario.allowed){
  $expectedIds=@($seed.input.expectedEvents.eventId)
  $readIds=@($observed.effects|Where-Object operation -CEQ 'ReadServer'|ForEach-Object {$_.event.eventId})
  Assert-CompletionContract (($readIds -join '|') -ceq ($expectedIds -join '|')) ($Scenario.name+': independent server read of EVERY event in original order')
  Assert-CompletionContract ((@($result.checkedEventIds) -join '|') -ceq ($expectedIds -join '|')) ($Scenario.name+': checked identities exact')
  Assert-CompletionContract (@($observed.effects|Where-Object operation -CEQ 'ListRecord').Count -eq 1) ($Scenario.name+': actual enumeration interaction')
  Assert-CompletionContract (@($observed.effects|Where-Object operation -CEQ 'ReadRecord').Count -eq $expectedIds.Count) ($Scenario.name+': actual storage read per expected event')
 }
 return @{name=$Scenario.name;input=(Copy-CompletionValue $seed.input);result=$result;effects=$observed.effects}
}
$results=[Collections.Generic.List[object]]::new();$controls=[Collections.Generic.List[object]]::new();$failures=[Collections.Generic.List[object]]::new()
foreach($case in $cases){$script:lastCompletionObservation=$null;try{$results.Add((Invoke-CompletionScenario $case))}catch{$failures.Add(@{case=$case.name;message=$_.Exception.ToString();observation=$script:lastCompletionObservation})}}
if($Mode -ceq 'Mock'){
 $faults=@(@{case='complete-full-text';fault='skip-storage'},@{case='complete-full-text';fault='skip-readback'},@{case='summary-replaces-full-text';fault='ignore-payload'},@{case='invalid-payload-hash';fault='ignore-hash'},@{case='mandatory-pending';fault='ignore-blocker'},@{case='mandatory-quarantine';fault='ignore-blocker'},@{case='mandatory-rejected';fault='ignore-blocker'},@{case='missing-file-zero-pending';fault='ignore-set'})
 foreach($field in @('workspacePath','agentIdentity','mcpSessionId','requestId','eventId','payloadHash','order','revision','code')){$faults+=@{case=('wrong-server-'+$field);fault='ignore-readback'}}
 foreach($fault in $faults){
  try{$null=Invoke-CompletionScenario ($cases|Where-Object name -CEQ $fault.case) $fault.fault;$failures.Add(@{control=$fault;message='Wrong dependency behavior was not rejected'})}
  catch{if($_.Exception.Message.StartsWith('COMPLETION_ASSERT:',[StringComparison]::Ordinal)){$controls.Add(@{case=$fault.case;fault=$fault.fault;assertion=$_.Exception.Message})}else{$failures.Add(@{control=$fault;message=$_.Exception.ToString()})}}
 }
}
$receipt=[ordered]@{timestamp=(Get-Date).ToString('o');mode=$Mode;plannedCases=$cases.Count;cases=$results.Count;assertions=$assertions;rejectingControls=$controls.Count;failed=$failures.Count;skipped=0;results=$results.ToArray();controls=$controls.ToArray();failures=$failures.ToArray();sourceHashes=@(Get-FileHash -LiteralPath $PSCommandPath,(Join-Path $PSScriptRoot 'GovernedAuditCompletion.Model.ps1')|Select-Object Path,Hash);scope='Expected-set assertion prerequisite only; no production completion implementation or host lifecycle acceptance';productComplete=$false}
Import-Module PowerShell.MCP -ErrorAction Stop
$var1=$receipt|ConvertTo-Json -Depth 100
Add-LinesToFile -Path $ReceiptPath -Content $var1|Out-Null
[pscustomobject]@{cases=$results.Count;planned=$cases.Count;assertions=$assertions;controls=$controls.Count;failed=$failures.Count;receipt=$ReceiptPath}
if($failures.Count){throw 'Expected-set completion gate failed; full chronological receipt retained'}
