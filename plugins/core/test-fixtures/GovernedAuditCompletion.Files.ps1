#Requires -Version 7.5
# SPDX-License-Identifier: Apache-2.0
<#
.SYNOPSIS
Binds the unchanged completion assertions to actual retained canonical outbox files.
.DESCRIPTION
Server replies remain scripted dependency inputs here. This does not claim SQLite,
native transport, host manifest capture or lifecycle completion acceptance.
#>
[CmdletBinding(SupportsShouldProcess)]
param([Parameter(Mandatory)][string]$ReceiptPath,[Parameter(Mandatory)][string]$ArtifactRoot,[Parameter(Mandatory)][string]$MockReceiptPath)
$ErrorActionPreference='Stop'
$mock=Get-Content $MockReceiptPath -Raw|ConvertFrom-Json -AsHashtable -DateKind String
if($mock.mode -cne 'Mock' -or $mock.cases -ne 32 -or $mock.rejectingControls -ne 17 -or $mock.failed -ne 0 -or $mock.skipped -ne 0){throw 'Exact independent32/17mock prerequisite required'}
foreach($pin in $mock.sourceHashes){if((Get-FileHash $pin.Path).Hash -cne $pin.Hash){throw 'Frozen common assertions/model changed'}}
if(-not [IO.Path]::IsPathRooted($ArtifactRoot)){throw 'Synthetic artifact root must be absolute'}
Write-Output 'Counted effects:32 fresh task-owned synthetic retained-file trees, scripted readback interactions, one same-assertion production receipt; no external write or lifecycle acceptance.'
if(-not $PSCmdlet.ShouldProcess($ArtifactRoot,'Run SAME32completion scenarios through actual canonical outbox files')){return}
Import-Module powershell-yaml -ErrorAction Stop
Import-Module PowerShell.MCP -ErrorAction Stop
. (Join-Path $PSScriptRoot '../lib-ps/governed-audit-outbox.ps1')
$outboxForProvider=(Get-Command Invoke-McpGovernedAuditOutbox).ScriptBlock
$artifactForProvider=$ArtifactRoot
$factory={
 param($Seed)
 $root=Join-Path $artifactForProvider ([guid]::NewGuid().ToString('N'))
 $context=ConvertFrom-Json (ConvertTo-Json $Seed.context -Depth 90 -Compress) -AsHashtable -DateKind String
 $context.hostSessionKey='completion-component-fixture'
 $keyJson=ConvertTo-Json -InputObject @($context.workspacePath,$context.agentIdentity,$context.mcpSessionId,$context.hostSessionKey) -Compress
 $directory=Join-Path $root ([Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($keyJson))).ToLowerInvariant())
 foreach($folder in @('pending','archive','quarantine','rejected')){[void][IO.Directory]::CreateDirectory((Join-Path $directory $folder))}
 $prefix=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($Seed.input.requestId))).ToLowerInvariant()
 $counter=0
 foreach($id in $Seed.recordIds){
  $record=$Seed.records[$id];$counter++
  $path=Join-Path (Join-Path $directory $record.state) ($prefix+'-'+$counter.ToString('D16')+'-'+$record.event.eventId+'.event.yaml')
  $document=@{version=1;attempts=1;event=$record.event;receipt=$record.receipt}
  $var1=ConvertTo-Yaml $document
  Add-LinesToFile $path -Content $var1|Out-Null
 }
 $effects=[Collections.Generic.List[object]]::new()
 $seedForRead=$Seed
 $transport=@{read={param($AuditRecord)
  $eventCopy=ConvertFrom-Json (ConvertTo-Json $AuditRecord -Depth 90 -Compress) -AsHashtable -DateKind String
  $effects.Add(@{operation='ReadServer';event=$eventCopy})
  if($seedForRead.throwServer){throw 'scripted independent read failure'}
  return ConvertFrom-Json (ConvertTo-Json -InputObject $seedForRead.server[$AuditRecord.eventId] -Depth 90 -Compress) -AsHashtable -DateKind String -NoEnumerate
 }.GetNewClosure()}
 $publication={param($Stage,$Path)
  if($Stage -ceq 'completion-enumerated'){$effects.Add(@{operation='ListRecord';path=$Path})}
  elseif($Stage -ceq 'completion-read'){$effects.Add(@{operation='ReadRecord';path=$Path})}
 }.GetNewClosure()
 $result=& $outboxForProvider -RootDirectory $root -Context $context -Operation CheckCompletion -InputObject $Seed.input -Transport $transport -PublicationObserver $publication
 return @{result=$result;effects=$effects.ToArray();artifactRoot=$root}
}.GetNewClosure()
& (Join-Path $PSScriptRoot 'GovernedAuditCompletion.Contract.ps1') -Mode Production -ReceiptPath $ReceiptPath -ProviderFactory $factory
