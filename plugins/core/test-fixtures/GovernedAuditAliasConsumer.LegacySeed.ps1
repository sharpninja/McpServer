#Requires -Version 7.5
# SPDX-License-Identifier: Apache-2.0
function Initialize-LegacyAuditAliasFixture {
 <# .SYNOPSIS
 FR-QB-003/TR-QB-AUDIT-001: Seed prior schema1 A8 plus C2 Node outbox inputs independently from either provider.
 .DESCRIPTION
 Fixture-only native objects mirror pinned prior contracts. No current production/model lifecycle function is used to create source state.
 #>
 [CmdletBinding(SupportsShouldProcess)]
 param([Collections.IDictionary]$Context,[object[]]$Bindings,[string]$Case,[string]$Store,[string]$Outbox,[switch]$AppliedJournal,[ValidateSet('none','wrong-owner','wrong-target')][string]$JournalMutation='none')
 if(-not $PSCmdlet.ShouldProcess(($Store+';'+$Outbox),('Seed '+(1+(3+[int][bool]$AppliedJournal)*$Bindings.Count)+' native prior-format documents and '+(4*$Bindings.Count)+' private fixture folders'))){return}
 function Get-LegacyFixtureDigest {
  param([string]$Text)
  [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($Text))).ToLowerInvariant()
 }
 function Write-LegacyFixtureDocument {
  param([string]$Path,[object]$Value)
  [void][IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($Path))
  $var1=ConvertTo-Json -InputObject $Value -Depth 80 -Compress
  Add-LinesToFile $Path -Content $var1|Out-Null
 }
 $turns=[Collections.Generic.List[object]]::new();$aliases=[Collections.Generic.List[object]]::new()
 foreach($binding in $Bindings){
  $turn=ConvertFrom-Json (ConvertTo-Json -InputObject $binding -Depth 40 -Compress) -AsHashtable -DateKind String
  $turn.delivered=$false;$turn.status='open'
  if($Case -cne 'none'){
   $alias=ConvertFrom-Json (ConvertTo-Json -InputObject $binding -Depth 40 -Compress) -AsHashtable -DateKind String
   $alias.pendingId=$binding.requestId;$alias.requestId=$binding.requestId.Replace('pending-nonce-','minted-');$alias.aliasedAtUtc='2026-10-14T23:01:00.0000000Z';$alias.acks=@('another-consumer')
   $aliases.Add($alias);$turn.requestId=$alias.requestId
  }
  $turns.Add($turn)
  $identity=@($Context.workspacePath,$Context.agentIdentity,$binding.mcpSessionId,$binding.hostSessionKey)
  $directory=Join-Path $Outbox (Get-LegacyFixtureDigest (ConvertTo-Json -InputObject $identity -Compress))
  foreach($folder in @('pending','archive','quarantine','rejected')){[void][IO.Directory]::CreateDirectory((Join-Path $directory $folder))}
  $logical=Get-LegacyFixtureDigest $binding.requestId
  $orders=@{};$orders[$logical]=2
  Write-LegacyFixtureDocument (Join-Path $directory 'sequence.yaml') @{version=1;orders=$orders}
  foreach($order in 1..2){
   $payload=ConvertTo-Json -InputObject @{text=('immutable '+$order);unicode=([string][char]0x96EA)} -Compress
   $eventData=[ordered]@{workspacePath=$Context.workspacePath;agentIdentity=$Context.agentIdentity;mcpSessionId=$binding.mcpSessionId;requestId=$binding.requestId;eventId=[guid]::NewGuid().ToString('D');order=$order;kind='dialog';payloadJson=$payload;payloadHash=(Get-LegacyFixtureDigest $payload)}
   $filename=$logical+'-'+([long]$order).ToString('D16')+'-'+$eventData.eventId+'.event.yaml'
   Write-LegacyFixtureDocument (Join-Path (Join-Path $directory 'pending') $filename) @{version=1;event=$eventData;attempts=0}
  }
  if($AppliedJournal -and $Case -cne 'none'){
   $applied=[ordered]@{workspacePath=$Context.workspacePath;agent=$Context.agentIdentity;mcpSessionId=$binding.mcpSessionId;hostSessionKey=$binding.hostSessionKey;ingressNonce=$binding.ingressNonce;pendingId=$binding.requestId;requestId=$binding.requestId.Replace('pending-nonce-','minted-');aliasedAtUtc='2026-10-14T23:01:00.0000000Z'}
   if($JournalMutation -ceq 'wrong-owner'){$applied.mcpSessionId='foreign-session'}
   if($JournalMutation -ceq 'wrong-target'){$applied.requestId='foreign-minted'}
   Write-LegacyFixtureDocument (Join-Path $directory 'applied-aliases.yaml') @{version=1;aliases=@($applied)}
  }
 }
 $snapshot=[ordered]@{schemaVersion=1;workspacePath=$Context.workspacePath;agentIdentity=$Context.agentIdentity;aliasConsumers=@($Context.aliasConsumers);revision=7;hosts=@(@{hostSessionKey='host-alias';turns=$turns.ToArray()});aliases=$aliases.ToArray()}
 Write-LegacyFixtureDocument (Join-Path $Store 'turn-store.json') $snapshot
}

