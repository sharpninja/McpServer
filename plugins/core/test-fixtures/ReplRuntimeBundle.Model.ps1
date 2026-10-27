# SPDX-License-Identifier: Apache-2.0
function Get-ReplBundleModel {
 <# .SYNOPSIS
 Independent test-only package inventory and publication model for TR-QB-RUNTIME-001.
 .PARAMETER Mutation
 Deliberately corrupts a derived publication so common assertions must reject it.
 #>
 [CmdletBinding()]
 param([string]$Mutation='')
 $fault=$Mutation
 return {
  param($InputSpec,$RequiredPackages)
  $tape=[Collections.Generic.List[object]]::new();$stored=@{}
  if($InputSpec.rid -cnotin @('win-x64','win-arm64','linux-x64','linux-arm64','osx-x64','osx-arm64')){return @{ok=$false;code='bundle_invalid';effects=@()}}
  if($fault -cne 'ignore-required-metadata'){
  if(-not $RequiredPackages -or $InputSpec.packages.Count -ne $RequiredPackages.Count){return @{ok=$false;code='bundle_prerequisite_missing';effects=$tape.ToArray()}}
  foreach($required in $RequiredPackages){
   $matched=@($InputSpec.packages|Where-Object id -CEQ $required.id)
   if($matched.Count -ne 1 -or $matched[0].version -cne $required.version -or $matched[0].licenseExpression -cne $required.licenseExpression -or (ConvertTo-Json @($matched[0].licenseDocuments) -Compress) -cne (ConvertTo-Json @($required.licenseDocuments) -Compress)){return @{ok=$false;code='bundle_prerequisite_missing';effects=$tape.ToArray()}}
  }
  }
  $packages=@{};foreach($package in $InputSpec.packages){$packages[$package.id]=$package}
  foreach($file in $InputSpec.files){
   $tape.Add(@{operation='read-source';path=$file.path})
   if($file.path -match '(^/|\\|(^|/)\.\.(/|$))' -or $stored.Contains($file.path) -or -not $packages.Contains($file.packageId)){return @{ok=$false;code='bundle_invalid';effects=$tape.ToArray()}}
   $stored[$file.path]=[Convert]::FromBase64String($file.bytes)
  }
  foreach($package in $InputSpec.packages){
   if(-not $package.version -or -not $package.licenseDocuments.Count){return @{ok=$false;code='bundle_prerequisite_missing';effects=$tape.ToArray()}}
   foreach($path in $package.licenseDocuments){if(-not $stored.Contains($path)){return @{ok=$false;code='bundle_prerequisite_missing';effects=$tape.ToArray()}}}
  }
  $exe=if($InputSpec.rid.StartsWith('win-')){'.exe'}else{''}
  foreach($path in @(('repl/qbrain-ai-repl'+$exe),'repl/System.Private.CoreLib.dll',('powershell/pwsh'+$exe),'modules/PowerShell.MCP/PowerShell.MCP.psd1','modules/powershell-yaml/powershell-yaml.psd1','core/governed-session-dispatch.ps1')){
   if(-not $stored.Contains($path)){return @{ok=$false;code='bundle_prerequisite_missing';effects=$tape.ToArray()}}
  }
  $inventory=[Collections.Generic.List[object]]::new();$archive=[Collections.Generic.List[object]]::new()
  foreach($path in @($stored.Keys|Sort-Object -CaseSensitive)){
   $original=@($InputSpec.files|Where-Object path -CEQ $path)[0]
   $hash=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($stored[$path]))
   $inventory.Add(@{path=$path;sha256=$hash;packageId=$original.packageId;byteLength=$stored[$path].Length})
   $archive.Add(@{path=$path;bytes=[Convert]::ToBase64String($stored[$path])});$tape.Add(@{operation='archive-write';path=$path})
  }
  $manifest=@{schemaVersion=1;manifestLicense='Apache-2.0';sourceCommit=$InputSpec.sourceCommit;version=$InputSpec.version;rid=$InputSpec.rid;packages=$InputSpec.packages;files=$inventory.ToArray()}
  switch -CaseSensitive ($fault){
   'wrong-hash'{$manifest.files[0].sha256='wrong'}
   'wrong-bytes'{$archive[0].bytes=[Convert]::ToBase64String([byte[]]@(1,2,3))}
   'missing-file'{$manifest.files=@($manifest.files|Select-Object -Skip 1)}
   'wrong-version'{$manifest.version='wrong'}
   'wrong-source'{$manifest.sourceCommit='wrong'}
   'wrong-rid'{$manifest.rid='wrong'}
   'wrong-license'{$manifest.manifestLicense='MIT'}
   'drop-provenance'{$manifest.packages=@()}
   'wrong-package'{$manifest.files[0].packageId='unknown'}
   'omit-publication'{$archive.Clear()}
  }
  return @{ok=$true;code='bundle_created';manifest=$manifest;archiveEntries=$archive.ToArray();effects=$tape.ToArray()}
 }.GetNewClosure()
}