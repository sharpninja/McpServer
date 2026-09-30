#Requires -Version 7.0

# TEST-MCP-SESSIONLIFE-001 / TEST-MCP-SESSIONLIFE-002 / FR-MCP-SESSIONLIFE-001..003
# BUG-TRIAGE-246. Drives the real builder, shim, and repl-invoke.ps1 process entry.
# MCP_PLUGIN_PERSIST_LOG stays unset. A local executable stands in for mcpserver-repl;
# durable server query is not claimed from that stand-in.

Describe 'FR-MCP-SESSIONLIFE P2 cache identity metadata and outcomes' {
    BeforeAll {
        $script:RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')).ProviderPath
        $script:ReplScript = Join-Path $script:RepoRoot 'plugins\core\lib-ps\repl-invoke.ps1'
        $script:ResolveScript = Join-Path $script:RepoRoot 'plugins\core\lib-ps\resolve-cache-dir.ps1'
        $script:Work = Join-Path ([System.IO.Path]::GetTempPath()) ('sessionlife-p2-' + [guid]::NewGuid().ToString('N'))
        [void][System.IO.Directory]::CreateDirectory($script:Work)
        if ($IsWindows) {
            $script:FakeRepl = Join-Path $script:Work 'fake-repl.cmd'
            $fakePs1 = Join-Path $script:Work 'fake-repl.ps1'
            $fakeBody = @(
                '$stdin = [Console]::In.ReadToEnd()'
                'if ($env:P2_REPL_LOG) { Add-Content -LiteralPath $env:P2_REPL_LOG -Value $stdin -Encoding utf8 }'
                '$mode = $env:P2_REPL_MODE'
                'if ([string]::IsNullOrWhiteSpace($mode)) { $mode = ''primary'' }'
                'if ($mode -eq ''primary'') {'
                '    if ($env:P2_SERVER_STATE) { Add-Content -LiteralPath $env:P2_SERVER_STATE -Value $stdin -Encoding utf8 }'
                '    @("type: result","payload:","  result:","    persisted: true","    degraded: false") -join [Environment]::NewLine | Write-Output'
                '    exit 0'
                '}'
                '@("type: error","payload:","  code: backend_unavailable","  message: HTTP 503","  retryable: true") -join [Environment]::NewLine | Write-Output'
                'exit 1'
            ) -join [Environment]::NewLine
            Set-Content -LiteralPath $fakePs1 -Value $fakeBody -Encoding utf8
            $pwshExe = (Get-Command pwsh -ErrorAction Stop).Source
            $cmdBody = '@echo off' + [Environment]::NewLine + '"' + $pwshExe + '" -NoLogo -NoProfile -NonInteractive -File "%~dp0fake-repl.ps1"' + [Environment]::NewLine + 'exit /b %ERRORLEVEL%' + [Environment]::NewLine
            [System.IO.File]::WriteAllText($script:FakeRepl, $cmdBody)
        }
        else {
            $script:FakeRepl = Join-Path $script:Work 'fake-repl.sh'
            $bashBody = @(
                '#!/bin/bash'
                'stdin=$(cat)'
                'if [ -n "${P2_REPL_LOG:-}" ]; then'
                '  printf ''%s\n'' "$stdin" >> "$P2_REPL_LOG"'
                'fi'
                'if [ "${P2_REPL_MODE:-primary}" = "primary" ]; then'
                '  if [ -n "${P2_SERVER_STATE:-}" ]; then'
                '    printf ''%s\n'' "$stdin" >> "$P2_SERVER_STATE"'
                '  fi'
                '  printf ''type: result\npayload:\n  result:\n    persisted: true\n    degraded: false\n'''
                '  exit 0'
                'fi'
                'printf ''type: error\npayload:\n  code: backend_unavailable\n  message: HTTP 503\n  retryable: true\n'''
                'exit 1'
            ) -join [Environment]::NewLine
            Set-Content -LiteralPath $script:FakeRepl -Value $bashBody -Encoding ascii
            & chmod +x -- $script:FakeRepl
        }
        . $script:ReplScript
        if ($env:MCP_PLUGIN_PERSIST_LOG) { Remove-Item Env:MCP_PLUGIN_PERSIST_LOG }

        function ConvertTo-P2Bool {
            param($Value)
            if ($Value -is [bool]) { return [bool]$Value }
            return ([string]$Value) -match '^(?i:true)$'
        }

        function New-P2Layout {
            param([Parameter(Mandatory)][string]$Name)
            $root = Join-Path $script:Work $Name
            $workspace = Join-Path $root 'ws'
            $cache = Join-Path $root 'cache'
            $failsafe = Join-Path $root 'failsafe'
            [void][System.IO.Directory]::CreateDirectory($workspace)
            [void][System.IO.Directory]::CreateDirectory($cache)
            [void][System.IO.Directory]::CreateDirectory($failsafe)
            $marker = Join-Path $workspace 'AGENTS-README-FIRST.yaml'
            [System.IO.File]::WriteAllText($marker, "workspacePath: $workspace`napiKey: p2-test`n")
            $snapshot = Get-MarkerFileSnapshot -StartDir $workspace
            $sessionId = 'Codex-20260930T000000Z-p2'
            Write-McpYamlObject -Path (Join-Path $cache 'session-state.yaml') -Document ([ordered]@{
                status = 'verified'
                sessionId = $sessionId
                agent = 'Codex'
                markerFilePath = $snapshot.markerFilePath
                markerLastWriteUtc = $snapshot.markerLastWriteUtc
            })
            return [pscustomobject]@{
                Root = $root
                Workspace = $workspace
                Cache = $cache
                Failsafe = $failsafe
                SessionId = $sessionId
                RequestId = 'req-p2-verb'
                Snapshot = $snapshot
            }
        }

        function Write-P2Turn {
            param($Layout, [string]$RequestId = 'req-p2-verb', [int]$AuditActions = 2, [string]$OpenedAt = '2026-09-30T00:00:00Z')
            Write-McpYamlObject -Path (Join-Path $Layout.Cache 'current-turn.yaml') -Document ([ordered]@{
                turnRequestId = $RequestId
                sessionId = $Layout.SessionId
                status = 'in_progress'
                queryText = 'keep me'
                queryTitle = 'kept title'
                planFile = 'docs/plans/p2.md'
                todoId = 'BUG-TRIAGE-246'
                openedAt = $OpenedAt
                auditActions = $AuditActions
                auditDialog = 1
                markerFilePath = $Layout.Snapshot.markerFilePath
                markerLastWriteUtc = $Layout.Snapshot.markerLastWriteUtc
            })
        }

        function Invoke-P2Verb {
            param(
                $Layout,
                [Parameter(Mandatory)][string]$Method,
                [string]$ParamsYaml = '',
                [ValidateSet('primary', 'queued', 'lost')][string]$Mode = 'primary',
                [switch]$BreakFailsafe
            )
            $log = Join-Path $Layout.Root 'repl-log.txt'
            $state = Join-Path $Layout.Root 'server-state.txt'
            $failsafe = $Layout.Failsafe
            if ($BreakFailsafe) {
                $blocker = Join-Path $Layout.Root 'failsafe-blocker'
                [System.IO.File]::WriteAllText($blocker, 'x')
                $failsafe = Join-Path $blocker 'pending'
            }
            $psi = [System.Diagnostics.ProcessStartInfo]::new()
            $psi.FileName = (Get-Command pwsh -ErrorAction Stop).Source
            foreach ($arg in @('-NoLogo', '-NoProfile', '-NonInteractive', '-File', $script:ReplScript, '-Method', $Method, '-ParamsYaml', $ParamsYaml)) {
                $psi.ArgumentList.Add($arg)
            }
            $psi.WorkingDirectory = $Layout.Workspace
            $psi.UseShellExecute = $false
            $psi.RedirectStandardOutput = $true
            $psi.RedirectStandardError = $true
            $psi.Environment['MCP_CACHE_DIR_OVERRIDE'] = $Layout.Cache
            $psi.Environment['MCPSERVER_FAILSAFE_DIR'] = $failsafe
            $psi.Environment['MCP_WORKSPACE_PATH'] = $Layout.Workspace
            $psi.Environment['MCPSERVER_WORKSPACE_PATH'] = $Layout.Workspace
            $psi.Environment['MCP_PLUGIN_HOST'] = 'codex'
            $psi.Environment['MCP_AGENT_NAME'] = 'Codex'
            $psi.Environment['MCP_REPL_EXECUTABLE'] = $script:FakeRepl
            $psi.Environment['P2_REPL_MODE'] = $Mode
            $psi.Environment['P2_REPL_LOG'] = $log
            $psi.Environment['P2_SERVER_STATE'] = $state
            $psi.Environment['MCP_AGENT_EXECUTABLE_VERSION'] = 'codex-test-1'
            $proc = [System.Diagnostics.Process]::Start($psi)
            $stdout = $proc.StandardOutput.ReadToEndAsync()
            $stderr = $proc.StandardError.ReadToEndAsync()
            if (-not $proc.WaitForExit(60000)) {
                try { $proc.Kill($true) } catch { }
                throw "timed out waiting for $Method $Mode"
            }
            $receiptPath = Join-Path $Layout.Cache 'session-verb-outcome.yaml'
            $receipt = $null
            if (Test-Path -LiteralPath $receiptPath) {
                $receipt = Read-McpYamlObject -Path $receiptPath
            }
            [pscustomobject]@{
                ExitCode = $proc.ExitCode
                Stdout = $stdout.Result
                Stderr = $stderr.Result
                Receipt = $receipt
                Failsafe = $failsafe
                ServerState = if (Test-Path -LiteralPath $state) { [System.IO.File]::ReadAllText($state) } else { '' }
                ReplLog = if (Test-Path -LiteralPath $log) { [System.IO.File]::ReadAllText($log) } else { '' }
            }
        }

        function Assert-P2Receipt {
            param($Receipt, [string]$Method, [string]$RequestId, [string]$Code)
            $Receipt | Should -Not -BeNullOrEmpty
            foreach ($field in @('code', 'retryable', 'persisted', 'degraded', 'queued', 'method', 'requestId', 'failsafePath', 'message', 'childStderr')) {
                $has = $false
                if ($Receipt -is [System.Collections.IDictionary]) { $has = $Receipt.Contains($field) }
                else { $has = $null -ne $Receipt.PSObject.Properties[$field] }
                $has | Should -BeTrue -Because $field
            }
            [string](Get-ReplObjectValue -InputObject $Receipt -Name 'code') | Should -Be $Code
            [string](Get-ReplObjectValue -InputObject $Receipt -Name 'method') | Should -Be $Method
            [string](Get-ReplObjectValue -InputObject $Receipt -Name 'requestId') | Should -Be $RequestId
            [string](Get-ReplObjectValue -InputObject $Receipt -Name 'message') | Should -Not -BeNullOrEmpty
        }
    }

    AfterAll {
        if ($script:Work -and (Test-Path -LiteralPath $script:Work)) {
            Remove-Item -LiteralPath $script:Work -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'covers primary queued and lost outcomes for every session verb' {
        $verbs = @(
            @{ Verb = 'workflow.sessionlog.beginTurn'; Yaml = "requestId: req-p2-verb`nqueryTitle: P2 title`nqueryText: P2 query`nplanFile: None`ntodoId: None"; NeedsTurn = $false },
            @{ Verb = 'workflow.sessionlog.updateTurn'; Yaml = "response: kept response`ninterpretation: noted`n"; NeedsTurn = $true },
            @{ Verb = 'workflow.sessionlog.appendActions'; Yaml = "actions:`n  - type: design_decision`n    description: note`n"; NeedsTurn = $true },
            @{ Verb = 'workflow.sessionlog.appendDialog'; Yaml = "dialogItems:`n  - role: model`n    content: hello`n    category: reasoning`n"; NeedsTurn = $true },
            @{ Verb = 'workflow.sessionlog.completeTurn'; Yaml = "response: done`n"; NeedsTurn = $true },
            @{ Verb = 'workflow.sessionlog.failTurn'; Yaml = "errorMessage: boom`n"; NeedsTurn = $true },
            @{ Verb = 'workflow.sessionlog.setTurnTitle'; Yaml = "queryTitle: renamed`n"; NeedsTurn = $true },
            @{ Verb = 'workflow.sessionlog.setSessionTitle'; Yaml = "title: session renamed`n"; NeedsTurn = $false }
        )
        $failures = New-Object System.Collections.Generic.List[string]
        foreach ($verb in $verbs) {
            foreach ($mode in @('primary', 'queued', 'lost')) {
                $name = "$($verb.Verb) $mode"
                try {
                    $layout = New-P2Layout -Name ($name -replace '[^A-Za-z0-9]+', '-')
                    if ($verb.NeedsTurn) { Write-P2Turn -Layout $layout }
                    $result = Invoke-P2Verb -Layout $layout -Method $verb.Verb -ParamsYaml $verb.Yaml -Mode $(if ($mode -eq 'lost') { 'queued' } else { $mode }) -BreakFailsafe:($mode -eq 'lost')
                    $requestId = if ($verb.Verb -eq 'workflow.sessionlog.setSessionTitle') { $layout.SessionId } else { $layout.RequestId }
                    if ($mode -eq 'primary') {
                        $result.ExitCode | Should -Be 0
                        $result.Stdout.Trim() | Should -Be ''
                        Assert-P2Receipt -Receipt $result.Receipt -Method $verb.Verb -RequestId $requestId -Code 'persisted'
                        (ConvertTo-P2Bool (Get-ReplObjectValue -InputObject $result.Receipt -Name 'persisted')) | Should -BeTrue
                        (ConvertTo-P2Bool (Get-ReplObjectValue -InputObject $result.Receipt -Name 'queued')) | Should -BeFalse
                        (ConvertTo-P2Bool (Get-ReplObjectValue -InputObject $result.Receipt -Name 'degraded')) | Should -BeFalse
                        $result.ServerState | Should -Not -BeNullOrEmpty
                        @(Get-ChildItem -LiteralPath $layout.Failsafe -Filter '*.yaml' -File -ErrorAction SilentlyContinue).Count | Should -Be 0
                        if ($verb.Verb -eq 'workflow.sessionlog.updateTurn') {
                            $result.ServerState | Should -Match 'codex-test-1'
                        }
                    } elseif ($mode -eq 'queued') {
                        $result.ExitCode | Should -Be 0
                        Assert-P2Receipt -Receipt $result.Receipt -Method $verb.Verb -RequestId $requestId -Code 'queued'
                        (ConvertTo-P2Bool (Get-ReplObjectValue -InputObject $result.Receipt -Name 'persisted')) | Should -BeFalse
                        (ConvertTo-P2Bool (Get-ReplObjectValue -InputObject $result.Receipt -Name 'degraded')) | Should -BeTrue
                        (ConvertTo-P2Bool (Get-ReplObjectValue -InputObject $result.Receipt -Name 'queued')) | Should -BeTrue
                        (ConvertTo-P2Bool (Get-ReplObjectValue -InputObject $result.Receipt -Name 'retryable')) | Should -BeTrue
                        $result.ServerState | Should -Be ''
                        $kept = @(Get-ChildItem -LiteralPath $layout.Failsafe -Filter '*.yaml' -File -ErrorAction SilentlyContinue)
                        $kept.Count | Should -BeGreaterThan 0
                        [System.IO.File]::ReadAllText($kept[0].FullName).Contains($requestId) | Should -BeTrue
                        if ($verb.Verb -eq 'workflow.sessionlog.completeTurn') {
                            [string](Read-McpYamlObject -Path (Join-Path $layout.Cache 'current-turn.yaml')).status | Should -Be 'in_progress'
                        }
                        if ($verb.Verb -eq 'workflow.sessionlog.failTurn') {
                            Test-Path -LiteralPath (Join-Path $layout.Cache 'current-turn.yaml') | Should -BeTrue
                        }
                    } else {
                        $result.ExitCode | Should -Not -Be 0
                        Assert-P2Receipt -Receipt $result.Receipt -Method $verb.Verb -RequestId $requestId -Code 'lost'
                        (ConvertTo-P2Bool (Get-ReplObjectValue -InputObject $result.Receipt -Name 'persisted')) | Should -BeFalse
                        (ConvertTo-P2Bool (Get-ReplObjectValue -InputObject $result.Receipt -Name 'queued')) | Should -BeFalse
                        [string](Get-ReplObjectValue -InputObject $result.Receipt -Name 'childStderr') | Should -Not -BeNullOrEmpty
                        $result.ServerState | Should -Be ''
                        if (Test-Path -LiteralPath $layout.Failsafe) {
                            @(Get-ChildItem -LiteralPath $layout.Failsafe -Filter '*.yaml' -File -ErrorAction SilentlyContinue).Count | Should -Be 0
                        }
                    }
                } catch {
                    $failures.Add("$name :: $($_.Exception.Message)")
                }
            }
        }
        if ($failures.Count -gt 0) {
            throw ($failures -join [Environment]::NewLine)
        }
    }

    It 'rejects an absent current-turn cache without a server write' {
        $layout = New-P2Layout -Name 'absent-cache'
        $result = Invoke-P2Verb -Layout $layout -Method 'workflow.sessionlog.appendActions' -ParamsYaml "actions:`n  - type: design_decision`n    description: note`n" -Mode 'primary'
        $result.ExitCode | Should -Not -Be 0
        Assert-P2Receipt -Receipt $result.Receipt -Method 'workflow.sessionlog.appendActions' -RequestId '' -Code 'lost'
        $result.ServerState | Should -Be ''
        @(Get-ChildItem -LiteralPath $layout.Failsafe -Filter '*.yaml' -File -ErrorAction SilentlyContinue).Count | Should -Be 0
    }

    It 'preserves degraded creation metadata and sends cached plan on the same request' {
        $layout = New-P2Layout -Name 'degraded-retry'
        $first = Invoke-P2Verb -Layout $layout -Method 'workflow.sessionlog.beginTurn' -ParamsYaml "requestId: req-p2-verb`nqueryTitle: kept title`nqueryText: keep me`nplanFile: docs/plans/p2.md`ntodoId: BUG-TRIAGE-246`n" -Mode 'queued'
        $first.ExitCode | Should -Be 0
        $turnPath = Join-Path $layout.Cache 'current-turn.yaml'
        $turn = Read-McpYamlObject -Path $turnPath
        $turn['auditActions'] = 4
        $turn['openedAt'] = '2026-09-30T00:11:00Z'
        Write-McpYamlObject -Path $turnPath -Document $turn
        Remove-Item -LiteralPath (Join-Path $layout.Root 'server-state.txt') -ErrorAction SilentlyContinue
        Remove-Item -LiteralPath (Join-Path $layout.Root 'repl-log.txt') -ErrorAction SilentlyContinue
        Get-ChildItem -LiteralPath $layout.Failsafe -Filter '*.yaml' -File -ErrorAction SilentlyContinue | Remove-Item -Force
        $second = Invoke-P2Verb -Layout $layout -Method 'workflow.sessionlog.beginTurn' -ParamsYaml "requestId: req-p2-verb`nqueryTitle: kept title`nqueryText: keep me`n" -Mode 'primary'
        $second.ExitCode | Should -Be 0
        $kept = Read-McpYamlObject -Path $turnPath
        [string]$kept.openedAt | Should -Be '2026-09-30T00:11:00Z'
        [string]$kept.auditActions | Should -Be '4'
        [string]$kept.queryText | Should -Be 'keep me'
        [string]$kept.planFile | Should -Be 'docs/plans/p2.md'
        [string]$kept.todoId | Should -Be 'BUG-TRIAGE-246'
        $second.ServerState | Should -Match 'docs/plans/p2.md'
        $second.ServerState | Should -Match 'BUG-TRIAGE-246'
    }

    It 'omits metadata on durable reopen unless the caller sends an explicit value' {
        $layout = New-P2Layout -Name 'durable-reopen'
        $first = Invoke-P2Verb -Layout $layout -Method 'workflow.sessionlog.beginTurn' -ParamsYaml "requestId: req-p2-verb`nqueryTitle: kept title`nqueryText: keep me`nplanFile: docs/plans/p2.md`ntodoId: BUG-TRIAGE-246`n" -Mode 'primary'
        $first.ExitCode | Should -Be 0
        Remove-Item -LiteralPath (Join-Path $layout.Root 'server-state.txt') -Force
        $second = Invoke-P2Verb -Layout $layout -Method 'workflow.sessionlog.beginTurn' -ParamsYaml "requestId: req-p2-verb`nqueryTitle: kept title`nqueryText: keep me`n" -Mode 'primary'
        $second.ExitCode | Should -Be 0
        $payloads = @($second.ServerState -split "(?<=\})\s*\n" | Where-Object { $_.Trim() })
        $reopen = $payloads[-1] | ConvertFrom-Json
        $turn = $reopen.payload.params.sessionLog.turns[0]
        $turn.PSObject.Properties.Name | Should -Not -Contain 'planFile'
        $turn.PSObject.Properties.Name | Should -Not -Contain 'todoId'
        $cached = Read-McpYamlObject -Path (Join-Path $layout.Cache 'current-turn.yaml')
        [string]$cached.planFile | Should -Be 'docs/plans/p2.md'
        [string]$cached.todoId | Should -Be 'BUG-TRIAGE-246'
    }

    It 'stores canceled with exact None when a new request supersedes an open turn' {
        $layout = New-P2Layout -Name 'supersede'
        Write-P2Turn -Layout $layout -RequestId 'req-p2-old'
        $result = Invoke-P2Verb -Layout $layout -Method 'workflow.sessionlog.beginTurn' -ParamsYaml "requestId: req-p2-new`nqueryTitle: next`nqueryText: next text`nplanFile: None`ntodoId: None`n" -Mode 'primary'
        $result.ExitCode | Should -Be 0
        $result.ServerState | Should -Match 'req-p2-old'
        $result.ServerState | Should -Match 'canceled'
        $result.ServerState | Should -Match 'None'
    }

    It 'rejects whitespace metadata on ordinary first persistence' {
        $layout = New-P2Layout -Name 'whitespace-meta'
        $result = Invoke-P2Verb -Layout $layout -Method 'workflow.sessionlog.beginTurn' -ParamsYaml "requestId: req-p2-verb`nqueryTitle: P2 title`nqueryText: P2 query`nplanFile: `" `"`ntodoId: None`n" -Mode 'primary'
        $result.ExitCode | Should -Not -Be 0
        Assert-P2Receipt -Receipt $result.Receipt -Method 'workflow.sessionlog.beginTurn' -RequestId 'req-p2-verb' -Code 'rejected'
        $result.ServerState | Should -Be ''
    }

    It 'omits an empty query through the real builder and shim' {
        $dir = Join-Path $script:Work 'empty-query'
        [void][System.IO.Directory]::CreateDirectory($dir)
        $prior = $env:MCP_CACHE_DIR_OVERRIDE
        $env:MCP_CACHE_DIR_OVERRIDE = $dir
        try {
            Write-McpYamlObject -Path (Join-Path $dir 'current-turn.yaml') -Document ([ordered]@{
                queryTitle = 'kept title'
                openedAt = '2026-09-30T00:00:00Z'
            })
            $updated = Invoke-ReplTurnUpsertParams -SourceType 'Codex' -SessionId 'Codex-20260930T000000Z-p2' -RequestId 'req-p2-empty' -Title '' -Status 'in_progress'
            $updated.turn.Contains('queryText') | Should -BeFalse
            $updated.turn.Contains('queryTitle') | Should -BeFalse
            $shim = New-McpPluginTurnUpsertRequest -Agent 'Codex' -SessionId 'Codex-20260930T000000Z-p2' -RequestId 'req-p2-empty' -Timestamp '2026-09-30T00:00:00Z' -QueryText '' -Title '' -Status 'in_progress' -Model 'codex'
            $map = $shim.ToParamsObject()
            $map.turn.Contains('queryText') | Should -BeFalse
            $canceled = Invoke-ReplTurnUpsertParams -SourceType 'Codex' -SessionId 'Codex-20260930T000000Z-p2' -RequestId 'req-p2-empty' -Title '' -Status 'cancelled'
            [string]$canceled.turn.status | Should -Be 'canceled'
            [string]$canceled.turn.queryText | Should -Be 'kept title'
        } finally {
            if ($null -eq $prior) { Remove-Item Env:MCP_CACHE_DIR_OVERRIDE -ErrorAction SilentlyContinue } else { $env:MCP_CACHE_DIR_OVERRIDE = $prior }
        }
    }

    It 'does not write a second failsafe for an unchanged queued update and keeps additive methods distinct' {
        $layout = New-P2Layout -Name 'duplicate-update'
        Write-P2Turn -Layout $layout
        $yaml = "response: same response`ninterpretation: same note`n"
        $first = Invoke-P2Verb -Layout $layout -Method 'workflow.sessionlog.updateTurn' -ParamsYaml $yaml -Mode 'queued'
        $second = Invoke-P2Verb -Layout $layout -Method 'workflow.sessionlog.updateTurn' -ParamsYaml $yaml -Mode 'queued'
        $first.ExitCode | Should -Be 0
        $second.ExitCode | Should -Be 0
        @(Get-ChildItem -LiteralPath $layout.Failsafe -Filter '*.yaml' -File).Count | Should -Be 1
        [string](Get-ReplObjectValue -InputObject $second.Receipt -Name 'code') | Should -Be 'queued'
        $second.ServerState | Should -Be ''

        $additive = New-P2Layout -Name 'additive'
        Write-P2Turn -Layout $additive
        $actions = Invoke-P2Verb -Layout $additive -Method 'workflow.sessionlog.appendActions' -ParamsYaml "actions:`n  - type: design_decision`n    description: one`n" -Mode 'queued'
        $dialog = Invoke-P2Verb -Layout $additive -Method 'workflow.sessionlog.appendDialog' -ParamsYaml "dialogItems:`n  - role: model`n    content: two`n    category: reasoning`n" -Mode 'queued'
        $actions.ExitCode | Should -Be 0
        $dialog.ExitCode | Should -Be 0
        $records = @(Get-ChildItem -LiteralPath $additive.Failsafe -Filter '*.yaml' -File)
        $records.Count | Should -Be 2
        $methods = @($records | ForEach-Object { [string](Read-McpYamlObject -Path $_.FullName).method })
        $methods | Should -Contain 'client.SessionLog.SubmitAsync'
        $methods | Should -Contain 'client.SessionLog.AppendDialogAsync'

        $drain = Invoke-P2Verb -Layout $additive -Method 'workflow.failsafe.drain' -ParamsYaml '' -Mode 'primary'
        $drain.ExitCode | Should -Be 0
        @(Get-ChildItem -LiteralPath $additive.Failsafe -Filter '*.yaml' -File -ErrorAction SilentlyContinue).Count | Should -Be 0
        ([regex]::Matches($drain.ServerState, 'client.SessionLog.SubmitAsync')).Count | Should -Be 1
        ([regex]::Matches($drain.ServerState, 'client.SessionLog.AppendDialogAsync')).Count | Should -Be 1
        $again = Invoke-P2Verb -Layout $additive -Method 'workflow.failsafe.drain' -ParamsYaml '' -Mode 'primary'
        $again.ExitCode | Should -Be 0
        $again.ServerState | Should -Be $drain.ServerState
    }

    It 'rejects a request mismatch and a wrong-workspace marker without changing session A or request R' {
        $layout = New-P2Layout -Name 'mismatch'
        Write-P2Turn -Layout $layout -RequestId 'req-R'
        $mismatch = Invoke-P2Verb -Layout $layout -Method 'workflow.sessionlog.appendActions' -ParamsYaml "requestId: req-other`nactions:`n  - type: design_decision`n    description: note`n" -Mode 'primary'
        $mismatch.ExitCode | Should -Not -Be 0
        $mismatch.ServerState | Should -Be ''
        [string](Read-McpYamlObject -Path (Join-Path $layout.Cache 'current-turn.yaml')).turnRequestId | Should -Be 'req-R'
        [string](Read-McpYamlObject -Path (Join-Path $layout.Cache 'current-turn.yaml')).sessionId | Should -Be $layout.SessionId

        $wrong = New-P2Layout -Name 'wrong-ws'
        $other = Join-Path $wrong.Root 'other-ws'
        [void][System.IO.Directory]::CreateDirectory($other)
        $otherMarker = Join-Path $other 'AGENTS-README-FIRST.yaml'
        [System.IO.File]::WriteAllText($otherMarker, "workspacePath: $other`napiKey: other`n")
        $otherSnap = Get-MarkerFileSnapshot -StartDir $other
        Write-McpYamlObject -Path (Join-Path $wrong.Cache 'current-turn.yaml') -Document ([ordered]@{
            turnRequestId = 'req-R'
            sessionId = 'Codex-20260930T000000Z-sessionA'
            status = 'in_progress'
            queryText = 'keep me'
            queryTitle = 'kept title'
            openedAt = '2026-09-30T00:00:00Z'
            markerFilePath = $otherSnap.markerFilePath
            markerLastWriteUtc = $otherSnap.markerLastWriteUtc
        })
        $session = Read-McpYamlObject -Path (Join-Path $wrong.Cache 'session-state.yaml')
        $session['sessionId'] = 'Codex-20260930T000000Z-sessionA'
        Write-McpYamlObject -Path (Join-Path $wrong.Cache 'session-state.yaml') -Document $session
        $rejected = Invoke-P2Verb -Layout $wrong -Method 'workflow.sessionlog.appendActions' -ParamsYaml "actions:`n  - type: design_decision`n    description: note`n" -Mode 'primary'
        $rejected.ExitCode | Should -Not -Be 0
        $rejected.Stderr | Should -Match 'wrong-workspace marker'
        $after = Read-McpYamlObject -Path (Join-Path $wrong.Cache 'current-turn.yaml')
        [string]$after.sessionId | Should -Be 'Codex-20260930T000000Z-sessionA'
        [string]$after.turnRequestId | Should -Be 'req-R'
        $rejected.ServerState | Should -Be ''
    }

    It 'refreshes same-path marker drift without rewriting the bound session' {
        $layout = New-P2Layout -Name 'drift'
        Write-P2Turn -Layout $layout -RequestId 'req-R'
        $marker = Join-Path $layout.Workspace 'AGENTS-README-FIRST.yaml'
        [System.IO.File]::SetLastWriteTimeUtc($marker, [datetime]'2026-09-30T03:00:00Z')
        $fresh = Get-MarkerFileSnapshot -StartDir $layout.Workspace
        $priorBootstrap = Get-Command Invoke-FullBootstrap -CommandType Function -ErrorAction Stop
        $priorPersist = Get-Command Invoke-ReplPersistTurn -CommandType Function -ErrorAction Stop
        $priorCache = $env:MCP_CACHE_DIR_OVERRIDE
        $priorWorkspace = $env:MCP_WORKSPACE_PATH
        $env:MCP_CACHE_DIR_OVERRIDE = $layout.Cache
        $env:MCP_WORKSPACE_PATH = $layout.Workspace
        try {
            function Invoke-FullBootstrap { param([string]$StartDir) return $true }
            function Invoke-ReplPersistTurn { return $true }
            $ok = Invoke-WorkflowAppendActions -ParamsYaml "actions:`n  - type: design_decision`n    description: drift`n"
            $ok | Should -BeTrue
            $turn = Read-McpYamlObject -Path (Join-Path $layout.Cache 'current-turn.yaml')
            [string]$turn.sessionId | Should -Be $layout.SessionId
            [string]$turn.turnRequestId | Should -Be 'req-R'
            [string]$turn.markerLastWriteUtc | Should -Be $fresh.markerLastWriteUtc
        } finally {
            Set-Item -Path Function:\Invoke-ReplPersistTurn -Value $priorPersist.ScriptBlock
            Set-Item -Path Function:\Invoke-FullBootstrap -Value $priorBootstrap.ScriptBlock
            if ($null -eq $priorCache) { Remove-Item Env:MCP_CACHE_DIR_OVERRIDE -ErrorAction SilentlyContinue } else { $env:MCP_CACHE_DIR_OVERRIDE = $priorCache }
            if ($null -eq $priorWorkspace) { Remove-Item Env:MCP_WORKSPACE_PATH -ErrorAction SilentlyContinue } else { $env:MCP_WORKSPACE_PATH = $priorWorkspace }
        }
    }

    It 'keeps Codex cache and audit identity when Grok agent variables are inherited' {
        $probe = Join-Path $script:Work 'identity-probe.ps1'
        @'
param([string]$Repl, [string]$Resolve)
$env:MCP_PLUGIN_HOST = 'codex'
$env:MCP_AGENT_NAME = 'GrokCode'
$env:PLUGIN_AGENT_NAME = 'GrokCode'
. $Resolve
. $Repl
Write-Output ("cache=" + (Get-McpCacheAgentKey))
Write-Output ("agent=" + $script:AgentName)
'@ | Set-Content -LiteralPath $probe -Encoding utf8
        $psi = [System.Diagnostics.ProcessStartInfo]::new()
        $psi.FileName = (Get-Command pwsh -ErrorAction Stop).Source
        foreach ($arg in @('-NoLogo', '-NoProfile', '-NonInteractive', '-File', $probe, '-Repl', $script:ReplScript, '-Resolve', $script:ResolveScript)) {
            $psi.ArgumentList.Add($arg)
        }
        $psi.UseShellExecute = $false
        $psi.RedirectStandardOutput = $true
        $psi.RedirectStandardError = $true
        $proc = [System.Diagnostics.Process]::Start($psi)
        $stdout = $proc.StandardOutput.ReadToEndAsync()
        $stderr = $proc.StandardError.ReadToEndAsync()
        $proc.WaitForExit(60000) | Should -BeTrue
        $proc.ExitCode | Should -Be 0 -Because $stderr.Result
        $stdout.Result | Should -Match 'cache=codex'
        $stdout.Result | Should -Match 'agent=Codex'
    }

    It 'preserves session A and request R from a native turn through a plugin append' {
        $layout = New-P2Layout -Name 'native-plugin'
        $session = Read-McpYamlObject -Path (Join-Path $layout.Cache 'session-state.yaml')
        $session['sessionId'] = 'Codex-20260930T000000Z-sessionA'
        Write-McpYamlObject -Path (Join-Path $layout.Cache 'session-state.yaml') -Document $session
        $layout.SessionId = 'Codex-20260930T000000Z-sessionA'
        Write-P2Turn -Layout $layout -RequestId 'req-R'
        $result = Invoke-P2Verb -Layout $layout -Method 'workflow.sessionlog.appendActions' -ParamsYaml "actions:`n  - type: design_decision`n    description: native handoff`n" -Mode 'primary'
        $result.ExitCode | Should -Be 0
        $result.ServerState | Should -Match 'Codex-20260930T000000Z-sessionA'
        $result.ServerState | Should -Match 'req-R'
        $result.ServerState | Should -Match 'codex-test-1'
        $after = Read-McpYamlObject -Path (Join-Path $layout.Cache 'current-turn.yaml')
        [string]$after.sessionId | Should -Be 'Codex-20260930T000000Z-sessionA'
        [string]$after.turnRequestId | Should -Be 'req-R'
    }

    It 'proves BUG-TRIAGE-246 module bootstrap user guide and message schema' {
        $bootstrap = [System.IO.File]::ReadAllText((Join-Path $script:RepoRoot 'docs\context\module-bootstrap.md'))
        $guide = [System.IO.File]::ReadAllText((Join-Path $script:RepoRoot 'docs\REPL-USER-GUIDE.md'))
        $schema = Get-Content -LiteralPath (Join-Path $script:RepoRoot 'docs\context\repl-yaml-message.schema.json') -Raw | ConvertFrom-Json
        $examples = @($bootstrap -split "`n" | Where-Object { $_ -match '"method":"workflow\.sessionlog\.beginTurn"' })
        $examples.Count | Should -BeGreaterThan 0
        foreach ($example in $examples) {
            $json = $example.Trim() | ConvertFrom-Json
            [string]$json.payload.params.planFile | Should -Be 'None'
            [string]$json.payload.params.todoId | Should -Be 'None'
        }
        $bootstrap | Should -Match 'explicit'
        $bootstrap | Should -Match 'verified cache'
        $bootstrap | Should -Match 'None'
        $bootstrap | Should -Match 'Ordinary first persistence'
        $bootstrap | Should -Match 'canceled'
        $bootstrap | Should -Match 'cancelled'
        $bootstrap | Should -Match 'durable reopen'

        $guideExamples = [regex]::Matches($guide, '(?s)method:\s*workflow\.sessionlog\.beginTurn\s+params:\s*(?<params>.*?)(?:```|\r?\n\r?\n)')
        $guideExamples.Count | Should -Be 3
        foreach ($example in $guideExamples) {
            $example.Groups['params'].Value | Should -Match 'planFile:\s*None'
            $example.Groups['params'].Value | Should -Match 'todoId:\s*None'
        }
        $guide | Should -Match 'exact `None`'
        $guide | Should -Match 'canceled'
        $guide | Should -Match 'cancelled'
        $guide | Should -Match 'durable reopen'

        $begin = $null
        foreach ($rule in @($schema.'$defs'.sessionlogRules.allOf)) {
            $method = $rule.if.properties.payload.properties.method.const
            if ($method -eq 'workflow.sessionlog.beginTurn') { $begin = $rule.then.properties.payload.properties.params }
        }
        $begin | Should -Not -BeNullOrEmpty
        @($begin.required) | Should -Contain 'requestId'
        @($begin.required) | Should -Contain 'queryTitle'
        @($begin.required) | Should -Contain 'queryText'
        @($begin.required) | Should -Not -Contain 'planFile'
        @($begin.required) | Should -Not -Contain 'todoId'
        [string]$begin.properties.planFile.type | Should -Be 'string'
        [int]$begin.properties.planFile.minLength | Should -Be 1
        [string]$begin.properties.todoId.type | Should -Be 'string'
        [int]$begin.properties.todoId.minLength | Should -Be 1
        [string]$begin.description | Should -Match 'Ordinary first persistence'
        [string]$begin.description | Should -Match 'None'
        [string]$begin.description | Should -Match 'canceled'
        [string]$begin.description | Should -Match 'cancelled'
        [string]$begin.description | Should -Match 'durable reopen'
        [string]$begin.description | Should -Match 'not a wire discriminator'
    }
}
