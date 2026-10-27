#Requires -Version 7.5
# SPDX-License-Identifier: Apache-2.0
<#
.SYNOPSIS
AC/TEST-QB-005/007/009/069: SAME composed host interruption/restart assertions.
.DESCRIPTION
Model mode proves the runner with independent input-only state transitions and
qualified wrong dependency effects. Production changes only the provider seam.
#>
[CmdletBinding(SupportsShouldProcess)]
param([ValidateSet('Mock','Production')][string]$Mode='Mock',[Parameter(Mandatory)][string]$ReceiptPath,[Parameter(Mandatory)][string]$ArtifactRoot,[scriptblock]$ProviderFactory)
$ErrorActionPreference='Stop'
if(Test-Path $ReceiptPath){throw 'Chronological receipt already exists'}
Write-Output 'Counted effects:8 isolated synthetic composed host interruptions/restarts, complete snapshots/interaction evidence and4 test-only rejecting controls.'
if(-not $PSCmdlet.ShouldProcess($ArtifactRoot,'Run composed host restart assertions')){return}
Import-Module PowerShell.MCP -ErrorAction Stop
if($Mode -ceq 'Mock'){. (Join-Path $PSScriptRoot 'GovernedSessionRestart.Model.ps1')}elseif(-not $ProviderFactory){throw 'Canonical restart provider required'}
$script:restartAssertions=0
function Assert-SessionRestartContract {
    param([bool]$Condition,[string]$Message)
    $script:restartAssertions++
    if(-not $Condition){throw ('RESTART_ASSERT: '+$Message)}
}
function Invoke-SessionRestartScenario {
    param([string]$Fault,[string]$Mutation='')
    $root=Join-Path $ArtifactRoot ([guid]::NewGuid().ToString('N'))
    $provider=if($Mode -ceq 'Mock'){Get-MockSessionRestartProvider -Root $root -Fault $Fault -Mutation $Mutation}else{& $ProviderFactory $root $Fault $Mutation}
    $context=@{workspacePath='F:/fixture/restart';agentIdentity='Codex';mcpSessionId='Codex-20260101T000000Z-restart';hostSessionKey='native-restart-one';keySource='native';ingressNonce='actual-nonce';boundAtUtc='2026-01-01T00:00:00Z';ingressText="  # Actual request $([char]0x03A9)`r`nFull **markdown** literal\n  ";actualIngress=$true;operationId='operation-ingress';requestClass='development'}
    $final=@{response="  # Actual final $([char]0x03A9)`r`nFull **markdown** literal\n  "}
    $terminal=$Fault -cin @('TerminalRead','TerminalTimeout')
    $first=$null;$firstError=''
    if($terminal){
        $begin=& $provider.Invoke $context 'beginTurn' @{queryTitle='Actual restart request'}
        Assert-SessionRestartContract $begin.ok 'Actual ingress is bound before terminal fault'
        $final.requestId=$begin.binding.requestId;$context.operationId='operation-terminal'
    }
    try{$first=& $provider.Invoke $context $(if($terminal){'completeTurn'}else{'beginTurn'}) $(if($terminal){$final}else{@{queryTitle='Actual restart request'}})}catch{$firstError=$_.Exception.ToString()}
    $before=ConvertFrom-Json (ConvertTo-Json -InputObject (& $provider.Inspect) -Depth 100 -Compress) -AsHashtable -DateKind String
    Assert-SessionRestartContract $before.faultHit ($Fault+': actual dependency fault was reached')
    if($Fault -ceq 'TerminalTimeout'){Assert-SessionRestartContract $first.ok 'Committed terminal timeout reconciles with independent full readback'}
    else{Assert-SessionRestartContract ($first.ok -ne $true) 'Interrupted operation never reports successful delivery/completion'}
    $originalEvents=ConvertFrom-Json (ConvertTo-Json -InputObject @($before.manifests|ForEach-Object {$_.expectedEvents}) -Depth 100 -Compress) -AsHashtable -NoEnumerate -DateKind String
    Assert-SessionRestartContract ($originalEvents.Count -ge 1) 'Full captured manifest survives before restart'
    & $provider.Restart
    if($Fault -cne 'TerminalTimeout'){
        $again=& $provider.Invoke $context $(if($terminal){'completeTurn'}else{'beginTurn'}) $(if($terminal){$final}else{@{queryTitle='Actual restart request'}})
        Assert-SessionRestartContract $again.ok 'Fresh worker reconstructs original binding and resumes accepted work'
        if(-not $terminal){
            $final.requestId=$again.binding.requestId;$context.operationId='operation-terminal'
            $finished=& $provider.Invoke $context 'completeTurn' $final
            Assert-SessionRestartContract $finished.ok 'All captured evidence verifies before completion after restart'
        }
    }
    $after=& $provider.Inspect
    Assert-SessionRestartContract ($after.restarts -eq 1) 'A new worker read retained durable state exactly once'
    $all=@($after.manifests|ForEach-Object {$_.expectedEvents})
    Assert-SessionRestartContract ($all.Count -eq 2 -and @($all.eventId|Select-Object -Unique).Count -eq 2 -and (@($all.order|Sort-Object)-join ',') -ceq '1,2') 'One ingress and one result keep distinct UUIDs and consecutive orders'
    foreach($original in $originalEvents){
        $found=@($all|Where-Object eventId -CEQ $original.eventId)
        Assert-SessionRestartContract ($found.Count -eq 1) 'Every originally captured UUID remains present exactly once'
        foreach($field in @('order','kind','payloadHash','payloadJson','workspacePath','agentIdentity','mcpSessionId')){Assert-SessionRestartContract ($found[0][$field] -ceq $original[$field]) ('Immutable original '+$field+' survives actual restart')}
    }
    $local=@($after.store.hosts|ForEach-Object {$_.turns})
    Assert-SessionRestartContract ($local.Count -eq 1 -and $local[0].status -ceq 'terminal' -and $local[0].delivered) 'Only original delivered host turn becomes terminal'
    foreach($field in @('workspacePath','agentIdentity','mcpSessionId','hostSessionKey','ingressNonce','keySource','boundAtUtc')){Assert-SessionRestartContract ($local[0][$field] -ceq $context[$field]) ('Captured '+$field+' never changes after restart')}
    $turns=@($after.server.turns.Values)
    Assert-SessionRestartContract ($turns.Count -eq 1 -and $turns[0].requestId -ceq $local[0].requestId -and $turns[0].mcpSessionId -ceq $context.mcpSessionId -and $turns[0].queryText -ceq $context.ingressText -and $turns[0].response -ceq $final.response -and $turns[0].status -ceq 'completed') 'Full ingress/final text and original server ownership remain exact'
    Assert-SessionRestartContract (@($after.server.events).Count -eq 1 -and $after.server.events.Count -eq 2) 'Exactly two immutable server audit UUIDs exist'
    Assert-SessionRestartContract (@($after.tape|Where-Object operation -CEQ 'Complete').Count -eq 1) 'Committed terminal write is never repeated after worker restart'
    Assert-SessionRestartContract (@($after.store.aliases).Count -eq 1 -and $after.store.aliases[0].pendingId -ceq 'pending-actual-nonce' -and $after.store.aliases[0].requestId -ceq $local[0].requestId -and 'qb-audit-outbox-v1' -cin $after.store.aliases[0].acks) 'One durable original alias is applied and acknowledged'
    return @{fault=$Fault;first=$first;firstError=$firstError;before=$before;after=$after;originalEvents=$originalEvents}
}
$faults=@('CaptureJournal','CaptureEvent','AliasCommitted','AliasApplied','ReceiptPublished','Retired','TerminalRead','TerminalTimeout')
$results=[Collections.Generic.List[object]]::new();$controls=[Collections.Generic.List[object]]::new();$failures=[Collections.Generic.List[object]]::new()
foreach($fault in $faults){try{$results.Add((Invoke-SessionRestartScenario $fault))}catch{$failures.Add(@{fault=$fault;error=$_.Exception.ToString()})}}
if($Mode -ceq 'Mock'){
    foreach($control in @(@('CaptureJournal','forget-capture'),@('CaptureEvent','forget-capture'),@('AliasCommitted','omit-alias-consume'),@('TerminalRead','repeat-terminal'))){
        try{$null=Invoke-SessionRestartScenario $control[0] $control[1];$failures.Add(@{control=$control;error='Wrong interaction/state escaped assertions'})}catch{if($_.Exception.Message.StartsWith('RESTART_ASSERT:',[StringComparison]::Ordinal)){$controls.Add(@{fault=$control[0];mutation=$control[1];rejection=$_.Exception.Message})}else{$failures.Add(@{control=$control;error=$_.Exception.ToString()})}}
    }
}
$var1=@{timestamp=Get-Date -Format o;mode=$Mode;plannedCases=$faults.Count;cases=$results.Count;assertions=$script:restartAssertions;rejectingControls=$controls.Count;failed=$failures.Count;skipped=0;results=$results.ToArray();controls=$controls.ToArray();failures=$failures.ToArray();sourceHashes=@(Get-FileHash $PSCommandPath,(Join-Path $PSScriptRoot 'GovernedSessionRestart.Model.ps1'),(Join-Path $PSScriptRoot 'GovernedSessionHost.Model.ps1'),(Join-Path $PSScriptRoot 'GovernedSessionHost.TransportModel.ps1')|Select-Object Path,Hash);productComplete=$false;scope='Composed host synchronous publication/restart seam; actual process termination/native/SQLite/fullsuite remain required'}|ConvertTo-Json -Depth 100
Add-LinesToFile $ReceiptPath -Content $var1|Out-Null
[pscustomobject]@{cases=$results.Count;planned=$faults.Count;assertions=$script:restartAssertions;controls=$controls.Count;failed=$failures.Count;receipt=$ReceiptPath}
if($failures.Count){throw 'Composed restart prerequisite failed; original evidence retained'}
