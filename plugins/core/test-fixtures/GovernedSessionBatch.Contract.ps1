#Requires -Version 7.5
# SPDX-License-Identifier: Apache-2.0
<#
.SYNOPSIS
FR-QB-003/005 TEST-QB-008/009/069: same batch capture/replay assertions for model and canonical providers.
.DESCRIPTION
Supplements, and never replaces, the frozen32host and31actualrouting cases.
Correct and deliberately corrupt inputs traverse independent host/storage/server models.
Production must supply the canonical dispatch boundary and real observations.
#>
[CmdletBinding(SupportsShouldProcess)]
param([ValidateSet('Mock','Production')][string]$Mode='Mock',[Parameter(Mandatory)][string]$ReceiptPath,[Parameter(Mandatory)][string]$ArtifactRoot,[Parameter(Mandatory)][string]$FixtureDirectory,[scriptblock]$ProviderFactory)
$ErrorActionPreference='Stop'
if(Test-Path $ReceiptPath){throw 'Chronological receipt exists'}
if($Mode -ceq 'Production' -and -not $ProviderFactory){throw 'Actual canonical batch provider required'}
Write-Output 'Counted effects:10batch scenarios,30intended field/count/order controls in Mock, retained synthetic storage and one receipt; no native/fullscope acceptance.'
if(-not $PSCmdlet.ShouldProcess($ArtifactRoot,'Execute SAME batch capture/retry assertions')){return}
Import-Module PowerShell.MCP -ErrorAction Stop
if($Mode -ceq 'Mock'){. (Join-Path $PSScriptRoot 'GovernedSessionBatch.Model.ps1')}
$script:batchAssertions=0
function Assert-SessionBatchCondition {
 <# .SYNOPSIS
 Counts and enforces a requirement-derived batch assertion.
 #>
 [CmdletBinding()]
 param([bool]$Condition,[string]$Message)
 $script:batchAssertions++
 if(-not $Condition){throw ('BATCH_ASSERT: '+$Message)}
}
function Invoke-SessionBatchScenario {
 <# .SYNOPSIS
 Runs the same supplied inputs, dependencies and effect assertions in either mode.
 #>
 [CmdletBinding()]
 param([string]$Kind,[bool]$NewOperation,[string]$Mutation='',[bool]$PrefixSingle=$false)
 $root=Join-Path $ArtifactRoot ([guid]::NewGuid().ToString('N'))
 $provider=if($Mode -ceq 'Mock'){Get-MockSessionBatchProvider -Root $root -FixtureDirectory $FixtureDirectory -Mutation $Mutation}else{& $ProviderFactory $root}
 $capture=@{workspacePath='F:/fixture/batch';agentIdentity='Codex';mcpSessionId='Codex-20260101T000000Z-batch';hostSessionKey='batch-native-host';keySource='native';ingressNonce='batch-nonce';boundAtUtc='2026-01-01T00:00:00Z';ingressText="  # Full actual request`r`nkept  ";actualIngress=$true;operationId='batch-call';requestClass='development'}
 $begin=& $provider.Invoke @{method='workflow.sessionlog.beginTurn';params=@{queryTitle='Batch fidelity';workspacePath='F:/foreign';agent='Foreign';sessionId='foreign'}} $capture
 Assert-SessionBatchCondition ($begin.ok -eq $true) 'Host ingress binding succeeds'
 $request=$begin.binding.requestId
 $items=@(for($index=0;$index -lt 3;$index++){
  $text="  # Item $index **full** $([char]0x03A9)`r`nkept  "
  if($Kind -ceq 'actions'){@{order=@(4,9,12)[$index];description=$text;type=@('edit','validation','tool_execution')[$index];status=@('completed','in_progress','failed')[$index];filePath=('F:/fixture/batch/'+$index+'-'+[char]0x03B2+'.cs')}}
  else{@{content=$text;role=@('model','tool','system')[$index];category=@('reasoning','tool_result','observation')[$index];timestamp=@('2026-01-01T01:02:03.0000001-05:00','2026-01-02T04:05:06.0000002+02:00','2026-01-03T07:08:09.0000003+00:00')[$index]}}
 })
 $method=if($Kind -ceq 'actions'){'workflow.sessionlog.appendActions'}else{'workflow.sessionlog.appendDialog'}
 $parameters=@{requestId=$request;workspacePath='F:/foreign';agent='Foreign';sessionId='foreign'};$parameters[$Kind]=$items
 $envelope=@{method=$method;params=$parameters}
 $expectedItems=@($items)
 if($PrefixSingle){
  $prelude=ConvertFrom-Json (ConvertTo-Json $items[0] -Depth 30 -Compress) -AsHashtable -DateKind String
  if($Kind -ceq 'actions'){$prelude.description='Distinct original single operation'}else{$prelude.content='Distinct original single operation'}
  $singleCapture=ConvertFrom-Json (ConvertTo-Json $capture -Depth 30 -Compress) -AsHashtable -DateKind String
  $singleCapture.operationId='batch-call:item:0'
  $singleParameters=@{requestId=$request};$singleParameters[$Kind]=@($prelude)
  $single=& $provider.Invoke @{method=$method;params=$singleParameters} $singleCapture
  Assert-SessionBatchCondition ($single.ok -eq $true) 'Original single operation persists before batch'
  $expectedItems=@($prelude)+@($items)
 }
 $first=& $provider.Invoke $envelope $capture
 Assert-SessionBatchCondition ($first.ok -eq $true) 'Nonempty full batch persists'
 $initial=& $provider.Inspect
 $eventKind=if($Kind -ceq 'actions'){'action'}else{'dialog'}
 $firstEvents=@($initial.server.events.Values | ForEach-Object {$_.event} | Where-Object kind -CEQ $eventKind | Sort-Object order)
 Assert-SessionBatchCondition ($firstEvents.Count -eq $expectedItems.Count) 'Every supplied batch item is captured and stored'
 $firstIds=@($firstEvents.eventId)
 $repeat=& $provider.Invoke $envelope $capture
 Assert-SessionBatchCondition ($repeat.ok -eq $true) 'Same actual operation retry reconciles'
 $afterRetry=& $provider.Inspect
 $retryEvents=@($afterRetry.server.events.Values | ForEach-Object {$_.event} | Where-Object kind -CEQ $eventKind | Sort-Object order)
 Assert-SessionBatchCondition (($retryEvents.eventId -join '|') -ceq ($firstIds -join '|')) 'Retry preserves every original UUID/order without duplicate events'
 if($NewOperation){
  $capture.operationId='batch-call-distinct'
  $distinct=& $provider.Invoke $envelope $capture
  Assert-SessionBatchCondition ($distinct.ok -eq $true) 'Distinct actual operation with identical input persists independently'
  $expectedItems+=@($items)
 }
 $observed=& $provider.Inspect
 $events=@($observed.server.events.Values | ForEach-Object {$_.event} | Where-Object kind -CEQ $eventKind | Sort-Object order)
 $expectedCount=$expectedItems.Count
 Assert-SessionBatchCondition ($events.Count -eq $expectedCount) 'Distinct operation cardinality retained'
 Assert-SessionBatchCondition (@($events.eventId | Sort-Object -Unique).Count -eq $expectedCount) 'All distinct events have unique UUIDs'
 for($index=0;$index -lt $events.Count;$index++){
  $record=$events[$index];$expected=$expectedItems[$index]
  foreach($field in @('workspacePath','agentIdentity','mcpSessionId')){Assert-SessionBatchCondition ($record[$field] -ceq $capture[$field]) ('Captured owning root retained: '+$field)}
  Assert-SessionBatchCondition ($record.requestId -ceq $request) 'Original owning turn retained'
  if($index){Assert-SessionBatchCondition ($record.order -gt $events[$index-1].order) 'Published event order follows supplied item order'}
  $body=ConvertFrom-Json $record.payloadJson -AsHashtable -DateKind String
  $requiredFields=if($Kind -ceq 'actions'){@('description','order','type','status','filePath')}else{@('content','role','category','timestamp')}
  foreach($field in $requiredFields){
   $valid=$body.Contains($field)
   if($field -ceq 'order'){$valid=$valid -and ($body[$field] -is [int] -or $body[$field] -is [long]) -and $body[$field] -eq $expected[$field]}
   else{$valid=$valid -and $body[$field] -is [string] -and [string]::Equals($body[$field],$expected[$field],[StringComparison]::Ordinal)}
   Assert-SessionBatchCondition $valid ('Stored full field retained: '+$field)
  }
  $digest=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($record.payloadJson))).ToLowerInvariant()
  Assert-SessionBatchCondition ($record.payloadHash -ceq $digest) 'Stored hash describes exact full payload bytes'
 }
 $submitted=@($observed.tape | Where-Object operation -CEQ Submit | Where-Object {$_.input.kind -ceq $eventKind})
 Assert-SessionBatchCondition ($submitted.Count -ge $expectedCount) 'Actual scripted publication interactions were exercised'
 return @{kind=$Kind;newOperation=$NewOperation;prefixSingle=$PrefixSingle;events=$events;publicationEffects=$submitted;artifactRoot=$root}
}
function Invoke-SessionBatchRecoveryScenario {
 <# .SYNOPSIS
 Verifies original single replay and non-atomic partial batch recovery through the same provider seam.
 #>
 [CmdletBinding()]
 param([string]$Kind,[bool]$Legacy,[string]$Mutation='')
 $root=Join-Path $ArtifactRoot ([guid]::NewGuid().ToString('N'))
 $provider=if($Mode -ceq 'Mock'){Get-MockSessionBatchProvider -Root $root -FixtureDirectory $FixtureDirectory -Mutation $Mutation}else{& $ProviderFactory $root}
 $context=@{workspacePath='F:/fixture/batch-recovery';agentIdentity='Codex';mcpSessionId='Codex-20260101T000000Z-batch-recovery';hostSessionKey='recovery-native-host';keySource='native';ingressNonce='recovery-nonce';boundAtUtc='2026-01-01T00:00:00Z';ingressText="  # Actual recovery request`r`nall text  ";actualIngress=$true;operationId='original-recovery-operation';requestClass='development'}
 $begin=& $provider.Invoke @{method='workflow.sessionlog.beginTurn';params=@{queryTitle='Original recovery request'}} $context
 Assert-SessionBatchCondition ($begin.ok -eq $true) 'Recovery ingress is bound'
 $request=$begin.binding.requestId
 $items=@(for($index=0;$index -lt 3;$index++){
  $text="  # Original item $index **kept**`r`nall text  "
  if($Kind -ceq 'actions'){@{order=5+$index;description=$text;type=@('edit','validation','tool_execution')[$index];status=@('completed','in_progress','failed')[$index];filePath=('F:/fixture/recovery/'+$index+'.cs')}}
  else{@{content=$text;role=@('model','tool','system')[$index];category=@('reasoning','tool_result','observation')[$index];timestamp=@('2026-01-01T01:02:03.0000001-05:00','2026-01-02T04:05:06.0000002+02:00','2026-01-03T07:08:09.0000003+00:00')[$index]}}
 })
 $action=$Kind -ceq 'actions';$method=if($action){'workflow.sessionlog.appendActions'}else{'workflow.sessionlog.appendDialog'};$eventKind=if($action){'action'}else{'dialog'};$operation=if($action){'appendAction'}else{'appendDialog'}
 $parameters=@{requestId=$request};$parameters[$Kind]=@($items)
 if($Legacy){$parameters[$Kind]=@($items[0])}
 $envelope=@{method=$method;params=$parameters}
 if($Legacy){
  $seed=& $provider.SeedLegacy $envelope $context
  Assert-SessionBatchCondition ($seed.ok -eq $true) 'Original single capture is seeded through legacy path'
  $original=ConvertFrom-Json (ConvertTo-Json $seed.event -Depth 50 -Compress) -AsHashtable -DateKind String
  $before=& $provider.StorageSnapshot
  $beforeJson=ConvertTo-Json $before -Depth 100 -Compress
  $oldKey=$operation+':'+$context.operationId
  $mapping=@($before.captureKeys | Where-Object key -CEQ $oldKey)
  Assert-SessionBatchCondition ($mapping.Count -eq 1 -and $mapping[0].eventId -ceq $original.eventId) 'Original raw single capture key identifies original UUID'
  Assert-SessionBatchCondition (@($before.filenames | Where-Object { $_.EndsWith(('-'+$original.eventId+'.event.yaml'),[StringComparison]::Ordinal) }).Count -eq 1) 'Original stable filename is retained'
  Assert-SessionBatchCondition (@($before.counter).Count -gt 0 -and -not [string]::IsNullOrEmpty($before.aliasBytes)) 'Original counter and alias evidence exists'
  $again=& $provider.Invoke $envelope $context
  Assert-SessionBatchCondition ($again.ok -eq $true) 'Old single operation retries successfully'
  $after=& $provider.Inspect
  $events=@($after.server.events.Values|ForEach-Object {$_.event}|Where-Object kind -CEQ $eventKind)
  Assert-SessionBatchCondition ($events.Count -eq 1 -and $events[0].eventId -ceq $original.eventId) 'Legacy retry retains original single event identity'
  foreach($field in $original.Keys){Assert-SessionBatchCondition ($events[0][$field] -ceq $original[$field]) ('Legacy immutable event field retained: '+$field)}
  $afterStorage=& $provider.StorageSnapshot
  Assert-SessionBatchCondition ((ConvertTo-Json $afterStorage -Depth 100 -Compress) -ceq $beforeJson) 'Legacy raw key UUID hash order counter filename alias and stored bytes unchanged'
  return @{kind=$Kind;legacy=$true;original=$original;before=$before;after=$afterStorage;events=$events;artifactRoot=$root}
 }
 & $provider.ArmFault
 $first=& $provider.Invoke $envelope $context
 $before=ConvertFrom-Json (ConvertTo-Json -InputObject (& $provider.Inspect) -Depth 100 -Compress) -AsHashtable -DateKind String
 Assert-SessionBatchCondition $before.faultHit 'Second item journal fault is actually reached'
 Assert-SessionBatchCondition ($first.ok -ne $true) 'Interrupted batch never reports success'
 $captured=@($before.manifests | ForEach-Object {$_.expectedEvents} | Where-Object kind -CEQ $eventKind | Sort-Object order)
 Assert-SessionBatchCondition ($captured.Count -eq 2) 'Only reached batch items are captured before interruption'
 $committed=@($before.server.events.Values | ForEach-Object {$_.event} | Where-Object kind -CEQ $eventKind)
 Assert-SessionBatchCondition ($committed.Count -eq 1 -and $committed[0].eventId -ceq $captured[0].eventId) 'First item persists before interrupted second; no batch atomicity assumed'
 $textField=if($action){'description'}else{'content'}
 Assert-SessionBatchCondition (@($captured | Where-Object {(ConvertFrom-Json $_.payloadJson -AsHashtable -DateKind String)[$textField] -ceq $items[2][$textField]}).Count -eq 0) 'Unreached tail item is not silently captured'
 & $provider.Restart
 $replayed=& $provider.Invoke $envelope $context
 Assert-SessionBatchCondition ($replayed.ok -eq $true) 'Same interrupted batch resumes after reopening'
 $final=& $provider.Inspect
 $events=@($final.server.events.Values | ForEach-Object {$_.event} | Where-Object kind -CEQ $eventKind | Sort-Object order)
 foreach($original in $captured){
  $match=@($events | Where-Object eventId -CEQ $original.eventId)
  Assert-SessionBatchCondition ($match.Count -eq 1) 'Original captured UUID survives resumed server replay'
  foreach($field in $original.Keys){Assert-SessionBatchCondition ($match[0][$field] -ceq $original[$field]) ('Interrupted immutable event field retained: '+$field)}
 }
 Assert-SessionBatchCondition ($events.Count -eq 3 -and @($events.eventId|Sort-Object -Unique).Count -eq 3) 'All three original items persist without duplicate event identities'
 for($index=0;$index -lt 3;$index++){
  $body=ConvertFrom-Json $events[$index].payloadJson -AsHashtable -DateKind String
  foreach($field in $items[$index].Keys){Assert-SessionBatchCondition ($body.Contains($field) -and $body[$field] -ceq $items[$index][$field]) ('Recovered full input field retained: '+$field)}
 }
 return @{kind=$Kind;legacy=$false;before=$before;captured=$captured;events=$events;artifactRoot=$root}
}
$results=[Collections.Generic.List[object]]::new();$failures=[Collections.Generic.List[object]]::new();$rejections=[Collections.Generic.List[object]]::new()
foreach($kind in @('actions','dialogItems')){foreach($distinct in @($false,$true)){try{$results.Add((Invoke-SessionBatchScenario -Kind $kind -NewOperation $distinct))}catch{$failures.Add(@{kind=$kind;newOperation=$distinct;error=$_.Exception.Message})}}}
foreach($kind in @('actions','dialogItems')){try{$results.Add((Invoke-SessionBatchScenario -Kind $kind -NewOperation $false -PrefixSingle $true))}catch{$failures.Add(@{kind=$kind;prefixSingle=$true;error=$_.Exception.Message})}}
if($Mode -ceq 'Mock'){
 foreach($kind in @('actions','dialogItems')){
  try{$null=Invoke-SessionBatchScenario -Kind $kind -NewOperation $false -PrefixSingle $true -Mutation 'single-batch-collision';$failures.Add(@{kind=$kind;mutation='single-batch-collision';error='Capture identity collision accepted'})}
  catch{if($_.Exception.Message -ceq 'BATCH_ASSERT: Nonempty full batch persists'){$rejections.Add(@{kind=$kind;mutation='single-batch-collision';assertion=$_.Exception.Message})}else{$failures.Add(@{kind=$kind;mutation='single-batch-collision';error=$_.Exception.Message})}}
 }
}
if($Mode -ceq 'Mock'){
 foreach($kind in @('actions','dialogItems')){
  $fields=if($kind -ceq 'actions'){@('order','description','type','status','filePath')}else{@('content','role','category','timestamp')}
  $mutations=@('omit-tail','reverse')+@($fields|ForEach-Object {'omit-field:'+$_;'wrong-field:'+$_})
  foreach($mutation in $mutations){
   try{$null=Invoke-SessionBatchScenario -Kind $kind -NewOperation $false -Mutation $mutation;$failures.Add(@{kind=$kind;mutation=$mutation;error='Corrupt dependency path accepted'})}
   catch{
    $message=$_.Exception.Message
    $intended=if($mutation -ceq 'omit-tail'){'BATCH_ASSERT: Every supplied batch item is captured and stored'}elseif($mutation -ceq 'reverse'){if($kind -ceq 'actions'){'BATCH_ASSERT: Stored full field retained: description'}else{'BATCH_ASSERT: Stored full field retained: content'}}else{'BATCH_ASSERT: Stored full field retained: '+$mutation.Substring($mutation.IndexOf(':')+1)}
    if($message -ceq $intended){$rejections.Add(@{kind=$kind;mutation=$mutation;assertion=$message})}else{$failures.Add(@{kind=$kind;mutation=$mutation;error=$message;expected=$intended})}
   }
  }
 }
}
foreach($kind in @('actions','dialogItems')){
 foreach($legacy in @($true,$false)){
  try{$results.Add((Invoke-SessionBatchRecoveryScenario -Kind $kind -Legacy $legacy))}catch{$failures.Add(@{kind=$kind;legacy=$legacy;error=$_.Exception.Message})}
 }
 if($Mode -ceq 'Mock'){
  foreach($mutation in @('legacy-single-rekey','tail-after-failure','rekey-after-restart')){
   $legacy=$mutation -ceq 'legacy-single-rekey'
   $expected=switch -CaseSensitive ($mutation){'legacy-single-rekey'{'BATCH_ASSERT: Legacy retry retains original single event identity'}'tail-after-failure'{'BATCH_ASSERT: Only reached batch items are captured before interruption'}'rekey-after-restart'{'BATCH_ASSERT: Original captured UUID survives resumed server replay'}}
   try{$null=Invoke-SessionBatchRecoveryScenario -Kind $kind -Legacy $legacy -Mutation $mutation;$failures.Add(@{kind=$kind;mutation=$mutation;error='Recovery mutation accepted'})}
   catch{if($_.Exception.Message -ceq $expected){$rejections.Add(@{kind=$kind;mutation=$mutation;assertion=$_.Exception.Message})}else{$failures.Add(@{kind=$kind;mutation=$mutation;error=$_.Exception.Message;expected=$expected})}}
  }
 }
}
$result=@{timestamp=Get-Date -Format o;mode=$Mode;requirements=@('FR-QB-003','FR-QB-005','TR-QB-AUDIT-001','AC-QB-008','AC-QB-009','AC-QB-069','TEST-QB-008','TEST-QB-009','TEST-QB-069');cases=$results.Count;assertions=$script:batchAssertions;rejectingControls=$rejections.Count;failed=$failures.Count;skipped=0;results=$results.ToArray();controls=$rejections.ToArray();failures=$failures.ToArray();sourceHashes=@(Get-FileHash $PSCommandPath,(Join-Path $PSScriptRoot 'GovernedSessionBatch.Model.ps1'),(Join-Path $FixtureDirectory 'GovernedSessionHost.Model.ps1'),(Join-Path $FixtureDirectory 'GovernedSessionHost.TransportModel.ps1')|Select-Object Path,Hash);productComplete=$false}
$var1=ConvertTo-Json $result -Depth 90
Add-LinesToFile $ReceiptPath -Content $var1|Out-Null
$result|Select-Object mode,cases,assertions,rejectingControls,failed,skipped|ConvertTo-Json
if($failures.Count){throw 'Batch assertion gate failed'}