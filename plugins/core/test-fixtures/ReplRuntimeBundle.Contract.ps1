#Requires -Version 7.5
# SPDX-License-Identifier: Apache-2.0
<# .SYNOPSIS
 TR-QB-RUNTIME-001: Same package provenance, offline prerequisite and archive assertions.
 #>
[CmdletBinding(SupportsShouldProcess)]
param([ValidateSet('Mock','Production')][string]$Mode='Mock',[Parameter(Mandatory)][string]$ReceiptPath,[scriptblock]$ProviderFactory)
$ErrorActionPreference='Stop'
if(Test-Path $ReceiptPath){throw 'Chronological receipt exists'}
if($Mode -ceq 'Production' -and -not $ProviderFactory){throw 'Explicit actual bundle provider required'}
. (Join-Path $PSScriptRoot 'ReplRuntimeBundle.Model.ps1')
$script:bundleAssertions=0
function Assert-BundleCondition {
 param([bool]$Condition,[string]$Message)
 $script:bundleAssertions++;if(-not $Condition){throw ('bundle_assert:'+$Message)}
}
function Get-BundleInput {
 param([string]$Rid)
 $exe=if($Rid.StartsWith('win-')){'.exe'}else{''}
 $files=[Collections.Generic.List[object]]::new();$packages=[Collections.Generic.List[object]]::new()
 foreach($entry in @(
  @{id='repl';version='1.2.3';path=('repl/qbrain-ai-repl'+$exe);license='MIT'},
  @{id='dotnet';version='10.0.0';path='repl/System.Private.CoreLib.dll';license='MIT'},
  @{id='powershell';version='7.6.7';path=('powershell/pwsh'+$exe);license='MIT'},
  @{id='PowerShell.MCP';version='1.14.0';path='modules/PowerShell.MCP/PowerShell.MCP.psd1';license='package-original'},
  @{id='powershell-yaml';version='0.4.12';path='modules/powershell-yaml/powershell-yaml.psd1';license='package-original'},
  @{id='core';version='1.2.3';path='core/governed-session-dispatch.ps1';license='Apache-2.0'}
 )){
  $notice='licenses/'+$entry.id+'/LICENSE.txt'
  $packages.Add(@{id=$entry.id;version=$entry.version;licenseExpression=$entry.license;licenseDocuments=@($notice)})
  foreach($path in @($entry.path,$notice)){$files.Add(@{path=$path;packageId=$entry.id;bytes=[Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes(('Original '+$entry.id+' '+$path+"`r`n")))})}
 }
 return @{rid=$Rid;version='1.2.3';sourceCommit=('a'*40);files=$files.ToArray();packages=$packages.ToArray()}
}
function Invoke-BundleScenario {
 param([scriptblock]$Provider,[Collections.IDictionary]$InputSpec,[string]$Refusal='')
 $source=ConvertTo-Json $InputSpec -Depth 50 -Compress
 $requiredPackages=(Get-BundleInput $InputSpec.rid).packages
 $result=& $Provider $InputSpec $requiredPackages
 Assert-BundleCondition ((ConvertTo-Json $InputSpec -Depth 50 -Compress) -ceq $source) 'input unchanged'
 if($Refusal){Assert-BundleCondition (-not $result.ok -and $result.code -ceq $Refusal) 'missing or invalid prerequisite refused';Assert-BundleCondition (-not $result.manifest -and -not $result.archiveEntries) 'no publication after refusal';return $result}
 Assert-BundleCondition ($result.ok -eq $true -and $result.code -ceq 'bundle_created') 'successful complete bundle'
 $manifest=$result.manifest
 foreach($field in @('rid','version','sourceCommit')){Assert-BundleCondition ($manifest[$field] -ceq $InputSpec[$field]) ('exact '+$field)}
 Assert-BundleCondition ($manifest.schemaVersion -eq 1 -and $manifest.manifestLicense -ceq 'Apache-2.0') 'manifest own license separate from included packages'
 Assert-BundleCondition ((ConvertTo-Json $manifest.packages -Depth 30 -Compress) -ceq (ConvertTo-Json $InputSpec.packages -Depth 30 -Compress)) 'exact original package versions and license provenance'
 Assert-BundleCondition (@($manifest.files).Count -eq $InputSpec.files.Count -and @($result.archiveEntries).Count -eq $InputSpec.files.Count) 'complete manifest and archive inventory'
 foreach($file in $InputSpec.files){
  $match=@($manifest.files|Where-Object path -CEQ $file.path);$entry=@($result.archiveEntries|Where-Object path -CEQ $file.path)
  Assert-BundleCondition ($match.Count -eq 1 -and $entry.Count -eq 1) 'unique exact original file path'
  $bytes=[Convert]::FromBase64String($file.bytes);$hash=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes))
  Assert-BundleCondition ($match[0].sha256 -ceq $hash -and $match[0].byteLength -eq $bytes.Length) 'exact original file digest and length'
  Assert-BundleCondition ($match[0].packageId -ceq $file.packageId) 'per-file original package provenance'
  Assert-BundleCondition ($entry[0].bytes -ceq $file.bytes) 'exact original archive bytes'
  Assert-BundleCondition (@($result.effects|Where-Object { $_.operation -ceq 'read-source' -and $_.path -ceq $file.path }).Count -eq 1) 'source read interaction'
  Assert-BundleCondition (@($result.effects|Where-Object { $_.operation -ceq 'archive-write' -and $_.path -ceq $file.path }).Count -eq 1) 'archive publication interaction'
 }
 return $result
}
Write-Output 'Counted effects:6 platform inventories,12 missing files,24 pinned package metadata refusals,3 invalid inventories; original10 publication and3 metadata controls; synthetic only. No runtime download/install.'
if(-not $PSCmdlet.ShouldProcess($ReceiptPath,'Run SAME bundle source/provenance/publication assertions')){return}
$results=[Collections.Generic.List[object]]::new();$failures=[Collections.Generic.List[object]]::new();$rejections=[Collections.Generic.List[object]]::new()
foreach($rid in @('win-x64','win-arm64','linux-x64','linux-arm64','osx-x64','osx-arm64')){
 try{$provider=if($Mode -ceq 'Mock'){Get-ReplBundleModel}else{& $ProviderFactory};$results.Add((Invoke-BundleScenario $provider (Get-BundleInput $rid)))}catch{$failures.Add(@{case=$rid;error=$_.Exception.Message})}
}
$base=Get-BundleInput 'win-x64'
foreach($path in @($base.files.path)){
 $inputSpec=Get-BundleInput 'win-x64';$inputSpec.files=@($inputSpec.files|Where-Object path -CNE $path)
 try{$provider=if($Mode -ceq 'Mock'){Get-ReplBundleModel}else{& $ProviderFactory};$results.Add((Invoke-BundleScenario $provider $inputSpec 'bundle_prerequisite_missing'))}catch{$failures.Add(@{case='missing:'+ $path;error=$_.Exception.Message})}
}
foreach($fault in @('unknown-rid','path-traversal','unknown-package')){
 $inputSpec=Get-BundleInput 'win-x64'
 switch($fault){'unknown-rid'{$inputSpec.rid='unknown'}'path-traversal'{$inputSpec.files[0].path='../outside'}'unknown-package'{$inputSpec.files[0].packageId='unknown'}}
 try{$provider=if($Mode -ceq 'Mock'){Get-ReplBundleModel}else{& $ProviderFactory};$results.Add((Invoke-BundleScenario $provider $inputSpec 'bundle_invalid'))}catch{$failures.Add(@{case=$fault;error=$_.Exception.Message})}
}
foreach($packageId in @($base.packages.id)){
 foreach($fault in @('missing-package','wrong-package-version','missing-license-expression','missing-license-documents')){
  $inputSpec=Get-BundleInput 'win-x64';$package=@($inputSpec.packages|Where-Object id -CEQ $packageId)[0]
  switch($fault){'missing-package'{$inputSpec.packages=@($inputSpec.packages|Where-Object id -CNE $packageId)}'wrong-package-version'{$package.version='unapproved'}'missing-license-expression'{$package.Remove('licenseExpression')}'missing-license-documents'{$package.licenseDocuments=@()}}
  try{$provider=if($Mode -ceq 'Mock'){Get-ReplBundleModel}else{& $ProviderFactory};$results.Add((Invoke-BundleScenario $provider $inputSpec 'bundle_prerequisite_missing'))}catch{$failures.Add(@{case=($fault+':'+$packageId);error=$_.Exception.Message})}
 }
}
if($Mode -ceq 'Mock'){
 foreach($metadataFault in @('extra-package','wrong-version','wrong-license')){
  $inputSpec=Get-BundleInput 'win-x64'
  switch($metadataFault){'extra-package'{$inputSpec.packages+=@{id='unapproved';version='1';licenseExpression='MIT';licenseDocuments=@('licenses/core/LICENSE.txt')}}'wrong-version'{$inputSpec.packages[0].version='unapproved'}'wrong-license'{$inputSpec.packages[0].licenseExpression='unapproved'}}
  try{$null=Invoke-BundleScenario (Get-ReplBundleModel -Mutation 'ignore-required-metadata') $inputSpec 'bundle_prerequisite_missing';$failures.Add(@{case=$metadataFault;error='unapproved metadata accepted'})}
  catch{if($_.Exception.Message -ceq 'bundle_assert:missing or invalid prerequisite refused'){$rejections.Add(@{mutation=$metadataFault;assertion=$_.Exception.Message})}else{$failures.Add(@{case=$metadataFault;error=$_.Exception.Message})}}
 }
 foreach($mutation in @('wrong-hash','wrong-bytes','missing-file','wrong-version','wrong-source','wrong-rid','wrong-license','drop-provenance','wrong-package','omit-publication')){
  $intended=switch -CaseSensitive ($mutation){
   'wrong-hash'{'bundle_assert:exact original file digest and length'}
   'wrong-bytes'{'bundle_assert:exact original archive bytes'}
   'missing-file'{'bundle_assert:complete manifest and archive inventory'}
   'wrong-version'{'bundle_assert:exact version'}
   'wrong-source'{'bundle_assert:exact sourceCommit'}
   'wrong-rid'{'bundle_assert:exact rid'}
   'wrong-license'{'bundle_assert:manifest own license separate from included packages'}
   'drop-provenance'{'bundle_assert:exact original package versions and license provenance'}
   'wrong-package'{'bundle_assert:per-file original package provenance'}
   'omit-publication'{'bundle_assert:complete manifest and archive inventory'}
  }
  try{$null=Invoke-BundleScenario (Get-ReplBundleModel -Mutation $mutation) (Get-BundleInput 'win-x64');$failures.Add(@{case=$mutation;error='corruption accepted'})}
  catch{if($_.Exception.Message -ceq $intended){$rejections.Add(@{mutation=$mutation;assertion=$_.Exception.Message})}else{$failures.Add(@{case=$mutation;error=$_.Exception.Message;expected=$intended})}}
 }
}
$receipt=[ordered]@{timestamp=Get-Date -Format o;mode=$Mode;requirements=@('FR-QB-005','TR-QB-RUNTIME-001','TEST-QB-014','TEST-QB-069');cases=$results.Count;assertions=$script:bundleAssertions;rejectingControls=$rejections.Count;failed=$failures.Count;skipped=0;results=$results.ToArray();rejections=$rejections.ToArray();failures=$failures.ToArray();sourceHashes=@(Get-FileHash $PSCommandPath,(Join-Path $PSScriptRoot 'ReplRuntimeBundle.Model.ps1')|Select-Object Path,Hash);productComplete=$false}
Add-LinesToFile $ReceiptPath -Content (ConvertTo-Json $receipt -Depth 80)|Out-Null
[pscustomobject]$receipt|Select-Object mode,cases,assertions,rejectingControls,failed,skipped|ConvertTo-Json
if($failures.Count){throw 'Bundle prerequisite gate failed'}


