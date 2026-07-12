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
            [string]$InputText = '',
            [bool]$RedirectStandardInput = $true
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
        $psi.RedirectStandardInput = $RedirectStandardInput
        $psi.RedirectStandardOutput = $true
        $psi.RedirectStandardError = $true
        foreach ($key in $Environment.Keys) {
            $psi.Environment[$key] = [string]$Environment[$key]
        }

        $process = [System.Diagnostics.Process]::Start($psi)
        if ($RedirectStandardInput) {
            if ($InputText) {
                $process.StandardInput.Write($InputText)
            }
            $process.StandardInput.Close()
        }
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

    It 'TEST-MCP-BUGTRIAGE-034 bash cache scope keeps MCP_CACHE_DIR_OVERRIDE flat' {
        $source = [System.IO.File]::ReadAllText((Join-Path $script:RepoRoot 'plugins\core\lib-sh\cache-scope.sh'))

        $source | Should -Match 'MCP_CACHE_DIR_OVERRIDE'
        $source | Should -Match 'if \[ -n "\$\{MCP_CACHE_DIR_OVERRIDE:-\}" \]; then'
        $source | Should -Match 'MCP_PLUGIN_WORKSPACE_CACHE_DIR="\$\{MCP_PLUGIN_CACHE_ROOT\}"'
        $source | Should -Match 'MCP_PLUGIN_SESSION_CACHE_DIR="\$\{MCP_PLUGIN_CACHE_ROOT\}"'
        $source | Should -Match 'REPL_INVOKE_CACHE_DIR="\$\{MCP_PLUGIN_CACHE_ROOT\}"'
    }
    It 'TEST-MCP-BUGTRIAGE-028 bash cache resolver and scope use canonical workspace agent root' {
        $resolver = [System.IO.File]::ReadAllText((Join-Path $script:RepoRoot 'plugins\core\lib-sh\resolve-cache-dir.sh'))
        $scope = [System.IO.File]::ReadAllText((Join-Path $script:RepoRoot 'plugins\core\lib-sh\cache-scope.sh'))

        $resolver | Should -Match '\.mcpServer'
        $resolver | Should -Match 'resolve_cache_agent_key'
        $resolver.Contains('printf ''%s/cache'' "$configured_workspace"') | Should -BeFalse
        $resolver.Contains('printf ''%s/cache'' "$(dirname "$marker_file")"') | Should -BeFalse
        $scope | Should -Match 'MCP_PLUGIN_WORKSPACE_CACHE_DIR="\$\{MCP_PLUGIN_CACHE_ROOT\}"'
        $scope | Should -Match 'MCP_PLUGIN_SESSION_CACHE_DIR="\$\{MCP_PLUGIN_CACHE_ROOT\}"'
        $scope | Should -Match 'CACHE_DIR="\$MCP_PLUGIN_CACHE_ROOT"'
        $scope | Should -Not -Match 'MCP_PLUGIN_WORKSPACE_CACHE_DIR="\$\{MCP_PLUGIN_CACHE_ROOT\}/workspaces/'
        $scope | Should -Not -Match 'MCP_PLUGIN_SESSION_CACHE_DIR="\$\{MCP_PLUGIN_WORKSPACE_CACHE_DIR\}/sessions/'
    }

    It 'TEST-MCP-BUGTRIAGE-036 creates current-turn.yaml when beginTurn is invoked directly' {
        $pluginRoot = Join-Path $script:SmokeCache ([guid]::NewGuid().ToString('N'))
        $cacheDir = Join-Path $pluginRoot 'cache'
        [void][System.IO.Directory]::CreateDirectory($cacheDir)
        . (Join-Path $script:LibRoot 'yaml-object-mutation.ps1')
        Import-McpYamlSerializer
        $previousCacheOverride = $env:MCP_CACHE_DIR_OVERRIDE
        $previousWorkspace = $env:MCP_WORKSPACE_PATH
        $script:beginTurnPersistArgs = $null

        try {
            $env:MCP_CACHE_DIR_OVERRIDE = $cacheDir
            $env:MCP_WORKSPACE_PATH = $pluginRoot
            . (Join-Path $script:LibRoot 'repl-invoke.ps1')
            $previousPersist = Get-Command Invoke-ReplPersistTurn -CommandType Function -ErrorAction Stop
            $previousSnapshot = Get-Command Get-MarkerFileSnapshot -CommandType Function -ErrorAction Stop
            function Invoke-ReplPersistTurn {
                param(
                    [Parameter(Mandatory)][string]$RequestId,
                    [Parameter(Mandatory)][string]$Title,
                    [Parameter(Mandatory)][string]$Status,
                    [string]$ResponseText = '',
                    [string]$ActionsYaml = '',
                    [object[]]$ProcessingDialog = @()
                )
                $script:beginTurnPersistArgs = [ordered]@{
                    RequestId = $RequestId
                    Title = $Title
                    Status = $Status
                    ResponseText = $ResponseText
                }
                return $true
            }
            function Get-MarkerFileSnapshot {
                param([string]$StartDir)
                [pscustomobject]@{
                    markerFilePath = (Join-Path $StartDir 'AGENTS-README-FIRST.yaml')
                    markerLastWriteUtc = '2026-07-11T00:00:00Z'
                }
            }

            Write-McpYamlObject -Path (Join-Path $cacheDir 'session-state.yaml') -Document ([ordered]@{
                status = 'verified'
                sessionId = 'Codex-20260711T000000Z-plugin-session'
                agent = 'Codex'
                started = '2026-07-11T00:00:00Z'
                lastUpdated = '2026-07-11T00:00:00Z'
            })
            $paramsYaml = [ordered]@{
                requestId = 'req-20260711T000000Z-begin-red'
                queryTitle = 'Begin turn direct test'
                queryText = "Line one`nLine two"
            } | ConvertTo-Yaml -Options WithIndentedSequences

            Invoke-ReplMethod -Method 'workflow.sessionlog.beginTurn' -ParamsYaml $paramsYaml | Out-Null

            $turn = Read-McpYamlObject -Path (Join-Path $cacheDir 'current-turn.yaml')
            $turn['turnRequestId'] | Should -Be 'req-20260711T000000Z-begin-red'
            $turn['queryTitle'] | Should -Be 'Begin turn direct test'
            $turn['queryText'] | Should -Be "Line one`nLine two"
            $turn['status'] | Should -Be 'in_progress'
            $turn['sessionId'] | Should -Be 'Codex-20260711T000000Z-plugin-session'
            $turn['auditActions'] | Should -Be 0
            $script:beginTurnPersistArgs.RequestId | Should -Be 'req-20260711T000000Z-begin-red'
            $script:beginTurnPersistArgs.Status | Should -Be 'in_progress'
            $script:beginTurnPersistArgs.ResponseText | Should -Be '(turn opened)'
            $script:LastInvokeReplMethodSuccess | Should -BeTrue
        } finally {
            if ($previousPersist) {
                Set-Item -Path Function:\Invoke-ReplPersistTurn -Value $previousPersist.ScriptBlock
            }
            if ($previousSnapshot) {
                Set-Item -Path Function:\Get-MarkerFileSnapshot -Value $previousSnapshot.ScriptBlock
            }
            if ($null -ne $previousCacheOverride) {
                $env:MCP_CACHE_DIR_OVERRIDE = $previousCacheOverride
            } else {
                Remove-Item Env:\MCP_CACHE_DIR_OVERRIDE -ErrorAction SilentlyContinue
            }
            if ($null -ne $previousWorkspace) {
                $env:MCP_WORKSPACE_PATH = $previousWorkspace
            } else {
                Remove-Item Env:\MCP_WORKSPACE_PATH -ErrorAction SilentlyContinue
            }
            Remove-Variable -Name beginTurnPersistArgs -Scope Script -ErrorAction SilentlyContinue
            Remove-Item -LiteralPath $pluginRoot -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'TEST-MCP-BUGTRIAGE-041 keeps current turn in_progress when completeTurn persistence fails' {
        $pluginRoot = Join-Path $script:SmokeCache ([guid]::NewGuid().ToString('N'))
        $cacheDir = Join-Path $pluginRoot 'cache'
        [void][System.IO.Directory]::CreateDirectory($cacheDir)
        $previousCacheOverride = $env:MCP_CACHE_DIR_OVERRIDE

        try {
            $env:MCP_CACHE_DIR_OVERRIDE = $cacheDir
            . (Join-Path $script:LibRoot 'repl-invoke.ps1')
            $previousFresh = Get-Command Assert-ReplCurrentTurnFresh -CommandType Function -ErrorAction Stop
            $previousPersist = Get-Command Invoke-ReplPersistTurn -CommandType Function -ErrorAction Stop
            function Assert-ReplCurrentTurnFresh { return $true }
            function Invoke-ReplPersistTurn { return $false }

            Write-McpYamlObject -Path (Join-Path $cacheDir 'session-state.yaml') -Document ([ordered]@{
                status = 'verified'
                sessionId = 'Codex-20260711T000000Z-plugin-session'
                agent = 'Codex'
            })
            Write-McpYamlObject -Path (Join-Path $cacheDir 'current-turn.yaml') -Document ([ordered]@{
                turnRequestId = 'req-20260711T000001Z-complete-red'
                queryTitle = 'Complete failure test'
                status = 'in_progress'
                queryText = 'Complete failure test'
                auditActions = 0
            })

            Invoke-WorkflowCompleteTurn -ParamsYaml '{"response":"Done"}' | Should -BeFalse
            (Read-McpYamlObject -Path (Join-Path $cacheDir 'current-turn.yaml'))['status'] | Should -Be 'in_progress'
        } finally {
            if ($previousFresh) {
                Set-Item -Path Function:\Assert-ReplCurrentTurnFresh -Value $previousFresh.ScriptBlock
            }
            if ($previousPersist) {
                Set-Item -Path Function:\Invoke-ReplPersistTurn -Value $previousPersist.ScriptBlock
            }
            if ($null -ne $previousCacheOverride) {
                $env:MCP_CACHE_DIR_OVERRIDE = $previousCacheOverride
            } else {
                Remove-Item Env:\MCP_CACHE_DIR_OVERRIDE -ErrorAction SilentlyContinue
            }
            Remove-Item -LiteralPath $pluginRoot -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'TEST-MCP-BUGTRIAGE-046 counts list-item action audit fields' {
        $pluginRoot = Join-Path $script:SmokeCache ([guid]::NewGuid().ToString('N'))
        $cacheDir = Join-Path $pluginRoot 'cache'
        [void][System.IO.Directory]::CreateDirectory($cacheDir)
        $previousCacheOverride = $env:MCP_CACHE_DIR_OVERRIDE
        $script:appendActionsPersistArgs = $null

        try {
            $env:MCP_CACHE_DIR_OVERRIDE = $cacheDir
            . (Join-Path $script:LibRoot 'repl-invoke.ps1')
            $previousFresh = Get-Command Assert-ReplCurrentTurnFresh -CommandType Function -ErrorAction Stop
            $previousPersist = Get-Command Invoke-ReplPersistTurn -CommandType Function -ErrorAction Stop
            function Assert-ReplCurrentTurnFresh { return $true }
            function Invoke-ReplPersistTurn {
                param(
                    [Parameter(Mandatory)][string]$RequestId,
                    [Parameter(Mandatory)][string]$Title,
                    [Parameter(Mandatory)][string]$Status,
                    [string]$ResponseText = '',
                    [string]$ActionsYaml = '',
                    [object[]]$ProcessingDialog = @()
                )
                $script:appendActionsPersistArgs = [ordered]@{
                    RequestId = $RequestId
                    Title = $Title
                    Status = $Status
                    ActionsYaml = $ActionsYaml
                }
                return $true
            }

            Write-McpYamlObject -Path (Join-Path $cacheDir 'session-state.yaml') -Document ([ordered]@{
                status = 'verified'
                sessionId = 'Codex-20260711T000000Z-plugin-session'
                agent = 'Codex'
            })
            Write-McpYamlObject -Path (Join-Path $cacheDir 'current-turn.yaml') -Document ([ordered]@{
                turnRequestId = 'req-20260711T000002Z-actions-red'
                queryTitle = 'Append actions list counter test'
                status = 'in_progress'
                queryText = 'Append actions list counter test'
                codeEdits = 0
                auditActions = 0
                auditFiles = 0
                auditDecisions = 0
                auditCommits = 0
            })
            $paramsYaml = @'
actions:
  - type: design_decision
    description: Chose the direct beginTurn cache owner
    status: completed
  - type: commit
    description: Captured checkpoint commit
    status: completed
  - filePath: src/FirstKey.cs
    type: edit
    description: Edited first-key file path
    status: completed
  - type: edit
    description: Edited nested file path
    status: completed
    filePath: src/Nested.cs
'@

            Invoke-WorkflowAppendActions -ParamsYaml $paramsYaml | Should -BeTrue

            $turn = Read-McpYamlObject -Path (Join-Path $cacheDir 'current-turn.yaml')
            $turn['auditActions'] | Should -Be 4
            $turn['auditFiles'] | Should -Be 2
            $turn['auditDecisions'] | Should -Be 1
            $turn['auditCommits'] | Should -Be 1
            $turn['codeEdits'] | Should -Be 2
            $script:appendActionsPersistArgs.ActionsYaml | Should -Match 'type: design_decision'
            $script:appendActionsPersistArgs.ActionsYaml | Should -Match 'filePath: src/Nested.cs'
        } finally {
            if ($previousFresh) {
                Set-Item -Path Function:\Assert-ReplCurrentTurnFresh -Value $previousFresh.ScriptBlock
            }
            if ($previousPersist) {
                Set-Item -Path Function:\Invoke-ReplPersistTurn -Value $previousPersist.ScriptBlock
            }
            if ($null -ne $previousCacheOverride) {
                $env:MCP_CACHE_DIR_OVERRIDE = $previousCacheOverride
            } else {
                Remove-Item Env:\MCP_CACHE_DIR_OVERRIDE -ErrorAction SilentlyContinue
            }
            Remove-Variable -Name appendActionsPersistArgs -Scope Script -ErrorAction SilentlyContinue
            Remove-Item -LiteralPath $pluginRoot -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'TEST-MCP-BUGTRIAGE-067 handles updateTurn locally with cached session state' {
        $pluginRoot = Join-Path $script:SmokeCache ([guid]::NewGuid().ToString('N'))
        $cacheDir = Join-Path $pluginRoot 'cache'
        [void][System.IO.Directory]::CreateDirectory($cacheDir)
        $previousCacheOverride = $env:MCP_CACHE_DIR_OVERRIDE
        $script:updateTurnPersistArgs = $null

        try {
            $env:MCP_CACHE_DIR_OVERRIDE = $cacheDir
            . (Join-Path $script:LibRoot 'repl-invoke.ps1')
            $previousFresh = Get-Command Assert-ReplCurrentTurnFresh -CommandType Function -ErrorAction Stop
            $previousPersist = Get-Command Invoke-ReplPersistTurn -CommandType Function -ErrorAction Stop
            $previousRaw = Get-Command Invoke-ReplRaw -CommandType Function -ErrorAction SilentlyContinue
            function Assert-ReplCurrentTurnFresh { return $true }
            function Invoke-ReplRaw { throw 'raw dispatch should not be used for workflow.sessionlog.updateTurn' }
            function Invoke-ReplPersistTurn {
                param(
                    [Parameter(Mandatory)][string]$RequestId,
                    [Parameter(Mandatory)][string]$Title,
                    [Parameter(Mandatory)][string]$Status,
                    [string]$ResponseText = '',
                    [string]$ActionsYaml = '',
                    [object[]]$ProcessingDialog = @(),
                    [string]$Interpretation = '',
                    [int]$TokenCount = 0,
                    [string[]]$Tags = @(),
                    [string[]]$ContextList = @()
                )
                $script:updateTurnPersistArgs = [ordered]@{
                    RequestId = $RequestId
                    Title = $Title
                    Status = $Status
                    ResponseText = $ResponseText
                    Interpretation = $Interpretation
                    TokenCount = $TokenCount
                    Tags = @($Tags)
                    ContextList = @($ContextList)
                }
                return $true
            }

            Write-McpYamlObject -Path (Join-Path $cacheDir 'session-state.yaml') -Document ([ordered]@{
                status = 'verified'
                sessionId = 'Codex-20260712T000000Z-plugin-session'
                agent = 'Codex'
            })
            Write-McpYamlObject -Path (Join-Path $cacheDir 'current-turn.yaml') -Document ([ordered]@{
                turnRequestId = 'req-20260712T000001Z-update-red'
                queryTitle = 'Original update title'
                status = 'in_progress'
                queryText = 'Original update prompt'
                response = 'previous response'
            })
            $paramsYaml = @'
queryTitle: Updated update title
response: Captured response
interpretation: Captured interpretation
tokenCount: 321
tags:
- triage
- codex
contextList:
- plugins/core/lib-ps/repl-invoke.ps1
'@

            Invoke-ReplMethod -Method 'workflow.sessionlog.updateTurn' -ParamsYaml $paramsYaml

            $script:LastInvokeReplMethodSuccess | Should -BeTrue
            $script:updateTurnPersistArgs.RequestId | Should -Be 'req-20260712T000001Z-update-red'
            $script:updateTurnPersistArgs.Title | Should -Be 'Updated update title'
            $script:updateTurnPersistArgs.Status | Should -Be 'in_progress'
            $script:updateTurnPersistArgs.ResponseText | Should -Be 'Captured response'
            $script:updateTurnPersistArgs.Interpretation | Should -Be 'Captured interpretation'
            $script:updateTurnPersistArgs.TokenCount | Should -Be 321
            $script:updateTurnPersistArgs.Tags | Should -Be @('triage', 'codex')
            $script:updateTurnPersistArgs.ContextList | Should -Be @('plugins/core/lib-ps/repl-invoke.ps1')

            $turn = Read-McpYamlObject -Path (Join-Path $cacheDir 'current-turn.yaml')
            $turn['queryTitle'] | Should -Be 'Updated update title'
            $turn['response'] | Should -Be 'Captured response'
            $turn['interpretation'] | Should -Be 'Captured interpretation'
            $turn['tokenCount'] | Should -Be 321
            @($turn['tags']) | Should -Be @('triage', 'codex')
            @($turn['contextList']) | Should -Be @('plugins/core/lib-ps/repl-invoke.ps1')
        } finally {
            if ($previousFresh) {
                Set-Item -Path Function:\Assert-ReplCurrentTurnFresh -Value $previousFresh.ScriptBlock
            }
            if ($previousPersist) {
                Set-Item -Path Function:\Invoke-ReplPersistTurn -Value $previousPersist.ScriptBlock
            }
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
            Remove-Variable -Name updateTurnPersistArgs -Scope Script -ErrorAction SilentlyContinue
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
        $source | Should -Match 'turn-open-failed'
        $source | Should -Match 'workflow\.sessionlog\.beginTurn did not create current-turn\.yaml'
        $source | Should -Match 'openedRequestId'
        $source | Should -Not -Match 'Write-McpYamlObject -Path \$turnFile -Document \$turnState'
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
        $source | Should -Match 'health-check the marker'
        $source | Should -Match 'Assert-ReplCurrentTurnFresh -Method ''workflow.sessionlog.appendActions'''
        $source | Should -Match 'Assert-ReplCurrentTurnFresh -Method ''workflow.sessionlog.completeTurn'''
    }


    It 'TEST-MCP-BUGTRIAGE-052 refreshes marker-only drift before appendActions' {
        $root = Join-Path $script:SmokeCache ([guid]::NewGuid().ToString('N'))
        $workspace = Join-Path $root 'workspace'
        $cacheDir = Join-Path $root 'cache'
        [void][System.IO.Directory]::CreateDirectory($workspace)
        [void][System.IO.Directory]::CreateDirectory($cacheDir)
        $previousCacheOverride = $env:MCP_CACHE_DIR_OVERRIDE
        $previousWorkspace = $env:MCP_WORKSPACE_PATH
        $previousAgent = $env:MCP_AGENT_NAME
        $previousBootstrap = $null
        $previousPersist = $null
        $script:capturedActionsYaml = $null

        try {
            $env:MCP_CACHE_DIR_OVERRIDE = $cacheDir
            $env:MCP_WORKSPACE_PATH = $workspace
            $env:MCP_AGENT_NAME = 'Codex'
            . (Join-Path $script:LibRoot 'repl-invoke.ps1')
            $previousBootstrap = Get-Command Invoke-FullBootstrap -CommandType Function -ErrorAction Stop
            $previousPersist = Get-Command Invoke-ReplPersistTurn -CommandType Function -ErrorAction Stop
            function Invoke-FullBootstrap { param([string]$StartDir) return $true }
            function Invoke-ReplPersistTurn {
                param(
                    [string]$RequestId,
                    [string]$Title,
                    [string]$Status,
                    [string]$ResponseText,
                    [string]$ActionsYaml
                )
                $script:capturedActionsYaml = $ActionsYaml
                return $true
            }

            $marker = Join-Path $workspace 'AGENTS-README-FIRST.yaml'
            [System.IO.File]::WriteAllText($marker, "workspacePath: $workspace`n")
            $fingerprintA = [datetime]'2026-07-11T19:00:00Z'
            [System.IO.File]::SetLastWriteTimeUtc($marker, $fingerprintA)
            $snapshotA = Get-MarkerFileSnapshot -StartDir $workspace
            Write-McpYamlObject -Path (Join-Path $cacheDir 'session-state.yaml') -Document ([ordered]@{
                status = 'verified'
                sessionId = 'Codex-20260711T190000Z-plugin-session'
                agent = 'Codex'
                markerFilePath = $snapshotA.markerFilePath
                markerLastWriteUtc = $snapshotA.markerLastWriteUtc
            })
            Write-McpYamlObject -Path (Join-Path $cacheDir 'current-turn.yaml') -Document ([ordered]@{
                turnRequestId = 'req-20260711T190000Z-marker-drift'
                queryTitle = 'Marker drift test'
                status = 'in_progress'
                sessionId = 'Codex-20260711T190000Z-plugin-session'
                auditActions = 0
                markerFilePath = $snapshotA.markerFilePath
                markerLastWriteUtc = $snapshotA.markerLastWriteUtc
            })
            $fingerprintB = [datetime]'2026-07-11T19:05:00Z'
            [System.IO.File]::SetLastWriteTimeUtc($marker, $fingerprintB)
            $snapshotB = Get-MarkerFileSnapshot -StartDir $workspace
            $payload = [ordered]@{
                actions = @(
                    [ordered]@{
                        type = 'test'
                        description = 'append after marker drift'
                    }
                )
            } | ConvertTo-Yaml -Options WithIndentedSequences

            Invoke-WorkflowAppendActions -ParamsYaml $payload | Should -BeTrue
            $turnState = Read-McpYamlObject -Path (Join-Path $cacheDir 'current-turn.yaml')
            $turnState['markerFilePath'] | Should -Be $snapshotB.markerFilePath
            $turnState['markerLastWriteUtc'] | Should -Be $snapshotB.markerLastWriteUtc
            $turnState['auditActions'] | Should -Be 1
            $script:capturedActionsYaml | Should -Match 'append after marker drift'
        } finally {
            if ($previousPersist) { Set-Item -Path Function:\Invoke-ReplPersistTurn -Value $previousPersist.ScriptBlock }
            if ($previousBootstrap) { Set-Item -Path Function:\Invoke-FullBootstrap -Value $previousBootstrap.ScriptBlock }
            if ($null -ne $previousCacheOverride) { $env:MCP_CACHE_DIR_OVERRIDE = $previousCacheOverride } else { Remove-Item Env:\MCP_CACHE_DIR_OVERRIDE -ErrorAction SilentlyContinue }
            if ($null -ne $previousWorkspace) { $env:MCP_WORKSPACE_PATH = $previousWorkspace } else { Remove-Item Env:\MCP_WORKSPACE_PATH -ErrorAction SilentlyContinue }
            if ($null -ne $previousAgent) { $env:MCP_AGENT_NAME = $previousAgent } else { Remove-Item Env:\MCP_AGENT_NAME -ErrorAction SilentlyContinue }
            Remove-Variable -Name capturedActionsYaml -Scope Script -ErrorAction SilentlyContinue
            Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
    It 'TEST-MCP-BUGTRIAGE-058 stores post-bootstrap marker snapshot in session-state' {
        $root = Join-Path $script:SmokeCache ([guid]::NewGuid().ToString('N'))
        $workspace = Join-Path $root 'workspace'
        $cacheDir = Join-Path $root 'cache'
        [void][System.IO.Directory]::CreateDirectory($workspace)
        [void][System.IO.Directory]::CreateDirectory($cacheDir)
        $previousCacheOverride = $env:MCP_CACHE_DIR_OVERRIDE
        $previousWorkspace = $env:MCP_WORKSPACE_PATH
        $previousAgent = $env:MCP_AGENT_NAME
        $previousSnapshot = $null
        $previousBootstrap = $null
        $script:markerSnapshotCalls = 0

        try {
            $env:MCP_CACHE_DIR_OVERRIDE = $cacheDir
            $env:MCP_WORKSPACE_PATH = $workspace
            $env:MCP_AGENT_NAME = 'Codex'
            . (Join-Path $script:LibRoot 'repl-invoke.ps1')
            $previousSnapshot = Get-Command Get-MarkerFileSnapshot -CommandType Function -ErrorAction Stop
            $previousBootstrap = Get-Command Invoke-FullBootstrap -CommandType Function -ErrorAction Stop
            function Get-MarkerFileSnapshot {
                param([string]$StartDir)

                $script:markerSnapshotCalls++
                $stamp = if ($script:markerSnapshotCalls -eq 1) {
                    '2026-07-11T19:00:00Z'
                } else {
                    '2026-07-11T19:05:00Z'
                }

                [ordered]@{
                    markerFilePath = Join-Path $StartDir 'AGENTS-README-FIRST.yaml'
                    markerLastWriteUtc = $stamp
                }
            }
            function Invoke-FullBootstrap { param([string]$StartDir) return $true }

            Write-McpYamlObject -Path (Join-Path $cacheDir 'session-state.yaml') -Document ([ordered]@{
                status = 'MCP_UNTRUSTED'
                markerFilePath = Join-Path $workspace 'AGENTS-README-FIRST.yaml'
                markerLastWriteUtc = '2026-07-11T18:55:00Z'
            })

            Assert-ReplMarkerFresh | Should -BeTrue

            $state = Read-McpYamlObject -Path (Join-Path $cacheDir 'session-state.yaml')
            $state['status'] | Should -Be 'verified'
            $state['markerLastWriteUtc'] | Should -Be '2026-07-11T19:05:00Z'
            $state['sessionId'] | Should -Not -BeNullOrEmpty
            $script:markerSnapshotCalls | Should -Be 2
        } finally {
            if ($previousSnapshot) { Set-Item -Path Function:\Get-MarkerFileSnapshot -Value $previousSnapshot.ScriptBlock }
            if ($previousBootstrap) { Set-Item -Path Function:\Invoke-FullBootstrap -Value $previousBootstrap.ScriptBlock }
            if ($null -ne $previousCacheOverride) { $env:MCP_CACHE_DIR_OVERRIDE = $previousCacheOverride } else { Remove-Item Env:\MCP_CACHE_DIR_OVERRIDE -ErrorAction SilentlyContinue }
            if ($null -ne $previousWorkspace) { $env:MCP_WORKSPACE_PATH = $previousWorkspace } else { Remove-Item Env:\MCP_WORKSPACE_PATH -ErrorAction SilentlyContinue }
            if ($null -ne $previousAgent) { $env:MCP_AGENT_NAME = $previousAgent } else { Remove-Item Env:\MCP_AGENT_NAME -ErrorAction SilentlyContinue }
            Remove-Variable -Name markerSnapshotCalls -Scope Script -ErrorAction SilentlyContinue
            Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
    It 'TEST-MCP-BUGTRIAGE-029 completeTurn refreshes marker-only drift before closeout' {
        $root = Join-Path $script:SmokeCache ([guid]::NewGuid().ToString('N'))
        $workspace = Join-Path $root 'workspace'
        $cacheDir = Join-Path $root 'cache'
        [void][System.IO.Directory]::CreateDirectory($workspace)
        [void][System.IO.Directory]::CreateDirectory($cacheDir)
        $previousCacheOverride = $env:MCP_CACHE_DIR_OVERRIDE
        $previousWorkspace = $env:MCP_WORKSPACE_PATH
        $previousAgent = $env:MCP_AGENT_NAME
        $previousBootstrap = $null
        $previousPersist = $null
        $script:capturedCompleteStatus = $null

        try {
            $env:MCP_CACHE_DIR_OVERRIDE = $cacheDir
            $env:MCP_WORKSPACE_PATH = $workspace
            $env:MCP_AGENT_NAME = 'Codex'
            . (Join-Path $script:LibRoot 'repl-invoke.ps1')
            $previousBootstrap = Get-Command Invoke-FullBootstrap -CommandType Function -ErrorAction Stop
            $previousPersist = Get-Command Invoke-ReplPersistTurn -CommandType Function -ErrorAction Stop
            function Invoke-FullBootstrap { param([string]$StartDir) return $true }
            function Invoke-ReplPersistTurn {
                param(
                    [string]$RequestId,
                    [string]$Title,
                    [string]$Status,
                    [string]$ResponseText,
                    [string]$ActionsYaml
                )
                $script:capturedCompleteStatus = $Status
                return $true
            }

            $marker = Join-Path $workspace 'AGENTS-README-FIRST.yaml'
            [System.IO.File]::WriteAllText($marker, "workspacePath: $workspace`n")
            [System.IO.File]::SetLastWriteTimeUtc($marker, [datetime]'2026-07-11T19:10:00Z')
            $snapshotA = Get-MarkerFileSnapshot -StartDir $workspace
            Write-McpYamlObject -Path (Join-Path $cacheDir 'session-state.yaml') -Document ([ordered]@{
                status = 'verified'
                sessionId = 'Codex-20260711T191000Z-plugin-session'
                agent = 'Codex'
                markerFilePath = $snapshotA.markerFilePath
                markerLastWriteUtc = $snapshotA.markerLastWriteUtc
            })
            Write-McpYamlObject -Path (Join-Path $cacheDir 'current-turn.yaml') -Document ([ordered]@{
                turnRequestId = 'req-20260711T191000Z-closeout-drift'
                queryTitle = 'Closeout marker drift test'
                status = 'in_progress'
                sessionId = 'Codex-20260711T191000Z-plugin-session'
                auditActions = 0
                markerFilePath = $snapshotA.markerFilePath
                markerLastWriteUtc = $snapshotA.markerLastWriteUtc
            })
            [System.IO.File]::SetLastWriteTimeUtc($marker, [datetime]'2026-07-11T19:15:00Z')
            $snapshotB = Get-MarkerFileSnapshot -StartDir $workspace

            Invoke-WorkflowCompleteTurn -ParamsYaml 'response: Done after marker drift' | Should -BeTrue
            $turnState = Read-McpYamlObject -Path (Join-Path $cacheDir 'current-turn.yaml')
            $turnState['status'] | Should -Be 'completed'
            $turnState['markerFilePath'] | Should -Be $snapshotB.markerFilePath
            $turnState['markerLastWriteUtc'] | Should -Be $snapshotB.markerLastWriteUtc
            $script:capturedCompleteStatus | Should -Be 'completed'
        } finally {
            if ($previousPersist) { Set-Item -Path Function:\Invoke-ReplPersistTurn -Value $previousPersist.ScriptBlock }
            if ($previousBootstrap) { Set-Item -Path Function:\Invoke-FullBootstrap -Value $previousBootstrap.ScriptBlock }
            if ($null -ne $previousCacheOverride) { $env:MCP_CACHE_DIR_OVERRIDE = $previousCacheOverride } else { Remove-Item Env:\MCP_CACHE_DIR_OVERRIDE -ErrorAction SilentlyContinue }
            if ($null -ne $previousWorkspace) { $env:MCP_WORKSPACE_PATH = $previousWorkspace } else { Remove-Item Env:\MCP_WORKSPACE_PATH -ErrorAction SilentlyContinue }
            if ($null -ne $previousAgent) { $env:MCP_AGENT_NAME = $previousAgent } else { Remove-Item Env:\MCP_AGENT_NAME -ErrorAction SilentlyContinue }
            Remove-Variable -Name capturedCompleteStatus -Scope Script -ErrorAction SilentlyContinue
            Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue
        }
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

    It 'TEST-MCP-BUGTRIAGE-038 exposes discoverable object-safe triage helpers' {
        Remove-Module McpPluginShim -Force -ErrorAction SilentlyContinue
        Import-Module (Join-Path $script:LibRoot 'McpPluginShim.psm1') -Force

        $commands = @(Get-Command -Module McpPluginShim '*Triage*').Name
        $commands | Should -Contain 'New-McpTriageReportParams'
        $commands | Should -Contain 'New-McpTriageGetReportParams'
        $commands | Should -Contain 'New-McpTriageQueryGroupsParams'

        $params = New-McpTriageReportParams `
            -Title 'Object-safe triage report' `
            -Summary 'The plugin exposes a typed object path for triage reports.' `
            -Component 'mcpserver-plugin' `
            -AffectedPaths @('lib/repl-invoke.ps1') `
            -AffectedSymbols @('workflow.triage.report') `
            -ErrorSignature 'object_safe_triage' `
            -ReporterAgent 'Codex'

        $params.title | Should -Be 'Object-safe triage report'
        $params.affectedPaths | Should -Be @('lib/repl-invoke.ps1')
        $params.reporterAgent | Should -Be 'Codex'

        $request = New-McpPluginReplRequest `
            -RequestId 'req-20260711T203000Z-triage' `
            -Method 'workflow.triage.report' `
            -Params $params
        $json = ConvertTo-McpPluginJson -InputObject $request -Depth 20 -Compress
        $json | Should -Match '"method":"workflow.triage.report"'
        $json | Should -Match '"title":"Object-safe triage report"'

        $help = Get-Help New-McpTriageReportParams -Full | Out-String -Width 200
        $help | Should -Match 'workflow\.triage\.report'
        $help | Should -Match 'Title'
        $help | Should -Match 'ReporterAgent'
    }

    It 'TEST-MCP-BUGTRIAGE-038 Invoke-McpPlugin serializes ParamsObject for triage reports' {
        Remove-Module McpPluginShim -Force -ErrorAction SilentlyContinue
        Import-Module (Join-Path $script:LibRoot 'McpPluginShim.psm1') -Force

        $root = Join-Path $script:SmokeCache ([guid]::NewGuid().ToString('N'))
        $workspace = Join-Path $root 'workspace'
        $pluginRoot = Join-Path $root 'plugin'
        $libRoot = Join-Path $pluginRoot 'lib'
        $cacheRoot = Join-Path $root 'cache'
        $capturePath = Join-Path $root 'params.yaml'
        [void][System.IO.Directory]::CreateDirectory($workspace)
        [void][System.IO.Directory]::CreateDirectory($libRoot)
        [void][System.IO.Directory]::CreateDirectory($cacheRoot)

        $replStub = @'
#Requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Method,
    [string]$ParamsYaml = ''
)

[System.IO.File]::WriteAllText($env:MCP_CAPTURE_METHOD_PATH, $Method, [System.Text.UTF8Encoding]::new($false))
[System.IO.File]::WriteAllText($env:MCP_CAPTURE_PARAMS_PATH, $ParamsYaml, [System.Text.UTF8Encoding]::new($false))
'{}'
'@
        [System.IO.File]::WriteAllText((Join-Path $libRoot 'repl-invoke.ps1'), $replStub, [System.Text.UTF8Encoding]::new($false))

        $previousCaptureParams = $env:MCP_CAPTURE_PARAMS_PATH
        $previousCaptureMethod = $env:MCP_CAPTURE_METHOD_PATH
        $env:MCP_CAPTURE_PARAMS_PATH = $capturePath
        $env:MCP_CAPTURE_METHOD_PATH = (Join-Path $root 'method.txt')

        try {
            $params = New-McpTriageReportParams `
                -Title 'ParamsObject triage report' `
                -Summary 'The wrapper converts native PowerShell objects to YAML.' `
                -Component 'mcpserver-plugin' `
                -AffectedPaths @('lib/Invoke-McpPlugin.ps1') `
                -ErrorSignature 'params_object_triage' `
                -ReporterAgent 'Codex'

            $output = & (Join-Path $script:LibRoot 'Invoke-McpPlugin.ps1') `
                -Command Invoke `
                -Method 'workflow.triage.report' `
                -ParamsObject $params `
                -WorkspacePath $workspace `
                -PluginRoot $pluginRoot `
                -CacheRoot $cacheRoot `
                -TimeoutSeconds 5

            $output | Should -Be '{}'
            [System.IO.File]::ReadAllText($env:MCP_CAPTURE_METHOD_PATH) | Should -Be 'workflow.triage.report'
            $paramsYaml = [System.IO.File]::ReadAllText($capturePath)
            $paramsYaml | Should -Match 'title: ParamsObject triage report'
            $paramsYaml | Should -Match 'reporterAgent: Codex'
            $paramsYaml | Should -Not -Match '^type: request'
        } finally {
            if ($null -ne $previousCaptureParams) { $env:MCP_CAPTURE_PARAMS_PATH = $previousCaptureParams } else { Remove-Item Env:\MCP_CAPTURE_PARAMS_PATH -ErrorAction SilentlyContinue }
            if ($null -ne $previousCaptureMethod) { $env:MCP_CAPTURE_METHOD_PATH = $previousCaptureMethod } else { Remove-Item Env:\MCP_CAPTURE_METHOD_PATH -ErrorAction SilentlyContinue }
            Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue
        }
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
                'ParamsObject',
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

    It 'TEST-MCP-BUGTRIAGE-037 wrapper prefers explicit markerless workspace cache over stale override' {
        . (Join-Path $script:LibRoot 'yaml-object-mutation.ps1')
        Import-McpYamlSerializer

        $root = Join-Path $script:SmokeCache ([guid]::NewGuid().ToString('N'))
        $workspace = Join-Path $root 'markerless-workspace'
        $staleOverride = Join-Path $root 'stale-cache'
        $sourcePluginRoot = Join-Path $script:RepoRoot 'plugins\core'
        $workspaceCache = Join-Path $workspace '.mcpServer\codex'
        [void][System.IO.Directory]::CreateDirectory($workspaceCache)
        [void][System.IO.Directory]::CreateDirectory($staleOverride)
        try {
            Write-McpYamlObject -Path (Join-Path $workspaceCache 'session-state.yaml') -Document ([ordered]@{
                status = 'verified'
                sessionId = 'Codex-20260711T000000Z-markerless'
                agent = 'Codex'
            })
            Write-McpYamlObject -Path (Join-Path $workspaceCache 'current-turn.yaml') -Document ([ordered]@{
                turnRequestId = 'req-20260711T000003Z-markerless'
                queryTitle = 'Markerless workspace cache test'
                status = 'in_progress'
                sessionId = 'Codex-20260711T000000Z-markerless'
                queryText = 'Markerless workspace cache test'
                codeEdits = 0
                auditActions = 0
            })

            $result = Invoke-PluginChildProcess `
                -ScriptPath (Join-Path $script:LibRoot 'Invoke-McpPlugin.ps1') `
                -Arguments @('-Command', 'Invoke', '-Method', 'workflow.sessionlog.appendActions', '-WorkspacePath', $workspace, '-PluginRoot', $sourcePluginRoot, '-TimeoutSeconds', '5') `
                -Environment @{
                    MCP_CACHE_DIR_OVERRIDE = $staleOverride
                    MCP_AGENT_NAME = 'Codex'
                    PLUGIN_AGENT_DEFAULT = 'Codex'
                    MCP_PLUGIN_ROOT = $sourcePluginRoot
                }

            $result.ExitCode | Should -Be 0
            ($result.Stdout + $result.Stderr) | Should -Not -Match ([regex]::Escape($staleOverride))
        } finally {
            Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'TEST-MCP-BUGTRIAGE-023 Claude wrapper prefers active workspace cache over foreign env cache' {
        . (Join-Path $script:LibRoot 'yaml-object-mutation.ps1')
        Import-McpYamlSerializer

        $root = Join-Path $script:SmokeCache ([guid]::NewGuid().ToString('N'))
        $activeWorkspace = Join-Path $root 'active-workspace'
        $foreignWorkspace = Join-Path $root 'foreign-workspace'
        $activeCache = Join-Path $activeWorkspace '.mcpServer\claude'
        $foreignCache = Join-Path $foreignWorkspace '.mcpServer\claude'
        $sourcePluginRoot = Join-Path $script:RepoRoot 'plugins\core'
        [void][System.IO.Directory]::CreateDirectory($activeCache)
        [void][System.IO.Directory]::CreateDirectory($foreignCache)

        try {
            Write-McpYamlObject -Path (Join-Path $activeCache 'session-state.yaml') -Document ([ordered]@{
                status = 'verified'
                sessionId = 'ClaudeCode-20260711T000000Z-active'
                agent = 'ClaudeCode'
            })
            Write-McpYamlObject -Path (Join-Path $activeCache 'current-turn.yaml') -Document ([ordered]@{
                turnRequestId = 'req-20260711T000004Z-claude-active'
                queryTitle = 'Claude active workspace cache test'
                status = 'in_progress'
                sessionId = 'ClaudeCode-20260711T000000Z-active'
                queryText = 'Claude active workspace cache test'
                codeEdits = 0
                auditActions = 0
                auditFiles = 0
                auditDecisions = 0
                auditCommits = 0
            })
            Write-McpYamlObject -Path (Join-Path $foreignCache 'session-state.yaml') -Document ([ordered]@{
                status = 'verified'
                sessionId = 'ClaudeCode-20260711T000000Z-foreign'
                agent = 'ClaudeCode'
            })

            $result = Invoke-PluginChildProcess `
                -ScriptPath (Join-Path $script:LibRoot 'Invoke-McpPlugin.ps1') `
                -Arguments @('-Command', 'Invoke', '-Method', 'workflow.sessionlog.appendActions', '-WorkspacePath', $activeWorkspace, '-PluginRoot', $sourcePluginRoot, '-TimeoutSeconds', '5') `
                -Environment @{
                    MCP_CACHE_DIR_OVERRIDE = $foreignCache
                    MCP_WORKSPACE_PATH = $foreignWorkspace
                    MCPSERVER_WORKSPACE_PATH = $foreignWorkspace
                    CLAUDE_PROJECT_DIR = $foreignWorkspace
                    MCP_AGENT_NAME = 'ClaudeCode'
                    PLUGIN_AGENT_DEFAULT = 'ClaudeCode'
                    MCP_PLUGIN_ROOT = $sourcePluginRoot
                }

            $result.ExitCode | Should -Be 0
            ($result.Stdout + $result.Stderr) | Should -Not -Match ([regex]::Escape($foreignCache))
            $turn = Read-McpYamlObject -Path (Join-Path $activeCache 'current-turn.yaml')
            $turn['sessionId'] | Should -Be 'ClaudeCode-20260711T000000Z-active'
            Test-Path (Join-Path $foreignCache 'current-turn.yaml') | Should -BeFalse
        } finally {
            Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue
        }
    }


    It 'TEST-MCP-BUGTRIAGE-024 Claude Invoke-McpPlugin pins child host over ambient Codex' {
        $root = Join-Path $script:SmokeCache ([guid]::NewGuid().ToString('N'))
        $workspace = Join-Path $root 'workspace'
        $pluginRoot = Join-Path $root 'mcpserver-claude-code-plugin'
        $libRoot = Join-Path $pluginRoot 'lib'
        $ambientCodexCache = Join-Path $root 'codex-cache'
        [void][System.IO.Directory]::CreateDirectory($workspace)
        [void][System.IO.Directory]::CreateDirectory($libRoot)
        [void][System.IO.Directory]::CreateDirectory($ambientCodexCache)

        $replStub = @'
#Requires -Version 7.0
[CmdletBinding()]
param(
    [string]$Method,
    [string]$ParamsYaml = ''
)

[pscustomobject]@{
    host = $env:MCP_PLUGIN_HOST
    agent = $env:MCP_AGENT_NAME
    agentDefault = $env:PLUGIN_AGENT_DEFAULT
    cacheOverride = $env:MCP_CACHE_DIR_OVERRIDE
    workspace = $env:MCP_WORKSPACE_PATH
    claudeProject = $env:CLAUDE_PROJECT_DIR
} | ConvertTo-Json -Compress
'@
        [System.IO.File]::WriteAllText((Join-Path $libRoot 'repl-invoke.ps1'), $replStub, [System.Text.UTF8Encoding]::new($false))

        try {
            $result = Invoke-PluginChildProcess `
                -ScriptPath (Join-Path $script:LibRoot 'Invoke-McpPlugin.ps1') `
                -Arguments @('-Command', 'Invoke', '-Method', 'workflow.sessionlog.appendActions', '-WorkspacePath', $workspace, '-PluginRoot', $pluginRoot, '-TimeoutSeconds', '5') `
                -Environment @{
                    MCP_PLUGIN_HOST = 'codex'
                    MCP_AGENT_NAME = 'Codex'
                    MCP_AGENT_ID = 'Codex'
                    MCP_SESSION_AGENT = 'Codex'
                    PLUGIN_AGENT_DEFAULT = 'Codex'
                    MCP_CACHE_DIR_OVERRIDE = $ambientCodexCache
                    CODEX_WORKSPACE_PATH = Join-Path $root 'codex-workspace'
                    MCP_PLUGIN_ROOT = Join-Path $root 'mcpserver-codex-plugin'
                }

            $result.ExitCode | Should -Be 0
            $child = $result.Stdout | ConvertFrom-Json
            $child.host | Should -Be 'claude-code'
            $child.agent | Should -Be 'ClaudeCode'
            $child.agentDefault | Should -Be 'ClaudeCode'
            $child.cacheOverride | Should -BeNullOrEmpty
            $child.workspace | Should -Be (Resolve-Path -LiteralPath $workspace).ProviderPath
            $child.claudeProject | Should -Be (Resolve-Path -LiteralPath $workspace).ProviderPath
        } finally {
            Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'TEST-MCP-BUGTRIAGE-032 Codex hook rejects verified foreign-agent session state' {
        . (Join-Path $script:LibRoot 'yaml-object-mutation.ps1')
        Import-McpYamlSerializer

        $root = Join-Path $script:SmokeCache ([guid]::NewGuid().ToString('N'))
        $pluginRoot = Join-Path $root 'mcpserver-codex-plugin'
        $libRoot = Join-Path $pluginRoot 'lib'
        $workspace = Join-Path $root 'workspace'
        $cacheDir = Join-Path $workspace '.mcpServer\codex'
        [void][System.IO.Directory]::CreateDirectory($libRoot)
        [void][System.IO.Directory]::CreateDirectory($cacheDir)
        Copy-Item -Path (Join-Path $script:LibRoot '*') -Destination $libRoot -Recurse -Force

        $replStub = @'
#Requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Method,
    [string]$ParamsYaml = ''
)

$ErrorActionPreference = 'Stop'
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
. (Join-Path $scriptDir 'yaml-object-mutation.ps1')
Import-McpYamlSerializer

if ($Method -ne 'workflow.sessionlog.beginTurn') {
    throw "Unexpected method $Method"
}

$params = $ParamsYaml | ConvertFrom-Yaml -Ordered -ErrorAction Stop
$cacheDir = Resolve-Path -LiteralPath (Join-Path $env:MCP_WORKSPACE_PATH '.mcpServer\codex')
Write-McpYamlObject -Path (Join-Path $cacheDir 'current-turn.yaml') -Document ([ordered]@{
    turnRequestId = [string]$params['requestId']
    queryTitle = [string]$params['queryTitle']
    queryText = [string]$params['queryText']
    status = 'active'
    sessionId = 'ClaudeCode-20260711T000000Z-foreign'
})
'{}'
'@
        [System.IO.File]::WriteAllText((Join-Path $libRoot 'repl-invoke.ps1'), $replStub, [System.Text.UTF8Encoding]::new($false))

        $markerPath = Join-Path $workspace 'AGENTS-README-FIRST.yaml'
        [System.IO.File]::WriteAllText($markerPath, "workspacePath: $workspace`nbaseUrl: http://127.0.0.1:1`napiKey: test-key`n", [System.Text.UTF8Encoding]::new($false))
        $markerItem = Get-Item -LiteralPath $markerPath
        Write-McpYamlObject -Path (Join-Path $cacheDir 'session-state.yaml') -Document ([ordered]@{
            status = 'verified'
            sessionId = 'ClaudeCode-20260711T000000Z-foreign'
            agent = 'ClaudeCode'
            markerFilePath = $markerItem.FullName
            markerLastWriteUtc = $markerItem.LastWriteTimeUtc.ToString('O')
        })

        try {
            $result = Invoke-PluginChildProcess `
                -ScriptPath (Join-Path $pluginRoot 'lib\plugin-hook.ps1') `
                -Arguments @('-HookName', 'user-prompt-submit', '-HostName', 'codex', '-WorkspacePath', $workspace, '-Params', '{"prompt":"Codex should not reuse Claude state"}') `
                -Environment @{
                    MCP_PLUGIN_ROOT = $pluginRoot
                    MCP_PLUGIN_HOST = 'codex'
                    MCP_AGENT_NAME = 'Codex'
                    PLUGIN_AGENT_DEFAULT = 'Codex'
                    MCP_WORKSPACE_PATH = $workspace
                    MCPSERVER_WORKSPACE_PATH = $workspace
                    MCP_WORKSPACE_START_DIR = $workspace
                } `
                -RedirectStandardInput:$false

            $result.ExitCode | Should -Be 0
            $output = $result.Stdout | ConvertFrom-Json
            $output.hookSpecificOutput.status | Should -Be 'no-session'
            Test-Path -LiteralPath (Join-Path $cacheDir 'current-turn.yaml') | Should -BeFalse
        } finally {
            Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue
        }
    }


    It 'TEST-MCP-BUGTRIAGE-047 health-checks no-session recovery before suppressing create retries' {
        . (Join-Path $script:LibRoot 'yaml-object-mutation.ps1')
        Import-McpYamlSerializer

        $root = Join-Path $script:SmokeCache ([guid]::NewGuid().ToString('N'))
        $workspace = Join-Path $root 'workspace'
        $pluginRoot = Join-Path $root 'plugin'
        $libRoot = Join-Path $pluginRoot 'lib'
        $cacheDir = Join-Path $root 'cache'
        [void][System.IO.Directory]::CreateDirectory($workspace)
        [void][System.IO.Directory]::CreateDirectory($libRoot)
        [void][System.IO.Directory]::CreateDirectory($cacheDir)
        Copy-Item -Path (Join-Path $script:LibRoot '*') -Destination $libRoot -Recurse -Force

        $markerPath = Join-Path $workspace 'AGENTS-README-FIRST.yaml'
        Write-McpYamlObject -Path $markerPath -Document ([ordered]@{
            workspace = 'NoSessionRecoveryTest'
            workspacePath = $workspace
            baseUrl = 'http://127.0.0.1:1'
            apiKey = 'test-key'
            port = 1
        })

        $markerStub = @'
#Requires -Version 7.0
$script:MARKER_FILENAME = 'AGENTS-README-FIRST.yaml'

function Find-MarkerFile {
    param([string]$StartDir = (Get-Location).Path)
    return (Join-Path $StartDir $script:MARKER_FILENAME)
}

function Get-MarkerFileSnapshot {
    param([string]$StartDir = (Get-Location).Path)
    $path = Find-MarkerFile -StartDir $StartDir
    $item = Get-Item -LiteralPath $path
    [ordered]@{
        markerFilePath = $item.FullName
        markerLastWriteUtc = $item.LastWriteTimeUtc.ToString('O')
    }
}

function Invoke-FullBootstrap {
    param([string]$StartDir = (Get-Location).Path)
    $counterPath = Join-Path $env:MCP_CACHE_DIR_OVERRIDE 'bootstrap-calls.txt'
    $count = 0
    if (Test-Path -LiteralPath $counterPath) {
        $count = [int]([System.IO.File]::ReadAllText($counterPath))
    }
    $count++
    [System.IO.File]::WriteAllText($counterPath, [string]$count)
    if ($count -eq 1 -or $count -eq 2 -or $count -eq 4) { return $false }
    return $true
}
'@
        [System.IO.File]::WriteAllText((Join-Path $libRoot 'marker-resolver.ps1'), $markerStub, [System.Text.UTF8Encoding]::new($false))

        $replStub = @'
#Requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Method,
    [string]$ParamsYaml = ''
)

$ErrorActionPreference = 'Stop'
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
. (Join-Path $scriptDir 'yaml-object-mutation.ps1')
Import-McpYamlSerializer

if ($Method -eq 'workflow.triage.report') {
    $params = $ParamsYaml | ConvertFrom-Yaml -Ordered -ErrorAction Stop
    Write-McpYamlObject -Path (Join-Path $env:MCP_CACHE_DIR_OVERRIDE 'triage-report.yaml') -Document $params
    "type: result`npayload:`n  reportId: BUG-TRIAGE-047"
    exit 0
}

if ($Method -eq 'workflow.sessionlog.beginTurn') {
    $params = $ParamsYaml | ConvertFrom-Yaml -Ordered -ErrorAction Stop
    Write-McpYamlObject -Path (Join-Path $env:MCP_CACHE_DIR_OVERRIDE 'current-turn.yaml') -Document ([ordered]@{
        turnRequestId = [string]$params['requestId']
        queryTitle = [string]$params['queryTitle']
        queryText = [string]$params['queryText']
        status = 'active'
    })
    '{}'
    exit 0
}

throw "Unexpected method $Method"
'@
        [System.IO.File]::WriteAllText((Join-Path $libRoot 'repl-invoke.ps1'), $replStub, [System.Text.UTF8Encoding]::new($false))

        $environment = @{
            MCP_PLUGIN_ROOT = $pluginRoot
            MCP_PLUGIN_HOST = 'codex'
            MCP_AGENT_NAME = 'Codex'
            PLUGIN_AGENT_DEFAULT = 'Codex'
            MCP_CACHE_DIR_OVERRIDE = $cacheDir
            MCP_WORKSPACE_PATH = $workspace
            MCPSERVER_WORKSPACE_PATH = $workspace
            MCP_WORKSPACE_START_DIR = $workspace
        }

        try {
            $first = Invoke-PluginChildProcess `
                -ScriptPath (Join-Path $pluginRoot 'lib\plugin-hook.ps1') `
                -Arguments @('-HookName', 'user-prompt-submit', '-HostName', 'codex', '-WorkspacePath', $workspace, '-Params', '{"prompt":"Recover no-session"}') `
                -Environment $environment `
                -RedirectStandardInput:$false

            $first.ExitCode | Should -Be 0
            $firstOutput = $first.Stdout | ConvertFrom-Json
            $firstOutput.hookSpecificOutput.status | Should -Be 'no-session'
            $firstOutput.hookSpecificOutput.recoveryStatus | Should -Be 'session-create-failed'
            $firstOutput.hookSpecificOutput.healthStatus | Should -Be 'healthy'
            $firstOutput.hookSpecificOutput.failsafePath | Should -Not -BeNullOrEmpty
            Test-Path -LiteralPath $firstOutput.hookSpecificOutput.failsafePath | Should -BeTrue
            (Read-McpYamlObject -Path (Join-Path $cacheDir 'no-session-recovery.yaml'))['triageSubmitted'] | Should -BeTrue
            Test-Path -LiteralPath (Join-Path $cacheDir 'triage-report.yaml') | Should -BeTrue
            [int]([System.IO.File]::ReadAllText((Join-Path $cacheDir 'bootstrap-calls.txt'))) | Should -Be 4

            $second = Invoke-PluginChildProcess `
                -ScriptPath (Join-Path $pluginRoot 'lib\plugin-hook.ps1') `
                -Arguments @('-HookName', 'user-prompt-submit', '-HostName', 'codex', '-WorkspacePath', $workspace, '-Params', '{"prompt":"Still degraded"}') `
                -Environment $environment `
                -RedirectStandardInput:$false

            $secondOutput = $second.Stdout | ConvertFrom-Json
            $secondOutput.hookSpecificOutput.status | Should -Be 'no-session'
            $secondOutput.hookSpecificOutput.recoveryStatus | Should -Be 'session-create-failed'
            [int]([System.IO.File]::ReadAllText((Join-Path $cacheDir 'bootstrap-calls.txt'))) | Should -Be 5

            (Get-Item -LiteralPath $markerPath).LastWriteTimeUtc = (Get-Date).ToUniversalTime().AddMinutes(1)
            $third = Invoke-PluginChildProcess `
                -ScriptPath (Join-Path $pluginRoot 'lib\plugin-hook.ps1') `
                -Arguments @('-HookName', 'user-prompt-submit', '-HostName', 'codex', '-WorkspacePath', $workspace, '-Params', '{"prompt":"Marker changed"}') `
                -Environment $environment `
                -RedirectStandardInput:$false

            $thirdOutput = $third.Stdout | ConvertFrom-Json
            $thirdOutput.hookSpecificOutput.status | Should -Be 'turn-opened'
            [int]([System.IO.File]::ReadAllText((Join-Path $cacheDir 'bootstrap-calls.txt'))) | Should -Be 6
            Test-Path -LiteralPath (Join-Path $cacheDir 'current-turn.yaml') | Should -BeTrue
        } finally {
            Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue
        }
    }


    It 'TEST-MCP-BUGTRIAGE-039 wrapper defaults workspace to cwd marker over stale env' {
        $root = Join-Path $script:SmokeCache ([guid]::NewGuid().ToString('N'))
        $activeWorkspace = Join-Path $root 'TCS2'
        $staleWorkspace = Join-Path $root 'MouseKeyProxy'
        $pluginRoot = Join-Path $root 'mcpserver-codex-plugin'
        $libRoot = Join-Path $pluginRoot 'lib'
        [void][System.IO.Directory]::CreateDirectory($activeWorkspace)
        [void][System.IO.Directory]::CreateDirectory($staleWorkspace)
        [void][System.IO.Directory]::CreateDirectory($libRoot)
        Copy-Item -Path (Join-Path $script:LibRoot '*') -Destination $libRoot -Recurse -Force

        [System.IO.File]::WriteAllText((Join-Path $activeWorkspace 'AGENTS-README-FIRST.yaml'), "workspacePath: $activeWorkspace`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $staleWorkspace 'AGENTS-README-FIRST.yaml'), "workspacePath: $staleWorkspace`n", [System.Text.UTF8Encoding]::new($false))

        $replStub = @'
#Requires -Version 7.0
[CmdletBinding()]
param(
    [string]$Method,
    [string]$ParamsYaml = ''
)

[pscustomobject]@{
    method = $Method
    workspace = $env:MCP_WORKSPACE_PATH
    serverWorkspace = $env:MCPSERVER_WORKSPACE_PATH
    cacheOverride = $env:MCP_CACHE_DIR_OVERRIDE
} | ConvertTo-Json -Compress
'@
        [System.IO.File]::WriteAllText((Join-Path $libRoot 'repl-invoke.ps1'), $replStub, [System.Text.UTF8Encoding]::new($false))

        try {
            $psi = [System.Diagnostics.ProcessStartInfo]::new()
            $psi.FileName = (Get-Command pwsh -ErrorAction Stop).Source
            $psi.ArgumentList.Add('-NoLogo')
            $psi.ArgumentList.Add('-NoProfile')
            $psi.ArgumentList.Add('-NonInteractive')
            $psi.ArgumentList.Add('-File')
            $psi.ArgumentList.Add((Join-Path $script:LibRoot 'Invoke-McpPlugin.ps1'))
            $psi.ArgumentList.Add('-Command')
            $psi.ArgumentList.Add('Invoke')
            $psi.ArgumentList.Add('-Method')
            $psi.ArgumentList.Add('workflow.sessionlog.appendActions')
            $psi.ArgumentList.Add('-PluginRoot')
            $psi.ArgumentList.Add($pluginRoot)
            $psi.ArgumentList.Add('-TimeoutSeconds')
            $psi.ArgumentList.Add('5')
            $psi.WorkingDirectory = $activeWorkspace
            $psi.UseShellExecute = $false
            $psi.RedirectStandardOutput = $true
            $psi.RedirectStandardError = $true
            $psi.Environment['MCP_WORKSPACE_PATH'] = $staleWorkspace
            $psi.Environment['MCPSERVER_WORKSPACE_PATH'] = $staleWorkspace
            $psi.Environment['CODEX_WORKSPACE_PATH'] = $staleWorkspace
            $psi.Environment['CODEX_PROJECT_DIR'] = $staleWorkspace
            [void]$psi.Environment.Remove('MCP_CACHE_DIR_OVERRIDE')
            [void]$psi.Environment.Remove('PLUGIN_ROOT_OVERRIDE')
            $psi.Environment['MCP_AGENT_NAME'] = 'Codex'
            $psi.Environment['PLUGIN_AGENT_DEFAULT'] = 'Codex'

            $process = [System.Diagnostics.Process]::Start($psi)
            $stdout = $process.StandardOutput.ReadToEndAsync()
            $stderr = $process.StandardError.ReadToEndAsync()
            $process.WaitForExit(30000) | Should -BeTrue

            $process.ExitCode | Should -Be 0
            $child = $stdout.Result.Trim() | ConvertFrom-Json
            $expectedWorkspace = (Resolve-Path -LiteralPath $activeWorkspace).ProviderPath
            $child.workspace | Should -Be $expectedWorkspace
            $child.serverWorkspace | Should -Be $expectedWorkspace
            $child.cacheOverride | Should -BeNullOrEmpty
            $stderr.Result | Should -Not -Match ([regex]::Escape($staleWorkspace))
        } finally {
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

    It 'TEST-MCP-BUGTRIAGE-059 generated Grok PostToolUse hook manifest is valid JSON' {
        $pluginRoot = Join-Path $script:SmokeCache ([guid]::NewGuid().ToString('N'))
        [void][System.IO.Directory]::CreateDirectory($pluginRoot)

        try {
            $result = Invoke-PluginChildProcess `
                -ScriptPath (Join-Path $script:RepoRoot 'plugins\core\hooks-templates\generate-wrappers.ps1') `
                -Arguments @('-HostName', 'grok', '-PluginRoot', $pluginRoot)

            $result.ExitCode | Should -Be 0
            $hooksPath = Join-Path $pluginRoot 'hooks\hooks.json'
            Test-Path -LiteralPath $hooksPath | Should -BeTrue
            $jsonText = [System.IO.File]::ReadAllText($hooksPath)
            $manifest = $jsonText | ConvertFrom-Json -Depth 20 -ErrorAction Stop
            $manifest.hooks.PostToolUse.Count | Should -Be 2
            $codeVerify = $manifest.hooks.PostToolUse[1].hooks[1].command
            $codeVerify | Should -Be 'pwsh -NoLogo -NoProfile -NonInteractive -File "${GROK_PLUGIN_ROOT:-${PLUGIN_ROOT:-$CLAUDE_PLUGIN_ROOT}}/hooks/scripts/code-verify.ps1"'
        } finally {
            Remove-Item -LiteralPath $pluginRoot -Recurse -Force -ErrorAction SilentlyContinue
        }
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


    It 'TEST-MCP-BUGTRIAGE-031 status rejects marker-only verified session-state' {
        $scratchRoot = Join-Path $script:SmokeCache ([guid]::NewGuid().ToString('N'))
        $cacheDir = Join-Path $scratchRoot 'cache'
        [void][System.IO.Directory]::CreateDirectory($cacheDir)
        . (Join-Path $script:LibRoot 'yaml-object-mutation.ps1')
        Import-McpYamlSerializer
        Write-McpYamlObject -Path (Join-Path $cacheDir 'session-state.yaml') -Document ([ordered]@{
            status = 'verified'
            lastUpdated = '2026-07-11T19:14:59Z'
            agent = 'Codex'
            markerFilePath = 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml'
            markerLastWriteUtc = '2026-07-11T17:50:01.7108064Z'
        })

        try {
            $result = Invoke-PluginChildProcess `
                -ScriptPath (Join-Path $script:LibRoot 'mcp-status.ps1') `
                -Environment @{
                    MCP_PLUGIN_ROOT = $script:StagedRoot
                    MCP_PLUGIN_HOST = 'codex'
                    MCP_AGENT_NAME = 'Codex'
                    MCP_CACHE_DIR_OVERRIDE = $cacheDir
                    MCP_WORKSPACE_PATH = $script:RepoRoot
                    MCPSERVER_WORKSPACE_PATH = $script:RepoRoot
                }

            $result.ExitCode | Should -Be 0
            $status = $result.Stdout | ConvertFrom-Json
            $status.status | Should -Be 'no-session'
            $status.hasSession | Should -BeFalse
        } finally {
            Remove-Item -LiteralPath $scratchRoot -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'TEST-MCP-BUGTRIAGE-031 plugin hook rejects verified session-state without sessionId' {
        $source = [System.IO.File]::ReadAllText((Join-Path $script:LibRoot 'plugin-hook.ps1'))

        $source | Should -Match 'function Test-PluginSessionStateValid'
        $source | Should -Match "Contains\('sessionId'\)"
        $source | Should -Match 'Test-PluginSessionStateValid -State \$openSessionState'
        $source | Should -Not -Match 'if \(\(Get-YamlScalar -Path \$sessionFile -Key ''status''\) -ne ''verified''\)'
    }


    It 'TEST-MCP-BUGTRIAGE-051 marker refresh writes sessionId before reporting verified' {
        $scratchRoot = Join-Path $script:SmokeCache ([guid]::NewGuid().ToString('N'))
        $cacheDir = Join-Path $scratchRoot 'cache'
        [void][System.IO.Directory]::CreateDirectory($cacheDir)
        $previousCacheOverride = $env:MCP_CACHE_DIR_OVERRIDE
        $previousWorkspace = $env:MCP_WORKSPACE_PATH
        $previousAgent = $env:MCP_AGENT_NAME
        $previousBootstrap = $null

        try {
            $env:MCP_CACHE_DIR_OVERRIDE = $cacheDir
            $env:MCP_WORKSPACE_PATH = $script:RepoRoot
            $env:MCP_AGENT_NAME = 'Codex'
            . (Join-Path $script:LibRoot 'repl-invoke.ps1')
            $previousBootstrap = Get-Command Invoke-FullBootstrap -CommandType Function -ErrorAction Stop
            function Invoke-FullBootstrap { param([string]$StartDir) return $true }
            $snapshot = Get-MarkerFileSnapshot -StartDir $script:RepoRoot
            Write-McpYamlObject -Path (Join-Path $cacheDir 'session-state.yaml') -Document ([ordered]@{
                status = 'verified'
                agent = 'Codex'
                markerFilePath = $snapshot.markerFilePath
                markerLastWriteUtc = $snapshot.markerLastWriteUtc
            })

            Assert-ReplMarkerFresh | Should -BeTrue
            $state = Read-McpYamlObject -Path (Join-Path $cacheDir 'session-state.yaml')
            $state['sessionId'] | Should -Match '^Codex-\d{8}T\d{6}Z-plugin-session$'
        } finally {
            if ($previousBootstrap) {
                Set-Item -Path Function:\Invoke-FullBootstrap -Value $previousBootstrap.ScriptBlock
            }
            if ($null -ne $previousCacheOverride) {
                $env:MCP_CACHE_DIR_OVERRIDE = $previousCacheOverride
            } else {
                Remove-Item Env:\MCP_CACHE_DIR_OVERRIDE -ErrorAction SilentlyContinue
            }
            if ($null -ne $previousWorkspace) {
                $env:MCP_WORKSPACE_PATH = $previousWorkspace
            } else {
                Remove-Item Env:\MCP_WORKSPACE_PATH -ErrorAction SilentlyContinue
            }
            if ($null -ne $previousAgent) {
                $env:MCP_AGENT_NAME = $previousAgent
            } else {
                Remove-Item Env:\MCP_AGENT_NAME -ErrorAction SilentlyContinue
            }
            Remove-Item -LiteralPath $scratchRoot -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'TEST-MCP-BUGTRIAGE-068 stop-gate blocks completed edited turns with failed last build status' {
        . (Join-Path $script:LibRoot 'yaml-object-mutation.ps1')
        Import-McpYamlSerializer

        $scratchRoot = Join-Path $script:SmokeCache ([guid]::NewGuid().ToString('N'))
        $pluginRoot = Join-Path $scratchRoot 'plugin'
        $libRoot = Join-Path $pluginRoot 'lib'
        $cacheDir = Join-Path $scratchRoot 'cache'
        [void][System.IO.Directory]::CreateDirectory($libRoot)
        [void][System.IO.Directory]::CreateDirectory($cacheDir)
        Copy-Item -Path (Join-Path $script:LibRoot '*') -Destination $libRoot -Recurse -Force
        Write-McpYamlObject -Path (Join-Path $cacheDir 'current-turn.yaml') -Document ([ordered]@{
            turnRequestId = 'req-20260712T000002Z-stopgate-red'
            queryTitle = 'Completed stale build status test'
            status = 'completed'
            queryText = 'Completed stale build status test'
            codeEdits = 4
            lastBuildStatus = 'failed'
            auditActions = 1
            auditFiles = 1
            auditDialog = 0
            auditDecisions = 0
        })

        $environment = @{
            MCP_PLUGIN_ROOT = $pluginRoot
            MCP_PLUGIN_HOST = 'codex'
            MCP_AGENT_NAME = 'Codex'
            PLUGIN_ROOT_OVERRIDE = $scratchRoot
            MCP_CACHE_DIR_OVERRIDE = $cacheDir
            MCP_WORKSPACE_PATH = $script:RepoRoot
            MCPSERVER_WORKSPACE_PATH = $script:RepoRoot
        }

        try {
            $result = Invoke-PluginChildProcess `
                -ScriptPath (Join-Path $pluginRoot 'lib\plugin-hook.ps1') `
                -Arguments @('-HookName', 'stop-gate', '-HostName', 'codex') `
                -Environment $environment
            $result.ExitCode | Should -Be 0
            $json = $result.Stdout | ConvertFrom-Json
            $json.decision | Should -Be 'block'
            $json.reason | Should -Match 'Last build in this turn failed after 4 code edit'
        } finally {
            Remove-Item -LiteralPath $scratchRoot -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'TEST-MCP-BUGTRIAGE-033 code-verify accepts explicit and pipeline hook payloads' {
        $scratchRoot = Join-Path $script:SmokeCache ([guid]::NewGuid().ToString('N'))
        $pluginRoot = Join-Path $scratchRoot 'plugin'
        $libRoot = Join-Path $pluginRoot 'lib'
        [void][System.IO.Directory]::CreateDirectory($scratchRoot)
        [void][System.IO.Directory]::CreateDirectory($libRoot)
        Copy-Item -Path (Join-Path $script:LibRoot '*') -Destination $libRoot -Recurse -Force
        & (Join-Path $script:RepoRoot 'plugins\core\hooks-templates\generate-wrappers.ps1') `
            -HostName 'claude-code' `
            -PluginRoot $pluginRoot | Out-Null

        $scratchFile = Join-Path $scratchRoot 'edited.txt'
        [System.IO.File]::WriteAllText($scratchFile, 'content')
        $paramsFile = Join-Path $scratchRoot 'hook-payload.json'
        $payload = [ordered]@{
            tool_name = 'Edit'
            tool_input = [ordered]@{ file_path = $scratchFile }
        } | ConvertTo-Json -Depth 10 -Compress
        [System.IO.File]::WriteAllText($paramsFile, $payload)

        $environment = @{
            MCP_PLUGIN_ROOT = $pluginRoot
            MCP_PLUGIN_HOST = 'claude-code'
            MCP_AGENT_NAME = 'ClaudeCode'
            PLUGIN_ROOT_OVERRIDE = $scratchRoot
            MCP_CACHE_DIR_OVERRIDE = (Join-Path $scratchRoot 'cache')
            MCP_WORKSPACE_PATH = $script:RepoRoot
            MCPSERVER_WORKSPACE_PATH = $script:RepoRoot
        }

        try {
            $paramsResult = Invoke-PluginChildProcess `
                -ScriptPath (Join-Path $pluginRoot 'lib\plugin-hook.ps1') `
                -Arguments @('-HookName', 'code-verify', '-HostName', 'claude-code', '-Params', $payload) `
                -Environment $environment
            $paramsResult.ExitCode | Should -Be 0
            ($paramsResult.Stdout | ConvertFrom-Json).status | Should -Be 'succeeded'

            $paramsPathResult = Invoke-PluginChildProcess `
                -ScriptPath (Join-Path $pluginRoot 'lib\plugin-hook.ps1') `
                -Arguments @('-HookName', 'code-verify', '-HostName', 'claude-code', '-ParamsPath', $paramsFile) `
                -Environment $environment
            $paramsPathResult.ExitCode | Should -Be 0
            ($paramsPathResult.Stdout | ConvertFrom-Json).status | Should -Be 'succeeded'

            $pipelineResult = Invoke-PluginChildProcess `
                -ScriptPath (Join-Path $pluginRoot 'hooks\scripts\code-verify.ps1') `
                -Environment $environment `
                -InputText $payload
            $pipelineResult.ExitCode | Should -Be 0
            ($pipelineResult.Stdout | ConvertFrom-Json).status | Should -Be 'succeeded'
            $pipelineResult.Stderr | Should -Not -Match 'input object cannot be bound'
        } finally {
            Remove-Item -LiteralPath $scratchRoot -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
    It 'TEST-MCP-BUGTRIAGE-030 user-prompt-submit uses explicit prompt payload without redirected stdin' {
        . (Join-Path $script:LibRoot 'yaml-object-mutation.ps1')
        Import-McpYamlSerializer

        $scratchRoot = Join-Path $script:SmokeCache ([guid]::NewGuid().ToString('N'))
        $pluginRoot = Join-Path $scratchRoot 'plugin'
        $libRoot = Join-Path $pluginRoot 'lib'
        $workspace = Join-Path $scratchRoot 'workspace'
        $cacheDir = Join-Path $scratchRoot 'cache'
        [void][System.IO.Directory]::CreateDirectory($libRoot)
        [void][System.IO.Directory]::CreateDirectory($workspace)
        [void][System.IO.Directory]::CreateDirectory($cacheDir)
        Copy-Item -Path (Join-Path $script:LibRoot '*') -Destination $libRoot -Recurse -Force

        $replStub = @'
#Requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Method,
    [string]$ParamsYaml = ''
)

$ErrorActionPreference = 'Stop'
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
. (Join-Path $scriptDir 'yaml-object-mutation.ps1')
Import-McpYamlSerializer

if ($Method -ne 'workflow.sessionlog.beginTurn') {
    throw "Unexpected method $Method"
}

$params = $ParamsYaml | ConvertFrom-Yaml -Ordered -ErrorAction Stop
$cacheDir = $env:MCP_CACHE_DIR_OVERRIDE
if (-not $cacheDir) {
    throw 'MCP_CACHE_DIR_OVERRIDE is required.'
}

Write-McpYamlObject -Path (Join-Path $cacheDir 'current-turn.yaml') -Document ([ordered]@{
    turnRequestId = [string]$params['requestId']
    queryTitle = [string]$params['queryTitle']
    queryText = [string]$params['queryText']
    status = 'active'
})

'{}'
'@
        [System.IO.File]::WriteAllText((Join-Path $libRoot 'repl-invoke.ps1'), $replStub, [System.Text.UTF8Encoding]::new($false))

        $markerPath = Join-Path $workspace 'AGENTS-README-FIRST.yaml'
        Write-McpYamlObject -Path $markerPath -Document ([ordered]@{
            workspace = 'PluginPromptPayloadTest'
            workspacePath = $workspace
            baseUrl = 'http://127.0.0.1:1'
            apiKey = 'test-key'
            port = 1
        })
        $markerItem = Get-Item -LiteralPath $markerPath
        Write-McpYamlObject -Path (Join-Path $cacheDir 'session-state.yaml') -Document ([ordered]@{
            status = 'verified'
            sessionId = 'session-bugtriage-030'
            workspacePath = $workspace
            agent = 'Codex'
            lastUpdated = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
            markerFilePath = $markerItem.FullName
            markerLastWriteUtc = $markerItem.LastWriteTimeUtc.ToString('O')
        })

        $prompt = "Real nonredirected prompt`nSecond line"
        $payload = [ordered]@{ prompt = $prompt } | ConvertTo-Json -Depth 10 -Compress
        $environment = @{
            MCP_PLUGIN_ROOT = $pluginRoot
            MCP_PLUGIN_HOST = 'codex'
            MCP_AGENT_NAME = 'Codex'
            MCP_CACHE_DIR_OVERRIDE = $cacheDir
            MCP_WORKSPACE_PATH = $workspace
            MCPSERVER_WORKSPACE_PATH = $workspace
            MCP_WORKSPACE_START_DIR = $workspace
        }

        try {
            $result = Invoke-PluginChildProcess `
                -ScriptPath (Join-Path $pluginRoot 'lib\plugin-hook.ps1') `
                -Arguments @('-HookName', 'user-prompt-submit', '-HostName', 'codex', '-WorkspacePath', $workspace, '-Params', $payload) `
                -Environment $environment `
                -RedirectStandardInput:$false

            $result.ExitCode | Should -Be 0
            $output = $result.Stdout | ConvertFrom-Json
            $output.hookSpecificOutput.status | Should -Be 'turn-opened'

            $turn = Read-McpYamlObject -Path (Join-Path $cacheDir 'current-turn.yaml')
            [string]$turn['queryTitle'] | Should -Be 'Real nonredirected prompt'
            [string]$turn['queryText'] | Should -Be $prompt
            [string]$turn['queryTitle'] | Should -Not -Be 'User prompt'
            [string]$turn['queryText'] | Should -Not -Be 'Continuation or hook-triggered turn.'
        } finally {
            Remove-Item -LiteralPath $scratchRoot -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'TEST-MCP-BUGTRIAGE-033 generated wrappers forward explicit hook payload parameters' {
        $template = [System.IO.File]::ReadAllText((Join-Path $script:RepoRoot 'plugins\core\hooks-templates\wrapper.ps1.template'))

        $template | Should -Match '\[string\]\$Params'
        $template | Should -Match '\[string\]\$ParamsPath'
        $template | Should -Match 'ValueFromPipeline'
        $template | Should -Match 'hookArguments\.Params'
        $template | Should -Match 'hookArguments\.ParamsPath'
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

    It 'TEST-MCP-TRANSCRIPT-010 does not expose transcript ingestion endpoints through plugins' {
        $helperPath = Join-Path $script:LibRoot 'transcript-ingestion.ps1'
        Test-Path -LiteralPath $helperPath | Should -BeFalse

        $skillPath = Join-Path $script:StagedRoot 'skills\transcript-ingestion\SKILL.md'
        Test-Path -LiteralPath $skillPath | Should -BeFalse

        $pluginFiles = Get-ChildItem -LiteralPath $script:StagedRoot -Recurse -File -ErrorAction SilentlyContinue |
            Where-Object { $_.FullName -notmatch '\\.git(\\|$)' }
        foreach ($file in $pluginFiles) {
            $content = [System.IO.File]::ReadAllText($file.FullName)
            $content | Should -Not -Match 'repl\.sessionlog\.ingestTranscripts'
            $content | Should -Not -Match 'repl\.sessionlog\.normalizeTranscripts'
            $content | Should -Not -Match 'transcript-ingestion\.ps1'
            $content | Should -Not -Match 'automatically captures rich session fields from .* JSONL transcripts'
            $content | Should -Not -Match 'plugin can auto-capture rich session fields'
            $content | Should -Not -Match 'Where your agent''s hooks or transcript integration are available'
            $content | Should -Not -Match 'JSONL-extracted text'
            $content | Should -Not -Match 'Subagent Transcript Import'
            $content | Should -Not -Match 'PowerShell import command exposed by the active plugin package'
        }

        $sessionSkillPath = Join-Path $script:StagedRoot 'skills\session\SKILL.md'
        $sessionSkillContent = [System.IO.File]::ReadAllText($sessionSkillPath)
        $sessionSkillContent | Should -Match 'The active model must write its own session log'
        $sessionSkillContent | Should -Not -Match 'importRecovery'
    }


    It 'TEST-MCP-TRANSCRIPT-010 marker prompt requires model-authored session logging' {
        $guidancePath = Join-Path $script:RepoRoot 'templates\prompt-templates.yaml'
        Test-Path -LiteralPath $guidancePath | Should -BeTrue

        $content = [System.IO.File]::ReadAllText($guidancePath)

        $content | Should -Match 'Model-Authored Session Logging'
        $content | Should -Match 'Models must write MCP Session Log turns'
        $content | Should -Match 'Plugins must not ingest local chat transcripts'
        $content | Should -Match 'Transcript ingestion and normalization are explicit non-plugin server/client/REPL/MCP operations'
    }

    It 'TEST-MCP-TRANSCRIPT-010 removes legacy Codex JSONL parser helpers' {
        Test-Path -LiteralPath (Join-Path $script:RepoRoot 'plugins\core\lib-sh\codex-jsonl.js') | Should -BeFalse
        Test-Path -LiteralPath (Join-Path $script:RepoRoot 'plugins\core\lib-sh\codex-jsonl-enrich.js') | Should -BeFalse

        $hookPath = Join-Path $script:RepoRoot 'plugins\core\lib-sh\hook-lib.sh'
        $hookContent = [System.IO.File]::ReadAllText($hookPath)
        $hookContent | Should -Not -Match 'codexJsonlPath'
        $hookContent | Should -Not -Match 'codex-jsonl'

        $matrixPath = Join-Path $script:RepoRoot 'docs\AGENT-PLUGIN-FEATURE-MATRIX.md'
        $matrixContent = [System.IO.File]::ReadAllText($matrixPath)
        $matrixContent | Should -Not -Match 'codex-jsonl'
        $matrixContent | Should -Not -Match 'transcript enrichment'
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

    It 'TEST-MCP-BUGTRIAGE-066 plugin hook scalar YAML updates use lock-safe object mutation' {
        $hookContent = [System.IO.File]::ReadAllText((Join-Path $script:LibRoot 'plugin-hook.ps1'))
        $yamlContent = [System.IO.File]::ReadAllText((Join-Path $script:LibRoot 'yaml-object-mutation.ps1'))

        $setBody = [regex]::Match($hookContent, 'function Set-YamlScalar \{.*?\n\}', [System.Text.RegularExpressions.RegexOptions]::Singleline).Value
        $getBody = [regex]::Match($hookContent, 'function Get-YamlScalar \{.*?\n\}', [System.Text.RegularExpressions.RegexOptions]::Singleline).Value

        $setBody | Should -Match 'Set-McpYamlObjectValue'
        $setBody | Should -Not -Match 'WriteAllText|ReadAllText|\[regex\]::Replace'
        $getBody | Should -Match 'Read-McpYamlObject'
        $yamlContent | Should -Match 'function Invoke-McpYamlFileOperation'
        $yamlContent | Should -Match 'catch \[System\.IO\.IOException\]'
        $yamlContent | Should -Match 'catch \[System\.UnauthorizedAccessException\]'
        $yamlContent | Should -Match 'Start-Sleep -Milliseconds \$delay'
        $yamlContent | Should -Match '\[System\.IO\.File\]::Move\(\$tempPath, \$resolvedPath, \$true\)'
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


    It 'TEST-MCP-BUGTRIAGE-050 triage skill documents schema-valid REPL status methods' {
        $skillPath = Join-Path $script:StagedRoot 'skills\triage\SKILL.md'
        $content = [System.IO.File]::ReadAllText($skillPath)

        $content | Should -Match 'Native MCP clients may use `triage_status`'
        $content | Should -Match 'workflow\.triage\.getReport'
        $content | Should -Match 'workflow\.triage\.getGroup'
        $content | Should -Match 'workflow\.triage\.queryGroups'
        $content | Should -Not -Match '- Use `triage_status` to inspect a report or group later\.'
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

    It 'TEST-MCP-BUGTRIAGE-027 core integrity checker exits 0 on success' {
        $result = Invoke-PluginChildProcess `
            -ScriptPath (Join-Path $script:RepoRoot 'plugins\core\sync\check-core-integrity.ps1') `
            -Arguments @('-PluginRoot', $script:StagedRoot)

        $result.ExitCode | Should -Be 0
        $result.Stdout | Should -Match 'core integrity OK: \d+ files match'
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
            "param([string]`$Method, [string]`$ParamsYaml)`nAdd-Content -LiteralPath '$($workRoot.Replace("'", "''"))\replay.log' -Value `"`$Method|`$ParamsYaml`"`n"
        )

        $env:MCP_CACHE_DIR_OVERRIDE = $cacheRoot
        try {
            & (Join-Path $libCopy 'cache-manager.ps1') -Action write -Method 'workflow.todo.create' -ParamsYaml "title: First" | Out-Null
            & (Join-Path $libCopy 'cache-manager.ps1') -Action write -Method 'workflow.todo.update' -ParamsYaml "id: TODO-1" | Out-Null
            $result = & (Join-Path $libCopy 'cache-manager.ps1') -Action flush

            $result | Should -Be 'flushed=2 failed=0 pending=0'
            [System.IO.File]::ReadAllLines((Join-Path $workRoot 'replay.log')) | Should -Be @(
                'workflow.todo.create|title: First',
                'workflow.todo.update|id: TODO-1'
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
            'MCP_AGENT_NAME',
            'PLUGIN_AGENT_NAME',
            'PLUGIN_AGENT_DEFAULT',
            'MCP_PLUGIN_HOST',
            'CLAUDE_PROJECT_DIR',
            'CODEX_CWD',
            'CODEX_WORKSPACE_PATH',
            'CODEX_PROJECT_DIR',
            'COWORK_WORKSPACE_PATH',
            'CLINE_WORKSPACE_PATH',
            'OPENCODE_WORKSPACE_PATH'
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


    It 'persists multi-item appendDialog payloads and increments auditDialog' {
        $pluginRoot = Join-Path $script:SmokeCache ([guid]::NewGuid().ToString('N'))
        $cacheDir = Join-Path $pluginRoot 'cache'
        [void][System.IO.Directory]::CreateDirectory($cacheDir)
        $previousCacheOverride = $env:MCP_CACHE_DIR_OVERRIDE
        $previousFresh = $null
        $previousPersist = $null
        $script:capturedProcessingDialog = $null

        try {
            $env:MCP_CACHE_DIR_OVERRIDE = $cacheDir
            . (Join-Path $script:LibRoot 'repl-invoke.ps1')
            . (Join-Path $script:LibRoot 'repl-invoke.ps1')
            $previousFresh = Get-Command Assert-ReplCurrentTurnFresh -CommandType Function -ErrorAction Stop
            $previousPersist = Get-Command Invoke-ReplPersistTurn -CommandType Function -ErrorAction Stop
            function Assert-ReplCurrentTurnFresh { return $true }
            function Invoke-ReplPersistTurn {
                param(
                    [string]$RequestId,
                    [string]$Title,
                    [string]$Status,
                    [string]$ResponseText,
                    [object[]]$ProcessingDialog
                )
                $script:capturedProcessingDialog = $ProcessingDialog
                return $true
            }
            Write-McpYamlObject -Path (Join-Path $cacheDir 'current-turn.yaml') -Document ([ordered]@{
                turnRequestId = 'req-20260709T211900Z-dialog-green'
                queryTitle = 'Dialog parser green test'
                status = 'in_progress'
                auditDialog = 0
            })
            $payload = [ordered]@{
                dialogItems = @(
                    [ordered]@{ role = 'assistant'; content = 'first diagnostic'; category = 'analysis' },
                    [ordered]@{ role = 'assistant'; content = 'selected the independent REPL failsafe strategy'; category = 'decision' }
                )
            } | ConvertTo-Yaml -Options WithIndentedSequences

            Invoke-WorkflowAppendDialog -ParamsYaml $payload | Should -BeTrue
            $turn = Read-McpYamlObject -Path (Join-Path $cacheDir 'current-turn.yaml')
            $turn['auditDialog'] | Should -Be 2
            @($script:capturedProcessingDialog).Count | Should -Be 2
            @($script:capturedProcessingDialog)[1]['category'] | Should -Be 'decision'
        } finally {
            if ($previousPersist) { Set-Item -Path Function:\Invoke-ReplPersistTurn -Value $previousPersist.ScriptBlock }
            if ($previousFresh) { Set-Item -Path Function:\Assert-ReplCurrentTurnFresh -Value $previousFresh.ScriptBlock }
            if ($null -ne $previousCacheOverride) {
                $env:MCP_CACHE_DIR_OVERRIDE = $previousCacheOverride
            } else {
                Remove-Item Env:\MCP_CACHE_DIR_OVERRIDE -ErrorAction SilentlyContinue
            }
            Remove-Variable -Name capturedProcessingDialog -Scope Script -ErrorAction SilentlyContinue
            Remove-Item -LiteralPath $pluginRoot -Recurse -Force -ErrorAction SilentlyContinue
        }
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

    It 'TEST-MCP-BUGTRIAGE-041 repl-invoke reloads marker resolver in its own script scope' {
        $source = [System.IO.File]::ReadAllText((Join-Path $script:LibRoot 'repl-invoke.ps1'))

        $source | Should -Match '\. \(Join-Path \$PSScriptRoot ''marker-resolver\.ps1''\)'
        $source | Should -Not -Match 'if \(-not \(Get-Command Find-MarkerFile'
    }

    It 'TEST-MCP-BUGTRIAGE-041 plugin hook checks REPL exit code instead of assignment success' {
        $source = [System.IO.File]::ReadAllText((Join-Path $script:LibRoot 'plugin-hook.ps1'))

        $source | Should -Match '\$script:LastPluginReplExitCode = 0'
        $source | Should -Match 'Get-Variable -Name LASTEXITCODE -Scope Global -ErrorAction SilentlyContinue'
        $source | Should -Match '\$script:LastPluginReplExitCode = if \(\$null -ne \$exitCodeVariable'
        $source | Should -Match 'if \(\$script:LastPluginReplExitCode -ne 0\)'
        $source | Should -Not -Match 'if \(-not \$\?\) \{'
    }

}
