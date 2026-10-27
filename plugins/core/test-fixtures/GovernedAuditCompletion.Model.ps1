# SPDX-License-Identifier: Apache-2.0
<#
.SYNOPSIS
Independent input-only expected-set checker model. No production imports/case names.
.DESCRIPTION
Reads scripted storage bytes and server receipts through injected dependencies.
The common runner owns inputs and assertions. Mutation is test-only fault injection.
#>
function Test-MockGovernedAuditCompletion {
 [CmdletBinding()]
 param([Collections.IDictionary]$Context,[Collections.IDictionary]$InputObject,[Collections.IDictionary]$Dependencies,[string]$Mutation='')
 $checked=[Collections.Generic.List[string]]::new()
 function Get-CompletionRefusal {
  param([string]$Reason)
  return @{allowed=$false;code='session_turn_completion_gate';detailCode=$Reason;checkedEventIds=$checked.ToArray()}
 }
 function Test-CompletionIdentity {
  param([Collections.IDictionary]$Actual,[Collections.IDictionary]$Expected,[bool]$Receipt)
  foreach($field in @('workspacePath','agentIdentity','mcpSessionId','requestId','eventId','payloadHash')){
   if($Actual[$field] -isnot [string] -or $Actual[$field] -cne $Expected[$field]){return $false}
  }
  if($Actual.order -isnot [long] -and $Actual.order -isnot [int]){return $false}
  if($Actual.order -ne $Expected.order){return $false}
  if(-not $Receipt -and ($Actual.kind -cne $Expected.kind -or $Actual.payloadJson -cne $Expected.payloadJson)){return $false}
  return $true
 }
 try{
  $expected=@($InputObject.expectedEvents)
  if($expected.Count -eq 0 -or [string]::IsNullOrWhiteSpace($InputObject.ingressEventId)){return Get-CompletionRefusal 'expected_set_missing'}
  if($InputObject.requiresResult -isnot [bool] -or ($InputObject.requiresResult -and [string]::IsNullOrWhiteSpace($InputObject.resultEventId))){return Get-CompletionRefusal 'expected_set_missing'}
  $byId=@{};$next=1L
  foreach($audit in $expected){
   if($audit -isnot [Collections.IDictionary] -or $byId.ContainsKey($audit.eventId) -or $audit.order -ne $next -or
    $audit.workspacePath -cne $Context.workspacePath -or $audit.agentIdentity -cne $Context.agentIdentity -or
    $audit.mcpSessionId -cne $Context.mcpSessionId -or $audit.requestId -cne $InputObject.requestId){return Get-CompletionRefusal 'expected_set_invalid'}
   $uuid=[guid]::Empty
   if(-not [guid]::TryParseExact($audit.eventId,'D',[ref]$uuid)){return Get-CompletionRefusal 'expected_set_invalid'}
   $byId[$audit.eventId]=$audit;$next++
  }
  if(-not $byId.ContainsKey($InputObject.ingressEventId) -or
   ($InputObject.requiresResult -and (-not $byId.ContainsKey($InputObject.resultEventId) -or $InputObject.resultEventId -ceq $InputObject.ingressEventId))){return Get-CompletionRefusal 'expected_set_invalid'}
  $paths=@(& $Dependencies.ListRecord $InputObject.requestId)
  if($Mutation -ceq 'skip-storage'){$paths=@()}
  if($paths.Count -ne $expected.Count -and $Mutation -cne 'ignore-set'){return Get-CompletionRefusal 'retained_set_mismatch'}
  $retained=@{}
  foreach($path in $paths){
   $record=ConvertFrom-Json (& $Dependencies.ReadRecord $path) -AsHashtable -DateKind String
   $audit=$record.event
   if($audit -isnot [Collections.IDictionary] -or -not $byId.ContainsKey($audit.eventId) -or $retained.ContainsKey($audit.eventId)){
    if($Mutation -cne 'ignore-set'){return Get-CompletionRefusal 'retained_set_mismatch'}else{continue}
   }
   $original=$byId[$audit.eventId]
   if(-not (Test-CompletionIdentity $audit $original $false) -and $Mutation -cne 'ignore-payload'){return Get-CompletionRefusal 'retained_event_mismatch'}
   $digest=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($audit.payloadJson))).ToLowerInvariant()
   if($digest -cne $audit.payloadHash -and $Mutation -cne 'ignore-hash'){return Get-CompletionRefusal 'retained_event_mismatch'}
   try{$document=[System.Text.Json.JsonDocument]::Parse($audit.payloadJson);$objectPayload=$document.RootElement.ValueKind -eq [System.Text.Json.JsonValueKind]::Object;$document.Dispose()}catch{$objectPayload=$false}
   if(-not $objectPayload){return Get-CompletionRefusal 'retained_event_mismatch'}
   if($record.state -cne 'archive' -and $Mutation -cne 'ignore-blocker'){return Get-CompletionRefusal 'mandatory_evidence_blocked'}
   $receipt=$record.receipt
   if($receipt -isnot [Collections.IDictionary] -or $receipt.code -cne 'persisted' -or
    ($receipt.revision -isnot [long] -and $receipt.revision -isnot [int]) -or $receipt.revision -lt 1 -or
    -not (Test-CompletionIdentity $receipt $original $true)){return Get-CompletionRefusal 'retained_receipt_mismatch'}
   $retained[$audit.eventId]=$record
  }
  if($retained.Count -ne $expected.Count -and $Mutation -cne 'ignore-set'){return Get-CompletionRefusal 'retained_set_mismatch'}
  foreach($audit in $expected){
   if($Mutation -ceq 'skip-readback'){$checked.Add($audit.eventId);continue}
   $reply=& $Dependencies.ReadServer $audit
   if($Mutation -cne 'ignore-readback'){
    if($reply -isnot [Collections.IDictionary] -or $reply.code -cne 'persisted' -or
     ($reply.revision -isnot [long] -and $reply.revision -isnot [int]) -or $reply.revision -lt 1 -or
     -not (Test-CompletionIdentity $reply $audit $true) -or $reply.revision -ne $retained[$audit.eventId].receipt.revision){return Get-CompletionRefusal 'server_readback_mismatch'}
   }
   $checked.Add($audit.eventId)
  }
  return @{allowed=$true;code='verified';detailCode='';checkedEventIds=$checked.ToArray()}
 }catch{$refusal=Get-CompletionRefusal 'evidence_unavailable';$refusal.diagnostic=$_.Exception.ToString();return $refusal}
}
