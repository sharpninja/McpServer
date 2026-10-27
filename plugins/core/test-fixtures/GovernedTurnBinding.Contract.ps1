# SPDX-License-Identifier: Apache-2.0
<#
.SYNOPSIS
Proves the A8 ordinary-write selection assertions with mocks and production.
.DESCRIPTION
FR-QB-003/TR-QB-AUDIT-001/TEST-QB-008/069 and FR-QB-005/TR-QB-RUNTIME-001/
TEST-QB-014. Fixtures use invented identities only and no operator state.
The same assertions run unchanged for both providers. This is not live evidence.
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [ValidateSet('Mock','Production')][string]$Mode='Mock',
    [Parameter(Mandatory)][string]$ReceiptPath
)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
if(-not $PSCmdlet.ShouldProcess($ReceiptPath,"Run $Mode A8 selection contract and write receipt")){return}
$started=Get-Date -Format s
. (Join-Path $PSScriptRoot 'GovernedTurnBinding.Model.ps1')
$script:checks=0
$script:negativeControls=0
$fields=@('workspacePath','agentIdentity','mcpSessionId','hostSessionKey','keySource','ingressNonce','requestId','boundAtUtc')

function Get-FixtureTurn {
    [CmdletBinding()]
    param([string]$Id='req-fixture-1',[string]$Session='session-fixture-1',[bool]$Delivered=$true,[string]$Status='open')
    return [ordered]@{
        workspacePath='/isolated-fixture/workspace';agentIdentity='ClineQb';mcpSessionId=$Session
        hostSessionKey='host-fixture-1';keySource='native';ingressNonce="nonce-$Id"
        requestId=$Id;boundAtUtc='2026-10-14T10:00:00.0000000Z';delivered=$Delivered;status=$Status
    }
}
function Get-FixtureContext {
    [CmdletBinding()]
    param([object[]]$Turns)
    return [ordered]@{workspacePath='/isolated-fixture/workspace';agentIdentity='ClineQb';hosts=@(
        [ordered]@{hostSessionKey='host-fixture-1';turns=@($Turns)}
    )}
}
function Get-FixtureExpected {
    [CmdletBinding()]
    param([string]$Code='resolved',[System.Collections.IDictionary]$Turn)
    $binding=$null
    if($Code -ceq 'resolved'){
        $binding=[ordered]@{}
        foreach($field in $fields){$binding[$field]=$Turn[$field]}
    }
    return [ordered]@{ok=($Code -ceq 'resolved');code=$Code;retryable=$false;binding=$binding}
}
function Get-FixtureCase {
    [CmdletBinding()]
    param([string]$Name,[System.Collections.IDictionary]$Context,[hashtable]$Selection,[System.Collections.IDictionary]$Expected)
    return [pscustomobject]@{Name=$Name;Context=$Context;Selection=$Selection;Expected=$Expected}
}
function Copy-FixtureObject {
    param([object]$Value)
    if($Value -is [System.Collections.IDictionary]){
        $copy=[ordered]@{}
        foreach($key in $Value.Keys){$copy[$key]=Copy-FixtureObject $Value[$key]}
        return $copy
    }
    if($Value -is [array]){
        $items=@(foreach($item in $Value){Copy-FixtureObject $item})
        return ,$items
    }
    return $Value
}
function Get-FixtureResponse {
    [CmdletBinding()]
    param([System.Collections.IDictionary]$Expected)
    $binding=$null
    if($null -ne $Expected.binding){
        $map=[System.Collections.Generic.Dictionary[string,string]]::new([StringComparer]::Ordinal)
        foreach($field in $fields){$map.Add($field,$Expected.binding[$field])}
        $binding=[System.Collections.ObjectModel.ReadOnlyDictionary[string,string]]::new($map)
    }
    return [ordered]@{ok=$Expected.ok;code=$Expected.code;retryable=$false;binding=$binding}
}
function Assert-FixtureResult {
    param([object]$Actual,[System.Collections.IDictionary]$Expected,[string]$Before,[object]$Context)
    $script:checks++
    if($Actual -isnot [System.Collections.IDictionary]){throw 'Result must be a dictionary.'}
    if((@($Actual.Keys | Sort-Object) -join '|') -cne 'binding|code|ok|retryable'){throw 'Result shape mismatch.'}
    if($Actual.ok -isnot [bool] -or $Actual.retryable -isnot [bool] -or $Actual.ok -cne $Expected.ok -or $Actual.code -cne $Expected.code -or $Actual.retryable){throw 'Result classification mismatch.'}
    if((ConvertTo-Json -InputObject $Context -Depth 40 -Compress) -cne $Before){throw 'Input snapshot mutated.'}
    if($null -eq $Expected.binding){if($null -ne $Actual.binding){throw 'Failure exposed a binding.'};return}
    if($Actual.binding -isnot [System.Collections.IDictionary]){throw 'Missing binding.'}
    if((@($Actual.binding.Keys | Sort-Object) -join '|') -cne (@($fields | Sort-Object) -join '|')){throw 'Binding shape mismatch.'}
    foreach($field in $fields){if($Actual.binding[$field] -isnot [string] -or $Actual.binding[$field] -cne $Expected.binding[$field]){throw "Binding field mismatch: $field"}}
    if(-not ([System.Collections.IDictionary]$Actual.binding).IsReadOnly){throw 'Binding is mutable.'}
    $denied=$false
    try{$Actual.binding['requestId']='attempted-mutation'}catch [System.NotSupportedException]{$denied=$true}catch [System.Management.Automation.RuntimeException]{$denied=$true}
    if(-not $denied -or $Actual.binding['requestId'] -cne $Expected.binding['requestId']){throw 'Binding mutation succeeded.'}
}
function Assert-FixtureRejection {
    param([object]$Actual,[System.Collections.IDictionary]$Expected,[string]$Before,[object]$Context)
    $rejected=$false
    try{Assert-FixtureResult $Actual $Expected $Before $Context}catch{$rejected=$true}
    if(-not $rejected){throw 'Negative control escaped the assertions.'}
    $script:negativeControls++
}

