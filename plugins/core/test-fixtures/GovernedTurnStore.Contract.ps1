# SPDX-License-Identifier: Apache-2.0
<#
.SYNOPSIS
TEST-QB-005/008/009/014/069: A8 durable lifecycle contract and assertion proof.
.DESCRIPTION
Independent scripted responses prove every scenario assertion before production
exists. Production uses the identical scenarios with only the provider changed.
Fixtures contain invented identities and write only inside ReceiptPath's tree.
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [ValidateSet('Mock','Production')][string]$Mode='Mock',
    [Parameter(Mandatory)][string]$ReceiptPath
)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
if(-not $PSCmdlet.ShouldProcess($ReceiptPath,"Run $Mode A8 store contract")){return}
$started=Get-Date -Format s
$script:checks=0
$script:negativeControls=0
$script:steps=[System.Collections.Generic.List[object]]::new()
$context=[ordered]@{workspacePath='/fixture/workspace';agentIdentity='ClineQb';aliasConsumers=@('qb-audit-outbox-v1','host-cache-v1')}

function Copy-ContractValue {
    param([AllowNull()][object]$Value)
    if($Value -is [System.Collections.IDictionary]){
        $copy=[ordered]@{}
        foreach($key in $Value.Keys){$copy[$key]=Copy-ContractValue $Value[$key]}
        return $copy
    }
    if($Value -is [array]){return ,@(foreach($item in $Value){Copy-ContractValue $item})}
    return $Value
}
function ConvertTo-ContractJson {
    param([AllowNull()][object]$Value)
    # Dictionary key order is not contractual; array order is.
    if($Value -is [System.Collections.IDictionary]){
        $sorted=[ordered]@{}
        foreach($key in @($Value.Keys|Sort-Object -CaseSensitive)){$sorted[$key]=ConvertFrom-Json (ConvertTo-ContractJson $Value[$key]) -AsHashtable -DateKind String -NoEnumerate}
        return ConvertTo-Json -InputObject $sorted -Depth 60 -Compress
    }
    if($Value -is [array]){
        $items=@(foreach($item in $Value){ConvertFrom-Json (ConvertTo-ContractJson $item) -AsHashtable -DateKind String -NoEnumerate})
        return ConvertTo-Json -InputObject $items -Depth 60 -Compress
    }
    return ConvertTo-Json -InputObject $Value -Depth 60 -Compress
}
function Get-ContractBinding {
    param([string]$Nonce='one',[string]$HostKey='host-one',[string]$Session='session-one',[string]$Id)
    if(-not $Id){$Id="pending-$Nonce"}
    return [ordered]@{
        workspacePath=$context.workspacePath;agentIdentity=$context.agentIdentity;mcpSessionId=$Session
        hostSessionKey=$HostKey;keySource='native';ingressNonce=$Nonce;requestId=$Id;boundAtUtc='2026-10-14T23:00:00.0000000Z'
    }
}
function Get-ContractState {
    return [ordered]@{schemaVersion=1;workspacePath=$context.workspacePath;agentIdentity=$context.agentIdentity;aliasConsumers=@($context.aliasConsumers);revision=0;hosts=@();aliases=@()}
}
function Add-ContractStep {
    param([string]$Name,[string]$Operation,[System.Collections.IDictionary]$InputData,[string]$Code,
        [System.Collections.IDictionary]$State,[System.Collections.IDictionary]$Binding,[object[]]$Aliases=@(),[bool]$Ok=$true)
    $script:steps.Add([pscustomobject]@{
        name=$Name;operation=$Operation;inputData=(Copy-ContractValue $InputData)
        expected=[ordered]@{ok=$Ok;code=$Code;retryable=$false;binding=(Copy-ContractValue $Binding);aliases=(Copy-ContractValue $Aliases);state=(Copy-ContractValue $State)}
    })
}
function Assert-ContractResult {
    param([object]$Actual,[System.Collections.IDictionary]$Expected,[string]$Name)
    $script:checks++
    if($Actual -isnot [System.Collections.IDictionary]){throw "${Name}: result is not a dictionary"}
    if((ConvertTo-ContractJson $Actual) -cne (ConvertTo-ContractJson $Expected)){throw "${Name}: complete observable result differs"}
    if($Actual.ok -isnot [bool] -or $Actual.retryable -isnot [bool]){throw "${Name}: result flags are not booleans"}
    if($null -ne $Actual.binding -and -not ([System.Collections.IDictionary]$Actual.binding).IsReadOnly){throw "${Name}: captured binding is mutable"}
}
function Get-ContractResponse {
    param([System.Collections.IDictionary]$Expected)
    $copy=Copy-ContractValue $Expected
    if($null -ne $copy.binding){
        $map=[System.Collections.Generic.Dictionary[string,string]]::new([StringComparer]::Ordinal)
        foreach($key in $copy.binding.Keys){$map.Add($key,$copy.binding[$key])}
        $copy.binding=[System.Collections.ObjectModel.ReadOnlyDictionary[string,string]]::new($map)
    }
    return $copy
}
function Assert-ContractNegative {
    param([object]$Actual,[System.Collections.IDictionary]$Expected,[string]$Name)
    $rejected=$false
    try{Assert-ContractResult $Actual $Expected $Name}catch{$rejected=$true}
    if(-not $rejected){throw "Negative control escaped: $Name"}
    $script:negativeControls++
}
function Invoke-ContractWorker {
    param([string]$Directory,[string]$Action,[string]$FaultAt='none',[string]$Nonce='one')
    $start=[Diagnostics.ProcessStartInfo]::new((Get-Process -Id $PID).Path)
    $start.UseShellExecute=$false;$start.RedirectStandardOutput=$true;$start.RedirectStandardError=$true
    foreach($argument in @('-NoProfile','-NonInteractive','-File',(Join-Path $PSScriptRoot 'GovernedTurnStore.Worker.ps1'),'-Directory',$Directory,'-Action',$Action,'-FaultAt',$FaultAt,'-Nonce',$Nonce)){$start.ArgumentList.Add($argument)}
    $process=[Diagnostics.Process]::Start($start)
    return @{process=$process;stdout=$process.StandardOutput.ReadToEndAsync();stderr=$process.StandardError.ReadToEndAsync()}
}
function Receive-ContractWorker {
    param([hashtable]$Worker,[int]$ExpectedExit=0)
    if(-not $Worker.process.WaitForExit(30000)){$Worker.process.Kill($true);throw 'Isolated fixture worker hung.'}
    if($Worker.process.ExitCode -ne $ExpectedExit){throw "Worker exit $($Worker.process.ExitCode): $($Worker.stderr.Result) $($Worker.stdout.Result)"}
    $Worker.process.Dispose()
}

