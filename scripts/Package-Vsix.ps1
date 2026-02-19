#Requires -Version 5.1
<#
.SYNOPSIS
  Packages the already-built McpServer MCP Todo extension into a VSIX (OPC/ZIP). Call after MSBuild Build.
.DESCRIPTION
  Assumes the extension DLL has been built. Runs CreatePkgDef, builds extension.vsixmanifest,
  [Content_Types].xml, _rels/.rels, and creates the .vsix ZIP. Used by the CreateVsixContainer MSBuild target.
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Debug'
)

$ErrorActionPreference = 'Stop'
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptDir
$extDir = Join-Path $repoRoot "src\McpServer.VsExtension.McpTodo"
$outDir = Join-Path $extDir "bin\$Configuration\net472"
$objDir = Join-Path $extDir "obj"
$stagingDir = Join-Path $objDir "vsixstaging"
$vsixName = "McpServer.VsExtension.McpTodo.vsix"
$vsixPath = Join-Path $outDir $vsixName
$dll = Join-Path $outDir "McpServer.VsExtension.McpTodo.dll"

if (-not (Test-Path $dll)) { throw "DLL not found. Build the project first: $dll" }

# Find VSSDK CreatePkgDef (from NuGet package)
$nugetRoot = $env:NuGetPackageRoot
if (-not $nugetRoot) {
    $line = dotnet nuget locals global-packages --list 2>$null | Where-Object { $_ -match 'global-packages:' }
    if ($line) { $nugetRoot = ($line -replace '^[^:]+:\s*', '').Trim() }
}
if (-not $nugetRoot -or -not (Test-Path $nugetRoot)) {
    $nugetRoot = Join-Path $env:USERPROFILE ".nuget\packages"
}
$vssdkBin = Join-Path $nugetRoot "microsoft.vssdk.buildtools\17.11.414\tools\vssdk\bin"
$createPkgDef = Join-Path $vssdkBin "CreatePkgDef.exe"
if (-not (Test-Path $createPkgDef)) {
    throw "CreatePkgDef not found at $createPkgDef. Restore NuGet packages."
}

New-Item -ItemType Directory -Force -Path $objDir | Out-Null
& $createPkgDef /out="$objDir\McpServer.VsExtension.McpTodo.pkgdef" $dll 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) { throw "CreatePkgDef failed." }

New-Item -ItemType Directory -Force -Path $stagingDir | Out-Null
Remove-Item (Join-Path $stagingDir "*") -Recurse -Force -ErrorAction SilentlyContinue
Copy-Item $dll -Destination $stagingDir
Copy-Item "$objDir\McpServer.VsExtension.McpTodo.pkgdef" -Destination $stagingDir

# extension.vsixmanifest: resolve Asset paths, remove ALL design-time (d:) attributes and xmlns:d
$manifestSource = Join-Path $extDir "source.extension.vsixmanifest"
$manifestDest = Join-Path $stagingDir "extension.vsixmanifest"
[xml] $manifest = Get-Content $manifestSource -Encoding UTF8
$designNs = "http://schemas.microsoft.com/developer/vsx-schema-design/2011"
$xmlnsNs = "http://www.w3.org/2000/xmlns/"
$nsmgr = New-Object System.Xml.XmlNamespaceManager($manifest.NameTable)
$nsmgr.AddNamespace("v", "http://schemas.microsoft.com/developer/vsx-schema/2011")
# Resolve Asset paths
$assets = $manifest.SelectNodes("//v:Asset", $nsmgr)
foreach ($asset in $assets) {
    $type = $asset.Type
    if ($type -eq "Microsoft.VisualStudio.VsPackage") { $asset.Path = "McpServer.VsExtension.McpTodo.pkgdef" }
    elseif ($type -eq "Microsoft.VisualStudio.MefComponent") { $asset.Path = "McpServer.VsExtension.McpTodo.dll" }
}
# Remove every attribute in the design namespace (d:Source, d:ProjectName, etc.)
foreach ($node in $manifest.SelectNodes("//*")) {
    $toRemove = @()
    foreach ($attr in $node.Attributes) { if ($attr.NamespaceURI -eq $designNs) { $toRemove += $attr } }
    foreach ($a in $toRemove) { [void]$node.Attributes.Remove($a) }
}
# Remove xmlns:d from root so installer does not see design schema
$root = $manifest.DocumentElement
$dAttr = $root.Attributes["d", $xmlnsNs]
if ($dAttr) { [void]$root.Attributes.Remove($dAttr) }
$utf8NoBom = [System.Text.UTF8Encoding]::new($false)
$writer = [System.IO.StreamWriter]::new($manifestDest, $false, $utf8NoBom)
try { $manifest.Save($writer) } finally { $writer.Dispose() }

$contentTypes = @"
<?xml version="1.0" encoding="utf-8"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
  <Default Extension="vsixmanifest" ContentType="text/xml"/>
  <Default Extension="dll" ContentType="application/octet-stream"/>
  <Default Extension="pkgdef" ContentType="text/plain"/>
  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
  <Override PartName="/extension.vsixmanifest" ContentType="text/xml"/>
</Types>
"@
[System.IO.File]::WriteAllText((Join-Path $stagingDir "[Content_Types].xml"), $contentTypes, $utf8NoBom)

$relsDir = Join-Path $stagingDir "_rels"
New-Item -ItemType Directory -Force -Path $relsDir | Out-Null
$relsContent = @"
<?xml version="1.0" encoding="utf-8"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Type="http://schemas.microsoft.com/developer/vsx/2011" Target="extension.vsixmanifest" Id="manifest"/>
</Relationships>
"@
[System.IO.File]::WriteAllText((Join-Path $relsDir ".rels"), $relsContent, $utf8NoBom)

if (Test-Path $vsixPath) { Remove-Item $vsixPath -Force }
Add-Type -AssemblyName System.IO.Compression.FileSystem
$stagingFullPath = (Resolve-Path $stagingDir).Path
$zip = [System.IO.Compression.ZipFile]::Open($vsixPath, [System.IO.Compression.ZipArchiveMode]::Create)
try {
    [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, (Join-Path $stagingFullPath "[Content_Types].xml"), "[Content_Types].xml") | Out-Null
    [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, (Join-Path $stagingFullPath "_rels\.rels"), "_rels/.rels") | Out-Null
    [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, (Join-Path $stagingFullPath "extension.vsixmanifest"), "extension.vsixmanifest") | Out-Null
    [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, (Join-Path $stagingFullPath "McpServer.VsExtension.McpTodo.dll"), "McpServer.VsExtension.McpTodo.dll") | Out-Null
    [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, (Join-Path $stagingFullPath "McpServer.VsExtension.McpTodo.pkgdef"), "McpServer.VsExtension.McpTodo.pkgdef") | Out-Null
} finally { $zip.Dispose() }

Write-Output $vsixPath
