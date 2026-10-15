#Requires -Version 7.0
# SPDX-License-Identifier: Apache-2.0

# TR-MCP-REPL-001 AC-001 (PLAN-SESSIONTRIAGEFIX-001 F1, triage-report-eb56d4eb): the plugin REPL bridge answers a call that failed before any
# response envelope with one typed error envelope and exit 1, a success without output writes nothing, and the bridge never writes a bare
# True or False.
# Mock gate: the outcome assertions are proven against pinned outcomes and reject today's bare False, a result envelope, an envelope without
# a code or without the failure text, a zero exit code, and a bare True on success. Binding: the shipped repl-invoke.ps1, in process with its
# backend call stubbed, and as its own process in a temporary workspace (Initialize-TestHookCache) with no REPL executable on PATH. Nothing
# here reaches a server.

BeforeAll {
    Import-Module powershell-yaml -ErrorAction Stop
    $script:RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../../../..')).ProviderPath
    $script:LibRoot = Join-Path $script:RepoRoot 'plugins/core/lib-ps'
    . (Join-Path $PSScriptRoot '../PluginTestFixtures.ps1')

    # TR-MCP-REPL-001 AC-001: a failure outcome is exactly one error envelope naming the failure, with exit code 1 when the outcome has one.
    # Outcome: Stdout, and ExitCode ($null for an in-process call).
    function Assert-ReplBridgeFailureOutcome {
        param([Parameter(Mandatory)]$Outcome, [Parameter(Mandatory)][string]$FailureText)
        $text = ([string]$Outcome.Stdout).Trim()
        $text | Should -Not -BeIn @('True', 'False') -Because 'the bridge never writes a bare boolean'
        $envelope = ConvertFrom-Yaml -Yaml $text -Ordered
        $envelope | Should -BeOfType ([System.Collections.IDictionary]) -Because "stdout is one YAML envelope: $text"
        $envelope['type'] | Should -Be 'error'
        $envelope['payload'] | Should -BeOfType ([System.Collections.IDictionary])
        [string]$envelope['payload']['code'] | Should -Not -BeNullOrEmpty
        [string]$envelope['payload']['message'] | Should -Match ([regex]::Escape($FailureText))
        $envelope['payload']['retryable'] | Should -BeOfType ([bool])
        if ($null -ne $Outcome.ExitCode) { $Outcome.ExitCode | Should -Be 1 }
    }

    # TR-MCP-REPL-001 AC-001: a success without output writes nothing (never True), with exit code 0 when the outcome has one.
    function Assert-ReplBridgeSilentSuccess {
        param([Parameter(Mandatory)]$Outcome)
        ([string]$Outcome.Stdout).Trim() | Should -BeNullOrEmpty -Because 'a success without output writes nothing'
        if ($null -ne $Outcome.ExitCode) { $Outcome.ExitCode | Should -Be 0 }
    }

    # A pinned envelope built from native objects.
    function ConvertTo-PinnedEnvelope {
        param([Parameter(Mandatory)][string]$Type, [Parameter(Mandatory)][System.Collections.IDictionary]$Payload)
        return (ConvertTo-Yaml -Data ([ordered]@{ type = $Type; payload = $Payload }))
    }
}