$state=Get-ContractState
$one=Get-ContractBinding
$two=Get-ContractBinding -Nonce two -Id req-two
$foreign=Get-ContractBinding -Nonce one -HostKey host-other -Session session-other
Add-ContractStep empty Resolve @{hostSessionKey='host-one'} host_session_unknown $state -Ok $false
$turn=Copy-ContractValue $one;$turn.delivered=$false;$turn.status='open'
$state.hosts+=@{hostSessionKey='host-one';turns=@($turn)};$state.revision=1
Add-ContractStep register Register $one registered $state
Add-ContractStep duplicateRegister Register $one unchanged $state
Add-ContractStep mintedNotDelivered Resolve @{hostSessionKey='host-one'} request_unknown $state -Ok $false
Add-ContractStep explicitMint Resolve @{hostSessionKey='host-one';requestId='pending-one'} resolved $state $one
$state.hosts[0].turns[0].delivered=$true;$state.revision=2
Add-ContractStep deliver Deliver @{hostSessionKey='host-one';requestId='pending-one'} delivered $state
Add-ContractStep duplicateDeliver Deliver @{hostSessionKey='host-one';requestId='pending-one'} unchanged $state
Add-ContractStep implicitSingle Resolve @{hostSessionKey='host-one'} resolved $state $one
$turn=Copy-ContractValue $two;$turn.delivered=$false;$turn.status='open';$state.hosts[0].turns+=@($turn);$state.revision=3
Add-ContractStep secondMint Register $two registered $state
Add-ContractStep mintDoesNotRetarget Resolve @{hostSessionKey='host-one'} resolved $state $one
$state.hosts[0].turns[1].delivered=$true;$state.revision=4
Add-ContractStep secondDelivery Deliver @{hostSessionKey='host-one';requestId='req-two'} delivered $state
Add-ContractStep ambiguous Resolve @{hostSessionKey='host-one'} request_ambiguous $state -Ok $false
Add-ContractStep explicitFirst Resolve @{hostSessionKey='host-one';requestId='pending-one'} resolved $state $one
Add-ContractStep explicitSecond Resolve @{hostSessionKey='host-one';requestId='req-two'} resolved $state $two
Add-ContractStep nullIsNotImplicit Resolve @{hostSessionKey='host-one';requestId=$null} request_unknown $state -Ok $false
$alias=Copy-ContractValue $one;$alias.requestId='req-one';$alias.pendingId='pending-one';$alias.aliasedAtUtc='2026-10-14T23:01:00.0000000Z'
$savedAlias=Copy-ContractValue $alias;$savedAlias.acks=@();$state.aliases+=@($savedAlias)
$state.hosts[0].turns[0].requestId='req-one';$state.revision=5
$bound=Copy-ContractValue $one;$bound.requestId='req-one'
Add-ContractStep aliasCommit Alias $alias aliased $state
Add-ContractStep aliasDuplicate Alias $alias unchanged $state
$reply=Copy-ContractValue $alias;$reply.aliasedAtUtc='2026-10-14T23:02:00.0000000Z'
Add-ContractStep duplicateReplyKeepsFirstTimestamp Alias $reply unchanged $state
Add-ContractStep restartOldId Resolve @{hostSessionKey='host-one';requestId='pending-one'} resolved $state $bound
Add-ContractStep restartNewId Resolve @{hostSessionKey='host-one';requestId='req-one'} resolved $state $bound
Add-ContractStep replayOriginalIngress Register $one unchanged $state
Add-ContractStep enumerateAliases ListAliases @{hostSessionKey='host-one'} listed $state -Aliases @($savedAlias)
Add-ContractStep enumerateConsumerPending ListAliases @{hostSessionKey='host-one';consumerId='qb-audit-outbox-v1'} listed $state -Aliases @($savedAlias)
$conflict=Copy-ContractValue $alias;$conflict.requestId='req-conflict'
Add-ContractStep conflictingMint Alias $conflict alias_conflict $state -Ok $false
$conflict=Copy-ContractValue $alias;$conflict.requestId='req-two'
Add-ContractStep aliasIdCollision Alias $conflict alias_conflict $state -Ok $false
foreach($field in @('workspacePath','agentIdentity','mcpSessionId','hostSessionKey','keySource','ingressNonce','boundAtUtc')){
    $conflict=Copy-ContractValue $alias;$conflict[$field]='foreign'
    Add-ContractStep "aliasForeign-$field" Alias $conflict alias_conflict $state -Ok $false
}
$ack=@{hostSessionKey='host-one';mcpSessionId='session-one';pendingId='pending-one';requestId='req-one';consumerId='qb-audit-outbox-v1'}
$state.aliases[0].acks+=@('qb-audit-outbox-v1');$state.revision=6
Add-ContractStep acknowledge Acknowledge $ack acknowledged $state
Add-ContractStep duplicateAck Acknowledge $ack unchanged $state
Add-ContractStep noLongerPendingForOneConsumer ListAliases @{hostSessionKey='host-one';consumerId='qb-audit-outbox-v1'} listed $state
Add-ContractStep stillPendingForOtherConsumer ListAliases @{hostSessionKey='host-one';consumerId='host-cache-v1'} listed $state -Aliases @($state.aliases[0])
$ack.consumerId='host-cache-v1';$state.aliases[0].acks+=@('host-cache-v1');$state.revision=7
Add-ContractStep finalConsumerAck Acknowledge $ack acknowledged $state
Add-ContractStep retainedAfterAllAcks ListAliases @{hostSessionKey='host-one'} listed $state -Aliases @($state.aliases[0])
Add-ContractStep unknownConsumer ListAliases @{hostSessionKey='host-one';consumerId='not-registered'} consumer_unknown $state -Ok $false
$ack.consumerId='not-registered'
Add-ContractStep rejectUnknownAck Acknowledge $ack consumer_unknown $state -Ok $false
$ack.consumerId='host-cache-v1';$ack.mcpSessionId='foreign'
Add-ContractStep rejectForeignAck Acknowledge $ack alias_conflict $state -Ok $false
$state.hosts[0].turns[0].status='terminal';$state.revision=8
Add-ContractStep closeOnlyFirst Terminal @{hostSessionKey='host-one';requestId='pending-one'} terminal $state
Add-ContractStep terminalRepeat Terminal @{hostSessionKey='host-one';requestId='req-one'} unchanged $state
Add-ContractStep terminalImmutable Deliver @{hostSessionKey='host-one';requestId='req-one'} turn_immutable $state -Ok $false
Add-ContractStep terminalSelection Resolve @{hostSessionKey='host-one';requestId='pending-one'} turn_immutable $state -Ok $false
Add-ContractStep onlySecondOpen Resolve @{hostSessionKey='host-one'} resolved $state $two
$conflict=Copy-ContractValue $two;$conflict.mcpSessionId='foreign'
Add-ContractStep nonceReuse Register $conflict ingress_nonce_invalid $state -Ok $false
$conflict=Copy-ContractValue $two;$conflict.ingressNonce='other'
Add-ContractStep idReuse Register $conflict ingress_nonce_invalid $state -Ok $false
$conflict=Copy-ContractValue $two;$conflict.requestId='pending-wrong';$conflict.ingressNonce='three'
Add-ContractStep badPendingNonce Register $conflict ingress_nonce_invalid $state -Ok $false
$turn=Copy-ContractValue $foreign;$turn.delivered=$false;$turn.status='open';$state.hosts+=@{hostSessionKey='host-other';turns=@($turn)};$state.revision=9
Add-ContractStep otherHostSameNonce Register $foreign registered $state
Add-ContractStep foreignHostCannotSelect Resolve @{hostSessionKey='host-other';requestId='req-two'} request_unknown $state -Ok $false
Add-ContractStep otherHostOldIdNotAliased Resolve @{hostSessionKey='host-other';requestId='pending-one'} resolved $state $foreign
Add-ContractStep otherHostHasNoAliases ListAliases @{hostSessionKey='host-other'} listed $state
foreach($field in @($one.Keys)){
    $bad=Copy-ContractValue $one;$bad.Remove($field)
    Add-ContractStep "missing-$field" Register $bad ingress_nonce_invalid $state -Ok $false
}
$bad=Copy-ContractValue $one;$bad.extra='not-a-binding-field'
Add-ContractStep extraBindingField Register $bad ingress_nonce_invalid $state -Ok $false
$bad=Copy-ContractValue $one;$bad.boundAtUtc=[datetime]'2026-10-14T23:00:00Z'
Add-ContractStep dateMustRemainString Register $bad ingress_nonce_invalid $state -Ok $false

