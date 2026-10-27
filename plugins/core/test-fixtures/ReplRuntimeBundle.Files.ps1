#Requires -Version 7.5
# SPDX-License-Identifier: Apache-2.0
<# .SYNOPSIS
 Bind SAME45 offline package/provenance cases to physical sources, ZIP and JSON reads.
 #>
[CmdletBinding(SupportsShouldProcess)]
param([Parameter(Mandatory)][string]$ReceiptPath,[Parameter(Mandatory)][string]$MockReceiptPath,[Parameter(Mandatory)][string]$ArtifactRoot)
$ErrorActionPreference='Stop'
$workspace=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$allowed=(Join-Path $workspace '.mcpServer')+[IO.Path]::DirectorySeparatorChar
if(-not [IO.Path]::GetFullPath($ArtifactRoot).StartsWith($allowed,[StringComparison]::OrdinalIgnoreCase)){throw 'Own synthetic root required'}
$mock=Get-Content $MockReceiptPath -Raw|ConvertFrom-Json -AsHashtable -DateKind String
if($mock.mode -cne 'Mock' -or $mock.cases -ne 45 -or $mock.rejectingControls -ne 13 -or $mock.failed -or $mock.skipped){throw 'Exact independent45/13 bundle prerequisite required'}
foreach($pin in $mock.sourceHashes){if((Get-FileHash $pin.Path).Hash -cne $pin.Hash){throw 'Original bundle inputs/assertions/model changed'}}
Write-Output 'Counted effects:45 SAME tiny synthetic source/ZIP/provenance cases, no .NET publication, runtime installation or real artifact acceptance. Retain all input files, archives and manifests.'
if(-not $PSCmdlet.ShouldProcess($ArtifactRoot,'Run SAME45 physical package assertions')){return}
Import-Module PowerShell.MCP -ErrorAction Stop
. (Join-Path $PSScriptRoot '../lib-ps/repl-runtime-bundle.ps1')
$publish=(Get-Command New-McpReplRuntimeBundle).ScriptBlock
$evidence=[Collections.Generic.List[object]]::new()
$factory={
 $provider={param($InputSpec,$RequiredPackages)
  $root=Join-Path $ArtifactRoot ([guid]::NewGuid().ToString('N'));$sources=Join-Path $root 'source';$output=Join-Path $root 'output'
  [void][IO.Directory]::CreateDirectory($sources)
  $spec=ConvertFrom-Json (ConvertTo-Json $InputSpec -Depth 50) -AsHashtable -DateKind String
  foreach($file in $spec.files){
   if($file.path -notmatch '(^/|\\|(^|/)\.\.(/|$)|:)'){
    $path=Join-Path $sources $file.path;[void][IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($path))
    [IO.File]::WriteAllBytes($path,[Convert]::FromBase64String($file.bytes))
   }
   $file.Remove('bytes')
  }
  $result=& $publish -Spec $spec -PackageRequirements $RequiredPackages -SourceDirectory $sources -OutputDirectory $output -Confirm:$false
  $evidence.Add(@{root=$root;result=$result})
  if(-not $result.ok){return $result}
  $manifest=ConvertFrom-Json ([IO.File]::ReadAllText($result.manifestPath)) -AsHashtable -DateKind String
  if((Get-FileHash $result.archivePath).Hash -cne $manifest.archiveSha256){throw 'Actual archive digest mismatch'}
  $entries=[Collections.Generic.List[object]]::new()
  $zip=[IO.Compression.ZipFile]::OpenRead($result.archivePath)
  try{foreach($entry in $zip.Entries){$stream=$entry.Open();$bytes=[IO.MemoryStream]::new();try{$stream.CopyTo($bytes);$entries.Add(@{path=$entry.FullName;bytes=[Convert]::ToBase64String($bytes.ToArray())})}finally{$bytes.Dispose();$stream.Dispose()}}}finally{$zip.Dispose()}
  return @{ok=$result.ok;code=$result.code;manifest=$manifest;archiveEntries=$entries.ToArray();effects=$result.effects}
 }.GetNewClosure()
 return $provider
}.GetNewClosure()
try{& (Join-Path $PSScriptRoot 'ReplRuntimeBundle.Contract.ps1') -Mode Production -ReceiptPath $ReceiptPath -ProviderFactory $factory}
finally{$var1=@{timestamp=Get-Date -Format o;mockReceipt=$MockReceiptPath;cases=$evidence.ToArray();sourceHashes=@(Get-FileHash $PSCommandPath,(Join-Path $PSScriptRoot '../lib-ps/repl-runtime-bundle.ps1')|Select-Object Path,Hash);scope='Physical synthetic source/archive/provenance binding only; real Nuke self-contained runtime/module acceptance still required';productComplete=$false}|ConvertTo-Json -Depth 80;Add-LinesToFile ($ReceiptPath+'.evidence.json') -Content $var1|Out-Null}

