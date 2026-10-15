# SPDX-License-Identifier: Apache-2.0
#Requires -Version 7.0

# TR-MCP-NUKE-012 AC-006 (PLAN-SESSIONTRIAGEFIX-001 D7 to D9; triage-report-b9a8ab7e, -30a24728, -60ac3433): the plugin-core suite carries
# none of the fixture dependencies that made 27 of its tests fail in a fresh worktree.

Describe 'Plugin-core suite isolation (TR-MCP-NUKE-012 AC-006)' {
    BeforeAll {
        # The AC-006 violations as text rules over a test file; a Windows-only command outside an $IsWindows branch is checked on the syntax tree.
        $script:IsolationRules = [ordered]@{
            'repo-root-marker' = '(?m)Get-TestMarkerSnapshot\s*(?:\)|;|\||$)'
            'repo-staged-plugin' = '\$script:RepoRoot\s+''plugins[\\/]core[\\/]\.staged-plugin'''
            'sibling-beside-checkout' = 'Split-Path\s+(?:-Parent\s+\$script:RepoRoot\b|\$script:\w+\s+-Parent\))'
            'hard-coded-path-separator' = '\$env:PATH\s*=\s*"[^"]*;'
            'bash-fake' = '#!/bin/(?:ba)?sh\b'
        }
        $script:WindowsOnlyCommands = @('subst')

        # True when the node sits in the body of an if clause whose condition names $IsWindows.
        function Test-IsolationWindowsBranch {
            param([Parameter(Mandatory)][System.Management.Automation.Language.Ast]$Node)
            for ($parent = $Node.Parent; $null -ne $parent; $parent = $parent.Parent) {
                if ($parent -isnot [System.Management.Automation.Language.IfStatementAst]) { continue }
                foreach ($clause in $parent.Clauses) {
                    $body = $clause.Item2.Extent
                    if ($clause.Item1.Extent.Text -match '\$IsWindows\b' -and $Node.Extent.StartOffset -ge $body.StartOffset -and $Node.Extent.EndOffset -le $body.EndOffset) {
                        return $true
                    }
                }
            }
            return $false
        }

        # The AC-006 rule names a test file's text breaks, each once.
        function Get-PluginCoreIsolationViolation {
            param([Parameter(Mandatory)][AllowEmptyString()][string]$Text)
            $found = [System.Collections.Generic.List[string]]::new()
            foreach ($rule in $script:IsolationRules.Keys) {
                if ([regex]::IsMatch($Text, $script:IsolationRules[$rule])) { $found.Add($rule) }
            }
            $tokens = $null
            $parseErrors = $null
            $ast = [System.Management.Automation.Language.Parser]::ParseInput($Text, [ref]$tokens, [ref]$parseErrors)
            $calls = @($ast.FindAll({ param($node) $node -is [System.Management.Automation.Language.CommandAst] -and $script:WindowsOnlyCommands -contains $node.GetCommandName() }, $true))
            if (@($calls | Where-Object { -not (Test-IsolationWindowsBranch -Node $_) }).Count -gt 0) { $found.Add('unguarded-windows-command') }
            return $found.ToArray()
        }
    }

    It 'IsolationCheck_FlagsEachViolationAndPassesCleanText' {
        # Negative controls: one snippet per AC-006 violation, each flagged by its own rule and by no other.
        $violating = [ordered]@{
            'repo-root-marker' = 'markerFilePath = (Get-TestMarkerSnapshot).markerFilePath'
            'repo-staged-plugin' = '$script:StagedRoot = Join-Path $script:RepoRoot ''plugins\core\.staged-plugin'''
            'sibling-beside-checkout' = '$wrapperPath = Join-Path (Split-Path $script:WrapperRepo -Parent) ''mcpserver-codex-plugin/Invoke-CodexMcpPlugin.ps1'''
            'hard-coded-path-separator' = '$env:PATH = "$bin;$savedPath"'
            'bash-fake' = '$body = @(''#!/bin/bash'', ''sleep 30'')'
            'unguarded-windows-command' = 'subst "Z:" $workspaceHost'
        }
        foreach ($rule in $violating.Keys) {
            (@(Get-PluginCoreIsolationViolation -Text $violating[$rule]) -join ',') | Should -BeExactly $rule -Because "the $rule snippet is flagged by that rule alone"
        }
        (@(Get-PluginCoreIsolationViolation -Text '$parentRoot = Split-Path -Parent $script:RepoRoot') -join ',') | Should -BeExactly 'sibling-beside-checkout'
        (@(Get-PluginCoreIsolationViolation -Text 'if ($IsLinux) { subst "Z:" $workspaceHost }') -join ',') | Should -BeExactly 'unguarded-windows-command'
        (@(Get-PluginCoreIsolationViolation -Text 'if ($IsWindows) { $letter = ''Z'' } else { subst "Z:" $workspaceHost }') -join ',') | Should -BeExactly 'unguarded-windows-command'

        # The compliant forms of the same fixtures raise nothing.
        $clean = @(
            '$snapshot = Get-TestMarkerSnapshot -Workspace $workspace'
            '$script:StagedRoot = Write-TestStagedPlugin -Root (Join-Path $TestDrive ''staged-plugin'')'
            '$siblingParent = Find-TestPluginSiblingParent -CheckoutRoot $script:RepoRoot'
            '$env:PATH = $bin + [IO.Path]::PathSeparator + $savedPath'
            '$body = @(''#!/usr/bin/env -S pwsh -NoLogo -NoProfile -NonInteractive -File'', ''Start-Sleep -Seconds 30'')'
            'if ($IsWindows) { subst "Z:" $workspaceHost } else { $volumeRoot = ''/'' }'
        ) -join [Environment]::NewLine
        @(Get-PluginCoreIsolationViolation -Text $clean).Count | Should -Be 0
    }

    It 'IsolationCheck_PluginCoreSuiteIsClean' {
        # Binding: every other test file of this folder carries none of the AC-006 fixture dependencies.
        $files = @(Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*.Tests.ps1' -File | Where-Object { $_.Name -ne 'PluginCoreSuiteIsolation.Tests.ps1' })
        $files.Count | Should -BeGreaterThan 0
        foreach ($file in $files) {
            (@(Get-PluginCoreIsolationViolation -Text ([System.IO.File]::ReadAllText($file.FullName))) -join ',') | Should -BeExactly '' -Because "$($file.Name) carries none of the AC-006 fixture dependencies"
        }
    }
}
