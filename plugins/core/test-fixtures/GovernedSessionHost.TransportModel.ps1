# SPDX-License-Identifier: Apache-2.0
<#
.SYNOPSIS
Independent scripted SessionLog server model for host interaction assertions.
.DESCRIPTION
State changes follow supplied operations and full rooted payloads. No production
imports, scenario names, expected outcome objects or external service writes.
#>
function Invoke-MockSessionHostTransport {
 [CmdletBinding()]
 param([hashtable]$State,[string]$Operation,[Collections.IDictionary]$Root,[Collections.IDictionary]$InputObject)
 $key=ConvertTo-Json -InputObject @($Root.workspacePath,$Root.agentIdentity,$Root.mcpSessionId,$Root.requestId) -Compress
 $State.effects.Add(@{operation=$Operation;root=(ConvertFrom-Json (ConvertTo-Json $Root -Depth 80 -Compress) -AsHashtable -DateKind String);input=(ConvertFrom-Json (ConvertTo-Json $InputObject -Depth 80 -Compress) -AsHashtable -DateKind String)})
 if($Operation -ceq 'Begin'){
  if(-not $State.turns.ContainsKey($key)){
   $State.turns[$key]=@{workspacePath=$Root.workspacePath;agentIdentity=$Root.agentIdentity;mcpSessionId=$Root.mcpSessionId;requestId=$Root.requestId;queryText=$InputObject.queryText;queryTitle=$InputObject.queryTitle;response='';status='in_progress';actions=@();decisions=@();dialog=@()}
  }
  if($State.mutation -ceq 'summary-ingress'){$State.turns[$key].queryText='summary'}
  return @{code='merged'}
 }
 if($Operation -ceq 'ReadTurn'){
  if(-not $State.turns.ContainsKey($key)){return $null}
  $turn=ConvertFrom-Json (ConvertTo-Json $State.turns[$key] -Depth 80 -Compress) -AsHashtable -DateKind String
  if($State.mutation -ceq 'wrong-terminal-root' -and $turn.status -ceq 'completed'){$turn.requestId='foreign-request'}
  return $turn
 }
 if($Operation -ceq 'Submit'){
  if(-not $State.turns.ContainsKey($key)){throw 'Unknown model owning turn'}
  $eventKey=$key+'|'+$InputObject.eventId
  if(-not $State.events.ContainsKey($eventKey)){
   $receipt=@{}
   foreach($field in @('workspacePath','agentIdentity','mcpSessionId','requestId','eventId','order','payloadHash')){$receipt[$field]=$InputObject[$field]}
   $receipt.code='persisted';$receipt.revision=[long]$InputObject.order
   $State.events[$eventKey]=@{event=(ConvertFrom-Json (ConvertTo-Json $InputObject -Depth 80 -Compress) -AsHashtable -DateKind String);receipt=$receipt}
   $body=ConvertFrom-Json $InputObject.payloadJson -AsHashtable -DateKind String
   $turn=$State.turns[$key]
   switch -CaseSensitive ($InputObject.kind){
    'action'{$turn.actions+=@(@{eventId=$InputObject.eventId;body=$body})}
    'dialog'{$turn.dialog+=@(@{eventId=$InputObject.eventId;body=$body})}
    'decision'{
     $turn.decisions+=@(@{eventId=$InputObject.eventId;conclusion=$body.conclusion})
     if($State.mutation -cne 'omit-decision-dialog'){$turn.dialog+=@(@{eventId=$InputObject.eventId;body=@{content=$body.conclusion;category='decision'}})}
     $turn.actions+=@(@{eventId=$InputObject.eventId;body=@{description=$body.conclusion;type='design_decision'}})
    }
   }
  }
  $reply=ConvertFrom-Json (ConvertTo-Json $State.events[$eventKey].receipt -Depth 80 -Compress) -AsHashtable -DateKind String
  if($State.mutation.StartsWith('receipt-', [StringComparison]::Ordinal)){$reply[$State.mutation.Substring(8)]='wrong'}
  return $reply
 }
 if($Operation -ceq 'ReadAudit'){
  $eventKey=$key+'|'+$InputObject.eventId
  if(-not $State.events.ContainsKey($eventKey)){return $null}
  return ConvertFrom-Json (ConvertTo-Json $State.events[$eventKey].receipt -Depth 80 -Compress) -AsHashtable -DateKind String
 }
 if($Operation -cin @('Complete','Fail')){
  if(-not $State.turns.ContainsKey($key)){throw 'Unknown model terminal target'}
  $State.turns[$key].status=if($Operation -ceq 'Complete'){'completed'}else{'failed'}
  if($Operation -ceq 'Complete'){$State.turns[$key].response=$InputObject.response}
  else{$State.turns[$key].failureNote=$InputObject.failureNote}
  if($State.mutation -ceq 'summary-final'){$State.turns[$key].response='summary'}
  if($State.mutation -ceq 'timeout-after-complete'){throw 'Scripted timeout after committed terminal write'}
  return @{code='merged'}
 }
 throw 'Unknown scripted SessionLog operation'
}