Describe 'TR-MCP-REPL-001 AC-001 plugin REPL bridge failures (mock gate)' {
    It 'ReplBridgeErrorAssertion_AcceptsErrorEnvelopeAndRejectsBareBooleanOrUntypedOutput' {
        $failure = 'qbrain-ai-repl or mcpserver-repl not found on PATH'
        $correct = ConvertTo-PinnedEnvelope -Type 'error' -Payload ([ordered]@{ code = 'internal_server_error'; message = $failure; retryable = $false; details = [ordered]@{ method = 'workflow.todo.get' } })
        Assert-ReplBridgeFailureOutcome -Outcome @{ Stdout = $correct; ExitCode = 1 } -FailureText $failure
        Assert-ReplBridgeFailureOutcome -Outcome @{ Stdout = $correct; ExitCode = $null } -FailureText $failure
        Assert-ReplBridgeSilentSuccess -Outcome @{ Stdout = ''; ExitCode = 0 }

        # Negative controls: today's bare False, a result envelope, an envelope without a code, one naming another failure, a zero exit code,
        # and a bare True or a non-zero exit code on success.
        { Assert-ReplBridgeFailureOutcome -Outcome @{ Stdout = 'False'; ExitCode = 1 } -FailureText $failure } | Should -Throw
        $result = ConvertTo-PinnedEnvelope -Type 'result' -Payload ([ordered]@{ code = 'internal_server_error'; message = $failure; retryable = $false })
        { Assert-ReplBridgeFailureOutcome -Outcome @{ Stdout = $result; ExitCode = 1 } -FailureText $failure } | Should -Throw
        $noCode = ConvertTo-PinnedEnvelope -Type 'error' -Payload ([ordered]@{ message = $failure; retryable = $false })
        { Assert-ReplBridgeFailureOutcome -Outcome @{ Stdout = $noCode; ExitCode = 1 } -FailureText $failure } | Should -Throw
        $otherFailure = ConvertTo-PinnedEnvelope -Type 'error' -Payload ([ordered]@{ code = 'internal_server_error'; message = 'Plugin invocation failed.'; retryable = $false })
        { Assert-ReplBridgeFailureOutcome -Outcome @{ Stdout = $otherFailure; ExitCode = 1 } -FailureText $failure } | Should -Throw
        { Assert-ReplBridgeFailureOutcome -Outcome @{ Stdout = $correct; ExitCode = 0 } -FailureText $failure } | Should -Throw
        { Assert-ReplBridgeSilentSuccess -Outcome @{ Stdout = 'True'; ExitCode = 0 } } | Should -Throw
        { Assert-ReplBridgeSilentSuccess -Outcome @{ Stdout = ''; ExitCode = 1 } } | Should -Throw
    }
}

