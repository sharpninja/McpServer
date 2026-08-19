# TEST-MCP-TRIAGEPLUGIN-001..005: plugin root session, cache resolve, beginTurn degraded, completeTurn rebind.
#Requires -Version 7.0

Describe 'TRIAGEPLUGIN identity and timeouts' {
    BeforeAll {
        $script:RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')).ProviderPath
        $script:LibRoot = Join-Path $script:RepoRoot 'plugins\core\lib-ps'
        . (Join-Path $script:LibRoot 'resolve-cache-dir.ps1')
        $script:ReplInvokeSource = Get-Content -LiteralPath (Join-Path $script:LibRoot 'repl-invoke.ps1') -Raw
        $script:PluginEnvSource = Get-Content -LiteralPath (Join-Path $script:LibRoot 'plugin-env.ps1') -Raw
        $script:PluginHookSource = Get-Content -LiteralPath (Join-Path $script:LibRoot 'plugin-hook.ps1') -Raw
    }

    It 'Resolve-McpCacheDir uses hook workspace path when cwd is the user profile and env is empty' {
        $workspace = Join-Path $TestDrive 'hook-workspace'
        New-Item -ItemType Directory -Path $workspace | Out-Null
        Set-Content -LiteralPath (Join-Path $workspace 'AGENTS-README-FIRST.yaml') -Value "workspace: test`n"
        $saved = Get-Location
        $savedCache = $env:MCP_CACHE_DIR_OVERRIDE
        $savedWs = $env:MCP_WORKSPACE_PATH
        $savedStart = $env:MCP_WORKSPACE_START_DIR
        try {
            $env:MCP_CACHE_DIR_OVERRIDE = $null
            $env:MCP_WORKSPACE_PATH = $null
            $env:MCPSERVER_WORKSPACE_PATH = $null
            $env:MCP_WORKSPACE_START_DIR = $null
            Set-Location $HOME
            $dir = Resolve-McpCacheDir -StartPath $workspace
            $dir | Should -Match 'hook-workspace'
        } finally {
            Set-Location $saved
            $env:MCP_CACHE_DIR_OVERRIDE = $savedCache
            $env:MCP_WORKSPACE_PATH = $savedWs
            $env:MCP_WORKSPACE_START_DIR = $savedStart
        }
    }

    It 'Get-ReplMethodTimeoutSeconds treats workflow.agenthelp.submitTurn as a long helper timeout' {
        $fn = [regex]::Match(
            $script:ReplInvokeSource,
            '(?ms)^function Get-ReplMethodTimeoutSeconds \{.*?^\}').Value
        $fn | Should -Not -BeNullOrEmpty
        Invoke-Expression $fn
        $savedHelper = $env:REPL_HELPER_TIMEOUT
        $savedDefault = $env:REPL_TIMEOUT
        try {
            $env:REPL_HELPER_TIMEOUT = $null
            $env:REPL_TIMEOUT = $null
            Get-ReplMethodTimeoutSeconds -Method 'workflow.agenthelp.submitTurn' | Should -Be 120
        } finally {
            $env:REPL_HELPER_TIMEOUT = $savedHelper
            $env:REPL_TIMEOUT = $savedDefault
        }
    }

    It 'Get-ReplMethodTimeoutSeconds keeps sessionlog methods on the short default' {
        $fn = [regex]::Match(
            $script:ReplInvokeSource,
            '(?ms)^function Get-ReplMethodTimeoutSeconds \{.*?^\}').Value
        $fn | Should -Not -BeNullOrEmpty
        Invoke-Expression $fn
        $savedHelper = $env:REPL_HELPER_TIMEOUT
        $savedDefault = $env:REPL_TIMEOUT
        try {
            $env:REPL_HELPER_TIMEOUT = $null
            $env:REPL_TIMEOUT = $null
            Get-ReplMethodTimeoutSeconds -Method 'workflow.sessionlog.beginTurn' | Should -Be 30
        } finally {
            $env:REPL_HELPER_TIMEOUT = $savedHelper
            $env:REPL_TIMEOUT = $savedDefault
        }
    }

    It 'CacheScope.BackgroundOpenSession_DoesNotRebindRootActiveSession' {
        $pathFn = [regex]::Match($script:ReplInvokeSource, '(?ms)^function Get-ReplOpenSessionStatePath \{.*?^\}').Value
        $writeFn = [regex]::Match($script:ReplInvokeSource, '(?ms)^function Write-ReplStickySessionState \{.*?^\}').Value
        $pathFn | Should -Not -BeNullOrEmpty
        $writeFn | Should -Not -BeNullOrEmpty
        Invoke-Expression $pathFn
        Invoke-Expression $writeFn
        $cache = Join-Path $TestDrive 'cache-root'
        New-Item -ItemType Directory -Path $cache | Out-Null
        $rootFile = Join-Path $cache 'session-state.yaml'
        Set-Content -LiteralPath $rootFile -Value "sessionId: GrokCode-20260818T000000Z-root`nstatus: verified`n"
        $openFn = [regex]::Match($script:ReplInvokeSource, '(?ms)^function Invoke-WorkflowOpenSession \{.*?^\}').Value
        $openFn | Should -Not -BeNullOrEmpty
        Invoke-Expression $openFn
        Invoke-WorkflowOpenSession -CacheDir $cache -RootSessionId 'GrokCode-20260818T000000Z-root' -SessionId 'GrokCode-20260818T000000Z-child' | Should -BeTrue
        $childFile = Write-ReplStickySessionState -CacheDir $cache -RootSessionId 'GrokCode-20260818T000000Z-root' -SessionId 'GrokCode-20260818T000000Z-child'
        $childFile | Should -Match 'sessions'
        (Get-Content -LiteralPath $rootFile -Raw) | Should -Match 'GrokCode-20260818T000000Z-root'
        (Get-Content -LiteralPath $rootFile -Raw) | Should -Not -Match 'GrokCode-20260818T000000Z-child'
        (Get-Content -LiteralPath $childFile -Raw) | Should -Match 'GrokCode-20260818T000000Z-child'
    }

    It 'PluginCache.ReplaceWhileTurnOpen_ResolvesOrNamedDrift' {
        $fn = [regex]::Match(
            $script:PluginEnvSource,
            '(?ms)^function Get-PluginCacheVersionDriftMessage \{.*?^function Resolve-PluginCacheOrVersionDrift \{.*?^\}').Value
        $fn | Should -Not -BeNullOrEmpty
        Invoke-Expression $fn
        $good = Join-Path $TestDrive 'plugin-good'
        New-Item -ItemType Directory -Path $good | Out-Null
        Resolve-PluginCacheOrVersionDrift -ConfiguredRoot $good -ReplacementRoot $null | Should -Be $good
        $replacement = Join-Path $TestDrive 'plugin-new'
        New-Item -ItemType Directory -Path $replacement | Out-Null
        Resolve-PluginCacheOrVersionDrift -ConfiguredRoot (Join-Path $TestDrive 'missing') -ReplacementRoot $replacement | Should -Be $replacement
        { Resolve-PluginCacheOrVersionDrift -ConfiguredRoot (Join-Path $TestDrive 'missing') -ReplacementRoot (Join-Path $TestDrive 'also-missing') } | Should -Throw '*version-drift*'
    }

    It 'BeginTurn.SubmitTimeoutAfterFailsafe_ReturnsDegradedQueued' {
        $testFn = [regex]::Match($script:ReplInvokeSource, '(?ms)^function Test-ReplBeginTurnDegradedQueued \{.*?^\}').Value
        $completeFn = [regex]::Match($script:ReplInvokeSource, '(?ms)^function Complete-ReplBeginTurnAfterPersist \{.*?^\}').Value
        $testFn | Should -Not -BeNullOrEmpty
        $completeFn | Should -Not -BeNullOrEmpty
        Invoke-Expression $testFn
        Invoke-Expression $completeFn
        $failsafe = Join-Path $TestDrive 'failsafe-queue'
        New-Item -ItemType Directory -Path $failsafe | Out-Null
        $queued = Join-Path $failsafe '20260818T000000Z-session_submit-0001.yaml'
        Set-Content -LiteralPath $queued -Value "method: client.SessionLog.SubmitAsync`nlabel: session_submit`n"
        $turnFile = Join-Path $TestDrive 'current-turn.yaml'
        $result = Complete-ReplBeginTurnAfterPersist -Persisted $false -Degraded $true -FailsafePath $queued -CurrentTurnFile $turnFile -TurnState @{
            turnRequestId = 'req-20260818T000000Z-001-begin'
            sessionId = 'GrokCode-20260818T000000Z-root'
        }
        $result.ok | Should -BeTrue
        $result.degraded | Should -BeTrue
        $result.failsafeRetained | Should -BeTrue
        Test-Path -LiteralPath $queued | Should -BeTrue
        Test-Path -LiteralPath $turnFile | Should -BeTrue
        (Get-Content -LiteralPath $turnFile -Raw) | Should -Match 'req-20260818T000000Z-001-begin'
    }

    It 'CompleteTurn.SessionIdRebind_PersistsAndClearsFailsafe' {
        $idFn = [regex]::Match($script:ReplInvokeSource, '(?ms)^function Get-ReplCompleteTurnPersistSessionId \{.*?^\}').Value
        $clearFn = [regex]::Match($script:ReplInvokeSource, '(?ms)^function Clear-ReplFailsafe \{.*?^\}').Value
        $idFn | Should -Not -BeNullOrEmpty
        $clearFn | Should -Not -BeNullOrEmpty
        Invoke-Expression $idFn
        $script:ReplFailsafeInFlight = [System.Collections.Generic.List[string]]::new()
        Invoke-Expression $clearFn
        $original = Get-ReplCompleteTurnPersistSessionId -CurrentTurnSessionId 'GrokCode-20260818T000000Z-turn' -ActiveSessionId 'GrokCode-20260818T000000Z-root'
        $original | Should -Be 'GrokCode-20260818T000000Z-turn'
        $script:ReplInvokeSource | Should -Match 'Do not overwrite it with the rotated active'
        $queued = Join-Path $TestDrive 'complete-failsafe.yaml'
        Set-Content -LiteralPath $queued -Value "method: client.SessionLog.SubmitAsync`n"
        $script:ReplFailsafeInFlight.Add($queued)
        Clear-ReplFailsafe -Path $queued
        Test-Path -LiteralPath $queued | Should -BeFalse
    }

    It 'plugin hook re-asserts workspace identity from hook payload' {
        $fn = [regex]::Match(
            $script:PluginHookSource,
            '(?ms)^function Set-PluginWorkspaceIdentity \{.*?^\}').Value
        $fn | Should -Not -BeNullOrEmpty
        Invoke-Expression $fn
        $workspace = Join-Path $TestDrive 'hook-identity'
        New-Item -ItemType Directory -Path $workspace | Out-Null
        $savedWs = $env:MCP_WORKSPACE_PATH
        $savedMcp = $env:MCPSERVER_WORKSPACE_PATH
        $savedStart = $env:MCP_WORKSPACE_START_DIR
        $savedLoc = Get-Location
        try {
            Set-PluginWorkspaceIdentity -ResolvedPath $workspace
            $env:MCP_WORKSPACE_PATH | Should -Be $workspace
            $env:MCPSERVER_WORKSPACE_PATH | Should -Be $workspace
            $env:MCP_WORKSPACE_START_DIR | Should -Be $workspace
            (Get-Location).ProviderPath | Should -Be $workspace
        } finally {
            $env:MCP_WORKSPACE_PATH = $savedWs
            $env:MCPSERVER_WORKSPACE_PATH = $savedMcp
            $env:MCP_WORKSPACE_START_DIR = $savedStart
            Set-Location $savedLoc
        }
        $script:PluginEnvSource | Should -Match 'version-drift'
    }

    It 'plugin shim preserves classified retryable instead of collapsing to internal_server_error' {
        . (Join-Path $script:LibRoot 'classified-error.ps1')
        $yaml = @"
type: error
payload:
  code: persistence_error
  message: The change could not be saved.
  retryable: true
  details:
    inner: SQLITE_BUSY
"@
        $classified = ConvertTo-McpPluginClassifiedError -Output $yaml -ErrorText 'internal_server_error'
        $classified.code | Should -Be 'persistence_error'
        $classified.retryable | Should -BeTrue
        $classified.preserved | Should -BeTrue
        $classified.code | Should -Not -Be 'internal_server_error'
    }

    It 'UserPromptSubmit.BackgroundPrompt_DoesNotSupersedeRootInProgressTurn' {
        $detectFn = [regex]::Match(
            $script:PluginHookSource,
            '(?ms)^function Test-PluginPromptIsBackgroundAgent \{.*?^\}').Value
        $decisionFn = [regex]::Match(
            $script:PluginHookSource,
            '(?ms)^function Get-PluginRootTurnIsolationDecision \{.*?^\}').Value
        $detectFn | Should -Not -BeNullOrEmpty
        $decisionFn | Should -Not -BeNullOrEmpty
        Invoke-Expression $detectFn
        Invoke-Expression $decisionFn

        $hostile = @'
You are the HOSTILE VALIDATOR for workspace F:\GitHub\McpServer.
FIRST ACTION (mandatory, before any validation):
Execute the add-profile skill now.
'@
        Test-PluginPromptIsBackgroundAgent -Prompt $hostile | Should -BeTrue
        Test-PluginPromptIsBackgroundAgent -Prompt 'Please remediATE the hostile FAIL list' | Should -BeFalse

        $inProgress = [ordered]@{
            turnRequestId = 'req-20260819T153500Z-019-remediate-hook-cache-isolation'
            status = 'in_progress'
            queryText = 'Please remediATE'
        }
        Get-PluginRootTurnIsolationDecision -OpenTurn $inProgress -IncomingPrompt $hostile | Should -Be 'reuse'

        $completed = [ordered]@{
            turnRequestId = 'req-20260819T150100Z-018-complete-resolve-after-hook-cancel'
            status = 'completed'
            queryText = 'Complete resolve after hook canceled 017'
        }
        Get-PluginRootTurnIsolationDecision -OpenTurn $completed -IncomingPrompt $hostile | Should -Be 'isolate-skip'

        Get-PluginRootTurnIsolationDecision -OpenTurn $inProgress -IncomingPrompt 'Please remediATE the hostile FAIL list' | Should -Be 'open-new'
    }

    It 'TEST-MCP-STRICTCOUNT-001 New-McpPluginTurnUpsertRequest accepts omitted empty null and scalar tags under StrictMode' {
        # FR-MCP-STRICTCOUNT-001 / BUG-TRIAGE-158: module StrictMode Latest
        # throws "Count cannot be found" when Tags or ContextList is $null or a
        # single scalar instead of a string[].
        Remove-Module McpPluginShim -Force -ErrorAction SilentlyContinue
        Import-Module (Join-Path $script:LibRoot 'McpPluginShim.psm1') -Force

        $common = @{
            Agent = 'GrokCode'
            SessionId = 'GrokCode-20260819T000000Z-plugin-session'
            RequestId = 'req-20260819T000000Z-001-strictcount'
            Timestamp = '2026-08-19T00:00:00Z'
            QueryText = 'update tags'
            Title = 'update tags'
            Status = 'in_progress'
            Model = 'grok'
        }

        { New-McpPluginTurnUpsertRequest @common } | Should -Not -Throw
        { New-McpPluginTurnUpsertRequest @common -Tags @() -ContextList @() } | Should -Not -Throw
        { New-McpPluginTurnUpsertRequest @common -Tags $null -ContextList $null } | Should -Not -Throw
        { New-McpPluginTurnUpsertRequest @common -Tags 'one-tag' -ContextList 'one-context' } | Should -Not -Throw

        $scalar = New-McpPluginTurnUpsertRequest @common -Tags 'one-tag' -ContextList 'one-context'
        $params = $scalar.ToParamsObject()
        @($params.turn.tags) | Should -Be @('one-tag')
        @($params.turn.contextList) | Should -Be @('one-context')

        $omitted = New-McpPluginTurnUpsertRequest @common
        $omittedMap = $omitted.ToParamsObject().turn
        $omittedMap.Contains('tags') | Should -BeFalse
        $omittedMap.Contains('contextList') | Should -BeFalse
    }

    It 'TEST-MCP-STRICTCOUNT-001 Invoke-WorkflowUpdateTurn omitted empty and scalar tags stay silent under StrictMode' {
        $cache = Join-Path $TestDrive 'strictcount-cache'
        New-Item -ItemType Directory -Path $cache | Out-Null
        $persistLog = Join-Path $TestDrive 'strictcount-persist.jsonl'
        $savedCache = $env:MCP_CACHE_DIR_OVERRIDE
        $savedPersist = $env:MCP_PLUGIN_PERSIST_LOG
        try {
            $env:MCP_CACHE_DIR_OVERRIDE = $cache
            $env:MCP_PLUGIN_PERSIST_LOG = $persistLog
            . (Join-Path $script:LibRoot 'yaml-object-mutation.ps1')
            Import-McpYamlSerializer
            Write-McpYamlObject -Path (Join-Path $cache 'session-state.yaml') -Document ([ordered]@{
                status = 'verified'
                sessionId = 'GrokCode-20260819T000000Z-plugin-session'
                agent = 'GrokCode'
            })
            Write-McpYamlObject -Path (Join-Path $cache 'current-turn.yaml') -Document ([ordered]@{
                turnRequestId = 'req-20260819T000000Z-001-strictcount'
                queryTitle = 'strictcount'
                openedAt = '2026-08-19T00:00:01Z'
                status = 'in_progress'
                sessionId = 'GrokCode-20260819T000000Z-plugin-session'
            })
            . (Join-Path $script:LibRoot 'repl-invoke.ps1')
            Set-StrictMode -Version Latest
            function Assert-ReplCurrentTurnFresh { param([string]$Method) return $true }

            { Invoke-WorkflowUpdateTurn -ParamsYaml "response: ok`n" } | Should -Not -Throw
            { Invoke-WorkflowUpdateTurn -ParamsYaml "response: ok`ntags: []`ncontextList: []`n" } | Should -Not -Throw
            { Invoke-WorkflowUpdateTurn -ParamsYaml "response: ok`ntags: one-tag`ncontextList: one-context`n" } | Should -Not -Throw

            $stdout = Invoke-ReplMethod -Method 'workflow.sessionlog.updateTurn' -ParamsYaml "response: ok`ntags: one-tag`ncontextList: one-context`n" | Out-String
            $stdout.Trim() | Should -Be ''
            $script:LastInvokeReplMethodSuccess | Should -BeTrue
        } finally {
            if ($null -ne $savedCache) { $env:MCP_CACHE_DIR_OVERRIDE = $savedCache } else { Remove-Item Env:\MCP_CACHE_DIR_OVERRIDE -ErrorAction SilentlyContinue }
            if ($null -ne $savedPersist) { $env:MCP_PLUGIN_PERSIST_LOG = $savedPersist } else { Remove-Item Env:\MCP_PLUGIN_PERSIST_LOG -ErrorAction SilentlyContinue }
            Set-StrictMode -Off
        }
    }

    It 'TEST-MCP-FAILSAFE-001 Test-ReplFailsafeBackendUnreachable treats backend_unavailable and HTTP 503 as down' {
        $fn = [regex]::Match($script:ReplInvokeSource, '(?ms)^function Test-ReplFailsafeBackendUnreachable \{.*?^\}').Value
        $fn | Should -Not -BeNullOrEmpty
        Invoke-Expression $fn

        Test-ReplFailsafeBackendUnreachable -Detail 'code: backend_unavailable' | Should -BeTrue
        Test-ReplFailsafeBackendUnreachable -Detail 'HTTP 503 Service Unavailable' | Should -BeTrue
        Test-ReplFailsafeBackendUnreachable -Detail "type: error`npayload:`n  code: backend_unavailable`n  retryable: true" | Should -BeTrue
        Test-ReplFailsafeBackendUnreachable -Detail 'MCP_UNTRUSTED: marker refresh failed' | Should -BeTrue
        Test-ReplFailsafeBackendUnreachable -Detail "type: error`npayload:`n  code: validation_failed" | Should -BeFalse
    }

    It 'TEST-MCP-XAGENT-001 different sourceType prefixes are incompatible and same-agent prefixes rebind' {
        $prefixFn = [regex]::Match($script:ReplInvokeSource, '(?ms)^function Get-ReplSessionIdSourceTypePrefix \{.*?^\}').Value
        $compatFn = [regex]::Match($script:ReplInvokeSource, '(?ms)^function Test-ReplSessionSourceTypeCompatible \{.*?^\}').Value
        $prefixFn | Should -Not -BeNullOrEmpty
        $compatFn | Should -Not -BeNullOrEmpty
        Invoke-Expression $prefixFn
        Invoke-Expression $compatFn

        Get-ReplSessionIdSourceTypePrefix -SessionId 'Codex-20260819T000000Z-plugin-session' | Should -Be 'Codex'
        Get-ReplSessionIdSourceTypePrefix -SessionId 'GrokCode-20260819T000000Z-plugin-session' | Should -Be 'GrokCode'
        Get-ReplSessionIdSourceTypePrefix -SessionId 'ClaudeCode-20260819T000000Z-plugin-session' | Should -Be 'ClaudeCode'
        Test-ReplSessionSourceTypeCompatible -Left 'Codex-20260819T000000Z-a' -Right 'GrokCode-20260819T000000Z-b' | Should -BeFalse
        Test-ReplSessionSourceTypeCompatible -Left 'GrokCode-20260819T010000Z-root' -Right 'GrokCode-20260819T020000Z-rotated' | Should -BeTrue
    }

    It 'TEST-MCP-VERIFYWRAP-001 disk-full IOException is typed and bounded process honors timeout' {
        $diskFn = [regex]::Match($script:PluginHookSource, '(?ms)^function Test-PluginDiskFullException \{.*?^\}').Value
        $statusFn = [regex]::Match($script:PluginHookSource, '(?ms)^function Get-PluginCodeVerifyFailureStatus \{.*?^\}').Value
        $timeoutFn = [regex]::Match($script:PluginHookSource, '(?ms)^function Get-PluginCodeVerifyTimeoutSeconds \{.*?^\}').Value
        $boundedFn = [regex]::Match($script:PluginHookSource, '(?ms)^function Invoke-PluginBoundedProcess \{.*?^\}').Value
        $diskFn | Should -Not -BeNullOrEmpty
        $statusFn | Should -Not -BeNullOrEmpty
        $timeoutFn | Should -Not -BeNullOrEmpty
        $boundedFn | Should -Not -BeNullOrEmpty
        Invoke-Expression $diskFn
        Invoke-Expression $statusFn
        Invoke-Expression $timeoutFn
        Invoke-Expression $boundedFn

        $disk = [System.IO.IOException]::new('There is not enough space on the disk.')
        $disk.HResult = -2147024784
        Test-PluginDiskFullException -Exception $disk | Should -BeTrue
        $status = Get-PluginCodeVerifyFailureStatus -Exception $disk
        $status.status | Should -Be 'failed'
        $status.code | Should -Be 'disk_full'

        $guardFn = [regex]::Match($script:PluginHookSource, '(?ms)^function Invoke-PluginCodeVerifyHandleDiskFull \{.*?^\}').Value
        $guardFn | Should -Not -BeNullOrEmpty
        Invoke-Expression $guardFn
        $turnFile = Join-Path $TestDrive 'current-turn-diskfull.yaml'
        Set-Content -LiteralPath $turnFile -Value @(
            'turnRequestId: req-20260819T000000Z-001-diskfull'
            'status: in_progress'
            'auditActions: 2'
            'auditDialog: 1'
            'lastBuildStatus: unknown'
        ) -Encoding utf8
        $guard = Invoke-PluginCodeVerifyHandleDiskFull -TurnFile $turnFile -Exception $disk
        $guard.code | Should -Be 'disk_full'
        Test-Path -LiteralPath $turnFile | Should -BeTrue
        $raw = Get-Content -LiteralPath $turnFile -Raw
        $raw | Should -Match 'auditActions: 2'
        $raw | Should -Match 'auditDialog: 1'
        $raw | Should -Match 'lastBuildStatus: unknown'

        $savedTimeout = $env:MCP_CODE_VERIFY_TIMEOUT_SECONDS
        try {
            $env:MCP_CODE_VERIFY_TIMEOUT_SECONDS = '45'
            Get-PluginCodeVerifyTimeoutSeconds | Should -Be 45
        } finally {
            if ($null -ne $savedTimeout) { $env:MCP_CODE_VERIFY_TIMEOUT_SECONDS = $savedTimeout } else { Remove-Item Env:\MCP_CODE_VERIFY_TIMEOUT_SECONDS -ErrorAction SilentlyContinue }
        }

        $pwsh = (Get-Command pwsh -ErrorAction Stop).Source
        $started = [DateTime]::UtcNow
        $result = Invoke-PluginBoundedProcess -FileName $pwsh -Arguments @('-NoLogo', '-NoProfile', '-NonInteractive', '-Command', 'Start-Sleep -Seconds 20') -TimeoutSeconds 1
        $elapsed = ([DateTime]::UtcNow - $started).TotalSeconds
        $result.timedOut | Should -BeTrue
        $elapsed | Should -BeLessThan 8
    }

    It 'TEST-MCP-TRIAGEPLUGIN-004 PersistTurn.SubmitAsyncChildTimeout_ReturnsDegradedQueued' {
        # Times out the real Invoke-ReplRaw SubmitAsync child (hanging
        # mcpserver-repl on PATH). Does not stub Invoke-ReplRaw and does not
        # inject Persisted=false.
        $root = Join-Path $TestDrive 'submit-timeout'
        $cache = Join-Path $root 'cache'
        $failsafe = Join-Path $root 'failsafe'
        $bin = Join-Path $root 'bin'
        New-Item -ItemType Directory -Path $cache, $failsafe, $bin | Out-Null
        $hang = Join-Path $bin 'mcpserver-repl.cmd'
        Set-Content -LiteralPath $hang -Value "@echo off`r`nping -n 21 127.0.0.1 >nul`r`n" -Encoding ascii

        $savedPath = $env:PATH
        $savedTimeout = $env:REPL_TIMEOUT
        $savedCache = $env:MCP_CACHE_DIR_OVERRIDE
        $savedFailsafe = $env:MCPSERVER_FAILSAFE_DIR
        $savedPersist = $env:MCP_PLUGIN_PERSIST_LOG
        try {
            $env:PATH = "$bin;$savedPath"
            $env:REPL_TIMEOUT = '1'
            $env:MCP_CACHE_DIR_OVERRIDE = $cache
            $env:MCPSERVER_FAILSAFE_DIR = $failsafe
            Remove-Item Env:\MCP_PLUGIN_PERSIST_LOG -ErrorAction SilentlyContinue
            . (Join-Path $script:LibRoot 'yaml-object-mutation.ps1')
            Import-McpYamlSerializer
            Write-McpYamlObject -Path (Join-Path $cache 'session-state.yaml') -Document ([ordered]@{
                status = 'verified'
                sessionId = 'GrokCode-20260819T000000Z-plugin-session'
                agent = 'GrokCode'
                markerFilePath = (Join-Path $TestDrive 'AGENTS-README-FIRST.yaml')
                markerLastWriteUtc = '2026-08-19T00:00:00Z'
            })
            Write-McpYamlObject -Path (Join-Path $cache 'current-turn.yaml') -Document ([ordered]@{
                turnRequestId = 'req-20260819T000000Z-001-submit-timeout'
                queryTitle = 'submit timeout'
                queryText = 'submit timeout'
                openedAt = '2026-08-19T00:00:01Z'
                status = 'in_progress'
                sessionId = 'GrokCode-20260819T000000Z-plugin-session'
                planFile = 'docs/plans/triage-cluster-002.md'
                todoId = 'BUG-TRIAGE-120'
            })
            . (Join-Path $script:LibRoot 'repl-invoke.ps1')
            function Assert-ReplMarkerFresh { return $true }

            $raw = Get-Command Invoke-ReplRaw -CommandType Function -ErrorAction Stop
            $raw.Definition | Should -Match 'client\.SessionLog\.SubmitAsync|Get-Command mcpserver-repl'
            $started = [DateTime]::UtcNow
            $persisted = $null
            $threw = $false
            try {
                $persisted = Invoke-ReplPersistTurn -RequestId 'req-20260819T000000Z-001-submit-timeout' -Title 'submit timeout' -Status 'in_progress' -ResponseText '(turn opened)' -PlanFile 'docs/plans/triage-cluster-002.md' -TodoId 'BUG-TRIAGE-120'
            } catch {
                $threw = $true
            }
            $elapsed = ([DateTime]::UtcNow - $started).TotalSeconds
            $threw | Should -BeFalse
            $elapsed | Should -BeLessThan 8
            $persisted | Should -BeFalse
            $script:LastReplPersistenceDetails.degraded | Should -BeTrue
            $script:LastReplPersistenceDetails.queued | Should -BeTrue
            $script:LastReplPersistenceDetails.failsafePath | Should -Not -BeNullOrEmpty
            Test-Path -LiteralPath ([string]$script:LastReplPersistenceDetails.failsafePath) | Should -BeTrue
        } finally {
            $env:PATH = $savedPath
            if ($null -ne $savedTimeout) { $env:REPL_TIMEOUT = $savedTimeout } else { Remove-Item Env:\REPL_TIMEOUT -ErrorAction SilentlyContinue }
            if ($null -ne $savedCache) { $env:MCP_CACHE_DIR_OVERRIDE = $savedCache } else { Remove-Item Env:\MCP_CACHE_DIR_OVERRIDE -ErrorAction SilentlyContinue }
            if ($null -ne $savedFailsafe) { $env:MCPSERVER_FAILSAFE_DIR = $savedFailsafe } else { Remove-Item Env:\MCPSERVER_FAILSAFE_DIR -ErrorAction SilentlyContinue }
            if ($null -ne $savedPersist) { $env:MCP_PLUGIN_PERSIST_LOG = $savedPersist }
        }
    }
}
