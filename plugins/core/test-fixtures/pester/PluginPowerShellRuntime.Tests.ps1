#Requires -Version 7.0

BeforeAll {
    $script:RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')).ProviderPath
    $script:LibRoot = Join-Path $script:RepoRoot 'plugins\core\lib-ps'
    $script:StagedRoot = Join-Path $script:RepoRoot 'plugins\core\.staged-plugin'
    $script:SmokeCache = Join-Path ([System.IO.Path]::GetTempPath()) 'mcp-plugin-psonly-pester'

    function Invoke-PluginChildProcess {
        param(
            [Parameter(Mandatory)][string]$ScriptPath,
            [string[]]$Arguments = @(),
            [hashtable]$Environment = @{},
            [string]$InputText = ''
        )

        $psi = [System.Diagnostics.ProcessStartInfo]::new()
        $psi.FileName = (Get-Command pwsh -ErrorAction Stop).Source
        $psi.ArgumentList.Add('-NoLogo')
        $psi.ArgumentList.Add('-NoProfile')
        $psi.ArgumentList.Add('-NonInteractive')
        $psi.ArgumentList.Add('-File')
        $psi.ArgumentList.Add($ScriptPath)
        foreach ($argument in $Arguments) {
            $psi.ArgumentList.Add($argument)
        }
        $psi.WorkingDirectory = $script:RepoRoot
        $psi.UseShellExecute = $false
        $psi.RedirectStandardInput = $true
        $psi.RedirectStandardOutput = $true
        $psi.RedirectStandardError = $true
        foreach ($key in $Environment.Keys) {
            $psi.Environment[$key] = [string]$Environment[$key]
        }

        $process = [System.Diagnostics.Process]::Start($psi)
        if ($InputText) {
            $process.StandardInput.Write($InputText)
        }
        $process.StandardInput.Close()
        $stdout = $process.StandardOutput.ReadToEndAsync()
        $stderr = $process.StandardError.ReadToEndAsync()
        $process.WaitForExit(30000) | Should -BeTrue

        [pscustomobject]@{
            ExitCode = $process.ExitCode
            Stdout = $stdout.Result.Trim()
            Stderr = $stderr.Result.Trim()
        }
    }
}

