#Requires -Version 7.5
# SPDX-License-Identifier: Apache-2.0
<#
.SYNOPSIS
FR-QB-003/005 TEST-QB-069: trusted route, legacy batch and raw review handoff assertions.
.DESCRIPTION
Independent callback interaction model first. Actual provider must route the same
method/input through the actual entrypoint; no direct adapter-only acceptance.
#>
[CmdletBinding(SupportsShouldProcess)]
param([ValidateSet('Mock','Production')][string]$Mode='Mock',[Parameter(Mandatory)][string]$ReceiptPath,[scriptblock]$ProviderFactory)
$ErrorActionPreference='Stop'
if(Test-Path $ReceiptPath){throw 'Chronological receipt exists'}
if($Mode -ceq 'Production' -and -not $ProviderFactory){throw 'Actual entrypoint provider required'}
Write-Output 'Counted effects:31trusted-route/batch/review scenarios retaining original27inputs plus4full typed vectors,38rejecting controls and one chronological receipt. No production entrypoint acceptance from Mock.'
if(-not $PSCmdlet.ShouldProcess($ReceiptPath,'Execute same route inputs and assertions')){return}
Import-Module PowerShell.MCP -ErrorAction Stop
. (Join-Path $PSScriptRoot 'GovernedSessionRoute.Model.ps1')
$fidelity=Join-Path $PSScriptRoot 'GovernedPayloadFidelity.Contract.ps1'
$tokens=$null;$errors=$null;$ast=[Management.Automation.Language.Parser]::ParseFile($fidelity,[ref]$tokens,[ref]$errors)
if($errors.Count){throw 'Fidelity contract parser errors'}
$definition=$ast.Find({param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -ceq 'Get-FidelityInput'},$true)
. ([scriptblock]::Create($definition.Extent.Text))
$lines=[IO.File]::ReadAllLines($fidelity)
. ([scriptblock]::Create(($lines[98..110] -join "`n")))
$cases=[Collections.Generic.List[object]]::new()
foreach($governed in @($false,$true)){
 foreach($kind in @('actions','dialogItems')){
  $counter=0;$items=@(foreach($id in @('first','second','third')){$counter++;@{id=$id;description="  Actual **$id**`r`nkept  ";order=$counter;type='tool_execution';status='completed';role='model';category='note';content="  Actual **$id**`r`nkept  "}})
  $params=@{requestId='selected-original';workspacePath='F:/foreign';agentIdentity='Foreign';mcpSessionId='foreign';hostOptions=@{workspacePath='F:/injected'}};$params[$kind]=$items
  $cases.Add(@{name=$(if($governed){'governed'}else{'legacy'})+'-'+$kind;governed=$governed;method=$(if($kind -ceq 'actions'){'workflow.sessionlog.appendActions'}else{'workflow.sessionlog.appendDialog'});paramsJson=(ConvertTo-Json $params -Depth 100 -Compress);batchField=$kind})
 }
}
foreach($governed in @($false,$true)){
 foreach($kind in @('actions','dialogItems')){
  $counter=0;$items=@(foreach($id in @('first','second','third')){
   $position=$counter;$counter++
   @{id=$id;description="  Actual **$id**`r`nkept  ";content="  Actual **$id**`r`nkept  ";order=@(4,9,12)[$position];type=@('edit','validation','tool_execution')[$position];status=@('completed','in_progress','failed')[$position];filePath=('F:/fixture/route/'+$id+'-'+[char]0x03B2+'.md');role=@('model','tool','system')[$position];category=@('reasoning','tool_result','observation')[$position];timestamp=@('2026-01-01T01:02:03.0000001-05:00','2026-01-02T04:05:06.0000002+02:00','2026-01-03T07:08:09.0000003+00:00')[$position]}
  })
  $params=@{requestId='selected-original';workspacePath='F:/foreign';agentIdentity='Foreign';mcpSessionId='foreign';hostOptions=@{workspacePath='F:/injected'}};$params[$kind]=$items
  $cases.Add(@{name=$(if($governed){'governed'}else{'legacy'})+'-fields-'+$kind;governed=$governed;method=$(if($kind -ceq 'actions'){'workflow.sessionlog.appendActions'}else{'workflow.sessionlog.appendDialog'});paramsJson=(ConvertTo-Json $params -Depth 100 -Compress);batchField=$kind})
 }
}
$cases.Add(@{name='trusted-begin';governed=$true;method='workflow.sessionlog.beginTurn';paramsJson=(ConvertTo-Json @{queryTitle='Actual request';workspacePath='F:/foreign';agentIdentity='Foreign';mcpSessionId='foreign';hostOptions=@{workspacePath='F:/injected'}} -Depth 30 -Compress)})
foreach($specification in $specifications){foreach($literal in @($false,$true)){
 $vector=Get-FidelityInput -Lexeme $specification.lexeme -Kind $specification.kind -LiteralUnicode $literal
 $cases.Add(@{name='review-'+$specification.name+$(if($literal){'-literal'}else{'-escaped'});governed=$true;method='workflow.sessionlog.recordReview';paramsJson=(ConvertTo-Json @{requestId='selected-original';reviewJson=$vector.json} -Depth 100 -Compress)})
}}
$script:routeAssertions=0
function Assert-RouteCondition {param([bool]$Condition,[string]$Message) $script:routeAssertions++;if(-not $Condition){throw ('ROUTE_ASSERT: '+$Message)}}
function Invoke-RouteScenario {
 param([Collections.IDictionary]$Case,[string]$Mutation='')
 $capture=@{workspacePath='F:/fixture/route';agentIdentity='Codex';mcpSessionId='Codex-fixture-original';hostSessionKey='native-fixture';ingressNonce='nonce-fixture';boundAtUtc='2026-01-01T00:00:00Z'}
 $provider=if($Mode -ceq 'Mock'){Get-MockSessionRoute -TrustedCapture $capture -Governed $Case.governed -Mutation $Mutation}else{& $ProviderFactory $capture $Case.governed}
 $null=& $provider.Invoke $Case.method $Case.paramsJson
 $effects=@(& $provider.Inspect)
 Assert-RouteCondition ($effects.Count -eq 1) 'Exactly one actual selected dependency call'
 $effect=$effects[0]
 Assert-RouteCondition ($effect.route -ceq $(if($Case.governed){'governed'}else{'legacy'})) 'Governed selection is trusted constructor state; generic legacy route preserved'
 foreach($field in $capture.Keys){Assert-RouteCondition ($effect.captured[$field] -ceq $capture[$field]) ('Captured root cannot come from tool params: '+$field)}
 Assert-RouteCondition ($effect.method -ceq $Case.method) 'Actual selected method unchanged'
 $expected=ConvertFrom-Json $Case.paramsJson -AsHashtable -DateKind String;$actual=ConvertFrom-Json $effect.paramsJson -AsHashtable -DateKind String
 if($Case.Contains('batchField')){
  $field=$Case.batchField
  Assert-RouteCondition (@($effect.typedBatch).Count -eq @($expected[$field]).Count) 'Actual typed callback receives every batch item'
  $identityField=if($field -ceq 'actions'){'description'}else{'content'}
  for($index=0;$index -lt @($expected[$field]).Count;$index++){Assert-RouteCondition ($effect.typedBatch[$index][$identityField] -ceq $expected[$field][$index][$identityField]) 'Actual typed callback retains original item text/order'}
  for($index=0;$index -lt @($expected[$field]).Count;$index++){
    $typed=$effect.typedBatch[$index];$original=$expected[$field][$index]
    $typedFields=if($field -ceq 'actions'){@('order','type','status','filePath')}else{@('role','category','timestamp')}
    foreach($typedField in $typedFields){
     if(-not $original.Contains($typedField)){continue}
     $valid=$typed.Contains($typedField)
     if($typedField -ceq 'order'){$valid=$valid -and ($typed[$typedField] -is [int] -or $typed[$typedField] -is [long]) -and $typed[$typedField] -eq $original[$typedField]}
     elseif($typedField -ceq 'timestamp'){
      $seenTime=[DateTimeOffset]::MinValue;$originalTime=[DateTimeOffset]::MinValue
      $valid=$valid -and $typed[$typedField] -is [string] -and [DateTimeOffset]::TryParse($typed[$typedField],[ref]$seenTime) -and [DateTimeOffset]::TryParse($original[$typedField],[ref]$originalTime) -and $seenTime.UtcTicks -eq $originalTime.UtcTicks -and $seenTime.Offset -eq $originalTime.Offset
     }else{$valid=$valid -and $typed[$typedField] -is [string] -and $typed[$typedField] -ceq $original[$typedField]}
     Assert-RouteCondition $valid ('Actual typed field retains '+$typedField)
    }
   }
   Assert-RouteCondition (@($actual[$field]).Count -eq @($expected[$field]).Count) 'Every legacy/governed batch item reaches dependency'
  Assert-RouteCondition ((@($actual[$field].id)-join '|') -ceq (@($expected[$field].id)-join '|')) 'Original batch order reaches dependency'
 }
 if($expected.Contains('reviewJson')){Assert-RouteCondition ($actual.reviewJson -ceq $expected.reviewJson) 'Raw full review JSON including numeric lexemes and CR preserved'}
 Assert-RouteCondition ($effect.paramsJson -ceq $Case.paramsJson) 'Full exact method parameters reach dependency'
 return @{name=$Case.name;effects=$effects}
}
$results=[Collections.Generic.List[object]]::new();$rejections=[Collections.Generic.List[object]]::new();$failures=[Collections.Generic.List[object]]::new()
foreach($case in $cases){try{$results.Add((Invoke-RouteScenario $case))}catch{$failures.Add(@{case=$case.name;error=$_.Exception.ToString();stack=$_.ScriptStackTrace})}}
if($Mode -ceq 'Mock'){
 $plannedControls=@(@('trusted-begin','tool-root'),@('trusted-begin','legacy-in-governed'),@('legacy-actions','governed-in-generic'),@('legacy-actions','omit-tail'),@('legacy-dialogItems','reverse'),@('governed-actions','omit-tail'),@('governed-dialogItems','reverse'),@('review-precision28-literal','coerce-review'),@('legacy-actions','omit-typed-tail'),@('legacy-dialogItems','reverse-typed'))
 foreach($prefix in @('legacy','governed')){foreach($kind in @('actions','dialogItems')){
  $fields=if($kind -ceq 'actions'){@('order','type','status','filePath')}else{@('role','category','timestamp')}
  foreach($field in $fields){foreach($fault in @('wrong','missing')){$plannedControls+=,@(($prefix+'-fields-'+$kind),('typed-'+$kind+'-'+$field+'-'+$fault))}}
 }}
 foreach($control in $plannedControls){
  $case=@($cases|Where-Object name -CEQ $control[0])[0]
  try{$null=Invoke-RouteScenario $case $control[1];$failures.Add(@{control=$control;error='Wrong dependency escaped'})}
  catch{
   $expectedField=if($control[1].StartsWith('typed-',[StringComparison]::Ordinal)){$control[1].Split('-')[2]}else{''}
   $intended=$_.Exception.Message.StartsWith('ROUTE_ASSERT:',[StringComparison]::Ordinal) -and (-not $expectedField -or $_.Exception.Message -ceq ('ROUTE_ASSERT: Actual typed field retains '+$expectedField))
   if($intended){$rejections.Add(@{case=$control[0];mutation=$control[1];rejection=$_.Exception.Message})}else{$failures.Add(@{control=$control;error=$_.Exception.ToString();stack=$_.ScriptStackTrace})}
  }
 }
}
$receipt=@{timestamp=Get-Date -Format o;mode=$Mode;requirements=@('FR-QB-003','FR-QB-005','TR-QB-AUDIT-001','AC-QB-007','AC-QB-008','AC-QB-009','AC-QB-069','TEST-QB-069');plannedCases=$cases.Count;cases=$results.Count;assertions=$script:routeAssertions;rejectingControls=$rejections.Count;failed=$failures.Count;skipped=0;results=$results.ToArray();controls=$rejections.ToArray();failures=$failures.ToArray();sourceHashes=@(Get-FileHash $PSCommandPath,(Join-Path $PSScriptRoot 'GovernedSessionRoute.Model.ps1'),$fidelity|Select-Object Path,Hash);scope='Actual route provider required for Production; Mock establishes assertion discrimination only';productComplete=$false}
$var1=ConvertTo-Json $receipt -Depth 100
Add-LinesToFile $ReceiptPath -Content $var1|Out-Null
[pscustomobject]@{mode=$Mode;cases=$results.Count;assertions=$script:routeAssertions;controls=$rejections.Count;failed=$failures.Count;receipt=$ReceiptPath}
if($failures.Count){throw 'Route prerequisite failed; original evidence retained'}
