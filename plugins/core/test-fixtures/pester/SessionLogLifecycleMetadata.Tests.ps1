#Requires -Version 7.0

# TEST-MCP-SESSIONLIFE-002
# Drives shipped stop-gate and appendActions. MCP_PLUGIN_PERSIST_LOG stays unset.

Describe 'FR-MCP-SESSIONLIFE-002 and FR-MCP-SESSIONLIFE-005 metadata and stop hook' {
    BeforeAll {
        $script:RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')).ProviderPath
        $script:HookScript = Join-Path $script:RepoRoot 'plugins\core\lib-ps\plugin-hook.ps1'
        $script:ReplScript = Join-Path $script:RepoRoot 'plugins\core\lib-ps\repl-invoke.ps1'
        . $script:ReplScript
        if ($env:MCP_PLUGIN_PERSIST_LOG) { Remove-Item Env:MCP_PLUGIN_PERSIST_LOG }
        function Invoke-ReplRaw {
            return $script:RawResult
        }
        function Invoke-ReplPersistTurn {
            param(
                [Parameter(Mandatory)][string]$RequestId,
                [AllowEmptyString()][string]$Title = '',
                [string]$Status = '',
                [string]$ResponseText = '',
                [string]$ActionsYaml = '',
                [string]$Interpretation = '',
                [int]$TokenCount = 0,
                [string[]]$Tags = @(),
                [string[]]$ContextList = @(),
                [string]$PlanFile,
                [string]$TodoId
            )
            $script:PersistCalls += 1
            $script:LastPersistResponse = $ResponseText
            $script:LastPersistInterpretation = $Interpretation
            $script:LastPersistTags = @($Tags)
            $script:LastPersistContext = @($ContextList)
            $disposition = if ($script:PersistOk) { 'primary' } elseif ($script:PersistQueued) { 'queued' } else { 'rejected' }
            [void](Publish-ReplSessionVerbReceipt -Receipt (New-ReplSessionVerbReceipt -Disposition $disposition -Method $script:ReplPersistVerbMethod -RequestId $RequestId))
            return $script:PersistOk
        }
    }

    BeforeEach {
        $script:PersistQueued = $false
    }

    It 'appendActions refuses a request id that does not match the cached turn' {
        $dir = Join-Path ([System.IO.Path]::GetTempPath()) ('sessionlife-meta-' + [guid]::NewGuid().ToString('N'))
        [void][System.IO.Directory]::CreateDirectory($dir)
        $prior = $env:MCP_CACHE_DIR_OVERRIDE
        $env:MCP_CACHE_DIR_OVERRIDE = $dir
        $env:MCP_WORKSPACE_PATH = $script:RepoRoot
        $script:PersistCalls = 0
        $script:PersistOk = $true
        try {
            Write-McpYamlObject -Path (Join-Path $dir 'session-state.yaml') -Document ([ordered]@{
                sessionId = 'GrokCode-20260923T205606Z-life'
                status = 'verified'
            })
            Write-McpYamlObject -Path (Join-Path $dir 'current-turn.yaml') -Document ([ordered]@{
                turnRequestId = 'req-cached'
                sessionId = 'GrokCode-20260923T205606Z-life'
                status = 'in_progress'
                queryText = 'keep'
            })
            $ok = Invoke-WorkflowAppendActions -ParamsYaml "requestId: req-other`nactions:`n  - type: design_decision`n    description: note`n"
            $ok | Should -BeFalse
            $script:PersistCalls | Should -Be 0
        } finally {
            if ($null -eq $prior) { Remove-Item Env:MCP_CACHE_DIR_OVERRIDE -ErrorAction SilentlyContinue } else { $env:MCP_CACHE_DIR_OVERRIDE = $prior }
            Remove-Item -LiteralPath $dir -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'appendActions returns false when persist fails and nothing is queued' {
        $dir = Join-Path ([System.IO.Path]::GetTempPath()) ('sessionlife-nq-' + [guid]::NewGuid().ToString('N'))
        [void][System.IO.Directory]::CreateDirectory($dir)
        $prior = $env:MCP_CACHE_DIR_OVERRIDE
        $env:MCP_CACHE_DIR_OVERRIDE = $dir
        $env:MCP_WORKSPACE_PATH = $script:RepoRoot
        $script:PersistCalls = 0
        $script:PersistOk = $false
        $script:PersistQueued = $false
        try {
            Write-McpYamlObject -Path (Join-Path $dir 'session-state.yaml') -Document ([ordered]@{
                sessionId = 'GrokCode-20260923T205606Z-life'
                status = 'verified'
            })
            Write-McpYamlObject -Path (Join-Path $dir 'current-turn.yaml') -Document ([ordered]@{
                turnRequestId = 'req-cached'
                sessionId = 'GrokCode-20260923T205606Z-life'
                status = 'in_progress'
                queryText = 'keep'
            })
            $ok = Invoke-WorkflowAppendActions -ParamsYaml "actions:`n  - type: design_decision`n    description: note`n"
            $ok | Should -BeFalse
            $script:PersistCalls | Should -Be 1
            Invoke-ReplMethod -Method 'workflow.sessionlog.appendActions' -ParamsYaml "actions:`n  - type: design_decision`n    description: note`n"
            $script:LastInvokeReplMethodSuccess | Should -BeFalse
        } finally {
            if ($null -eq $prior) { Remove-Item Env:MCP_CACHE_DIR_OVERRIDE -ErrorAction SilentlyContinue } else { $env:MCP_CACHE_DIR_OVERRIDE = $prior }
            Remove-Item -LiteralPath $dir -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'appendActions wrapper treats a retained failsafe as success and does not claim primary persistence' {
        $dir = Join-Path ([System.IO.Path]::GetTempPath()) ('sessionlife-q-' + [guid]::NewGuid().ToString('N'))
        [void][System.IO.Directory]::CreateDirectory($dir)
        $prior = $env:MCP_CACHE_DIR_OVERRIDE
        $env:MCP_CACHE_DIR_OVERRIDE = $dir
        $env:MCP_WORKSPACE_PATH = $script:RepoRoot
        $script:PersistCalls = 0
        $script:PersistOk = $false
        $script:PersistQueued = $true
        try {
            Write-McpYamlObject -Path (Join-Path $dir 'session-state.yaml') -Document ([ordered]@{
                sessionId = 'GrokCode-20260923T205606Z-life'
                status = 'verified'
            })
            Write-McpYamlObject -Path (Join-Path $dir 'current-turn.yaml') -Document ([ordered]@{
                turnRequestId = 'req-cached'
                sessionId = 'GrokCode-20260923T205606Z-life'
                status = 'in_progress'
                queryText = 'keep'
            })
            Invoke-ReplMethod -Method 'workflow.sessionlog.appendActions' -ParamsYaml "actions:`n  - type: design_decision`n    description: note`n"
            $script:LastInvokeReplMethodSuccess | Should -BeTrue
            $script:PersistCalls | Should -Be 1
            $script:LastReplPersistenceDetails.persisted | Should -BeFalse
            $script:LastReplPersistenceDetails.queued | Should -BeTrue
        } finally {
            if ($null -eq $prior) { Remove-Item Env:MCP_CACHE_DIR_OVERRIDE -ErrorAction SilentlyContinue } else { $env:MCP_CACHE_DIR_OVERRIDE = $prior }
            Remove-Item -LiteralPath $dir -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'updateTurn sends response interpretation tags and contextList and treats a retained failsafe as success' {
        $dir = Join-Path ([System.IO.Path]::GetTempPath()) ('sessionlife-upd-' + [guid]::NewGuid().ToString('N'))
        [void][System.IO.Directory]::CreateDirectory($dir)
        $prior = $env:MCP_CACHE_DIR_OVERRIDE
        $env:MCP_CACHE_DIR_OVERRIDE = $dir
        $env:MCP_WORKSPACE_PATH = $script:RepoRoot
        $script:PersistCalls = 0
        $script:PersistOk = $false
        $script:PersistQueued = $true
        try {
            Write-McpYamlObject -Path (Join-Path $dir 'session-state.yaml') -Document ([ordered]@{
                sessionId = 'GrokCode-20260923T205606Z-life'
                status = 'verified'
            })
            Write-McpYamlObject -Path (Join-Path $dir 'current-turn.yaml') -Document ([ordered]@{
                turnRequestId = 'req-cached'
                sessionId = 'GrokCode-20260923T205606Z-life'
                status = 'in_progress'
                queryText = 'keep'
            })
            Invoke-ReplMethod -Method 'workflow.sessionlog.updateTurn' -ParamsYaml "response: kept response`ninterpretation: noted interpretation`ntags:`n  - life`ncontextList:`n  - src/McpServer.Services/Services/SessionLogService.cs`n"
            $script:LastInvokeReplMethodSuccess | Should -BeTrue
            $script:PersistCalls | Should -Be 1
            $script:LastPersistResponse | Should -Be 'kept response'
            $script:LastPersistInterpretation | Should -Be 'noted interpretation'
            $script:LastPersistTags | Should -Contain 'life'
            $script:LastPersistContext | Should -Contain 'src/McpServer.Services/Services/SessionLogService.cs'
            $script:LastReplPersistenceDetails.persisted | Should -BeFalse
        } finally {
            if ($null -eq $prior) { Remove-Item Env:MCP_CACHE_DIR_OVERRIDE -ErrorAction SilentlyContinue } else { $env:MCP_CACHE_DIR_OVERRIDE = $prior }
            Remove-Item -LiteralPath $dir -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'stop gate blocks an in-progress turn when timestamp is stale even if lastUpdated is fresh' {
        $dir = Join-Path ([System.IO.Path]::GetTempPath()) ('sessionlife-stop-' + [guid]::NewGuid().ToString('N'))
        $cache = Join-Path $dir '.mcpServer\grok'
        [void][System.IO.Directory]::CreateDirectory($cache)
        $priorCache = $env:MCP_CACHE_DIR_OVERRIDE
        $priorHost = $env:MCP_PLUGIN_HOST
        $env:MCP_CACHE_DIR_OVERRIDE = $cache
        $env:MCP_PLUGIN_HOST = 'grok'
        $old = (Get-Date).ToUniversalTime().AddHours(-30).ToString('o')
        $fresh = (Get-Date).ToUniversalTime().ToString('o')
        Write-McpYamlObject -Path (Join-Path $cache 'session-state.yaml') -Document ([ordered]@{
            status = 'verified'
            sessionId = 'GrokCode-20260923T205606Z-life'
            timestamp = $old
            lastUpdated = $fresh
        })
        Write-McpYamlObject -Path (Join-Path $cache 'current-turn.yaml') -Document ([ordered]@{
            turnRequestId = 'req-stale-pin'
            sessionId = 'GrokCode-20260923T205606Z-life'
            status = 'in_progress'
            queryText = 'real work'
            auditActions = 2
        })
        try {
            $output = & $script:HookScript -HookName stop-gate -HostName grok -WorkspacePath $script:RepoRoot | Out-String
            $output | Should -Match 'stale cached session cannot be reused'
        } finally {
            if ($null -eq $priorCache) { Remove-Item Env:MCP_CACHE_DIR_OVERRIDE -ErrorAction SilentlyContinue } else { $env:MCP_CACHE_DIR_OVERRIDE = $priorCache }
            if ($null -eq $priorHost) { Remove-Item Env:MCP_PLUGIN_HOST -ErrorAction SilentlyContinue } else { $env:MCP_PLUGIN_HOST = $priorHost }
            Remove-Item -LiteralPath $dir -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'stop gate does not block a completed turn only because both timestamps are stale' {
        $dir = Join-Path ([System.IO.Path]::GetTempPath()) ('sessionlife-done-' + [guid]::NewGuid().ToString('N'))
        $cache = Join-Path $dir '.mcpServer\grok'
        [void][System.IO.Directory]::CreateDirectory($cache)
        $priorCache = $env:MCP_CACHE_DIR_OVERRIDE
        $priorHost = $env:MCP_PLUGIN_HOST
        $env:MCP_CACHE_DIR_OVERRIDE = $cache
        $env:MCP_PLUGIN_HOST = 'grok'
        $old = (Get-Date).ToUniversalTime().AddHours(-30).ToString('o')
        Write-McpYamlObject -Path (Join-Path $cache 'session-state.yaml') -Document ([ordered]@{
            status = 'verified'
            sessionId = 'GrokCode-20260923T205606Z-life'
            timestamp = $old
            lastUpdated = $old
        })
        Write-McpYamlObject -Path (Join-Path $cache 'current-turn.yaml') -Document ([ordered]@{
            turnRequestId = 'req-done'
            sessionId = 'GrokCode-20260923T205606Z-life'
            status = 'completed'
            queryText = 'finished'
            auditActions = 1
            codeEdits = 0
        })
        try {
            $output = & $script:HookScript -HookName stop-gate -HostName grok -WorkspacePath $script:RepoRoot | Out-String
            $output | Should -Not -Match 'stale cached session cannot be reused'
        } finally {
            if ($null -eq $priorCache) { Remove-Item Env:MCP_CACHE_DIR_OVERRIDE -ErrorAction SilentlyContinue } else { $env:MCP_CACHE_DIR_OVERRIDE = $priorCache }
            if ($null -eq $priorHost) { Remove-Item Env:MCP_PLUGIN_HOST -ErrorAction SilentlyContinue } else { $env:MCP_PLUGIN_HOST = $priorHost }
            Remove-Item -LiteralPath $dir -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}
