# SPDX-License-Identifier: Apache-2.0
<#
.SYNOPSIS
TEST-QB-008/009/069: Durable audit events, aliases and receipt discrimination.
.DESCRIPTION
Mock responses validate the same complete outcome assertions used against real
storage. All data is invented; all writes stay under the supplied receipt path.
#>
[CmdletBinding(SupportsShouldProcess)]
param([ValidateSet('Mock','Production')][string]$Mode='Mock',[Parameter(Mandatory)][string]$ReceiptPath)
$ErrorActionPreference='Stop'
if(-not $PSCmdlet.ShouldProcess($ReceiptPath,"Run $Mode audit outbox contract")){return}
Import-Module powershell-yaml
$started=Get-Date -Format s
$script:negative=0;$script:assertions=0
$cases=[Collections.Generic.List[object]]::new()
function Add-AuditCase {
    param([string]$Name,[System.Collections.IDictionary]$Expected,[string]$Value='')
    $cases.Add(@{name=$Name;expected=$Expected;value=$Value})
}
function Copy-AuditFixture {
    param([object]$Value)
    return ConvertFrom-Json (ConvertTo-Json -InputObject $Value -Depth 50 -Compress) -AsHashtable -DateKind String -NoEnumerate
}
function ConvertTo-AuditComparable {
    param([object]$Value)
    if($Value -is [Collections.IDictionary]){
        $sorted=[ordered]@{};foreach($key in @($Value.Keys|Sort-Object -CaseSensitive)){$sorted[$key]=ConvertFrom-Json (ConvertTo-AuditComparable $Value[$key]) -AsHashtable -NoEnumerate}
        return ConvertTo-Json -InputObject $sorted -Depth 50 -Compress
    }
    return ConvertTo-Json -InputObject $Value -Depth 50 -Compress
}
function Assert-AuditOutcome {
    param([object]$Actual,[object]$Expected,[string]$Name)
    $script:assertions++
    if((ConvertTo-AuditComparable $Actual) -cne (ConvertTo-AuditComparable $Expected)){throw "$Name complete outcome/interaction mismatch: $(ConvertTo-AuditComparable $Actual)"}
}
Add-AuditCase capture @{codes=@('queued','queued','queued');orders=@(1,2,1);files=3;exact=$true;unique=$true}
Add-AuditCase pending @{persisted=0;pending=1;quarantined=0;rejected=0;calls=0;attempts=0}
Add-AuditCase alias @{persisted=4;pending=0;stable=$true;exact=$true;orders=@(1,2,3,4);resolved='req-one'}
Add-AuditCase duplicateAlias @{code='unchanged';calls=0;next=2}
Add-AuditCase independentAlias @{code='lost';unchanged=$true}
Add-AuditCase attemptedAlias @{code='lost';requestId='pending-one'}
foreach($field in @('workspacePath','agent','mcpSessionId','hostSessionKey','ingressNonce','pendingId','aliasedAtUtc')){Add-AuditCase "aliasForeign-$field" @{code='lost';unchanged=$true} $field}
foreach($field in @('workspacePath','agentIdentity','mcpSessionId','requestId','eventId','order','payloadHash','revision')){Add-AuditCase "receipt-$field" @{persisted=0;pending=0;quarantined=1;rejected=0;calls=1;reads=0} $field}
foreach($code in @('turn_immutable','skipped_terminal','host_session_unknown','host_session_ambiguous','request_ambiguous','request_unknown','turn_logging_unsupported','mint_requires_host_ingress','nonce_source_missing','session_turn_completion_gate','request_class_invalid','review_invalid','ingress_nonce_invalid','mint_collision_exhausted','unknown_new_code')){
    Add-AuditCase "final-$code" @{persisted=0;pending=0;quarantined=1;rejected=0;calls=1;sleeps=@();detail=$code;retryable=$false;reported=$true} $code
}
Add-AuditCase unknown @{persisted=0;pending=0;quarantined=1;rejected=0;calls=1;detail='unknown_error';retryable=$false}
foreach($flag in @('true','false')){Add-AuditCase "persistence-$flag" @{persisted=$(if($flag -eq 'true'){1}else{0});quarantined=$(if($flag -eq 'true'){0}else{1});calls=$(if($flag -eq 'true'){2}else{1})} $flag}
Add-AuditCase timeout @{persisted=1;pending=0;quarantined=0;rejected=0;calls=2;reads=1;sleeps=@(1000);exact=$true;archive=1;repeatCalls=0}
Add-AuditCase badReadback @{persisted=0;pending=0;quarantined=1;rejected=0;calls=1;reads=1}
Add-AuditCase badReadbackRevision @{persisted=0;pending=0;quarantined=1;rejected=0;calls=1;reads=1}
Add-AuditCase terminal @{persisted=0;pending=0;quarantined=0;rejected=1;calls=1;sleeps=@();repeatCalls=0;retained=$true;next=2}
Add-AuditCase cap @{persisted=0;pending=1;quarantined=1;rejected=0;calls=10;sleeps=@(1000,2000,4000,8000,16000,30000,30000,30000,30000);repeatCalls=0;attempts=10}
Add-AuditCase corrupt @{persisted=0;pending=0;quarantined=1;rejected=0;calls=0;retained=$true}
Add-AuditCase renamed @{persisted=0;pending=1;quarantined=1;rejected=0;calls=0}
foreach($folder in @('pending','archive','quarantine','rejected')){foreach($damage in @('rollback','missing-key')){Add-AuditCase "sequence-$folder-$damage" @{code='lost';unchanged=$true;sequenceUnchanged=$true} "$folder,$damage"}}
Add-AuditCase isolation @{first=1;other=1;exact=$true;calls=2}
Add-AuditCase hostIsolation @{first=1;otherPending=1;calls=1}
Add-AuditCase deadline @{lost='lost';persisted=1;sleeps=@(1000);exact=$true}
Add-AuditCase publishFailure @{code='lost';next=2;quarantined=1;calls=0}
Add-AuditCase finalPublicationCrash @{first='lost';quarantined=1;repeatCalls=0}
Add-AuditCase terminalPublicationCrash @{first='lost';rejected=1;quarantined=0;repeatCalls=0}

