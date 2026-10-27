# SPDX-License-Identifier: Apache-2.0
function Get-SessionBatchStorageSnapshot {
 <#
 .SYNOPSIS
 Reads actual synthetic capture keys, counters, filenames, aliases and full file bytes.
 .PARAMETER StoreDirectory
 Existing canonical A8 directory under this worktree's private synthetic root.
 .PARAMETER OutboxRoot
 Existing canonical audit root under this worktree's private synthetic root.
 .OUTPUTS
 Independent native snapshot; no writes or fabricated storage fields.
 #>
 [CmdletBinding()]
 param([string]$StoreDirectory,[string]$OutboxRoot)
 $workspace=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
 $allowed=(Join-Path $workspace '.mcpServer')+[IO.Path]::DirectorySeparatorChar
 foreach($directory in @($StoreDirectory,$OutboxRoot)){if(-not [IO.Path]::GetFullPath($directory).StartsWith($allowed,[StringComparison]::OrdinalIgnoreCase)){throw 'Only task-owned synthetic storage may be observed'}}
 $keys=[Collections.Generic.List[object]]::new();$counters=[Collections.Generic.List[object]]::new();$manifests=[Collections.Generic.List[object]]::new()
 foreach($file in @(Get-ChildItem $OutboxRoot -Recurse -File -Filter 'sequence.yaml' | Sort-Object FullName)){
  $journal=ConvertFrom-Yaml ([IO.File]::ReadAllText($file.FullName))
  foreach($key in @($journal.orders.Keys|Sort-Object)){$counters.Add($journal.orders[$key])}
  if($journal.evidence){foreach($manifest in $journal.evidence.Values){
   $manifests.Add($manifest)
   foreach($key in @($manifest.captureKeys.Keys|Sort-Object)){$keys.Add(@{key=$key;eventId=$manifest.captureKeys[$key].eventId})}
  }}
 }
 $files=@(Get-ChildItem @($StoreDirectory,$OutboxRoot) -Recurse -File | Where-Object Extension -In '.json','.yaml' | Sort-Object FullName | ForEach-Object {
  $bytes=[IO.File]::ReadAllBytes($_.FullName)
  [ordered]@{path=$_.FullName;sha256=(Get-FileHash $_.FullName).Hash;bytes=[Convert]::ToBase64String($bytes)}
 })
 return @{captureKeys=$keys.ToArray();counter=$counters.ToArray();filenames=@(Get-ChildItem $OutboxRoot -Recurse -File -Filter '*.event.yaml'|Sort-Object Name|ForEach-Object {$_.Name});stateJson=(ConvertTo-Json -InputObject $files -Depth 20 -Compress);aliasBytes=[IO.File]::ReadAllText((Join-Path $StoreDirectory 'turn-store.json'));manifests=$manifests.ToArray()}
}
