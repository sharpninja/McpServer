#Requires -Version 7.0
# SPDX-License-Identifier: Apache-2.0

# TR-MCP-MEMORY-008 AC-002 (PLAN-SESSIONTRIAGEFIX-001 F2, triage-report-f01b3544; operator 2026-10-15: "Fix injection now"): the generated
# UserPromptSubmit hook entry adds the plugin's required-memory block to the hook output, keeps the hook's own fields and exit code, leaves
# the output alone without a helper, and keeps the memory fetch to the hook deadline (TR-MCP-REPL-012 AC-001).
# Mock gate: the outcome assertions are proven against pinned outcomes and reject today's output, a replaced status, a dropped field, a lost
# earlier context, a changed exit code, a second JSON document, a fetch started past the deadline and a fetch timeout longer than the time
# left. Binding: the shipped generate-wrappers.ps1 and wrapper template, staged into temporary plugin roots with a stand-in memory helper
# that logs the hook output it receives; the hook runs against a temporary workspace (Initialize-TestHookCache) with the persist seam, never
# a server.

BeforeAll {
    $script:RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../../../..')).ProviderPath
    $script:LibRoot = Join-Path $script:RepoRoot 'plugins/core/lib-ps'
    . (Join-Path $PSScriptRoot '../PluginTestFixtures.ps1')

    # The JSON documents (one per line) a hook entry wrote to stdout.
    function Get-HookJsonLine {
        param([AllowEmptyString()][string]$Stdout)
        return @(([string]$Stdout -split "`r?`n") | ForEach-Object { $_.Trim() } | Where-Object { $_.StartsWith('{') -and $_.EndsWith('}') })
    }

    # TR-MCP-MEMORY-008 AC-002: exactly one JSON document whose hookSpecificOutput carries ExpectedBlock in additionalContext (before any
    # context the hook wrote), every other field the hook wrote unchanged, and the hook's exit code. Outcome: Stdout, ExitCode, FetchLog
    # (the stand-in helper's lines; 'hook-output: <json>' is what the hook alone wrote).
    function Assert-InjectedHookOutcome {
        param([Parameter(Mandatory)]$Outcome, [Parameter(Mandatory)][string]$ExpectedBlock, [int]$ExpectedExitCode = 0)
        $documents = @(Get-HookJsonLine -Stdout $Outcome.Stdout)
        $documents.Count | Should -Be 1 -Because "the entry writes one JSON document: $($Outcome.Stdout)"
        $merged = $documents[0] | ConvertFrom-Json -AsHashtable
        $hookLine = @($Outcome.FetchLog | Where-Object { $_ -like 'hook-output: *' } | Select-Object -First 1)
        $hookLine.Count | Should -Be 1 -Because 'the helper received the hook output'
        $original = $hookLine[0].Substring('hook-output: '.Length) | ConvertFrom-Json -AsHashtable
        foreach ($key in $original.Keys) {
            if ($key -ne 'hookSpecificOutput') { ($merged[$key] | ConvertTo-Json -Compress -Depth 20) | Should -Be ($original[$key] | ConvertTo-Json -Compress -Depth 20) -Because "top-level $key is kept" }
        }
        $mergedHso = $merged['hookSpecificOutput']
        $mergedHso | Should -Not -BeNullOrEmpty
        foreach ($key in $original['hookSpecificOutput'].Keys) {
            if ($key -eq 'additionalContext') { continue }
            ($mergedHso[$key] | ConvertTo-Json -Compress -Depth 20) | Should -Be ($original['hookSpecificOutput'][$key] | ConvertTo-Json -Compress -Depth 20) -Because "hookSpecificOutput.$key is kept"
        }
        $context = [string]$mergedHso['additionalContext']
        $context | Should -Match ([regex]::Escape($ExpectedBlock))
        $earlier = [string]$original['hookSpecificOutput']['additionalContext']
        if ($earlier) { $context | Should -Match ([regex]::Escape($earlier)) -Because 'the context the hook wrote is kept' }
        $Outcome.ExitCode | Should -Be $ExpectedExitCode
    }

    # TR-MCP-MEMORY-008 AC-002: without a helper the entry writes the hook's output only: no memory block and no fetch.
    function Assert-UnchangedWithoutHelper {
        param([Parameter(Mandatory)]$Outcome)
        $documents = @(Get-HookJsonLine -Stdout $Outcome.Stdout)
        $documents.Count | Should -Be 1
        [string](($documents[0] | ConvertFrom-Json -AsHashtable)['hookSpecificOutput']['additionalContext']) | Should -Not -Match 'REQUIRED MEMORIES'
        @($Outcome.FetchLog).Count | Should -Be 0
        $Outcome.ExitCode | Should -Be 0
    }

    # TR-MCP-MEMORY-008 AC-002 with TR-MCP-REPL-012 AC-001: past the deadline (under one second left) no fetch starts; otherwise the fetch
    # timeout is between one second and the whole seconds the helper still saw left (one boundary of slack for the time between the two).
    function Assert-FetchKeptToDeadline {
        param([Parameter(Mandatory)]$Outcome, [Parameter(Mandatory)][bool]$PastDeadline)
        $fetch = @($Outcome.FetchLog | Where-Object { $_ -like 'fetch-*' })
        $fetch.Count | Should -Be 1
        if ($PastDeadline) {
            $fetch[0] | Should -BeLike 'fetch-skipped*' -Because 'with under one second left the fetch is not started'
            return
        }
        # Should -Match does not set $Matches in this scope, so the match is taken explicitly.
        $match = [regex]::Match([string]$fetch[0], '^fetch-started timeout=(\d+) remainingMs=(-?\d+)$')
        $match.Success | Should -BeTrue -Because "a started fetch logs its timeout and the time left: $($fetch[0])"
        $timeout = [int]$match.Groups[1].Value
        $remaining = [long]$match.Groups[2].Value
        $timeout | Should -BeGreaterOrEqual 1
        $timeout | Should -BeLessOrEqual ([Math]::Floor($remaining / 1000) + 1) -Because 'the fetch timeout is no longer than the whole seconds left'
    }

    # A stand-in for a plugin's memory-context.ps1: logs the hook output it receives and how the fetch went, then merges a fixed block.
    function Write-StandInMemoryHelper {
        param([Parameter(Mandatory)][string]$Path)
        $lines = @(
            'function Add-McpRequiredMemoryToHookOutput {'
            '    param([string]$HookOutput, [string]$PluginRoot, [string]$DescriptorPath, [scriptblock]$FetchOverride)'
            '    $log = $env:INJECTION_TEST_LOG'
            '    $json = @(($HookOutput -split "`r?`n") | ForEach-Object { $_.Trim() } | Where-Object { $_.StartsWith(''{'') -and $_.EndsWith(''}'') } | Select-Object -Last 1)'
            '    if ($json.Count -eq 0) { return $HookOutput }'
            '    [System.IO.File]::AppendAllText($log, ''hook-output: '' + $json[0] + "`n")'
            '    $block = ''REQUIRED MEMORIES - STUB-MEMORY'''
            '    if ($FetchOverride) {'
            '        try { $null = & $FetchOverride; [System.IO.File]::AppendAllText($log, "fetch-override-returned`n") }'
            '        catch { $block = ''REQUIRED MEMORIES - None.''; [System.IO.File]::AppendAllText($log, ''fetch-skipped: '' + $_.Exception.Message + "`n") }'
            '    } else {'
            '        $deadline = 0L'
            '        $remaining = if ([long]::TryParse([string]$env:MCP_HOOK_DEADLINE_UTC_MS, [ref]$deadline)) { $deadline - [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds() } else { -1 }'
            '        [System.IO.File]::AppendAllText($log, ''fetch-started timeout='' + [string]$env:MCP_MEMORY_REPL_TIMEOUT_SECONDS + '' remainingMs='' + $remaining + "`n")'
            '    }'
            '    $doc = $json[0] | ConvertFrom-Json -AsHashtable'
            '    $hso = $doc[''hookSpecificOutput'']'
            '    $existing = [string]$hso[''additionalContext'']'
            '    $hso[''additionalContext''] = if ($existing) { $block + "`n`n" + $existing } else { $block }'
            '    return ($doc | ConvertTo-Json -Depth 20 -Compress)'
            '}'
        ) -join [Environment]::NewLine
        [void][System.IO.Directory]::CreateDirectory((Split-Path -Parent $Path))
        [System.IO.File]::WriteAllText($Path, $lines, [System.Text.UTF8Encoding]::new($false))
    }

    # Stages the plugin core and the generated wrappers for HostName into Root (the scripts Nuke SyncAgentPlugins runs; both write only under
    # their -PluginRoot).
    function Initialize-InjectionPlugin {
        param([Parameter(Mandatory)][string]$Root, [Parameter(Mandatory)][string]$HostName)
        $pwsh = (Get-Command pwsh -ErrorAction Stop).Source
        [void][System.IO.Directory]::CreateDirectory($Root)
        $output = & $pwsh -NoLogo -NoProfile -NonInteractive -File (Join-Path $script:RepoRoot 'plugins/core/sync/sync-plugin-core.ps1') -PluginRoot $Root 2>&1 | Out-String
        if ($LASTEXITCODE -ne 0) { throw "sync-plugin-core.ps1 exited $LASTEXITCODE staging ${Root}: $output" }
        $output = & $pwsh -NoLogo -NoProfile -NonInteractive -File (Join-Path $script:RepoRoot 'plugins/core/hooks-templates/generate-wrappers.ps1') -HostName $HostName -PluginRoot $Root 2>&1 | Out-String
        if ($LASTEXITCODE -ne 0) { throw "generate-wrappers.ps1 exited $LASTEXITCODE staging ${Root}: $output" }
        return $Root
    }

    # Runs a generated UserPromptSubmit entry as its own pwsh process against a temporary workspace cache, with the persist seam and the
    # failsafe drain disabled; Environment adds or overrides variables ($null removes one). Returns ExitCode, Stdout, Stderr, FetchLog.
    function Invoke-UserPromptEntry {
        param(
            [Parameter(Mandatory)][string]$EntryPath,
            [Parameter(Mandatory)]$Cache,
            [Parameter(Mandatory)][string]$AgentName,
            [System.Collections.IDictionary]$Environment = @{})
        $log = Join-Path $Cache.Root 'injection.log'
        [System.IO.File]::WriteAllText($log, '')
        $psi = [System.Diagnostics.ProcessStartInfo]::new((Get-Command pwsh -ErrorAction Stop).Source)
        foreach ($argument in @('-NoLogo', '-NoProfile', '-NonInteractive', '-File', $EntryPath)) { $psi.ArgumentList.Add($argument) }
        $psi.WorkingDirectory = $Cache.Workspace
        $psi.UseShellExecute = $false
        $psi.RedirectStandardInput = $true
        $psi.RedirectStandardOutput = $true
        $psi.RedirectStandardError = $true
        foreach ($name in @('MCP_SUBAGENT_ID', 'GROK_SUBAGENT_ID', 'MCP_PLUGIN_REPL_LOG', 'MCP_PLUGIN_REPL_RESPONSE', 'CLAUDE_STOP_HOOK_ACTIVE', 'MCP_HOOK_DEADLINE_UTC_MS', 'MCP_MEMORY_REPL_TIMEOUT_SECONDS', 'MCP_PLUGIN_ROOT')) {
            [void]$psi.Environment.Remove($name)
        }
        $psi.Environment['MCP_AGENT_NAME'] = $AgentName
        $psi.Environment['MCP_CACHE_DIR_OVERRIDE'] = $Cache.Cache
        $psi.Environment['MCP_PLUGIN_PERSIST_LOG'] = $Cache.PersistLog
        $psi.Environment['MCP_FAILSAFE_DRAIN_DISABLED'] = '1'
        $psi.Environment['MCP_WORKSPACE_PATH'] = $Cache.Workspace
        $psi.Environment['MCPSERVER_WORKSPACE_PATH'] = $Cache.Workspace
        $psi.Environment['INJECTION_TEST_LOG'] = $log
        foreach ($key in $Environment.Keys) {
            if ($null -eq $Environment[$key]) { [void]$psi.Environment.Remove([string]$key) } else { $psi.Environment[[string]$key] = [string]$Environment[$key] }
        }
        $process = [System.Diagnostics.Process]::Start($psi)
        $process.StandardInput.Write((@{ prompt = 'Check the required-memory injection'; hook_event_name = 'UserPromptSubmit' } | ConvertTo-Json -Compress))
        $process.StandardInput.Close()
        $stdout = $process.StandardOutput.ReadToEndAsync()
        $stderr = $process.StandardError.ReadToEndAsync()
        if (-not $process.WaitForExit(120000)) { $process.Kill($true); throw "$EntryPath did not exit within 120 s" }
        return [pscustomobject]@{
            ExitCode = $process.ExitCode
            Stdout = $stdout.Result
            Stderr = $stderr.Result
            FetchLog = @([System.IO.File]::ReadAllLines($log) | Where-Object { $_ })
        }
    }

    # A pinned hook output and its merged form, built from native objects.
    function ConvertTo-PinnedHookJson {
        param([Parameter(Mandatory)][System.Collections.IDictionary]$HookSpecificOutput, [System.Collections.IDictionary]$Extra = @{})
        $doc = [ordered]@{ hookSpecificOutput = $HookSpecificOutput }
        foreach ($key in $Extra.Keys) { $doc[$key] = $Extra[$key] }
        return ($doc | ConvertTo-Json -Depth 20 -Compress)
    }
}

