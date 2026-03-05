<#
.SYNOPSIS
    Builds, packs, and installs a .NET executable project as a global dotnet tool.

.DESCRIPTION
    Generalized tool redeploy script with Director defaults. It can:
    - optionally bump GitVersion next-version patch
    - compute package version via dotnet-gitversion (or accept an explicit version)
    - stop a running process by command name
    - uninstall previous global tool package
    - pack a target project into a nupkg
    - install the global tool from a local package source

.PARAMETER SkipVersionBump
    When set, skips the GitVersion.yml next-version patch bump.

.PARAMETER ProjectPath
    Path to the target .csproj. Defaults to McpServer.Director.

.PARAMETER ToolId
    Dotnet tool package id to uninstall/install.

.PARAMETER ToolCommand
    Command/process name to stop before reinstall.

.PARAMETER NupkgDir
    Output directory for generated .nupkg packages.

.PARAMETER PackageVersion
    Explicit package version. If omitted, version is computed from dotnet-gitversion SemVer.

.PARAMETER SkipProcessStop
    When set, skips stopping running processes for ToolCommand.

.EXAMPLE
    .\Update-DotnetTool.ps1
    .\Update-DotnetTool.ps1 -SkipVersionBump
    .\Update-DotnetTool.ps1 -ProjectPath src\McpServer.Web\McpServer.Web.csproj -ToolId SharpNinja.McpServer.Web -ToolCommand mcp-web -SkipVersionBump