function Invoke-AuditRealCase {
    param([hashtable]$Case,[string]$Root)
    $script:fixtureRoot=$Root
    $script:identity=@{workspacePath='/fixture/workspace';agentIdentity='ClineQb';mcpSessionId='session-one';hostSessionKey='host-one'}
    $script:wireCalls=[Collections.Generic.List[object]]::new();$script:reads=0;$script:sleeps=[Collections.Generic.List[long]]::new()
    $script:now=5000L;$script:behavior='persisted';$script:changedField=$Case.value;$script:fault='';$script:didFault=$false
    $script:submit={param($Event)
        $script:wireCalls.Add((Copy-AuditFixture $Event))
        if($script:behavior -eq 'final' -or $script:behavior -eq 'cap' -or ($script:behavior -eq 'timeout' -and $script:wireCalls.Count -eq 1) -or ($script:behavior -like 'persistence-*' -and $script:wireCalls.Count -eq 1)){
            $exception=[Exception]::new('Invented test transport failure')
            $exception.Data['code']=switch($script:behavior){'final'{$script:changedField};'cap'{'ENETUNREACH'};'timeout'{'ETIMEDOUT'};default{'persistence_error'}}
            if($script:behavior -eq 'final'){$exception.Data['retryable']=$true}
            if($script:behavior -like 'persistence-*'){$exception.Data['retryable']=($script:behavior -eq 'persistence-true')}
            throw $exception
        }
        if($script:behavior -eq 'unknown'){throw 'Invented unclassified failure'}
        $receipt=Copy-AuditFixture $Event;$receipt.code='persisted';$receipt.revision=7
        if($script:behavior -in @('timeout','badReadback','badReadbackRevision')){$receipt.code='unchanged'}
        if($script:behavior -eq 'terminal'){$receipt.code='rejected';$receipt.reason='skipped_terminal';$receipt.storedStatus='completed'}
        if($script:behavior -eq 'badReceipt'){$receipt[$script:changedField]=switch($script:changedField){'order'{500};'revision'{0};default{'wrong'}}}
        return $receipt
    }
    $script:read={param($Event)
        $script:reads++;$receipt=Copy-AuditFixture $Event;$receipt.code='persisted';$receipt.revision=7
        if($script:behavior -eq 'badReadback'){$receipt.payloadHash='wrong'}
        if($script:behavior -eq 'badReadbackRevision'){$receipt.revision=8}
        return $receipt
    }
    $script:sleep={param($Milliseconds)
        if($script:fault -eq 'sleep' -and -not $script:didFault){$script:didFault=$true;throw 'Invented interruption during backoff'}
        $script:sleeps.Add($Milliseconds);$script:now+=$Milliseconds
    }
    $script:observer={param($Stage,$Path)
        if($script:didFault){return}
        $hit=($script:fault -eq 'event-stage' -and $Stage -eq 'staged' -and $Path.EndsWith('.event.yaml')) -or
            ($script:fault -eq 'quarantine' -and $Stage -eq 'retiring' -and $Path.Contains('/quarantine/')) -or
            ($script:fault -eq 'rejected' -and $Stage -eq 'retiring' -and $Path.Contains('/rejected/'))
        if($hit){$script:didFault=$true;throw 'Invented publication interruption'}
    }
    function Invoke-FixtureBox {
        param([string]$Operation,[hashtable]$InputData=@{})
        Invoke-McpGovernedAuditOutbox -RootDirectory $script:fixtureRoot -Context $script:identity -Operation $Operation -InputObject $InputData -Transport @{submit=$script:submit;read=$script:read} -Clock {$script:now} -Sleep $script:sleep -PublicationObserver $script:observer
    }
    function Add-FixtureEvent {
        param([string]$Id='req-one',[string]$Text='first',[string]$Kind='action')
        Invoke-FixtureBox Enqueue @{requestId=$Id;kind=$Kind;payloadJson=(ConvertTo-Json -InputObject ([ordered]@{text=$Text;n=1}) -Compress)}
    }
    function Get-FixtureFiles {
        param([string]$Folder='')
        return @(Get-ChildItem $script:fixtureRoot -Recurse -File -Filter '*.event.yaml'|Where-Object {-not $Folder -or $_.Directory.Name -ceq $Folder}|Sort-Object FullName)
    }
    function Get-FixtureRecord {param([object]$File);return ConvertFrom-Yaml ([IO.File]::ReadAllText($File.FullName))}
    function Get-FixtureAlias {
        return @{workspacePath='/fixture/workspace';agent='ClineQb';mcpSessionId='session-one';hostSessionKey='host-one';ingressNonce='one';pendingId='pending-one';requestId='req-one';aliasedAtUtc='2026-10-14T23:01:00Z';acks=@()}
    }
    function Get-FixtureSummary {param([object]$Result);return @{persisted=$Result.persisted;pending=$Result.pending;quarantined=$Result.quarantined;rejected=$Result.rejected;calls=$script:wireCalls.Count}}
    $name=$Case.name
    if($name -eq 'capture'){
        $a=Add-FixtureEvent;$b=Add-FixtureEvent -Text second -Kind decision;$c=Add-FixtureEvent -Id req-other -Kind dialog
        $saved=Get-FixtureRecord (Get-FixtureFiles|Where-Object Name -Like "*$($a.event.eventId)*")
        return @{codes=@($a.code,$b.code,$c.code);orders=@($a.event.order,$b.event.order,$c.event.order);files=(Get-FixtureFiles).Count;exact=($saved.event.payloadJson -ceq $a.event.payloadJson);unique=($a.event.eventId -cne $b.event.eventId -and [guid]::Parse($a.event.eventId) -ne [guid]::Empty)}
    }
    if($name -in @('alias','duplicateAlias','independentAlias','attemptedAlias') -or $name.StartsWith('aliasForeign-')){
        $a=Add-FixtureEvent pending-one '雪';$before=@(Get-FixtureFiles|ForEach-Object {$_.Name});$raw=[IO.File]::ReadAllText((Get-FixtureFiles)[0].FullName)
        $alias=Get-FixtureAlias
        if($name -eq 'independentAlias'){$null=Add-FixtureEvent req-one; $before=@(Get-FixtureFiles|ForEach-Object {[IO.File]::ReadAllText($_.FullName)})}
        if($name -eq 'attemptedAlias'){$file=(Get-FixtureFiles)[0];$record=Get-FixtureRecord $file;$record.attempts=1;$record.nextAttemptAt=1;[IO.File]::WriteAllText($file.FullName,(ConvertTo-Yaml $record))}
        if($name.StartsWith('aliasForeign-')){$alias[$Case.value]='foreign-invalid'}
        if($name -eq 'alias'){$b=Add-FixtureEvent pending-one second dialog;$before=@(Get-FixtureFiles|ForEach-Object Name)}
        $applied=Invoke-FixtureBox ApplyAlias $alias
        if($name -eq 'independentAlias'){return @{code=$applied.code;unchanged=((ConvertTo-Json -InputObject @(Get-FixtureFiles|ForEach-Object {[IO.File]::ReadAllText($_.FullName)}) -Compress) -ceq (ConvertTo-Json -InputObject $before -Compress))}}
        if($name -eq 'attemptedAlias'){return @{code=$applied.code;requestId=(Get-FixtureRecord (Get-FixtureFiles)[0]).event.requestId}}
        if($name.StartsWith('aliasForeign-')){return @{code=$applied.code;unchanged=([IO.File]::ReadAllText((Get-FixtureFiles)[0].FullName) -ceq $raw)}}
        if($name -eq 'duplicateAlias'){
            $null=Invoke-FixtureBox Flush;$alias.acks=@('other');$again=Invoke-FixtureBox ApplyAlias $alias;$script:wireCalls.Clear();$null=Invoke-FixtureBox Flush
            return @{code=$again.code;calls=$script:wireCalls.Count;next=(Add-FixtureEvent pending-one).event.order}
        }
        $stable=((ConvertTo-Json -InputObject @(Get-FixtureFiles|ForEach-Object Name) -Compress) -ceq (ConvertTo-Json -InputObject $before -Compress))
        $null=Add-FixtureEvent req-one third decision;$null=Add-FixtureEvent pending-one fourth metadata
        $resolved=Invoke-FixtureBox Resolve @{requestId='pending-one'};$result=Invoke-FixtureBox Flush
        $a.event.requestId='req-one';$b.event.requestId='req-one'
        return @{persisted=$result.persisted;pending=$result.pending;stable=$stable;exact=((ConvertTo-AuditComparable $script:wireCalls[0]) -ceq (ConvertTo-AuditComparable $a.event) -and (ConvertTo-AuditComparable $script:wireCalls[1]) -ceq (ConvertTo-AuditComparable $b.event));orders=@($script:wireCalls|ForEach-Object order);resolved=$resolved.requestId}
    }
    if($name -eq 'hostIsolation'){
        $null=Add-FixtureEvent pending-one;$script:identity.hostSessionKey='host-two';$null=Add-FixtureEvent pending-one
        $script:identity.hostSessionKey='host-one';$null=Invoke-FixtureBox ApplyAlias (Get-FixtureAlias);$a=Invoke-FixtureBox Flush
        $script:identity.hostSessionKey='host-two';$b=Invoke-FixtureBox Flush
        return @{first=$a.persisted;otherPending=$b.pending;calls=$script:wireCalls.Count}
    }
    if($name -eq 'isolation'){
        $a=Add-FixtureEvent;$script:identity.mcpSessionId='session-two';$b=Add-FixtureEvent
        $one=Invoke-FixtureBox Flush;$script:identity.mcpSessionId='session-one';$two=Invoke-FixtureBox Flush
        return @{first=$one.persisted;other=$two.persisted;exact=($script:wireCalls[0].eventId -ceq $b.event.eventId -and $script:wireCalls[1].eventId -ceq $a.event.eventId);calls=$script:wireCalls.Count}
    }
    if($name -eq 'publishFailure'){
        $script:fault='event-stage';$lost=Add-FixtureEvent;$next=Add-FixtureEvent;$result=Invoke-FixtureBox Flush
        return @{code=$lost.code;next=$next.event.order;quarantined=$result.quarantined;calls=$script:wireCalls.Count}
    }
    $id=if($name -eq 'pending'){'pending-one'}else{'req-one'}
    $first=Add-FixtureEvent $id
    if($name.StartsWith('sequence-')){
        $folder,$damage=$Case.value.Split(',');$null=Add-FixtureEvent
        if($folder -eq 'archive'){$null=Invoke-FixtureBox Flush}
        if($folder -eq 'quarantine'){$script:behavior='unknown';$null=Invoke-FixtureBox Flush}
        if($folder -eq 'rejected'){$script:behavior='terminal';$null=Invoke-FixtureBox Flush}
        $files=Get-FixtureFiles;$before=@($files|ForEach-Object {[IO.File]::ReadAllText($_.FullName)})
        $path=Join-Path $files[0].Directory.Parent.FullName 'sequence.yaml';$sequence=ConvertFrom-Yaml ([IO.File]::ReadAllText($path));$key=@($sequence.orders.Keys)[0]
        if($damage -eq 'rollback'){$sequence.orders[$key]=0}else{$sequence.orders.Remove($key)}
        $text=ConvertTo-Yaml $sequence;[IO.File]::WriteAllText($path,$text);$result=Add-FixtureEvent
        return @{code=$result.code;unchanged=((ConvertTo-Json -InputObject @($files|ForEach-Object {[IO.File]::ReadAllText($_.FullName)}) -Compress) -ceq (ConvertTo-Json -InputObject $before -Compress));sequenceUnchanged=([IO.File]::ReadAllText($path) -ceq $text)}
    }
    if($name -eq 'cap'){$null=Add-FixtureEvent -Text second;$script:behavior='cap'}
    if($name -eq 'corrupt'){[IO.File]::WriteAllText((Get-FixtureFiles)[0].FullName,'invalid: [')}
    if($name -eq 'renamed'){$null=Add-FixtureEvent -Text second;$file=(Get-FixtureFiles)[1];[IO.File]::Move($file.FullName,(Join-Path $file.DirectoryName '000-first.event.yaml'))}
    if($name.StartsWith('final-')){$script:behavior='final'}
    if($name.StartsWith('receipt-')){$script:behavior='badReceipt'}
    if($name.StartsWith('persistence-')){$script:behavior=$name}
    if($name -in @('unknown','timeout','badReadback','badReadbackRevision','terminal')){$script:behavior=$name}
    if($name -eq 'deadline'){$script:behavior='timeout';$script:fault='sleep';$lost=Invoke-FixtureBox Flush;$script:behavior='persisted'}
    if($name -eq 'finalPublicationCrash'){$script:behavior='final';$script:changedField='request_unknown';$script:fault='quarantine'}
    if($name -eq 'terminalPublicationCrash'){$script:behavior='terminal';$script:fault='rejected'}
    $result=Invoke-FixtureBox Flush
    if($name -in @('finalPublicationCrash','terminalPublicationCrash')){
        $script:wireCalls.Clear();$again=Invoke-FixtureBox Flush
        if($name -eq 'finalPublicationCrash'){return @{first=$result.code;quarantined=$again.quarantined;repeatCalls=$script:wireCalls.Count}}
        return @{first=$result.code;rejected=$again.rejected;quarantined=$again.quarantined;repeatCalls=$script:wireCalls.Count}
    }
    if($name -eq 'deadline'){return @{lost=$lost.code;persisted=$result.persisted;sleeps=@($script:sleeps);exact=($script:wireCalls[1].eventId -ceq $first.event.eventId)}}
    if($name.StartsWith('persistence-')){return @{persisted=$result.persisted;quarantined=$result.quarantined;calls=$script:wireCalls.Count}}
    $outcome=Get-FixtureSummary $result
    if($name.StartsWith('final-')){$record=Get-FixtureRecord (Get-FixtureFiles quarantine)[0];$outcome.sleeps=@($script:sleeps);$outcome.detail=$record.failure.detailCode;$outcome.retryable=$record.failure.retryable;$outcome.reported=$record.failure.reportedRetryable}
    if($name -eq 'unknown'){$record=Get-FixtureRecord (Get-FixtureFiles quarantine)[0];$outcome.detail=$record.failure.detailCode;$outcome.retryable=$record.failure.retryable}
    if($name.StartsWith('receipt-') -or $name.StartsWith('badReadback')){$outcome.reads=$script:reads}
    if($name -eq 'pending'){$outcome.attempts=(Get-FixtureRecord (Get-FixtureFiles)[0]).attempts}
    if($name -eq 'corrupt'){$outcome.retained=([IO.File]::ReadAllText((Get-FixtureFiles quarantine)[0].FullName) -ceq 'invalid: [')}
    if($name -in @('timeout','terminal','cap')){
        $outcome.sleeps=@($script:sleeps)
        if($name -eq 'timeout'){$outcome.reads=$script:reads;$outcome.exact=((ConvertTo-AuditComparable $script:wireCalls[0]) -ceq (ConvertTo-AuditComparable $script:wireCalls[1]));$outcome.archive=(Get-FixtureFiles archive).Count}
        if($name -eq 'terminal'){$outcome.retained=((Get-FixtureRecord (Get-FixtureFiles rejected)[0]).receipt.reason -ceq 'skipped_terminal')}
        if($name -eq 'cap'){$outcome.attempts=(Get-FixtureRecord (Get-FixtureFiles quarantine)[0]).attempts}
        $script:wireCalls.Clear();$null=Invoke-FixtureBox Flush;$outcome.repeatCalls=$script:wireCalls.Count
        if($name -eq 'terminal'){$outcome.next=(Add-FixtureEvent).event.order}
    }
    return $outcome
}

