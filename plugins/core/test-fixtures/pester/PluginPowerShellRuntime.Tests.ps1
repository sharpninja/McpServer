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

    It 'TEST-MCP-PLUGIN-PSONLY-001 rejects append and complete calls when no active turn cache exists' {
        $pluginRoot = Join-Path $script:SmokeCache ([guid]::NewGuid().ToString('N'))
        [void][System.IO.Directory]::CreateDirectory($pluginRoot)
        $previousPluginRoot = $env:PLUGIN_ROOT_OVERRIDE
        $previousCacheOverride = $env:MCP_CACHE_DIR_OVERRIDE

        try {
            $env:PLUGIN_ROOT_OVERRIDE = $pluginRoot
            $env:MCP_CACHE_DIR_OVERRIDE = (Join-Path $pluginRoot 'cache')
            . (Join-Path $script:LibRoot 'repl-invoke.ps1')

            $actionsPayload = [ordered]@{
                actions = @(
                    [ordered]@{
                        type = 'edit'
                        filePath = 'src/Example.cs'
                        status = 'succeeded'
                    }
                )
            } | ConvertTo-Json -Depth 10 -Compress
            $completePayload = [ordered]@{
                response = 'Done'
            } | ConvertTo-Json -Depth 10 -Compress

            Invoke-ReplMethod -Method 'workflow.sessionlog.appendActions' -ParamsYaml $actionsPayload | Should -BeFalse
            Invoke-ReplMethod -Method 'workflow.sessionlog.completeTurn' -ParamsYaml $completePayload | Should -BeFalse
        } finally {
            if ($null -ne $previousPluginRoot) {
                $env:PLUGIN_ROOT_OVERRIDE = $previousPluginRoot
            } else {
                Remove-Item Env:\PLUGIN_ROOT_OVERRIDE -ErrorAction SilentlyContinue
            }
            if ($null -ne $previousCacheOverride) {
                $env:MCP_CACHE_DIR_OVERRIDE = $previousCacheOverride
            } else {
                Remove-Item Env:\MCP_CACHE_DIR_OVERRIDE -ErrorAction SilentlyContinue
            }
            Remove-Item -LiteralPath $pluginRoot -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'TEST-MCP-CACHE-001 resolves runtime state under the workspace .mcpServer agent directory' {
        $workspaceRoot = Join-Path $script:SmokeCache ([guid]::NewGuid().ToString('N'))
        $pluginRoot = Join-Path $script:SmokeCache ([guid]::NewGuid().ToString('N'))
        [void][System.IO.Directory]::CreateDirectory($workspaceRoot)
        [void][System.IO.Directory]::CreateDirectory($pluginRoot)

        $previous = @{}
        foreach ($name in @(
                'MCP_CACHE_DIR_OVERRIDE',
                'PLUGIN_ROOT_OVERRIDE',
                'MCP_WORKSPACE_PATH',
                'MCPSERVER_WORKSPACE_PATH',
                'MCP_WORKSPACE_START_DIR',
                'CLAUDE_PROJECT_DIR',
                'MCP_PLUGIN_ROOT',
                'MCP_AGENT_NAME',
                'PLUGIN_AGENT_DEFAULT')) {
            $previous[$name] = [Environment]::GetEnvironmentVariable($name)
        }

        try {
            Remove-Item Env:\MCP_CACHE_DIR_OVERRIDE -ErrorAction SilentlyContinue
            $env:PLUGIN_ROOT_OVERRIDE = $pluginRoot
            $env:MCP_WORKSPACE_PATH = $workspaceRoot
            $env:MCPSERVER_WORKSPACE_PATH = $workspaceRoot
            $env:MCP_WORKSPACE_START_DIR = $workspaceRoot
            $env:CLAUDE_PROJECT_DIR = $workspaceRoot
            $env:MCP_PLUGIN_ROOT = $pluginRoot
            $env:MCP_AGENT_NAME = 'Codex'
            $env:PLUGIN_AGENT_DEFAULT = 'Codex'

            . (Join-Path $script:LibRoot 'resolve-cache-dir.ps1')
            Resolve-McpCacheDir | Should -Be (Join-Path $workspaceRoot '.mcpServer\codex')
        } finally {
            foreach ($entry in $previous.GetEnumerator()) {
                if ($null -eq $entry.Value) {
                    Remove-Item ("Env:\$($entry.Key)") -ErrorAction SilentlyContinue
                } else {
                    Set-Item ("Env:\$($entry.Key)") $entry.Value
                }
            }
            Remove-Item -LiteralPath $workspaceRoot -Recurse -Force -ErrorAction SilentlyContinue
            Remove-Item -LiteralPath $pluginRoot -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'TEST-MCP-PLUGIN-PSONLY-001 handles appendDialog locally instead of dispatching it as a raw server method' {
        $pluginRoot = Join-Path $script:SmokeCache ([guid]::NewGuid().ToString('N'))
        [void][System.IO.Directory]::CreateDirectory($pluginRoot)
        $previousPluginRoot = $env:PLUGIN_ROOT_OVERRIDE
        $previousCacheOverride = $env:MCP_CACHE_DIR_OVERRIDE

        try {
            $env:PLUGIN_ROOT_OVERRIDE = $pluginRoot
            $env:MCP_CACHE_DIR_OVERRIDE = (Join-Path $pluginRoot 'cache')
            . (Join-Path $script:LibRoot 'repl-invoke.ps1')
            $previousRaw = Get-Command Invoke-ReplRaw -CommandType Function -ErrorAction SilentlyContinue
            function Invoke-ReplRaw {
                throw 'raw dispatch should not be used for workflow.sessionlog.appendDialog'
            }

            $dialogPayload = [ordered]@{
                dialogItems = @(
                    [ordered]@{
                        role = 'assistant'
                        content = 'diagnostic'
                        category = 'decision'
                    }
                )
            } | ConvertTo-Json -Depth 10 -Compress

            { $script:appendDialogResult = Invoke-ReplMethod -Method 'workflow.sessionlog.appendDialog' -ParamsYaml $dialogPayload } | Should -Not -Throw
            $script:appendDialogResult | Should -BeFalse
        } finally {
            if ($previousRaw) {
                Set-Item -Path Function:\Invoke-ReplRaw -Value $previousRaw.ScriptBlock
            } else {
                Remove-Item Function:\Invoke-ReplRaw -ErrorAction SilentlyContinue
            }
            if ($null -ne $previousPluginRoot) {
                $env:PLUGIN_ROOT_OVERRIDE = $previousPluginRoot
            } else {
                Remove-Item Env:\PLUGIN_ROOT_OVERRIDE -ErrorAction SilentlyContinue
            }
            if ($null -ne $previousCacheOverride) {
                $env:MCP_CACHE_DIR_OVERRIDE = $previousCacheOverride
            } else {
                Remove-Item Env:\MCP_CACHE_DIR_OVERRIDE -ErrorAction SilentlyContinue
            }
            Remove-Item -LiteralPath $pluginRoot -Recurse -Force -ErrorAction SilentlyContinue
            Remove-Variable -Name appendDialogResult -Scope Script -ErrorAction SilentlyContinue
        }
    }

    It 'TEST-MCP-PLUGIN-PSONLY-001 exits nonzero when appendActions has no active turn cache' {
        $pluginRoot = Join-Path $script:SmokeCache ([guid]::NewGuid().ToString('N'))
        [void][System.IO.Directory]::CreateDirectory($pluginRoot)
        $actionsPayload = [ordered]@{
            actions = @(
                [ordered]@{
                    type = 'edit'
                    filePath = 'src/Example.cs'
                    status = 'succeeded'
                }
            )
        } | ConvertTo-Json -Depth 10 -Compress

        try {
            $result = Invoke-PluginChildProcess `
                -ScriptPath (Join-Path $script:LibRoot 'repl-invoke.ps1') `
                -Arguments @('-Method', 'workflow.sessionlog.appendActions', '-ParamsYaml', $actionsPayload) `
                -Environment @{ PLUGIN_ROOT_OVERRIDE = $pluginRoot; MCP_CACHE_DIR_OVERRIDE = (Join-Path $pluginRoot 'cache') }

            $result.ExitCode | Should -Be 1
            ($result.Stdout + $result.Stderr) | Should -Match 'current-turn\.yaml'
        } finally {
            Remove-Item -LiteralPath $pluginRoot -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'TEST-MCP-MARKER-REFRESH-001 plugin hooks cache marker path and timestamp in session state' {
        $source = [System.IO.File]::ReadAllText((Join-Path $script:LibRoot 'plugin-hook.ps1'))

        $source | Should -Match 'markerFilePath'
        $source | Should -Match 'markerLastWriteUtc'
        $source | Should -Match 'Get-MarkerFileSnapshot'
    }

    It 'TEST-MCP-MARKER-REFRESH-001 plugin hooks check marker freshness before opening turns' {
        $source = [System.IO.File]::ReadAllText((Join-Path $script:LibRoot 'plugin-hook.ps1'))

        $source | Should -Match 'function Ensure-PluginMarkerFresh'
        $source | Should -Match 'Ensure-PluginMarkerFresh -StartPath'
        $source | Should -Match 'Start-PluginSession -StartPath'
    }

    It 'TEST-MCP-MARKER-REFRESH-001 plugin REPL dispatch checks marker freshness before requests' {
        $source = [System.IO.File]::ReadAllText((Join-Path $script:LibRoot 'repl-invoke.ps1'))

        $source | Should -Match 'function Assert-ReplMarkerFresh'
        $source | Should -Match 'Assert-ReplMarkerFresh'
        $source.IndexOf('Assert-ReplMarkerFresh') | Should -BeLessThan $source.IndexOf('Invoke-ReplRaw -Method')
    }

    It 'TEST-MCP-BUGTRIAGE-017 repl-invoke force reloads stale McpPluginShim modules' {
        $source = [System.IO.File]::ReadAllText((Join-Path $script:LibRoot 'repl-invoke.ps1'))

        $source | Should -Match 'Import-Module \$shimModule -Force'
        $source | Should -Match 'Remove-Module McpPluginShim -Force'
        $source | Should -Match 'New-McpPluginTurnUpsertRequest'
        $source | Should -Match 'ProcessingDialog'
    }

    It 'TEST-MCP-BUGTRIAGE-019 plugin hooks create meaningful continuation titles and object-written turn cache' {
        $source = [System.IO.File]::ReadAllText((Join-Path $script:LibRoot 'plugin-hook.ps1'))

        $source | Should -Match 'Continuation or hook-triggered turn'
        $source | Should -Match 'Continuation turn'
        $source | Should -Not -Match ([regex]::Escape('if (-not $prompt) { $prompt = ''User prompt'' }'))
        $source | Should -Match 'Write-McpYamlObject -Path \$turnFile -Document \$turnState'
        $source | Should -Match 'markerFilePath'
        $source | Should -Match 'markerLastWriteUtc'
        $source | Should -Match 'sessionId'
    }

    It 'TEST-MCP-BUGTRIAGE-019 repl-invoke supports queryTitle overrides through append and complete paths' {
        $source = [System.IO.File]::ReadAllText((Join-Path $script:LibRoot 'repl-invoke.ps1'))

        $source | Should -Match 'function Update-ReplTurnTitleFromParams'
        $source | Should -Match "Name 'queryTitle'"
        $source | Should -Match 'Set-ReplTurnCacheField -Field ''queryTitle'''
        $source | Should -Match 'Update-ReplTurnTitleFromParams -ParamsYaml \$ParamsYaml'
    }

    It 'TEST-MCP-BUGTRIAGE-020 repl-invoke rejects stale current-turn cache with actionable diagnostics' {
        $source = [System.IO.File]::ReadAllText((Join-Path $script:LibRoot 'repl-invoke.ps1'))

        $source | Should -Match 'function Assert-ReplCurrentTurnFresh'
        $source | Should -Match 'staleSessionId'
        $source | Should -Match 'activeSessionId'
        $source | Should -Match 'markerFilePath'
        $source | Should -Match 'markerLastWriteUtc'
        $source | Should -Match 'Run the active agent prompt hook again'
        $source | Should -Match 'Assert-ReplCurrentTurnFresh -Method ''workflow.sessionlog.appendActions'''
        $source | Should -Match 'Assert-ReplCurrentTurnFresh -Method ''workflow.sessionlog.completeTurn'''
    }

    It 'TEST-MCP-PLUGIN-PSONLY-001 emits raw REPL YAML on the success stream' {
        . (Join-Path $script:LibRoot 'repl-invoke.ps1')
        $previousRaw = Get-Command Invoke-ReplRaw -CommandType Function -ErrorAction SilentlyContinue
        function Invoke-ReplRaw {
            New-McpPluginReplResult -Success $true -Output "type: result`npayload:`n  ok: true" -ExitCode 0
        }

        try {
            $output = Invoke-ReplMethod -Method 'client.Health.GetAsync'
            $envelope = ConvertFrom-Yaml -Yaml ($output | Out-String) -Ordered
        } finally {
            if ($previousRaw) {
                Set-Item -Path Function:\Invoke-ReplRaw -Value $previousRaw.ScriptBlock
            } else {
                Remove-Item Function:\Invoke-ReplRaw -ErrorAction SilentlyContinue
            }
        }

        $script:LastInvokeReplMethodSuccess | Should -BeTrue
        $envelope['type'] | Should -Be 'result'
        $envelope['payload']['ok'] | Should -BeTrue
    }

    It 'TEST-MCP-PLUGIN-PSONLY-001 emits generated-document YAML as success output for YAML object parsing' {
        . (Join-Path $script:LibRoot 'repl-invoke.ps1')
        $previousRaw = Get-Command Invoke-ReplRaw -CommandType Function -ErrorAction SilentlyContinue
        function Invoke-ReplRaw {
            New-McpPluginReplResult -Success $true -Output "type: result`npayload:`n  result:`n    contentBase64: QUJD" -ExitCode 0
        }

        try {
            $output = Invoke-ReplMethod -Method 'workflow.requirements.generateDocument'
            $envelope = ConvertFrom-Yaml -Yaml ($output | Out-String) -Ordered
        } finally {
            if ($previousRaw) {
                Set-Item -Path Function:\Invoke-ReplRaw -Value $previousRaw.ScriptBlock
            } else {
                Remove-Item Function:\Invoke-ReplRaw -ErrorAction SilentlyContinue
            }
        }

        $script:LastInvokeReplMethodSuccess | Should -BeTrue
        $envelope['payload']['result']['contentBase64'] | Should -Be 'QUJD'
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
            -Actions @($action.ToMap()) `
            -ProcessingDialog @([ordered]@{
                role = 'assistant'
                content = 'diagnostic'
                category = 'decision'
            })

        $turn.GetType().Name | Should -Be 'McpPluginTurnUpsertRequest'
        $params = $turn.ToParamsObject()
        $params.agent | Should -Be 'Codex'
        $params.sessionId | Should -Be 'Codex-20260628T030000Z-pester'
        $params.turn.filesModified | Should -Be @('src/Example.cs')
        $params.turn.actions[0].filePath | Should -Be 'src/Example.cs'
        $params.turn.processingDialog[0].category | Should -Be 'decision'

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
                'actions',
                'processingDialog'
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
        $replContent | Should -Match 'Write-McpYamlObject'
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

    It 'TEST-MCP-TRANSCRIPT-010 transcript ingestion helper and skill are staged for plugin hosts' {
        $helperPath = Join-Path $script:LibRoot 'transcript-ingestion.ps1'
        Test-Path -LiteralPath $helperPath | Should -BeTrue
        $helper = [System.IO.File]::ReadAllText($helperPath)

        $helper | Should -Match 'repl\.sessionlog\.ingestTranscripts'
        $helper | Should -Match 'repl\.sessionlog\.normalizeTranscripts'
        $helper | Should -Match 'ConvertTo-Yaml'
        $helper | Should -Match 'targetProfile'
        $helper | Should -Match 'Persist\.IsPresent'

        $skillPath = Join-Path $script:StagedRoot 'skills\transcript-ingestion\SKILL.md'
        Test-Path -LiteralPath $skillPath | Should -BeTrue
        $skill = [System.IO.File]::ReadAllText($skillPath)
        $skill | Should -Match 'Claude, Codex, and Grok'
        $skill | Should -Match 'transcript-ingestion\.ps1'
        $skill | Should -Match 'repl\.sessionlog\.ingestTranscripts'
        $skill | Should -Match 'repl\.sessionlog\.normalizeTranscripts'
        $skill | Should -Match 'YAML Mutation Rule'
    }

    It 'TEST-MCP-TRANSCRIPT-010 resolves real host transcript paths for Claude Codex and Grok plugin recovery' {
        $helperRoot = Join-Path $script:SmokeCache ([guid]::NewGuid().ToString('N'))
        $capturePath = Join-Path $helperRoot 'captures.jsonl'
        [void][System.IO.Directory]::CreateDirectory($helperRoot)

        try {
            Copy-Item -LiteralPath (Join-Path $script:LibRoot 'transcript-ingestion.ps1') -Destination $helperRoot
            Copy-Item -LiteralPath (Join-Path $script:LibRoot 'yaml-object-mutation.ps1') -Destination $helperRoot
            $stub = @'
param(
    [Parameter(Mandatory)][string]$Method,
    [string]$ParamsYaml = ''
)

$record = [ordered]@{
    method = $Method
    paramsYaml = $ParamsYaml
}
[System.IO.File]::AppendAllText($env:MCP_TRANSCRIPT_TEST_CAPTURE, (($record | ConvertTo-Json -Compress) + [Environment]::NewLine))
Write-Output "type: result"
Write-Output "payload:"
Write-Output "  result:"
Write-Output "    status: captured"
'@
            [System.IO.File]::WriteAllText((Join-Path $helperRoot 'repl-invoke.ps1'), $stub)

            $fixturesRoot = Join-Path $script:RepoRoot 'tests\McpServer.Support.Mcp.Tests\Fixtures\Transcripts\real'
            $claudeFixture = Join-Path $fixturesRoot 'claude\session.jsonl'
            $codexFixture = Join-Path $fixturesRoot 'codex\session.jsonl'
            $grokRoot = Join-Path $helperRoot 'grok-root'
            [void][System.IO.Directory]::CreateDirectory($grokRoot)
            $grokFixture = Join-Path $grokRoot 'chat_history.jsonl'
            Copy-Item -LiteralPath (Join-Path $fixturesRoot 'grok\chat_history.jsonl') -Destination $grokFixture

            $cases = @(
                [pscustomobject]@{ Name = 'Claude hook transcript_path'; Agent = 'ClaudeCode'; Source = 'Claude'; ExpectedPath = $claudeFixture; Environment = @{ transcript_path = $claudeFixture } },
                [pscustomobject]@{ Name = 'Codex active session JSONL'; Agent = 'Codex'; Source = 'Codex'; ExpectedPath = $codexFixture; Environment = @{ CODEX_SESSION_FILE = $codexFixture } },
                [pscustomobject]@{ Name = 'Grok configured transcript root'; Agent = 'GrokCode'; Source = 'Grok'; ExpectedPath = $grokFixture; Environment = @{ GROK_TRANSCRIPT_ROOT = $grokRoot } }
            )

            . (Join-Path $script:LibRoot 'repl-invoke.ps1')
            foreach ($case in $cases) {
                Remove-Item -LiteralPath $capturePath -Force -ErrorAction SilentlyContinue
                $environment = @{
                    MCP_TRANSCRIPT_TEST_CAPTURE = $capturePath
                    PLUGIN_AGENT_NAME = $case.Agent
                }
                foreach ($entry in $case.Environment.GetEnumerator()) {
                    $environment[$entry.Key] = $entry.Value
                }

                $result = Invoke-PluginChildProcess `
                    -ScriptPath (Join-Path $helperRoot 'transcript-ingestion.ps1') `
                    -Arguments @('-Source', $case.Source, '-NoPersist') `
                    -Environment $environment

                $result.ExitCode | Should -Be 0 -Because $case.Name
                $record = Get-Content -LiteralPath $capturePath | Select-Object -Last 1 | ConvertFrom-Json
                $record.method | Should -Be 'repl.sessionlog.ingestTranscripts'
                $params = Convert-ReplParamsYamlToObject -ParamsYaml $record.paramsYaml
                $params.path | Should -Be $case.ExpectedPath
                $params.agent | Should -Be $case.Agent
                $params.source | Should -Be $case.Source
                $params.persist | Should -BeFalse
            }
        } finally {
            Remove-Item -LiteralPath $helperRoot -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'TEST-MCP-YAML-MUTATION-001 all staged plugin skills teach object-first YAML mutation' {
        $skillFiles = Get-ChildItem -LiteralPath (Join-Path $script:StagedRoot 'skills') -Filter 'SKILL.md' -Recurse -File
        $skillFiles.Count | Should -BeGreaterThan 0

        foreach ($skillFile in $skillFiles) {
            $content = [System.IO.File]::ReadAllText($skillFile.FullName)

            $content | Should -Match 'YAML Mutation Rule'
            $content | Should -Match 'deserialize the complete document into an object'
            $content | Should -Match 'mutate the object'
            $content | Should -Match 'serialize the object'
            $content | Should -Match 'yaml-object-mutation\.ps1'
            $content | Should -Match 'Set-McpYamlObjectValue'
            $content | Should -Match 'Update-McpYamlObject'
        }
    }

    It 'TEST-MCP-YAML-MUTATION-002 YAML helper mutates nested values through object serialization' {
        . (Join-Path $script:LibRoot 'yaml-object-mutation.ps1')

        $yamlPath = Join-Path $script:SmokeCache 'yaml-object-mutation\appsettings.yaml'
        if (Test-Path -LiteralPath $yamlPath) {
            Remove-Item -LiteralPath $yamlPath -Force
        }

        Set-McpYamlObjectValue -Path $yamlPath -KeyPath Triage,AgentPath -Value 'codex' -Create | Out-Null
        Set-McpYamlObjectValue -Path $yamlPath -KeyPath Triage,QuietPeriodMinutes -Value 15 | Out-Null

        $document = ConvertFrom-Yaml -Yaml ([System.IO.File]::ReadAllText($yamlPath)) -Ordered
        $document['Triage']['AgentPath'] | Should -Be 'codex'
        $document['Triage']['QuietPeriodMinutes'] | Should -Be 15
    }

    It 'TEST-MCP-YAML-MUTATION-003 core sync manifest is built from a serialized object' {
        $content = [System.IO.File]::ReadAllText((Join-Path $script:RepoRoot 'plugins\core\sync\sync-plugin-core.ps1'))

        $content | Should -Match 'ConvertFrom-Yaml'
        $content | Should -Match 'ConvertTo-Yaml'
        $content | Should -Match '\[ordered\]@'
        $content | Should -Match '\.TrimEnd\(\)'
        $content | Should -Not -Match '\$lines\.Add'
        $content | Should -Not -Match 'Get-Content -LiteralPath \$manifest'
    }

    It 'TEST-MCP-YAML-MUTATION-004 final response params are serialized from an object' {
        $content = [System.IO.File]::ReadAllText((Join-Path $script:LibRoot 'final-response.ps1'))

        $content | Should -Match 'ConvertTo-Yaml'
        $content | Should -Match '\[ordered\]@\{ response = \$Response \}'
        $content | Should -Not -Match 'response:\s*\|'
        $content | Should -Not -Match '\$indented'
    }

    It 'TEST-MCP-TRIAGE-003 PowerShell hooks generate canonical suffixed session ids' {
        $content = [System.IO.File]::ReadAllText((Join-Path $script:LibRoot 'plugin-hook.ps1'))

        $content | Should -Match 'New-PluginSessionId'
        $content | Should -Match 'yyyyMMddTHHmmssZ'
        $content | Should -Match 'plugin-session'
        $content | Should -Not -Match '''\{0\}-\{1\}'' -f \$env:MCP_AGENT_NAME'
    }

    It 'TEST-MCP-TRIAGE-003 PowerShell hooks serialize workflow params from objects' {
        $content = [System.IO.File]::ReadAllText((Join-Path $script:LibRoot 'plugin-hook.ps1'))

        $content | Should -Match 'ConvertTo-Yaml'
        $content | Should -Not -Match '\$paramsYaml\s*=\s*"requestId:'
        $content | Should -Not -Match '\$paramsYaml\s*=\s*"response:\s*\|'
    }

    It 'TEST-MCP-TRIAGE-003 session log persistence failures are observable' {
        $content = [System.IO.File]::ReadAllText((Join-Path $script:LibRoot 'repl-invoke.ps1'))

        $content | Should -Not -Match 'best-effort'
        $content | Should -Not -Match 'Invoke-ReplPersistTurn[^\r\n]*(?:\r?\n\s+-[^\r\n]*)*\s*\|\s*Out-Null'
        $content | Should -Match 'throw'
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

    It 'TEST-MCP-PLUGIN-PSONLY-001 resolves explicit override, workspace, and legacy test cache precedence' {
        $envNames = @(
            'MCP_CACHE_DIR_OVERRIDE',
            'MCPSERVER_WORKSPACE_PATH',
            'MCP_WORKSPACE_PATH',
            'MCP_PLUGIN_ROOT',
            'MCP_WORKSPACE_START_DIR',
            'PLUGIN_ROOT_OVERRIDE',
            'MCP_AGENT_NAME'
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
            $env:MCP_AGENT_NAME = 'Codex'
            Resolve-McpCacheDir | Should -Be (Join-Path $workspace '.mcpServer\codex')

            Remove-Item Env:\MCPSERVER_WORKSPACE_PATH -ErrorAction SilentlyContinue
            Remove-Item Env:\MCP_WORKSPACE_PATH -ErrorAction SilentlyContinue
            $env:MCP_PLUGIN_ROOT = $pluginRoot
            $env:MCP_WORKSPACE_START_DIR = $pluginRoot
            $legacyRoot = Join-Path $root 'legacy-cache-root'
            [void][System.IO.Directory]::CreateDirectory($legacyRoot)
            $env:PLUGIN_ROOT_OVERRIDE = $legacyRoot
            Resolve-McpCacheDir | Should -Be (Join-Path $legacyRoot 'cache')
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

Describe 'TEST-MCP-PLUGINCORE-004 session-log dialog parsing' {
    It 'parses dictionary-backed dialogItems YAML and property-backed JSON' {
        . (Join-Path $script:LibRoot 'repl-invoke.ps1')

        $payload = [ordered]@{
            dialogItems = @(
                [ordered]@{
                    role = 'assistant'
                    content = 'first diagnostic'
                    category = 'analysis'
                },
                [ordered]@{
                    role = 'assistant'
                    content = 'selected the independent REPL failsafe strategy'
                    category = 'decision'
                }
            )
        }
        $yaml = $payload | ConvertTo-Yaml -Options WithIndentedSequences
        $json = $payload | ConvertTo-Json -Depth 10 -Compress

        $yamlParams = Convert-ReplParamsYamlToObject -ParamsYaml $yaml
        $yamlParams | Should -BeOfType ([System.Collections.IDictionary])
        $yamlItems = @(Get-ReplDialogItemsFromParams -ParamsYaml $yaml)
        $jsonItems = @(Get-ReplDialogItemsFromParams -ParamsYaml $json)

        $yamlItems.Count | Should -Be 2
        $yamlItems[0].content | Should -Be 'first diagnostic'
        $yamlItems[1].category | Should -Be 'decision'
        $jsonItems.Count | Should -Be 2
        $jsonItems[1].content | Should -Be 'selected the independent REPL failsafe strategy'
    }

    It 'rejects appendDialog when no dialog items can be parsed' {
        $pluginRoot = Join-Path $script:SmokeCache ([guid]::NewGuid().ToString('N'))
        $cacheDir = Join-Path $pluginRoot 'cache'
        [void][System.IO.Directory]::CreateDirectory($cacheDir)
        $previousCacheOverride = $env:MCP_CACHE_DIR_OVERRIDE

        try {
            $env:MCP_CACHE_DIR_OVERRIDE = $cacheDir
            . (Join-Path $script:LibRoot 'repl-invoke.ps1')
            . (Join-Path $script:LibRoot 'repl-invoke.ps1')
            $previousFresh = Get-Command Assert-ReplCurrentTurnFresh -CommandType Function -ErrorAction Stop
            function Assert-ReplCurrentTurnFresh { return $true }

            Write-McpYamlObject -Path (Join-Path $cacheDir 'current-turn.yaml') -Document ([ordered]@{
                turnRequestId = 'req-20260709T211800Z-dialog-red'
                queryTitle = 'Dialog parser red test'
                status = 'in_progress'
                auditDialog = 0
            })

            Invoke-WorkflowAppendDialog -ParamsYaml '{"unexpected":true}' | Should -BeFalse
            (Read-McpYamlObject -Path (Join-Path $cacheDir 'current-turn.yaml'))['auditDialog'] | Should -Be 0
        } finally {
            if ($previousFresh) {
                Set-Item -Path Function:\Assert-ReplCurrentTurnFresh -Value $previousFresh.ScriptBlock
            }
            if ($null -ne $previousCacheOverride) {
                $env:MCP_CACHE_DIR_OVERRIDE = $previousCacheOverride
            } else {
                Remove-Item Env:\MCP_CACHE_DIR_OVERRIDE -ErrorAction SilentlyContinue
            }
            Remove-Item -LiteralPath $pluginRoot -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}

Describe 'TEST-MCP-REPL-025 PowerShell REPL persistence boundary' {
    It 'routes persistence through canonical client submit and enforces failsafe ordering' {
        $source = [System.IO.File]::ReadAllText((Join-Path $script:LibRoot 'repl-invoke.ps1'))
        $match = [regex]::Match(
            $source,
            '(?s)function Invoke-ReplPersistTurn\s*\{(?<body>.*?)\r?\n\}\r?\nfunction Update-ReplTurnCacheStatus')

        $match.Success | Should -BeTrue
        $match.Groups['body'].Value | Should -Match 'ConvertTo-Yaml\s+-Data'
        $match.Groups['body'].Value | Should -Match 'Write-ReplFailsafe'
        $match.Groups['body'].Value | Should -Match 'client\.SessionLog\.SubmitAsync'
        $match.Groups['body'].Value | Should -Match 'Clear-ReplFailsafe'
        $match.Groups['body'].Value | Should -Not -Match 'repl\.sessionlog\.persistTurn'
    }

    It 'writes a YAML failsafe before submit and clears it after durable success' {
        $pluginRoot = Join-Path $script:SmokeCache ([guid]::NewGuid().ToString('N'))
        $cacheDir = Join-Path $pluginRoot 'cache'
        [void][System.IO.Directory]::CreateDirectory($cacheDir)
        $previousCacheOverride = $env:MCP_CACHE_DIR_OVERRIDE
        $previousRaw = $null

        try {
            $env:MCP_CACHE_DIR_OVERRIDE = $cacheDir
            . (Join-Path $script:LibRoot 'repl-invoke.ps1')
            Write-McpYamlObject -Path (Join-Path $cacheDir 'session-state.yaml') -Document ([ordered]@{
                sessionId = 'Codex-20260709T220000Z-plugin-session'
                title = 'Failsafe order test'
                started = '2026-07-09T22:00:00Z'
            })
            . (Join-Path $script:LibRoot 'repl-invoke.ps1')
            $previousRaw = Get-Command Invoke-ReplRaw -CommandType Function -ErrorAction SilentlyContinue
            function Invoke-ReplRaw {
                param([string]$Method, [string]$ParamsYaml = '')
                $script:rawMethod = $Method
                $script:failsafeAtSubmit = @(Get-ChildItem -LiteralPath (Get-ReplFailsafeDir) -Filter '*.yaml' -File).Count
                $script:rawYaml = $ParamsYaml
                return New-McpPluginReplResult -Success $true -Output ("type: result" + [Environment]::NewLine + "payload:" + [Environment]::NewLine + "  result:" + [Environment]::NewLine + "    persisted: true") -ExitCode 0
            }

            Invoke-ReplPersistTurn -RequestId 'req-20260709T220001Z-failsafe' -Title 'Failsafe order test' -Status 'completed' -ResponseText 'Done' | Should -BeTrue
            $script:rawMethod | Should -Be 'client.SessionLog.SubmitAsync'
            $script:failsafeAtSubmit | Should -Be 1
            $script:rawYaml | Should -Match 'sessionLog:'
            @(Get-ChildItem -LiteralPath (Get-ReplFailsafeDir) -Filter '*.yaml' -File).Count | Should -Be 0
        } finally {
            if ($previousRaw) {
                Set-Item -Path Function:\Invoke-ReplRaw -Value $previousRaw.ScriptBlock
            } else {
                Remove-Item Function:\Invoke-ReplRaw -ErrorAction SilentlyContinue
            }
            if ($null -ne $previousCacheOverride) {
                $env:MCP_CACHE_DIR_OVERRIDE = $previousCacheOverride
            } else {
                Remove-Item Env:\MCP_CACHE_DIR_OVERRIDE -ErrorAction SilentlyContinue
            }
            Remove-Item -LiteralPath $pluginRoot -Recurse -Force -ErrorAction SilentlyContinue
            Remove-Variable -Name rawMethod, failsafeAtSubmit, rawYaml -Scope Script -ErrorAction SilentlyContinue
        }
    }

    It 'retains the YAML failsafe when MCP rejects the submit' {
        $pluginRoot = Join-Path $script:SmokeCache ([guid]::NewGuid().ToString('N'))
        $cacheDir = Join-Path $pluginRoot 'cache'
        [void][System.IO.Directory]::CreateDirectory($cacheDir)
        $previousCacheOverride = $env:MCP_CACHE_DIR_OVERRIDE
        $previousRaw = $null

        try {
            $env:MCP_CACHE_DIR_OVERRIDE = $cacheDir
            . (Join-Path $script:LibRoot 'repl-invoke.ps1')
            Write-McpYamlObject -Path (Join-Path $cacheDir 'session-state.yaml') -Document ([ordered]@{
                sessionId = 'Codex-20260709T220000Z-plugin-session'
                title = 'Failsafe rejection test'
                started = '2026-07-09T22:00:00Z'
            })
            . (Join-Path $script:LibRoot 'repl-invoke.ps1')
            $previousRaw = Get-Command Invoke-ReplRaw -CommandType Function -ErrorAction SilentlyContinue
            function Invoke-ReplRaw {
                param([string]$Method, [string]$ParamsYaml = '')
                return New-McpPluginReplResult -Success $false -Output ("type: error" + [Environment]::NewLine + "payload:" + [Environment]::NewLine + "  code: unavailable") -ExitCode 1
            }

            { Invoke-ReplPersistTurn -RequestId 'req-20260709T220002Z-failsafe' -Title 'Failsafe rejection test' -Status 'completed' -ResponseText 'Done' } | Should -Throw '*FailsafePath*'
            @(Get-ChildItem -LiteralPath (Get-ReplFailsafeDir) -Filter '*.yaml' -File).Count | Should -Be 1
        } finally {
            if ($previousRaw) {
                Set-Item -Path Function:\Invoke-ReplRaw -Value $previousRaw.ScriptBlock
            } else {
                Remove-Item Function:\Invoke-ReplRaw -ErrorAction SilentlyContinue
            }
            if ($null -ne $previousCacheOverride) {
                $env:MCP_CACHE_DIR_OVERRIDE = $previousCacheOverride
            } else {
                Remove-Item Env:\MCP_CACHE_DIR_OVERRIDE -ErrorAction SilentlyContinue
            }
            Remove-Item -LiteralPath $pluginRoot -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'reports degraded persistence and its failsafe path only when completeTurn closes the turn' {
        $pluginRoot = Join-Path $script:SmokeCache ([guid]::NewGuid().ToString('N'))
        $cacheDir = Join-Path $pluginRoot 'cache'
        [void][System.IO.Directory]::CreateDirectory($cacheDir)
        $previousCacheOverride = $env:MCP_CACHE_DIR_OVERRIDE
        $originalError = [Console]::Error
        $errorWriter = [System.IO.StringWriter]::new()

        try {
            $env:MCP_CACHE_DIR_OVERRIDE = $cacheDir
            . (Join-Path $script:LibRoot 'repl-invoke.ps1')
            . (Join-Path $script:LibRoot 'repl-invoke.ps1')
            $previousFresh = Get-Command Assert-ReplCurrentTurnFresh -CommandType Function -ErrorAction Stop
            $previousPersist = Get-Command Invoke-ReplPersistTurn -CommandType Function -ErrorAction Stop
            function Assert-ReplCurrentTurnFresh { return $true }
            function Invoke-ReplPersistTurn {
                $script:LastReplPersistenceDetails = [ordered]@{
                    persisted = $true
                    degraded = $true
                    persistenceStrategy = 'filesystem-failsafe'
                    failsafePath = 'C:\failsafe\turn.yaml'
                    message = 'MCP Session Log persistence is degraded. Turn saved to failsafe path.'
                }
                return $true
            }

            Write-McpYamlObject -Path (Join-Path $cacheDir 'current-turn.yaml') -Document ([ordered]@{
                turnRequestId = 'req-20260709T214500Z-degraded-close'
                queryTitle = 'Degraded close test'
                status = 'in_progress'
                auditDialog = 0
            })
            [Console]::SetError($errorWriter)

            Invoke-WorkflowCompleteTurn -ParamsYaml '{"response":"Done"}' | Should -BeTrue

            $errorWriter.ToString() | Should -Match 'degraded'
            $errorWriter.ToString() | Should -Match 'C:\\failsafe\\turn\.yaml'
        } finally {
            [Console]::SetError($originalError)
            if ($previousPersist) {
                Set-Item -Path Function:\Invoke-ReplPersistTurn -Value $previousPersist.ScriptBlock
            }
            if ($previousFresh) {
                Set-Item -Path Function:\Assert-ReplCurrentTurnFresh -Value $previousFresh.ScriptBlock
            }
            if ($null -ne $previousCacheOverride) {
                $env:MCP_CACHE_DIR_OVERRIDE = $previousCacheOverride
            } else {
                Remove-Item Env:\MCP_CACHE_DIR_OVERRIDE -ErrorAction SilentlyContinue
            }
            Remove-Item -LiteralPath $pluginRoot -Recurse -Force -ErrorAction SilentlyContinue
            $errorWriter.Dispose()
        }
    }
}