#>
[CmdletBinding()]
param(
    [switch]$SkipVersionBump,
    [string]$ProjectPath = 'src\McpServer.Director\McpServer.Director.csproj',
    [string]$ToolId = 'SharpNinja.McpServer.Director',
    [string]$ToolCommand = 'director',
    [string]$NupkgDir = 'nupkg',
    [string]$PackageVersion,
    [switch]$SkipProcessStop
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$ProgressPreference    = 'SilentlyContinue'

$RepoRoot          = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$ResolvedProject   = Join-Path $RepoRoot $ProjectPath
$ResolvedNupkgDir  = Join-Path $RepoRoot $NupkgDir

if (-not (Test-Path $ResolvedProject)) {
    throw "Project not found: $ResolvedProject"
}

if (-not (Test-Path $ResolvedNupkgDir)) {
    New-Item -ItemType Directory -Path $ResolvedNupkgDir -Force | Out-Null
}

# ---------------------------------------------------------------------------
# Shared: Bump-GitVersionPatch (also used by Update-McpService.ps1)
# ---------------------------------------------------------------------------

. (Join-Path $PSScriptRoot 'Bump-GitVersionPatch.ps1')

# ---------------------------------------------------------------------------
# Pipeline
# ---------------------------------------------------------------------------

function Write-Step {
    param([string]$Message)
    Write-Host "`n>> $Message" -ForegroundColor Cyan
}

# 1. Bump version
if (-not $SkipVersionBump) {
    Write-Step "1/7  Bumping GitVersion next-version patch ..."
    $bumpResult = Bump-GitVersionPatch -RepoRoot $RepoRoot
    Write-Host "  $($bumpResult.OldVersion) -> $($bumpResult.NewVersion)" -ForegroundColor Green
}
else {
    Write-Step "1/7  Skipping version bump."
}

# 2. Compute package version
if (-not $PackageVersion) {
    Write-Step "2/7  Computing package version ..."
    Push-Location $RepoRoot
    try {
        $gitVersionJson = dotnet gitversion /output json 2>&1
        if ($LASTEXITCODE -ne 0) { Write-Error "dotnet gitversion failed: $gitVersionJson" }
        $versionInfo = $gitVersionJson | ConvertFrom-Json
        $PackageVersion = $versionInfo.SemVer
    }
    finally { Pop-Location }
}
else {
    Write-Step "2/7  Using provided package version."
}
Write-Host "  Package version: $packageVersion" -ForegroundColor Green

# 3. Stop running command process
if (-not $SkipProcessStop) {
    Write-Step "3/7  Stopping running process '$ToolCommand' ..."
    $procs = @(Get-Process -Name $ToolCommand -ErrorAction SilentlyContinue)
    if ($procs.Count -gt 0) {
        foreach ($p in $procs) { Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue }
        Start-Sleep -Seconds 1
        Write-Host "  Killed $($procs.Count) process(es)." -ForegroundColor Green
    }
    else {
        Write-Host "  No running '$ToolCommand' process found." -ForegroundColor DarkGray
    }
}
else {
    Write-Step "3/7  Skipping process stop."
}

# 4. Uninstall previous version
Write-Step "4/7  Uninstalling previous version ..."
dotnet tool uninstall --global $ToolId 2>&1 | Out-Null
Write-Host "  Uninstalled (or was not installed)." -ForegroundColor DarkGray

# 5. Publish (framework-dependent — produces DLLs + wwwroot with RCL content)
Write-Step "5/7  Publishing $ToolId (Release) ..."
$publishDir = Join-Path (Split-Path $ResolvedProject) 'bin\Release\net9.0\publish'
& dotnet publish $ResolvedProject -c Release -o $publishDir
if ($LASTEXITCODE -ne 0) { Write-Error "dotnet publish failed (exit code $LASTEXITCODE)" }
$publishFiles = (Get-ChildItem $publishDir -Recurse -File).Count
Write-Host "  Publish complete — $publishFiles file(s)." -ForegroundColor Green

# 6. Generate .nuspec and pack with nuget
Write-Step "6/7  Packing $ToolId v$PackageVersion with nuget ..."

# Read metadata from csproj
[xml]$csproj = Get-Content $ResolvedProject
$description = $ToolId
$authors     = 'SharpNinja'
$assemblyName = [System.IO.Path]::GetFileNameWithoutExtension($ResolvedProject)
$toolCommandName = $ToolCommand
foreach ($pg in $csproj.Project.PropertyGroup) {
    $node = $null
    $node = $pg.SelectSingleNode('Description');  if ($node) { $description = $node.InnerText }
    $node = $pg.SelectSingleNode('Authors');      if ($node) { $authors = $node.InnerText }
    $node = $pg.SelectSingleNode('AssemblyName'); if ($node) { $assemblyName = $node.InnerText }
    $node = $pg.SelectSingleNode('ToolCommandName'); if ($node) { $toolCommandName = $node.InnerText }
}
$entryPointDll = "$assemblyName.dll"

# Write DotnetToolSettings.xml into publish dir
$toolSettingsPath = Join-Path $publishDir 'DotnetToolSettings.xml'
@"
<?xml version="1.0" encoding="utf-8"?>
<DotNetCliTool Version="1">
  <Commands>
    <Command Name="$toolCommandName" EntryPoint="$entryPointDll" Runner="dotnet" />
  </Commands>
</DotNetCliTool>
"@ | Set-Content -Path $toolSettingsPath -Encoding UTF8

# Generate .nuspec
$nuspecPath = Join-Path $ResolvedNupkgDir "$ToolId.nuspec"
@"
<?xml version="1.0" encoding="utf-8"?>
<package xmlns="http://schemas.microsoft.com/packaging/2012/06/nuspec.xsd">
  <metadata>
    <id>$ToolId</id>
    <version>$PackageVersion</version>
    <authors>$authors</authors>
    <description>$description</description>
    <packageTypes>
      <packageType name="DotnetTool" />
    </packageTypes>
  </metadata>
  <files>
    <file src="$publishDir\**" target="tools/net9.0/any" />
  </files>
</package>
"@ | Set-Content -Path $nuspecPath -Encoding UTF8

# Remove old nupkg if present
$nupkgFile = Join-Path $ResolvedNupkgDir "$ToolId.$PackageVersion.nupkg"
if (Test-Path $nupkgFile) { Remove-Item $nupkgFile -Force }

& nuget pack $nuspecPath -OutputDirectory $ResolvedNupkgDir -NoPackageAnalysis
if ($LASTEXITCODE -ne 0) { Write-Error "nuget pack failed (exit code $LASTEXITCODE)" }

$nupkgSize = (Get-Item $nupkgFile).Length / 1MB
Write-Host "  Pack complete — $("{0:N1}" -f $nupkgSize) MB." -ForegroundColor Green

# 7. Install globally
Write-Step "7/7  Installing globally ..."
dotnet tool install --global $ToolId --add-source $ResolvedNupkgDir --version $PackageVersion
if ($LASTEXITCODE -ne 0) { Write-Error "dotnet tool install failed (exit code $LASTEXITCODE)" }

Write-Host "`n=== Tool updated ===" -ForegroundColor Green
Write-Host "  Version : $packageVersion"
Write-Host "  Command : $ToolCommand interactive"