$root=Join-Path ([IO.Path]::GetDirectoryName($ReceiptPath)) ('audit-fixture-'+[guid]::NewGuid().ToString('N'))
[void][IO.Directory]::CreateDirectory($root)
if($Mode -eq 'Production'){. (Join-Path $PSScriptRoot '../lib-ps/governed-audit-outbox.ps1')}
$results=@(foreach($case in $cases){
    $correct=Copy-AuditFixture $case.expected
    Assert-AuditOutcome $correct $case.expected $case.name
    foreach($field in @($case.expected.Keys)){
        $wrong=Copy-AuditFixture $correct;$wrong[$field]='__wrong__';$caught=$false
        try{Assert-AuditOutcome $wrong $case.expected "negative-$($case.name)-$field"}catch{$caught=$true}
        if(-not $caught){throw "Negative control escaped: $($case.name)-$field"};$script:negative++
        $missing=Copy-AuditFixture $correct;$missing.Remove($field);$caught=$false
        try{Assert-AuditOutcome $missing $case.expected "missing-$($case.name)-$field"}catch{$caught=$true}
        if(-not $caught){throw "Missing-field control escaped: $($case.name)-$field"};$script:negative++
    }
    if($Mode -eq 'Production'){$actual=Invoke-AuditRealCase $case (Join-Path $root $case.name)}else{$actual=Copy-AuditFixture $correct}
    Assert-AuditOutcome $actual $case.expected $case.name
    @{name=$case.name;passed=$true;expected=$case.expected;actual=$actual}
})
$receipt=[ordered]@{started=$started;finished=(Get-Date -Format s);mode=$Mode;cases=$cases.Count;checks=$script:assertions;negativeControls=$script:negative;failed=0;skipped=0;testSha256=(Get-FileHash $PSCommandPath).Hash;results=$results;fixtureRoot=$root}
if($Mode -eq 'Production'){$receipt.implementationSha256=(Get-FileHash (Join-Path $PSScriptRoot '../lib-ps/governed-audit-outbox.ps1')).Hash}
[IO.File]::WriteAllText($ReceiptPath,(ConvertTo-Json -InputObject $receipt -Depth 60))
Write-Output "$(Get-Date -Format s) - $Mode audit contract: $($cases.Count) cases; $script:assertions checks; $script:negative negative controls; 0 failed; 0 skipped. Receipt: $ReceiptPath"
