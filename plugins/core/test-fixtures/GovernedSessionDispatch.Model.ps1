# SPDX-License-Identifier: Apache-2.0
<#
.SYNOPSIS
Independent test-only native method decoder over the existing host interaction model.
.DESCRIPTION
Reads serialized invocation inputs, not scenario names or expected result objects.
Trusted host capture is a separate constructor argument, never a model parameter.
No production lifecycle, REPL dispatcher, normalizer or transport is imported.
#>
function Invoke-MockSessionDispatch {
 [CmdletBinding()]
 param([hashtable]$Envelope,[hashtable]$TrustedCapture,[hashtable]$State,[hashtable]$ServerState,[string]$Mutation='',[scriptblock]$HostModel,[scriptblock]$TransportModel)
 $arguments=$Envelope.params
 $context=ConvertFrom-Json (ConvertTo-Json $TrustedCapture -Depth 90 -Compress) -AsHashtable -DateKind String
 if($Mutation -ceq 'tool-roots' -and $arguments.Contains('workspacePath')){$context.workspacePath=$arguments.workspacePath}
 $data=@{}
 if($arguments.Contains('requestId')){$data.requestId=$arguments.requestId}
 switch -CaseSensitive ($Envelope.method){
  'workflow.sessionlog.mintTurn' {$operation='mintTurn';$data.queryTitle=$arguments.queryTitle}
  'workflow.sessionlog.beginTurn' {$operation='beginTurn';$data.queryTitle=$arguments.queryTitle}
  'workflow.sessionlog.currentSession' {$operation='resolve'}
  'workflow.sessionlog.appendActions' {
   if(@($arguments.actions).Count -ne 1){return @{ok=$false;code='invalid_test_input'}}
   $operation='appendAction';$data.payloadJson=ConvertTo-Json $arguments.actions[0] -Depth 90 -Compress
  }
  'workflow.sessionlog.appendDialog' {
   if(@($arguments.dialogItems).Count -ne 1){return @{ok=$false;code='invalid_test_input'}}
   $operation='appendDialog';$data.payloadJson=ConvertTo-Json $arguments.dialogItems[0] -Depth 90 -Compress
  }
  'workflow.sessionlog.updateTurn' {$operation='decision';$data.payloadJson=$arguments.decisionJson}
  'workflow.sessionlog.recordReview' {$operation='recordReview';$data.payloadJson=$arguments.reviewJson}
  'workflow.sessionlog.completeTurn' {$operation='completeTurn';$data.response=$arguments.response}
  'workflow.sessionlog.failTurn' {$operation='failTurn';$data.failureNote=$arguments.errorMessage}
  default {return @{ok=$false;code='method_not_found'}}
 }
 $transportState=$ServerState
 $modelServer=$TransportModel
 $server={param($Operation,$Binding,$Data) & $modelServer -State $transportState -Operation $Operation -Root $Binding -InputObject $Data}.GetNewClosure()
 return & $HostModel -State $State -HostContext $context -Operation $operation -InputObject $data -Server $server -Mutation $Mutation
}

