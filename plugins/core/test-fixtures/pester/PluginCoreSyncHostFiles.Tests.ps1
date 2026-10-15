#Requires -Version 7.0
# SPDX-License-Identifier: Apache-2.0

# TR-MCP-MEMORY-008 AC-001 (PLAN-SESSIONTRIAGEFIX-001 F2, triage-report-834cec2e): a core sync leaves a plugin's own
# hooks/scripts/memory-context.ps1 byte-identical while it still replaces the core files it owns.
# Mock gate: the outcome assertion is proven against a reference sync stand-in and rejects a sync that deletes the helper, one that rewrites
# it, and one that skips its own work. Binding: the shipped plugins/core/sync/sync-plugin-core.ps1 against a temporary plugin root.

BeforeAll {
    $script:RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../../../..')).ProviderPath
    $script:SyncScript = Join-Path $script:RepoRoot 'plugins/core/sync/sync-plugin-core.ps1'

    # A temporary plugin root: a plugin-owned memory helper under hooks/scripts, and a core file that a previous sync wrote and recorded in
    # CORE-MANIFEST.yaml.
    function Initialize-SyncFixtureRoot {
        $root = Join-Path ([System.IO.Path]::GetTempPath()) ('core-sync-host-' + [guid]::NewGuid().ToString('N'))
        $scripts = Join-Path $root 'hooks/scripts'
        $lib = Join-Path $root 'lib'
        foreach ($dir in $scripts, $lib) { [void][System.IO.Directory]::CreateDirectory($dir) }
        [System.IO.File]::WriteAllText((Join-Path $scripts 'memory-context.ps1'), "# plugin-owned memory helper`nfunction Get-McpMemoryDescriptor { param([string]`$PluginRoot) }`n")
        [System.IO.File]::WriteAllText((Join-Path $lib 'stale-core.ps1'), "# a core file a previous sync wrote`n")
        [System.IO.File]::WriteAllText((Join-Path $root 'CORE-MANIFEST.yaml'), "coreVersion: previous`nfiles:`n  lib/stale-core.ps1: 0000`n")
        return $root
    }

    # TR-MCP-MEMORY-008 AC-001: after $Sync runs on a fixture root, the helper exists byte-identical, and the sync did its own work (the
    # previous core file is gone, the core runtime is present). $Sync: param($PluginRoot).
    function Assert-HostHelperKept {
        param([Parameter(Mandatory)][scriptblock]$Sync)
        $root = Initialize-SyncFixtureRoot
        try {
            $helper = Join-Path $root 'hooks/scripts/memory-context.ps1'
            $before = (Get-FileHash -LiteralPath $helper -Algorithm SHA256).Hash
            $null = & $Sync $root
            Test-Path -LiteralPath $helper -PathType Leaf | Should -BeTrue -Because 'a core sync does not delete a plugin-owned hook helper'
            (Get-FileHash -LiteralPath $helper -Algorithm SHA256).Hash | Should -Be $before -Because 'a core sync does not rewrite a plugin-owned hook helper'
            Test-Path -LiteralPath (Join-Path $root 'lib/stale-core.ps1') | Should -BeFalse -Because 'the sync removes the core files its previous manifest lists'
            Test-Path -LiteralPath (Join-Path $root 'lib/repl-invoke.ps1') -PathType Leaf | Should -BeTrue -Because 'the sync copies the core runtime'
        } finally {
            Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    # Reference stand-in for the sync's observable work on the fixture: removes the manifest's core file, writes the core runtime, and leaves
    # every other file alone.
    $script:ReferenceSync = {
        param($PluginRoot)
        Remove-Item -LiteralPath (Join-Path $PluginRoot 'lib/stale-core.ps1') -Force
        [System.IO.File]::WriteAllText((Join-Path $PluginRoot 'lib/repl-invoke.ps1'), "# core runtime`n")
    }
}

Describe 'TR-MCP-MEMORY-008 AC-001 core sync keeps plugin hook helpers (mock gate)' {
    It 'HostHelperSyncAssertion_AcceptsKeptHelperAndRejectsDeletedRewrittenOrSkippedSync' {
        Assert-HostHelperKept -Sync $script:ReferenceSync
        # Negative controls: a sync that deletes the helper, one that rewrites it, and one that does no sync work at all.
        $deletesHelper = { param($PluginRoot) & $script:ReferenceSync $PluginRoot; Remove-Item -LiteralPath (Join-Path $PluginRoot 'hooks/scripts/memory-context.ps1') -Force }
        { Assert-HostHelperKept -Sync $deletesHelper } | Should -Throw
        $rewritesHelper = { param($PluginRoot) & $script:ReferenceSync $PluginRoot; [System.IO.File]::AppendAllText((Join-Path $PluginRoot 'hooks/scripts/memory-context.ps1'), "# rewritten`n") }
        { Assert-HostHelperKept -Sync $rewritesHelper } | Should -Throw
        $skipsSync = { param($PluginRoot) $null = $PluginRoot }
        { Assert-HostHelperKept -Sync $skipsSync } | Should -Throw
    }
}

Describe 'TR-MCP-MEMORY-008 AC-001 core sync keeps plugin hook helpers (binding)' {
    It 'CoreSync_KeepsPluginMemoryHelper' {
        Assert-HostHelperKept -Sync { param($PluginRoot) & $script:SyncScript -PluginRoot $PluginRoot }
    }
}
