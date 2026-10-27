#Requires -Version 7.5
# SPDX-License-Identifier: Apache-2.0
function Get-MockSessionRestartProvider {
    <#
    .SYNOPSIS
    Independent host model with append-only durable snapshots and scripted interruption dependencies.
    .DESCRIPTION
    Recreates worker state from retained native JSON, independently of the remote
    scripted server. Faults interrupt observed interactions, never return expected
    scenario outcomes. No production lifecycle or transport code is imported.
    #>
    [CmdletBinding()]
    param([string]$Root,[string]$Fault,[string]$Mutation='')
    . (Join-Path $PSScriptRoot 'GovernedSessionHost.Model.ps1')
    . (Join-Path $PSScriptRoot 'GovernedSessionHost.TransportModel.ps1')
    $model=(Get-Command Invoke-MockGovernedSessionHost).ScriptBlock
    $transportModel=(Get-Command Invoke-MockSessionHostTransport).ScriptBlock
    $tape=[Collections.Generic.List[object]]::new()
    $serverState=@{turns=@{};events=@{};effects=$tape;mutation=''}
    $io=@{armed=$true;hit=$false;fault=$Fault;checkpoint=0;latest='';latestHash='';snapshots=[Collections.Generic.List[object]]::new();restarts=0;mutation=$Mutation}
    $worker=@{active=@{storeDirectory=(Join-Path $Root 'store');outboxes=@{};nextRequest=0;policy='allow';effects=$tape;crashAfterJournal=($Fault -ceq 'CaptureJournal')}}
    $observer={param($Stage,$Binding)
        $tape.Add(@{operation=$Stage;requestId=$Binding.requestId})
        if($io.armed -and $Stage -ceq $io.fault){$io.armed=$false;$io.hit=$true;throw ('Injected model interruption: '+$Stage)}
    }.GetNewClosure()
    $worker.active.observer=$observer
    $server={param($Operation,$Binding,$Data) & $transportModel -State $serverState -Operation $Operation -Root $Binding -InputObject $Data}.GetNewClosure()
    $invoke={param($Context,$Operation,$Data)
        try{
            if($io.fault -ceq 'TerminalTimeout' -and $Operation -ceq 'completeTurn'){$serverState.mutation='timeout-after-complete';$io.hit=$true;$io.armed=$false}
            return & $model -State $worker.active -HostContext $Context -Operation $Operation -InputObject $Data -Server $server -Mutation $io.mutation
        }finally{
            if($io.armed -and $io.fault -ceq 'CaptureJournal'){
                $all=@($worker.active.outboxes.Values|ForEach-Object {$_.manifests.Values}|ForEach-Object {$_.expectedEvents})
                if($all.Count){$io.hit=$true;$io.armed=$false}
            }
            $io.checkpoint++
            $path=Join-Path $Root ('model-storage-'+$io.checkpoint.ToString('D4')+'.json')
            $var1=ConvertTo-Json -InputObject @{nextRequest=$worker.active.nextRequest;outboxes=$worker.active.outboxes} -Depth 100
            Add-LinesToFile $path -Content $var1|Out-Null
            $hash=(Get-FileHash $path).Hash;$io.latest=$path;$io.latestHash=$hash;$io.snapshots.Add(@{path=$path;sha256=$hash})
        }
    }.GetNewClosure()
    $restart={
        if((Get-FileHash $io.latest).Hash -cne $io.latestHash){throw 'Independent model checkpoint changed'}
        $persisted=ConvertFrom-Json ([IO.File]::ReadAllText($io.latest)) -AsHashtable -DateKind String
        foreach($box in $persisted.outboxes.Values){
            $records=[Collections.Generic.List[object]]::new();foreach($record in $box.records){$records.Add($record)};$box.records=$records
            $effects=[Collections.Generic.List[object]]::new();foreach($effect in $box.effects){$effects.Add($effect)};$box.effects=$effects;$box.crashAfterJournal=$false
            if($io.mutation -ceq 'forget-capture'){$box.manifests.Clear();$box.orders.Clear();$box.records.Clear()}
        }
        $worker.active=@{storeDirectory=(Join-Path $Root 'store');outboxes=$persisted.outboxes;nextRequest=$persisted.nextRequest;policy='allow';effects=$tape;crashAfterJournal=$false;observer=$observer}
        $io.restarts++;$tape.Add(@{operation='Restart';checkpoint=$io.latest;sha256=$io.latestHash})
    }.GetNewClosure()
    $inspect={
        $path=Join-Path $Root 'store/turn-store.json'
        $store=if(Test-Path $path){ConvertFrom-Json ([IO.File]::ReadAllText($path)) -AsHashtable -DateKind String}else{$null}
        $manifests=@($worker.active.outboxes.Values|ForEach-Object {$_.manifests.Values})
        return @{store=$store;manifests=$manifests;server=$serverState;tape=$tape.ToArray();faultHit=$io.hit;restarts=$io.restarts;snapshots=$io.snapshots.ToArray()}
    }.GetNewClosure()
    return @{Invoke=$invoke;Restart=$restart;Inspect=$inspect}
}
