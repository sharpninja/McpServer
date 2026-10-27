#Requires -Version 7.5
# SPDX-License-Identifier: Apache-2.0
<#
.SYNOPSIS
Bind unchanged host/restart and supplemental batch assertions to actual typed clients, controllers and SQLite.
.DESCRIPTION
HTTP handlers are injected; no network authentication/native dispatch acceptance.
Requires a fresh coordinator heavy lease for the exact source-pinned command.
#>
[CmdletBinding(SupportsShouldProcess)]
param([ValidateSet('Host','Restart','Batch')][string]$ContractSet='Host',[Parameter(Mandatory)][string]$ReceiptPath,[Parameter(Mandatory)][string]$MockReceiptPath,[Parameter(Mandatory)][string]$ArtifactRoot,[Parameter(Mandatory)][string]$AssemblyDirectory)
$ErrorActionPreference='Stop'
$mock=Get-Content $MockReceiptPath -Raw|ConvertFrom-Json -AsHashtable -DateKind String
$restart=$ContractSet -ceq 'Restart'
$batch=$ContractSet -ceq 'Batch'
if($mock.mode -cne 'Mock' -or $mock.cases -ne $(if($batch){10}elseif($restart){8}else{32}) -or $mock.rejectingControls -ne $(if($batch){30}elseif($restart){4}else{9}) -or $mock.failed -ne 0 -or $mock.skipped -ne 0){throw 'Exact independent host/restart/batch prerequisite required'}
foreach($pin in $mock.sourceHashes){if((Get-FileHash $pin.Path).Hash -cne $pin.Hash){throw 'Frozen input/assertion/model pin changed'}}
if(-not [IO.Path]::IsPathRooted($ArtifactRoot)){throw 'Synthetic artifact root must be absolute'}
Write-Output ('Counted effects: SAME '+$ContractSet+' inputs/assertions, actual canonical host/A8/outbox, typed client/controller and fresh SQLite contexts; retained databases/files/receipts. No external network or native dispatch.')
if(-not $PSCmdlet.ShouldProcess($ArtifactRoot,'Bind frozen host assertions to actual typed client/SQLite')){return}
Import-Module PowerShell.MCP -ErrorAction Stop
Import-Module powershell-yaml -ErrorAction Stop
. (Join-Path $PSScriptRoot 'ServerAuditFramework.ps1')
. (Join-Path $PSScriptRoot '../lib-ps/governed-session-host.ps1')
. (Join-Path $PSScriptRoot '../lib-ps/governed-session-dispatch.ps1')
. (Join-Path $PSScriptRoot 'SessionBatchStorageObservation.ps1')
$assembly=Get-ServerAuditFixtureAssembly -AssemblyDirectory $AssemblyDirectory
$type=$assembly.GetType('QBrainAi.Support.Mcp.Tests.Services.ServerAuditSqliteFixture',$true)
if(-not $type.GetMethod('InvokeHostTyped')){throw 'Current compiled actual host typed-client fixture required'}
$hostForProvider=(Get-Command Invoke-McpGovernedSessionHost).ScriptBlock
$dispatchForProvider=(Get-Command Invoke-McpGovernedSessionDispatch).ScriptBlock
$storageForProvider=(Get-Command Get-SessionBatchStorageSnapshot).ScriptBlock
$batchForProvider=$batch
$roots=[Collections.Generic.List[object]]::new()
$makeProvider={param($Root,$State,$ServerState,$Fault)
    $store=Join-Path $Root 'store';$outbox=Join-Path $Root 'outbox'
    $instance=$type.GetMethod('Create').Invoke($null,[object[]]@([string]$Root))
    $hostCall=$hostForProvider;$fixtureState=$State;$fixtureServer=$ServerState;$dispatchCall=$dispatchForProvider;$storageRead=$storageForProvider
    $bindings=@{};$io=@{fault=$Fault;armed=$true;hit=$false;restarts=0;snapshots=[Collections.Generic.List[object]]::new();batchCaptureCount=0}
    $server={param($Operation,$Binding,$Data)
        $key=ConvertTo-Json -InputObject @($Binding.workspacePath,$Binding.agentIdentity,$Binding.mcpSessionId,$Binding.requestId) -Compress
        $bindings[$key]=ConvertFrom-Json (ConvertTo-Json $Binding -Depth 100 -Compress) -AsHashtable -DateKind String
        $fixtureState.effects.Add(@{operation=$Operation;root=$bindings[$key];input=$Data})
        if($Operation -cin @('Submit','ReadAudit')){
            $wire=ConvertTo-Json $Data -Depth 100 -Compress
            $actual=ConvertFrom-Json ($instance.InvokeTypedClient($(if($Operation -ceq 'Submit'){'Submit'}else{'Read'}),$wire)) -AsHashtable -DateKind String
            $reply=$actual.reply
            if($Operation -ceq 'Submit' -and $fixtureServer.mutation.StartsWith('receipt-',[StringComparison]::Ordinal)){$reply[$fixtureServer.mutation.Substring(8)]='wrong'}
            return $reply
        }
        $body=ConvertFrom-Json (ConvertTo-Json $Data -Depth 100 -Compress) -AsHashtable -DateKind String
        if($Operation -ceq 'Begin' -and $fixtureServer.mutation -ceq 'summary-ingress'){$body.queryText='summary'}
        if($Operation -ceq 'Complete' -and $fixtureServer.mutation -ceq 'summary-final'){$body.response='summary'}
        $packet=ConvertTo-Json @{root=$Binding;data=$body} -Depth 100 -Compress
        $actual=ConvertFrom-Json ($instance.InvokeHostTyped($Operation,$packet)) -AsHashtable -DateKind String
        $reply=$actual.reply
        if($Operation -ceq 'ReadTurn' -and $reply){
            $fixtureServer.turns[$key]=$reply
            if($fixtureServer.mutation -ceq 'wrong-terminal-root' -and $reply.status -ceq 'completed'){$reply.requestId='foreign-request'}
        }
        if($Operation -ceq 'Complete' -and $fixtureServer.mutation -ceq 'timeout-after-complete'){throw 'Injected reply loss after actual committed terminal write'}
        return $reply
    }.GetNewClosure()
    $policy={param($Binding,$Operation,$Data) [void]$Binding;[void]$Operation;[void]$Data;return @{code=$fixtureState.policy}}.GetNewClosure()
    $observer={param($Stage,$Binding)
        $fixtureState.effects.Add(@{operation=$Stage;requestId=$Binding.requestId})
        if($io.armed -and $Stage -ceq $io.fault){
            if($batchForProvider -and $io.fault -ceq 'CaptureJournal'){$io.batchCaptureCount++;if($io.batchCaptureCount -lt 2){return}}
            $io.armed=$false;$io.hit=$true;throw ('Injected actual canonical interruption: '+$Stage)
        } # Second batch capture is journaled before interruption; unreached tail stays absent.
    }.GetNewClosure()
    $invoke={param($Context,$Operation,$Data)
        if($io.fault -ceq 'TerminalTimeout' -and $Operation -ceq 'completeTurn'){$fixtureServer.mutation='timeout-after-complete';$io.hit=$true;$io.armed=$false}
        return & $hostCall -StoreDirectory $store -OutboxRoot $outbox -HostContext $Context -Operation $Operation -InputObject $Data -Server $server -CompletionPolicy $policy -StepObserver $observer
    }.GetNewClosure()
    if($batchForProvider){
        $invoke={param($Envelope,$Context)
            return & $dispatchCall -Envelope $Envelope -HostOptions @{StoreDirectory=$store;OutboxRoot=$outbox;HostContext=$Context;Server=$server;CompletionPolicy=$policy;StepObserver=$observer}
        }.GetNewClosure()
    }
    $reopen={
        $pins=@(Get-ChildItem $Root -Recurse -File|Where-Object {$_.Extension -cin @('.json','.yaml','.sqlite')}|Get-FileHash|Select-Object Path,Hash)
        foreach($pin in $pins){if((Get-FileHash $pin.Path).Hash -cne $pin.Hash){throw 'Retained restart bytes changed'}}
        foreach($binding in $bindings.Values){$null=$instance.Invoke('Reopen',(ConvertTo-Json @{workspacePath=$binding.workspacePath} -Compress))}
        $io.snapshots.Add(@{files=$pins});$io.restarts++;$fixtureState.effects.Add(@{operation='Restart';files=$pins})
    }.GetNewClosure()
    $inspect={
        # Observation reads go through the actual typed GET and fresh SQLite child
        # projection queries; the provider never invents a server outcome object.
        $fixtureServer.turns.Clear();$fixtureServer.events.Clear()
        foreach($key in $bindings.Keys){
            $binding=$bindings[$key]
            $packet=ConvertTo-Json @{root=$binding;data=@{}} -Depth 100 -Compress
            $read=ConvertFrom-Json ($instance.InvokeHostTyped('ReadTurn',$packet)) -AsHashtable -DateKind String
            if($read.reply){$fixtureServer.turns[$key]=$read.reply}
            $snapshot=ConvertFrom-Json ($instance.Invoke('Snapshot',(ConvertTo-Json @{workspacePath=$binding.workspacePath} -Compress))) -AsHashtable -DateKind String
            foreach($uuid in $snapshot.events.Keys){$fixtureServer.events[$uuid]=$snapshot.events[$uuid]}
        }
        $storePath=Join-Path $store 'turn-store.json'
        $snapshot=if(Test-Path $storePath){ConvertFrom-Json ([IO.File]::ReadAllText($storePath)) -AsHashtable -DateKind String}else{$null}
        $manifests=@(if(Test-Path $outbox){foreach($file in @(Get-ChildItem $outbox -Recurse -File -Filter 'sequence.yaml')){$journal=ConvertFrom-Yaml ([IO.File]::ReadAllText($file.FullName));if($journal.evidence){$journal.evidence.Values}}})
        return @{store=$snapshot;outboxes=$manifests;manifests=$manifests;server=$fixtureServer;tape=$fixtureState.effects.ToArray();faultHit=$io.hit;restarts=$io.restarts;snapshots=$io.snapshots.ToArray();databasePath=$instance.DatabasePath}
    }.GetNewClosure()
    $seed={param($Envelope,$Context)
        $action=$Envelope.method -ceq 'workflow.sessionlog.appendActions'
        $kind=if($action){'actions'}else{'dialogItems'};$operation=if($action){'appendAction'}else{'appendDialog'}
        $item=$Envelope.params[$kind][0]
        return & $hostCall -StoreDirectory $store -OutboxRoot $outbox -HostContext $Context -Operation $operation -InputObject @{requestId=$Envelope.params.requestId;payloadJson=(ConvertTo-Json $item -Depth 100 -Compress)} -Server $server -CompletionPolicy $policy -StepObserver $observer
    }.GetNewClosure()
    $snapshotRead={return & $storageRead -StoreDirectory $store -OutboxRoot $outbox}.GetNewClosure()
    $arm={$io.fault='CaptureJournal';$io.armed=$true;$io.hit=$false;$io.batchCaptureCount=0}.GetNewClosure()
    $roots.Add(@{root=$Root;databasePath=$instance.DatabasePath;fault=$Fault})
    return @{Invoke=$invoke;Inspect=$inspect;Restart=$reopen;SeedLegacy=$seed;StorageSnapshot=$snapshotRead;ArmFault=$arm}
}.GetNewClosure()
$factory=if($batch){
    {param($Root)
        $tape=[Collections.Generic.List[object]]::new();$state=@{policy='allow';effects=$tape};$serverState=@{turns=@{};events=@{};effects=$tape;mutation=''}
        return & $makeProvider $Root $state $serverState ''
    }.GetNewClosure()
}elseif($restart){
    {param($Root,$Fault,$Mutation)
        if($Mutation){throw 'Mock-only wrong provider cannot enter actual binding'}
        $tape=[Collections.Generic.List[object]]::new();$state=@{policy='allow';effects=$tape};$serverState=@{turns=@{};events=@{};effects=$tape;mutation=''}
        return & $makeProvider $Root $state $serverState $Fault
    }.GetNewClosure()
}else{{param($Root,$State,$ServerState) return & $makeProvider $Root $State $ServerState ''}.GetNewClosure()}
try{
    $contract=if($batch){'GovernedSessionBatch.Contract.ps1'}elseif($restart){'GovernedSessionRestart.Contract.ps1'}else{'GovernedSessionHost.Contract.ps1'}
    $arguments=@{Mode='Production';ReceiptPath=$ReceiptPath;ArtifactRoot=$ArtifactRoot;ProviderFactory=$factory}
    if($batch){$arguments.FixtureDirectory=$PSScriptRoot}
    & (Join-Path $PSScriptRoot $contract) @arguments
}
finally{
    $var1=@{timestamp=Get-Date -Format o;mockReceipt=$MockReceiptPath;sourceHashes=@(Get-FileHash $PSCommandPath,(Join-Path $PSScriptRoot '../lib-ps/governed-session-host.ps1'),(Join-Path $PSScriptRoot '../lib-ps/governed-audit-outbox.ps1')|Select-Object Path,Hash);roots=$roots.ToArray();scope='Actual typed client/controller/SQLite plus canonical A8/outbox, injected HTTP handlers and publication faults; native/fullscope open';productComplete=$false}|ConvertTo-Json -Depth 100
    Add-LinesToFile ($ReceiptPath+'.evidence.json') -Content $var1|Out-Null
}