$one=Get-FixtureTurn
$two=Get-FixtureTurn -Id 'req-fixture-2' -Session 'session-fixture-2'
$mint=Get-FixtureTurn -Id 'req-fixture-minted' -Delivered $false
$terminal=Get-FixtureTurn -Id 'req-fixture-terminal' -Status terminal
$selection=@{HostSessionKey='host-fixture-1'}
$cases=[System.Collections.Generic.List[object]]::new()
$cases.Add((Get-FixtureCase single (Get-FixtureContext @($one)) $selection (Get-FixtureExpected -Turn $one)))
$cases.Add((Get-FixtureCase empty (Get-FixtureContext @()) $selection (Get-FixtureExpected request_unknown)))
$cases.Add((Get-FixtureCase ambiguous (Get-FixtureContext @($one,$two)) $selection (Get-FixtureExpected request_ambiguous)))
foreach($turn in @($one,$two)){
    $cases.Add((Get-FixtureCase "explicit-$($turn.requestId)" (Get-FixtureContext @($one,$two)) @{HostSessionKey='host-fixture-1';RequestId=$turn.requestId} (Get-FixtureExpected -Turn $turn)))
}
$cases.Add((Get-FixtureCase reversed (Get-FixtureContext @($two,$one)) @{HostSessionKey='host-fixture-1';RequestId=$one.requestId} (Get-FixtureExpected -Turn $one)))
$cases.Add((Get-FixtureCase mintDoesNotSteer (Get-FixtureContext @($one,$mint)) $selection (Get-FixtureExpected -Turn $one)))
$cases.Add((Get-FixtureCase mintOnly (Get-FixtureContext @($mint)) $selection (Get-FixtureExpected request_unknown)))
$cases.Add((Get-FixtureCase explicitMint (Get-FixtureContext @($mint)) @{HostSessionKey='host-fixture-1';RequestId=$mint.requestId} (Get-FixtureExpected -Turn $mint)))
$cases.Add((Get-FixtureCase terminalExplicit (Get-FixtureContext @($one,$terminal)) @{HostSessionKey='host-fixture-1';RequestId=$terminal.requestId} (Get-FixtureExpected turn_immutable)))
$cases.Add((Get-FixtureCase terminalExcluded (Get-FixtureContext @($terminal,$one)) $selection (Get-FixtureExpected -Turn $one)))
foreach($badId in @('missing','REQ-fixture-1','', ' ', $null, 42)){
    $cases.Add((Get-FixtureCase "badRequest-$($cases.Count)" (Get-FixtureContext @($one)) @{HostSessionKey='host-fixture-1';RequestId=$badId} (Get-FixtureExpected request_unknown)))
}
foreach($badKey in @('missing','HOST-fixture-1','', ' ', $null, 42)){
    $cases.Add((Get-FixtureCase "badHost-$($cases.Count)" (Get-FixtureContext @($one)) @{HostSessionKey=$badKey} (Get-FixtureExpected host_session_unknown)))
}
$duplicate=Get-FixtureContext @($one)
$duplicate.hosts+=Copy-FixtureObject $duplicate.hosts[0]
$cases.Add((Get-FixtureCase duplicateHost $duplicate $selection (Get-FixtureExpected host_session_ambiguous)))
$duplicateRequest=Get-FixtureTurn -Session 'different-owning-session'
$cases.Add((Get-FixtureCase duplicateRequest (Get-FixtureContext @($one,$duplicateRequest)) @{HostSessionKey='host-fixture-1';RequestId=$one.requestId} (Get-FixtureExpected request_ambiguous)))
foreach($field in @('workspacePath','agentIdentity','hostSessionKey')){
    $foreign=Copy-FixtureObject $one
    $foreign[$field]='foreign'
    $cases.Add((Get-FixtureCase "foreign-$field" (Get-FixtureContext @($foreign)) $selection (Get-FixtureExpected request_unknown)))
}
$foreignContext=Get-FixtureContext @($one)
$foreignTurn=Copy-FixtureObject $two
$foreignTurn.hostSessionKey='foreign-host'
$foreignContext.hosts+=@{hostSessionKey='foreign-host';turns=@($foreignTurn)}
$cases.Add((Get-FixtureCase foreignRequest $foreignContext @{HostSessionKey='host-fixture-1';RequestId=$two.requestId} (Get-FixtureExpected request_unknown)))
$cases.Add((Get-FixtureCase foreignDoesNotSteer $foreignContext $selection (Get-FixtureExpected -Turn $one)))
foreach($field in $fields){
    $malformed=Copy-FixtureObject $one
    $malformed.Remove($field)
    $cases.Add((Get-FixtureCase "missing-$field" (Get-FixtureContext @($malformed)) $selection (Get-FixtureExpected request_unknown)))
}
foreach($pair in @(@{field='delivered';value='true'},@{field='status';value='unknown'},@{field='boundAtUtc';value=[datetime]'2026-10-14T10:00:00Z'})){
    $malformed=Copy-FixtureObject $one
    $malformed[$pair.field]=$pair.value
    $cases.Add((Get-FixtureCase "malformed-$($pair.field)" (Get-FixtureContext @($malformed)) $selection (Get-FixtureExpected request_unknown)))
}

