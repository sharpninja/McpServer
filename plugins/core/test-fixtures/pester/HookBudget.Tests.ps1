#Requires -Version 7.0
# SPDX-License-Identifier: Apache-2.0

# TR-MCP-REPL-012 AC-001 and AC-002, TR-MCP-REPL-016 AC-001 and AC-002, TR-MCP-REPL-019 AC-001 (PLAN-SESSIONTRIAGEFIX-001 D1,
# triage-report-124a374c; operator decision 2026-10-14 "Full fix (Recommended)").
# Mock gate: each assertion below is proven against a reference script block and rejects a faulty one (negative control) before the
# production functions change. The binding tests run the same assertions against the shipped functions and processes, with a pwsh
# fake REPL, a temporary cache, failsafe queue and plugin root; nothing here reaches a live server.

BeforeAll {
    Import-Module powershell-yaml -ErrorAction Stop

    function Initialize-D1TempDir {
        param([Parameter(Mandatory)][string]$Prefix)
        $dir = Join-Path ([System.IO.Path]::GetTempPath()) ($Prefix + [guid]::NewGuid().ToString('N'))
        [void][System.IO.Directory]::CreateDirectory($dir)
        return $dir
    }

    # A hooks.json shaped like plugins/core/hooks-templates/hooks.claude-code.json, registering each named hook script with its timeout.
    function Write-D1HooksJson {
        param([Parameter(Mandatory)][string]$PluginRoot, [Parameter(Mandatory)][System.Collections.IDictionary]$Timeouts)
        $events = [ordered]@{}
        $index = 0
        foreach ($name in $Timeouts.Keys) {
            $events["Event$index"] = @([ordered]@{
                    hooks = @([ordered]@{
                            type = 'command'
                            command = "pwsh -NoLogo -NoProfile -NonInteractive -File `"`${CLAUDE_PLUGIN_ROOT}/hooks/scripts/$name.ps1`""
                            timeout = [int]$Timeouts[$name]
                        })
                })
            $index++
        }
        $hooksDir = Join-Path $PluginRoot 'hooks'
        [void][System.IO.Directory]::CreateDirectory($hooksDir)
        $path = Join-Path $hooksDir 'hooks.json'
        [System.IO.File]::WriteAllText($path, ([ordered]@{ description = 'D1 fixture'; hooks = $events } | ConvertTo-Json -Depth 20))
        return $path
    }

    # Failsafe queue records shaped like Write-ReplFailsafe output; the file name sort is the capture order.
    function Write-D1Queue {
        param([Parameter(Mandatory)][string]$Dir, [int]$Count = 2, [string]$SessionId = 'ClaudeCode-20261014T000000Z-d1', [string]$FirstRequestId = '')
        $paths = @()
        for ($i = 0; $i -lt $Count; $i++) {
            $requestId = if ($i -eq 0 -and $FirstRequestId) { $FirstRequestId } else { 'req-20261014T00000{0}Z-queued-{1}' -f $i, $i }
            $stamp = '20261014T00000{0}Z' -f $i
            $record = [ordered]@{
                method = 'client.SessionLog.SubmitAsync'
                label = 'session_submit'
                timestamp = $stamp
                params = [ordered]@{
                    sessionLog = [ordered]@{
                        sourceType = 'ClaudeCode'
                        sessionId = $SessionId
                        turns = @([ordered]@{ requestId = $requestId; status = 'in_progress'; queryTitle = 'queued'; planFile = 'None'; todoId = 'None' })
                    }
                }
            }
            $path = Join-Path $Dir ('{0}-session_submit-{1:x4}.yaml' -f $stamp, $i)
            [System.IO.File]::WriteAllText($path, (ConvertTo-Yaml -Data $record))
            $paths += $path
        }
        return $paths
    }

    # TR-MCP-REPL-012 AC-001: deadline = process start + registered timeout - 3 s; no registered timeout or no hooks.json, no deadline.
    function Assert-HookDeadline {
        param([Parameter(Mandatory)][scriptblock]$Compute)
        $root = Initialize-D1TempDir -Prefix 'd1-deadline-'
        try {
            $hooks = Write-D1HooksJson -PluginRoot $root -Timeouts ([ordered]@{ 'user-prompt-submit' = 10; 'session-start' = 15 })
            $start = [datetime]::new(2026, 10, 14, 0, 0, 0, [DateTimeKind]::Utc)
            $startMs = ([DateTimeOffset]$start).ToUnixTimeMilliseconds()
            (& $Compute $hooks 'user-prompt-submit' $start) | Should -Be ($startMs + 7000)
            (& $Compute $hooks 'session-start' $start) | Should -Be ($startMs + 12000)
            (& $Compute $hooks 'health-check' $start) | Should -BeNullOrEmpty
            (& $Compute (Join-Path $root 'absent.json') 'user-prompt-submit' $start) | Should -BeNullOrEmpty
        } finally {
            Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    # TR-MCP-REPL-012 AC-001: timeout = min(per-method, whole seconds left); 0 (do not start) under one second left;
    # no deadline, per-method unchanged.
    function Assert-TimeoutCap {
        param([Parameter(Mandatory)][scriptblock]$Cap)
        (& $Cap 30 $null) | Should -Be 30
        (& $Cap 120 $null) | Should -Be 120
        (& $Cap 30 7400) | Should -Be 7
        (& $Cap 120 3000) | Should -Be 3
        (& $Cap 30 1500) | Should -Be 1
        (& $Cap 30 500) | Should -Be 0
        (& $Cap 30 -2500) | Should -Be 0
        (& $Cap 5 60000) | Should -Be 5
    }

    # TR-MCP-REPL-016 AC-002: with under one second left the drain replays nothing and leaves records untouched; with time left it replays.
    # $Drain: param($QueueDir, $DeadlineUtcMs) returns the number of replay calls it made (replays succeed).
    function Assert-DrainStopsAtDeadline {
        param([Parameter(Mandatory)][scriptblock]$Drain)
        $dir = Initialize-D1TempDir -Prefix 'd1-drain-'
        try {
            $records = Write-D1Queue -Dir $dir -Count 2
            $now = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()
            (& $Drain $dir ($now + 500)) | Should -Be 0
            foreach ($record in $records) {
                Test-Path -LiteralPath $record | Should -BeTrue
                [System.IO.File]::ReadAllText($record) | Should -Not -Match 'drainAttempts'
            }
            (& $Drain $dir ([DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds() + 120000)) | Should -Be 2
        } finally {
            Remove-Item -LiteralPath $dir -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    # TR-MCP-REPL-016 AC-001: the drain replays after the method's own backend calls, also in a one-call invocation.
    # $Run: param($Scenario) returns the backend calls in order as 'method|requestId' ('' when the call carries no turn).
    # -Scenario limits the check to one scenario; by default both are checked.
    function Assert-ReplayOrder {
        param([Parameter(Mandatory)][scriptblock]$Run, [ValidateSet('single-call', 'begin-turn', 'both')][string]$Scenario = 'both')
        if ($Scenario -in 'single-call', 'both') {
            (@(& $Run 'single-call') -join ';') | Should -Be 'workflow.requirements.getFr|;client.SessionLog.SubmitAsync|req-20261014T000000Z-queued'
        }
        if ($Scenario -in 'begin-turn', 'both') {
            (@(& $Run 'begin-turn') -join ';') | Should -Be 'client.SessionLog.SubmitAsync|req-20261014T000001Z-old;client.SessionLog.SubmitAsync|req-20261014T000002Z-new;client.SessionLog.SubmitAsync|req-20261014T000000Z-queued'
        }
    }

    # TR-MCP-REPL-019 AC-001: no server read; a refined local title is sent, a raw or empty one is omitted.
    # $Supersede: param($LocalTitle, $QueryText) returns @{ QueryCalls; SentTitle } (SentTitle empty when omitted).
    function Assert-SupersedeTitle {
        param([Parameter(Mandatory)][scriptblock]$Supersede)
        $refined = & $Supersede 'Refined: harden the login retry path' "Fix the login flow`nIt fails on retry."
        $refined.QueryCalls | Should -Be 0
        $refined.SentTitle | Should -Be 'Refined: harden the login retry path'
        foreach ($case in @(@('Fix the login flow', "Fix the login flow`nIt fails on retry."), @('User prompt', 'User prompt'), @('', 'Fix it'))) {
            $result = & $Supersede $case[0] $case[1]
            $result.QueryCalls | Should -Be 0
            $result.SentTitle | Should -BeNullOrEmpty
        }
    }

    # TR-MCP-REPL-012 AC-002: a hook whose server calls do not answer returns before its registered timeout, degraded, record kept,
    # also when it must first supersede an in-progress turn (two calls in one hook). The registered timeout is the production
    # UserPromptSubmit value (10 s, hooks.claude-code.json), so a call is cut at the deadline rather than never started.
    # $Run: param($RegisteredTimeoutSeconds, $WithOpenTurn) returns @{ ElapsedMs; Status; FailsafeCount; Launches } (Launches: server
    # calls the hook started; triage-report-124a374c: each prompt attempts its server call within the budget).
    function Assert-HookWithinBudget {
        param([Parameter(Mandatory)][scriptblock]$Run)
        foreach ($withOpenTurn in $false, $true) {
            $result = & $Run 10 $withOpenTurn
            $result.ElapsedMs | Should -BeLessThan 10000 -Because "open turn: $withOpenTurn"
            $result.Status | Should -Be 'turn-opened-degraded' -Because "open turn: $withOpenTurn"
            $result.FailsafeCount | Should -BeGreaterOrEqual 1 -Because "open turn: $withOpenTurn"
            $result.Launches | Should -BeGreaterOrEqual 1 -Because "open turn: $withOpenTurn"
        }
    }
}

Describe 'D1 hook budget mock gate' {
    BeforeAll {
        $script:RefDeadline = {
            param($HooksJsonPath, $HookName, $ProcessStartUtc)
            if (-not (Test-Path -LiteralPath $HooksJsonPath -PathType Leaf)) { return $null }
            $doc = [System.IO.File]::ReadAllText($HooksJsonPath) | ConvertFrom-Json -Depth 20
            foreach ($hookEvent in $doc.hooks.PSObject.Properties) {
                foreach ($group in @($hookEvent.Value)) {
                    foreach ($hook in @($group.hooks)) {
                        if ([string]$hook.command -match ('[\\/]hooks[\\/]scripts[\\/]' + [regex]::Escape($HookName) + '\.ps1') -and $hook.timeout) {
                            return ([DateTimeOffset]$ProcessStartUtc).ToUnixTimeMilliseconds() + ([long]$hook.timeout * 1000) - 3000
                        }
                    }
                }
            }
            return $null
        }
        $script:RefCap = {
            param($TimeoutSeconds, $RemainingMs)
            if ($null -eq $RemainingMs) { return [int]$TimeoutSeconds }
            if ($RemainingMs -lt 1000) { return 0 }
            return [int][Math]::Min([double]$TimeoutSeconds, [Math]::Floor([double]$RemainingMs / 1000))
        }
        $script:RefDrain = {
            param($QueueDir, $DeadlineUtcMs)
            $calls = 0
            foreach ($record in @(Get-ChildItem -LiteralPath $QueueDir -Filter '*.yaml' -File | Sort-Object Name)) {
                if (($DeadlineUtcMs - [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()) -lt 1000) { break }
                $calls++
                Remove-Item -LiteralPath $record.FullName
            }
            return $calls
        }
        $script:RefOrder = {
            param($Scenario)
            if ($Scenario -eq 'single-call') { return @('workflow.requirements.getFr|', 'client.SessionLog.SubmitAsync|req-20261014T000000Z-queued') }
            return @('client.SessionLog.SubmitAsync|req-20261014T000001Z-old', 'client.SessionLog.SubmitAsync|req-20261014T000002Z-new', 'client.SessionLog.SubmitAsync|req-20261014T000000Z-queued')
        }
        $script:RefSupersede = {
            param($LocalTitle, $QueryText)
            $firstLine = @(($QueryText -replace "`r", '') -split "`n")[0].Trim()
            $raw = [string]::IsNullOrWhiteSpace($LocalTitle) -or $LocalTitle.Trim() -in @('User prompt', $firstLine)
            return @{ QueryCalls = 0; SentTitle = $(if ($raw) { $null } else { $LocalTitle }) }
        }
    }

    It 'DeadlineAssertion_AcceptsMarginAndRejectsNoMargin' {
        Assert-HookDeadline -Compute $script:RefDeadline
        $noMargin = { param($h, $n, $s) $v = & $script:RefDeadline $h $n $s; if ($null -ne $v) { $v + 3000 } }
        # Negative control: the two-second margin first chosen, which a loaded supersede-first hook overran (6167 ms of 6000).
        $twoSecondMargin = { param($h, $n, $s) $v = & $script:RefDeadline $h $n $s; if ($null -ne $v) { $v + 1000 } }
        { Assert-HookDeadline -Compute $twoSecondMargin } | Should -Throw
        { Assert-HookDeadline -Compute $noMargin } | Should -Throw
    }

    It 'TimeoutCapAssertion_AcceptsCapAndRejectsUncapped' {
        Assert-TimeoutCap -Cap $script:RefCap
        # Negative control: returns the per-method timeout and ignores the time left (second positional argument).
        $uncapped = { [int]$args[0] }
        { Assert-TimeoutCap -Cap $uncapped } | Should -Throw
        # Negative control: a one-second floor starts a call after the deadline, which can carry a hook past its budget.
        { Assert-TimeoutCap -Cap { param($t, $r) if ($null -eq $r) { [int]$t } else { [int][Math]::Max(1, [Math]::Min([double]$t, [Math]::Floor([double]$r / 1000))) } } } | Should -Throw
    }

    It 'DrainDeadlineAssertion_AcceptsStopAndRejectsDrainPastDeadline' {
        Assert-DrainStopsAtDeadline -Drain $script:RefDrain
        # Negative control: replays every record and ignores the deadline (second positional argument).
        $ignoresDeadline = {
            $calls = 0
            foreach ($record in @(Get-ChildItem -LiteralPath $args[0] -Filter '*.yaml' -File | Sort-Object Name)) { $calls++; Remove-Item -LiteralPath $record.FullName }
            return $calls
        }
        { Assert-DrainStopsAtDeadline -Drain $ignoresDeadline } | Should -Throw
    }

    It 'ReplayOrderAssertion_AcceptsDrainLastAndRejectsDrainBetweenCalls' {
        Assert-ReplayOrder -Run $script:RefOrder
        $drainBetween = {
            param($Scenario)
            if ($Scenario -eq 'single-call') { return (& $script:RefOrder $Scenario) }
            return @('client.SessionLog.SubmitAsync|req-20261014T000001Z-old', 'client.SessionLog.SubmitAsync|req-20261014T000000Z-queued', 'client.SessionLog.SubmitAsync|req-20261014T000002Z-new')
        }
        { Assert-ReplayOrder -Run $drainBetween } | Should -Throw
        $neverDrainsOneCall = {
            param($Scenario)
            if ($Scenario -eq 'single-call') { return @('workflow.requirements.getFr|') }
            return (& $script:RefOrder $Scenario)
        }
        { Assert-ReplayOrder -Run $neverDrainsOneCall } | Should -Throw
    }

    It 'SupersedeTitleAssertion_AcceptsLocalOnlyAndRejectsServerRead' {
        Assert-SupersedeTitle -Supersede $script:RefSupersede
        $serverRead = {
            param($LocalTitle, $QueryText)
            $local = & $script:RefSupersede $LocalTitle $QueryText
            if ($null -eq $local.SentTitle) { return @{ QueryCalls = 1; SentTitle = 'Server refined title' } }
            return $local
        }
        { Assert-SupersedeTitle -Supersede $serverRead } | Should -Throw
        $sendsRaw = { param($LocalTitle, $QueryText) @{ QueryCalls = 0; SentTitle = $(if ($LocalTitle) { $LocalTitle } else { $QueryText }) } }
        { Assert-SupersedeTitle -Supersede $sendsRaw } | Should -Throw
    }

    It 'BudgetAssertion_AcceptsDegradedWithinBudgetAndRejectsOverrun' {
        # Scripted run results: the registered timeout is fixed by the assertion, so these stand-ins return a result for any value.
        Assert-HookWithinBudget -Run { @{ ElapsedMs = 8600; Status = 'turn-opened-degraded'; FailsafeCount = 1; Launches = 1 } }
        { Assert-HookWithinBudget -Run { @{ ElapsedMs = 10100; Status = ''; FailsafeCount = 0; Launches = 1 } } } | Should -Throw
        { Assert-HookWithinBudget -Run { @{ ElapsedMs = 8600; Status = 'turn-open-failed'; FailsafeCount = 1; Launches = 1 } } } | Should -Throw
        # Negative control: within budget with no open turn, but past it when a supersede comes first.
        $overrunsWithOpenTurn = { if ($args[1]) { @{ ElapsedMs = 10167; Status = 'turn-opened-degraded'; FailsafeCount = 2; Launches = 2 } } else { @{ ElapsedMs = 8600; Status = 'turn-opened-degraded'; FailsafeCount = 1; Launches = 1 } } }
        { Assert-HookWithinBudget -Run $overrunsWithOpenTurn } | Should -Throw
        # Negative control: stays within budget only by never attempting the server call.
        { Assert-HookWithinBudget -Run { @{ ElapsedMs = 2100; Status = 'turn-opened-degraded'; FailsafeCount = 1; Launches = 0 } } } | Should -Throw
    }
}

Describe 'D1 hook budget binding' {
    BeforeAll {
        $script:RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../../../..')).ProviderPath
        $script:LibRoot = Join-Path $script:RepoRoot 'plugins/core/lib-ps'
        $script:ReplScript = Join-Path $script:LibRoot 'repl-invoke.ps1'
        $script:HookScript = Join-Path $script:LibRoot 'plugin-hook.ps1'
        . $script:ReplScript

        # plugin-hook.ps1 runs a hook when invoked, so only its deadline functions are loaded, from the parsed script.
        $hookAst = [System.Management.Automation.Language.Parser]::ParseFile($script:HookScript, [ref]$null, [ref]$null)
        foreach ($name in 'Get-PluginHookDeadline', 'Register-PluginHookDeadline') {
            $definition = $hookAst.Find({ param($node) $node -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq $name }, $true)
            if (-not $definition) { throw "plugin-hook.ps1 defines no function $name" }
            . ([scriptblock]::Create($definition.Extent.Text))
        }

        # A pwsh stand-in for qbrain-ai-repl that never contacts a server: it logs each launch as 'method|turn requestId' to
        # D1_REPL_LOG and answers a success envelope echoing the submitted ids, or sleeps 60 s when D1_REPL_MODE is hang.
        # repl-invoke.ps1 writes the request as UTF-8 with a byte order mark; Windows decodes console input with the OEM code page
        # unless told otherwise, so the stand-in reads UTF-8, as the SessionLogP2Contracts.Tests.ps1 stand-in does.
        $script:FakeDir = Initialize-D1TempDir -Prefix 'd1-fake-repl-'
        $fakePs1 = Join-Path $script:FakeDir 'qbrain-ai-repl-fake.ps1'
        $fakeBody = @(
            '#!/usr/bin/env -S pwsh -NoLogo -NoProfile -NonInteractive -File'
            '[Console]::InputEncoding = [System.Text.UTF8Encoding]::new($false); [Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)'
            '$stdin = [Console]::In.ReadToEnd()'
            'if ($stdin.Length -gt 0 -and [int][char]$stdin[0] -eq 0xFEFF) { $stdin = $stdin.Substring(1) }'
            '$request = $stdin | ConvertFrom-Json -Depth 64'
            '$method = [string]$request.payload.method'
            '$sessionId = ''''; $turnRequestId = '''''
            'if ($method -eq ''client.SessionLog.SubmitAsync'') { $log = $request.payload.params.sessionLog; $sessionId = [string]$log.sessionId; $turnRequestId = [string]@($log.turns)[0].requestId }'
            'if ($env:D1_REPL_LOG) { [System.IO.File]::AppendAllText($env:D1_REPL_LOG, "$method|$turnRequestId" + [Environment]::NewLine) }'
            'if ($env:D1_REPL_MODE -eq ''hang'') { Start-Sleep -Seconds 60; exit 1 }'
            '$lines = @(''type: result'', ''payload:'', ''  result:'')'
            'if ($method -eq ''client.SessionLog.SubmitAsync'') { $lines += @("    sessionId: $sessionId", "    requestId: $turnRequestId", ''    persisted: true'', ''    degraded: false'') }'
            'else { $lines += @(''    id: FR-MCP-170'', ''    title: D1 fixture'') }'
            '[Console]::Out.Write(($lines -join [Environment]::NewLine) + [Environment]::NewLine)'
            'exit 0'
        ) -join [Environment]::NewLine
        [System.IO.File]::WriteAllText($fakePs1, $fakeBody, [System.Text.UTF8Encoding]::new($false))
        if ($IsWindows) {
            # Windows cannot start a .ps1 as a process; a one-line launcher hands the call to pwsh, as SessionLogP2Contracts.Tests.ps1 does.
            $script:FakeRepl = Join-Path $script:FakeDir 'qbrain-ai-repl-fake.cmd'
            $pwshExe = (Get-Command pwsh -ErrorAction Stop).Source
            [System.IO.File]::WriteAllText($script:FakeRepl, ('@echo off' + "`r`n" + '"' + $pwshExe + '" -NoLogo -NoProfile -NonInteractive -File "%~dp0qbrain-ai-repl-fake.ps1" %*' + "`r`n" + 'exit /b %ERRORLEVEL%' + "`r`n"))
        } else {
            $script:FakeRepl = $fakePs1
            [System.IO.File]::SetUnixFileMode($fakePs1, [System.IO.UnixFileMode]'UserRead, UserWrite, UserExecute')
        }

        # A workspace with a marker, a verified session cache, an empty failsafe queue and a plugin root, all temporary.
        function Initialize-D1Layout {
            param([string]$CurrentTurnRequestId = '')
            $root = Initialize-D1TempDir -Prefix 'd1-layout-'
            $layout = [pscustomobject]@{
                Root = $root
                Workspace = Join-Path $root 'ws'
                Cache = Join-Path $root 'cache'
                Failsafe = Join-Path $root 'failsafe'
                PluginRoot = Join-Path $root 'plugin'
                Log = Join-Path $root 'repl.log'
                SessionId = 'ClaudeCode-20261014T000000Z-d1'
            }
            foreach ($dir in $layout.Workspace, $layout.Cache, $layout.Failsafe, $layout.PluginRoot) { [void][System.IO.Directory]::CreateDirectory($dir) }
            [System.IO.File]::WriteAllText((Join-Path $layout.Workspace 'AGENTS-README-FIRST.yaml'), "workspacePath: $($layout.Workspace)`napiKey: d1-fixture`n")
            $snapshot = Get-MarkerFileSnapshot -StartDir $layout.Workspace
            Write-McpYamlObject -Path (Join-Path $layout.Cache 'session-state.yaml') -Document ([ordered]@{
                    status = 'verified'
                    sessionId = $layout.SessionId
                    agent = 'ClaudeCode'
                    title = 'D1 fixture session'
                    markerFilePath = $snapshot.markerFilePath
                    markerLastWriteUtc = $snapshot.markerLastWriteUtc
                })
            if ($CurrentTurnRequestId) {
                Write-McpYamlObject -Path (Join-Path $layout.Cache 'current-turn.yaml') -Document ([ordered]@{
                        turnRequestId = $CurrentTurnRequestId
                        sessionId = $layout.SessionId
                        status = 'in_progress'
                        queryTitle = 'Refined: the old turn'
                        queryText = 'old prompt'
                        openedAt = '2026-10-14T00:00:01Z'
                        planFile = 'None'
                        todoId = 'None'
                        markerFilePath = $snapshot.markerFilePath
                        markerLastWriteUtc = $snapshot.markerLastWriteUtc
                    })
            }
            return $layout
        }

        function Invoke-D1Process {
            param([Parameter(Mandatory)]$Layout, [Parameter(Mandatory)][string[]]$Arguments, [string]$StdIn = '', [string]$Mode = '', [int]$WaitMs = 60000)
            $psi = [System.Diagnostics.ProcessStartInfo]::new((Get-Command pwsh -ErrorAction Stop).Source)
            foreach ($argument in @('-NoLogo', '-NoProfile', '-NonInteractive', '-File') + $Arguments) { $psi.ArgumentList.Add($argument) }
            $psi.WorkingDirectory = $Layout.Workspace
            $psi.UseShellExecute = $false
            $psi.RedirectStandardInput = $true
            $psi.RedirectStandardOutput = $true
            $psi.RedirectStandardError = $true
            foreach ($inherited in 'MCP_FAILSAFE_DRAIN_DISABLED', 'MCP_HOOK_DEADLINE_UTC_MS', 'MCP_PLUGIN_REPL_LOG', 'MCP_PLUGIN_PERSIST_LOG', 'MCP_FAILSAFE_DIR') { [void]$psi.Environment.Remove($inherited) }
            $psi.Environment['MCP_CACHE_DIR_OVERRIDE'] = $Layout.Cache
            $psi.Environment['MCPSERVER_FAILSAFE_DIR'] = $Layout.Failsafe
            $psi.Environment['MCP_WORKSPACE_PATH'] = $Layout.Workspace
            $psi.Environment['MCPSERVER_WORKSPACE_PATH'] = $Layout.Workspace
            $psi.Environment['MCP_PLUGIN_ROOT'] = $Layout.PluginRoot
            $psi.Environment['MCP_PLUGIN_HOST'] = 'claude-code'
            $psi.Environment['MCP_AGENT_NAME'] = 'ClaudeCode'
            $psi.Environment['MCP_REPL_EXECUTABLE'] = $script:FakeRepl
            $psi.Environment['D1_REPL_LOG'] = $Layout.Log
            $psi.Environment['D1_REPL_MODE'] = $Mode
            $watch = [System.Diagnostics.Stopwatch]::StartNew()
            $process = [System.Diagnostics.Process]::Start($psi)
            $process.StandardInput.Write($StdIn)
            $process.StandardInput.Close()
            $stdout = $process.StandardOutput.ReadToEndAsync()
            $stderr = $process.StandardError.ReadToEndAsync()
            $exited = $process.WaitForExit($WaitMs)
            $watch.Stop()
            if (-not $exited) { try { $process.Kill($true) } catch { $null = $_ } }
            [pscustomobject]@{
                Exited = $exited
                ExitCode = $(if ($exited) { $process.ExitCode } else { $null })
                ElapsedMs = $(if ($exited) { $watch.Elapsed.TotalMilliseconds } else { [double]::MaxValue })
                Stdout = $(if ($exited) { $stdout.Result } else { '' })
                Stderr = $(if ($exited) { $stderr.Result } else { '' })
            }
        }

        function Read-D1Log {
            param([Parameter(Mandatory)]$Layout)
            if (-not (Test-Path -LiteralPath $Layout.Log)) { return @() }
            return @([System.IO.File]::ReadAllLines($Layout.Log) | Where-Object { $_ })
        }
    }

    AfterAll {
        Remove-Item -LiteralPath $script:FakeDir -Recurse -Force -ErrorAction SilentlyContinue
    }

    It 'HookDeadline_RegisteredTimeoutLessMargin_FromProcessStart' {
        Assert-HookDeadline -Compute { param($h, $n, $s) Get-PluginHookDeadline -HooksJsonPath $h -HookName $n -ProcessStartUtc $s }
        $root = Initialize-D1TempDir -Prefix 'd1-set-deadline-'
        $priorRoot = $env:MCP_PLUGIN_ROOT
        $priorDeadline = $env:MCP_HOOK_DEADLINE_UTC_MS
        try {
            [void](Write-D1HooksJson -PluginRoot $root -Timeouts ([ordered]@{ 'user-prompt-submit' = 10 }))
            $env:MCP_PLUGIN_ROOT = $root
            Remove-Item Env:MCP_HOOK_DEADLINE_UTC_MS -ErrorAction SilentlyContinue
            Register-PluginHookDeadline -HookName 'user-prompt-submit'
            $startMs = ([DateTimeOffset][System.Diagnostics.Process]::GetCurrentProcess().StartTime.ToUniversalTime()).ToUnixTimeMilliseconds()
            [long]$env:MCP_HOOK_DEADLINE_UTC_MS | Should -Be ($startMs + 7000)
        } finally {
            if ($null -eq $priorRoot) { Remove-Item Env:MCP_PLUGIN_ROOT -ErrorAction SilentlyContinue } else { $env:MCP_PLUGIN_ROOT = $priorRoot }
            if ($null -eq $priorDeadline) { Remove-Item Env:MCP_HOOK_DEADLINE_UTC_MS -ErrorAction SilentlyContinue } else { $env:MCP_HOOK_DEADLINE_UTC_MS = $priorDeadline }
            Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'HookDeadline_NoRegisteredTimeout_NoDeadline' {
        $root = Initialize-D1TempDir -Prefix 'd1-no-deadline-'
        $priorRoot = $env:MCP_PLUGIN_ROOT
        $priorDeadline = $env:MCP_HOOK_DEADLINE_UTC_MS
        try {
            [void](Write-D1HooksJson -PluginRoot $root -Timeouts ([ordered]@{ 'user-prompt-submit' = 10 }))
            $env:MCP_PLUGIN_ROOT = $root
            Remove-Item Env:MCP_HOOK_DEADLINE_UTC_MS -ErrorAction SilentlyContinue
            Register-PluginHookDeadline -HookName 'health-check'
            $env:MCP_HOOK_DEADLINE_UTC_MS | Should -BeNullOrEmpty
            $env:MCP_PLUGIN_ROOT = Join-Path $root 'no-plugin-here'
            Register-PluginHookDeadline -HookName 'user-prompt-submit'
            $env:MCP_HOOK_DEADLINE_UTC_MS | Should -BeNullOrEmpty
        } finally {
            if ($null -eq $priorRoot) { Remove-Item Env:MCP_PLUGIN_ROOT -ErrorAction SilentlyContinue } else { $env:MCP_PLUGIN_ROOT = $priorRoot }
            if ($null -eq $priorDeadline) { Remove-Item Env:MCP_HOOK_DEADLINE_UTC_MS -ErrorAction SilentlyContinue } else { $env:MCP_HOOK_DEADLINE_UTC_MS = $priorDeadline }
            Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'ReplTimeout_InsideHook_CappedByTimeLeft' {
        Assert-TimeoutCap -Cap { param($t, $r) Get-ReplCappedTimeout -TimeoutSeconds $t -RemainingMs $r }
        $priorDeadline = $env:MCP_HOOK_DEADLINE_UTC_MS
        try {
            $env:MCP_HOOK_DEADLINE_UTC_MS = [string]([DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds() + 7900)
            Get-ReplMethodTimeoutSeconds -Method 'workflow.sessionlog.beginTurn' | Should -Be 7
            Get-ReplMethodTimeoutSeconds -Method 'workflow.requirements.generateDocument' | Should -Be 7
        } finally {
            if ($null -eq $priorDeadline) { Remove-Item Env:MCP_HOOK_DEADLINE_UTC_MS -ErrorAction SilentlyContinue } else { $env:MCP_HOOK_DEADLINE_UTC_MS = $priorDeadline }
        }
    }

    It 'ReplTimeout_OutsideHook_Unchanged' {
        $priorDeadline = $env:MCP_HOOK_DEADLINE_UTC_MS
        $priorTimeout = $env:REPL_TIMEOUT
        $priorLong = $env:REPL_LONG_TIMEOUT
        $priorDrain = $env:REPL_FAILSAFE_DRAIN_TIMEOUT
        try {
            Remove-Item Env:MCP_HOOK_DEADLINE_UTC_MS, Env:REPL_TIMEOUT, Env:REPL_LONG_TIMEOUT, Env:REPL_FAILSAFE_DRAIN_TIMEOUT -ErrorAction SilentlyContinue
            Get-ReplMethodTimeoutSeconds -Method 'workflow.sessionlog.beginTurn' | Should -Be 30
            Get-ReplMethodTimeoutSeconds -Method 'workflow.requirements.generateDocument' | Should -Be 300
            $script:ReplFailsafeDraining = $true
            Get-ReplMethodTimeoutSeconds -Method 'client.SessionLog.SubmitAsync' | Should -Be 120
        } finally {
            $script:ReplFailsafeDraining = $false
            if ($null -eq $priorDeadline) { Remove-Item Env:MCP_HOOK_DEADLINE_UTC_MS -ErrorAction SilentlyContinue } else { $env:MCP_HOOK_DEADLINE_UTC_MS = $priorDeadline }
            if ($null -ne $priorTimeout) { $env:REPL_TIMEOUT = $priorTimeout }
            if ($null -ne $priorLong) { $env:REPL_LONG_TIMEOUT = $priorLong }
            if ($null -ne $priorDrain) { $env:REPL_FAILSAFE_DRAIN_TIMEOUT = $priorDrain }
        }
    }

    It 'Drain_InsideHookPastDeadline_LeavesRecordsQueuedUnattempted' {
        $priorDeadline = $env:MCP_HOOK_DEADLINE_UTC_MS
        $priorFailsafe = $env:MCPSERVER_FAILSAFE_DIR
        try {
            Assert-DrainStopsAtDeadline -Drain {
                param($QueueDir, $DeadlineUtcMs)
                $env:MCPSERVER_FAILSAFE_DIR = $QueueDir
                $env:MCP_HOOK_DEADLINE_UTC_MS = [string]$DeadlineUtcMs
                $script:ReplFailsafeDraining = $false
                $script:D1ReplayCalls = 0
                # Every replay succeeds; the drain passes -Method and -ParamsYaml, which this counter does not need.
                function Invoke-ReplRaw {
                    $script:D1ReplayCalls++
                    return New-McpPluginReplResult -Success $true -Output 'type: result' -ExitCode 0
                }
                [void](Invoke-ReplFailsafeDrain)
                return $script:D1ReplayCalls
            }
        } finally {
            if ($null -eq $priorDeadline) { Remove-Item Env:MCP_HOOK_DEADLINE_UTC_MS -ErrorAction SilentlyContinue } else { $env:MCP_HOOK_DEADLINE_UTC_MS = $priorDeadline }
            if ($null -eq $priorFailsafe) { Remove-Item Env:MCPSERVER_FAILSAFE_DIR -ErrorAction SilentlyContinue } else { $env:MCPSERVER_FAILSAFE_DIR = $priorFailsafe }
        }
    }

    It 'Drain_SingleCallInvocation_RunsAfterMethodOutput' {
        Assert-ReplayOrder -Scenario 'single-call' -Run {
            $layout = Initialize-D1Layout
            try {
                [void](Write-D1Queue -Dir $layout.Failsafe -Count 1 -SessionId $layout.SessionId -FirstRequestId 'req-20261014T000000Z-queued')
                $run = Invoke-D1Process -Layout $layout -Arguments @($script:ReplScript, '-Method', 'workflow.requirements.getFr', '-ParamsYaml', 'id: FR-MCP-170')
                $run.Exited | Should -BeTrue
                $run.Stdout | Should -Match 'FR-MCP-170'
                return (Read-D1Log -Layout $layout)
            } finally {
                Remove-Item -LiteralPath $layout.Root -Recurse -Force -ErrorAction SilentlyContinue
            }
        }
    }

    It 'Drain_BeginTurn_ReplaysQueueOnlyAfterTurnPersists' {
        Assert-ReplayOrder -Scenario 'begin-turn' -Run {
            $layout = Initialize-D1Layout -CurrentTurnRequestId 'req-20261014T000001Z-old'
            try {
                [void](Write-D1Queue -Dir $layout.Failsafe -Count 1 -SessionId $layout.SessionId -FirstRequestId 'req-20261014T000000Z-queued')
                $params = "requestId: req-20261014T000002Z-new`nqueryTitle: new prompt`nqueryText: new prompt"
                $run = Invoke-D1Process -Layout $layout -Arguments @($script:ReplScript, '-Method', 'workflow.sessionlog.beginTurn', '-ParamsYaml', $params)
                $run.Exited | Should -BeTrue
                $run.ExitCode | Should -Be 0
                return (Read-D1Log -Layout $layout)
            } finally {
                Remove-Item -LiteralPath $layout.Root -Recurse -Force -ErrorAction SilentlyContinue
            }
        }
    }

    It 'SupersedeTitle_LocalCacheOnly_NoServerRead' {
        $priorCache = $env:MCP_CACHE_DIR_OVERRIDE
        $priorFailsafe = $env:MCPSERVER_FAILSAFE_DIR
        $priorWorkspace = $env:MCP_WORKSPACE_PATH
        try {
            Assert-SupersedeTitle -Supersede {
                param($LocalTitle, $QueryText)
                $layout = Initialize-D1Layout -CurrentTurnRequestId 'req-20261014T000001Z-old'
                try {
                    $turnPath = Join-Path $layout.Cache 'current-turn.yaml'
                    $turn = Read-McpYamlObject -Path $turnPath
                    $turn['queryTitle'] = $LocalTitle
                    $turn['queryText'] = $QueryText
                    Write-McpYamlObject -Path $turnPath -Document $turn
                    $env:MCP_CACHE_DIR_OVERRIDE = $layout.Cache
                    $env:MCPSERVER_FAILSAFE_DIR = $layout.Failsafe
                    $env:MCP_WORKSPACE_PATH = $layout.Workspace
                    $script:D1QueryCalls = 0
                    $script:D1SentTitle = $null
                    function Invoke-ReplRaw {
                        param([string]$Method, [string]$ParamsYaml = '')
                        if ($Method -eq 'client.SessionLog.QueryAsync') {
                            $script:D1QueryCalls++
                            return New-McpPluginReplResult -Success $true -Output 'type: result' -ExitCode 0
                        }
                        $submitted = ConvertFrom-Yaml -Yaml $ParamsYaml
                        $sent = @($submitted.sessionLog.turns)[0]
                        $script:D1SentTitle = if ($sent.Contains('queryTitle')) { [string]$sent['queryTitle'] } else { $null }
                        $echo = "type: result`npayload:`n  result:`n    sessionId: $($submitted.sessionLog.sessionId)`n    requestId: $($sent['requestId'])`n    persisted: true`n    degraded: false"
                        return New-McpPluginReplResult -Success $true -Output $echo -ExitCode 0
                    }
                    Invoke-ReplSupersedeCurrentTurnIfInProgress -NextRequestId 'req-20261014T000002Z-new'
                    return @{ QueryCalls = $script:D1QueryCalls; SentTitle = $script:D1SentTitle }
                } finally {
                    Remove-Item -LiteralPath $layout.Root -Recurse -Force -ErrorAction SilentlyContinue
                }
            }
        } finally {
            if ($null -eq $priorCache) { Remove-Item Env:MCP_CACHE_DIR_OVERRIDE -ErrorAction SilentlyContinue } else { $env:MCP_CACHE_DIR_OVERRIDE = $priorCache }
            if ($null -eq $priorFailsafe) { Remove-Item Env:MCPSERVER_FAILSAFE_DIR -ErrorAction SilentlyContinue } else { $env:MCPSERVER_FAILSAFE_DIR = $priorFailsafe }
            if ($null -eq $priorWorkspace) { Remove-Item Env:MCP_WORKSPACE_PATH -ErrorAction SilentlyContinue } else { $env:MCP_WORKSPACE_PATH = $priorWorkspace }
        }
    }

    It 'UserPromptSubmit_UnansweredServer_OpensDegradedTurnWithinBudget' {
        Assert-HookWithinBudget -Run {
            param($RegisteredTimeoutSeconds, $WithOpenTurn)
            $layout = if ($WithOpenTurn) { Initialize-D1Layout -CurrentTurnRequestId 'req-20261014T000001Z-old' } else { Initialize-D1Layout }
            try {
                [void](Write-D1HooksJson -PluginRoot $layout.PluginRoot -Timeouts ([ordered]@{ 'user-prompt-submit' = $RegisteredTimeoutSeconds }))
                $payload = @{ prompt = 'D1 budget probe'; hook_event_name = 'UserPromptSubmit' } | ConvertTo-Json -Compress
                $run = Invoke-D1Process -Layout $layout -Mode 'hang' -StdIn $payload -WaitMs 30000 -Arguments @(
                    $script:HookScript, '-HookName', 'user-prompt-submit', '-HostName', 'claude-code', '-CacheMode', 'scoped', '-WorkspacePath', $layout.Workspace)
                $status = ''
                if ($run.Exited -and $run.Stdout) {
                    try { $status = [string]($run.Stdout | ConvertFrom-Json -Depth 20).hookSpecificOutput.status } catch { $status = "unparsed: $($run.Stdout)" }
                }
                return @{
                    ElapsedMs = $run.ElapsedMs
                    Status = $status
                    FailsafeCount = @(Get-ChildItem -LiteralPath $layout.Failsafe -Filter '*.yaml' -File -Recurse -ErrorAction SilentlyContinue).Count
                    Launches = @(Read-D1Log -Layout $layout).Count
                }
            } finally {
                Remove-Item -LiteralPath $layout.Root -Recurse -Force -ErrorAction SilentlyContinue
            }
        }
    }
}
