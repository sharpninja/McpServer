#Requires -Version 7.5
# SPDX-License-Identifier: Apache-2.0
<#
.SYNOPSIS
Common host scenario inputs/assertions for independent mocks and canonical A8 composition.
#>
[CmdletBinding(SupportsShouldProcess)]
param([ValidateSet('Mock','Production')][string]$Mode='Mock',[Parameter(Mandatory)][string]$ReceiptPath,[Parameter(Mandatory)][string]$ArtifactRoot,[scriptblock]$ProviderFactory)
$ErrorActionPreference='Stop'
if(Test-Path $ReceiptPath){throw 'Chronological receipt exists'}
Write-Output 'Counted effects: isolated synthetic A8store/host/outbox scenarios with full interaction receipts and test-only rejecting controls.'
if(-not $PSCmdlet.ShouldProcess($ArtifactRoot,'Run host lifecycle ownership/fidelity assertions')){return}
if($Mode -ceq 'Mock'){
 . (Join-Path $PSScriptRoot 'GovernedSessionHost.Model.ps1')
 . (Join-Path $PSScriptRoot 'GovernedSessionHost.TransportModel.ps1')
}elseif(-not $ProviderFactory){throw 'Canonical host composition provider required'}
$script:hostAssertions=0
function Assert-SessionHostContract {
 param([bool]$Condition,[string]$Message)
 $script:hostAssertions++
 if(-not $Condition){throw ('HOST_ASSERT: '+$Message)}
}
function Get-SessionHostReviewJson {
 param([string]$Accuracy='98.0000000000000000000',[string]$Completeness='98.1234567890123456789012345678',[string]$Type='Standard',[string]$Notes='')
 $stream=[IO.MemoryStream]::new();$writer=[System.Text.Json.Utf8JsonWriter]::new($stream)
 try{
  $writer.WriteStartObject();$writer.WriteString('OverallVerdict','AGREE');$writer.WriteString('reviewType',$Type);$writer.WriteString('otherNotes',$Notes)
  foreach($pair in @(@('accuracy',$Accuracy),@('completeness',$Completeness))){$writer.WritePropertyName($pair[0]);if($pair[1].StartsWith('string:')){$writer.WriteStringValue($pair[1].Substring(7))}else{$writer.WriteRawValue($pair[1])}}
  $writer.WriteString('findings',"Full **review**`r`nwith all fields  ");$writer.WriteEndObject();$writer.Flush()
  return [Text.Encoding]::UTF8.GetString($stream.ToArray())
 }finally{$writer.Dispose();$stream.Dispose()}
}
function Invoke-SessionHostScenario {
 param([string]$Name,[string]$Mutation='')
 $root=Join-Path $ArtifactRoot ([guid]::NewGuid().ToString('N'))
 $tape=[Collections.Generic.List[object]]::new()
 $serverState=@{turns=@{};events=@{};effects=$tape;mutation=''}
 $state=@{storeDirectory=(Join-Path $root 'store');outboxes=@{};nextRequest=0;policy='allow';effects=$tape}
 $hostInput=@{workspacePath='F:/fixture/host';agentIdentity='Codex';mcpSessionId='Codex-20260101T000000Z-host';hostSessionKey='native-host-one';keySource='native';ingressNonce='nonce-one';boundAtUtc='2026-01-01T00:00:00Z';ingressText="  # Actual **request** $([char]0x03A9)`r`nline2 literal\n $([char]0x2013)$([char]0x2014)  ";actualIngress=$true;operationId='operation-one';requestClass='development'}
 $normalizedIngress=$hostInput.ingressText.Replace([char]0x2013,[char]0x002D).Replace([char]0x2014,[char]0x002D)
 $modeForCall=$Mode;$mutationForCall=$Mutation
 $modelCall=if($Mode -ceq 'Mock'){(Get-Command Invoke-MockGovernedSessionHost).ScriptBlock}else{$null}
 $serverModel=if($Mode -ceq 'Mock'){(Get-Command Invoke-MockSessionHostTransport).ScriptBlock}else{$null}
 $server={param($Operation,$Binding,$Data) return & $serverModel -State $serverState -Operation $Operation -Root $Binding -InputObject $Data}.GetNewClosure()
 $provider=if($Mode -ceq 'Production'){& $ProviderFactory $root $state $serverState}else{$null}
 $invoke={param($Context,$Operation,$Data)
  if($modeForCall -ceq 'Mock'){return & $modelCall -State $state -HostContext $Context -Operation $Operation -InputObject $Data -Server $server -Mutation $mutationForCall}
  return & $provider.Invoke $Context $Operation $Data
 }.GetNewClosure()
 $inspect={
  if($modeForCall -ceq 'Mock'){
   $snapshot=if(Test-Path (Join-Path $state.storeDirectory 'turn-store.json')){ConvertFrom-Json ([IO.File]::ReadAllText((Join-Path $state.storeDirectory 'turn-store.json'))) -AsHashtable -DateKind String}else{$null}
   return @{store=$snapshot;outboxes=$state.outboxes;server=$serverState;tape=$tape.ToArray()}
  }
  return & $provider.Inspect
 }.GetNewClosure()
 if($Name -ceq 'no-actual-ingress'){$hostInput.actualIngress=$false}
 if($Name -ceq 'missing-nonce'){$hostInput.ingressNonce=''}
 if($Name -ceq 'summary-ingress'){$serverState.mutation='summary-ingress'}
 $beginArguments=@{queryTitle='Preserve exact actual request';workspacePath='F:/foreign';agent='Foreign';sessionId='foreign-session'}
 $begin=& $invoke $hostInput $(if($Name -ceq 'mint-only'){ 'mintTurn' }else{'beginTurn'}) $beginArguments
 if($Name -cin @('no-actual-ingress','missing-nonce','summary-ingress')){
  Assert-SessionHostContract (-not $begin.ok) ($Name+': refused before ordinary execution')
  $data=& $inspect
  Assert-SessionHostContract (@($data.store.hosts|ForEach-Object {$_.turns}|Where-Object delivered).Count -eq 0) ($Name+': no fabricated delivery')
  return @{name=$Name;result=$begin;evidence=$data}
 }
 Assert-SessionHostContract ($begin.ok -eq $true) ($Name+': actual ingress bound')
 $request=$begin.binding.requestId
 Assert-SessionHostContract ($begin.binding.workspacePath -ceq $hostInput.workspacePath -and $begin.binding.agentIdentity -ceq $hostInput.agentIdentity -and $begin.binding.mcpSessionId -ceq $hostInput.mcpSessionId -and $begin.binding.hostSessionKey -ceq $hostInput.hostSessionKey) ($Name+': tool roots cannot replace captured host roots')
 $data=& $inspect
 $initial=@($data.server.turns.Values|Where-Object requestId -CEQ $request)[0]
 Assert-SessionHostContract ($initial.queryText -ceq $normalizedIngress) ($Name+': complete normalized ingress exactly retained')
 $action=@{order=1;type='tool_execution';description='Actual tool effect';status='completed';filePath='F:/fixture/a.cs'}|ConvertTo-Json -Compress
 $decision=@{conclusion="Selected existing lifecycle, alternatives rejected.`r`nFull reasoning  ";timestamp='2026-01-01T00:00:01Z';noDecisionAttestation=($Name -ceq 'explicit-no-decision')}|ConvertTo-Json -Compress
 if($Name -ceq 'mint-only'){
  $write=& $invoke $hostInput 'appendAction' @{requestId=$request;payloadJson=$action}
  Assert-SessionHostContract (-not $write.ok) 'Minting alone cannot authorize ordinary writes even with explicit request'
 }elseif($Name -cin @('multi-open-ambiguous','explicit-original-session','foreign-host')){
  $other=ConvertFrom-Json (ConvertTo-Json $hostInput -Depth 80 -Compress) -AsHashtable -DateKind String
  $other.ingressNonce='nonce-two';$other.operationId='operation-two';$other.mcpSessionId='Codex-20260101T000001Z-host'
  if($Name -ceq 'foreign-host'){$other.hostSessionKey='native-other-host'}
  $second=& $invoke $other 'beginTurn' @{queryTitle='Second actual request'}
  Assert-SessionHostContract $second.ok 'Second request opens without cancelling first'
  $writeArgs=@{payloadJson=$action}
  if($Name -cne 'multi-open-ambiguous'){$writeArgs.requestId=$request}
  $write=& $invoke $other 'appendAction' $writeArgs
  if($Name -ceq 'explicit-original-session'){
   Assert-SessionHostContract ($write.ok -and $write.binding.mcpSessionId -ceq $begin.binding.mcpSessionId -and $write.event.requestId -ceq $request) 'Explicit request retains original owning MCP session despite later session'
  }else{Assert-SessionHostContract (-not $write.ok -and $write.code -ceq $(if($Name -ceq 'foreign-host'){'request_unknown'}else{'request_ambiguous'})) 'Ambiguous/foreign ordinary target is refused'}
  $data=& $inspect
  Assert-SessionHostContract (@($data.store.hosts|ForEach-Object {$_.turns}|Where-Object status -CEQ 'open').Count -eq 2) 'Both original requests remain open'
 }elseif($Name.StartsWith('receipt-',[StringComparison]::Ordinal)){
  $serverState.mutation=$Name
  $write=& $invoke $hostInput 'appendAction' @{requestId=$request;payloadJson=$action}
  Assert-SessionHostContract (-not $write.ok) 'Wrong actual submit receipt cannot report persistence'
 }elseif($Name.StartsWith('review-',[StringComparison]::Ordinal)){
  $score=switch -CaseSensitive ($Name){'review-below'{'97.99999999999999999999999999999'};'review-exponent'{'9.8e1'};'review-signed'{'-98'};'review-string'{'string:98'};'review-no-upper-bound'{'10000000000000000000000000000.0'};default{'98.0000000000000000000'}}
  $raw=Get-SessionHostReviewJson -Accuracy $score -Type $(if($Name -ceq 'review-other-missing'){'Other'}else{'Standard'})
  $write=& $invoke $hostInput 'recordReview' @{requestId=$request;payloadJson=$raw}
  $valid=$Name -cin @('review-lossless','review-no-upper-bound')
  Assert-SessionHostContract ($write.ok -eq $valid) 'Review validation follows lexical score/Other-notes contract'
  if($valid){
   Assert-SessionHostContract ($write.event.payloadJson -ceq $raw) 'Every review field and exact score lexeme preserved'
   $document=[System.Text.Json.JsonDocument]::Parse($write.event.payloadJson)
   try{Assert-SessionHostContract ($document.RootElement.GetProperty('accuracy').GetRawText() -ceq $score -and $document.RootElement.GetProperty('completeness').GetRawText() -ceq '98.1234567890123456789012345678') '19/28digit precision and trailing zeros unchanged'}finally{$document.Dispose()}
  }
 }elseif($Name -ceq 'action-never-decision'){
  $write=& $invoke $hostInput 'appendAction' @{requestId=$request;payloadJson=$action}
  $dialog=@{timestamp='2026-01-01T00:00:01Z';role='tool';category='tool_result';content="Whole result`r`n  "}|ConvertTo-Json -Compress
  $hostInput.operationId='dialog-one';$spoken=& $invoke $hostInput 'appendDialog' @{requestId=$request;payloadJson=$dialog}
  $data=& $inspect;$turn=@($data.server.turns.Values|Where-Object requestId -CEQ $request)[0]
  Assert-SessionHostContract ($write.ok -and $spoken.ok -and @($turn.actions).Count -eq 1 -and @($turn.dialog).Count -eq 1 -and @($turn.decisions).Count -eq 0) 'Actual tool action/dialog remain distinct without invented decision'
 }else{
  $write=& $invoke $hostInput 'decision' @{requestId=$request;payloadJson=$decision}
  Assert-SessionHostContract $write.ok 'Explicit decision persisted'
  $again=& $invoke $hostInput 'decision' @{requestId=$request;payloadJson=$decision}
  $data=& $inspect;$turn=@($data.server.turns.Values|Where-Object requestId -CEQ $request)[0]
  Assert-SessionHostContract ($again.ok -and $again.event.eventId -ceq $write.event.eventId -and @($turn.decisions).Count -eq 1 -and @($turn.actions|Where-Object {$_.body.type -ceq 'design_decision'}).Count -eq 1 -and @($turn.dialog|Where-Object {$_.body.category -ceq 'decision'}).Count -eq 1) 'One replayed decision has exactly three projections and one UUID'
  Assert-SessionHostContract ($turn.decisions[0].conclusion -ceq $turn.dialog[0].body.content -and $turn.decisions[0].conclusion -ceq $turn.actions[0].body.description) 'Decision conclusion matches all three representations'
  if($Name -ceq 'explicit-no-decision'){
   $body=ConvertFrom-Json $write.event.payloadJson -AsHashtable
   Assert-SessionHostContract ($body.noDecisionAttestation -eq $true) 'Explicit attestation remains identifiable, not absent decision evidence'
  }elseif($Name -cne 'decision-replay'){
   $hostInput.operationId='terminal-one'
   if($Name -ceq 'policy-continue'){$state.policy='continue'}
   if($Name -ceq 'policy-blocked'){$state.policy='blocked'}
   if($Name -cin @('summary-final','wrong-terminal-root','timeout-after-complete')){$serverState.mutation=$Name}
   $response="  # Complete **response** $([char]0x03A9)`r`nline2 literal\n $([char]0x2013)$([char]0x2014)  "
   $normalizedResponse=$response.Replace([char]0x2013,[char]0x002D).Replace([char]0x2014,[char]0x002D)
   $cancel=$Name -ceq 'cancel-no-result'
   $final=& $invoke $hostInput $(if($cancel){'failTurn'}else{'completeTurn'}) @{requestId=$request;response=$response;failureNote="Actual cancellation`r`n  "}
   $valid=$Name -cnotin @('policy-continue','policy-blocked','summary-final','wrong-terminal-root')
   Assert-SessionHostContract ($final.ok -eq $valid) 'Completion reflects actual policy/audit/final readback, not empty queues'
   $data=& $inspect;$stored=@($data.server.turns.Values|Where-Object requestId -CEQ $request)[0]
   $local=@($data.store.hosts|ForEach-Object {$_.turns}|Where-Object requestId -CEQ $request)[0]
   if($valid){
    Assert-SessionHostContract ($local.status -ceq 'terminal' -and $stored.status -ceq $(if($cancel){'failed'}else{'completed'})) 'Only selected server-confirmed turn becomes locally terminal'
    if(-not $cancel){Assert-SessionHostContract ($stored.response -ceq $normalizedResponse -and $stored.queryText -ceq $normalizedIngress) 'Full final delivery text and original ingress exactly match'}
    else{Assert-SessionHostContract ($stored.response -ceq '') 'Cancellation never invents a final response'}
    $events=@($data.tape.operation)
    $terminalIndex=[array]::LastIndexOf($events,'Terminal');$readIndex=[array]::LastIndexOf($events,'ReadTurn');$completeIndex=[array]::LastIndexOf($events,$(if($cancel){'Fail'}else{'Complete'}))
    Assert-SessionHostContract ($completeIndex -ge 0 -and $readIndex -gt $completeIndex -and $terminalIndex -gt $readIndex) 'Terminal follows actual terminal write and independent full turn readback'
    if($Name -ceq 'timeout-after-complete'){Assert-SessionHostContract (@($data.tape|Where-Object operation -CEQ 'Complete').Count -eq 1) 'Ambiguous committed timeout reconciles by readback without duplicate write'}
   }else{Assert-SessionHostContract ($local.status -ceq 'open') 'Refusal retains original open turn for correction/recovery'}
  }
 }
 return @{name=$Name;binding=$begin.binding;evidence=(& $inspect)}
}
$names=@('complete-exact-text','decision-replay','explicit-no-decision','action-never-decision','mint-only','multi-open-ambiguous','explicit-original-session','foreign-host','no-actual-ingress','missing-nonce','summary-ingress','policy-continue','policy-blocked','summary-final','wrong-terminal-root','timeout-after-complete','cancel-no-result','review-lossless','review-no-upper-bound','review-below','review-exponent','review-signed','review-string','review-other-missing')
foreach($field in @('workspacePath','agentIdentity','mcpSessionId','requestId','eventId','order','payloadHash','revision')){$names+=('receipt-'+$field)}
$results=[Collections.Generic.List[object]]::new();$controls=[Collections.Generic.List[object]]::new();$failures=[Collections.Generic.List[object]]::new()
foreach($name in $names){try{$results.Add((Invoke-SessionHostScenario $name))}catch{$failures.Add(@{case=$name;error=$_.Exception.ToString()})}}
if($Mode -ceq 'Mock'){
 foreach($fault in @(@('summary-ingress','ignore-ingress-readback'),@('mint-only','skip-delivery-guard'),@('explicit-original-session','target-latest'),@('decision-replay','duplicate-decision'),@('policy-continue','omit-completion-gate'),@('policy-blocked','omit-completion-gate'),@('summary-final','skip-final-readback'),@('wrong-terminal-root','skip-final-readback'),@('receipt-payloadHash','ignore-submit-receipt'))){
  try{$null=Invoke-SessionHostScenario $fault[0] $fault[1];$failures.Add(@{control=$fault;error='Wrong dependency/effect escaped assertions'})}catch{if($_.Exception.Message.StartsWith('HOST_ASSERT:',[StringComparison]::Ordinal)){$controls.Add(@{case=$fault[0];mutation=$fault[1];rejection=$_.Exception.Message})}else{$failures.Add(@{control=$fault;error=$_.Exception.ToString()})}}
 }
}
$receipt=[ordered]@{timestamp=Get-Date -Format o;mode=$Mode;plannedCases=$names.Count;cases=$results.Count;assertions=$script:hostAssertions;rejectingControls=$controls.Count;failed=$failures.Count;skipped=0;results=$results.ToArray();controls=$controls.ToArray();failures=$failures.ToArray();sourceHashes=@(Get-FileHash $PSCommandPath,(Join-Path $PSScriptRoot 'GovernedSessionHost.Model.ps1'),(Join-Path $PSScriptRoot 'GovernedSessionHost.TransportModel.ps1')|Select-Object Path,Hash);scope='Canonical A8host composition prerequisite; no native/fullscope acceptance';productComplete=$false}
Import-Module PowerShell.MCP -ErrorAction Stop
$var1=ConvertTo-Json $receipt -Depth 100
Add-LinesToFile $ReceiptPath -Content $var1|Out-Null
[pscustomobject]@{cases=$results.Count;planned=$names.Count;assertions=$script:hostAssertions;controls=$controls.Count;failed=$failures.Count;receipt=$ReceiptPath}
if($failures.Count){throw 'Host mock prerequisite failed; full chronological evidence retained'}
