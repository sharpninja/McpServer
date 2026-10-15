# SPDX-License-Identifier: Apache-2.0
# TR-MCP-PLUGIN-013 AC-005 (PLAN-SESSIONTRIAGEFIX-001 D3, triage-report-061f30e9): a background or hostile-agent prompt opens and logs its
# own turn in the root's session, kept in a cache folder of its own; the root's turn is never reused, skipped, canceled or completed by it.
# Mock gate: the outcome assertions and the identity and routing rules are proven against pinned outcomes, with negative controls, before
# any production change. Fixtures: PluginTestFixtures.ps1 (assertions, pinned cases); no live marker, no server.
#Requires -Version 7.0

BeforeAll {
    . (Join-Path (Split-Path $PSScriptRoot -Parent) 'PluginTestFixtures.ps1')
}

Describe 'D3 background prompt turns: assertion mock gate (TR-MCP-PLUGIN-013 AC-005)' {
    BeforeAll {
        $script:Session = 'ClaudeCode-20261015T000000Z-plugin-session'
        $script:RootTurn = 'req-20261015T100000Z-root-work'
        $script:BackgroundTurn = 'req-20261015T100500Z-prompt-0b0b'

        function Get-TestTurnRef {
            param([string]$Id, [string]$Status, [string]$SessionId = '')
            return [pscustomobject]@{ Id = $Id; Status = $Status; SessionId = $SessionId }
        }

        function Get-OwnTurnOutcome {
            param([string]$RootStatus = 'in_progress')
            return [pscustomobject]@{
                RootSessionId = $script:Session
                RootBefore = Get-TestTurnRef -Id $script:RootTurn -Status $RootStatus
                RootAfter = Get-TestTurnRef -Id $script:RootTurn -Status $RootStatus
                Scope = Get-TestTurnRef -Id $script:BackgroundTurn -Status 'in_progress' -SessionId $script:Session
                Persist = @([pscustomobject]@{
                        requestId = $script:BackgroundTurn
                        status = 'in_progress'
                        contextList = @("root session: $($script:Session)", "root turn: $($script:RootTurn)")
                        tags = @('background-agent')
                    })
                HookStatus = 'turn-opened'
                HookIsolation = ''
            }
        }
    }

    It 'BackgroundTurnAssertion_AcceptsOwnTurnAndRejectsReuseSkipOrSupersede' {
        # The criterion's outcome passes with the root turn in progress and with it completed.
        @(Test-BackgroundTurnOutcome -Outcome (Get-OwnTurnOutcome)) | Should -HaveCount 0
        @(Test-BackgroundTurnOutcome -Outcome (Get-OwnTurnOutcome -RootStatus 'completed')) | Should -HaveCount 0

        # Negative control: today's reuse outcome (no turn of its own; the hook reports the root turn as already open).
        $reuse = Get-OwnTurnOutcome
        $reuse.Scope = $null
        $reuse.Persist = @()
        $reuse.HookStatus = 'turn-already-open'
        $reuse.HookIsolation = 'reuse'
        $violations = @(Test-BackgroundTurnOutcome -Outcome $reuse)
        $violations | Should -Contain 'no background turn was opened'
        $violations | Should -Contain 'hook reused or skipped (turn-already-open reuse)'

        # Negative control: today's isolate-skip outcome on a completed root turn.
        $skip = Get-OwnTurnOutcome -RootStatus 'completed'
        $skip.Scope = $null
        $skip.Persist = @()
        $skip.HookStatus = 'root-turn-isolated'
        $skip.HookIsolation = 'isolate-skip'
        $violations = @(Test-BackgroundTurnOutcome -Outcome $skip)
        $violations | Should -Contain 'no background turn was opened'
        $violations | Should -Contain 'hook reused or skipped (root-turn-isolated isolate-skip)'

        # Negative control: an ordinary open that supersedes the root turn and replaces the root's current-turn.yaml.
        $supersede = Get-OwnTurnOutcome
        $supersede.Scope = $null
        $supersede.RootAfter = Get-TestTurnRef -Id $script:BackgroundTurn -Status 'in_progress'
        $supersede.Persist = @(
            [pscustomobject]@{ requestId = $script:RootTurn; status = 'canceled'; contextList = @(); tags = @() }
            [pscustomobject]@{ requestId = $script:BackgroundTurn; status = 'in_progress'; contextList = @(); tags = @() }
        )
        $violations = @(Test-BackgroundTurnOutcome -Outcome $supersede)
        $violations | Should -Contain 'root turn was canceled'
        $violations | Should -Contain "root current-turn changed from $($script:RootTurn)/in_progress to $($script:BackgroundTurn)/in_progress"

        # Negative control: a background turn opened in another session.
        $otherSession = Get-OwnTurnOutcome
        $otherSession.Scope = Get-TestTurnRef -Id $script:BackgroundTurn -Status 'in_progress' -SessionId 'ClaudeCode-20261015T090000Z-plugin-session'
        @(Test-BackgroundTurnOutcome -Outcome $otherSession) | Should -Contain "background turn is in session 'ClaudeCode-20261015T090000Z-plugin-session'"

        # Negative control: a background turn whose first write does not carry its root ids or its tag.
        $noRootIds = Get-OwnTurnOutcome
        $noRootIds.Persist = @([pscustomobject]@{ requestId = $script:BackgroundTurn; status = 'in_progress'; contextList = @(); tags = @() })
        $violations = @(Test-BackgroundTurnOutcome -Outcome $noRootIds)
        $violations | Should -Contain 'first background write lacks the root session id'
        $violations | Should -Contain 'first background write lacks the root turn id'
        $violations | Should -Contain 'first background write lacks the background-agent tag'

        # A brief with the root's own identity opens a root turn like an operator prompt.
        $rootPrompt = [pscustomobject]@{
            RootSessionId = $script:Session
            RootBefore = Get-TestTurnRef -Id $script:RootTurn -Status 'in_progress'
            RootAfter = Get-TestTurnRef -Id 'req-20261015T100600Z-prompt-0c0c' -Status 'in_progress' -SessionId $script:Session
            ScopeFolders = 0
            Persist = @([pscustomobject]@{ requestId = $script:RootTurn; status = 'canceled' })
        }
        @(Test-RootPromptOutcome -Outcome $rootPrompt) | Should -HaveCount 0
        # Negative control: the same brief scoped as a background agent, leaving the root turn as it was.
        $scopedRootPrompt = [pscustomobject]@{
            RootSessionId = $script:Session
            RootBefore = Get-TestTurnRef -Id $script:RootTurn -Status 'in_progress'
            RootAfter = Get-TestTurnRef -Id $script:RootTurn -Status 'in_progress' -SessionId $script:Session
            ScopeFolders = 1
            Persist = @()
        }
        $violations = @(Test-RootPromptOutcome -Outcome $scopedRootPrompt)
        $violations | Should -Contain 'no new root turn was opened'
        $violations | Should -Contain 'a background folder was created for a brief with the root identity'
        $violations | Should -Contain 'the previous in-progress root turn was not superseded'
    }

    It 'ScopeIdentityRule_ReferencePassesAndRejectsWrongRules' {
        $cases = @(Get-TestBackgroundScopeCase)
        $cases.Count | Should -Be 7
        $mismatches = {
            param([scriptblock]$Rule)
            @($cases | Where-Object { [string](& $Rule $_) -cne [string]$_.Expected } | ForEach-Object { $_.Name })
        }
        $reference = {
            param($Case)
            $key = @($Case.Mcp, $Case.Grok, $Case.Payload) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -First 1
            if (-not $key -or $key -ceq $Case.Root) { '' } else { $key }
        }
        @(& $mismatches $reference) | Should -HaveCount 0

        # Negative controls: the payload before the environment ids, scoping an identity equal to the root's, never scoping.
        $payloadFirst = {
            param($Case)
            $key = @($Case.Payload, $Case.Mcp, $Case.Grok) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -First 1
            if (-not $key -or $key -ceq $Case.Root) { '' } else { $key }
        }
        @(& $mismatches $payloadFirst) | Should -Contain 'subagent id first'
        $scopesRoot = {
            param($Case)
            $key = @($Case.Mcp, $Case.Grok, $Case.Payload) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -First 1
            if (-not $key) { '' } else { $key }
        }
        @(& $mismatches $scopesRoot) | Should -Contain 'payload session id equal to the root'
        $neverScopes = { param($Case) [void]$Case; '' }
        @(& $mismatches $neverScopes) | Should -Contain 'grok id without a subagent id'

        # Where a session-log verb the agent calls itself resolves its cache.
        $routes = @(Get-TestBackgroundRoutingCase)
        $routes.Count | Should -Be 5
        $routeMismatches = {
            param([scriptblock]$Rule)
            @($routes | Where-Object { [string](& $Rule $_) -cne [string]$_.Expected } | ForEach-Object { $_.Name })
        }
        $routeReference = {
            param($Case)
            $key = @($Case.Mcp, $Case.Grok) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -First 1
            if ($Case.Override) { 'override' } elseif ($key -and $Case.ScopeExists) { 'scope' } else { 'root' }
        }
        @(& $routeMismatches $routeReference) | Should -HaveCount 0
        # Negative controls: never routing to the folder, routing without checking that the folder holds a turn, ignoring the override.
        $alwaysRoot = { param($Case) if ($Case.Override) { 'override' } else { 'root' } }
        @(& $routeMismatches $alwaysRoot) | Should -Contain 'subagent id with its folder'
        $noExistenceCheck = {
            param($Case)
            $key = @($Case.Mcp, $Case.Grok) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -First 1
            if ($Case.Override) { 'override' } elseif ($key) { 'scope' } else { 'root' }
        }
        @(& $routeMismatches $noExistenceCheck) | Should -Contain 'subagent id without a folder'
        $overrideIgnored = {
            param($Case)
            $key = @($Case.Mcp, $Case.Grok) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -First 1
            if ($key -and $Case.ScopeExists) { 'scope' } else { 'root' }
        }
        @(& $routeMismatches $overrideIgnored) | Should -Contain 'explicit override wins'
    }

    It 'StopOutcomeAssertion_AcceptsOwnCompletionAndRejectsRootCompletion' {
        $correct = [pscustomobject]@{
            RootBefore = Get-TestTurnRef -Id $script:RootTurn -Status 'in_progress'
            RootAfter = Get-TestTurnRef -Id $script:RootTurn -Status 'in_progress'
            ScopeBefore = Get-TestTurnRef -Id $script:BackgroundTurn -Status 'in_progress'
            ScopeAfter = Get-TestTurnRef -Id $script:BackgroundTurn -Status 'completed'
            Persist = @([pscustomobject]@{ requestId = $script:BackgroundTurn; status = 'completed' })
        }
        @(Test-BackgroundStopOutcome -Outcome $correct) | Should -HaveCount 0

        # Negative control: the background host's Stop completes the root turn and leaves its own open.
        $rootCompleted = [pscustomobject]@{
            RootBefore = Get-TestTurnRef -Id $script:RootTurn -Status 'in_progress'
            RootAfter = Get-TestTurnRef -Id $script:RootTurn -Status 'completed'
            ScopeBefore = Get-TestTurnRef -Id $script:BackgroundTurn -Status 'in_progress'
            ScopeAfter = Get-TestTurnRef -Id $script:BackgroundTurn -Status 'in_progress'
            Persist = @([pscustomobject]@{ requestId = $script:RootTurn; status = 'completed' })
        }
        $violations = @(Test-BackgroundStopOutcome -Outcome $rootCompleted)
        $violations | Should -Contain 'the background Stop wrote the root turn'
        $violations | Should -Contain "root current-turn changed from $($script:RootTurn)/in_progress to $($script:RootTurn)/completed"
        $violations | Should -Contain "background turn status is 'in_progress'"

        # Negative control: a background turn with no recorded work left in progress (today's phantom no-op).
        $leftOpen = [pscustomobject]@{
            RootBefore = Get-TestTurnRef -Id $script:RootTurn -Status 'in_progress'
            RootAfter = Get-TestTurnRef -Id $script:RootTurn -Status 'in_progress'
            ScopeBefore = Get-TestTurnRef -Id $script:BackgroundTurn -Status 'in_progress'
            ScopeAfter = Get-TestTurnRef -Id $script:BackgroundTurn -Status 'in_progress'
            Persist = @()
        }
        $violations = @(Test-BackgroundStopOutcome -Outcome $leftOpen)
        $violations | Should -Contain "background turn status is 'in_progress'"
        $violations | Should -Contain 'the background completion was not written'
    }
}

