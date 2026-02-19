#Requires -Version 5.1
<#
.SYNOPSIS
  Builds the McpServer MCP Todo Visual Studio extension (VSIX) with MSBuild and optionally installs it.
.DESCRIPTION
  Builds the legacy VSIX project (McpServer.VsExtension.McpTodo.Vsix) with Visual Studio MSBuild.
  Restore + Build produces the VSIX in bin\<Configuration>\. You can also build from Visual Studio:
  open the solution, build the McpServer.VsExtension.McpTodo.Vsix project, then install the .vsix from bin\Debug or bin\Release.
.EXAMPLE
  .\Build-AndInstall-Vsix.ps1
  .\Build-AndInstall-Vsix.ps1 -Configuration Release
  .\Build-AndInstall-Vsix.ps1 -SkipInstall
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Debug',
    [switch] $SkipInstall
)

$ErrorActionPreference = 'Stop'
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptDir
$extDir = Join-Path $repoRoot "src\McpServer.VsExtension.McpTodo.Vsix"
$outDir = Join-Path $extDir "bin\$Configuration"
$vsixName = "McpServer.VsExtension.McpTodo.vsix"
$vsixPath = Join-Path $outDir $vsixName
$csproj = Join-Path $extDir "McpServer.VsExtension.McpTodo.Vsix.csproj"

# Find MSBuild via vswhere (Visual Studio 2022)
$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
if (-not (Test-Path $vswhere)) {
    throw "vswhere.exe not found. Install Visual Studio 2022."
}
$vsPath = & $vswhere -latest -property installationPath
if (-not $vsPath) { throw "Visual Studio installation not found." }
$msbuild = Join-Path $vsPath "MSBuild\Current\Bin\MSBuild.exe"
if (-not (Test-Path $msbuild)) {
    throw "MSBuild not found at $msbuild"
}

Write-Host "Building VSIX project with MSBuild (Configuration=$Configuration)..." -ForegroundColor Cyan
Push-Location $repoRoot
try {
    & $msbuild $csproj /t:Restore /t:Build /p:Configuration=$Configuration /p:Platform=AnyCPU /v:minimal
    if ($LASTEXITCODE -ne 0) { throw "MSBuild failed (exit code $LASTEXITCODE)." }
} finally { Pop-Location }

if (-not (Test-Path $vsixPath)) {
    throw "MSBuild did not produce VSIX at $vsixPath. Build the project in Visual Studio (McpServer.VsExtension.McpTodo.Vsix) to produce the VSIX."
}
Write-Host "VSIX created: $vsixPath" -ForegroundColor Green

if (-not $SkipInstall) {
    Write-Host "Launching VSIX installer..." -ForegroundColor Cyan
    Start-Process -FilePath $vsixPath
    Write-Host "Complete installation in the dialog, then use View > Other Windows > MCP Todo in Visual Studio." -ForegroundColor Green
} else {
    Write-Host "Skipping install (omit -SkipInstall to install)." -ForegroundColor Yellow
}
