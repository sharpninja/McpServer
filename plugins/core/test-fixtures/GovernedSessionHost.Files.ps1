#Requires -Version 7.5
# SPDX-License-Identifier: Apache-2.0
<#
.SYNOPSIS
Bind unchanged host scenarios to canonical A8/outbox files and scripted typed-client replies.
.DESCRIPTION
No network, native REPL, actual SQLite, process-crash or fullscope acceptance.
#>
[CmdletBinding(SupportsShouldProcess)]
param([Parameter(Mandatory)][string]$ReceiptPath,[Parameter(Mandatory)][string]$MockReceiptPath,[Parameter(Mandatory)][string]$ArtifactRoot)
$ErrorActionPreference='Stop'
$mock=Get-Content -Raw $MockReceiptPath|ConvertFrom-Json -AsHashtable -DateKind String
if($mock.mode -cne 'Mock' -or $mock.cases -ne 32 -or $mock.rejectingControls -ne 9 -or $mock.failed -ne 0 -or $mock.skipped -ne 0){throw 'Frozen32/9 independent host prerequisite required'}
foreach($pin in $mock.sourceHashes){if((Get-FileHash $pin.Path).Hash -cne $pin.Hash){throw 'Frozen host tests/model changed'}}
if(-not [IO.Path]::IsPathRooted($ArtifactRoot)){throw 'Synthetic artifact root must be absolute'}
Write-Output 'Counted effects:32 fresh canonical host A8store/outbox trees, scripted existing typed-client seam replies and full retained observation receipts. No external writes.'
if(-not $PSCmdlet.ShouldProcess($ArtifactRoot,'Run SAME32host scenarios against canonical composition/files')){return}
Import-Module PowerShell.MCP -ErrorAction Stop
Import-Module powershell-yaml -ErrorAction Stop
. (Join-Path $PSScriptRoot '../lib-ps/governed-session-host.ps1')
. (Join-Path $PSScriptRoot 'GovernedSessionHost.TransportModel.ps1')
$hostForProvider=(Get-Command Invoke-McpGovernedSessionHost).ScriptBlock
$serverForProvider=(Get-Command Invoke-MockSessionHostTransport).ScriptBlock
$evidence=[Collections.Generic.List[object]]::new()
$factory={param($Root,$State,$ServerState)
    $store=Join-Path $Root 'store';$outbox=Join-Path $Root 'outbox'
    $hostCall=$hostForProvider;$serverCall=$serverForProvider;$fixtureState=$State;$fixtureServer=$ServerState
    $server={param($Operation,$Binding,$Data) & $serverCall -State $fixtureServer -Operation $Operation -Root $Binding -InputObject $Data}.GetNewClosure()
    $policy={param($Binding,$Operation,$Data) [void]$Binding;[void]$Operation;[void]$Data;return @{code=$fixtureState.policy}}.GetNewClosure()
    $observer={param($Stage,$Binding) $fixtureState.effects.Add(@{operation=$Stage;requestId=$Binding.requestId})}.GetNewClosure()
    $invoke={param($Context,$Operation,$Data)
        return & $hostCall -StoreDirectory $store -OutboxRoot $outbox -HostContext $Context -Operation $Operation -InputObject $Data -Server $server -CompletionPolicy $policy -StepObserver $observer
    }.GetNewClosure()
    $inspect={
        $snapshot=if(Test-Path (Join-Path $store 'turn-store.json')){ConvertFrom-Json ([IO.File]::ReadAllText((Join-Path $store 'turn-store.json'))) -AsHashtable -DateKind String}else{$null}
        $records=@(if(Test-Path $outbox){foreach($file in @(Get-ChildItem $outbox -Recurse -File -Filter '*.event.yaml')){@{path=$file.FullName;sha256=(Get-FileHash $file.FullName).Hash;document=(ConvertFrom-Yaml ([IO.File]::ReadAllText($file.FullName)))}}})
        return @{store=$snapshot;outboxes=$records;server=$fixtureServer;tape=$fixtureState.effects.ToArray()}
    }.GetNewClosure()
    $evidence.Add(@{root=$Root;store=$store;outbox=$outbox})
    return @{Invoke=$invoke;Inspect=$inspect}
}.GetNewClosure()
try{& (Join-Path $PSScriptRoot 'GovernedSessionHost.Contract.ps1') -Mode Production -ReceiptPath $ReceiptPath -ArtifactRoot $ArtifactRoot -ProviderFactory $factory}
finally{
    $var1=@{timestamp=Get-Date -Format o;mockReceipt=$MockReceiptPath;sourceHashes=@(Get-FileHash $PSCommandPath,(Join-Path $PSScriptRoot '../lib-ps/governed-session-host.ps1'),(Join-Path $PSScriptRoot '../lib-ps/governed-turn-store.ps1'),(Join-Path $PSScriptRoot '../lib-ps/governed-audit-alias-consumer.ps1'),(Join-Path $PSScriptRoot '../lib-ps/governed-audit-outbox.ps1'),(Join-Path $PSScriptRoot '../lib-ps/governed-payload.ps1')|Select-Object Path,Hash);roots=$evidence.ToArray();scope='Actual canonical host composition/store/alias/outbox/payload; scripted typed-client replies only';productComplete=$false}|ConvertTo-Json -Depth 100
    Add-LinesToFile ($ReceiptPath+'.evidence.json') -Content $var1|Out-Null
}