Describe 'D3 background prompt turns: bindings (TR-MCP-PLUGIN-013 AC-005)' {
    # The same pinned identities, routing cases and outcome assertions as the mock gate, bound to the shipped functions, the shipped hook
    # (its own pwsh process, the real REPL local verbs, the persist seam MCP_PLUGIN_PERSIST_LOG, a temporary cache and test marker) and the
    # REPL's cache resolution. Never the live marker or server.
    BeforeAll {
        $script:LibRoot = Join-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) 'lib-ps'
        . (Join-Path $script:LibRoot 'resolve-cache-dir.ps1')
        $script:Brief = "You are the HOSTILE VALIDATOR for workspace D3.`nFIRST ACTION (mandatory, before any validation):`nExecute the add-profile skill now."
        $script:RootTurn = 'req-20261015T100000Z-root-work'

        function Get-HookOutputField {
            param([string]$Stdout, [string]$Name)
            $line = @($Stdout -split "`r?`n" | Where-Object { $_.TrimStart().StartsWith('{') }) | Select-Object -Last 1
            if (-not $line) { return '' }
            try { return [string](($line | ConvertFrom-Json -Depth 20).hookSpecificOutput.$Name) } catch { return '' }
        }

        function Get-PromptOutcome {
            param($Cache, $Hook, [string]$ScopeKey, [string]$RootStatus)
            $rootAfter = Read-TestTurnState -Path (Join-Path $Cache.Cache 'current-turn.yaml')
            $scopeState = Read-TestTurnState -Path (Join-Path (Join-McpBackgroundScopePath -CacheDir $Cache.Cache -Key $ScopeKey) 'current-turn.yaml')
            return [pscustomobject]@{
                RootSessionId = $Cache.SessionId
                RootBefore = [pscustomobject]@{ Id = $Cache.RootTurnId; Status = $RootStatus }
                RootAfter = [pscustomobject]@{ Id = $rootAfter.Id; Status = $rootAfter.Status; SessionId = $rootAfter.SessionId }
                Scope = if ($scopeState.Exists) { [pscustomobject]@{ Id = $scopeState.Id; Status = $scopeState.Status; SessionId = $scopeState.SessionId } } else { $null }
                Persist = @(Read-TestPersistLog -Path $Cache.PersistLog)
                HookStatus = Get-HookOutputField -Stdout $Hook.Stdout -Name 'status'
                HookIsolation = Get-HookOutputField -Stdout $Hook.Stdout -Name 'isolation'
                ScopeFolders = @(Get-ChildItem -LiteralPath (Join-Path $Cache.Cache 'background') -Directory -ErrorAction SilentlyContinue).Count
            }
        }
    }

    It 'ScopeIdentityRule_ShippedFunction_PinnedIdentities' {
        $savedMcp = $env:MCP_SUBAGENT_ID
        $savedGrok = $env:GROK_SUBAGENT_ID
        try {
            foreach ($case in @(Get-TestBackgroundScopeCase)) {
                if ($case.Mcp) { $env:MCP_SUBAGENT_ID = $case.Mcp } else { Remove-Item Env:\MCP_SUBAGENT_ID -ErrorAction SilentlyContinue }
                if ($case.Grok) { $env:GROK_SUBAGENT_ID = $case.Grok } else { Remove-Item Env:\GROK_SUBAGENT_ID -ErrorAction SilentlyContinue }
                [string](Get-McpBackgroundScopeKey -PayloadSessionId $case.Payload -RootAgentSessionId $case.Root) | Should -BeExactly $case.Expected -Because $case.Name
            }
        } finally {
            if ($null -ne $savedMcp) { $env:MCP_SUBAGENT_ID = $savedMcp } else { Remove-Item Env:\MCP_SUBAGENT_ID -ErrorAction SilentlyContinue }
            if ($null -ne $savedGrok) { $env:GROK_SUBAGENT_ID = $savedGrok } else { Remove-Item Env:\GROK_SUBAGENT_ID -ErrorAction SilentlyContinue }
        }
    }

    It 'UserPromptSubmit_BackgroundBrief_OpensOwnTurnInRootSession' {
        $cache = Initialize-TestHookCache -LibRoot $script:LibRoot -Root (Join-Path $TestDrive 'bg-open-in-progress') -RootTurnId $script:RootTurn -RootTurnStatus 'in_progress'
        $hook = Invoke-TestPluginHook -LibRoot $script:LibRoot -Cache $cache -HookName 'user-prompt-submit' -Payload @{ prompt = $script:Brief; session_id = 'bg-host-1' }
        $outcome = Get-PromptOutcome -Cache $cache -Hook $hook -ScopeKey 'bg-host-1' -RootStatus 'in_progress'
        @(Test-BackgroundTurnOutcome -Outcome $outcome) | Should -HaveCount 0 -Because "hook stdout: $($hook.Stdout) stderr: $($hook.Stderr)"
        $outcome.ScopeFolders | Should -Be 1
    }

    It 'UserPromptSubmit_BackgroundBrief_CompletedRoot_OpensOwnTurn' {
        $cache = Initialize-TestHookCache -LibRoot $script:LibRoot -Root (Join-Path $TestDrive 'bg-open-completed') -RootTurnId $script:RootTurn -RootTurnStatus 'completed'
        $hook = Invoke-TestPluginHook -LibRoot $script:LibRoot -Cache $cache -HookName 'user-prompt-submit' -Payload @{ prompt = $script:Brief; session_id = 'bg-host-2' }
        $outcome = Get-PromptOutcome -Cache $cache -Hook $hook -ScopeKey 'bg-host-2' -RootStatus 'completed'
        @(Test-BackgroundTurnOutcome -Outcome $outcome) | Should -HaveCount 0 -Because "hook stdout: $($hook.Stdout) stderr: $($hook.Stderr)"
    }

    It 'UserPromptSubmit_BriefWithRootIdentity_OpensRootTurn' {
        $cache = Initialize-TestHookCache -LibRoot $script:LibRoot -Root (Join-Path $TestDrive 'root-identity') -RootTurnId $script:RootTurn -RootTurnStatus 'in_progress'
        $hook = Invoke-TestPluginHook -LibRoot $script:LibRoot -Cache $cache -HookName 'user-prompt-submit' -Payload @{ prompt = $script:Brief; session_id = $cache.RootAgentSessionId }
        $outcome = Get-PromptOutcome -Cache $cache -Hook $hook -ScopeKey $cache.RootAgentSessionId -RootStatus 'in_progress'
        @(Test-RootPromptOutcome -Outcome $outcome) | Should -HaveCount 0 -Because "hook stdout: $($hook.Stdout) stderr: $($hook.Stderr)"
    }

    It 'Stop_BackgroundHost_CompletesOwnTurnOnly' {
        $cache = Initialize-TestHookCache -LibRoot $script:LibRoot -Root (Join-Path $TestDrive 'bg-stop') -RootTurnId $script:RootTurn -RootTurnStatus 'in_progress'
        $open = Invoke-TestPluginHook -LibRoot $script:LibRoot -Cache $cache -HookName 'user-prompt-submit' -Payload @{ prompt = $script:Brief; session_id = 'bg-host-3' }
        $scopeTurnFile = Join-Path (Join-McpBackgroundScopePath -CacheDir $cache.Cache -Key 'bg-host-3') 'current-turn.yaml'
        $scopeBefore = Read-TestTurnState -Path $scopeTurnFile
        $scopeBefore.Status | Should -Be 'in_progress' -Because "open stdout: $($open.Stdout) stderr: $($open.Stderr)"
        $writesBefore = @(Read-TestPersistLog -Path $cache.PersistLog).Count
        $stop = Invoke-TestPluginHook -LibRoot $script:LibRoot -Cache $cache -HookName 'stop-gate' -Payload @{ session_id = 'bg-host-3' }
        $rootAfter = Read-TestTurnState -Path (Join-Path $cache.Cache 'current-turn.yaml')
        $scopeAfter = Read-TestTurnState -Path $scopeTurnFile
        $outcome = [pscustomobject]@{
            RootBefore = [pscustomobject]@{ Id = $script:RootTurn; Status = 'in_progress' }
            RootAfter = [pscustomobject]@{ Id = $rootAfter.Id; Status = $rootAfter.Status }
            ScopeBefore = [pscustomobject]@{ Id = $scopeBefore.Id; Status = $scopeBefore.Status }
            ScopeAfter = [pscustomobject]@{ Id = $scopeAfter.Id; Status = $scopeAfter.Status }
            Persist = @(Read-TestPersistLog -Path $cache.PersistLog | Select-Object -Skip $writesBefore)
        }
        @(Test-BackgroundStopOutcome -Outcome $outcome) | Should -HaveCount 0 -Because "stop stdout: $($stop.Stdout) stderr: $($stop.Stderr)"
    }

    It 'SessionLogVerb_SubagentEnvironment_ReachesOwnTurn' {
        $names = @('MCP_CACHE_DIR_OVERRIDE', 'MCP_WORKSPACE_PATH', 'MCPSERVER_WORKSPACE_PATH', 'CLAUDE_PROJECT_DIR', 'MCP_AGENT_NAME', 'PLUGIN_AGENT_NAME', 'PLUGIN_AGENT_DEFAULT', 'MCP_PLUGIN_HOST', 'MCP_SUBAGENT_ID', 'GROK_SUBAGENT_ID', 'MCP_PLUGIN_PERSIST_LOG')
        $saved = @{}
        foreach ($name in $names) { $saved[$name] = [Environment]::GetEnvironmentVariable($name) }
        $priorLocation = Get-Location
        try {
            $cache = Initialize-TestHookCache -LibRoot $script:LibRoot -Root (Join-Path $TestDrive 'repl-route') -RootTurnId $script:RootTurn -RootTurnStatus 'in_progress'
            foreach ($name in $names) { [Environment]::SetEnvironmentVariable($name, $null) }
            $env:MCP_AGENT_NAME = 'ClaudeCode'
            $env:MCP_WORKSPACE_PATH = $cache.Workspace
            Set-Location -LiteralPath $cache.Workspace
            # Without an override the REPL resolves the root cache from the workspace: <workspace>/.mcpServer/<agent key>.
            $rootCache = Join-McpWorkspaceCachePath -WorkspacePath $cache.Workspace
            [void][IO.Directory]::CreateDirectory($rootCache)
            foreach ($file in @('session-state.yaml', 'current-turn.yaml')) { Copy-Item -LiteralPath (Join-Path $cache.Cache $file) -Destination (Join-Path $rootCache $file) }
            $rootTurnText = [IO.File]::ReadAllText((Join-Path $rootCache 'current-turn.yaml'))
            $backgroundTurn = 'req-20261015T100700Z-prompt-0d0d'
            $scopeTurnText = $rootTurnText.Replace($script:RootTurn, $backgroundTurn)
            $override = Join-Path $cache.Root 'override'
            [void][IO.Directory]::CreateDirectory($override)
            . (Join-Path $script:LibRoot 'repl-invoke.ps1')

            foreach ($case in @(Get-TestBackgroundRoutingCase)) {
                $key = @($case.Mcp, $case.Grok) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -First 1
                $env:MCP_SUBAGENT_ID = if ($case.Mcp) { $case.Mcp } else { $null }
                $env:GROK_SUBAGENT_ID = if ($case.Grok) { $case.Grok } else { $null }
                $env:MCP_CACHE_DIR_OVERRIDE = if ($case.Override) { $override } else { $null }
                $scopeDir = if ($key) { Join-McpBackgroundScopePath -CacheDir $rootCache -Key $key } else { '' }
                if ($key) {
                    [void][IO.Directory]::CreateDirectory($scopeDir)
                    Copy-Item -LiteralPath (Join-Path $rootCache 'session-state.yaml') -Destination (Join-Path $scopeDir 'session-state.yaml') -Force
                    $scopeTurnFile = Join-Path $scopeDir 'current-turn.yaml'
                    if ($case.ScopeExists) { [IO.File]::WriteAllText($scopeTurnFile, $scopeTurnText) } elseif (Test-Path -LiteralPath $scopeTurnFile) { Remove-Item -LiteralPath $scopeTurnFile }
                }
                $expected = switch ($case.Expected) { 'override' { $override } 'scope' { $scopeDir } default { $rootCache } }
                [string](Get-ReplInvokeCacheDir) | Should -Be $expected -Because $case.Name
            }

            # A session-log verb the subagent calls itself writes its own turn and leaves the root's turn as it was.
            $env:MCP_SUBAGENT_ID = $null
            $env:MCP_CACHE_DIR_OVERRIDE = $null
            $env:GROK_SUBAGENT_ID = 'grok-7'
            $env:MCP_PLUGIN_PERSIST_LOG = $cache.PersistLog
            $grokScope = Join-McpBackgroundScopePath -CacheDir $rootCache -Key 'grok-7'
            [IO.File]::WriteAllText((Join-Path $grokScope 'current-turn.yaml'), $scopeTurnText)
            Invoke-WorkflowUpdateTurn -ParamsYaml "response: background agent work`n" | Should -BeTrue
            [IO.File]::ReadAllText((Join-Path $grokScope 'current-turn.yaml')) | Should -Match '(?m)^response: background agent work'
            [IO.File]::ReadAllText((Join-Path $rootCache 'current-turn.yaml')) | Should -BeExactly $rootTurnText
            $writes = @(Read-TestPersistLog -Path $cache.PersistLog)
            @($writes | Where-Object { $_.requestId -eq $backgroundTurn }).Count | Should -Be 1
            @($writes | Where-Object { $_.requestId -eq $script:RootTurn }).Count | Should -Be 0
        } finally {
            Set-Location -LiteralPath $priorLocation
            foreach ($name in $names) { [Environment]::SetEnvironmentVariable($name, $saved[$name]) }
        }
    }
}
