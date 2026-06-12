<#
.SYNOPSIS
    FR-MCP-PLUGINCORE-001: Sync the canonical plugin core into a plugin repo.
.DESCRIPTION
    PowerShell twin of sync-plugin-core.sh. Copies lib-sh/ (and lib-ps/ when
    -IncludePs) into <plugin>/lib/ and writes CORE-MANIFEST.yaml with per-file
    sha256 hashes so CI can detect local edits.
.PARAMETER PluginRoot
    Root of the target plugin repository.
.PARAMETER IncludePs
    Also sync lib-ps/ (for plugins that ship PowerShell parallels).
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

$lines = [System.Collections.Generic.List[string]]::new()
$lines.Add("coreVersion: $coreVersion")
$lines.Add("syncedAtUtc: $syncedAt")
$lines.Add('files:')

function Sync-Tree {
    param([string]$SourceDir)
    if (-not (Test-Path $SourceDir)) { return }
    Get-ChildItem -Path $SourceDir -Recurse -File | Sort-Object FullName | ForEach-Object {
        $rel = $_.FullName.Substring($SourceDir.Length + 1) -replace '\\', '/'
        $dest = Join-Path $PluginRoot "lib/$rel"
        New-Item -ItemType Directory -Force (Split-Path -Parent $dest) | Out-Null
        Copy-Item $_.FullName $dest -Force
        $hash = (Get-FileHash -Path $dest -Algorithm SHA256).Hash.ToLowerInvariant()
        $lines.Add("  lib/${rel}: $hash")
    }
}

Sync-Tree (Join-Path $coreRoot 'lib-sh')
if ($IncludePs) {
    Sync-Tree (Join-Path $coreRoot 'lib-ps')
}

Set-Content -Path $manifest -Value ($lines -join "`n")
$count = ($lines | Where-Object { $_ -like '  lib/*' }).Count
Write-Output "synced $count core files into $PluginRoot/lib (core $coreVersion)"
