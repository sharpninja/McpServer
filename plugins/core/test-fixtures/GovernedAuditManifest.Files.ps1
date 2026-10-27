#Requires -Version 7.5
# SPDX-License-Identifier: Apache-2.0
<#
.SYNOPSIS
Runs unchanged manifest scenarios against the canonical outbox's durable files.
.DESCRIPTION
The existing submit/read seam receives deterministic receipts. No native network,
SQLite service, host bootstrap or full lifecycle acceptance is inferred.
#>
[CmdletBinding(SupportsShouldProcess)]
param([Parameter(Mandatory)][string]$ReceiptPath,[Parameter(Mandatory)][string]$MockReceiptPath,[Parameter(Mandatory)][string]$ArtifactRoot)
$ErrorActionPreference='Stop'
$mock=Get-Content $MockReceiptPath -Raw|ConvertFrom-Json -AsHashtable -DateKind String
if($mock.mode -cne 'Mock' -or $mock.cases -ne 20 -or $mock.rejectingControls -ne 12 -or $mock.failed -ne 0 -or $mock.skipped -ne 0){throw 'Exact independent20/12manifest prerequisite required'}
foreach($pin in $mock.sourceHashes){if((Get-FileHash $pin.Path).Hash -cne $pin.Hash){throw 'Frozen manifest assertions/model changed'}}
if(-not [IO.Path]::IsPathRooted($ArtifactRoot)){throw 'Synthetic artifact root must be absolute'}
Write-Output 'Counted effects:20 fresh canonical outbox file trees, injected committed receipts, full storage/publication evidence and SAME assertion receipt; no external writes.'
if(-not $PSCmdlet.ShouldProcess($ArtifactRoot,'Run SAME20manifest scenarios on canonical retained files')){return}
Import-Module PowerShell.MCP -ErrorAction Stop
Import-Module powershell-yaml -ErrorAction Stop
. (Join-Path $PSScriptRoot '../lib-ps/governed-audit-outbox.ps1')
$outboxForProvider=(Get-Command Invoke-McpGovernedAuditOutbox).ScriptBlock
$rootForProvider=$ArtifactRoot
$artifactEvidence=[Collections.Generic.List[object]]::new()
$factory={param($Owner,$Seed)
 $root=Join-Path $rootForProvider ([guid]::NewGuid().ToString('N'))
 $keyJson=ConvertTo-Json -InputObject @($Owner.workspacePath,$Owner.agentIdentity,$Owner.mcpSessionId,$Owner.hostSessionKey) -Compress
 $directory=Join-Path $root ([Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($keyJson))).ToLowerInvariant())
 if($Seed.orders.Count){
  [void][IO.Directory]::CreateDirectory($directory);$orders=@{}
  foreach($id in $Seed.orders.Keys){$digest=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($id))).ToLowerInvariant();$orders[$digest]=$Seed.orders[$id]}
  $var1=ConvertTo-Yaml @{version=1;orders=$orders}
  Add-LinesToFile (Join-Path $directory 'sequence.yaml') -Content $var1|Out-Null
 }
 $fixtureState=$Seed
 $outboxCall=$outboxForProvider
 $contextForCall=$Owner
 $publication={param($Stage,$Path)
  if($Stage -cne 'published'){return}
  if([IO.Path]::GetFileName($Path) -ceq 'sequence.yaml'){
   $fixtureState.effects.Add(@{operation='PublishJournal';path=$Path;sha256=(Get-FileHash $Path).Hash;document=(ConvertFrom-Yaml ([IO.File]::ReadAllText($Path)))})
   if($fixtureState.crashAfterJournal){throw 'Injected interruption after committed full manifest before event publication'}
  }elseif($Path.EndsWith('.event.yaml',[StringComparison]::Ordinal)){$fixtureState.effects.Add(@{operation='PublishEvent';path=$Path;sha256=(Get-FileHash $Path).Hash})}
 }.GetNewClosure()
 $transport=@{
  submit={param($AuditRecord)
   $receipt=ConvertFrom-Json (ConvertTo-Json $AuditRecord -Depth 90 -Compress) -AsHashtable -DateKind String
   $null=$receipt.Remove('kind');$null=$receipt.Remove('payloadJson');$receipt.code='persisted';$receipt.revision=[long]$receipt.order
   $fixtureState.server[$AuditRecord.eventId]=$receipt;$fixtureState.effects.Add(@{operation='Submit';eventId=$AuditRecord.eventId})
   return $receipt
  }.GetNewClosure()
  read={param($AuditRecord) return $fixtureState.server[$AuditRecord.eventId]}.GetNewClosure()
 }
 $invoke={param($Operation,$InputData)
  if($Operation -ceq 'Restart'){$fixtureState.crashAfterJournal=$false;$fixtureState.effects.Add(@{operation='Restart'});return @{code='restarted'}}
  return & $outboxCall -RootDirectory $root -Context $contextForCall -Operation $Operation -InputObject $InputData -Transport $transport -PublicationObserver $publication
 }.GetNewClosure()
 $inspect={
  $records=@(foreach($folder in @('pending','archive','quarantine','rejected')){
   $path=Join-Path $directory $folder
   if(Test-Path $path){foreach($file in @(Get-ChildItem $path -File -Filter '*.event.yaml')){ConvertFrom-Yaml ([IO.File]::ReadAllText($file.FullName))}}
  })
  return @{records=$records;effects=$fixtureState.effects.ToArray();root=$root}
 }.GetNewClosure()
 $artifactEvidence.Add(@{root=$root;directory=$directory;effects=$fixtureState.effects})
 return @{Invoke=$invoke;Inspect=$inspect}
}.GetNewClosure()
try{& (Join-Path $PSScriptRoot 'GovernedAuditManifest.Contract.ps1') -Mode Production -ReceiptPath $ReceiptPath -ProviderFactory $factory}
finally{
 $var1=@{timestamp=Get-Date -Format o;sourceHashes=@(Get-FileHash $PSCommandPath,(Join-Path $PSScriptRoot '../lib-ps/governed-audit-outbox.ps1')|Select-Object Path,Hash);roots=$artifactEvidence.ToArray();scope='Actual canonical journal/files with scripted transport replies only';productComplete=$false}|ConvertTo-Json -Depth 100
 Add-LinesToFile ($ReceiptPath+'.evidence.json') -Content $var1|Out-Null
}
