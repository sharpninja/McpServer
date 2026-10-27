#Requires -Version 7.5
# SPDX-License-Identifier: Apache-2.0
<#
.SYNOPSIS
Bind SAME composed restart inputs/assertions to canonical host/A8/outbox files.
.DESCRIPTION
Synchronous publication faults and fresh stateless invocation only. No actual
process-kill, SQLite, network, native dispatch or fullscope acceptance.
#>
[CmdletBinding(SupportsShouldProcess)]
param([Parameter(Mandatory)][string]$ReceiptPath,[Parameter(Mandatory)][string]$MockReceiptPath,[Parameter(Mandatory)][string]$ArtifactRoot)
$ErrorActionPreference='Stop'
$mock=Get-Content $MockReceiptPath -Raw|ConvertFrom-Json -AsHashtable -DateKind String
if($mock.mode -cne 'Mock' -or $mock.cases -ne 8 -or $mock.rejectingControls -ne 4 -or $mock.failed -ne 0 -or $mock.skipped -ne 0){throw 'Exact8/4 independent restart prerequisite required'}
foreach($pin in $mock.sourceHashes){if((Get-FileHash $pin.Path).Hash -cne $pin.Hash){throw 'Frozen restart contract/model changed'}}
if(-not [IO.Path]::IsPathRooted($ArtifactRoot)){throw 'Synthetic artifact root must be absolute'}
Write-Output 'Counted effects:8 fresh canonical A8/outbox trees, injected publication/terminal faults, fresh invocation reconstruction and full retained file evidence. No external writes.'
if(-not $PSCmdlet.ShouldProcess($ArtifactRoot,'Run SAME8host restart cases on canonical retained files')){return}
Import-Module PowerShell.MCP -ErrorAction Stop
Import-Module powershell-yaml -ErrorAction Stop
. (Join-Path $PSScriptRoot '../lib-ps/governed-session-host.ps1')
. (Join-Path $PSScriptRoot 'GovernedSessionHost.TransportModel.ps1')
$hostForProvider=(Get-Command Invoke-McpGovernedSessionHost).ScriptBlock
$serverForProvider=(Get-Command Invoke-MockSessionHostTransport).ScriptBlock
$roots=[Collections.Generic.List[object]]::new()
$factory={param($Root,$Fault,$Mutation)
    if($Mutation){throw 'Test-only wrong providers cannot enter production binding'}
    $store=Join-Path $Root 'store';$outbox=Join-Path $Root 'outbox'
    $hostCall=$hostForProvider;$serverCall=$serverForProvider
    $tape=[Collections.Generic.List[object]]::new()
    $serverState=@{turns=@{};events=@{};effects=$tape;mutation=''}
    $io=@{fault=$Fault;armed=$true;hit=$false;restarts=0;snapshots=[Collections.Generic.List[object]]::new()}
    $server={param($Operation,$Binding,$Data) & $serverCall -State $serverState -Operation $Operation -Root $Binding -InputObject $Data}.GetNewClosure()
    $policy={param($Binding,$Operation,$Data) [void]$Binding;[void]$Operation;[void]$Data;return @{code='allow'}}
    $observer={param($Stage,$Binding)
        $tape.Add(@{operation=$Stage;requestId=$Binding.requestId})
        if($io.armed -and $Stage -ceq $io.fault){$io.armed=$false;$io.hit=$true;throw ('Injected canonical publication interruption: '+$Stage)}
    }.GetNewClosure()
    $invoke={param($Context,$Operation,$Data)
        if($io.fault -ceq 'TerminalTimeout' -and $Operation -ceq 'completeTurn'){$serverState.mutation='timeout-after-complete';$io.hit=$true;$io.armed=$false}
        return & $hostCall -StoreDirectory $store -OutboxRoot $outbox -HostContext $Context -Operation $Operation -InputObject $Data -Server $server -CompletionPolicy $policy -StepObserver $observer
    }.GetNewClosure()
    $restart={
        $files=@(Get-ChildItem $Root -Recurse -File|Where-Object {$_.Extension -cin @('.json','.yaml')})
        $pins=@($files|Get-FileHash|Select-Object Path,Hash)
        foreach($pin in $pins){if((Get-FileHash $pin.Path).Hash -cne $pin.Hash){throw 'Retained canonical restart evidence changed'}}
        $io.snapshots.Add(@{timestamp=Get-Date -Format o;files=$pins});$io.restarts++
        $tape.Add(@{operation='Restart';files=$pins})
        # Canonical host is stateless per invocation: subsequent Invoke imports
        # its dependencies afresh and all ownership/evidence comes from these files.
    }.GetNewClosure()
    $inspect={
        $storePath=Join-Path $store 'turn-store.json'
        $snapshot=if(Test-Path $storePath){ConvertFrom-Json ([IO.File]::ReadAllText($storePath)) -AsHashtable -DateKind String}else{$null}
        $manifests=@(if(Test-Path $outbox){foreach($file in @(Get-ChildItem $outbox -Recurse -File -Filter 'sequence.yaml')){$journal=ConvertFrom-Yaml ([IO.File]::ReadAllText($file.FullName));if($journal.evidence){$journal.evidence.Values}}})
        return @{store=$snapshot;manifests=$manifests;server=$serverState;tape=$tape.ToArray();faultHit=$io.hit;restarts=$io.restarts;snapshots=$io.snapshots.ToArray()}
    }.GetNewClosure()
    $roots.Add(@{root=$Root;fault=$Fault})
    return @{Invoke=$invoke;Restart=$restart;Inspect=$inspect}
}.GetNewClosure()
try{& (Join-Path $PSScriptRoot 'GovernedSessionRestart.Contract.ps1') -Mode Production -ReceiptPath $ReceiptPath -ArtifactRoot $ArtifactRoot -ProviderFactory $factory}
finally{
    $var1=@{timestamp=Get-Date -Format o;mockReceipt=$MockReceiptPath;sourceHashes=@(Get-FileHash $PSCommandPath,(Join-Path $PSScriptRoot '../lib-ps/governed-session-host.ps1'),(Join-Path $PSScriptRoot '../lib-ps/governed-audit-outbox.ps1')|Select-Object Path,Hash);roots=$roots.ToArray();scope='Canonical files/synchronous faults/fresh stateless invocation only, scripted remote replies';productComplete=$false}|ConvertTo-Json -Depth 100
    Add-LinesToFile ($ReceiptPath+'.evidence.json') -Content $var1|Out-Null
}
