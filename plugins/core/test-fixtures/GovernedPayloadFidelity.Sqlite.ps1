#Requires -Version 7.5
# SPDX-License-Identifier: Apache-2.0
[CmdletBinding(SupportsShouldProcess)]
param(
 [Parameter(Mandatory)][string]$ReceiptPath,
 [Parameter(Mandatory)][string]$MockReceiptPath,
 [Parameter(Mandatory)][string]$AssemblyDirectory,
 [Parameter(Mandatory)][string]$ArtifactRoot,
 [string]$HostedAssemblyDirectory
)
$ErrorActionPreference='Stop'
$workspace=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$artifactDirectory=[IO.Path]::GetFullPath($ArtifactRoot)
$allowedRoot=Join-Path $workspace '.mcpServer'
if(-not $artifactDirectory.StartsWith($allowedRoot+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Own private fixture root required'}
if((Test-Path $ReceiptPath) -or (Test-Path ($ReceiptPath+'.evidence.json'))){throw 'Chronological receipt exists'}
$gate=Get-Content $MockReceiptPath -Raw|ConvertFrom-Json -DateKind String
if($gate.mode -cne 'Mock' -or $gate.cases -ne 22 -or $gate.rejectingControls -ne 65 -or $gate.failed -ne 0 -or $gate.skipped -ne 0){throw 'Exact22/65 independent prerequisite required'}
foreach($pin in $gate.sourceHashes){if((Get-FileHash $pin.Path).Hash -cne $pin.Hash){throw 'Original fidelity inputs/assertions/model changed'}}
Write-Output 'Counted effects: SAME22 fidelity vectors/assertions; actual normalizer, ParamsPath reader and optional existing public hosted PowerShell API, typed client with injected HTTP handler forwarding to real SQLite service, canonical outbox capture/archive/reopen. Two distinct metadata events per vector, retained synthetic databases/outboxes and two receipts. No native REPL process/network/authentication or lifecycle acceptance.'
if(-not $PSCmdlet.ShouldProcess($ReceiptPath,'Bind frozen fidelity assertions to actual canonical components')){return}
Import-Module PowerShell.MCP -ErrorAction Stop
Import-Module powershell-yaml -ErrorAction Stop
. (Join-Path $PSScriptRoot '../lib-ps/governed-payload.ps1')
. (Join-Path $PSScriptRoot '../lib-ps/governed-audit-outbox.ps1')
. (Join-Path $PSScriptRoot '../lib-ps/repl-params-transfer.ps1')
. (Join-Path $PSScriptRoot 'ServerAuditFramework.ps1')
$assembly=Get-ServerAuditFixtureAssembly -AssemblyDirectory $AssemblyDirectory
$type=$assembly.GetType('QBrainAi.Support.Mcp.Tests.Services.ServerAuditSqliteFixture',$true)
$hostedFixture=$null
if($HostedAssemblyDirectory){
 $hostedPath=Join-Path ([IO.Path]::GetFullPath($HostedAssemblyDirectory)) 'QBrainAi.McpAgent.Tests.dll'
 if(-not (Test-Path $hostedPath)){throw 'Compiled existing public hosted API fixture required'}
 $framework=Get-ServerAuditFramework -RuntimeLine @(& dotnet --list-runtimes) -RuntimeMajor ([Environment]::Version.Major)
 $hostedContext=[Runtime.Loader.AssemblyLoadContext]::new(('hosted-fidelity-'+[guid]::NewGuid().ToString('N')),$false)
 $hostedFiles=Get-ServerAuditAssemblyFile -FrameworkFile @(Get-ChildItem $framework.FullName -Filter '*.dll') -ApplicationFile @(Get-ChildItem $HostedAssemblyDirectory -Filter '*.dll')
 foreach($dependency in $hostedFiles){try{$null=$hostedContext.LoadFromAssemblyPath($dependency)}catch [BadImageFormatException]{Write-Verbose ('Native dependency deferred: '+[IO.Path]::GetFileName($dependency))}}
 $hostedAssembly=$hostedContext.LoadFromAssemblyPath($hostedPath)
 $hostedType=$hostedAssembly.GetType('QBrainAi.McpAgent.Tests.GovernedHostedPowerShellFixture',$true)
 $hostedFixture=$hostedType.GetMethod('Create').Invoke($null,[object[]]@([string](Join-Path $artifactDirectory 'existing-public-hosted-api')))
}
if(-not $type.GetMethod('InvokeTypedClient')){throw 'Current compiled typed-client adapter required'}
$tokens=$null;$errors=$null
$entryAst=[Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot '../lib-ps/repl-invoke.ps1'),[ref]$tokens,[ref]$errors)
if($errors.Count){throw 'Actual entry reader parse failure'}
$reader=$entryAst.Find({param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -ceq 'Read-ReplInvocationParam'},$true)
if(-not $reader){throw 'Actual existing ParamsPath reader missing'}
. ([scriptblock]::Create($reader.Extent.Text))
$evidence=[Collections.Generic.List[object]]::new()
function Read-S6OutboxRecord {
 param([string]$Root,[string]$EventId,[string]$Folder)
 $files=@(Get-ChildItem $Root -Recurse -File -Filter ('*'+$EventId+'.event.yaml')|Where-Object {$_.Directory.Name -ceq $Folder})
 if($files.Count -ne 1){throw 'Exactly one actual retained event file required'}
 $record=ConvertFrom-Yaml ([IO.File]::ReadAllText($files[0].FullName))
 return @{path=$files[0].FullName;record=$record;fileSha256=(Get-FileHash $files[0].FullName).Hash}
}
function ConvertTo-S6ObservedFrame {
 param([string]$Boundary,[string]$Json)
 $document=[System.Text.Json.JsonDocument]::Parse($Json)
 try{
  return @{boundary=$Boundary;json=$Json;accuracyAccepted=(Test-McpGovernedReviewScore $document.RootElement.GetProperty('accuracy'));completenessAccepted=(Test-McpGovernedReviewScore $document.RootElement.GetProperty('completeness'))}
 }finally{$document.Dispose()}
}
$factory={
 $instance=$type.GetMethod('Create').Invoke($null,[object[]]@([string]$artifactDirectory))
 $root=Join-Path $artifactDirectory ([guid]::NewGuid().ToString('N'))
 $context=@{workspacePath='F:/fixture/qb';agentIdentity='Codex';mcpSessionId='Codex-20260101T000000Z-s6';hostSessionKey='s6-owned-host'}
 $request='req-20260101T000001Z-s6'
 $seed=@{workspacePath=$context.workspacePath;agentIdentity=$context.agentIdentity;mcpSessionId=$context.mcpSessionId;requestId=$request;status='in_progress'}
 $null=$instance.Invoke('Seed',(ConvertTo-Json $seed -Compress))
 $provider={
  param([string]$Operation,[Collections.IDictionary]$InputObject)
  if($Operation -cne 'RoundTrip'){throw 'Unknown frozen fidelity operation'}
  $frames=[Collections.Generic.List[object]]::new()
  $effects=[Collections.Generic.List[object]]::new()
  $calls=[Collections.Generic.List[object]]::new()
  $publications=[Collections.Generic.List[object]]::new()
  $observer={param($Stage,$Path) $publications.Add(@{stage=$Stage;path=$Path})}.GetNewClosure()
  $normalized=ConvertTo-McpGovernedPayloadJson -Json $InputObject.json
  $captured=Invoke-McpGovernedAuditOutbox -RootDirectory $root -Context $context -Operation Enqueue -InputObject @{requestId=$request;kind='metadata';payloadJson=$normalized} -PublicationObserver $observer
  if($captured.code -cne 'queued'){throw 'Actual core capture failed'}
  $core=Read-S6OutboxRecord -Root $root -EventId $captured.event.eventId -Folder pending
  $coreJson=$core.record.event.payloadJson
  $frames.Add((ConvertTo-S6ObservedFrame core $coreJson))
  $effects.Add(@{boundary='core';operation='write';byteLength=[Text.Encoding]::UTF8.GetByteCount($captured.event.payloadJson)})
  $effects.Add(@{boundary='core';operation='read';byteLength=[Text.Encoding]::UTF8.GetByteCount($coreJson)})
  $transfer=New-McpReplParamsTransfer -ParamsText $coreJson
  try{$hostJson=Read-ReplInvocationParam -ParamsPath $transfer.Path}finally{Remove-McpReplParamsTransfer $transfer}
  if(Test-Path $transfer.Path){throw 'Actual host transfer was not cleaned'}
  if($hostedFixture){
   $hostObserved=ConvertFrom-Json ($hostedFixture.RoundTrip($hostJson)) -AsHashtable -DateKind String
   $publications.Add(@{stage='existing-public-hosted-api';observation=$hostObserved})
   if(-not $hostObserved.result.success -or $hostObserved.result.hadErrors -or $hostObserved.result.errorOutput -or $hostObserved.payloadJson -isnot [string]){throw 'Actual existing hosted PowerShell API failed'}
   $hostJson=$hostObserved.payloadJson
  }
  $frames.Add((ConvertTo-S6ObservedFrame host $hostJson))
  $effects.Add(@{boundary='host';operation='write';byteLength=[Text.Encoding]::UTF8.GetByteCount($coreJson)})
  $effects.Add(@{boundary='host';operation='read';byteLength=[Text.Encoding]::UTF8.GetByteCount($hostJson)})
  if($hostJson -cne $captured.event.payloadJson){throw 'Actual host changed immutable captured payload'}
  $packet=ConvertTo-Json $captured.event -Depth 100 -Compress
  $sent=ConvertFrom-Json ($instance.InvokeTypedClient('Submit',$packet)) -AsHashtable -DateKind String
  $calls.Add(@{operation='Submit';result=$sent})
  if($sent.reply.code -cne 'persisted' -or $sent.reply.revision -lt 1 -or $sent.storedPayloadJson -cne $hostJson){throw 'Actual typed-client/service commit failed'}
  $frames.Add((ConvertTo-S6ObservedFrame transport $sent.wirePayloadJson))
  $effects.Add(@{boundary='transport';operation='write';byteLength=[Text.Encoding]::UTF8.GetByteCount($hostJson)})
  $effects.Add(@{boundary='transport';operation='read';byteLength=[Text.Encoding]::UTF8.GetByteCount($sent.wirePayloadJson)})
  $fixtureForTransport=$instance
  $transport=@{
   submit={param($AuditRecord) try{$reply=ConvertFrom-Json ($fixtureForTransport.InvokeTypedClient('Submit',(ConvertTo-Json $AuditRecord -Depth 100 -Compress))) -AsHashtable -DateKind String;$calls.Add(@{operation='Submit';result=$reply});return $reply.reply}catch{$calls.Add(@{operation='Submit';error=$_.Exception.ToString()});throw}}.GetNewClosure()
   read={param($AuditRecord) try{$reply=ConvertFrom-Json ($fixtureForTransport.InvokeTypedClient('Read',(ConvertTo-Json $AuditRecord -Depth 100 -Compress))) -AsHashtable -DateKind String;$calls.Add(@{operation='Read';result=$reply});return $reply.reply}catch{$calls.Add(@{operation='Read';error=$_.Exception.ToString()});throw}}.GetNewClosure()
  }
  $evidence.Add(@{stage='before-first-flush';databasePath=$instance.DatabasePath;outboxRoot=$root;captured=$captured;coreFile=$core;hostTransfer=$transfer;typedCalls=$calls;publications=$publications})
  $firstFlush=Invoke-McpGovernedAuditOutbox -RootDirectory $root -Context $context -Operation Flush -Transport $transport -PublicationObserver $observer
  if($firstFlush.persisted -ne 1 -or $firstFlush.pending -or $firstFlush.quarantined -or $firstFlush.rejected){throw 'Actual canonical replay/receipt gate failed'}
  $archived=Read-S6OutboxRecord -Root $root -EventId $captured.event.eventId -Folder archive
  $frames.Add((ConvertTo-S6ObservedFrame cache $archived.record.event.payloadJson))
  $effects.Add(@{boundary='cache';operation='write';byteLength=[Text.Encoding]::UTF8.GetByteCount($captured.event.payloadJson)})
  $effects.Add(@{boundary='cache';operation='read';byteLength=[Text.Encoding]::UTF8.GetByteCount($archived.record.event.payloadJson)})
  $second=Invoke-McpGovernedAuditOutbox -RootDirectory $root -Context $context -Operation Enqueue -InputObject @{requestId=$request;kind='metadata';payloadJson=$archived.record.event.payloadJson} -PublicationObserver $observer
  if($second.code -cne 'queued' -or $second.event.order -ne 2 -or $second.event.eventId -ceq $captured.event.eventId){throw 'Actual reopened outbox did not preserve distinct capture/order'}
  $secondDisk=Read-S6OutboxRecord -Root $root -EventId $second.event.eventId -Folder pending
  $frames.Add((ConvertTo-S6ObservedFrame outbox $secondDisk.record.event.payloadJson))
  $effects.Add(@{boundary='outbox';operation='write';byteLength=[Text.Encoding]::UTF8.GetByteCount($second.event.payloadJson)})
  $effects.Add(@{boundary='outbox';operation='read';byteLength=[Text.Encoding]::UTF8.GetByteCount($secondDisk.record.event.payloadJson)})
  $secondFlush=Invoke-McpGovernedAuditOutbox -RootDirectory $root -Context $context -Operation Flush -Transport $transport -PublicationObserver $observer
  if($secondFlush.persisted -ne 1 -or $secondFlush.pending -or $secondFlush.quarantined -or $secondFlush.rejected){throw 'Actual second capture was not independently committed'}
  $snapshot=ConvertFrom-Json ($instance.Invoke('Snapshot',(ConvertTo-Json $seed -Compress))) -AsHashtable -DateKind String
  $evidence.Add(@{databasePath=$instance.DatabasePath;outboxRoot=$root;captured=$captured;coreFile=$core;hostTransfer=$transfer;typedCalls=$calls.ToArray();firstFlush=$firstFlush;archive=$archived;second=$second;secondFile=$secondDisk;secondFlush=$secondFlush;snapshot=$snapshot;publications=$publications.ToArray()})
  return @{inputJson=$InputObject.json;capturedJson=$coreJson;captureHash=$captured.event.payloadHash.ToUpperInvariant();frames=$frames.ToArray();effects=$effects.ToArray()}
 }.GetNewClosure()
 return $provider
}.GetNewClosure()
try{& (Join-Path $PSScriptRoot 'GovernedPayloadFidelity.Contract.ps1') -Mode Production -ReceiptPath $ReceiptPath -ProviderFactory $factory}
finally{
 if($hostedFixture){$hostedFixture.Dispose()}
 $var1=[ordered]@{timestamp=(Get-Date -Format o);componentScope='Real canonical normalizer/host file reader/outbox and typed client serialization forwarding to real SQLite service; injected HTTP handler, no network/native REPL/lifecycle acceptance';mockReceipt=$MockReceiptPath;hostedAssemblyDirectory=$HostedAssemblyDirectory;sourceHashes=@(Get-FileHash -LiteralPath $PSCommandPath,(Join-Path $PSScriptRoot '../lib-ps/governed-payload.ps1'),(Join-Path $PSScriptRoot '../lib-ps/governed-audit-outbox.ps1')|Select-Object Path,Hash);cases=$evidence.ToArray();productComplete=$false}|ConvertTo-Json -Depth 100
 Add-LinesToFile -Path ($ReceiptPath+'.evidence.json') -Content $var1|Out-Null
}