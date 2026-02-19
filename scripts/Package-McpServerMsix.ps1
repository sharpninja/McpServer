<#
.SYNOPSIS
    Builds and packages McpServer.Support.Mcp as an MSIX package.
.DESCRIPTION
    Publishes the MCP server, generates a minimal AppxManifest.xml, then creates an
    MSIX package using makeappx.exe. Optionally signs with signtool.exe if cert path
    and password are provided.
#>
[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release", "Staging")]
    [string]$Configuration = "Release",
    [string]$Version = "1.0.0.0",
    [string]$Publisher = "CN=FunWasHad",
    [string]$PackageName = "McpServer.Support.Mcp",
    [string]$OutputDirectory = "artifacts\\msix",
    [string]$CertificatePath,
    [string]$CertificatePassword
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path $PSScriptRoot -Parent
$projectPath = Join-Path $repoRoot "src\\McpServer.Support.Mcp\\McpServer.Support.Mcp.csproj"
$publishDir = Join-Path $repoRoot "artifacts\\mcp-msix-publish"
$stagingDir = Join-Path $repoRoot "artifacts\\mcp-msix-staging"
$outputDir = Join-Path $repoRoot $OutputDirectory

function Find-SdkTool {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ToolName
    )

    $onPath = Get-Command $ToolName -ErrorAction SilentlyContinue
    if ($onPath) {
        return $onPath.Source
    }

    $kitsRoot = "C:\Program Files (x86)\Windows Kits\10\bin"
    if (-not (Test-Path $kitsRoot)) {
        return $null
    }

    $candidates = Get-ChildItem -Path $kitsRoot -Recurse -File -Filter $ToolName `
        -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -match "\\x64\\" } |
        Sort-Object FullName -Descending

    if ($candidates.Count -gt 0) {
        return $candidates[0].FullName
    }

    return $null
}

if (-not (Test-Path $projectPath)) {
    throw "Project not found: $projectPath"
}

New-Item -ItemType Directory -Force -Path $publishDir | Out-Null
New-Item -ItemType Directory -Force -Path $stagingDir | Out-Null
New-Item -ItemType Directory -Force -Path $outputDir | Out-Null

Write-Host "Publishing MCP server..."
& dotnet publish $projectPath -c $Configuration -o $publishDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed." }

Write-Host "Staging MSIX content..."
Remove-Item -Recurse -Force (Join-Path $stagingDir "*") -ErrorAction SilentlyContinue
Copy-Item -Path (Join-Path $publishDir "*") -Destination $stagingDir -Recurse -Force

$manifestPath = Join-Path $stagingDir "AppxManifest.xml"
$manifest = @"
<?xml version="1.0" encoding="utf-8"?>
<Package xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10"
         xmlns:uap="http://schemas.microsoft.com/appx/manifest/uap/windows10">
  <Identity Name="$PackageName" Publisher="$Publisher" Version="$Version" />
  <Properties>
    <DisplayName>$PackageName</DisplayName>
    <PublisherDisplayName>FunWasHad</PublisherDisplayName>
    <Logo>Square44x44Logo.png</Logo>
  </Properties>
  <Dependencies>
    <TargetDeviceFamily Name="Windows.Universal" MinVersion="10.0.17763.0" MaxVersionTested="10.0.22631.0" />
  </Dependencies>
  <Resources>
    <Resource Language="en-us" />
  </Resources>
  <Applications>
    <Application Id="McpServer" Executable="McpServer.Support.Mcp.exe" EntryPoint="Windows.FullTrustApplication">
      <uap:VisualElements DisplayName="$PackageName" Square44x44Logo="Square44x44Logo.png" Square150x150Logo="Square150x150Logo.png" Description="FunWasHad MCP Server" BackgroundColor="transparent" />
    </Application>
  </Applications>
</Package>
"@
Set-Content -Path $manifestPath -Value $manifest -Encoding UTF8

# Required by manifest visual elements.
$logo44Path = Join-Path $stagingDir "Square44x44Logo.png"
$logo150Path = Join-Path $stagingDir "Square150x150Logo.png"
if (-not (Test-Path $logo44Path) -or -not (Test-Path $logo150Path)) {
    # 1x1 transparent PNG placeholder used for both required logo assets.
    $png = [Convert]::FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO5oY0QAAAAASUVORK5CYII=")
    if (-not (Test-Path $logo44Path)) {
        [IO.File]::WriteAllBytes($logo44Path, $png)
    }
    if (-not (Test-Path $logo150Path)) {
        [IO.File]::WriteAllBytes($logo150Path, $png)
    }
}

$makeAppxPath = Find-SdkTool -ToolName "makeappx.exe"
if (-not $makeAppxPath) {
    throw "makeappx.exe not found. Install Windows SDK and retry."
}

$msixPath = Join-Path $outputDir "$PackageName-$Version.msix"
Write-Host "Creating MSIX: $msixPath"
& $makeAppxPath pack /d $stagingDir /p $msixPath /o
if ($LASTEXITCODE -ne 0) { throw "makeappx pack failed." }

if ($CertificatePath) {
    $signtoolPath = Find-SdkTool -ToolName "signtool.exe"
    if (-not $signtoolPath) {
        throw "signtool.exe not found. Install Windows SDK and retry."
    }
    if (-not $CertificatePassword) { throw "CertificatePassword is required when CertificatePath is provided." }

    Write-Host "Signing MSIX..."
    & $signtoolPath sign /fd SHA256 /f $CertificatePath /p $CertificatePassword $msixPath
    if ($LASTEXITCODE -ne 0) { throw "signtool sign failed." }
}

Write-Host "MSIX package ready: $msixPath"
