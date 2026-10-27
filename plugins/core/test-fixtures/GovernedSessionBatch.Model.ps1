# SPDX-License-Identifier: Apache-2.0
function Get-MockSessionBatchProvider {
 <#
 .SYNOPSIS
 Independent input-driven decoder over existing host/storage/transport models.
 .DESCRIPTION
 Seeds original single keys and injects dependency failures at capture.
 No production imports, scenario names or expected outcome objects.
 #>
 [CmdletBinding()]
 param([string]$Root,[string]$FixtureDirectory,[string]$Mutation='')
 . (Join-Path $FixtureDirectory 'GovernedSessionHost.Model.ps1')
 . (Join-Path $FixtureDirectory 'GovernedSessionHost.TransportModel.ps1')
 $fault=$Mutation;$control=@{armed=$false;hit=$false;restarted=$false}
 $tape=[Collections.Generic.List[object]]::new()
 $serverState=@{turns=@{};events=@{};effects=$tape;mutation=''}
 $state=@{storeDirectory=(Join-Path $Root 'store');outboxes=@{};nextRequest=0;policy='allow';effects=$tape}
 $hostModel=(Get-Command Invoke-MockGovernedSessionHost).ScriptBlock
 $transportModel=(Get-Command Invoke-MockSessionHostTransport).ScriptBlock
 $server={param($Operation,$Binding,$Data) & $transportModel -State $serverState -Operation $Operation -Root $Binding -InputObject $Data}.GetNewClosure()
 $invoke={
  param($Envelope,$TrustedCapture)
  $arguments=$Envelope.params
  $context=ConvertFrom-Json (ConvertTo-Json $TrustedCapture -Depth 80 -Compress) -AsHashtable -DateKind String
  if($Envelope.method -ceq 'workflow.sessionlog.beginTurn'){return & $hostModel -State $state -HostContext $context -Operation beginTurn -InputObject @{queryTitle=$arguments.queryTitle} -Server $server}
  $kind=switch -CaseSensitive ($Envelope.method){'workflow.sessionlog.appendActions' {'actions'} 'workflow.sessionlog.appendDialog' {'dialogItems'} default {throw 'Unsupported batch model method'}}
  $items=@($arguments[$kind]);if(-not $items.Count){return @{ok=$false;code='invalid_params'}}
  if($fault -ceq 'omit-tail'){$items=@($items | Select-Object -SkipLast 1)}
  if($fault -ceq 'reverse'){[array]::Reverse($items)}
  $operation=if($kind -ceq 'actions'){'appendAction'}else{'appendDialog'};$last=$null
  for($index=0;$index -lt $items.Count;$index++){
   $item=ConvertFrom-Json (ConvertTo-Json $items[$index] -Depth 80 -Compress) -AsHashtable -DateKind String
   if($fault.StartsWith('omit-field:',[StringComparison]::Ordinal)){$null=$item.Remove($fault.Substring(11))}
   if($fault.StartsWith('wrong-field:',[StringComparison]::Ordinal)){$field=$fault.Substring(12);$item[$field]=if($field -ceq 'order'){-99}else{'WRONG'}}
   $owned=ConvertFrom-Json (ConvertTo-Json $context -Depth 80 -Compress) -AsHashtable -DateKind String
   $data=@{requestId=$arguments.requestId;payloadJson=(ConvertTo-Json $item -Depth 80 -Compress)}
   if($items.Count -gt 1){$data.batchIndex=$index}
   if($fault -ceq 'single-batch-collision' -and $items.Count -gt 1){$null=$data.Remove('batchIndex');$owned.operationId=$context.operationId+':item:'+($index.ToString([Globalization.CultureInfo]::InvariantCulture))}
   if($fault -ceq 'legacy-single-rekey' -and $items.Count -eq 1){$data.batchIndex=0}
   if($fault -ceq 'rekey-after-restart' -and $control.restarted){$owned.operationId+=':wrong-rekey'}
   if($control.armed -and $index -eq 1){foreach($box in $state.outboxes.Values){$box.crashAfterJournal=$true};$control.armed=$false;$control.hit=$true}
   $last=& $hostModel -State $state -HostContext $owned -Operation $operation -InputObject $data -Server $server
   if(-not $last.ok -and $fault -cne 'tail-after-failure'){return $last}
  }
  return $last
 }.GetNewClosure()
 $seed={
  param($Envelope,$Context)
  $action=$Envelope.method -ceq 'workflow.sessionlog.appendActions';$kind=if($action){'actions'}else{'dialogItems'};$operation=if($action){'appendAction'}else{'appendDialog'}
  $data=@{requestId=$Envelope.params.requestId;payloadJson=(ConvertTo-Json $Envelope.params[$kind][0] -Depth 80 -Compress)}
  return & $hostModel -State $state -HostContext $Context -Operation $operation -InputObject $data -Server $server
 }.GetNewClosure()
 $snapshot={
  $keys=@(foreach($box in $state.outboxes.Values){foreach($manifest in $box.manifests.Values){foreach($key in $manifest.captureKeys.Keys){@{key=$key;eventId=$manifest.captureKeys[$key].eventId}}}})
  $names=@(foreach($box in $state.outboxes.Values){foreach($record in $box.records){$logical=$record.event.requestId;foreach($pending in $box.aliases.Keys){if($box.aliases[$pending] -ceq $logical){$logical=$pending;break}};$digest=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($logical))).ToLowerInvariant();$digest+'-'+([long]$record.event.order).ToString('D16')+'-'+$record.event.eventId+'.event.yaml'}})
  return @{captureKeys=$keys;filenames=$names;counter=@($state.outboxes.Values|ForEach-Object {$_.orders.Values});stateJson=(ConvertTo-Json $state.outboxes -Depth 100 -Compress);aliasBytes=[IO.File]::ReadAllText((Join-Path $state.storeDirectory 'turn-store.json'))}
 }.GetNewClosure()
 $inspect={return @{server=$serverState;tape=$tape.ToArray();outboxes=$state.outboxes;manifests=@($state.outboxes.Values|ForEach-Object {$_.manifests.Values});faultHit=$control.hit}}.GetNewClosure()
 $arm={$control.armed=$true}.GetNewClosure()
 $restart={foreach($box in $state.outboxes.Values){$box.crashAfterJournal=$false};$control.armed=$false;$control.restarted=$true;$tape.Add(@{operation='Restart'})}.GetNewClosure()
 return @{Invoke=$invoke;Inspect=$inspect;SeedLegacy=$seed;StorageSnapshot=$snapshot;ArmFault=$arm;Restart=$restart}
}
