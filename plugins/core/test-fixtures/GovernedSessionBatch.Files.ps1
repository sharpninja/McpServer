#Requires -Version 7.5
# SPDX-License-Identifier: Apache-2.0
<#
.SYNOPSIS
Bind SAME10 batch and recovery scenarios to canonical dispatch/host/A8/outbox files.
.DESCRIPTION
Only the existing typed-client dependency is scripted; every stored event comes
from actual canonical capture, alias reconciliation, replay and receipt checks.
The injected journal interruption and disk reopen are distinct from native crash acceptance.
#>
[CmdletBinding(SupportsShouldProcess)]
param([Parameter(Mandatory)][string]$ReceiptPath,[Parameter(Mandatory)][string]$MockReceiptPath,[Parameter(Mandatory)][string]$ArtifactRoot)
$ErrorActionPreference='Stop'
if(Test-Path $ReceiptPath){throw 'Chronological receipt exists'}
$gate=Get-Content $MockReceiptPath -Raw|ConvertFrom-Json -AsHashtable -DateKind String
if($gate.mode -cne 'Mock' -or $gate.cases -ne 10 -or $gate.rejectingControls -ne 30 -or $gate.failed -or $gate.skipped){throw 'Exact independent10/30 batch prerequisite required'}
foreach($pin in $gate.sourceHashes){if((Get-FileHash $pin.Path).Hash -cne $pin.Hash){throw 'Original batch input/assertion/model source changed'}}
$workspace=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$rootPath=[IO.Path]::GetFullPath($ArtifactRoot)
if(-not $rootPath.StartsWith((Join-Path $workspace '.mcpServer')+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Own excluded synthetic artifact root required'}
Write-Output 'Counted effects:10 SAME batch capture/retry/recovery/fullfield assertions against canonical files, retained scripted dependency effects/receipts. No native/SQLite/fullscope acceptance.'
if(-not $PSCmdlet.ShouldProcess($ArtifactRoot,'Run SAME10 batch scenarios through canonical dispatch and storage')){return}
Import-Module PowerShell.MCP -ErrorAction Stop
Import-Module powershell-yaml -ErrorAction Stop
. (Join-Path $PSScriptRoot '../lib-ps/governed-session-host.ps1')
. (Join-Path $PSScriptRoot '../lib-ps/governed-session-dispatch.ps1')
. (Join-Path $PSScriptRoot 'GovernedSessionHost.TransportModel.ps1')
. (Join-Path $PSScriptRoot 'SessionBatchStorageObservation.ps1')
$dispatch=(Get-Command Invoke-McpGovernedSessionDispatch).ScriptBlock
$hostCall=(Get-Command Invoke-McpGovernedSessionHost).ScriptBlock
$storageRead=(Get-Command Get-SessionBatchStorageSnapshot).ScriptBlock
$transport=(Get-Command Invoke-MockSessionHostTransport).ScriptBlock
$evidence=[Collections.Generic.List[object]]::new()
$factory={
 param($Root)
 $dispatchLocal=$dispatch;$hostLocal=$hostCall;$storageLocal=$storageRead;$transportLocal=$transport
 $tape=[Collections.Generic.List[object]]::new()
 $state=@{turns=@{};events=@{};effects=$tape;mutation=''}
 $io=@{armed=$false;hit=$false;count=0}
 $store=Join-Path $Root 'store';$outbox=Join-Path $Root 'outbox'
 $server={param($Operation,$Binding,$Data) & $transportLocal -State $state -Operation $Operation -Root $Binding -InputObject $Data}.GetNewClosure()
 $policy={param($Binding,$Operation,$Data) [void]$Binding;[void]$Operation;[void]$Data;return @{code='allow'}}
 $observer={param($Stage,$Binding)
  $tape.Add(@{operation=$Stage;requestId=$Binding.requestId})
  if($io.armed -and $Stage -ceq 'CaptureJournal'){$io.count++;if($io.count -eq 2){$io.armed=$false;$io.hit=$true;throw 'Injected second batch journal interruption'}}
 }.GetNewClosure()
 $invoke={param($Envelope,$Context) return & $dispatchLocal -Envelope $Envelope -HostOptions @{StoreDirectory=$store;OutboxRoot=$outbox;HostContext=$Context;Server=$server;CompletionPolicy=$policy;StepObserver=$observer}}.GetNewClosure()
 $seed={param($Envelope,$Context)
  $action=$Envelope.method -ceq 'workflow.sessionlog.appendActions'
  $kind=if($action){'actions'}else{'dialogItems'};$operation=if($action){'appendAction'}else{'appendDialog'}
  $item=$Envelope.params[$kind][0]
  return & $hostLocal -StoreDirectory $store -OutboxRoot $outbox -HostContext $Context -Operation $operation -InputObject @{requestId=$Envelope.params.requestId;payloadJson=(ConvertTo-Json $item -Depth 100 -Compress)} -Server $server -CompletionPolicy $policy -StepObserver $observer
 }.GetNewClosure()
 $snapshot={return & $storageLocal -StoreDirectory $store -OutboxRoot $outbox}.GetNewClosure()
 $inspect={$disk=& $storageLocal -StoreDirectory $store -OutboxRoot $outbox;return @{server=$state;tape=$tape.ToArray();store=$store;outbox=$outbox;manifests=$disk.manifests;faultHit=$io.hit}}.GetNewClosure()
 $arm={$io.armed=$true;$io.hit=$false;$io.count=0}.GetNewClosure()
 $reopen={$io.armed=$false;$tape.Add(@{operation='DiskReopen';store=$store;outbox=$outbox})}.GetNewClosure()
 $evidence.Add(@{root=$Root;store=$store;outbox=$outbox})
 return @{Invoke=$invoke;Inspect=$inspect;SeedLegacy=$seed;StorageSnapshot=$snapshot;ArmFault=$arm;Restart=$reopen}
}.GetNewClosure()
try{& (Join-Path $PSScriptRoot 'GovernedSessionBatch.Contract.ps1') -Mode Production -ReceiptPath $ReceiptPath -ArtifactRoot $ArtifactRoot -FixtureDirectory $PSScriptRoot -ProviderFactory $factory}
finally{
 $var1=@{timestamp=Get-Date -Format o;mockReceipt=$MockReceiptPath;roots=$evidence.ToArray();sourceHashes=@(Get-FileHash $PSCommandPath,(Join-Path $PSScriptRoot 'SessionBatchStorageObservation.ps1'),(Join-Path $PSScriptRoot '../lib-ps/governed-session-dispatch.ps1'),(Join-Path $PSScriptRoot '../lib-ps/governed-session-host.ps1'),(Join-Path $PSScriptRoot '../lib-ps/governed-audit-outbox.ps1'),(Join-Path $PSScriptRoot '../lib-ps/governed-turn-store.ps1')|Select-Object Path,Hash);scope='Canonical file components with scripted existing typed-client dependency and injected publication interruption; native process/SQLite/cumulative remain open';productComplete=$false}|ConvertTo-Json -Depth 80
 Add-LinesToFile ($ReceiptPath+'.evidence.json') -Content $var1|Out-Null
}