$directory=Join-Path (Split-Path -Parent $ReceiptPath) ('store-fixture-'+[guid]::NewGuid().ToString('N'))
$production=Join-Path $PSScriptRoot '../lib-ps/governed-turn-store.ps1'
if($Mode -eq 'Production'){. $production}
foreach($step in $script:steps){
    $inputBefore=ConvertTo-ContractJson $step.inputData
    $actual=if($Mode -eq 'Mock'){Get-ContractResponse $step.expected}else{
        Invoke-McpGovernedTurnStore -Directory $directory -Context $context -Operation $step.operation -InputObject $step.inputData
    }
    Assert-ContractResult $actual $step.expected $step.name
    if((ConvertTo-ContractJson $step.inputData) -cne $inputBefore){throw 'Caller input mutated.'}
    # Each independent state/root/binding/alias outcome is attacked, not just counts.
    foreach($key in @('ok','code','retryable','binding','aliases','state')){
        $wrong=Get-ContractResponse $step.expected
        $wrong[$key]=switch($key){ok {-not $wrong.ok};retryable {$true};code {'wrong'};binding {if($null -eq $wrong.binding){@{requestId='wrong'}}else{$null}};aliases {@(@{requestId='wrong'})};state {$null}}
        Assert-ContractNegative $wrong $step.expected "$($step.name)-$key"
    }
    foreach($field in @('workspacePath','agentIdentity','revision','hosts','aliases','aliasConsumers')){
        $wrong=Get-ContractResponse $step.expected;$wrong.state[$field]='wrong'
        Assert-ContractNegative $wrong $step.expected "$($step.name)-state-$field"
    }
    # Corrupt every captured binding field even when counts and all other data match.
    if($null -ne $step.expected.binding){
        foreach($field in $step.expected.binding.Keys){
            $wrong=Get-ContractResponse $step.expected;$map=Copy-ContractValue $step.expected.binding;$map[$field]='wrong'
            $wrong.binding=$map;Assert-ContractNegative $wrong $step.expected "$($step.name)-binding-$field"
        }
    }
    if($Mode -eq 'Production'){
        # Returned snapshots are detached from the durable store.
        $actual.state.workspacePath='caller-mutated'
    }
}
# Storage integration assertions are defined before production implementation.
# Their mock responses exercise the same complete-state assertions and controls.
$ioCases=0
foreach($faultAt in @('staged','published')){
    $caseDirectory="$directory-crash-$faultAt"
    $expectedState=Get-ContractState
    $original=Copy-ContractValue $one;$original.delivered=$false;$original.status='open'
    $expectedState.hosts=@(@{hostSessionKey='host-one';turns=@($original)});$expectedState.revision=1
    $expectedAliases=@()
    if($faultAt -ceq 'published'){
        $expectedState.hosts[0].turns[0].requestId='req-one';$expectedState.revision=2
        $a=Copy-ContractValue $alias;$a.acks=@();$expectedState.aliases=@($a);$expectedAliases=@($a)
    }
    $expected=[ordered]@{ok=$true;code='listed';retryable=$false;binding=$null;aliases=$expectedAliases;state=$expectedState}
    $actual=if($Mode -eq 'Mock'){Get-ContractResponse $expected}else{
        $registered=Invoke-McpGovernedTurnStore -Directory $caseDirectory -Context $context -Operation Register -InputObject $one
        if(-not $registered.ok){throw 'Crash fixture setup failed.'}
        Receive-ContractWorker (Invoke-ContractWorker $caseDirectory AliasCrash $faultAt) 75
        Invoke-McpGovernedTurnStore -Directory $caseDirectory -Context $context -Operation ListAliases -InputObject @{hostSessionKey='host-one'}
    }
    Assert-ContractResult $actual $expected "crash-$faultAt"
    foreach($field in @('aliases','revision','hosts')){
        $wrong=Get-ContractResponse $expected;$wrong.state[$field]='wrong'
        Assert-ContractNegative $wrong $expected "crash-$faultAt-$field"
    }
    $ioCases++
}
foreach($sameIngress in @($false,$true)){
    $caseDirectory="$directory-concurrent-$sameIngress"
    $expectedState=Get-ContractState
    $turns=@(foreach($n in $(if($sameIngress){@('same')}else{@('one','two','three','four')|Sort-Object})){
        $t=Get-ContractBinding -Nonce $n;$t.delivered=$true;$t.status='open';$t
    })
    $expectedState.hosts=@(@{hostSessionKey='host-one';turns=$turns});$expectedState.revision=2*$turns.Count
    $expected=[ordered]@{ok=$true;code='listed';retryable=$false;binding=$null;aliases=@();state=$expectedState}
    $actual=if($Mode -eq 'Mock'){Get-ContractResponse $expected}else{
        $workers=@(foreach($n in @('one','two','three','four')){
            Invoke-ContractWorker -Directory $caseDirectory -Action Writer -Nonce $(if($sameIngress){'same'}else{$n})
        })
        foreach($worker in $workers){Receive-ContractWorker $worker}
        $result=Invoke-McpGovernedTurnStore -Directory $caseDirectory -Context $context -Operation ListAliases -InputObject @{hostSessionKey='host-one'}
        $result.state.hosts[0].turns=@($result.state.hosts[0].turns|Sort-Object ingressNonce)
        $result
    }
    Assert-ContractResult $actual $expected "concurrent-$sameIngress"
    $wrong=Get-ContractResponse $expected;$wrong.state.hosts[0].turns=@()
    Assert-ContractNegative $wrong $expected 'concurrency-lost-turns'
    $wrong=Get-ContractResponse $expected;$wrong.state.revision++
    Assert-ContractNegative $wrong $expected 'concurrency-duplicate-commit'
    $ioCases++
}
foreach($case in @('corrupt','context','lock','symlink','dangling-link')){
    $caseDirectory="$directory-$case"
    $expected=[ordered]@{ok=$false;code=$(switch($case){corrupt {'store_corrupt'};context {'context_mismatch'};lock {'store_unavailable'};symlink {'store_corrupt'};'dangling-link' {'store_corrupt'}});retryable=($case -ceq 'lock');binding=$null;aliases=@();state=$null}
    $actual=if($Mode -eq 'Mock'){Get-ContractResponse $expected}else{
        $null=Invoke-McpGovernedTurnStore -Directory $caseDirectory -Context $context -Operation Register -InputObject $one
        $statePath=Join-Path $caseDirectory 'turn-store.json'
        $altered=Copy-ContractValue $context
        $lockStream=$null
        switch($case){
            corrupt {[IO.File]::WriteAllText($statePath,'not a valid snapshot')}
            context {$altered.agentIdentity='OtherAgent'}
            lock {$lockStream=[IO.File]::Open((Join-Path $caseDirectory 'turn-store.lock'),[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)}
            symlink {
                $target=Join-Path $caseDirectory 'retained-original.json'
                Copy-Item $statePath $target
                Remove-Item $statePath
                $null=New-Item -ItemType SymbolicLink -Path $statePath -Target $target
            }
            'dangling-link' {
                Copy-Item $statePath (Join-Path $caseDirectory 'retained-original.json')
                Remove-Item $statePath
                $null=New-Item -ItemType SymbolicLink -Path $statePath -Target (Join-Path $caseDirectory 'absent-target.json')
            }
        }
        $before=if($case -ceq 'dangling-link'){[IO.FileInfo]::new($statePath).LinkTarget}else{(Get-FileHash $statePath -Algorithm SHA256).Hash}
        try{Invoke-McpGovernedTurnStore -Directory $caseDirectory -Context $altered -Operation Resolve -InputObject @{hostSessionKey='host-one'}}finally{
            if($lockStream){$lockStream.Dispose()}
            $after=if($case -ceq 'dangling-link'){[IO.FileInfo]::new($statePath).LinkTarget}else{(Get-FileHash $statePath -Algorithm SHA256).Hash}
            if($after -cne $before){throw "$case refusal changed stored evidence"}
        }
    }
    Assert-ContractResult $actual $expected "storage-$case"
    $wrong=Get-ContractResponse $expected;$wrong.ok=$true
    Assert-ContractNegative $wrong $expected "$case-false-success"
    $wrong=Get-ContractResponse $expected;$wrong.retryable=-not $wrong.retryable
    Assert-ContractNegative $wrong $expected "$case-wrong-retry"
    $ioCases++
}
[void][IO.Directory]::CreateDirectory((Split-Path -Parent $ReceiptPath))
$receipt=[ordered]@{started=$started;completed=(Get-Date -Format s);provider=$Mode;cases=($script:steps.Count+$ioCases);lifecycleCases=$script:steps.Count;storageCases=$ioCases;checks=$script:checks;negativeControls=$script:negativeControls;failed=0;skipped=0;directory=$directory;testSha256=(Get-FileHash $PSCommandPath -Algorithm SHA256).Hash;workerSha256=(Get-FileHash (Join-Path $PSScriptRoot 'GovernedTurnStore.Worker.ps1') -Algorithm SHA256).Hash;productionSha256=$null;phaseAccepted=$false}
if($Mode -eq 'Production'){$receipt.productionSha256=(Get-FileHash $production -Algorithm SHA256).Hash}
$receipt|ConvertTo-Json -Depth 8|Set-Content -LiteralPath $ReceiptPath -Encoding utf8NoBOM
$receipt|ConvertTo-Json -Depth 8
