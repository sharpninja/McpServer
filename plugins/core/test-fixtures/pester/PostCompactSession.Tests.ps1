#Requires -Version 7.0
# SPDX-License-Identifier: Apache-2.0

# TR-MCP-PLUGIN-012 AC-003 (PLAN-SESSIONTRIAGEFIX-001 D2, triage-report-d9bd6fc3): a compaction leaves exactly one new session-log session.
# The PostCompact hook starts no session, leaves session-state.yaml unchanged and emits only {} (the status-only output contract); the
# SessionStart hook that follows a compaction starts the one session. Pinned 2026-10-14: the store holds 22 empty "plugin-session" twins,
# each minted 3 to 5 seconds before the session SessionStart minted. Mock gate: the assertion is proven against a reference result and
# rejects faulty ones before plugin-hook.ps1 changes. The binding test runs the shipped hook with -HookName post-compact on a temporary
# workspace (unsigned marker, verified session-state) through the MCP_PLUGIN_REPL_LOG seam; nothing reaches a server.

BeforeAll {
    # $Run returns @{ OpenSessionCalls; SessionStateBefore; SessionStateAfter; Output } for one PostCompact hook run.
    function Assert-PostCompactStartsNoSession {
        param([Parameter(Mandatory)][scriptblock]$Run)
        $result = & $Run
        $result.OpenSessionCalls | Should -Be 0 -Because 'PostCompact opens no session'
        $result.SessionStateAfter | Should -BeExactly $result.SessionStateBefore -Because 'PostCompact leaves session-state.yaml unchanged'
        ([string]$result.Output).Trim() | Should -BeExactly '{}' -Because 'PostCompact is status-only'
    }
}

Describe 'D2 post-compact session mock gate' {
    It 'PostCompactAssertion_AcceptsNoSessionAndRejectsMintOrOutput' {
        $state = "status: verified`nsessionId: ClaudeCode-20261014T000000Z-d2`n"
        Assert-PostCompactStartsNoSession -Run { @{ OpenSessionCalls = 0; SessionStateBefore = $state; SessionStateAfter = $state; Output = '{}' } }
        # Negative control: today's PostCompact, which mints and opens a new session.
        { Assert-PostCompactStartsNoSession -Run { @{ OpenSessionCalls = 1; SessionStateBefore = $state; SessionStateAfter = "status: verified`nsessionId: ClaudeCode-20261014T000004Z-d2`n"; Output = '{}' } } } | Should -Throw
        # Negative control: no open call, but session-state is overwritten (a failed bootstrap writes MCP_UNTRUSTED).
        { Assert-PostCompactStartsNoSession -Run { @{ OpenSessionCalls = 0; SessionStateBefore = $state; SessionStateAfter = "status: MCP_UNTRUSTED`n"; Output = '{}' } } } | Should -Throw
        # Negative control: output beyond {} (the status-only contract forbids additionalContext).
        { Assert-PostCompactStartsNoSession -Run { @{ OpenSessionCalls = 0; SessionStateBefore = $state; SessionStateAfter = $state; Output = '{"hookSpecificOutput":{"additionalContext":"x"}}' } } } | Should -Throw
    }
}

Describe 'D2 post-compact session binding' {
    BeforeAll {
        $script:RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../../../..')).ProviderPath
        $script:LibRoot = Join-Path $script:RepoRoot 'plugins/core/lib-ps'
        $script:HookScript = Join-Path $script:LibRoot 'plugin-hook.ps1'
        . (Join-Path $script:LibRoot 'repl-invoke.ps1')
    }

    It 'PostCompact_StartsNoSessionAndKeepsSessionState' {
        Assert-PostCompactStartsNoSession -Run {
            $root = Join-Path ([System.IO.Path]::GetTempPath()) ('d2-hook-' + [guid]::NewGuid().ToString('N'))
            $workspace = Join-Path $root 'ws'
            $cache = Join-Path $root 'cache'
            $pluginRoot = Join-Path $root 'plugin'
            foreach ($dir in $workspace, $cache, $pluginRoot) { [void][System.IO.Directory]::CreateDirectory($dir) }
            try {
                # An unsigned marker: a session start here fails bootstrap and writes MCP_UNTRUSTED, so any start shows in session-state.
                [System.IO.File]::WriteAllText((Join-Path $workspace 'AGENTS-README-FIRST.yaml'), "workspacePath: $workspace`napiKey: d2-fixture`n")
                $snapshot = Get-MarkerFileSnapshot -StartDir $workspace
                $sessionFile = Join-Path $cache 'session-state.yaml'
                Write-McpYamlObject -Path $sessionFile -Document ([ordered]@{
                        status = 'verified'
                        sessionId = 'ClaudeCode-20261014T000000Z-d2'
                        agent = 'ClaudeCode'
                        markerFilePath = $snapshot.markerFilePath
                        markerLastWriteUtc = $snapshot.markerLastWriteUtc
                    })
                $before = [System.IO.File]::ReadAllText($sessionFile)
                $log = Join-Path $root 'repl.log'
                $psi = [System.Diagnostics.ProcessStartInfo]::new((Get-Command pwsh -ErrorAction Stop).Source)
                foreach ($argument in @('-NoLogo', '-NoProfile', '-NonInteractive', '-File', $script:HookScript, '-HookName', 'post-compact', '-HostName', 'claude-code', '-CacheMode', 'flat', '-WorkspacePath', $workspace)) { $psi.ArgumentList.Add($argument) }
                $psi.WorkingDirectory = $workspace
                $psi.UseShellExecute = $false
                $psi.RedirectStandardInput = $true
                $psi.RedirectStandardOutput = $true
                $psi.RedirectStandardError = $true
                foreach ($inherited in 'MCP_HOOK_DEADLINE_UTC_MS', 'MCP_PLUGIN_PERSIST_LOG', 'MCPSERVER_FAILSAFE_DIR', 'MCP_FAILSAFE_DIR') { [void]$psi.Environment.Remove($inherited) }
                $psi.Environment['MCP_PLUGIN_HOST'] = 'claude-code'
                $psi.Environment['MCP_AGENT_NAME'] = 'ClaudeCode'
                $psi.Environment['MCP_PLUGIN_ROOT'] = $pluginRoot
                $psi.Environment['MCP_CACHE_DIR_OVERRIDE'] = $cache
                $psi.Environment['MCP_PLUGIN_REPL_LOG'] = $log
                $psi.Environment['MCP_PLUGIN_REPL_RESPONSE'] = 'ok'
                $psi.Environment['MCP_WORKSPACE_PATH'] = $workspace
                $psi.Environment['MCPSERVER_WORKSPACE_PATH'] = $workspace
                $process = [System.Diagnostics.Process]::Start($psi)
                $process.StandardInput.Write((@{ hook_event_name = 'PostCompact'; trigger = 'auto' } | ConvertTo-Json -Compress))
                $process.StandardInput.Close()
                $stdout = $process.StandardOutput.ReadToEndAsync()
                $stderr = $process.StandardError.ReadToEndAsync()
                $process.WaitForExit(60000) | Should -BeTrue
                $null = $stderr.Result
                $logText = if (Test-Path -LiteralPath $log) { [System.IO.File]::ReadAllText($log) } else { '' }
                return @{
                    OpenSessionCalls = ([regex]::Matches($logText, '(?m)^method: client\.SessionLog\.OpenSessionAsync\s*$')).Count
                    SessionStateBefore = $before
                    SessionStateAfter = [System.IO.File]::ReadAllText($sessionFile)
                    Output = $stdout.Result
                }
            } finally {
                Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue
            }
        }
    }
}
