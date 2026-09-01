<#
.SYNOPSIS
    FR-MCP-PLUGINCORE-001: Sync the canonical plugin core into a plugin repo.
.DESCRIPTION
    Copies lib-ps/ into <plugin>/lib/ and writes CORE-MANIFEST.yaml by
    serializing a PowerShell object with per-file sha256 hashes so CI can detect
    local edits.
.PARAMETER PluginRoot
    Root of the target plugin repository.
.PARAMETER IncludePs
    Obsolete compatibility switch. The PowerShell runtime is always synced.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$PluginRoot,
    [switch]$IncludePs
)

$ErrorActionPreference = 'Stop'
$coreRoot = Split-Path -Parent $PSScriptRoot

if (-not (Test-Path $PluginRoot)) {
    throw "plugin root not found: $PluginRoot"
}

$coreVersion = try { (git -C $coreRoot rev-parse --short HEAD 2>$null).Trim() } catch { 'unknown' }
if (-not $coreVersion) { $coreVersion = 'unknown' }
$syncedAt = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
$manifest = Join-Path $PluginRoot 'CORE-MANIFEST.yaml'

New-Item -ItemType Directory -Force (Join-Path $PluginRoot 'lib') | Out-Null

function Import-YamlSerializer {
    if (-not (Get-Command ConvertFrom-Yaml -ErrorAction SilentlyContinue) -or
        -not (Get-Command ConvertTo-Yaml -ErrorAction SilentlyContinue)) {
        Import-Module powershell-yaml -ErrorAction Stop
    }
}

function Read-CoreManifest {
    param([Parameter(Mandatory)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        return $null
    }

    Import-YamlSerializer
    return (ConvertFrom-Yaml -Yaml ([System.IO.File]::ReadAllText($Path)) -Ordered -ErrorAction Stop)
}

function Get-CoreManifestFileKeys {
    param($ManifestDocument)

    if ($null -eq $ManifestDocument) {
        return @()
    }

    if ($ManifestDocument -isnot [System.Collections.IDictionary] -or
        -not $ManifestDocument.Contains('files') -or
        $ManifestDocument['files'] -isnot [System.Collections.IDictionary]) {
        return @()
    }

    return @($ManifestDocument['files'].Keys)
}

function Remove-PreviousCoreFiles {
    foreach ($relativePath in Get-CoreManifestFileKeys (Read-CoreManifest -Path $manifest)) {
        if ($relativePath -notlike 'lib/*') {
            continue
        }

        $target = Join-Path $PluginRoot ($relativePath -replace '/', [System.IO.Path]::DirectorySeparatorChar)
        if (Test-Path -LiteralPath $target -PathType Leaf) {
            Remove-Item -LiteralPath $target -Force
        }
    }

    $libRoot = Join-Path $PluginRoot 'lib'
    if (Test-Path -LiteralPath $libRoot) {
        Get-ChildItem -LiteralPath $libRoot -Recurse -File -Include '*.sh','*.bash','*.js' -ErrorAction SilentlyContinue |
            Remove-Item -Force
        Get-ChildItem -LiteralPath $libRoot -File -Filter 'Invoke-*McpPlugin.ps1' -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -ne 'Invoke-McpPlugin.ps1' } |
            Remove-Item -Force
        $gapFile = Join-Path $libRoot 'GAPS.md'
        if (Test-Path -LiteralPath $gapFile) {
            Remove-Item -LiteralPath $gapFile -Force
        }
    }

    $hookRoot = Join-Path $PluginRoot 'hooks\scripts'
    if (Test-Path -LiteralPath $hookRoot) {
        Get-ChildItem -LiteralPath $hookRoot -File -Include '*.sh','*.bash' -ErrorAction SilentlyContinue |
            Remove-Item -Force
    }
}

Remove-PreviousCoreFiles

$manifestFiles = [ordered]@{}

function Copy-CoreFile {
    param(
        [Parameter(Mandatory)][string]$SourcePath,
        [Parameter(Mandatory)][string]$DestinationPath
    )

    $textExtensions = @('.ps1', '.psm1', '.psd1', '.sh', '.bash', '.yaml', '.yml', '.json', '.md', '.txt')
    if ($textExtensions -contains [System.IO.Path]::GetExtension($SourcePath).ToLowerInvariant()) {
        $text = [System.IO.File]::ReadAllText($SourcePath)
        $text = $text -replace "`r`n", "`n" -replace "`r", "`n"
        [System.IO.File]::WriteAllText($DestinationPath, $text, [System.Text.UTF8Encoding]::new($false))
        return
    }

    Copy-Item $SourcePath $DestinationPath -Force
}

function Sync-Tree {
    param([string]$SourceDir)
    if (-not (Test-Path $SourceDir)) { return }
    Get-ChildItem -Path $SourceDir -Recurse -File | Sort-Object FullName | ForEach-Object {
        if ($_.Name -eq 'GAPS.md') { return }
        $rel = $_.FullName.Substring($SourceDir.Length + 1) -replace '\\', '/'
        $dest = Join-Path $PluginRoot "lib/$rel"
        New-Item -ItemType Directory -Force (Split-Path -Parent $dest) | Out-Null
        Copy-CoreFile -SourcePath $_.FullName -DestinationPath $dest
        $hash = (Get-FileHash -Path $dest -Algorithm SHA256).Hash.ToLowerInvariant()
        $manifestFiles["lib/$rel"] = $hash
    }
}

Sync-Tree (Join-Path $coreRoot 'lib-ps')

$handoffSkillSource = Join-Path $coreRoot 'skills\handoff\SKILL.md'
if (Test-Path -LiteralPath $handoffSkillSource) {
    $handoffSkillDestDir = Join-Path $PluginRoot 'skills\handoff'
    New-Item -ItemType Directory -Force $handoffSkillDestDir | Out-Null
    $handoffSkillDest = Join-Path $handoffSkillDestDir 'SKILL.md'
    Copy-CoreFile -SourcePath $handoffSkillSource -DestinationPath $handoffSkillDest
    $manifestFiles['skills/handoff/SKILL.md'] = (Get-FileHash -Path $handoffSkillDest -Algorithm SHA256).Hash.ToLowerInvariant()
}

Import-YamlSerializer
$manifestObject = [ordered]@{
    coreVersion = $coreVersion
    syncedAtUtc = $syncedAt
    files = $manifestFiles
}
$manifestYaml = ConvertTo-Yaml -Data $manifestObject -Options WithIndentedSequences
$manifestYaml = (($manifestYaml -replace "`r`n", "`n" -replace "`r", "`n") -split "`n" |
    ForEach-Object { $_.TrimEnd() }) -join "`n"
[System.IO.File]::WriteAllText($manifest, ($manifestYaml.TrimEnd() + "`n"), [System.Text.UTF8Encoding]::new($false))
$count = $manifestFiles.Count
Write-Output "synced $count core files into $PluginRoot/lib (core $coreVersion)"
