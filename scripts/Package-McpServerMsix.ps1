<#
.SYNOPSIS
    Builds and packages FWH.Support.Mcp as an MSIX package.
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
    [string]$PackageName = "FWH.Support.Mcp",
    [string]$OutputDirectory = "artifacts\\msix",
    [string]$CertificatePath,
    [string]$CertificatePassword
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path $PSScriptRoot -Parent
$projectPath = Join-Path $repoRoot "src\\FWH.Support.Mcp\\FWH.Support.Mcp.csproj"
$publishDir = Join-Path $repoRoot "artifacts\\mcp-msix-publish"
$stagingDir = Join-Path $repoRoot "artifacts\\mcp-msix-staging"
$outputDir = Join-Path $repoRoot $OutputDirectory

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
    <Application Id="McpServer" Executable="FWH.Support.Mcp.exe" EntryPoint="Windows.FullTrustApplication">
      <uap:VisualElements DisplayName="$PackageName" Square44x44Logo="Square44x44Logo.png" Description="FunWasHad MCP Server" BackgroundColor="transparent" />
    </Application>
  </Applications>
</Package>
"@
Set-Content -Path $manifestPath -Value $manifest -Encoding UTF8

# Required by manifest visual elements.
$logoPath = Join-Path $stagingDir "Square44x44Logo.png"
if (-not (Test-Path $logoPath)) {
    # 1x1 transparent PNG
    $png = [Convert]::FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO5oY0QAAAAASUVORK5CYII=")
    [IO.File]::WriteAllBytes($logoPath, $png)
}

$makeAppx = Get-Command makeappx.exe -ErrorAction SilentlyContinue
if (-not $makeAppx) {
    $fallbackMakeAppx = "C:\Program Files (x86)\Windows Kits\10\bin\x64\makeappx.exe"
    if (Test-Path $fallbackMakeAppx) {
        $makeAppx = @{ Source = $fallbackMakeAppx }
    }
}
if (-not $makeAppx) { throw "makeappx.exe not found. Install Windows SDK and retry." }

$msixPath = Join-Path $outputDir "$PackageName-$Version.msix"
Write-Host "Creating MSIX: $msixPath"
& $makeAppx.Source pack /d $stagingDir /p $msixPath /o
if ($LASTEXITCODE -ne 0) { throw "makeappx pack failed." }

if ($CertificatePath) {
    $signtool = Get-Command signtool.exe -ErrorAction SilentlyContinue
    if (-not $signtool) {
        $fallbackSignTool = "C:\Program Files (x86)\Windows Kits\10\bin\x64\signtool.exe"
        if (Test-Path $fallbackSignTool) {
            $signtool = @{ Source = $fallbackSignTool }
        }
    }
    if (-not $signtool) { throw "signtool.exe not found. Install Windows SDK and retry." }
    if (-not $CertificatePassword) { throw "CertificatePassword is required when CertificatePath is provided." }

    Write-Host "Signing MSIX..."
    & $signtool.Source sign /fd SHA256 /f $CertificatePath /p $CertificatePassword $msixPath
    if ($LASTEXITCODE -ne 0) { throw "signtool sign failed." }
}

Write-Host "MSIX package ready: $msixPath"
