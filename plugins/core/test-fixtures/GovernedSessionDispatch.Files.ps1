#Requires -Version 7.5
# SPDX-License-Identifier: Apache-2.0
<#
.SYNOPSIS
Bind SAME32 serialized method scenarios to canonical PowerShell dispatch/host/files.
.DESCRIPTION
Scripted existing typed-client dependencies only. No native REPL/process/SQLite or
fullscope acceptance. Mock source hashes and all original assertions remain pinned.
#>
[CmdletBinding(SupportsShouldProcess)]
param([Parameter(Mandatory)][string]$ReceiptPath,[Parameter(Mandatory)][string]$MockReceiptPath,[Parameter(Mandatory)][string]$ArtifactRoot)
$ErrorActionPreference='Stop'
$mock=Get-Content -Raw $MockReceiptPath|ConvertFrom-Json -AsHashtable -DateKind String
if($mock.mode -cne 'Mock' -or $mock.cases -ne 32 -or $mock.rejectingControls -ne 14 -or $mock.failed -ne 0 -or $mock.skipped -ne 0){throw 'Frozen32/14 independent method prerequisite required'}
foreach($pin in $mock.sourceHashes){if((Get-FileHash $pin.Path).Hash -cne $pin.Hash){throw 'Frozen method tests/model changed'}}
if(-not [IO.Path]::IsPathRooted($ArtifactRoot)){throw 'Synthetic artifact root must be absolute'}
Write-Output 'Counted effects:32canonical dispatch/host/A8/outbox roots, SAMEserialized inputs/assertions, scripted typed-client replies and retained receipt/evidence. No external/native service writes.'
if(-not $PSCmdlet.ShouldProcess($ArtifactRoot,'Run SAME32 methods through actual canonical dispatch/files')){return}
Import-Module PowerShell.MCP -ErrorAction Stop
Import-Module powershell-yaml -ErrorAction Stop
. (Join-Path $PSScriptRoot '../lib-ps/governed-session-dispatch.ps1')
. (Join-Path $PSScriptRoot 'GovernedSessionHost.TransportModel.ps1')
$dispatchForProvider=(Get-Command Invoke-McpGovernedSessionDispatch).ScriptBlock
$serverForProvider=(Get-Command Invoke-MockSessionHostTransport).ScriptBlock
$evidence=[Collections.Generic.List[object]]::new()
$factory={param($Root,$State,$ServerState)
 $store=Join-Path $Root 'store';$outbox=Join-Path $Root 'outbox'
 $dispatchCall=$dispatchForProvider;$serverCall=$serverForProvider;$fixtureState=$State;$fixtureServer=$ServerState
 $server={param($Operation,$Binding,$Data) & $serverCall -State $fixtureServer -Operation $Operation -Root $Binding -InputObject $Data}.GetNewClosure()
 $policy={param($Binding,$Operation,$Data) [void]$Binding;[void]$Operation;[void]$Data;return @{code=$fixtureState.policy}}.GetNewClosure()
 $observer={param($Stage,$Binding) $fixtureState.effects.Add(@{operation=$Stage;requestId=$Binding.requestId})}.GetNewClosure()
 $invoke={param($Envelope,$Context)
  $options=@{StoreDirectory=$store;OutboxRoot=$outbox;HostContext=$Context;Server=$server;CompletionPolicy=$policy;StepObserver=$observer}
  return & $dispatchCall -Envelope $Envelope -HostOptions $options
 }.GetNewClosure()
 $inspect={
  $snapshot=if(Test-Path (Join-Path $store 'turn-store.json')){ConvertFrom-Json ([IO.File]::ReadAllText((Join-Path $store 'turn-store.json'))) -AsHashtable -DateKind String}else{$null}
  $records=@(if(Test-Path $outbox){foreach($file in @(Get-ChildItem $outbox -Recurse -File -Filter '*.event.yaml')){@{path=$file.FullName;sha256=(Get-FileHash $file.FullName).Hash;document=(ConvertFrom-Yaml ([IO.File]::ReadAllText($file.FullName)))}}})
  return @{store=$snapshot;outboxes=$records;server=$fixtureServer;tape=$fixtureState.effects.ToArray()}
 }.GetNewClosure()
 $evidence.Add(@{root=$Root;store=$store;outbox=$outbox})
 return @{Invoke=$invoke;Inspect=$inspect}
}.GetNewClosure()
try{& (Join-Path $PSScriptRoot 'GovernedSessionDispatch.Contract.ps1') -EvidenceMode Production -ReceiptPath $ReceiptPath -ArtifactRoot $ArtifactRoot -NativeProviderFactory $factory}
finally{
 $var1=@{timestamp=Get-Date -Format o;mockReceipt=$MockReceiptPath;sourceHashes=@(Get-FileHash $PSCommandPath,(Join-Path $PSScriptRoot '../lib-ps/governed-session-dispatch.ps1'),(Join-Path $PSScriptRoot '../lib-ps/governed-session-host.ps1'),(Join-Path $PSScriptRoot '../lib-ps/governed-turn-store.ps1'),(Join-Path $PSScriptRoot '../lib-ps/governed-audit-alias-consumer.ps1'),(Join-Path $PSScriptRoot '../lib-ps/governed-audit-outbox.ps1'),(Join-Path $PSScriptRoot '../lib-ps/governed-payload.ps1')|Select-Object Path,Hash);roots=$evidence.ToArray();scope='Actual canonical method adapter/host/files; scripted typed-client replies. Native process/entrypoint/SQLite/generic batch/fullscope open';productComplete=$false}|ConvertTo-Json -Depth 100
 Add-LinesToFile ($ReceiptPath+'.evidence.json') -Content $var1|Out-Null
}
