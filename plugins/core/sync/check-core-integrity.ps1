<#
.SYNOPSIS
    FR-MCP-PLUGINCORE-002: CI checksum guard (PowerShell twin of check-core-integrity.sh).
.DESCRIPTION
    Verifies every file listed in a plugin repo's CORE-MANIFEST.yaml still
    matches its synced sha256. A mismatch means a synced core file was edited
    locally - the fix belongs in McpServer/plugins/core followed by a re-sync.
.PARAMETER PluginRoot
    Root of the plugin repository to verify.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$PluginRoot
)

$ErrorActionPreference = 'Stop'
$manifest = Join-Path $PluginRoot 'CORE-MANIFEST.yaml'

if (-not (Test-Path $manifest)) {
    Write-Error "no CORE-MANIFEST.yaml in $PluginRoot (run sync-plugin-core first)"
    exit 1
}

$failures = 0
$checked = 0
foreach ($line in Get-Content $manifest) {
    if ($line -notmatch '^  (lib/[^:]+):\s*([0-9a-f]{64})$') { continue }
    $rel = $Matches[1]
    $expected = $Matches[2]
    $target = Join-Path $PluginRoot $rel
    $checked++
    if (-not (Test-Path $target)) {
        Write-Warning "MISSING: $rel (listed in manifest, not on disk)"
        $failures++
        continue
    }
    $actual = (Get-FileHash -Path $target -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($actual -ne $expected) {
        Write-Warning "MODIFIED: $rel (local edit detected - edit McpServer/plugins/core and re-sync)"
        $failures++
    }
}

if ($checked -eq 0) {
    Write-Error 'manifest lists no files'
    exit 1
}

if ($failures -gt 0) {
    Write-Error "core integrity check FAILED: $failures of $checked files diverged"
    exit 1
}

Write-Output "core integrity OK: $checked files match"
