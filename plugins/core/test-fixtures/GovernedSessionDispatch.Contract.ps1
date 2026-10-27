#Requires -Version 7.5
# SPDX-License-Identifier: Apache-2.0
<#
.SYNOPSIS
Reuse the frozen32 host assertions through serialized S6/legacy method invocations.
.DESCRIPTION
Mock uses only independent host/storage/transport models and physical JSON handoff.
Production must supply an actual native dispatcher provider; no model fallback.
The shared function's external-provider selection branch is named Production;
that internal branch selects a callback and is not this receipt's evidence mode.
#>
[CmdletBinding(SupportsShouldProcess)]
param([ValidateSet('Mock','Production')][string]$EvidenceMode='Mock',[Parameter(Mandatory)][string]$ReceiptPath,[Parameter(Mandatory)][string]$ArtifactRoot,[scriptblock]$NativeProviderFactory)
$ErrorActionPreference='Stop'
if(Test-Path $ReceiptPath){throw 'Chronological receipt exists'}
if($EvidenceMode -ceq 'Production' -and -not $NativeProviderFactory){throw 'Actual native entrypoint provider required'}
$shared=Join-Path $PSScriptRoot 'GovernedSessionHost.Contract.ps1'
if((Get-FileHash $shared).Hash -cne '38CCD62D5DB8CC244D3A126DD9E77D474B7127500CF2D64172FD5F90B415CF1A'){throw 'Frozen common inputs/assertions changed'}
Write-Output 'Counted effects: SAME32 frozen host scenarios through serialized method/params; independent model mode includes original9 controls plus5 native input mutations; retained physical invocation/reply files and one native receipt. No compiled/native acceptance from Mock.'
if(-not $PSCmdlet.ShouldProcess($ArtifactRoot,'Execute common host assertions through method boundary')){return}
Import-Module PowerShell.MCP -ErrorAction Stop
$tokens=$null;$errors=$null
$ast=[Management.Automation.Language.Parser]::ParseFile($shared,[ref]$tokens,[ref]$errors)
if($errors.Count){throw 'Frozen common runner parse error'}
foreach($name in @('Assert-SessionHostContract','Get-SessionHostReviewJson','Invoke-SessionHostScenario')){
 $definition=$ast.Find({param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -ceq $name},$true)
 if(-not $definition){throw 'Frozen common function missing'}
 . ([scriptblock]::Create($definition.Extent.Text))
}
$sharedLines=[IO.File]::ReadAllLines($shared)
. ([scriptblock]::Create(($sharedLines[149..150] -join "`n")))
if($names.Count -ne 32){throw 'All original32 scenario names required'}
if($EvidenceMode -ceq 'Mock'){
 . (Join-Path $PSScriptRoot 'GovernedSessionHost.Model.ps1')
 . (Join-Path $PSScriptRoot 'GovernedSessionHost.TransportModel.ps1')
 . (Join-Path $PSScriptRoot 'GovernedSessionDispatch.Model.ps1')
}
$script:hostAssertions=0
$dispatchControl=@{mutation=''}
$dispatchCode=@{decoder=(Get-Command Invoke-MockSessionDispatch -ErrorAction SilentlyContinue).ScriptBlock;host=(Get-Command Invoke-MockGovernedSessionHost -ErrorAction SilentlyContinue).ScriptBlock;transport=(Get-Command Invoke-MockSessionHostTransport -ErrorAction SilentlyContinue).ScriptBlock}
$dispatchEvidence=[Collections.Generic.List[object]]::new()
$Mode='Production' # Existing function's external provider branch; see evidenceMode.
$modeForProvider=$EvidenceMode;$nativeFactoryForProvider=$NativeProviderFactory
$ProviderFactory={param($Root,$State,$ServerState)
  $providerMode=$modeForProvider
  $native=if($providerMode -ceq 'Production'){& $nativeFactoryForProvider $Root $State $ServerState}else{$null}
  $providerState=$State;$providerServerState=$ServerState;$providerEvidence=$dispatchEvidence
  $mutation=$dispatchControl.mutation;$decoder=$dispatchCode.decoder;$hostModel=$dispatchCode.host;$transportModel=$dispatchCode.transport
  $io=@{serial=0};[void][IO.Directory]::CreateDirectory($Root)
  $invoke={param($Context,$Operation,$Data)
   $methods=@{mintTurn='workflow.sessionlog.mintTurn';beginTurn='workflow.sessionlog.beginTurn';resolve='workflow.sessionlog.currentSession';appendAction='workflow.sessionlog.appendActions';appendDialog='workflow.sessionlog.appendDialog';decision='workflow.sessionlog.updateTurn';recordReview='workflow.sessionlog.recordReview';completeTurn='workflow.sessionlog.completeTurn';failTurn='workflow.sessionlog.failTurn'}
   if(-not $methods.ContainsKey($Operation)){throw 'Unknown common operation'}
   $parameters=@{};foreach($key in $Data.Keys){$parameters[$key]=$Data[$key]}
   switch -CaseSensitive ($Operation){
    'appendAction' {$parameters.actions=@((ConvertFrom-Json $Data.payloadJson -AsHashtable -DateKind String));$parameters.Remove('payloadJson')}
    'appendDialog' {$parameters.dialogItems=@((ConvertFrom-Json $Data.payloadJson -AsHashtable -DateKind String));$parameters.Remove('payloadJson')}
    'decision' {$parameters.decisionJson=$Data.payloadJson;$parameters.Remove('payloadJson')}
    'recordReview' {$parameters.reviewJson=$Data.payloadJson;$parameters.Remove('payloadJson')}
    'failTurn' {$parameters.errorMessage=$Data.failureNote;$parameters.Remove('failureNote')}
   }
   $method=$methods[$Operation]
   if($mutation -ceq 'wrong-native-method' -and $Operation -ceq 'beginTurn'){$method='workflow.sessionlog.unknown'}
   if($mutation -ceq 'complete-as-resolve' -and $Operation -ceq 'completeTurn'){$method='workflow.sessionlog.currentSession'}
   if($mutation -ceq 'drop-explicit-target'){$parameters.Remove('requestId')}
   $trusted=ConvertFrom-Json (ConvertTo-Json $Context -Depth 90 -Compress) -AsHashtable -DateKind String
   if($mutation -ceq 'raw-review-coercion' -and $Operation -ceq 'recordReview'){$parameters.reviewJson=ConvertTo-Json (ConvertFrom-Json $parameters.reviewJson -AsHashtable -DateKind String) -Depth 90 -Compress}
   $io.serial++;$path=Join-Path $Root ('invocation-'+$io.serial+'.json')
   $var1=@{requestId=('wire-'+$io.serial);method=$method;params=$parameters}|ConvertTo-Json -Depth 100 -Compress
   Add-LinesToFile $path -Content $var1|Out-Null
   $decoded=ConvertFrom-Json ([IO.File]::ReadAllText($path)) -AsHashtable -DateKind String
   $providerState.effects.Add(@{operation='NativeDispatch';method=$decoded.method;invocationPath=$path;invocationHash=(Get-FileHash $path).Hash})
   $reply=if($providerMode -ceq 'Production'){& $native.Invoke $decoded $trusted}else{& $decoder -Envelope $decoded -TrustedCapture $trusted -State $providerState -ServerState $providerServerState -Mutation $mutation -HostModel $hostModel -TransportModel $transportModel}
   $replyPath=Join-Path $Root ('reply-'+$io.serial+'.json')
   $var1=ConvertTo-Json $reply -Depth 100 -Compress
   Add-LinesToFile $replyPath -Content $var1|Out-Null
   $providerEvidence.Add(@{method=$decoded.method;operation=$Operation;constructorCapture=$trusted;invocationPath=$path;invocationHash=(Get-FileHash $path).Hash;replyPath=$replyPath;replyHash=(Get-FileHash $replyPath).Hash})
   return ConvertFrom-Json ([IO.File]::ReadAllText($replyPath)) -AsHashtable -DateKind String
  }.GetNewClosure()
$inspect={
    if($native){return & $native.Inspect}
   $snapshot=if(Test-Path (Join-Path $providerState.storeDirectory 'turn-store.json')){ConvertFrom-Json ([IO.File]::ReadAllText((Join-Path $providerState.storeDirectory 'turn-store.json'))) -AsHashtable -DateKind String}else{$null}
   return @{store=$snapshot;outboxes=$providerState.outboxes;server=$providerServerState;tape=$providerState.effects.ToArray()}
  }.GetNewClosure()
  return @{Invoke=$invoke;Inspect=$inspect}
}.GetNewClosure()
if(-not $ProviderFactory){throw 'Selected scenario provider is missing'}
$results=[Collections.Generic.List[object]]::new();$controls=[Collections.Generic.List[object]]::new();$failures=[Collections.Generic.List[object]]::new()
foreach($name in $names){try{$results.Add((Invoke-SessionHostScenario $name))}catch{$failures.Add(@{case=$name;error=$_.Exception.ToString();scriptStackTrace=$_.ScriptStackTrace})}}
if($EvidenceMode -ceq 'Mock'){
 $faults=@(@('summary-ingress','ignore-ingress-readback'),@('mint-only','skip-delivery-guard'),@('explicit-original-session','target-latest'),@('decision-replay','duplicate-decision'),@('policy-continue','omit-completion-gate'),@('policy-blocked','omit-completion-gate'),@('summary-final','skip-final-readback'),@('wrong-terminal-root','skip-final-readback'),@('receipt-payloadHash','ignore-submit-receipt'),@('complete-exact-text','wrong-native-method'),@('complete-exact-text','complete-as-resolve'),@('explicit-original-session','drop-explicit-target'),@('review-lossless','raw-review-coercion'),@('complete-exact-text','tool-roots'))
 foreach($fault in $faults){
  $dispatchControl.mutation=$fault[1]
  try{$null=Invoke-SessionHostScenario $fault[0];$failures.Add(@{control=$fault;error='Wrong native dependency/effect escaped'})}catch{if($_.Exception.Message.StartsWith('HOST_ASSERT:',[StringComparison]::Ordinal)){$controls.Add(@{case=$fault[0];mutation=$fault[1];rejection=$_.Exception.Message})}else{$failures.Add(@{control=$fault;error=$_.Exception.ToString();scriptStackTrace=$_.ScriptStackTrace})}}
 }
}
$receipt=[ordered]@{timestamp=Get-Date -Format o;mode=$EvidenceMode;sharedProviderBranch=$Mode;requirements=@('FR-QB-003','FR-QB-005','TR-QB-AUDIT-001','AC-QB-005','AC-QB-007','AC-QB-008','AC-QB-009','AC-QB-012','AC-QB-014','AC-QB-069');plannedCases=$names.Count;cases=$results.Count;assertions=$script:hostAssertions;rejectingControls=$controls.Count;failed=$failures.Count;skipped=0;results=$results.ToArray();controls=$controls.ToArray();failures=$failures.ToArray();invocations=$dispatchEvidence.ToArray();sourceHashes=@(Get-FileHash $PSCommandPath,$shared,(Join-Path $PSScriptRoot 'GovernedSessionDispatch.Model.ps1'),(Join-Path $PSScriptRoot 'GovernedSessionHost.Model.ps1'),(Join-Path $PSScriptRoot 'GovernedSessionHost.TransportModel.ps1')|Select-Object Path,Hash);scope='S6/legacy method handoff prerequisite only; generic compatibility, actual dispatcher/native process/SQLite/lifecycle/fullscope remain open';productComplete=$false}
$var1=ConvertTo-Json $receipt -Depth 100
Add-LinesToFile $ReceiptPath -Content $var1|Out-Null
[pscustomobject]@{mode=$EvidenceMode;cases=$results.Count;assertions=$script:hostAssertions;controls=$controls.Count;failed=$failures.Count;receipt=$ReceiptPath}
if($failures.Count){throw 'Native method prerequisite failed; chronological evidence retained'}



