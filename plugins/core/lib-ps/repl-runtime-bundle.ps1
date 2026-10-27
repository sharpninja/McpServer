#Requires -Version 7.5
# SPDX-License-Identifier: Apache-2.0
function New-McpReplRuntimeBundle {
 <#
 .SYNOPSIS
 FR-QB-005/TR-QB-RUNTIME-001: Package pinned offline files with exact package provenance.
 .DESCRIPTION
 Consumes an explicit source inventory after Nuke self-contained publication. Missing
 runtime/module/license provenance refuses before archive publication; no installation,
 module-cache lookup, network request, implicit license or mutable latest pin exists.
 .PARAMETER Spec
 Native inventory: rid/version/sourceCommit, packages with exact version/licenseDocuments,
 and files with relative path/packageId. Every source file must have one package owner.
 .PARAMETER PackageRequirements
 Pinned complete package versions and original license provenance from authoritative catalog metadata.
 .PARAMETER SourceDirectory
 Complete staged self-contained REPL, PowerShell, required modules/core and original notices.
 .PARAMETER OutputDirectory
 New retained versioned archive/manifest directory. Existing outputs are never overwritten.
 .OUTPUTS
 Explicit creation or refusal result with physical archive/manifest paths and source effects.
 #>
 [CmdletBinding(SupportsShouldProcess)]
 param([Parameter(Mandatory)][Collections.IDictionary]$Spec,[Parameter(Mandatory)][object[]]$PackageRequirements,[Parameter(Mandatory)][string]$SourceDirectory,[Parameter(Mandatory)][string]$OutputDirectory)
 $ErrorActionPreference='Stop'
 $effects=[Collections.Generic.List[object]]::new()
 $invalid=@{ok=$false;code='bundle_invalid';effects=@()}
 if($Spec.rid -cnotin @('win-x64','win-arm64','linux-x64','linux-arm64','osx-x64','osx-arm64') -or $Spec.sourceCommit -cnotmatch '^[a-f0-9]{40}$' -or [string]::IsNullOrWhiteSpace($Spec.version)){return $invalid}
 # Requirements originate from the pinned Nuke catalog, separately from submitted inventory.
 # The catalog builder derives them from original module manifests and published deps metadata.
 if(-not $PackageRequirements.Count -or $Spec.packages.Count -ne $PackageRequirements.Count){return @{ok=$false;code='bundle_prerequisite_missing';effects=@()}}
 foreach($required in $PackageRequirements){
  $match=@($Spec.packages|Where-Object id -CEQ $required.id)
  if($match.Count -ne 1 -or $match[0].version -cne $required.version -or $match[0].licenseExpression -cne $required.licenseExpression -or (ConvertTo-Json @($match[0].licenseDocuments) -Compress) -cne (ConvertTo-Json @($required.licenseDocuments) -Compress)){return @{ok=$false;code='bundle_prerequisite_missing';effects=@()}}
 }
 $root=[IO.Path]::GetFullPath($SourceDirectory);$prefix=$root+[IO.Path]::DirectorySeparatorChar
 if(-not (Test-Path -LiteralPath $root -PathType Container)){return @{ok=$false;code='bundle_prerequisite_missing';effects=@()}}
 if((Get-Item -LiteralPath $root).Attributes -band [IO.FileAttributes]::ReparsePoint){return $invalid}
 $packages=@{};$inventory=[Collections.Generic.List[object]]::new();$paths=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
 foreach($package in $Spec.packages){
  if([string]::IsNullOrWhiteSpace($package.id) -or $packages.ContainsKey($package.id)){return $invalid}
  if([string]::IsNullOrWhiteSpace($package.version) -or -not $package.licenseDocuments.Count){return @{ok=$false;code='bundle_prerequisite_missing';effects=@()}}
  $packages[$package.id]=$package
 }
 foreach($file in $Spec.files){
  if($file.path -isnot [string] -or $file.path -match '(^/|\\|(^|/)\.\.(/|$)|:)' -or -not $paths.Add($file.path) -or -not $packages.ContainsKey($file.packageId)){return $invalid}
  $path=[IO.Path]::GetFullPath((Join-Path $root $file.path))
  if(-not $path.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase)){return $invalid}
  if(-not (Test-Path -LiteralPath $path -PathType Leaf)){return @{ok=$false;code='bundle_prerequisite_missing';effects=@()}}
  if((Get-Item -LiteralPath $path).Attributes -band [IO.FileAttributes]::ReparsePoint){return $invalid}
  $bytes=[IO.File]::ReadAllBytes($path);$effects.Add(@{operation='read-source';path=$file.path})
  $inventory.Add([ordered]@{path=$file.path;sha256=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes));byteLength=$bytes.Length;packageId=$file.packageId})
 }
 foreach($package in $Spec.packages){foreach($path in $package.licenseDocuments){if(-not $paths.Contains($path)){return @{ok=$false;code='bundle_prerequisite_missing';effects=$effects.ToArray()}}}}
 $exe=if($Spec.rid.StartsWith('win-')){'.exe'}else{''}
 foreach($required in @(('repl/qbrain-ai-repl'+$exe),'repl/System.Private.CoreLib.dll',('powershell/pwsh'+$exe),'modules/PowerShell.MCP/PowerShell.MCP.psd1','modules/powershell-yaml/powershell-yaml.psd1','core/governed-session-dispatch.ps1')){
  if(-not $paths.Contains($required)){return @{ok=$false;code='bundle_prerequisite_missing';effects=$effects.ToArray()}}
 }
 $actual=@(Get-ChildItem -LiteralPath $root -Recurse -File)
 if($actual.Count -ne $paths.Count){return $invalid}
 foreach($file in $actual){if($file.Attributes -band [IO.FileAttributes]::ReparsePoint -or -not $paths.Contains([IO.Path]::GetRelativePath($root,$file.FullName).Replace('\','/'))){return $invalid}}
 $out=[IO.Path]::GetFullPath($OutputDirectory);$stem='qbrain-ai-repl-'+$Spec.version+'-'+$Spec.rid
 $archivePath=Join-Path $out ($stem+'.zip');$manifestPath=Join-Path $out ($stem+'.manifest.json')
 if((Test-Path $archivePath) -or (Test-Path $manifestPath)){throw 'Retained bundle output exists; choose a new versioned output directory'}
 Write-Verbose ('Counted effects: '+$inventory.Count+' exact source files, one new archive and one new native provenance manifest.')
 if(-not $PSCmdlet.ShouldProcess($out,('Publish '+$inventory.Count+' inventoried files and exact provenance'))){return @{ok=$false;code='what_if';effects=$effects.ToArray()}}
 [void][IO.Directory]::CreateDirectory($out)
 $stream=[IO.File]::Open($archivePath,[IO.FileMode]::CreateNew,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
 try{
  $zip=[IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Create,$true)
  try{foreach($item in @($inventory|Sort-Object path -CaseSensitive)){
   $entry=$zip.CreateEntry($item.path,[IO.Compression.CompressionLevel]::Optimal)
   if(-not $Spec.rid.StartsWith('win-') -and $item.path -cin @('repl/qbrain-ai-repl','powershell/pwsh')){$entry.ExternalAttributes=[BitConverter]::ToInt32([BitConverter]::GetBytes([uint32]0x81ED0000u),0)}
   $inputFile=[IO.File]::OpenRead((Join-Path $root $item.path));$entryStream=$entry.Open()
   try{$inputFile.CopyTo($entryStream)}finally{$entryStream.Dispose();$inputFile.Dispose()}
   $effects.Add(@{operation='archive-write';path=$item.path})
  }}finally{$zip.Dispose()}
  $stream.Flush($true)
 }finally{$stream.Dispose()}
 $manifest=[ordered]@{schemaVersion=1;manifestLicense='Apache-2.0';sourceCommit=$Spec.sourceCommit;version=$Spec.version;rid=$Spec.rid;packages=$Spec.packages;files=@($inventory|Sort-Object path -CaseSensitive);archiveSha256=(Get-FileHash $archivePath).Hash}
 $var1=ConvertTo-Json $manifest -Depth 80
 Add-LinesToFile $manifestPath -Content $var1|Out-Null
 return @{ok=$true;code='bundle_created';manifestPath=$manifestPath;archivePath=$archivePath;effects=$effects.ToArray()}
}