Describe 'TEST-MCP-PLUGIN-PSONLY-001 PowerShell runtime behavior' {
    It 'TEST-MCP-PLUGIN-PSONLY-001 parses acceptance criteria YAML without external helpers' {
        . (Join-Path $script:LibRoot 'repl-invoke.ps1')

        $yaml = @'
id: FR-MCP-PLUGIN-PSONLY-001
title: Plugin runtime uses PowerShell only
acceptanceCriteria:
  - id: ac-1
    text: Runtime paths are PowerShell scripts
    isSatisfied: false
  - id: ac-2
    text: Static validation rejects forbidden runtime files
    isSatisfied: true
'@

        $parsed = Convert-ReplParamsYamlToObject -ParamsYaml $yaml

        $parsed.id | Should -Be 'FR-MCP-PLUGIN-PSONLY-001'
        $parsed.acceptanceCriteria.Count | Should -Be 2
        $parsed.acceptanceCriteria[0].id | Should -Be 'ac-1'
        $parsed.acceptanceCriteria[1].isSatisfied | Should -BeTrue
    }

    It 'TEST-MCP-PLUGIN-PSONLY-001 parses literal block YAML responses without external helpers' {
        . (Join-Path $script:LibRoot 'repl-invoke.ps1')

        $parsed = Convert-ReplParamsYamlToObject -ParamsYaml "response: |`n  line one`n  line two"

        $parsed.response | Should -Be "line one`nline two`n"
    }

    It 'TEST-MCP-PLUGIN-PSONLY-001 imports shim module and serializes DTO contracts with PowerShell-native JSON' {
        Remove-Module McpPluginShim -Force -ErrorAction SilentlyContinue
        Import-Module (Join-Path $script:LibRoot 'McpPluginShim.psm1') -Force

        $request = New-McpPluginReplRequest `
            -RequestId 'req-20260628T030000Z-pester' `
            -Method 'workflow.todo.query' `
            -Params @{ done = $false }
        $request.GetType().Name | Should -Be 'McpPluginReplRequest'
        $requestJson = ConvertTo-McpPluginJson -InputObject $request -Depth 20 -Compress
        $requestJson | Should -Be '{"type":"request","payload":{"requestId":"req-20260628T030000Z-pester","method":"workflow.todo.query","params":{"done":false}}}'

        $action = New-McpPluginActionRecord -Values @{
            type = 'edit'
            filePath = 'src/Example.cs'
            status = 'succeeded'
        }
        $turn = New-McpPluginTurnUpsertRequest `
            -Agent 'Codex' `
            -SessionId 'Codex-20260628T030000Z-pester' `
            -RequestId 'req-20260628T030000Z-pester' `
            -Timestamp '2026-06-28T03:00:00Z' `
            -QueryText 'Hello' `
            -Title 'Hello' `
            -Status 'completed' `
            -ResponseText 'Done' `
            -Model 'codex' `
            -FilesModified @('src/Example.cs') `
            -Actions @($action.ToMap())

        $turn.GetType().Name | Should -Be 'McpPluginTurnUpsertRequest'
        $params = $turn.ToParamsObject()
        $params.agent | Should -Be 'Codex'
        $params.sessionId | Should -Be 'Codex-20260628T030000Z-pester'
        $params.turn.filesModified | Should -Be @('src/Example.cs')
        $params.turn.actions[0].filePath | Should -Be 'src/Example.cs'

        $failsafe = New-McpPluginFailsafeRecord `
            -Method 'client.SessionLog.UpsertTurnAsync' `
            -Label 'session_upsertTurn' `
            -Timestamp '20260628T030000Z' `
            -ParamsYaml '{"type":"request"}'
        $failsafe.ToYaml() | Should -Match 'method: client\.SessionLog\.UpsertTurnAsync'
        $failsafe.ToYaml() | Should -Match 'params:'

        $result = New-McpPluginReplResult -Success $true -Output 'type: result' -ExitCode 0
        $result.GetType().Name | Should -Be 'McpPluginReplResult'
        $result.Success | Should -BeTrue
    }

    It 'TEST-MCP-PLUGIN-PSONLY-001 documents every public shim DTO member through discoverable help' {
        Remove-Module McpPluginShim -Force -ErrorAction SilentlyContinue
        Import-Module (Join-Path $script:LibRoot 'McpPluginShim.psm1') -Force

        $expectedHelp = @{
            'New-McpPluginInvocationOptions' = @(
                'McpPluginInvocationOptions',
                'Command',
                'Method',
                'Params',
                'ParamsPath',
                'Response',
                'ResponsePath',
                'WorkspacePath',
                'PluginRoot',
                'CacheRoot',
                'TimeoutSeconds'
            )
            'New-McpPluginReplRequest' = @(
                'McpPluginReplRequest',
                'Type',
                'RequestId',
                'Method',
                'Params'
            )
            'New-McpPluginReplResult' = @(
                'McpPluginReplResult',
                'Success',
                'Output',
                'ExitCode',
                'Error'
            )
            'New-McpPluginSessionMeta' = @(
                'McpPluginSessionMeta',
                'SourceType',
                'SessionId'
            )
            'New-McpPluginActionRecord' = @(
                'McpPluginActionRecord',
                'Values'
            )
            'New-McpPluginTurnUpsertRequest' = @(
                'McpPluginTurnUpsertRequest',
                'Agent',
                'SessionId',
                'Turn',
                'requestId',
                'timestamp',
                'queryText',
                'queryTitle',
                'response',
                'status',
                'model',
                'tokenCount',
                'filesModified',
                'actions'
            )
            'New-McpPluginFailsafeRecord' = @(
                'McpPluginFailsafeRecord',
                'Method',
                'Label',
                'Timestamp',
                'ParamsYaml',
                'Path'
            )
            'ConvertTo-McpPluginJson' = @(
                'InputObject',
                'Depth',
                'Compress'
            )
        }

        foreach ($entry in $expectedHelp.GetEnumerator()) {
            $help = Get-Help $entry.Key -Full | Out-String -Width 200
            foreach ($member in $entry.Value) {
                $help | Should -Match ([regex]::Escape($member))
            }
        }
    }

    It 'TEST-MCP-PLUGIN-PSONLY-001 wires source entrypoints through the shim module DTO factories' {
        $invokeContent = [System.IO.File]::ReadAllText((Join-Path $script:LibRoot 'Invoke-McpPlugin.ps1'))
        $replContent = [System.IO.File]::ReadAllText((Join-Path $script:LibRoot 'repl-invoke.ps1'))

        $invokeContent | Should -Match 'McpPluginShim\.psm1'
        $invokeContent | Should -Match 'New-McpPluginInvocationOptions'

        $replContent | Should -Match 'McpPluginShim\.psm1'
        $replContent | Should -Match 'New-McpPluginReplRequest'
        $replContent | Should -Match 'New-McpPluginReplResult'
        $replContent | Should -Match 'New-McpPluginSessionMeta'
        $replContent | Should -Match 'New-McpPluginActionRecord'
        $replContent | Should -Match 'New-McpPluginTurnUpsertRequest'
        $replContent | Should -Match 'New-McpPluginFailsafeRecord'
        $replContent | Should -Match 'ConvertTo-McpPluginJson'
    }

    It 'TEST-MCP-PLUGIN-PSONLY-001 starts mcpserver-repl in the PowerShell workspace when the .NET current directory differs' {
        . (Join-Path $script:LibRoot 'repl-invoke.ps1')

        $root = Join-Path $script:SmokeCache ([guid]::NewGuid().ToString('N'))
        $workspace = Join-Path $root 'workspace'
        $wrongCurrentDirectory = Join-Path $root 'home'
        [void][System.IO.Directory]::CreateDirectory($workspace)
        [void][System.IO.Directory]::CreateDirectory($wrongCurrentDirectory)

        $oldLocation = (Get-Location).ProviderPath
        $oldCurrentDirectory = [Environment]::CurrentDirectory
        $envNames = @(
            'MCP_WORKSPACE_PATH',
            'MCPSERVER_WORKSPACE_PATH',
            'MCP_WORKSPACE_START_DIR',
            'CLAUDE_PROJECT_DIR'
        )
        $previousEnv = @{}
        foreach ($name in $envNames) {
            $previousEnv[$name] = [Environment]::GetEnvironmentVariable($name, 'Process')
            Remove-Item "Env:\$name" -ErrorAction SilentlyContinue
        }

        try {
            Set-Location -LiteralPath $workspace
            [Environment]::CurrentDirectory = $wrongCurrentDirectory

            $psi = [System.Diagnostics.ProcessStartInfo]::new()
            $psi.UseShellExecute = $false
            Set-ReplProcessWorkspace -StartInfo $psi

            $expectedWorkspace = (Resolve-Path -LiteralPath $workspace).ProviderPath
            $psi.WorkingDirectory | Should -Be $expectedWorkspace
            $psi.Environment['MCP_WORKSPACE_PATH'] | Should -Be $expectedWorkspace
            $psi.Environment['MCPSERVER_WORKSPACE_PATH'] | Should -Be $expectedWorkspace
            $psi.Environment['MCP_WORKSPACE_START_DIR'] | Should -Be $expectedWorkspace
            $psi.Environment['CLAUDE_PROJECT_DIR'] | Should -Be $expectedWorkspace
        } finally {
            Set-Location -LiteralPath $oldLocation
            [Environment]::CurrentDirectory = $oldCurrentDirectory
            foreach ($name in $envNames) {
                if ($null -ne $previousEnv[$name]) {
                    [Environment]::SetEnvironmentVariable($name, [string]$previousEnv[$name], 'Process')
                } else {
                    Remove-Item "Env:\$name" -ErrorAction SilentlyContinue
                }
            }
            Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'TEST-MCP-PLUGIN-PSONLY-001 FR-MCP-PLUGIN-PSONLY-003 fails closed before MCP work when the runtime is refused' {
        $result = Invoke-PluginChildProcess `
            -ScriptPath (Join-Path $script:StagedRoot 'lib\plugin-hook.ps1') `
            -Arguments @('-HookName', 'health-check', '-HostName', 'claude-code') `
            -Environment @{
                MCP_PLUGIN_ROOT = $script:StagedRoot
                MCP_PLUGIN_HOST = 'claude-code'
                MCP_AGENT_NAME = 'ClaudeCode'
                MCP_PLUGIN_REFUSE_POWERSHELL = '1'
                PLUGIN_ROOT_OVERRIDE = $script:SmokeCache
                MCP_WORKSPACE_PATH = $script:RepoRoot
                MCPSERVER_WORKSPACE_PATH = $script:RepoRoot
            }

        $result.ExitCode | Should -Be 0
        $result.Stdout | Should -Be 'MCP_PLUGIN_UNAVAILABLE:ClaudeCode'
    }

    It 'TEST-MCP-PLUGIN-PSONLY-002 generated wrappers invoke PowerShell hook entrypoint' {
        $result = Invoke-PluginChildProcess `
            -ScriptPath (Join-Path $script:StagedRoot 'hooks\scripts\session-end.ps1') `
            -Environment @{
                MCP_PLUGIN_ROOT = $script:StagedRoot
                MCP_PLUGIN_HOST = 'claude-code'
                MCP_AGENT_NAME = 'ClaudeCode'
                PLUGIN_ROOT_OVERRIDE = $script:SmokeCache
                MCP_WORKSPACE_PATH = $script:RepoRoot
                MCPSERVER_WORKSPACE_PATH = $script:RepoRoot
            }

        $result.ExitCode | Should -Be 0
        $result.Stdout | Should -Be '{}'
    }

    It 'TEST-MCP-PLUGIN-PSONLY-002 status entrypoint returns JSON status without mutation' {
        $result = Invoke-PluginChildProcess `
            -ScriptPath (Join-Path $script:StagedRoot 'lib\mcp-status.ps1') `
            -Environment @{
                MCP_PLUGIN_ROOT = $script:StagedRoot
                MCP_PLUGIN_HOST = 'claude-code'
                MCP_AGENT_NAME = 'ClaudeCode'
                PLUGIN_ROOT_OVERRIDE = $script:SmokeCache
                MCP_WORKSPACE_PATH = $script:RepoRoot
                MCPSERVER_WORKSPACE_PATH = $script:RepoRoot
            }

        $result.ExitCode | Should -Be 0
        $status = $result.Stdout | ConvertFrom-Json
        $status.agent | Should -Be 'ClaudeCode'
        $status.status | Should -BeIn @('available', 'no-session')
        $status.namespaces | Should -Contain 'workflow.sessionlog'
        $status.namespaces | Should -Contain 'workflow.todo'
        $status.namespaces | Should -Contain 'workflow.requirements'
        $status.namespaces | Should -Contain 'workflow.triage'
        $status.requirementMethods | Should -Contain 'workflow.requirements.listLayers'
        $status.requirementMethods | Should -Contain 'workflow.requirements.createLayer'
        $status.requirementMethods | Should -Contain 'workflow.requirements.updateLayer'
        $status.requirementMethods | Should -Contain 'workflow.requirements.effective'
        $status.requirementClientMethods | Should -Contain 'client.Requirements.ListRequirementLayersAsync'
        $status.requirementClientMethods | Should -Contain 'client.Requirements.CreateRequirementLayerAsync'
        $status.requirementClientMethods | Should -Contain 'client.Requirements.UpdateRequirementLayerAsync'
        $status.requirementClientMethods | Should -Contain 'client.Requirements.GetEffectiveRequirementsAsync'
    }

    It 'TEST-MCP-REQSCOPE-005 requirements guidance tells agents to use current effective layer visibility' {
        $guidancePath = Join-Path $script:RepoRoot 'templates\prompt-templates.yaml'
        Test-Path -LiteralPath $guidancePath | Should -BeTrue

        $content = [System.IO.File]::ReadAllText($guidancePath)

        $content | Should -Match 'current effective requirements'
        $content | Should -Match 'workflow\.requirements\.effective'
        $content | Should -Match 'active workspace layer'
        $content | Should -Match 'future-layer requirements'
        $content | Should -Match 'currently enforceable'
    }

    It 'TEST-MCP-REQSCOPE-005 shell requirements wrapper exposes layer commands' {
        $shimPath = Join-Path $script:StagedRoot 'lib\repl-invoke.sh'
        if (-not (Test-Path -LiteralPath $shimPath)) {
            $shimPath = Join-Path $script:RepoRoot 'plugins\core\lib-sh\repl-invoke.sh'
        }
        Test-Path -LiteralPath $shimPath | Should -BeTrue

        $content = [System.IO.File]::ReadAllText($shimPath)

        $content | Should -Match 'workflow\.requirements\.listLayers'
        $content | Should -Match 'workflow\.requirements\.createLayer'
        $content | Should -Match 'workflow\.requirements\.updateLayer'
        $content | Should -Match 'workflow\.requirements\.effective'
        $content | Should -Match 'client\.Requirements\.ListRequirementLayersAsync'
        $content | Should -Match 'client\.Requirements\.CreateRequirementLayerAsync'
        $content | Should -Match 'client\.Requirements\.UpdateRequirementLayerAsync'
        $content | Should -Match 'client\.Requirements\.GetEffectiveRequirementsAsync'
    }

    It 'TEST-MCP-PLUGIN-PSONLY-001 forces each PowerShell host identity over inherited agent environment' {
        $cases = @(
            @{ Host = 'claude-code'; Agent = 'ClaudeCode'; Model = 'claude'; Tag = 'claude-code' },
            @{ Host = 'claude-cowork'; Agent = 'ClaudeCowork'; Model = 'claude'; Tag = 'claude-cowork' },
            @{ Host = 'codex'; Agent = 'Codex'; Model = 'codex'; Tag = 'codex' },
            @{ Host = 'copilot'; Agent = 'Copilot'; Model = 'copilot'; Tag = 'copilot' },
            @{ Host = 'grok'; Agent = 'GrokCode'; Model = 'grok'; Tag = 'grok' },
            @{ Host = 'cline'; Agent = 'Cline'; Model = 'cline'; Tag = 'cline' },
            @{ Host = 'cline-v2'; Agent = 'Cline'; Model = 'cline'; Tag = 'cline-v2' },
            @{ Host = 'opencode'; Agent = 'OpenCode'; Model = 'opencode'; Tag = 'opencode' }
        )

        $envScript = (Join-Path $script:LibRoot 'plugin-env.ps1').Replace("'", "''")
        $probe = Join-Path $script:SmokeCache 'identity-probe.ps1'
        [System.IO.Directory]::CreateDirectory($script:SmokeCache) | Out-Null
        [System.IO.File]::WriteAllText($probe, @"
. '$envScript'
[pscustomobject]@{
  agent = `$env:MCP_AGENT_NAME
  agentId = `$env:MCP_AGENT_ID
  sessionAgent = `$env:MCP_SESSION_AGENT
  sourceType = `$env:CT2R_SOURCE_TYPE
  model = `$env:MCP_SESSION_MODEL
  tag = `$env:PLUGIN_TAG
} | ConvertTo-Json -Compress
"@)

        foreach ($case in $cases) {
            $result = Invoke-PluginChildProcess `
                -ScriptPath $probe `
                -Environment @{
                    MCP_PLUGIN_HOST = $case.Host
                    MCP_PLUGIN_ENV_LOADED = '1'
                    MCP_AGENT_NAME = 'WrongAgent'
                    MCP_AGENT_ID = 'WrongAgent'
                    MCP_SESSION_AGENT = 'WrongAgent'
                    CT2R_SOURCE_TYPE = 'WrongAgent'
                    PLUGIN_AGENT_DEFAULT = 'WrongAgent'
                    PLUGIN_TAG = 'wrong-plugin'
                    MCP_SESSION_MODEL = 'wrong-model'
                    MCP_PLUGIN_ROOT = $script:StagedRoot
                    PLUGIN_ROOT_OVERRIDE = $script:SmokeCache
                    MCP_WORKSPACE_PATH = $script:RepoRoot
                    MCPSERVER_WORKSPACE_PATH = $script:RepoRoot
                }

            $result.ExitCode | Should -Be 0
            $status = $result.Stdout | ConvertFrom-Json
            $status.agent | Should -Be $case.Agent
            $status.agentId | Should -Be $case.Agent
            $status.sessionAgent | Should -Be $case.Agent
            $status.sourceType | Should -Be $case.Agent
            $status.model | Should -Be $case.Model
            $status.tag | Should -Be $case.Tag
        }
    }

    It 'TEST-MCP-PLUGIN-PSONLY-002 synced manifest contains PowerShell runtime files only' {
        $manifest = Join-Path $script:StagedRoot 'CORE-MANIFEST.yaml'
        Test-Path -LiteralPath $manifest | Should -BeTrue

        $content = [System.IO.File]::ReadAllText($manifest)
        $content | Should -Match 'lib/McpPluginShim.psm1'
        $content | Should -Match 'lib/plugin-hook.ps1'
        $content | Should -Match 'lib/repl-invoke.ps1'
        $content | Should -Not -Match '\.sh:'
        $content | Should -Not -Match '\.js:'
        $content | Should -Not -Match 'GAPS.md'
    }

    It 'TEST-MCP-PLUGIN-PSONLY-001 writes pending cache records with deterministic sequence and payload fields' {
        $cacheRoot = Join-Path $script:SmokeCache ([guid]::NewGuid().ToString('N'))
        $env:MCP_CACHE_DIR_OVERRIDE = $cacheRoot
        try {
            $first = & (Join-Path $script:LibRoot 'cache-manager.ps1') -Action write -Method 'workflow.todo.create' -ParamsYaml "title: First"
            $second = & (Join-Path $script:LibRoot 'cache-manager.ps1') -Action write -Method 'workflow.todo.update' -ParamsYaml "id: TODO-1"
            $status = & (Join-Path $script:LibRoot 'cache-manager.ps1') -Action status

            Split-Path $first -Leaf | Should -Be '001-workflow-todo-create.yaml'
            Split-Path $second -Leaf | Should -Be '002-workflow-todo-update.yaml'
            $status | Should -Be '2'

            $content = [System.IO.File]::ReadAllText($first)
            $content | Should -Match 'id: "001"'
            $content | Should -Match 'timestamp: "\d{4}-\d{2}-\d{2}T'
            $content | Should -Match 'method: workflow\.todo\.create'
            $content | Should -Match 'params:'
            $content | Should -Match 'title: First'
            $content | Should -Match 'retryCount: 0'
        } finally {
            Remove-Item Env:\MCP_CACHE_DIR_OVERRIDE -ErrorAction SilentlyContinue
            Remove-Item -LiteralPath $cacheRoot -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'TEST-MCP-PLUGIN-PSONLY-001 flushes cache records in order and leaves no pending records on success' {
        $workRoot = Join-Path $script:SmokeCache ([guid]::NewGuid().ToString('N'))
        $libCopy = Join-Path $workRoot 'lib'
        $cacheRoot = Join-Path $workRoot 'cache'
        [void][System.IO.Directory]::CreateDirectory($libCopy)
        Copy-Item -LiteralPath (Join-Path $script:LibRoot 'cache-manager.ps1') -Destination $libCopy
        Copy-Item -LiteralPath (Join-Path $script:LibRoot 'resolve-cache-dir.ps1') -Destination $libCopy
        [System.IO.File]::WriteAllText(
            (Join-Path $libCopy 'repl-invoke.ps1'),
            "param([string]`$Method)`nAdd-Content -LiteralPath '$($workRoot.Replace("'", "''"))\replay.log' -Value `$Method`n"
        )

        $env:MCP_CACHE_DIR_OVERRIDE = $cacheRoot
        try {
            & (Join-Path $libCopy 'cache-manager.ps1') -Action write -Method 'workflow.todo.create' | Out-Null
            & (Join-Path $libCopy 'cache-manager.ps1') -Action write -Method 'workflow.todo.update' | Out-Null
            $result = & (Join-Path $libCopy 'cache-manager.ps1') -Action flush

            $result | Should -Be 'flushed=2 failed=0 pending=0'
            [System.IO.File]::ReadAllLines((Join-Path $workRoot 'replay.log')) | Should -Be @(
                'workflow.todo.create',
                'workflow.todo.update'
            )
        } finally {
            Remove-Item Env:\MCP_CACHE_DIR_OVERRIDE -ErrorAction SilentlyContinue
            Remove-Item -LiteralPath $workRoot -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'TEST-MCP-PLUGIN-PSONLY-001 increments retry count on failed cache replay and skips exhausted retries' {
        $workRoot = Join-Path $script:SmokeCache ([guid]::NewGuid().ToString('N'))
        $libCopy = Join-Path $workRoot 'lib'
        $cacheRoot = Join-Path $workRoot 'cache'
        [void][System.IO.Directory]::CreateDirectory($libCopy)
        Copy-Item -LiteralPath (Join-Path $script:LibRoot 'cache-manager.ps1') -Destination $libCopy
        Copy-Item -LiteralPath (Join-Path $script:LibRoot 'resolve-cache-dir.ps1') -Destination $libCopy
        [System.IO.File]::WriteAllText(
            (Join-Path $libCopy 'repl-invoke.ps1'),
            "param([string]`$Method)`nif (`$Method -eq 'workflow.todo.fail') { throw 'replay failed' }`n"
        )

        $env:MCP_CACHE_DIR_OVERRIDE = $cacheRoot
        try {
            $failedItem = & (Join-Path $libCopy 'cache-manager.ps1') -Action write -Method 'workflow.todo.fail'
            $exhaustedItem = & (Join-Path $libCopy 'cache-manager.ps1') -Action write -Method 'workflow.todo.exhausted'
            $exhaustedContent = [System.IO.File]::ReadAllText($exhaustedItem) -replace 'retryCount: 0', 'retryCount: 3'
            [System.IO.File]::WriteAllText($exhaustedItem, $exhaustedContent)

            $result = & (Join-Path $libCopy 'cache-manager.ps1') -Action flush

            $result | Should -Be 'flushed=0 failed=1 pending=2'
            [System.IO.File]::ReadAllText($failedItem) | Should -Match 'retryCount: 1'
            [System.IO.File]::ReadAllText($exhaustedItem) | Should -Match 'retryCount: 3'
        } finally {
            Remove-Item Env:\MCP_CACHE_DIR_OVERRIDE -ErrorAction SilentlyContinue
            Remove-Item -LiteralPath $workRoot -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'TEST-MCP-PLUGIN-PSONLY-001 resolves cache directories by override, workspace, and plugin fallback precedence' {
        $envNames = @(
            'MCP_CACHE_DIR_OVERRIDE',
            'MCPSERVER_WORKSPACE_PATH',
            'MCP_WORKSPACE_PATH',
            'MCP_PLUGIN_ROOT',
            'MCP_WORKSPACE_START_DIR',
            'PLUGIN_ROOT_OVERRIDE'
        )
        $previousEnv = @{}
        foreach ($name in $envNames) {
            $previousEnv[$name] = [Environment]::GetEnvironmentVariable($name, 'Process')
            Remove-Item -LiteralPath "Env:\$name" -ErrorAction SilentlyContinue
        }

        . (Join-Path $script:LibRoot 'resolve-cache-dir.ps1')

        $root = Join-Path $script:SmokeCache ([guid]::NewGuid().ToString('N'))
        $override = Join-Path $root 'override'
        $workspace = Join-Path $root 'workspace'
        $pluginRoot = Join-Path $root 'plugin'
        [void][System.IO.Directory]::CreateDirectory($workspace)
        [void][System.IO.Directory]::CreateDirectory($pluginRoot)

        try {
            $env:MCP_CACHE_DIR_OVERRIDE = $override
            Resolve-McpCacheDir | Should -Be $override

            Remove-Item Env:\MCP_CACHE_DIR_OVERRIDE -ErrorAction SilentlyContinue
            $env:MCPSERVER_WORKSPACE_PATH = $workspace
            Resolve-McpCacheDir | Should -Be (Join-Path $workspace 'cache')

            Remove-Item Env:\MCPSERVER_WORKSPACE_PATH -ErrorAction SilentlyContinue
            Remove-Item Env:\MCP_WORKSPACE_PATH -ErrorAction SilentlyContinue
            $env:MCP_PLUGIN_ROOT = $pluginRoot
            $env:MCP_WORKSPACE_START_DIR = $pluginRoot
            Resolve-McpCacheDir | Should -Be (Join-Path $pluginRoot 'cache')
        } finally {
            Remove-Item Env:\MCP_CACHE_DIR_OVERRIDE -ErrorAction SilentlyContinue
            Remove-Item Env:\MCPSERVER_WORKSPACE_PATH -ErrorAction SilentlyContinue
            Remove-Item Env:\MCP_WORKSPACE_PATH -ErrorAction SilentlyContinue
            Remove-Item Env:\MCP_PLUGIN_ROOT -ErrorAction SilentlyContinue
            Remove-Item Env:\MCP_WORKSPACE_START_DIR -ErrorAction SilentlyContinue
            Remove-Item Env:\PLUGIN_ROOT_OVERRIDE -ErrorAction SilentlyContinue
            foreach ($name in $envNames) {
                if ($null -ne $previousEnv[$name]) {
                    [Environment]::SetEnvironmentVariable($name, [string]$previousEnv[$name], 'Process')
                }
            }
            Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'TEST-MCP-PLUGIN-PSONLY-001 reads marker fields, endpoints, and agent plugin policy using PowerShell only' {
        . (Join-Path $script:LibRoot 'marker-resolver.ps1')

        $root = Join-Path $script:SmokeCache ([guid]::NewGuid().ToString('N'))
        $child = Join-Path $root 'src\sub'
        [void][System.IO.Directory]::CreateDirectory($child)
        $marker = Join-Path $root 'AGENTS-README-FIRST.yaml'
        [System.IO.File]::WriteAllText($marker, @'
port: 7147
baseUrl: http://localhost:7147
apiKey: test-key
workspacePath: F:\GitHub\McpServer
endpoints:
  health: /health
  sessionLog: /mcpserver/sessionlog
agent_plugins:
  policy: allow
  contract_digest: abc123
'@)

        try {
            Find-MarkerFile -StartDir $child | Should -Be $marker
            Get-MarkerField -MarkerFile $marker -FieldName 'apiKey' | Should -Be 'test-key'
            Get-MarkerEndpoint -MarkerFile $marker -EndpointName 'sessionLog' | Should -Be '/mcpserver/sessionlog'
            Get-MarkerAgentPluginField -MarkerFile $marker -FieldName 'policy' | Should -Be 'allow'
        } finally {
            Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}