Describe 'TR-MCP-MEMORY-008 AC-002 UserPromptSubmit required-memory injection (mock gate)' {
    It 'InjectedHookAssertion_AcceptsMergedBlockAndRejectsMissingReplacedOrLateFetch' {
        $block = 'REQUIRED MEMORIES - STUB-MEMORY'
        $hook = ConvertTo-PinnedHookJson -HookSpecificOutput ([ordered]@{ hookEventName = 'UserPromptSubmit'; status = 'turn-opened'; turnRequestId = 'req-1'; additionalContext = 'session log turn req-1 is now active.' }) -Extra ([ordered]@{ continue = $true })
        $merged = ConvertTo-PinnedHookJson -HookSpecificOutput ([ordered]@{ hookEventName = 'UserPromptSubmit'; status = 'turn-opened'; turnRequestId = 'req-1'; additionalContext = $block + "`n`nsession log turn req-1 is now active." }) -Extra ([ordered]@{ continue = $true })
        $log = @("hook-output: $hook", 'fetch-started timeout=4 remainingMs=4500')
        $correct = @{ Stdout = $merged; ExitCode = 0; FetchLog = $log }
        Assert-InjectedHookOutcome -Outcome $correct -ExpectedBlock $block
        Assert-FetchKeptToDeadline -Outcome $correct -PastDeadline $false

        # Negative controls for the merge: today's output, a replaced status, a dropped field, the hook's context lost, a changed exit code,
        # and a second JSON document.
        { Assert-InjectedHookOutcome -Outcome @{ Stdout = $hook; ExitCode = 0; FetchLog = $log } -ExpectedBlock $block } | Should -Throw
        $replaced = ConvertTo-PinnedHookJson -HookSpecificOutput ([ordered]@{ hookEventName = 'UserPromptSubmit'; status = 'memory-injected'; turnRequestId = 'req-1'; additionalContext = $block + "`n`nsession log turn req-1 is now active." }) -Extra ([ordered]@{ continue = $true })
        { Assert-InjectedHookOutcome -Outcome @{ Stdout = $replaced; ExitCode = 0; FetchLog = $log } -ExpectedBlock $block } | Should -Throw
        $dropped = ConvertTo-PinnedHookJson -HookSpecificOutput ([ordered]@{ hookEventName = 'UserPromptSubmit'; status = 'turn-opened'; additionalContext = $block + "`n`nsession log turn req-1 is now active." }) -Extra ([ordered]@{ continue = $true })
        { Assert-InjectedHookOutcome -Outcome @{ Stdout = $dropped; ExitCode = 0; FetchLog = $log } -ExpectedBlock $block } | Should -Throw
        $lost = ConvertTo-PinnedHookJson -HookSpecificOutput ([ordered]@{ hookEventName = 'UserPromptSubmit'; status = 'turn-opened'; turnRequestId = 'req-1'; additionalContext = $block }) -Extra ([ordered]@{ continue = $true })
        { Assert-InjectedHookOutcome -Outcome @{ Stdout = $lost; ExitCode = 0; FetchLog = $log } -ExpectedBlock $block } | Should -Throw
        { Assert-InjectedHookOutcome -Outcome @{ Stdout = $merged; ExitCode = 1; FetchLog = $log } -ExpectedBlock $block } | Should -Throw
        { Assert-InjectedHookOutcome -Outcome @{ Stdout = ($hook + "`n" + $merged); ExitCode = 0; FetchLog = $log } -ExpectedBlock $block } | Should -Throw

        # Without a helper: the hook output alone passes; a block or a fetch does not.
        Assert-UnchangedWithoutHelper -Outcome @{ Stdout = $hook; ExitCode = 0; FetchLog = @() }
        { Assert-UnchangedWithoutHelper -Outcome @{ Stdout = $merged; ExitCode = 0; FetchLog = @() } } | Should -Throw
        { Assert-UnchangedWithoutHelper -Outcome @{ Stdout = $hook; ExitCode = 0; FetchLog = $log } } | Should -Throw

        # The deadline: a skipped fetch with the empty fallback passes; a fetch started past the deadline, and a timeout longer than the time
        # left, do not.
        $none = 'REQUIRED MEMORIES - None.'
        $mergedNone = ConvertTo-PinnedHookJson -HookSpecificOutput ([ordered]@{ hookEventName = 'UserPromptSubmit'; status = 'turn-opened'; turnRequestId = 'req-1'; additionalContext = $none + "`n`nsession log turn req-1 is now active." }) -Extra ([ordered]@{ continue = $true })
        $skipped = @{ Stdout = $mergedNone; ExitCode = 0; FetchLog = @("hook-output: $hook", 'fetch-skipped: the hook deadline left no time to fetch required memories') }
        Assert-InjectedHookOutcome -Outcome $skipped -ExpectedBlock $none
        Assert-FetchKeptToDeadline -Outcome $skipped -PastDeadline $true
        { Assert-FetchKeptToDeadline -Outcome $correct -PastDeadline $true } | Should -Throw
        { Assert-FetchKeptToDeadline -Outcome @{ Stdout = $merged; ExitCode = 0; FetchLog = @("hook-output: $hook", 'fetch-started timeout=7 remainingMs=3000') } -PastDeadline $false } | Should -Throw
        { Assert-FetchKeptToDeadline -Outcome @{ Stdout = $merged; ExitCode = 0; FetchLog = @("hook-output: $hook", 'fetch-started timeout= remainingMs=3000') } -PastDeadline $false } | Should -Throw
    }
}