Describe 'TR-MCP-REPL-001 AC-001 plugin REPL bridge failures (binding)' {
    It 'ReplBridge_FailureWithoutOutput_WritesErrorEnvelope' {
        . (Join-Path $script:LibRoot 'repl-invoke.ps1')
        $previousRaw = Get-Command Invoke-ReplRaw -CommandType Function -ErrorAction SilentlyContinue
        try {
            # A pre-process failure that carries its error text.
            function Invoke-ReplRaw { New-McpPluginReplResult -Success $false -Output '' -Error 'qbrain-ai-repl or mcpserver-repl not found on PATH' }
            $stdout = Invoke-ReplMethod -Method 'workflow.todo.get' -ParamsYaml 'id: PLAN-F1-TEST-001' | Out-String
            $script:LastInvokeReplMethodSuccess | Should -BeFalse
            Assert-ReplBridgeFailureOutcome -Outcome @{ Stdout = $stdout; ExitCode = $null } -FailureText 'qbrain-ai-repl or mcpserver-repl not found on PATH'

            # The REPL exited non-zero and wrote nothing: the envelope names its exit code.
            function Invoke-ReplRaw { New-McpPluginReplResult -Success $false -Output '' -ExitCode 3 }
            $stdout = Invoke-ReplMethod -Method 'workflow.memory.list' -ParamsYaml 'scope: Effective' | Out-String
            $script:LastInvokeReplMethodSuccess | Should -BeFalse
            Assert-ReplBridgeFailureOutcome -Outcome @{ Stdout = $stdout; ExitCode = $null } -FailureText 'exited with code 3'
        } finally {
            if ($previousRaw) { Set-Item -Path Function:\Invoke-ReplRaw -Value $previousRaw.ScriptBlock } else { Remove-Item Function:\Invoke-ReplRaw -ErrorAction SilentlyContinue }
        }
    }

    It 'ReplBridge_SuccessWithoutOutput_WritesNothing' {
        . (Join-Path $script:LibRoot 'repl-invoke.ps1')
        $previousRaw = Get-Command Invoke-ReplRaw -CommandType Function -ErrorAction SilentlyContinue
        try {
            function Invoke-ReplRaw { New-McpPluginReplResult -Success $true -Output '' -ExitCode 0 }
            $stdout = Invoke-ReplMethod -Method 'workflow.todo.get' -ParamsYaml 'id: PLAN-F1-TEST-001' | Out-String
            $script:LastInvokeReplMethodSuccess | Should -BeTrue
            Assert-ReplBridgeSilentSuccess -Outcome @{ Stdout = $stdout; ExitCode = $null }
        } finally {
            if ($previousRaw) { Set-Item -Path Function:\Invoke-ReplRaw -Value $previousRaw.ScriptBlock } else { Remove-Item Function:\Invoke-ReplRaw -ErrorAction SilentlyContinue }
        }
    }

    It 'ReplBridge_Script_MissingRepl_ExitsOneWithErrorEnvelope' {
        $root = Join-Path ([System.IO.Path]::GetTempPath()) ('f1-bridge-' + [guid]::NewGuid().ToString('N'))
        try {
            $cache = Initialize-TestHookCache -LibRoot $script:LibRoot -Root $root
            $separator = [System.IO.Path]::PathSeparator
            $replNames = @('qbrain-ai-repl', 'mcpserver-repl', 'qbrain-ai-repl.exe', 'mcpserver-repl.exe', 'qbrain-ai-repl.cmd', 'mcpserver-repl.cmd')
            # PATH without any directory that holds a REPL executable, so the bridge finds none.
            $path = @(foreach ($dir in @($env:PATH -split [regex]::Escape([string]$separator))) {
                    if (-not $dir) { continue }
                    $holdsRepl = @($replNames | Where-Object { Test-Path -LiteralPath (Join-Path $dir $_) -ErrorAction SilentlyContinue }).Count -gt 0
                    if (-not $holdsRepl) { $dir }
                })
            $psi = [System.Diagnostics.ProcessStartInfo]::new((Get-Command pwsh -ErrorAction Stop).Source)
            foreach ($argument in @('-NoLogo', '-NoProfile', '-NonInteractive', '-File', (Join-Path $script:LibRoot 'repl-invoke.ps1'), '-Method', 'workflow.todo.get', '-ParamsYaml', 'id: PLAN-F1-TEST-001')) { $psi.ArgumentList.Add($argument) }
            $psi.WorkingDirectory = $cache.Workspace
            $psi.UseShellExecute = $false
            $psi.RedirectStandardOutput = $true
            $psi.RedirectStandardError = $true
            foreach ($name in @('MCP_REPL_EXECUTABLE', 'MCP_PLUGIN_REPL_LOG', 'MCP_PLUGIN_REPL_RESPONSE', 'MCP_PLUGIN_PERSIST_LOG', 'MCP_HOOK_DEADLINE_UTC_MS')) { [void]$psi.Environment.Remove($name) }
            $psi.Environment['PATH'] = ($path -join $separator)
            $psi.Environment['MCP_PLUGIN_HOST'] = 'claude-code'
            $psi.Environment['MCP_AGENT_NAME'] = 'ClaudeCode'
            $psi.Environment['MCP_CACHE_DIR_OVERRIDE'] = $cache.Cache
            $psi.Environment['MCP_FAILSAFE_DRAIN_DISABLED'] = '1'
            $psi.Environment['MCP_WORKSPACE_PATH'] = $cache.Workspace
            $psi.Environment['MCPSERVER_WORKSPACE_PATH'] = $cache.Workspace
            $process = [System.Diagnostics.Process]::Start($psi)
            $stdout = $process.StandardOutput.ReadToEndAsync()
            $stderr = $process.StandardError.ReadToEndAsync()
            $process.WaitForExit(120000) | Should -BeTrue
            Assert-ReplBridgeFailureOutcome -Outcome @{ Stdout = $stdout.Result; ExitCode = $process.ExitCode } -FailureText 'not found on PATH'
            $stderr.Result | Should -Not -Match 'MCP_UNTRUSTED' -Because 'the temporary workspace''s verified cache passes the marker check, so the failure is the missing REPL'
        } finally {
            Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}
