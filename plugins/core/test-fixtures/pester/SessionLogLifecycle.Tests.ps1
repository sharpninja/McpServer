#Requires -Version 7.0

# TEST-MCP-SESSIONLIFE-001 / FR-MCP-SESSIONLIFE-001
# Drives the shipped Complete-ReplBeginTurnAfterPersist function. No persist-log shortcut.

Describe 'FR-MCP-SESSIONLIFE-001 degraded begin keeps the turn cache' {
    BeforeAll {
        $script:RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')).ProviderPath
        $script:ReplScript = Join-Path $script:RepoRoot 'plugins\core\lib-ps\repl-invoke.ps1'
        . $script:ReplScript
        if ($env:MCP_PLUGIN_PERSIST_LOG) {
            Remove-Item Env:MCP_PLUGIN_PERSIST_LOG
        }
        function Invoke-ReplRaw {
            return $script:DialogResult
        }
        function Invoke-ReplPersistTurn {
            param(
                [Parameter(Mandatory)][string]$RequestId,
                [AllowEmptyString()][string]$Title = '',
                [string]$Status = '',
                [string]$ResponseText = '',
                [switch]$IncludeSessionTitle,
                [string]$PlanFile,
                [string]$TodoId
            )
            $script:LastPersistBoundPlan = $PSBoundParameters.ContainsKey('PlanFile')
            $script:LastPersistPlan = $PlanFile
            $script:LastPersistTodo = $TodoId
            if ($script:ForceDegradedPersist) {
                $script:LastReplPersistenceDetails = [ordered]@{
                    degraded = $true
                    persisted = $false
                    queued = $true
                    failsafePath = ''
                }
                return $false
            }
            return $true
        }
    }

    It 'does not replace queryText queryTitle planFile todoId openedAt or audit counters' {
        $dir = Join-Path ([System.IO.Path]::GetTempPath()) ('sessionlife-' + [guid]::NewGuid().ToString('N'))
        [void][System.IO.Directory]::CreateDirectory($dir)
        $turn = Join-Path $dir 'current-turn.yaml'
        @(
            'turnRequestId: req-20260923T205606Z-001-life'
            'sessionId: GrokCode-20260923T205606Z-life'
            'status: in_progress'
            'queryText: keep me'
            'queryTitle: kept title'
            'planFile: docs/plans/foo.md'
            'todoId: BUG-TRIAGE-175'
            'openedAt: 2026-09-23T20:00:00Z'
            'auditDialog: 2'
            'auditActions: 1'
        ) | Set-Content -LiteralPath $turn -Encoding utf8

        $result = Complete-ReplBeginTurnAfterPersist -Persisted $false -Degraded $true -FailsafePath (Join-Path $dir 'missing.yaml') -CurrentTurnFile $turn -TurnState @{
            turnRequestId = 'req-20260923T205606Z-001-life'
            sessionId = 'GrokCode-20260923T205606Z-life'
        }

        $result.degraded | Should -BeTrue
        $text = Get-Content -LiteralPath $turn -Raw
        $text | Should -Match 'queryText: keep me'
        $text | Should -Match 'queryTitle: kept title'
        $text | Should -Match 'planFile: docs/plans/foo.md'
        $text | Should -Match 'todoId: BUG-TRIAGE-175'
        $text | Should -Match 'openedAt: 2026-09-23T20:00:00Z'
        $text | Should -Match 'auditDialog: 2'
        $text | Should -Match 'auditActions: 1'
        $text | Should -Match 'degraded: true'
    }

    It 'complete recovery uses cached queryText then queryTitle then the placeholder' {
        $dir = Join-Path ([System.IO.Path]::GetTempPath()) ('sessionlife-q-' + [guid]::NewGuid().ToString('N'))
        [void][System.IO.Directory]::CreateDirectory($dir)
        $prior = $env:MCP_CACHE_DIR_OVERRIDE
        $env:MCP_CACHE_DIR_OVERRIDE = $dir
        try {
            Write-McpYamlObject -Path (Join-Path $dir 'current-turn.yaml') -Document ([ordered]@{
                queryTitle = 'kept title'
                openedAt = '2026-09-23T20:00:00Z'
            })
            $completed = Invoke-ReplTurnUpsertParams -SourceType 'GrokCode' -SessionId 's' -RequestId 'r' -Title '' -Status 'completed'
            $completed.turn.queryText | Should -Be 'kept title'
            $completed.turn.Contains('queryTitle') | Should -BeFalse

            Write-McpYamlObject -Path (Join-Path $dir 'current-turn.yaml') -Document ([ordered]@{
                openedAt = '2026-09-23T20:00:00Z'
            })
            $placeholder = Invoke-ReplTurnUpsertParams -SourceType 'GrokCode' -SessionId 's' -RequestId 'r' -Title '' -Status 'completed'
            $placeholder.turn.queryText | Should -Be 'Recovered session-log turn'
            $placeholder.turn.Contains('queryTitle') | Should -BeFalse
        } finally {
            if ($null -eq $prior) { Remove-Item Env:MCP_CACHE_DIR_OVERRIDE -ErrorAction SilentlyContinue } else { $env:MCP_CACHE_DIR_OVERRIDE = $prior }
            Remove-Item -LiteralPath $dir -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'update omits an empty queryText so the server value can stay' {
        $dir = Join-Path ([System.IO.Path]::GetTempPath()) ('sessionlife-u-' + [guid]::NewGuid().ToString('N'))
        [void][System.IO.Directory]::CreateDirectory($dir)
        $prior = $env:MCP_CACHE_DIR_OVERRIDE
        $env:MCP_CACHE_DIR_OVERRIDE = $dir
        try {
            Write-McpYamlObject -Path (Join-Path $dir 'current-turn.yaml') -Document ([ordered]@{
                queryTitle = 'server title'
                openedAt = '2026-09-23T20:00:00Z'
            })
            $updated = Invoke-ReplTurnUpsertParams -SourceType 'GrokCode' -SessionId 's' -RequestId 'r' -Title '' -Status 'in_progress'
            $updated.turn.Contains('queryText') | Should -BeFalse
        } finally {
            if ($null -eq $prior) { Remove-Item Env:MCP_CACHE_DIR_OVERRIDE -ErrorAction SilentlyContinue } else { $env:MCP_CACHE_DIR_OVERRIDE = $prior }
            Remove-Item -LiteralPath $dir -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'degraded 404 queues a session_dialog failsafe and does not say failsafe not used' {
        $dir = Join-Path ([System.IO.Path]::GetTempPath()) ('sessionlife-d-' + [guid]::NewGuid().ToString('N'))
        [void][System.IO.Directory]::CreateDirectory($dir)
        $prior = $env:MCP_CACHE_DIR_OVERRIDE
        $env:MCP_CACHE_DIR_OVERRIDE = $dir
        $env:MCP_WORKSPACE_PATH = $script:RepoRoot
        $script:DialogResult = @{ Success = $false; Output = 'HTTP 404'; Error = 'not_found' }
        try {
            Write-McpYamlObject -Path (Join-Path $dir 'session-state.yaml') -Document ([ordered]@{
                sessionId = 'GrokCode-20260923T205606Z-life'
                status = 'verified'
            })
            Write-McpYamlObject -Path (Join-Path $dir 'current-turn.yaml') -Document ([ordered]@{
                turnRequestId = 'req-20260923T205606Z-001-life'
                sessionId = 'GrokCode-20260923T205606Z-life'
                status = 'in_progress'
                degraded = $true
                queryText = 'keep me'
            })
            $ok = Invoke-WorkflowAppendDialog -ParamsYaml "dialogItems:`n  - role: model`n    content: hello`n"
            $ok | Should -BeTrue
            $script:LastReplPersistenceDetails.queued | Should -BeTrue
            $script:LastReplPersistenceDetails.message | Should -Not -Match 'failsafe not used'
            @(Get-ChildItem -LiteralPath (Get-McpFailsafeDir) -Filter '*session_dialog*' -ErrorAction SilentlyContinue).Count | Should -BeGreaterThan 0
        } finally {
            if ($null -eq $prior) { Remove-Item Env:MCP_CACHE_DIR_OVERRIDE -ErrorAction SilentlyContinue } else { $env:MCP_CACHE_DIR_OVERRIDE = $prior }
            Remove-Item -LiteralPath $dir -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'same-request degraded beginTurn sends cached planFile and todoId' {
        $dir = Join-Path ([System.IO.Path]::GetTempPath()) ('sessionlife-re-' + [guid]::NewGuid().ToString('N'))
        [void][System.IO.Directory]::CreateDirectory($dir)
        $prior = $env:MCP_CACHE_DIR_OVERRIDE
        $env:MCP_CACHE_DIR_OVERRIDE = $dir
        $env:MCP_WORKSPACE_PATH = $script:RepoRoot
        $script:ForceDegradedPersist = $true
        try {
            Write-McpYamlObject -Path (Join-Path $dir 'session-state.yaml') -Document ([ordered]@{
                sessionId = 'GrokCode-20260923T205606Z-life'
                status = 'verified'
            })
            Write-McpYamlObject -Path (Join-Path $dir 'current-turn.yaml') -Document ([ordered]@{
                turnRequestId = 'req-20260923T205606Z-001-life'
                sessionId = 'GrokCode-20260923T205606Z-life'
                status = 'in_progress'
                degraded = $true
                planFile = 'docs/plans/kept.md'
                todoId = 'BUG-TRIAGE-245'
                queryText = 'keep me'
            })
            $ok = Invoke-WorkflowBeginTurn -ParamsYaml "requestId: req-20260923T205606Z-001-life`nqueryText: keep me`nqueryTitle: kept`n"
            $ok | Should -BeTrue
            $script:LastPersistBoundPlan | Should -BeTrue
            $script:LastPersistPlan | Should -Be 'docs/plans/kept.md'
            $script:LastPersistTodo | Should -Be 'BUG-TRIAGE-245'
        } finally {
            $script:ForceDegradedPersist = $false
            if ($null -eq $prior) { Remove-Item Env:MCP_CACHE_DIR_OVERRIDE -ErrorAction SilentlyContinue } else { $env:MCP_CACHE_DIR_OVERRIDE = $prior }
            Remove-Item -LiteralPath $dir -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'a child that does not exit is killed inside the REPL timeout' {
        $dir = Join-Path ([System.IO.Path]::GetTempPath()) ('sessionlife-child-' + [guid]::NewGuid().ToString('N'))
        [void][System.IO.Directory]::CreateDirectory($dir)
        $bat = Join-Path $dir 'sleep.cmd'
        Set-Content -LiteralPath $bat -Value "@echo off`r`nping -n 40 127.0.0.1 >nul`r`n" -Encoding ascii
        $priorExe = $env:MCP_REPL_EXECUTABLE
        $priorTimeout = $env:REPL_TIMEOUT
        $env:MCP_REPL_EXECUTABLE = $bat
        $env:REPL_TIMEOUT = '2'
        $clock = [System.Diagnostics.Stopwatch]::StartNew()
        try {
            $result = Invoke-ReplRawCore -Method 'client.Health.GetAsync' -ParamsYaml ''
            $clock.Stop()
            $result.Success | Should -BeFalse
            $result.Error | Should -Match 'timed out'
            $clock.Elapsed.TotalSeconds | Should -BeLessThan 12
        } finally {
            if ($null -eq $priorExe) { Remove-Item Env:MCP_REPL_EXECUTABLE -ErrorAction SilentlyContinue } else { $env:MCP_REPL_EXECUTABLE = $priorExe }
            if ($null -eq $priorTimeout) { Remove-Item Env:REPL_TIMEOUT -ErrorAction SilentlyContinue } else { $env:REPL_TIMEOUT = $priorTimeout }
            Remove-Item -LiteralPath $dir -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'drain repairs a numeric-keyed turns map and deletes the record after success' {
        $dir = Join-Path ([System.IO.Path]::GetTempPath()) ('sessionlife-qrepair-' + [guid]::NewGuid().ToString('N'))
        [void][System.IO.Directory]::CreateDirectory($dir)
        $prior = $env:MCP_CACHE_DIR_OVERRIDE
        $priorFailsafe = $env:MCPSERVER_FAILSAFE_DIR
        $env:MCP_CACHE_DIR_OVERRIDE = $dir
        $env:MCPSERVER_FAILSAFE_DIR = Join-Path $dir 'failsafe-only'
        $env:MCP_WORKSPACE_PATH = $script:RepoRoot
        $script:DialogResult = @{ Success = $true; Output = "type: result`n"; Error = ''; ExitCode = 0 }
        try {
            $failsafeDir = Get-ReplFailsafeDir
            [void][System.IO.Directory]::CreateDirectory($failsafeDir)
            $record = Join-Path $failsafeDir '20260722T232406Z-session_submit-3358.yaml'
            Write-McpYamlObject -Path $record -Document ([ordered]@{
                method = 'client.SessionLog.SubmitAsync'
                drainAttempts = 9
                params = [ordered]@{
                    turns = [ordered]@{
                        '0' = [ordered]@{ requestId = 'req-only' }
                    }
                }
            })
            $summary = Invoke-ReplFailsafeDrain -MaxAttempts 5
            $summary.replayed | Should -Be 1
            Test-Path -LiteralPath $record | Should -BeFalse
            (Test-ReplFailsafeBackendUnreachable -Detail 'SQLite Error 5: database is locked') | Should -BeTrue
        } finally {
            if ($null -eq $prior) { Remove-Item Env:MCP_CACHE_DIR_OVERRIDE -ErrorAction SilentlyContinue } else { $env:MCP_CACHE_DIR_OVERRIDE = $prior }
            if ($null -eq $priorFailsafe) { Remove-Item Env:MCPSERVER_FAILSAFE_DIR -ErrorAction SilentlyContinue } else { $env:MCPSERVER_FAILSAFE_DIR = $priorFailsafe }
            Remove-Item -LiteralPath $dir -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'drain fills missing metadata with None and keeps the file when submit fails' {
        $dir = Join-Path ([System.IO.Path]::GetTempPath()) ('sessionlife-meta-repair-' + [guid]::NewGuid().ToString('N'))
        [void][System.IO.Directory]::CreateDirectory($dir)
        $prior = $env:MCP_CACHE_DIR_OVERRIDE
        $priorFailsafe = $env:MCPSERVER_FAILSAFE_DIR
        $env:MCP_CACHE_DIR_OVERRIDE = $dir
        $env:MCPSERVER_FAILSAFE_DIR = Join-Path $dir 'failsafe-only'
        $env:MCP_WORKSPACE_PATH = $script:RepoRoot
        $script:DialogResult = @{ Success = $false; Output = ''; Error = 'validation rejected the turn'; ExitCode = 1 }
        try {
            $failsafeDir = Get-ReplFailsafeDir
            [void][System.IO.Directory]::CreateDirectory($failsafeDir)
            $record = Join-Path $failsafeDir '20260817T123219Z-session_submit-9ad7.yaml'
            Write-McpYamlObject -Path $record -Document ([ordered]@{
                method = 'client.SessionLog.SubmitAsync'
                drainAttempts = 0
                params = [ordered]@{
                    turns = @(
                        [ordered]@{ requestId = 'req-missing' }
                        [ordered]@{ requestId = 'req-kept'; planFile = 'docs/plans/keep.md'; todoId = 'PLAN-KEEP-001' }
                    )
                }
            })
            $summary = Invoke-ReplFailsafeDrain -MaxAttempts 5
            $summary.failed | Should -Be 1
            Test-Path -LiteralPath $record | Should -BeTrue
            $kept = Read-McpYamlObject -Path $record
            [int]$kept.drainAttempts | Should -Be 1
            [string]$kept.params.turns[0].planFile | Should -Be 'None'
            [string]$kept.params.turns[0].todoId | Should -Be 'None'
            [string]$kept.params.turns[1].planFile | Should -Be 'docs/plans/keep.md'
            [string]$kept.params.turns[1].todoId | Should -Be 'PLAN-KEEP-001'
        } finally {
            if ($null -eq $prior) { Remove-Item Env:MCP_CACHE_DIR_OVERRIDE -ErrorAction SilentlyContinue } else { $env:MCP_CACHE_DIR_OVERRIDE = $prior }
            if ($null -eq $priorFailsafe) { Remove-Item Env:MCPSERVER_FAILSAFE_DIR -ErrorAction SilentlyContinue } else { $env:MCPSERVER_FAILSAFE_DIR = $priorFailsafe }
            Remove-Item -LiteralPath $dir -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'a locked database leaves the failsafe, the attempt count, and the drain latch' {
        $priorDrainDisabled = $env:MCP_FAILSAFE_DRAIN_DISABLED
        $env:MCP_FAILSAFE_DRAIN_DISABLED = '0'
        $dir = Join-Path ([System.IO.Path]::GetTempPath()) ('sessionlife-lock-' + [guid]::NewGuid().ToString('N'))
        [void][System.IO.Directory]::CreateDirectory($dir)
        $prior = $env:MCP_CACHE_DIR_OVERRIDE
        $priorFailsafe = $env:MCPSERVER_FAILSAFE_DIR
        $env:MCP_CACHE_DIR_OVERRIDE = $dir
        $env:MCPSERVER_FAILSAFE_DIR = Join-Path $dir 'failsafe-only'
        $env:MCP_WORKSPACE_PATH = $script:RepoRoot
        $script:DialogResult = @{ Success = $false; Output = ''; Error = 'SQLite Error 5: database is locked'; ExitCode = 1 }
        $script:ReplFailsafeDrainCompleted = $false
        try {
            $failsafeDir = Get-ReplFailsafeDir
            [void][System.IO.Directory]::CreateDirectory($failsafeDir)
            $record = Join-Path $failsafeDir '20260817T123057Z-session_submit-d5f9.yaml'
            Write-McpYamlObject -Path $record -Document ([ordered]@{
                method = 'client.SessionLog.SubmitAsync'
                drainAttempts = 2
                params = [ordered]@{
                    turns = @(
                        [ordered]@{ requestId = 'req-lock'; planFile = 'docs/plans/keep.md'; todoId = 'PLAN-KEEP-001' }
                    )
                }
            })
            $locked = Invoke-WorkflowFailsafeDrain -ParamsYaml 'maxAttempts: 5'
            $locked.Success | Should -BeFalse
            $script:ReplFailsafeDrainCompleted | Should -BeFalse
            Test-Path -LiteralPath $record | Should -BeTrue
            $kept = Read-McpYamlObject -Path $record
            [int]$kept.drainAttempts | Should -Be 2
            [string]$kept.params.turns[0].planFile | Should -Be 'docs/plans/keep.md'
            [string]$kept.params.turns[0].todoId | Should -Be 'PLAN-KEEP-001'

            $script:DialogResult = @{ Success = $true; Output = "type: result`n"; Error = ''; ExitCode = 0 }
            Invoke-ReplFailsafeDrainOnFirstSuccess
            Test-Path -LiteralPath $record | Should -BeFalse
        } finally {
            [Environment]::SetEnvironmentVariable('MCP_FAILSAFE_DRAIN_DISABLED', $priorDrainDisabled, 'Process')
            if ($null -eq $prior) { Remove-Item Env:MCP_CACHE_DIR_OVERRIDE -ErrorAction SilentlyContinue } else { $env:MCP_CACHE_DIR_OVERRIDE = $prior }
            if ($null -eq $priorFailsafe) { Remove-Item Env:MCPSERVER_FAILSAFE_DIR -ErrorAction SilentlyContinue } else { $env:MCPSERVER_FAILSAFE_DIR = $priorFailsafe }
            Remove-Item -LiteralPath $dir -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}
