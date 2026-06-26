<#
.SYNOPSIS
    FR-MCP-PLUGINCORE-001: Sync the canonical plugin core into a plugin repo.
.DESCRIPTION
    Copies lib-ps/ into <plugin>/lib/ and writes CORE-MANIFEST.yaml with
    per-file sha256 hashes so CI can detect local edits.
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

function Remove-PreviousCoreFiles {
    if (Test-Path -LiteralPath $manifest) {
        foreach ($line in Get-Content -LiteralPath $manifest) {
            if ($line -match '^  (lib/[^:]+):\s*[0-9a-f]{64}$') {
                $target = Join-Path $PluginRoot ($Matches[1] -replace '/', [System.IO.Path]::DirectorySeparatorChar)
                if (Test-Path -LiteralPath $target -PathType Leaf) {
                    Remove-Item -LiteralPath $target -Force
                }
            }
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

$lines = [System.Collections.Generic.List[string]]::new()
$lines.Add("coreVersion: $coreVersion")
$lines.Add("syncedAtUtc: $syncedAt")
$lines.Add('files:')

function Sync-Tree {
    param([string]$SourceDir)
    if (-not (Test-Path $SourceDir)) { return }
    Get-ChildItem -Path $SourceDir -Recurse -File | Sort-Object FullName | ForEach-Object {
        if ($_.Name -eq 'GAPS.md') { return }
        $rel = $_.FullName.Substring($SourceDir.Length + 1) -replace '\\', '/'
        $dest = Join-Path $PluginRoot "lib/$rel"
        New-Item -ItemType Directory -Force (Split-Path -Parent $dest) | Out-Null
        Copy-Item $_.FullName $dest -Force
        $hash = (Get-FileHash -Path $dest -Algorithm SHA256).Hash.ToLowerInvariant()
        $lines.Add("  lib/${rel}: $hash")
    }
}

Sync-Tree (Join-Path $coreRoot 'lib-ps')

[System.IO.File]::WriteAllText($manifest, (($lines -join "`n") + "`n"), [System.Text.UTF8Encoding]::new($false))
$count = ($lines | Where-Object { $_ -like '  lib/*' }).Count
Write-Output "synced $count core files into $PluginRoot/lib (core $coreVersion)"
