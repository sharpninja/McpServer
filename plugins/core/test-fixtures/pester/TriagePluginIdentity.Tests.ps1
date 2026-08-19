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
}
