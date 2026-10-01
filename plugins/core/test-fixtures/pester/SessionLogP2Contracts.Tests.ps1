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
                '[Console]::InputEncoding = [System.Text.UTF8Encoding]::new($false); [Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false); $stdin = [Console]::In.ReadToEnd(); if ($stdin.Length -gt 0 -and [int][char]$stdin[0] -eq 0xFEFF) { $stdin = $stdin.Substring(1) }'
                'if ($env:P2_REPL_LOG) { [System.IO.File]::AppendAllText($env:P2_REPL_LOG, $stdin + [Environment]::NewLine, [System.Text.UTF8Encoding]::new($false)) }'
                '$mode = $env:P2_REPL_MODE'
                'if ([string]::IsNullOrWhiteSpace($mode)) { $mode = ''primary'' }'
                'if ($mode -eq ''primary'') {'
                '    if ($env:P2_SERVER_STATE) { [System.IO.File]::AppendAllText($env:P2_SERVER_STATE, $stdin + [Environment]::NewLine, [System.Text.UTF8Encoding]::new($false)) }'
                '    $fault = [string]$env:P2_TYPED_FAULT'
                '    $sessionMatches = [regex]::Matches($stdin, ''"sessionId"\s*:\s*"([^"]+)"'')'
                '    $requestMatches = [regex]::Matches($stdin, ''"requestId"\s*:\s*"([^"]+)"'')'
                '    $sessionId = if ($sessionMatches.Count -gt 0) { $sessionMatches[$sessionMatches.Count-1].Groups[1].Value } else { ''Codex-20260930T000000Z-p2'' }'
                '    $requestId = if ($requestMatches.Count -gt 0) { $requestMatches[$requestMatches.Count-1].Groups[1].Value } else { ''req-p2-verb'' }'
                '    if ($fault -eq ''wrong-dialog'') { $sessionId = ''sessionB''; $requestId = ''req-OTHER'' }'
                '    elseif ($fault -eq ''blank-session'') { $sessionId = ''   '' }'
                '    elseif ($fault -eq ''blank-request'') { $requestId = ''  '' }'
                '    elseif ($fault -eq ''case-session'') { if (-not [string]::IsNullOrWhiteSpace($sessionId)) { $chars = $sessionId.ToCharArray(); for ($i=0; $i -lt $chars.Length; $i++) { if ([char]::IsLetter($chars[$i])) { if ([char]::IsUpper($chars[$i])) { $chars[$i] = [char]::ToLowerInvariant($chars[$i]) } else { $chars[$i] = [char]::ToUpperInvariant($chars[$i]) }; break } }; $sessionId = [string]::new($chars) } }'
                '    elseif ($fault -eq ''case-request'') { if (-not [string]::IsNullOrWhiteSpace($requestId)) { $chars = $requestId.ToCharArray(); for ($i=0; $i -lt $chars.Length; $i++) { if ([char]::IsLetter($chars[$i])) { if ([char]::IsUpper($chars[$i])) { $chars[$i] = [char]::ToLowerInvariant($chars[$i]) } else { $chars[$i] = [char]::ToUpperInvariant($chars[$i]) }; break } }; $requestId = [string]::new($chars) } }'
                '    elseif ($fault -eq ''missing-ids'') { $sessionId = $null; $requestId = $null }'
                '    $retitled = ''true''; if ($fault -eq ''retitled-false'') { $retitled = ''false'' }'
                '    $omitIds = ($null -eq $sessionId -and $null -eq $requestId)'
                '    $lines = [System.Collections.Generic.List[string]]::new(); $lines.Add(''type: result''); $lines.Add(''payload:''); $lines.Add(''  result:'')'
                '    if ($stdin -match ''SetTurnTitleAsync'') { if (-not $omitIds) { $lines.Add((''    sessionId: '' + $sessionId)); $lines.Add((''    requestId: '' + $requestId)) }; $lines.Add((''    retitled: '' + $retitled)); $lines.Add(''    turnId: 1'') }'
                '    elseif ($stdin -match ''AppendDialogAsync'') { if (-not $omitIds) { $lines.Add((''    sessionId: '' + $sessionId)); $lines.Add((''    requestId: '' + $requestId)) }; $lines.Add(''    totalDialogCount: 1'') }'
                '    else { if (-not $omitIds) { $lines.Add((''    sessionId: '' + $sessionId)); $lines.Add((''    requestId: '' + $requestId)) }; $lines.Add(''    persisted: true''); $lines.Add(''    degraded: false'') }'
                '    $out = ($lines -join [Environment]::NewLine); [Console]::Out.Write($out + [Environment]::NewLine)'
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
            if (-not [string]::IsNullOrWhiteSpace([string]$env:MCP_PLUGIN_PERSIST_LOG)) {
                $psi.Environment['MCP_PLUGIN_PERSIST_LOG'] = [string]$env:MCP_PLUGIN_PERSIST_LOG
            }
            if (-not [string]::IsNullOrWhiteSpace([string]$env:P2_TYPED_FAULT)) {
                $psi.Environment['P2_TYPED_FAULT'] = [string]$env:P2_TYPED_FAULT
            }
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
        $reopenJson = ($payloads[-1] -replace "[\uFEFF]", '').Trim(); $reopen = $reopenJson | ConvertFrom-Json
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
        $priorLocation = Get-Location
        $env:MCP_CACHE_DIR_OVERRIDE = $layout.Cache
        $env:MCP_WORKSPACE_PATH = $layout.Workspace
        Set-Location -LiteralPath $layout.Workspace
        try {
            function Invoke-FullBootstrap { param([string]$StartDir) return $true }
            function Invoke-ReplPersistTurn { param($RequestId,$Title,$IncludeSessionTitle,$Status,$ResponseText,$ActionsYaml,$ProcessingDialog,$Interpretation,$TokenCount,$Tags,$ContextList,$PlanFile,$TodoId) return $true }
            $ok = Invoke-WorkflowAppendActions -ParamsYaml "actions:`n  - type: design_decision`n    description: drift`n"
            $ok | Should -BeTrue
            $turn = Read-McpYamlObject -Path (Join-Path $layout.Cache 'current-turn.yaml')
            [string]$turn.sessionId | Should -Be $layout.SessionId
            [string]$turn.turnRequestId | Should -Be 'req-R'
            [string]$turn.markerLastWriteUtc | Should -Be $fresh.markerLastWriteUtc
        } finally {
            Set-Location -LiteralPath $priorLocation.Path
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
        $fence = [string]::new([char]96, 3)
        $blocks = [regex]::Matches(($bootstrap -replace "`r`n", "`n"), ('(?ms)^' + $fence + 'json[ \t]*\n(?<body>.*?)^' + $fence + '[ \t]*$'))
        $beginBlocks = @($blocks | Where-Object { $_.Groups['body'].Value -match 'workflow\.sessionlog\.beginTurn' })
        $beginBlocks.Count | Should -BeGreaterThan 0
        foreach ($block in $beginBlocks) {
            $json = $block.Groups['body'].Value.Trim() | ConvertFrom-Json
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

        # Exhaustive fenced-document oracle: parse every json/yaml/yml fence in
        # module-bootstrap.md and REPL-USER-GUIDE.md; require raw beginTurn
        # occurrence count to equal parsed beginTurn example count.
        $fenceDoc = [string]::new([char]96, 3)
        $docOracle = @()
        foreach ($rel in @('docs\context\module-bootstrap.md', 'docs\REPL-USER-GUIDE.md')) {
            $docPath = Join-Path $script:RepoRoot $rel
            $text = ([System.IO.File]::ReadAllText($docPath) -replace "`r`n", "`n")
            $blocks = [regex]::Matches($text, ('(?ms)^(?<indent>[ \t]{0,3})' + $fenceDoc + '(?<lang>json|yaml|yml)[ \t]*\n(?<body>.*?)^[ \t]{0,3}' + $fenceDoc + '[ \t]*$'))
            $rawBegin = 0
            $parsedBegin = 0
            foreach ($block in $blocks) {
                $body = $block.Groups['body'].Value
                $indent = $block.Groups['indent'].Value
                if ($indent.Length -gt 0) {
                    $body = $body -replace ('(?m)^' + [regex]::Escape($indent)), ''
                }
                $rawBegin += [regex]::Matches($body, 'workflow\.sessionlog\.beginTurn').Count
                $parsed = @()
                if ($block.Groups['lang'].Value -eq 'json') {
                    $parsed = @(ConvertFrom-Json -InputObject $body -AsHashtable -ErrorAction Stop)
                } else {
                    if (-not (Get-Command ConvertFrom-Yaml -ErrorAction SilentlyContinue)) {
                        . (Join-Path $script:RepoRoot 'plugins\core\lib-ps\yaml-object-mutation.ps1')
                        Import-McpYamlSerializer
                    }
                    $parsed = @(ConvertFrom-Yaml -Yaml $body -AllDocuments -ErrorAction Stop)
                }
                foreach ($item in $parsed) {
                    $method = $null
                    $params = $null
                    if ($item -is [System.Collections.IDictionary]) {
                        if ($item.Contains('payload')) {
                            $payload = $item['payload']
                            if ($payload -is [System.Collections.IDictionary]) {
                                $method = [string]$payload['method']
                                $params = $payload['params']
                            } else {
                                $method = [string]$payload.method
                                $params = $payload.params
                            }
                        }
                    } else {
                        $method = [string]$item.payload.method
                        $params = $item.payload.params
                    }
                    if ($method -eq 'workflow.sessionlog.beginTurn') {
                        $parsedBegin++
                        [string]$params.planFile | Should -Be 'None'
                        [string]$params.todoId | Should -Be 'None'
                    }
                }
            }
            $rawBegin | Should -Be $parsedBegin
            $docOracle += [pscustomobject]@{ Path = $rel; Blocks = $blocks.Count; RawBegin = $rawBegin; ParsedBegin = $parsedBegin }
        }
        ($docOracle | Measure-Object -Property Blocks -Sum).Sum | Should -BeGreaterThan 0
        ($docOracle | Measure-Object -Property ParsedBegin -Sum).Sum | Should -BeGreaterThan 0
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

    It 'rejects unproven persistence envelopes without clearing failsafe recovery' {
        $astPath = $script:ReplScript
        $tokens = $null; $errors = $null
        $ast = [System.Management.Automation.Language.Parser]::ParseFile($astPath, [ref]$tokens, [ref]$errors)
        foreach ($name in @('Invoke-ReplPersistTurn','New-ReplSessionVerbReceipt','Publish-ReplSessionVerbReceipt','Get-ReplStableFingerprint','Find-ReplFailsafeByFingerprint')) {
            $fn = $ast.Find({ param($n) $n -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $n.Name -eq $name }, $true)
            . ([scriptblock]::Create($fn.Extent.Text))
        }
        function Get-ReplObjectValue { param($InputObject, [string]$Name) if ($null -eq $InputObject) { return $null }; if ($InputObject -is [System.Collections.IDictionary]) { return $InputObject[$Name] }; return $InputObject.$Name }
        function Get-ReplSessionMeta { return @{ SourceType = 'Codex'; SessionId = 'session-A' } }
        function Invoke-ReplTurnUpsertParams { param($SourceType,$SessionId,$RequestId,$Title,$Status,$ResponseText,$ActionsYaml,$ProcessingDialog,$Interpretation,$TokenCount,$Tags,$ContextList,$PlanFile,$TodoId) return @{ turn = @{ queryText = 'q'; timestamp = '2026-09-30T00:00:00Z' } } }
        function Get-ReplTurnCacheField { param($Field) return '' }
        function Set-ReplTurnCacheField { param($Field,$Value) }
        function Get-ReplSessionStateValue { param($Key) return '' }
        function Resolve-McpPluginAgentHeaderFields { param($SessionId,$CacheDir,$AgentName,$HostName) return @{ agentSessionId=''; agentSessionTranscriptFile=''; agentExecutablePath=''; agentExecutableVersion='' } }
        function Get-McpPluginFirstText { param($Values) return '' }
        function Get-McpPluginFirstExistingFile { param($Values) return '' }
        function ConvertTo-Yaml { param($Data,$Options) return (ConvertTo-Json $Data -Depth 20 -Compress) }
        function Convert-ReplParamsYamlToObject { param($ParamsYaml) return ($ParamsYaml | ConvertFrom-Json -AsHashtable) }
        $script:records = @(); $script:cleared = $false
        function Write-ReplFailsafe { param($Method,$ParamsYaml,$Label,$PayloadFingerprint) $script:records += $PayloadFingerprint; return 'fs-1' }
        function Clear-ReplFailsafe { param($Path) $script:cleared = $true }
        function Invoke-ReplRaw { param($Method,$ParamsYaml) return @{ Success = $true; Output = '{}'; Error = '' } }
        $script:ReplPersistVerbMethod = 'workflow.sessionlog.updateTurn'
        { Invoke-ReplPersistTurn -RequestId 'req-R' -Status 'in_progress' -ResponseText 'x' } | Should -Throw
        $script:cleared | Should -BeFalse
        $script:records.Count | Should -Be 1
        Assert-P2Receipt -Receipt $script:LastReplPersistenceDetails -Method 'workflow.sessionlog.updateTurn' -RequestId 'req-R' -Code 'rejected'
    }

    It 'rejects update and appendDialog caller request mismatches and binds explicit update metadata' {
        $layout = New-P2Layout -Name 'req-meta'
        Write-P2Turn -Layout $layout -RequestId 'req-R'
        $badUpdate = Invoke-P2Verb -Layout $layout -Method 'workflow.sessionlog.updateTurn' -ParamsYaml "requestId: req-OTHER`nresponse: nope`nplanFile: plan-NEW`ntodoId: todo-NEW`n" -Mode 'primary'
        $badUpdate.ExitCode | Should -Not -Be 0
        $badUpdate.ServerState | Should -Be ''
        $turn = Read-McpYamlObject -Path (Join-Path $layout.Cache 'current-turn.yaml')
        [string]$turn.planFile | Should -Be 'docs/plans/p2.md'
        $badDialog = Invoke-P2Verb -Layout $layout -Method 'workflow.sessionlog.appendDialog' -ParamsYaml "requestId: req-OTHER`ndialogItems:`n  - role: model`n    content: nope`n" -Mode 'primary'
        $badDialog.ExitCode | Should -Not -Be 0
        $good = Invoke-P2Verb -Layout $layout -Method 'workflow.sessionlog.updateTurn' -ParamsYaml "requestId: req-R`nresponse: ok`nplanFile: plan-NEW`ntodoId: todo-NEW`n" -Mode 'primary'
        $good.ExitCode | Should -Be 0
        $after = Read-McpYamlObject -Path (Join-Path $layout.Cache 'current-turn.yaml')
        [string]$after.planFile | Should -Be 'plan-NEW'
        [string]$after.todoId | Should -Be 'todo-NEW'
        $good.ServerState | Should -Match 'plan-NEW'
        $good.ServerState | Should -Match 'todo-NEW'
    }

    It 'refuses beginTurn session rebind without durable proof and reports degraded recovery path' {
        $layout = New-P2Layout -Name 'rebind'
        Write-P2Turn -Layout $layout -RequestId 'req-R'
        Write-McpYamlObject -Path (Join-Path $layout.Cache 'session-state.yaml') -Document ([ordered]@{
            status = 'verified'
            sessionId = 'Codex-20260930T000000Z-sessionB'
            agent = 'Codex'
            markerFilePath = $layout.Snapshot.markerFilePath
            markerLastWriteUtc = $layout.Snapshot.markerLastWriteUtc
        })
        $result = Invoke-P2Verb -Layout $layout -Method 'workflow.sessionlog.beginTurn' -ParamsYaml "requestId: req-R`nqueryTitle: x`nqueryText: x`nplanFile: None`ntodoId: None`n" -Mode 'primary'
        $result.ExitCode | Should -Not -Be 0
        $turn = Read-McpYamlObject -Path (Join-Path $layout.Cache 'current-turn.yaml')
        [string]$turn.sessionId | Should -Be $layout.SessionId
        $degLayout = New-P2Layout -Name 'hv07-deg'
        $deg = Invoke-P2Verb -Layout $degLayout -Method 'workflow.sessionlog.beginTurn' -ParamsYaml ("requestId: req-DEG`nqueryTitle: deg`nqueryText: deg`nplanFile: None`ntodoId: None`n") -Mode 'queued'
        $deg.ExitCode | Should -Be 0
        $turnDeg = Read-McpYamlObject -Path (Join-Path $degLayout.Cache 'current-turn.yaml')
        [string]$turnDeg.degraded | Should -Match '^(?i:true|1)$'
        $turnDeg.Contains('failsafePath') | Should -BeTrue
        Test-Path -LiteralPath ([string]$turnDeg.failsafePath) | Should -BeTrue
    }

    It 'binds cached planFile and todoId on appendActions completeTurn and recovery submit' {
        $layout = New-P2Layout -Name 'hv01-meta'
        Write-P2Turn -Layout $layout -RequestId 'req-R'
        $append = Invoke-P2Verb -Layout $layout -Method 'workflow.sessionlog.appendActions' -ParamsYaml "actions:`n  - type: design_decision`n    description: note`n" -Mode 'primary'
        $append.ExitCode | Should -Be 0
        $append.ServerState | Should -Match 'docs/plans/p2.md'
        $append.ServerState | Should -Match 'BUG-TRIAGE-246'
        $complete = Invoke-P2Verb -Layout $layout -Method 'workflow.sessionlog.completeTurn' -ParamsYaml "requestId: req-R`nresponse: done`n" -Mode 'primary'
        $complete.ExitCode | Should -Be 0
        $complete.ServerState | Should -Match 'docs/plans/p2.md'
        $complete.ServerState | Should -Match 'BUG-TRIAGE-246'
    }

    It 'sends exact None on updateTurn when caller clears planFile and todoId' {
        $layout = New-P2Layout -Name 'hv01-none'
        Write-P2Turn -Layout $layout -RequestId 'req-R'
        $upd = Invoke-P2Verb -Layout $layout -Method 'workflow.sessionlog.updateTurn' -ParamsYaml "requestId: req-R`nresponse: cleared`nplanFile: `ntodoId: `n" -Mode 'primary'
        $upd.ExitCode | Should -Be 0
        $upd.ServerState | Should -Match 'planFile:\s*None|planFile: None|"planFile":"None"'
        $after = Read-McpYamlObject -Path (Join-Path $layout.Cache 'current-turn.yaml')
        [string]$after.planFile | Should -Be 'None'
        [string]$after.todoId | Should -Be 'None'
    }

    It 'refuses durable reopen without sessionId and without workspace marker proof' {
        $layout = New-P2Layout -Name 'hv02-proof'
        Write-P2Turn -Layout $layout -RequestId 'req-R'
        $turnPath = Join-Path $layout.Cache 'current-turn.yaml'
        $turn = Read-McpYamlObject -Path $turnPath
        $turn['persisted'] = 'true'
        $turn.Remove('sessionId')
        Write-McpYamlObject -Path $turnPath -Document $turn
        $noSession = Invoke-P2Verb -Layout $layout -Method 'workflow.sessionlog.beginTurn' -ParamsYaml "requestId: req-R`nqueryTitle: x`nqueryText: x`nplanFile: None`ntodoId: None`n" -Mode 'primary'
        $noSession.ExitCode | Should -Not -Be 0

        Write-P2Turn -Layout $layout -RequestId 'req-R'
        $turn = Read-McpYamlObject -Path $turnPath
        $turn['persisted'] = 'true'
        $turn['markerFilePath'] = 'F:\other-workspace\AGENTS-README-FIRST.yaml'
        Write-McpYamlObject -Path $turnPath -Document $turn
        $wrongWs = Invoke-P2Verb -Layout $layout -Method 'workflow.sessionlog.beginTurn' -ParamsYaml "requestId: req-R`nqueryTitle: x`nqueryText: x`nplanFile: None`ntodoId: None`n" -Mode 'primary'
        $wrongWs.ExitCode | Should -Not -Be 0
        $after = Read-McpYamlObject -Path $turnPath
        [string]$after.sessionId | Should -Be $layout.SessionId
    }

    It 'rejects failTurn setTurnTitle and completeTurn caller request mismatches without mutation' {
        $layout = New-P2Layout -Name 'hv03-mismatch'
        Write-P2Turn -Layout $layout -RequestId 'req-R'
        $before = Get-FileHash -LiteralPath (Join-Path $layout.Cache 'current-turn.yaml') -Algorithm SHA256
        $fail = Invoke-P2Verb -Layout $layout -Method 'workflow.sessionlog.failTurn' -ParamsYaml "requestId: req-OTHER`nerrorMessage: nope`n" -Mode 'primary'
        $fail.ExitCode | Should -Not -Be 0
        $fail.ServerState | Should -Be ''
        $title = Invoke-P2Verb -Layout $layout -Method 'workflow.sessionlog.setTurnTitle' -ParamsYaml "requestId: req-OTHER`nqueryTitle: hijack`n" -Mode 'primary'
        $title.ExitCode | Should -Not -Be 0
        $title.ServerState | Should -Be ''
        $complete = Invoke-P2Verb -Layout $layout -Method 'workflow.sessionlog.completeTurn' -ParamsYaml "requestId: req-OTHER`nresponse: nope`nqueryTitle: hijack`n" -Mode 'primary'
        $complete.ExitCode | Should -Not -Be 0
        $complete.ServerState | Should -Be ''
        $after = Get-FileHash -LiteralPath (Join-Path $layout.Cache 'current-turn.yaml') -Algorithm SHA256
        $after.Hash | Should -Be $before.Hash
        $turn = Read-McpYamlObject -Path (Join-Path $layout.Cache 'current-turn.yaml')
        [string]$turn.queryTitle | Should -Be 'kept title'
        Test-Path -LiteralPath (Join-Path $layout.Cache 'current-turn.yaml') | Should -BeTrue
    }

    It 'round-trips hash and space failsafePath through object YAML and clears degraded after primary success' {
        $layout = New-P2Layout -Name 'hv04-06'
        $hashPath = Join-Path $layout.Failsafe 'dir with space#hash'
        [void][System.IO.Directory]::CreateDirectory($hashPath)
        $artifact = Join-Path $hashPath 'keep.yaml'
        [System.IO.File]::WriteAllText($artifact, "method: client.SessionLog.SubmitAsync`n")
        Write-P2Turn -Layout $layout -RequestId 'req-R'
        $turnPath = Join-Path $layout.Cache 'current-turn.yaml'
        $doc = Read-McpYamlObject -Path $turnPath
        $doc['degraded'] = $true
        $doc['failsafePath'] = $artifact
        Write-McpYamlObject -Path $turnPath -Document $doc
        $round = Read-McpYamlObject -Path $turnPath
        [string]$round.failsafePath | Should -Be $artifact
        Test-Path -LiteralPath ([string]$round.failsafePath) | Should -BeTrue
        $ok = Invoke-P2Verb -Layout $layout -Method 'workflow.sessionlog.updateTurn' -ParamsYaml "requestId: req-R`nresponse: recovered`n" -Mode 'primary'
        $ok.ExitCode | Should -Be 0
        $after = Read-McpYamlObject -Path $turnPath
        $after.Contains('degraded') | Should -BeFalse
        $after.Contains('failsafePath') | Should -BeFalse
        [string]$after.persisted | Should -Match '^(?i:true|1)$'
    }

    It 'retries primary submit for identical degraded fingerprint instead of only reusing failsafe' {
        $layout = New-P2Layout -Name 'hv05-retry'
        $first = Invoke-P2Verb -Layout $layout -Method 'workflow.sessionlog.beginTurn' -ParamsYaml "requestId: req-p2-verb`nqueryTitle: kept title`nqueryText: keep me`nplanFile: docs/plans/p2.md`ntodoId: BUG-TRIAGE-246`n" -Mode 'queued'
        $first.ExitCode | Should -Be 0
        $fs = @(Get-ChildItem -LiteralPath $layout.Failsafe -Filter '*.yaml' -File -ErrorAction SilentlyContinue)
        $fs.Count | Should -BeGreaterThan 0
        $second = Invoke-P2Verb -Layout $layout -Method 'workflow.sessionlog.beginTurn' -ParamsYaml "requestId: req-p2-verb`nqueryTitle: kept title`nqueryText: keep me`nplanFile: docs/plans/p2.md`ntodoId: BUG-TRIAGE-246`n" -Mode 'primary'
        $second.ExitCode | Should -Be 0
        $second.ServerState | Should -Match 'keep me'
        $after = Read-McpYamlObject -Path (Join-Path $layout.Cache 'current-turn.yaml')
        if ($after.Contains('degraded')) { [string]$after.degraded | Should -Not -Match '^(?i:true|1)$' }
    }

    It 'emits recoveryArtifactPath for degraded duplicate Open-PluginTurn path' {
        $layout = New-P2Layout -Name 'hv04-hook'
        $hook = Join-Path $script:RepoRoot 'plugins\core\lib-ps\plugin-hook.ps1'
        $artifact = Join-Path $layout.Failsafe 'keep-#space.yaml'
        [System.IO.File]::WriteAllText($artifact, "method: x`n")
        Write-P2Turn -Layout $layout -RequestId 'req-R'
        $marker = Join-Path $layout.Workspace 'AGENTS-README-FIRST.yaml'
        if (-not (Test-Path -LiteralPath $marker)) {
            Set-Content -LiteralPath $marker -Value "marker: p2-hv04`n" -Encoding utf8
        }
        # Keep Ensure-PluginMarkerFresh from rewriting session to MCP_UNTRUSTED: seed
        # verified session with matching marker fingerprint and Codex agent key.
        . (Join-Path $script:RepoRoot 'plugins\core\lib-ps\marker-resolver.ps1')
        $snap = Get-MarkerFileSnapshot -StartDir $layout.Workspace
        $now = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
        $sessionPath = Join-Path $layout.Cache 'session-state.yaml'
        Write-McpYamlObject -Path $sessionPath -Document ([ordered]@{
            sessionId = 'Codex-20260930T000000Z-plugin-session'
            status = 'verified'
            agent = 'Codex'
            workspacePath = $layout.Workspace
            timestamp = $now
            lastUpdated = $now
            markerFilePath = [string]$snap.markerFilePath
            markerLastWriteUtc = [string]$snap.markerLastWriteUtc
        })
        $turnPath = Join-Path $layout.Cache 'current-turn.yaml'
        $doc = Read-McpYamlObject -Path $turnPath
        $doc['degraded'] = $true
        $doc['failsafePath'] = $artifact
        $doc['openedAt'] = $now
        $doc['queryText'] = 'same prompt'
        $doc['status'] = 'in_progress'
        $doc['sessionId'] = 'Codex-20260930T000000Z-plugin-session'
        Write-McpYamlObject -Path $turnPath -Document $doc
        $psi = [System.Diagnostics.ProcessStartInfo]::new()
        $psi.FileName = (Get-Command pwsh -ErrorAction Stop).Source
        foreach ($arg in @('-NoLogo','-NoProfile','-NonInteractive','-File',$hook,'-HookName','user-prompt-submit')) { $psi.ArgumentList.Add($arg) }
        $psi.WorkingDirectory = $layout.Workspace
        $psi.UseShellExecute = $false
        $psi.RedirectStandardInput = $true
        $psi.RedirectStandardOutput = $true
        $psi.RedirectStandardError = $true
        $toRemove = @($psi.Environment.Keys | Where-Object { $_ -match 'GROK|PLUGIN_AGENT|MCP_AGENT|MCP_SESSION|MCP_CACHE|MCP_WORKSPACE|MCPSERVER' })
        foreach ($k in $toRemove) { [void]$psi.Environment.Remove($k) }
        $psi.Environment['MCP_CACHE_DIR_OVERRIDE'] = $layout.Cache
        $psi.Environment['MCP_WORKSPACE_PATH'] = $layout.Workspace
        $psi.Environment['MCPSERVER_WORKSPACE_PATH'] = $layout.Workspace
        $psi.Environment['MCP_PLUGIN_HOST'] = 'codex'
        $psi.Environment['MCP_AGENT_NAME'] = 'Codex'
        $psi.Environment['PLUGIN_AGENT_NAME'] = 'Codex'
        $proc = [System.Diagnostics.Process]::Start($psi)
        $stdin = '{"prompt":"same prompt"}'
        $proc.StandardInput.Write($stdin)
        $proc.StandardInput.Close()
        $stdout = $proc.StandardOutput.ReadToEndAsync()
        $stderr = $proc.StandardError.ReadToEndAsync()
        if (-not $proc.WaitForExit(60000)) { try { $proc.Kill($true) } catch { }; throw 'hook timed out' }
        $out = [string]$stdout.Result
        $err = [string]$stderr.Result
        if ([string]::IsNullOrWhiteSpace($out) -and -not [string]::IsNullOrWhiteSpace($err)) {
            throw "hook produced empty stdout; stderr=$err"
        }
        $out | Should -Match 'turn-already-open'
        $out | Should -Match 'recoveryArtifactPath'
        $out | Should -Match 'keep-'
    }

    It 'HV10 durable update/append/complete omit planFile and todoId when caller omits them' {
        $layout = New-P2Layout -Name 'hv10-omit'
        $null = Invoke-P2Verb -Layout $layout -Method 'workflow.sessionlog.beginTurn' -ParamsYaml "requestId: req-p2-verb`nqueryTitle: kept title`nqueryText: kept query`nplanFile: docs/plans/p2.md`ntodoId: BUG-TRIAGE-246"
        $persistLog = Join-Path $layout.Cache 'persist-log.jsonl'
        if (Test-Path $persistLog) { Remove-Item -LiteralPath $persistLog -Force }
        $env:MCP_PLUGIN_PERSIST_LOG = $persistLog
        try {
            $upd = Invoke-P2Verb -Layout $layout -Method 'workflow.sessionlog.updateTurn' -ParamsYaml "response: mid`n"
            $upd.ExitCode | Should -Be 0
            $app = Invoke-P2Verb -Layout $layout -Method 'workflow.sessionlog.appendActions' -ParamsYaml "actions:`n  - type: design_decision`n    description: note`n"
            $app.ExitCode | Should -Be 0
            $fin = Invoke-P2Verb -Layout $layout -Method 'workflow.sessionlog.completeTurn' -ParamsYaml "response: done`n"
            $fin.ExitCode | Should -Be 0
        } finally {
            Remove-Item Env:MCP_PLUGIN_PERSIST_LOG -ErrorAction SilentlyContinue
        }
        $lines = @(Get-Content -LiteralPath $persistLog -ErrorAction Stop)
        $lines.Count | Should -BeGreaterThan 0
        foreach ($line in $lines) {
            $rec = $line | ConvertFrom-Json
            [bool]$rec.boundPlanFile | Should -BeFalse
            [bool]$rec.boundTodoId | Should -BeFalse
            [string]$rec.planFile | Should -BeNullOrEmpty
            [string]$rec.todoId | Should -BeNullOrEmpty
        }
        $cached = Read-McpYamlObject -Path (Join-Path $layout.Cache 'current-turn.yaml')
        [string]$cached.planFile | Should -Be 'docs/plans/p2.md'
        [string]$cached.todoId | Should -Be 'BUG-TRIAGE-246'
    }

    It 'HV11 explicit append metadata updates cache used by later complete' {
        $layout = New-P2Layout -Name 'hv11-append-meta'
        $null = Invoke-P2Verb -Layout $layout -Method 'workflow.sessionlog.beginTurn' -ParamsYaml "requestId: req-p2-verb`nqueryTitle: kept title`nqueryText: kept query`nplanFile: docs/plans/p2.md`ntodoId: BUG-TRIAGE-246"
        $persistLog = Join-Path $layout.Cache 'persist-log.jsonl'
        if (Test-Path $persistLog) { Remove-Item -LiteralPath $persistLog -Force }
        $env:MCP_PLUGIN_PERSIST_LOG = $persistLog
        try {
            $app = Invoke-P2Verb -Layout $layout -Method 'workflow.sessionlog.appendActions' -ParamsYaml "planFile: docs/plans/new-plan.md`ntodoId: BUG-TRIAGE-245`nactions:`n  - type: design_decision`n    description: note`n"
            $app.ExitCode | Should -Be 0
            $cached = Read-McpYamlObject -Path (Join-Path $layout.Cache 'current-turn.yaml')
            [string]$cached.planFile | Should -Be 'docs/plans/new-plan.md'
            [string]$cached.todoId | Should -Be 'BUG-TRIAGE-245'
            $fin = Invoke-P2Verb -Layout $layout -Method 'workflow.sessionlog.completeTurn' -ParamsYaml "response: done`n"
            $fin.ExitCode | Should -Be 0
        } finally {
            Remove-Item Env:MCP_PLUGIN_PERSIST_LOG -ErrorAction SilentlyContinue
        }
        $records = @(Get-Content -LiteralPath $persistLog | ForEach-Object { $_ | ConvertFrom-Json })
        $appendRec = $records | Where-Object { [bool]$_.boundPlanFile -eq $true } | Select-Object -First 1
        $appendRec | Should -Not -BeNullOrEmpty
        [string]$appendRec.planFile | Should -Be 'docs/plans/new-plan.md'
        [string]$appendRec.todoId | Should -Be 'BUG-TRIAGE-245'
        $completeRec = $records | Select-Object -Last 1
        # HV10: durable complete omits plan/todo so the server keeps the explicit append values.
        [bool]$completeRec.boundPlanFile | Should -BeFalse
        [bool]$completeRec.boundTodoId | Should -BeFalse
        $cachedAfter = Read-McpYamlObject -Path (Join-Path $layout.Cache 'current-turn.yaml')
        [string]$cachedAfter.planFile | Should -Be 'docs/plans/new-plan.md'
        [string]$cachedAfter.todoId | Should -Be 'BUG-TRIAGE-245'
    }

    It 'HV12 beginTurn rejects missing marker proof and wrong-marker degraded reopen' {
        $layout = New-P2Layout -Name 'hv12-marker'
        $null = Invoke-P2Verb -Layout $layout -Method 'workflow.sessionlog.beginTurn' -ParamsYaml "requestId: req-p2-verb`nqueryTitle: kept title`nqueryText: kept query`nplanFile: docs/plans/p2.md`ntodoId: BUG-TRIAGE-246"
        $turnPath = Join-Path $layout.Cache 'current-turn.yaml'
        $turn = Read-McpYamlObject -Path $turnPath
        $turn['persisted'] = 'true'
        if ($turn.Contains('markerFilePath')) { $turn.Remove('markerFilePath') }
        if ($turn.Contains('markerLastWriteUtc')) { $turn.Remove('markerLastWriteUtc') }
        Write-McpYamlObject -Path $turnPath -Document $turn
        $missing = Invoke-P2Verb -Layout $layout -Method 'workflow.sessionlog.beginTurn' -ParamsYaml "requestId: req-p2-verb`nqueryTitle: kept title`nqueryText: kept query`n"
        $missing.ExitCode | Should -Not -Be 0
        $missing.Stderr | Should -Match 'workspace identity proof is missing'

        $layout2 = New-P2Layout -Name 'hv12-degraded-wrong'
        $null = Invoke-P2Verb -Layout $layout2 -Method 'workflow.sessionlog.beginTurn' -ParamsYaml "requestId: req-p2-verb`nqueryTitle: kept title`nqueryText: kept query`nplanFile: docs/plans/p2.md`ntodoId: BUG-TRIAGE-246"
        $turn2 = Read-McpYamlObject -Path (Join-Path $layout2.Cache 'current-turn.yaml')
        $turn2['persisted'] = 'true'
        $turn2['degraded'] = $true
        $turn2['markerFilePath'] = 'F:\other-workspace\AGENTS-README-FIRST.yaml'
        $turn2['markerLastWriteUtc'] = '2020-01-01T00:00:00Z'
        Write-McpYamlObject -Path (Join-Path $layout2.Cache 'current-turn.yaml') -Document $turn2
        $wrong = Invoke-P2Verb -Layout $layout2 -Method 'workflow.sessionlog.beginTurn' -ParamsYaml "requestId: req-p2-verb`nqueryTitle: kept title`nqueryText: kept query`n"
        $wrong.ExitCode | Should -Not -Be 0
        $wrong.Stderr | Should -Match 'wrong-workspace marker'
        $after = Read-McpYamlObject -Path (Join-Path $layout2.Cache 'current-turn.yaml')
        [string]$after.markerFilePath | Should -Be 'F:\other-workspace\AGENTS-README-FIRST.yaml'
    }

    It 'HV13 request mismatch does not mutate current-turn when markers are missing' {
        $layout = New-P2Layout -Name 'hv13-mismatch'
        $null = Invoke-P2Verb -Layout $layout -Method 'workflow.sessionlog.beginTurn' -ParamsYaml "requestId: req-p2-verb`nqueryTitle: kept title`nqueryText: kept query`nplanFile: docs/plans/p2.md`ntodoId: BUG-TRIAGE-246"
        $turnPath = Join-Path $layout.Cache 'current-turn.yaml'
        $turn = Read-McpYamlObject -Path $turnPath
        if ($turn.Contains('markerFilePath')) { $turn.Remove('markerFilePath') }
        if ($turn.Contains('markerLastWriteUtc')) { $turn.Remove('markerLastWriteUtc') }
        Write-McpYamlObject -Path $turnPath -Document $turn
        $before = Get-FileHash -LiteralPath $turnPath -Algorithm SHA256
        $rej = Invoke-P2Verb -Layout $layout -Method 'workflow.sessionlog.updateTurn' -ParamsYaml "requestId: req-OTHER`nresponse: nope`n"
        $rej.ExitCode | Should -Not -Be 0
        $after = Get-FileHash -LiteralPath $turnPath -Algorithm SHA256
        $after.Hash | Should -Be $before.Hash
    }

    It 'HV14 appendDialog and setTurnTitle reject wrong typed identity or retitled=false' {
        $layout = New-P2Layout -Name 'hv14-typed'
        $null = Invoke-P2Verb -Layout $layout -Method 'workflow.sessionlog.beginTurn' -ParamsYaml "requestId: req-p2-verb`nqueryTitle: kept title`nqueryText: kept query`nplanFile: docs/plans/p2.md`ntodoId: BUG-TRIAGE-246"

        $env:P2_TYPED_FAULT = 'wrong-dialog'
        try {
            $dialog = Invoke-P2Verb -Layout $layout -Method 'workflow.sessionlog.appendDialog' -ParamsYaml "dialogItems:`n  - role: model`n    content: hello`n"
            $dialog.ExitCode | Should -Not -Be 0
            "$($dialog.Stderr)$($dialog.Stdout)" | Should -Match 'typed result'
        } finally {
            Remove-Item Env:P2_TYPED_FAULT -ErrorAction SilentlyContinue
        }

        foreach ($fault in @('blank-session','blank-request','missing-ids')) {
            $env:P2_TYPED_FAULT = $fault
            try {
                $blankDialog = Invoke-P2Verb -Layout $layout -Method 'workflow.sessionlog.appendDialog' -ParamsYaml "dialogItems:`n  - role: model`n    content: hello-$fault`n"
                $blankDialog.ExitCode | Should -Not -Be 0 -Because $fault
                "$($blankDialog.Stderr)$($blankDialog.Stdout)" | Should -Match 'typed result|missing or blank' -Because $fault

                $blankTitle = Invoke-P2Verb -Layout $layout -Method 'workflow.sessionlog.setTurnTitle' -ParamsYaml "queryTitle: Renamed-$fault`n"
                $blankTitle.ExitCode | Should -Not -Be 0 -Because $fault
                "$($blankTitle.Stderr)$($blankTitle.Stdout)" | Should -Match 'typed result|missing or blank|retitled=true' -Because $fault
                @(Get-ChildItem -LiteralPath $layout.Failsafe -ErrorAction SilentlyContinue).Count | Should -BeGreaterThan 0 -Because "title recovery retained for $fault"
            } finally {
                Remove-Item Env:P2_TYPED_FAULT -ErrorAction SilentlyContinue
            }
        }

        $env:P2_TYPED_FAULT = 'retitled-false'
        try {
            $title = Invoke-P2Verb -Layout $layout -Method 'workflow.sessionlog.setTurnTitle' -ParamsYaml "queryTitle: Renamed`n"
            $title.ExitCode | Should -Not -Be 0
            "$($title.Stderr)$($title.Stdout)" | Should -Match 'retitled=true|typed result'
            @(Get-ChildItem -LiteralPath $layout.Failsafe -ErrorAction SilentlyContinue).Count | Should -BeGreaterThan 0
        } finally {
            Remove-Item Env:P2_TYPED_FAULT -ErrorAction SilentlyContinue
        }
    }
    It 'proves unit inventory excludes PluginIntegration and keeps SessionLife gate scripts' {
        $buildTest = Join-Path $script:RepoRoot 'build\Build.Test.cs'
        $buildText = [System.IO.File]::ReadAllText($buildTest)
        $buildText | Should -Match '\!p\.Name\.Contains\("PluginIntegration"\)'
        $buildText | Should -Match '\!p\.Name\.Contains\("IntegrationTests"\)'
        $buildText | Should -Match 'Category!=Integration'
        $gate = Join-Path $script:RepoRoot 'tools\validation\Invoke-SessionLifeUnitGate.ps1'
        Test-Path -LiteralPath $gate | Should -BeTrue
        $gateText = [System.IO.File]::ReadAllText($gate)
        $gateText | Should -Match 'PluginIntegration'
        $gateText | Should -Match 'deliberately excluded'
        $validator = Join-Path $script:RepoRoot 'tests\Build.Tests\SessionLifeUnitGateValidatorTests.cs'
        $consumer = Join-Path $script:RepoRoot 'tests\Build.Tests\SessionLifeUnitGateConsumerTests.cs'
        Test-Path -LiteralPath $validator | Should -BeTrue
        Test-Path -LiteralPath $consumer | Should -BeTrue
        $vText = [System.IO.File]::ReadAllText($validator)
        $vText | Should -Match 'Validate_SharedInvalidArtifact_IsRejectedInUnitLane'
        $vText | Should -Match 'Validate_UnitOnlyInvalidArtifact_IsRejected'
        $cText = [System.IO.File]::ReadAllText($consumer)
        $cText | Should -Match 'ValidateSessionLifeUnitGate_DelegatesExactRequestExactlyOnce'
        $manifest = Get-Content -LiteralPath (Join-Path $script:RepoRoot 'docs\receipts\sessionlife-completion\20260928-p0-r3\acceptance-manifest.json') -Raw | ConvertFrom-Json
        $todos = @($manifest.todoAcceptanceRows)
        $todos.Count | Should -Be 35
        @($todos | Where-Object { $_.acceptanceState -ne 'not-accepted' -or $_.done -eq $true }).Count | Should -Be 0
    }

    It 'HV16 case-different caller requestId is rejected without mutation' {

        $layout = New-P2Layout -Name 'hv16-case'

        Write-P2Turn -Layout $layout -RequestId 'req-20261001T000000Z-casea'

        $before = Get-FileHash -LiteralPath (Join-Path $layout.Cache 'current-turn.yaml') -Algorithm SHA256

        $result = Invoke-P2Verb -Layout $layout -Method 'workflow.sessionlog.updateTurn' -ParamsYaml "requestId: req-20261001T000000Z-caseA`nresponse: should-not-write`n" -Mode 'primary'

        $result.ExitCode | Should -Not -Be 0

        Assert-P2Receipt -Receipt $result.Receipt -Method 'workflow.sessionlog.updateTurn' -RequestId 'req-20261001T000000Z-casea' -Code 'rejected'

        $after = Get-FileHash -LiteralPath (Join-Path $layout.Cache 'current-turn.yaml') -Algorithm SHA256

        $after.Hash | Should -Be $before.Hash

        $result.ServerState | Should -Be ''

    }



    It 'HV17 case-different typed sessionId or requestId is not primary' {

        $layout = New-P2Layout -Name 'hv17-typed-case'

        Write-P2Turn -Layout $layout -RequestId 'req-R'

        foreach ($fault in @('case-session','case-request')) {

            $env:P2_TYPED_FAULT = $fault

            try {

                $dlg = Invoke-P2Verb -Layout $layout -Method 'workflow.sessionlog.appendDialog' -ParamsYaml "dialogItems:`n  - role: model`n    content: x`n" -Mode 'primary'

                $dlg.ExitCode | Should -Not -Be 0

                $title = Invoke-P2Verb -Layout $layout -Method 'workflow.sessionlog.setTurnTitle' -ParamsYaml "title: new title`n" -Mode 'primary'

                $title.ExitCode | Should -Not -Be 0

            } finally { Remove-Item Env:P2_TYPED_FAULT -ErrorAction SilentlyContinue }

        }

    }



    It 'rejects explicit empty and null planFile on ordinary first persistence' {

        $layout = New-P2Layout -Name 'raw-meta'

        $empty = Invoke-P2Verb -Layout $layout -Method 'workflow.sessionlog.beginTurn' -ParamsYaml "requestId: req-raw-empty`nqueryTitle: t`nqueryText: q`nplanFile: `"`"`ntodoId: None`n" -Mode 'primary'

        $empty.ExitCode | Should -Not -Be 0

        $nullish = Invoke-P2Verb -Layout $layout -Method 'workflow.sessionlog.beginTurn' -ParamsYaml "requestId: req-raw-null`nqueryTitle: t`nqueryText: q`nplanFile: null`ntodoId: None`n" -Mode 'primary'

        $nullish.ExitCode | Should -Not -Be 0

    }



    It 'HV19 case-different SubmitAsync response identity is not persisted and keeps recovery' {
        foreach ($fault in @('case-session','case-request')) {
            foreach ($verb in @(
                @{ Method = 'workflow.sessionlog.updateTurn'; Yaml = "requestId: req-p2-verb`nresponse: should-not-clear`n" },
                @{ Method = 'workflow.sessionlog.appendActions'; Yaml = "requestId: req-p2-verb`nactions:`n  - type: design_decision`n    description: note`n" },
                @{ Method = 'workflow.sessionlog.completeTurn'; Yaml = "requestId: req-p2-verb`nresponse: done`n" },
                @{ Method = 'workflow.sessionlog.failTurn'; Yaml = "requestId: req-p2-verb`nerrorMessage: boom`n" }
            )) {
                $layout = New-P2Layout -Name ("hv19-" + $fault + "-" + ($verb.Method -replace '\W',''))
                Write-P2Turn -Layout $layout -RequestId 'req-p2-verb'
                $turnPath = Join-Path $layout.Cache 'current-turn.yaml'
                $turn = Read-McpYamlObject -Path $turnPath
                $turn.persisted = 'true'
                Write-McpYamlObject -Path $turnPath -Document $turn
                $env:P2_TYPED_FAULT = $fault
                try {
                    $result = Invoke-P2Verb -Layout $layout -Method $verb.Method -ParamsYaml $verb.Yaml -Mode 'primary'
                    $result.ExitCode | Should -Not -Be 0
                    Assert-P2Receipt -Receipt $result.Receipt -Method $verb.Method -RequestId 'req-p2-verb' -Code 'rejected'
                    $fails = @(Get-ChildItem -LiteralPath $layout.Failsafe -File -ErrorAction SilentlyContinue)
                    $fails.Count | Should -BeGreaterThan 0
                    Test-Path -LiteralPath $turnPath | Should -BeTrue
                    $after = Read-McpYamlObject -Path $turnPath
                    [string]$after.turnRequestId | Should -Be 'req-p2-verb'
                } finally { Remove-Item Env:P2_TYPED_FAULT -ErrorAction SilentlyContinue }
            }
        }
    }


    It 'HV20 beginTurn case-different request or session does not durable-reopen overwrite' {
        # case-request: caller requestId differs only by case from bound turn
        $layout = New-P2Layout -Name 'hv20-case-request'
        Write-P2Turn -Layout $layout -RequestId 'req-20261001T000000Z-casea'
        $turnPath = Join-Path $layout.Cache 'current-turn.yaml'
        $turn = Read-McpYamlObject -Path $turnPath
        $turn.persisted = 'true'
        $turn.planFile = 'docs/plans/bound.md'
        $turn.todoId = 'TODO-BOUND'
        Write-McpYamlObject -Path $turnPath -Document $turn
        $before = Get-FileHash -LiteralPath $turnPath -Algorithm SHA256
        $result = Invoke-P2Verb -Layout $layout -Method 'workflow.sessionlog.beginTurn' -ParamsYaml "requestId: req-20261001T000000Z-caseA`nqueryTitle: overwrite?`nqueryText: must not reopen`n" -Mode 'primary'
        $result.ExitCode | Should -Not -Be 0
        Assert-P2Receipt -Receipt $result.Receipt -Method 'workflow.sessionlog.beginTurn' -RequestId 'req-20261001T000000Z-casea' -Code 'rejected'
        $afterHash = (Get-FileHash -LiteralPath $turnPath -Algorithm SHA256).Hash
        $afterHash | Should -Be $before.Hash
        $after = Read-McpYamlObject -Path $turnPath
        [string]$after.turnRequestId | Should -Be 'req-20261001T000000Z-casea'
        [string]$after.planFile | Should -Be 'docs/plans/bound.md'

        # case-session: session-state casing differs from bound turn sessionId
        $layout2 = New-P2Layout -Name 'hv20-case-session'
        Write-P2Turn -Layout $layout2 -RequestId 'req-20261001T000000Z-casea'
        $turnPath2 = Join-Path $layout2.Cache 'current-turn.yaml'
        $turn2 = Read-McpYamlObject -Path $turnPath2
        $turn2.persisted = 'true'
        $turn2.sessionId = 'Codex-20260930T000000Z-sessiona'
        $turn2.planFile = 'docs/plans/bound.md'
        Write-McpYamlObject -Path $turnPath2 -Document $turn2
        $sp = Join-Path $layout2.Cache 'session-state.yaml'
        $state = Read-McpYamlObject -Path $sp
        $state.sessionId = 'Codex-20260930T000000Z-sessionA'
        Write-McpYamlObject -Path $sp -Document $state
        $before2 = Get-FileHash -LiteralPath $turnPath2 -Algorithm SHA256
        $result2 = Invoke-P2Verb -Layout $layout2 -Method 'workflow.sessionlog.beginTurn' -ParamsYaml "requestId: req-20261001T000000Z-casea`nqueryTitle: overwrite?`nqueryText: must not reopen`n" -Mode 'primary'
        $result2.ExitCode | Should -Not -Be 0
        Assert-P2Receipt -Receipt $result2.Receipt -Method 'workflow.sessionlog.beginTurn' -RequestId 'req-20261001T000000Z-casea' -Code 'rejected'
        (Get-FileHash -LiteralPath $turnPath2 -Algorithm SHA256).Hash | Should -Be $before2.Hash
        $after2 = Read-McpYamlObject -Path $turnPath2
        [string]$after2.sessionId | Should -Be 'Codex-20260930T000000Z-sessiona'
        [string]$after2.planFile | Should -Be 'docs/plans/bound.md'
    }

}