$production=Join-Path $PSScriptRoot '../lib-ps/governed-turn-binding.ps1'
if($Mode -eq 'Production'){. $production}
$results=[Collections.Generic.List[object]]::new()
foreach($case in $cases){
    $context=Copy-FixtureObject $case.Context
    $before=ConvertTo-Json -InputObject $context -Depth 40 -Compress
    if($Mode -eq 'Mock'){$arguments=$case.Selection;$actual=Resolve-MockGovernedTurnBinding -Context $context @arguments}
    else{$arguments=$case.Selection;$actual=Resolve-McpGovernedTurnBinding -Context $context @arguments}
    try{Assert-FixtureResult $actual $case.Expected $before $context}catch{throw "Case $($case.Name): $_"}
    $results.Add(@{name=$case.Name;selection=(Copy-FixtureObject $case.Selection);input=(Copy-FixtureObject $context);expected=$case.Expected;actual=$actual})
    foreach($mutation in @('ok','code','retryable','shape','context')){
        $wrong=Get-FixtureResponse $case.Expected
        $wrongContext=Copy-FixtureObject $context
        switch($mutation){
            ok {$wrong.ok=-not $wrong.ok}
            code {$wrong.code='wrong_code'}
            retryable {$wrong.retryable=$true}
            shape {$wrong.extra='unexpected'}
            context {$wrongContext.agentIdentity='different-agent'}
        }
        Assert-FixtureRejection $wrong $case.Expected $before $wrongContext
    }
    if($null -ne $case.Expected.binding){
        foreach($field in $fields){
            $wrongExpected=Copy-FixtureObject $case.Expected
            $wrongExpected.binding[$field]='wrong-value'
            Assert-FixtureRejection (Get-FixtureResponse $wrongExpected) $case.Expected $before $context
        }
        $mutable=Copy-FixtureObject $case.Expected
        Assert-FixtureRejection $mutable $case.Expected $before $context
    }else{
        $wrong=Get-FixtureResponse $case.Expected
        $wrong.binding=@{requestId='wrongly-exposed'}
        Assert-FixtureRejection $wrong $case.Expected $before $context
    }
}
$modelControls=@(
    @{case='mintDoesNotSteer';mutation='include-minted'},
    @{case='ambiguous';mutation='first-implicit'},
    @{case='foreign-agentIdentity';mutation='ignore-identity'},
    @{case='badRequest-12';mutation='case-fold-id'},
    @{case='single';mutation='mutable-binding'},
    @{case='single';mutation='mutate-input'}
)
$controlResults=@(foreach($control in $modelControls){
    $selected=@($cases|Where-Object Name -CEQ $control.case)[0]
    if($null -eq $selected){throw 'Unknown model control case'}
    $controlContext=Copy-FixtureObject $selected.Context
    $controlBefore=ConvertTo-Json -InputObject $controlContext -Depth 40 -Compress
    $arguments=$selected.Selection
    $wrong=Resolve-MockGovernedTurnBinding -Context $controlContext @arguments -Mutation $control.mutation
    $rejection=''
    try{Assert-FixtureResult $wrong $selected.Expected $controlBefore $controlContext}catch{$rejection=$_.Exception.Message}
    if(-not $rejection){throw "Model control escaped scenario assertion: $($control.mutation)"}
    $script:negativeControls++
    @{case=$control.case;mutation=$control.mutation;rejected=$true;assertion=$rejection;selection=$arguments;before=$controlBefore;after=(Copy-FixtureObject $controlContext);actual=$wrong;expected=$selected.Expected}
})
$receipt=[ordered]@{
    started=$started;finished=(Get-Date -Format s);mode=$Mode
    cases=$cases.Count;checks=$script:checks;negativeControls=$script:negativeControls
    failed=0;skipped=0;scope='A8 ordinary-write binding selection only; no lifecycle or product acceptance'
    testsSha256=(Get-FileHash $PSCommandPath -Algorithm SHA256).Hash
    modelSha256=(Get-FileHash (Join-Path $PSScriptRoot 'GovernedTurnBinding.Model.ps1')).Hash
    results=@($results);modelControls=$controlResults
    productionSha256=$(if($Mode -eq 'Production'){(Get-FileHash $production -Algorithm SHA256).Hash}else{$null})
}
$parent=Split-Path -Parent $ReceiptPath
if($parent){[void][IO.Directory]::CreateDirectory($parent)}
$receipt | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $ReceiptPath -Encoding utf8NoBOM
$receipt | ConvertTo-Json -Depth 10
