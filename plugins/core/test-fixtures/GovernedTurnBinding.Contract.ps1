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
$script:checks=0
$script:negativeControls=0
$fields=@('workspacePath','agentIdentity','mcpSessionId','hostSessionKey','keySource','ingressNonce','requestId','boundAtUtc')

function New-FixtureTurn {
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions','',Justification='Pure in-memory fixture factory; no external mutation.')]
    [CmdletBinding()]
    param([string]$Id='req-fixture-1',[string]$Session='session-fixture-1',[bool]$Delivered=$true,[string]$Status='open')
    return [ordered]@{
        workspacePath='/isolated-fixture/workspace';agentIdentity='ClineQb';mcpSessionId=$Session
        hostSessionKey='host-fixture-1';keySource='native';ingressNonce="nonce-$Id"
        requestId=$Id;boundAtUtc='2026-10-14T10:00:00.0000000Z';delivered=$Delivered;status=$Status
    }
}
function New-FixtureContext {
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions','',Justification='Pure in-memory fixture factory; no external mutation.')]
    [CmdletBinding()]
    param([object[]]$Turns)
    return [ordered]@{workspacePath='/isolated-fixture/workspace';agentIdentity='ClineQb';hosts=@(
        [ordered]@{hostSessionKey='host-fixture-1';turns=@($Turns)}
    )}
}
function New-FixtureExpected {
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions','',Justification='Pure in-memory expected-value factory; no external mutation.')]
    [CmdletBinding()]
    param([string]$Code='resolved',[System.Collections.IDictionary]$Turn)
    $binding=$null
    if($Code -ceq 'resolved'){
        $binding=[ordered]@{}
        foreach($field in $fields){$binding[$field]=$Turn[$field]}
    }
    return [ordered]@{ok=($Code -ceq 'resolved');code=$Code;retryable=$false;binding=$binding}
}
function New-FixtureCase {
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions','',Justification='Pure in-memory fixture factory; no external mutation.')]
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
function New-FixtureResponse {
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions','',Justification='Pure in-memory mock response factory; no external mutation.')]
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

$one=New-FixtureTurn
$two=New-FixtureTurn -Id 'req-fixture-2' -Session 'session-fixture-2'
$mint=New-FixtureTurn -Id 'req-fixture-minted' -Delivered $false
$terminal=New-FixtureTurn -Id 'req-fixture-terminal' -Status terminal
$selection=@{HostSessionKey='host-fixture-1'}
$cases=[System.Collections.Generic.List[object]]::new()
$cases.Add((New-FixtureCase single (New-FixtureContext @($one)) $selection (New-FixtureExpected -Turn $one)))
$cases.Add((New-FixtureCase empty (New-FixtureContext @()) $selection (New-FixtureExpected request_unknown)))
$cases.Add((New-FixtureCase ambiguous (New-FixtureContext @($one,$two)) $selection (New-FixtureExpected request_ambiguous)))
foreach($turn in @($one,$two)){
    $cases.Add((New-FixtureCase "explicit-$($turn.requestId)" (New-FixtureContext @($one,$two)) @{HostSessionKey='host-fixture-1';RequestId=$turn.requestId} (New-FixtureExpected -Turn $turn)))
}
$cases.Add((New-FixtureCase reversed (New-FixtureContext @($two,$one)) @{HostSessionKey='host-fixture-1';RequestId=$one.requestId} (New-FixtureExpected -Turn $one)))
$cases.Add((New-FixtureCase mintDoesNotSteer (New-FixtureContext @($one,$mint)) $selection (New-FixtureExpected -Turn $one)))
$cases.Add((New-FixtureCase mintOnly (New-FixtureContext @($mint)) $selection (New-FixtureExpected request_unknown)))
$cases.Add((New-FixtureCase explicitMint (New-FixtureContext @($mint)) @{HostSessionKey='host-fixture-1';RequestId=$mint.requestId} (New-FixtureExpected -Turn $mint)))
$cases.Add((New-FixtureCase terminalExplicit (New-FixtureContext @($one,$terminal)) @{HostSessionKey='host-fixture-1';RequestId=$terminal.requestId} (New-FixtureExpected turn_immutable)))
$cases.Add((New-FixtureCase terminalExcluded (New-FixtureContext @($terminal,$one)) $selection (New-FixtureExpected -Turn $one)))
foreach($badId in @('missing','REQ-fixture-1','', ' ', $null, 42)){
    $cases.Add((New-FixtureCase "badRequest-$($cases.Count)" (New-FixtureContext @($one)) @{HostSessionKey='host-fixture-1';RequestId=$badId} (New-FixtureExpected request_unknown)))
}
foreach($badKey in @('missing','HOST-fixture-1','', ' ', $null, 42)){
    $cases.Add((New-FixtureCase "badHost-$($cases.Count)" (New-FixtureContext @($one)) @{HostSessionKey=$badKey} (New-FixtureExpected host_session_unknown)))
}
$duplicate=New-FixtureContext @($one)
$duplicate.hosts+=Copy-FixtureObject $duplicate.hosts[0]
$cases.Add((New-FixtureCase duplicateHost $duplicate $selection (New-FixtureExpected host_session_ambiguous)))
$duplicateRequest=New-FixtureTurn -Session 'different-owning-session'
$cases.Add((New-FixtureCase duplicateRequest (New-FixtureContext @($one,$duplicateRequest)) @{HostSessionKey='host-fixture-1';RequestId=$one.requestId} (New-FixtureExpected request_ambiguous)))
foreach($field in @('workspacePath','agentIdentity','hostSessionKey')){
    $foreign=Copy-FixtureObject $one
    $foreign[$field]='foreign'
    $cases.Add((New-FixtureCase "foreign-$field" (New-FixtureContext @($foreign)) $selection (New-FixtureExpected request_unknown)))
}
$foreignContext=New-FixtureContext @($one)
$foreignTurn=Copy-FixtureObject $two
$foreignTurn.hostSessionKey='foreign-host'
$foreignContext.hosts+=@{hostSessionKey='foreign-host';turns=@($foreignTurn)}
$cases.Add((New-FixtureCase foreignRequest $foreignContext @{HostSessionKey='host-fixture-1';RequestId=$two.requestId} (New-FixtureExpected request_unknown)))
$cases.Add((New-FixtureCase foreignDoesNotSteer $foreignContext $selection (New-FixtureExpected -Turn $one)))
foreach($field in $fields){
    $malformed=Copy-FixtureObject $one
    $malformed.Remove($field)
    $cases.Add((New-FixtureCase "missing-$field" (New-FixtureContext @($malformed)) $selection (New-FixtureExpected request_unknown)))
}
foreach($pair in @(@{field='delivered';value='true'},@{field='status';value='unknown'},@{field='boundAtUtc';value=[datetime]'2026-10-14T10:00:00Z'})){
    $malformed=Copy-FixtureObject $one
    $malformed[$pair.field]=$pair.value
    $cases.Add((New-FixtureCase "malformed-$($pair.field)" (New-FixtureContext @($malformed)) $selection (New-FixtureExpected request_unknown)))
}

$production=Join-Path $PSScriptRoot '../lib-ps/governed-turn-binding.ps1'
if($Mode -eq 'Production'){. $production}
foreach($case in $cases){
    $context=Copy-FixtureObject $case.Context
    $before=ConvertTo-Json -InputObject $context -Depth 40 -Compress
    if($Mode -eq 'Mock'){$actual=New-FixtureResponse $case.Expected}
    else{$arguments=$case.Selection;$actual=Resolve-McpGovernedTurnBinding -Context $context @arguments}
    try{Assert-FixtureResult $actual $case.Expected $before $context}catch{throw "Case $($case.Name): $_"}
    foreach($mutation in @('ok','code','retryable','shape','context')){
        $wrong=New-FixtureResponse $case.Expected
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
            Assert-FixtureRejection (New-FixtureResponse $wrongExpected) $case.Expected $before $context
        }
        $mutable=Copy-FixtureObject $case.Expected
        Assert-FixtureRejection $mutable $case.Expected $before $context
    }else{
        $wrong=New-FixtureResponse $case.Expected
        $wrong.binding=@{requestId='wrongly-exposed'}
        Assert-FixtureRejection $wrong $case.Expected $before $context
    }
}
$receipt=[ordered]@{
    started=$started;finished=(Get-Date -Format s);mode=$Mode
    cases=$cases.Count;checks=$script:checks;negativeControls=$script:negativeControls
    failed=0;skipped=0;scope='A8 ordinary-write binding selection only; no lifecycle or product acceptance'
    testsSha256=(Get-FileHash $PSCommandPath -Algorithm SHA256).Hash
    productionSha256=$(if($Mode -eq 'Production'){(Get-FileHash $production -Algorithm SHA256).Hash}else{$null})
}
$parent=Split-Path -Parent $ReceiptPath
if($parent){[void][IO.Directory]::CreateDirectory($parent)}
$receipt | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $ReceiptPath -Encoding utf8NoBOM
$receipt | ConvertTo-Json -Depth 10
