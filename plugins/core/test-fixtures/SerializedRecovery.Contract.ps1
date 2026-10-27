#Requires -Version 7.5
# SPDX-License-Identifier: Apache-2.0
[CmdletBinding(SupportsShouldProcess)]
param([ValidateSet('Mock','Production')][string]$Mode='Mock',[Parameter(Mandatory)][string]$ReceiptPath,[scriptblock]$ProductionInvoke)
$ErrorActionPreference='Stop'
if(Test-Path -LiteralPath $ReceiptPath){throw 'Chronological receipt exists'}
if($Mode -ceq 'Production' -and -not $ProductionInvoke){throw 'Actual existing serialized store provider required'}
Write-Output 'Counted effects:14 preserved original recovery inputs plus7 storage boundary inputs,11 rejecting interaction controls,one chronological receipt; Mock uses scripted storage dependency and independent recovery model. Production must bind SAME assertions to existing store lock and complete snapshot.'
if(-not $PSCmdlet.ShouldProcess($ReceiptPath,'Verify serialized original-pair recovery without changing a turn')){return}
Import-Module PowerShell.MCP -ErrorAction Stop
. (Join-Path $PSScriptRoot 'GovernedRecoverySelection.Model.ps1')
. (Join-Path $PSScriptRoot 'SerializedRecovery.Model.ps1')
$original=Join-Path $PSScriptRoot 'GovernedRecoverySelection.Inputs.json'
if((Get-FileHash $original).Hash -cne '189B7AA93E446969CA8CC77F3C6EBFA2E463837B92C7B9D4CFBD81ADC40D8E5F'){throw 'Original14inputs receipt changed'}
$inputs=[IO.File]::ReadAllText($original)|ConvertFrom-Json -AsHashtable -DateKind String
$fields=@('workspacePath','agentIdentity','mcpSessionId','hostSessionKey','keySource','ingressNonce','requestId','boundAtUtc')
$scenarios=[Collections.Generic.List[object]]::new()
foreach($item in $inputs.cases){
    $snapshot=ConvertFrom-Json $item.inputSnapshotJson -AsHashtable -DateKind String
    $store=@{schemaVersion=1;workspacePath=$snapshot.workspacePath;agentIdentity=$snapshot.agentIdentity;aliasConsumers=@('qb-audit-outbox-v1');revision=17;hosts=$snapshot.hosts;aliases=@()}
    foreach($alias in $snapshot.aliases){
        $complete=@{};$originalTurn=$snapshot.hosts[0].turns[0]
        foreach($field in $fields){$complete[$field]=$originalTurn[$field]}
        foreach($key in $alias.Keys){$complete[$key]=$alias[$key]}
        $complete.aliasedAtUtc='2026-01-01T00:00:01Z';$complete.acks=@();$store.aliases+=@($complete)
    }
    $code=switch -CaseSensitive ($item.name){
        {$_ -cin @('same-pair','no-key-pair','other-host-pair','other-session-same-request','pending-alias')}{'resolved'}
        'duplicate-pair'{'request_ambiguous'}
        {$_ -cin @('foreign-workspace','foreign-agent')}{'context_mismatch'}
        'wrong-alias-owner'{'store_corrupt'}
        default{'request_unknown'}
    }
    $expected=@{};foreach($field in $fields){$expected[$field]=$inputs.cases[0].result.binding[$field]}
    $scenario=@{name=$item.name;context=@{workspacePath='F:/fixture/recovery';agentIdentity='Codex';aliasConsumers=@('qb-audit-outbox-v1')};serialized=(ConvertTo-Json $store -Depth 100 -Compress);trustedPair=$item.trustedPair;hostSessionKey=$snapshot.currentHostSessionKey;toolParams=$snapshot.untrustedToolParams;lockAvailable=$true;code=$code;expected=$expected}
    $scenarios.Add($scenario)
}
foreach($name in @('missing-store','terminal-pair','undelivered-pair','corrupt-json','corrupt-binding','corrupt-alias-ack','lock-busy')){
    $scenario=ConvertFrom-Json (ConvertTo-Json $scenarios[0] -Depth 100 -Compress) -AsHashtable -DateKind String
    $scenario.name=$name;$state=ConvertFrom-Json $scenario.serialized -AsHashtable -DateKind String
    switch -CaseSensitive ($name){
        'missing-store'{$scenario.serialized=$null;$scenario.code='request_unknown'}
        'terminal-pair'{$state.hosts[0].turns[0].status='terminal'}
        'undelivered-pair'{$state.hosts[0].turns[0].delivered=$false}
        'corrupt-json'{$scenario.serialized='{not-json';$scenario.code='store_corrupt'}
        'corrupt-binding'{$state.hosts[0].turns[0].Remove('ingressNonce');$scenario.code='store_corrupt'}
        'corrupt-alias-ack'{
            $alias=@{};foreach($field in $fields){$alias[$field]=$state.hosts[0].turns[0][$field]}
            $alias.pendingId='pending-original-nonce';$alias.aliasedAtUtc='2026-01-01T00:00:01Z';$alias.acks=@('unknown-consumer');$state.aliases=@($alias);$scenario.code='store_corrupt'
        }
        'lock-busy'{$scenario.lockAvailable=$false;$scenario.code='store_unavailable'}
    }
    if($name -cnotin @('missing-store','corrupt-json')){$scenario.serialized=ConvertTo-Json $state -Depth 100 -Compress}
    $scenarios.Add($scenario)
}
$script:serializedChecks=0
function Assert-SerializedRecovery {
    param([bool]$Condition,[string]$Description)
    $script:serializedChecks++;if(-not $Condition){throw ('SERIALIZED_ASSERT: '+$Description)}
}
function Invoke-SerializedRecoveryCase {
    param([hashtable]$Scenario,[string]$Mutation='')
    $inputCopy=ConvertTo-Json $Scenario -Depth 100 -Compress
    $observed=if($Mode -ceq 'Mock' -or $Mutation){Invoke-ModelSerializedRecovery $Scenario $Mutation}else{& $ProductionInvoke $Scenario}
    $result=$observed.result;$success=$Scenario.code -ceq 'resolved'
    Assert-SerializedRecovery ($result.Count -eq 6 -and $result.aliases -is [array] -and $result.aliases.Count -eq 0) 'complete existing store result schema'

    Assert-SerializedRecovery ($result.ok -is [bool] -and $result.ok -eq $success -and $result.code -ceq $Scenario.code) 'exact storage selection/refusal'
    Assert-SerializedRecovery ($result.retryable -is [bool] -and $result.retryable -eq ($Scenario.code -ceq 'store_unavailable')) 'retry classification'
    Assert-SerializedRecovery ($observed.before -ceq $Scenario.serialized -and $observed.after -ceq $Scenario.serialized -and $observed.writes -eq 0) 'selection preserves exact snapshot bytes'
    Assert-SerializedRecovery ((ConvertTo-Json $Scenario -Depth 100 -Compress) -ceq $inputCopy) 'provider preserves complete scenario input'
    if($success){
        Assert-SerializedRecovery ($result.binding.Count -eq 8 -and ([Collections.IDictionary]$result.binding).IsReadOnly) 'complete immutable binding'
        foreach($field in $fields){Assert-SerializedRecovery ($result.binding[$field] -ceq $Scenario.expected[$field]) ('captured '+$field)}
    }else{Assert-SerializedRecovery ($null -eq $result.binding) 'refusal grants no target'}
    if($Scenario.code -cin @('context_mismatch','store_corrupt','store_unavailable')){Assert-SerializedRecovery ($null -eq $result.state) 'invalid storage grants no snapshot'}
    elseif($null -ne $Scenario.serialized){Assert-SerializedRecovery ((ConvertTo-Json $result.state -Depth 100 -Compress) -ceq $Scenario.serialized) 'entire snapshot and acknowledgement/revision retained'}
    else{Assert-SerializedRecovery ($null -ne $result.state -and $result.state.Count -eq 7 -and $result.state.schemaVersion -eq 1 -and $result.state.revision -eq 0 -and $result.state.workspacePath -ceq $Scenario.context.workspacePath -and $result.state.agentIdentity -ceq $Scenario.context.agentIdentity -and ($result.state.aliasConsumers -join '|') -ceq ($Scenario.context.aliasConsumers -join '|') -and $result.state.hosts.Count -eq 0 -and $result.state.aliases.Count -eq 0) 'missing store returns original empty snapshot without publishing'}
    return @{name=$Scenario.name;input=$Scenario;observed=$observed}
}
$results=[Collections.Generic.List[object]]::new();$failures=[Collections.Generic.List[object]]::new();$controls=[Collections.Generic.List[object]]::new()
foreach($scenario in $scenarios){try{$results.Add((Invoke-SerializedRecoveryCase $scenario))}catch{$failures.Add(@{name=$scenario.name;message=$_.Exception.Message;stack=$_.ScriptStackTrace})}}
$attacks=@(@{mutation='bypass-lock';case='lock-busy'},@{mutation='ignore-context';case='foreign-agent'},@{mutation='ignore-corrupt';case='corrupt-binding'},@{mutation='tool-pair-authority';case='tool-pair-spoof'},@{mutation='latest-pair';case='other-session-same-request'},@{mutation='first-duplicate';case='duplicate-pair'},@{mutation='write-selection';case='same-pair'},@{mutation='ignore-context';case='foreign-workspace'},@{mutation='drop-empty-state';case='missing-store'},@{mutation='mutate-ack';case='pending-alias'},@{mutation='mutable-binding';case='same-pair'})
$requiredAssertions=@{
 'bypass-lock'='exact storage selection/refusal';'ignore-context'='exact storage selection/refusal';'ignore-corrupt'='exact storage selection/refusal';'tool-pair-authority'='exact storage selection/refusal'
 'latest-pair'='captured mcpSessionId';'first-duplicate'='exact storage selection/refusal';'write-selection'='selection preserves exact snapshot bytes'
 'drop-empty-state'='missing store returns original empty snapshot without publishing';'mutate-ack'='entire snapshot and acknowledgement/revision retained';'mutable-binding'='complete immutable binding'
}
foreach($attack in $attacks){$reason=$null;try{Invoke-SerializedRecoveryCase @($scenarios|Where-Object name -CEQ $attack.case)[0] $attack.mutation|Out-Null}catch{$reason=$_.Exception.Message};$requiredReason='SERIALIZED_ASSERT: '+$requiredAssertions[$attack.mutation];$controls.Add(@{mutation=$attack.mutation;case=$attack.case;reason=$reason;expectedAssertion=$requiredReason;rejected=($reason -ceq $requiredReason)});if($reason -cne $requiredReason){$failures.Add(@{name=$attack.mutation;message='Intended scenario assertion did not reject dependency/effect mutation';reason=$reason;expectedAssertion=$requiredReason})}}
$receipt=[ordered]@{timestamp=[DateTimeOffset]::Now.ToString('o');mode=$Mode;cases=$results.Count;plannedCases=$scenarios.Count;assertions=$script:serializedChecks;controls=$controls.ToArray();negativeControls=$controls.Count;failed=$failures.Count;skipped=0;failures=$failures.ToArray();results=$results.ToArray();sourcePins=@((Get-FileHash $PSCommandPath),(Get-FileHash (Join-Path $PSScriptRoot 'SerializedRecovery.Model.ps1')),(Get-FileHash (Join-Path $PSScriptRoot 'GovernedRecoverySelection.Model.ps1')),(Get-FileHash $original));requirements=@('FR-QB-003','FR-QB-005','TR-QB-AUDIT-001','TEST-QB-008','TEST-QB-009','TEST-QB-069');scope='Serialized read-only original-pair lookup only; preserves pure14/12/150 original gate, adds storage-specific strict refusals. No mutation permission/native ingress acceptance.';productComplete=$false}
$var1=$receipt|ConvertTo-Json -Depth 100;Add-LinesToFile $ReceiptPath -Content $var1|Out-Null
[pscustomobject]@{mode=$Mode;cases=$results.Count;planned=$scenarios.Count;assertions=$script:serializedChecks;controls=$controls.Count;failed=$failures.Count;skipped=0}
if($failures.Count){throw 'Serialized recovery assertions failed; full chronological receipt retained'}