Describe 'TR-MCP-MEMORY-008 AC-002 UserPromptSubmit required-memory injection (binding)' {
    BeforeEach {
        $script:Work = Join-Path ([System.IO.Path]::GetTempPath()) ('prompt-memory-' + [guid]::NewGuid().ToString('N'))
        [void][System.IO.Directory]::CreateDirectory($script:Work)
    }

    AfterEach {
        Remove-Item -LiteralPath $script:Work -Recurse -Force -ErrorAction SilentlyContinue
    }

    It 'Wrapper_UserPromptSubmit_MergesHelperBlockKeepingStatusAndExit' {
        $plugin = Initialize-InjectionPlugin -Root (Join-Path $script:Work 'plugin') -HostName 'claude-code'
        Write-StandInMemoryHelper -Path (Join-Path $plugin 'hooks/scripts/memory-context.ps1')
        $cache = Initialize-TestHookCache -LibRoot $script:LibRoot -Root (Join-Path $script:Work 'cache')
        $outcome = Invoke-UserPromptEntry -EntryPath (Join-Path $plugin 'hooks/scripts/user-prompt-submit.ps1') -Cache $cache -AgentName 'ClaudeCode'
        Assert-InjectedHookOutcome -Outcome $outcome -ExpectedBlock 'REQUIRED MEMORIES - STUB-MEMORY'
        Assert-FetchKeptToDeadline -Outcome $outcome -PastDeadline $false
    }

    It 'Wrapper_UserPromptSubmit_NoHelper_LeavesOutputUnchanged' {
        $plugin = Initialize-InjectionPlugin -Root (Join-Path $script:Work 'plugin') -HostName 'claude-code'
        $cache = Initialize-TestHookCache -LibRoot $script:LibRoot -Root (Join-Path $script:Work 'cache')
        $outcome = Invoke-UserPromptEntry -EntryPath (Join-Path $plugin 'hooks/scripts/user-prompt-submit.ps1') -Cache $cache -AgentName 'ClaudeCode'
        Assert-UnchangedWithoutHelper -Outcome $outcome
    }

    It 'Wrapper_UserPromptSubmit_PastDeadline_SkipsFetchWithEmptyFallback' {
        $plugin = Initialize-InjectionPlugin -Root (Join-Path $script:Work 'plugin') -HostName 'claude-code'
        Write-StandInMemoryHelper -Path (Join-Path $plugin 'hooks/scripts/memory-context.ps1')
        # Without a registered timeout the hook publishes no deadline of its own, so the expired one given here stays in force.
        Remove-Item -LiteralPath (Join-Path $plugin 'hooks/hooks.json') -Force
        $cache = Initialize-TestHookCache -LibRoot $script:LibRoot -Root (Join-Path $script:Work 'cache')
        $expired = [string]([DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds() - 5000)
        $outcome = Invoke-UserPromptEntry -EntryPath (Join-Path $plugin 'hooks/scripts/user-prompt-submit.ps1') -Cache $cache -AgentName 'ClaudeCode' -Environment @{ MCP_HOOK_DEADLINE_UTC_MS = $expired }
        Assert-InjectedHookOutcome -Outcome $outcome -ExpectedBlock 'REQUIRED MEMORIES - None.'
        Assert-FetchKeptToDeadline -Outcome $outcome -PastDeadline $true
    }

    It 'Wrapper_CodexLibEntry_FindsHelperInHooksScripts' {
        $plugin = Initialize-InjectionPlugin -Root (Join-Path $script:Work 'plugin') -HostName 'codex'
        Write-StandInMemoryHelper -Path (Join-Path $plugin 'hooks/scripts/memory-context.ps1')
        $cache = Initialize-TestHookCache -LibRoot $script:LibRoot -Root (Join-Path $script:Work 'cache')
        # The cache fixture records a ClaudeCode session; a Codex agent would hit the foreign-agent refusal (TEST-MCP-BUGTRIAGE-032), which
        # is not what this case proves: that Codex's lib/ entry finds the helper under hooks/scripts.
        $outcome = Invoke-UserPromptEntry -EntryPath (Join-Path $plugin 'lib/user-prompt-submit.ps1') -Cache $cache -AgentName 'ClaudeCode'
        Assert-InjectedHookOutcome -Outcome $outcome -ExpectedBlock 'REQUIRED MEMORIES - STUB-MEMORY'
        @($outcome.FetchLog | Where-Object { $_ -like 'fetch-started*' }).Count | Should -Be 1
    }
}
