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
            [bool]$RedirectStandardInput = $true,
            [string]$WorkingDirectory = $script:RepoRoot,
            [int]$TimeoutMs = 30000
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
        $psi.WorkingDirectory = $WorkingDirectory
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
        $process.WaitForExit($TimeoutMs) | Should -BeTrue

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

    It 'TEST-MCP-PLUGIN-PSONLY-001 resolves default agent runtime header fields' {
        . (Join-Path $script:LibRoot 'agent-runtime-header.ps1')
        $cacheDir = Join-Path $script:SmokeCache ([guid]::NewGuid().ToString('N'))
        [void][System.IO.Directory]::CreateDirectory($cacheDir)
        $pwshPath = (Get-Command pwsh -ErrorAction Stop).Source
        $previousPluginVersion = $env:MCP_PLUGIN_VERSION

        try {
            $env:MCP_PLUGIN_VERSION = '1.82.0'
            $headers = Resolve-McpPluginAgentHeaderFields -SessionId 'Codex-20260722T000000Z-plugin-session' -CacheDir $cacheDir -AgentName 'Codex' -HostName 'codex' -ExecutableCandidates @($pwshPath)

            # TR-MCP-PLUGIN-HEADER-001: agentSessionId is the PROVIDER-NATIVE id. With
            # no provider id available it stays empty rather than echoing the MCP id.
            $headers.agentSessionId | Should -BeNullOrEmpty
            # TR-MCP-PLUGIN-HEADER-001: never emit a transcript path for a file that
            # does not exist. The cache session.jsonl was never created here.
            $headers.agentSessionTranscriptFile | Should -BeNullOrEmpty
            $headers.agentExecutablePath | Should -Be $pwshPath
            [string]$headers.agentExecutableVersion | Should -Not -BeNullOrEmpty
        } finally {
            if ($null -ne $previousPluginVersion) { $env:MCP_PLUGIN_VERSION = $previousPluginVersion } else { Remove-Item Env:\MCP_PLUGIN_VERSION -ErrorAction SilentlyContinue }
            Remove-Item -LiteralPath $cacheDir -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'TEST-MCP-PLUGIN-HEADER-005 never re-submits a stale fabricated transcript path or echoed session id from cache' {
        $pluginRoot = Join-Path $script:SmokeCache ([guid]::NewGuid().ToString('N'))
        $cacheDir = Join-Path $pluginRoot 'cache'
        [void][System.IO.Directory]::CreateDirectory($cacheDir)
        $previousCacheOverride = $env:MCP_CACHE_DIR_OVERRIDE
        $sessionId = 'Codex-20260722T000000Z-plugin-session'
        $fabricated = Join-Path $cacheDir 'session.jsonl'   # deliberately NOT created
        $saved = @{}
        foreach ($n in @('MCP_AGENT_SESSION_ID','MCP_AGENT_SESSION_TRANSCRIPT_FILE')) {
            $saved[$n] = [Environment]::GetEnvironmentVariable($n)
            Remove-Item "Env:\$n" -ErrorAction SilentlyContinue
        }
        $script:hdr005Yaml = ''
        try {
            $env:MCP_CACHE_DIR_OVERRIDE = $cacheDir
            . (Join-Path $script:LibRoot 'repl-invoke.ps1')
            $previousRaw = Get-Command Invoke-ReplRaw -CommandType Function -ErrorAction SilentlyContinue
            function Invoke-ReplRaw {
                param([Parameter(Mandatory)][string]$Method, [string]$ParamsYaml = '')
                $script:hdr005Yaml = $ParamsYaml
                return [pscustomobject]@{ Success = $true; Output = "type: result`npayload:`n  result:`n    persisted: true"; Error = '' }
            }

            # Stale cache written by the pre-fix plugin: a transcript path for a file
            # that does not exist, and agentSessionId echoing the MCP session id.
            Write-McpYamlObject -Path (Join-Path $cacheDir 'session-state.yaml') -Document ([ordered]@{
                status = 'verified'
                sessionId = $sessionId
                agent = 'Codex'
                agentSessionId = $sessionId
                agentSessionTranscriptFile = $fabricated
            })

            $null = Invoke-ReplPersistTurn -RequestId 'req-20260722T000001Z-hdr005' -Title 'hdr005' -Status 'in_progress' -ResponseText 'x'

            $script:hdr005Yaml | Should -Not -BeNullOrEmpty
            # TR-MCP-PLUGIN-HEADER-001: the fabricated path must never reach the server.
            $script:hdr005Yaml | Should -Not -Match ([regex]::Escape('session.jsonl'))
            # agentSessionId must not echo the MCP session id.
            $script:hdr005Yaml | Should -Not -Match ([regex]::Escape("agentSessionId: $sessionId"))
        } finally {
            if ($previousRaw) { Set-Item -Path Function:\Invoke-ReplRaw -Value $previousRaw.ScriptBlock } else { Remove-Item Function:\Invoke-ReplRaw -ErrorAction SilentlyContinue }
            foreach ($n in $saved.Keys) {
                if ($null -ne $saved[$n]) { Set-Item -Path "Env:\$n" -Value $saved[$n] } else { Remove-Item "Env:\$n" -ErrorAction SilentlyContinue }
            }
            if ($null -ne $previousCacheOverride) { $env:MCP_CACHE_DIR_OVERRIDE = $previousCacheOverride } else { Remove-Item Env:\MCP_CACHE_DIR_OVERRIDE -ErrorAction SilentlyContinue }
            Remove-Variable -Name hdr005Yaml -Scope Script -ErrorAction SilentlyContinue
            Remove-Item -LiteralPath $pluginRoot -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'TEST-MCP-PLUGIN-HEADER-002 emits the cache transcript path only when that file exists' {
        . (Join-Path $script:LibRoot 'agent-runtime-header.ps1')
        $cacheDir = Join-Path $script:SmokeCache ([guid]::NewGuid().ToString('N'))
        [void][System.IO.Directory]::CreateDirectory($cacheDir)
        $pwshPath = (Get-Command pwsh -ErrorAction Stop).Source
        $transcript = Join-Path $cacheDir 'session.jsonl'
        try {
            [System.IO.File]::WriteAllText($transcript, '{"t":1}')
            $headers = Resolve-McpPluginAgentHeaderFields -SessionId 'Codex-20260722T000000Z-plugin-session' -CacheDir $cacheDir -AgentName 'Codex' -HostName 'codex' -ExecutableCandidates @($pwshPath)
            $headers.agentSessionTranscriptFile | Should -Be $transcript
        } finally {
            Remove-Item -LiteralPath $cacheDir -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'TEST-MCP-PLUGIN-HEADER-003 prefers the verified provider session id and transcript from the host payload' {
        . (Join-Path $script:LibRoot 'agent-runtime-header.ps1')
        $cacheDir = Join-Path $script:SmokeCache ([guid]::NewGuid().ToString('N'))
        [void][System.IO.Directory]::CreateDirectory($cacheDir)
        $pwshPath = (Get-Command pwsh -ErrorAction Stop).Source
        $providerTranscript = Join-Path $cacheDir 'provider-real.jsonl'
        try {
            [System.IO.File]::WriteAllText($providerTranscript, '{"t":1}')
            $headers = Resolve-McpPluginAgentHeaderFields -SessionId 'ClaudeCode-20260723T000000Z-plugin-session' -CacheDir $cacheDir -AgentName 'ClaudeCode' -HostName 'claude' -ExecutableCandidates @($pwshPath) -ProviderSessionId '45f1b597-40a2-4f1c-983c-1be5b16ab5b9' -TranscriptPath $providerTranscript
            $headers.agentSessionId | Should -Be '45f1b597-40a2-4f1c-983c-1be5b16ab5b9'
            $headers.agentSessionTranscriptFile | Should -Be $providerTranscript
        } finally {
            Remove-Item -LiteralPath $cacheDir -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'TEST-MCP-PLUGIN-HEADER-004 never reports the plugin version as the agent executable version' {
        . (Join-Path $script:LibRoot 'agent-runtime-header.ps1')
        $cacheDir = Join-Path $script:SmokeCache ([guid]::NewGuid().ToString('N'))
        [void][System.IO.Directory]::CreateDirectory($cacheDir)
        $saved = @{}
        foreach ($n in @('MCP_PLUGIN_VERSION','MCP_AGENT_EXECUTABLE_PATH','MCP_AGENT_EXECUTABLE_VERSION','CODEX_EXECUTABLE_VERSION','CLAUDE_EXECUTABLE_VERSION','GROK_EXECUTABLE_VERSION','COPILOT_EXECUTABLE_VERSION','CLINE_EXECUTABLE_VERSION','OPENCODE_EXECUTABLE_VERSION','CODEX_EXECUTABLE_PATH','CLAUDE_EXECUTABLE_PATH','GROK_EXECUTABLE_PATH','COPILOT_EXECUTABLE_PATH','CLINE_EXECUTABLE_PATH','OPENCODE_EXECUTABLE_PATH')) {
            $saved[$n] = [Environment]::GetEnvironmentVariable($n)
            Remove-Item "Env:\$n" -ErrorAction SilentlyContinue
        }
        try {
            $env:MCP_PLUGIN_VERSION = '1.82.0'
            $headers = Resolve-McpPluginAgentHeaderFields -SessionId 'NoSuch-20260723T000000Z-plugin-session' -CacheDir $cacheDir -AgentName 'NoSuchAgent' -HostName 'nosuchagent'
            $headers.agentExecutableVersion | Should -Not -Be '1.82.0'
            $headers.agentExecutableVersion | Should -Be 'unknown'
        } finally {
            foreach ($n in $saved.Keys) {
                if ($null -ne $saved[$n]) { Set-Item -Path "Env:\$n" -Value $saved[$n] } else { Remove-Item "Env:\$n" -ErrorAction SilentlyContinue }
            }
            Remove-Item -LiteralPath $cacheDir -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'TEST-MCP-PLUGIN-PSONLY-001 preserves agent runtime header fields in session submit bodies' {
        $builder = Join-Path $script:RepoRoot 'plugins\core\lib-sh\sessionlog-submit-body.js'
        $node = (Get-Command node -ErrorAction Stop).Source
        $psi = [System.Diagnostics.ProcessStartInfo]::new()
        $psi.FileName = $node
        $psi.ArgumentList.Add($builder)
        $psi.ArgumentList.Add('build')
        $psi.UseShellExecute = $false
        $psi.RedirectStandardOutput = $true
        $psi.RedirectStandardError = $true
        $psi.Environment['SESSION_SOURCE_TYPE'] = 'Codex'
        $psi.Environment['SESSION_ID'] = 'Codex-20260722T000000Z-runtime-header'
        $psi.Environment['SESSION_AGENT_SESSION_ID'] = 'codex-root-session-001'
        $psi.Environment['SESSION_AGENT_SESSION_TRANSCRIPT_FILE'] = 'F:\GitHub\McpServer\.mcpServer\codex\session.jsonl'
        $psi.Environment['SESSION_AGENT_EXECUTABLE_PATH'] = 'C:\Users\kingd\AppData\Roaming\npm\codex.cmd'
        $psi.Environment['SESSION_AGENT_EXECUTABLE_VERSION'] = '1.81.0'

        $process = [System.Diagnostics.Process]::Start($psi)
        $stdout = $process.StandardOutput.ReadToEndAsync()
        $stderr = $process.StandardError.ReadToEndAsync()
        $process.WaitForExit(30000) | Should -BeTrue
        $process.ExitCode | Should -Be 0 -Because $stderr.Result

        $session = $stdout.Result | ConvertFrom-Json
        $session.agentSessionId | Should -Be 'codex-root-session-001'
        $session.agentSessionTranscriptFile | Should -Be 'F:\GitHub\McpServer\.mcpServer\codex\session.jsonl'
        $session.agentExecutablePath | Should -Be 'C:\Users\kingd\AppData\Roaming\npm\codex.cmd'
        $session.agentExecutableVersion | Should -Be '1.81.0'

        $existingPath = Join-Path ([System.IO.Path]::GetTempPath()) "mcp-existing-$([guid]::NewGuid().ToString('N')).json"
        $incomingPath = Join-Path ([System.IO.Path]::GetTempPath()) "mcp-incoming-$([guid]::NewGuid().ToString('N')).json"
        try {
            [System.IO.File]::WriteAllText($existingPath, (@{
                items = @(
                    [ordered]@{
                        sourceType = 'Codex'
                        sessionId = 'Codex-20260722T000000Z-runtime-header'
                        agentSessionId = 'codex-root-session-001'
                        agentSessionTranscriptFile = 'F:\GitHub\McpServer\.mcpServer\codex\session.jsonl'
                        agentExecutablePath = 'C:\Users\kingd\AppData\Roaming\npm\codex.cmd'
                        agentExecutableVersion = '1.81.0'
                        turns = @()
                    }
                )
            } | ConvertTo-Json -Depth 10 -Compress))
            [System.IO.File]::WriteAllText($incomingPath, (@{
                sourceType = 'Codex'
                sessionId = 'Codex-20260722T000000Z-runtime-header'
                turns = @()
            } | ConvertTo-Json -Depth 10 -Compress))

            $merge = & $node $builder merge $existingPath $incomingPath | ConvertFrom-Json
            $merge.agentSessionId | Should -Be 'codex-root-session-001'
            $merge.agentSessionTranscriptFile | Should -Be 'F:\GitHub\McpServer\.mcpServer\codex\session.jsonl'
            $merge.agentExecutablePath | Should -Be 'C:\Users\kingd\AppData\Roaming\npm\codex.cmd'
            $merge.agentExecutableVersion | Should -Be '1.81.0'
        } finally {
            Remove-Item -LiteralPath $existingPath, $incomingPath -Force -ErrorAction SilentlyContinue
        }
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

        $oldLocation = (Get-Location).ProviderPath
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
            Set-Location -LiteralPath $workspaceRoot

            . (Join-Path $script:LibRoot 'resolve-cache-dir.ps1')
            Resolve-McpCacheDir | Should -Be (Join-Path $workspaceRoot '.mcpServer\codex')
        } finally {
            Set-Location -LiteralPath $oldLocation
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
                    [AllowEmptyString()][string]$Title = '',
                    [switch]$IncludeSessionTitle,
                    [Parameter(Mandatory)][string]$Status,
                    [string]$ResponseText = '',
                    [string]$ActionsYaml = '',
                    [object[]]$ProcessingDialog = @(),
                    [string]$Interpretation = '',
                    [int]$TokenCount = 0,
                    [string[]]$Tags = @(),
                    [string[]]$ContextList = @(),
                    [string]$PlanFile = '',
                    [string]$TodoId = ''
                )
                $script:beginTurnPersistArgs = [ordered]@{
                    RequestId = $RequestId
                    Title = $Title
                    Status = $Status
                    ResponseText = $ResponseText
                    PlanFile = $PlanFile
                    TodoId = $TodoId
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
                    [AllowEmptyString()][string]$Title = '',
                    [switch]$IncludeSessionTitle,
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

    It 'TEST-MCP-REPL-030 omits the turn title on an incidental appendActions but sends an explicit one' {
        $pluginRoot = Join-Path $script:SmokeCache ([guid]::NewGuid().ToString('N'))
        $cacheDir = Join-Path $pluginRoot 'cache'
        [void][System.IO.Directory]::CreateDirectory($cacheDir)
        $previousCacheOverride = $env:MCP_CACHE_DIR_OVERRIDE
        $script:reql030Args = $null

        try {
            $env:MCP_CACHE_DIR_OVERRIDE = $cacheDir
            . (Join-Path $script:LibRoot 'repl-invoke.ps1')
            $previousFresh = Get-Command Assert-ReplCurrentTurnFresh -CommandType Function -ErrorAction Stop
            $previousPersist = Get-Command Invoke-ReplPersistTurn -CommandType Function -ErrorAction Stop
            function Assert-ReplCurrentTurnFresh { return $true }
            function Invoke-ReplPersistTurn {
                param(
                    [Parameter(Mandatory)][string]$RequestId,
                    [AllowEmptyString()][string]$Title = '',
                    [switch]$IncludeSessionTitle,
                    [Parameter(Mandatory)][string]$Status,
                    [string]$ResponseText = '',
                    [string]$ActionsYaml = '',
                    [object[]]$ProcessingDialog = @(),
                    [string]$Interpretation = '',
                    [int]$TokenCount = 0,
                    [string[]]$Tags = @(),
                    [string[]]$ContextList = @()
                )
                $script:reql030Args = [ordered]@{ Title = $Title; IncludeSessionTitle = [bool]$IncludeSessionTitle }
                return $true
            }

            Write-McpYamlObject -Path (Join-Path $cacheDir 'session-state.yaml') -Document ([ordered]@{
                status = 'verified'
                sessionId = 'Codex-20260712T000000Z-plugin-session'
                agent = 'Codex'
                title = 'Stable session title'
            })
            Write-McpYamlObject -Path (Join-Path $cacheDir 'current-turn.yaml') -Document ([ordered]@{
                turnRequestId = 'req-20260712T000002Z-omit'
                queryTitle = 'Provisional turn title'
                status = 'in_progress'
                queryText = 'Prompt'
            })

            # TR-MCP-REPL-015: incidental append (no queryTitle) must omit the title.
            Invoke-ReplMethod -Method 'workflow.sessionlog.appendActions' -ParamsYaml "actions:`n- type: note`n  description: work"
            $script:LastInvokeReplMethodSuccess | Should -BeTrue
            $script:reql030Args.Title | Should -Be ''
            $script:reql030Args.IncludeSessionTitle | Should -BeFalse

            # An explicit queryTitle param still sends and updates the turn title.
            $script:reql030Args = $null
            Invoke-ReplMethod -Method 'workflow.sessionlog.appendActions' -ParamsYaml "queryTitle: Explicit new title`nactions:`n- type: note`n  description: work"
            $script:reql030Args.Title | Should -Be 'Explicit new title'
        } finally {
            if ($previousFresh) { Set-Item -Path Function:\Assert-ReplCurrentTurnFresh -Value $previousFresh.ScriptBlock }
            if ($previousPersist) { Set-Item -Path Function:\Invoke-ReplPersistTurn -Value $previousPersist.ScriptBlock }
            if ($null -ne $previousCacheOverride) { $env:MCP_CACHE_DIR_OVERRIDE = $previousCacheOverride } else { Remove-Item Env:\MCP_CACHE_DIR_OVERRIDE -ErrorAction SilentlyContinue }
            Remove-Variable -Name reql030Args -Scope Script -ErrorAction SilentlyContinue
            Remove-Item -LiteralPath $pluginRoot -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'TEST-MCP-REPL-029 setTurnTitle and setSessionTitle update the cache and call the dedicated server methods' {
        $pluginRoot = Join-Path $script:SmokeCache ([guid]::NewGuid().ToString('N'))
        $cacheDir = Join-Path $pluginRoot 'cache'
        [void][System.IO.Directory]::CreateDirectory($cacheDir)
        $previousCacheOverride = $env:MCP_CACHE_DIR_OVERRIDE
        $script:reql029Calls = @()

        try {
            $env:MCP_CACHE_DIR_OVERRIDE = $cacheDir
            . (Join-Path $script:LibRoot 'repl-invoke.ps1')
            $previousFresh = Get-Command Assert-ReplCurrentTurnFresh -CommandType Function -ErrorAction Stop
            $previousRaw = Get-Command Invoke-ReplRaw -CommandType Function -ErrorAction SilentlyContinue
            function Assert-ReplCurrentTurnFresh { return $true }
            function Invoke-ReplRaw {
                param([Parameter(Mandatory)][string]$Method, [string]$ParamsYaml = '')
                $script:reql029Calls += [ordered]@{ Method = $Method; ParamsYaml = $ParamsYaml }
                return [pscustomobject]@{ Success = $true; Output = ''; Error = '' }
            }

            Write-McpYamlObject -Path (Join-Path $cacheDir 'session-state.yaml') -Document ([ordered]@{
                status = 'verified'
                sessionId = 'Codex-20260712T000000Z-plugin-session'
                agent = 'Codex'
            })
            Write-McpYamlObject -Path (Join-Path $cacheDir 'current-turn.yaml') -Document ([ordered]@{
                turnRequestId = 'req-20260712T000003Z-retitle'
                queryTitle = 'Provisional'
                status = 'in_progress'
                queryText = 'Prompt'
            })

            Invoke-ReplMethod -Method 'workflow.sessionlog.setTurnTitle' -ParamsYaml 'queryTitle: Refined turn title'
            $script:LastInvokeReplMethodSuccess | Should -BeTrue
            $turn = Read-McpYamlObject -Path (Join-Path $cacheDir 'current-turn.yaml')
            $turn['queryTitle'] | Should -Be 'Refined turn title'
            @($script:reql029Calls | Where-Object { $_.Method -eq 'client.SessionLog.SetTurnTitleAsync' }).Count | Should -Be 1

            Invoke-ReplMethod -Method 'workflow.sessionlog.setSessionTitle' -ParamsYaml 'title: Refined session title'
            $script:LastInvokeReplMethodSuccess | Should -BeTrue
            $session = Read-McpYamlObject -Path (Join-Path $cacheDir 'session-state.yaml')
            $session['title'] | Should -Be 'Refined session title'
            @($script:reql029Calls | Where-Object { $_.Method -eq 'client.SessionLog.SetSessionTitleAsync' }).Count | Should -Be 1
        } finally {
            if ($previousFresh) { Set-Item -Path Function:\Assert-ReplCurrentTurnFresh -Value $previousFresh.ScriptBlock }
            if ($previousRaw) { Set-Item -Path Function:\Invoke-ReplRaw -Value $previousRaw.ScriptBlock } else { Remove-Item Function:\Invoke-ReplRaw -ErrorAction SilentlyContinue }
            if ($null -ne $previousCacheOverride) { $env:MCP_CACHE_DIR_OVERRIDE = $previousCacheOverride } else { Remove-Item Env:\MCP_CACHE_DIR_OVERRIDE -ErrorAction SilentlyContinue }
            Remove-Variable -Name reql029Calls -Scope Script -ErrorAction SilentlyContinue
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
        $oldLocation = (Get-Location).ProviderPath
        $script:capturedActionsYaml = $null

        try {
            $env:MCP_CACHE_DIR_OVERRIDE = $cacheDir
            $env:MCP_WORKSPACE_PATH = $workspace
            $env:MCP_AGENT_NAME = 'Codex'
            Set-Location -LiteralPath $workspace
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
            Set-Location -LiteralPath $oldLocation
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
        $oldLocation = (Get-Location).ProviderPath
        $script:capturedCompleteStatus = $null

        try {
            $env:MCP_CACHE_DIR_OVERRIDE = $cacheDir
            $env:MCP_WORKSPACE_PATH = $workspace
            $env:MCP_AGENT_NAME = 'Codex'
            Set-Location -LiteralPath $workspace
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
            Set-Location -LiteralPath $oldLocation
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

    It 'TEST-MCP-BUGTRIAGE-053 Codex hook resolves explicit workspace cache before stale generic env' {
        . (Join-Path $script:LibRoot 'yaml-object-mutation.ps1')
        Import-McpYamlSerializer

        $root = Join-Path $script:SmokeCache ([guid]::NewGuid().ToString('N'))
        $activeWorkspace = Join-Path $root 'McpServer'
        $staleWorkspace = Join-Path $root 'MouseKeyProxy'
        $activeCache = Join-Path $activeWorkspace '.mcpServer\codex'
        $staleCache = Join-Path $staleWorkspace '.mcpServer\codex'
        $pluginRoot = Join-Path $root 'mcpserver-codex-plugin'
        $libRoot = Join-Path $pluginRoot 'lib'
        [void][System.IO.Directory]::CreateDirectory($activeCache)
        [void][System.IO.Directory]::CreateDirectory($staleCache)
        [void][System.IO.Directory]::CreateDirectory($libRoot)
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

[System.IO.File]::AppendAllText($env:TEST_REPL_LOG, "$Method`n", [System.Text.UTF8Encoding]::new($false))
if ($Method -ne 'workflow.sessionlog.beginTurn') {
    throw "Unexpected method $Method"
}

$params = $ParamsYaml | ConvertFrom-Yaml -Ordered -ErrorAction Stop
Write-McpYamlObject -Path (Join-Path $env:TEST_ACTIVE_CACHE 'current-turn.yaml') -Document ([ordered]@{
    turnRequestId = [string]$params['requestId']
    queryTitle = [string]$params['queryTitle']
    queryText = [string]$params['queryText']
    status = 'active'
    sessionId = 'Codex-20260712T000000Z-active'
})
'{}'
'@
        [System.IO.File]::WriteAllText((Join-Path $libRoot 'repl-invoke.ps1'), $replStub, [System.Text.UTF8Encoding]::new($false))

        try {
            $activeMarker = Join-Path $activeWorkspace 'AGENTS-README-FIRST.yaml'
            $staleMarker = Join-Path $staleWorkspace 'AGENTS-README-FIRST.yaml'
            [System.IO.File]::WriteAllText($activeMarker, "workspacePath: $activeWorkspace`n", [System.Text.UTF8Encoding]::new($false))
            [System.IO.File]::WriteAllText($staleMarker, "workspacePath: $staleWorkspace`n", [System.Text.UTF8Encoding]::new($false))
            $activeMarkerItem = Get-Item -LiteralPath $activeMarker

            Write-McpYamlObject -Path (Join-Path $activeCache 'session-state.yaml') -Document ([ordered]@{
                status = 'verified'
                sessionId = 'Codex-20260712T000000Z-active'
                agent = 'Codex'
                markerFilePath = $activeMarkerItem.FullName
                markerLastWriteUtc = $activeMarkerItem.LastWriteTimeUtc.ToString('O')
            })

            $replLog = Join-Path $root 'repl.log'
            $result = Invoke-PluginChildProcess `
                -ScriptPath (Join-Path $pluginRoot 'lib\plugin-hook.ps1') `
                -Arguments @('-HookName', 'user-prompt-submit', '-HostName', 'codex', '-WorkspacePath', $activeWorkspace) `
                -Environment @{
                    MCP_WORKSPACE_PATH = $staleWorkspace
                    MCPSERVER_WORKSPACE_PATH = $staleWorkspace
                    MCP_WORKSPACE_START_DIR = $staleWorkspace
                    MCP_AGENT_NAME = 'Codex'
                    PLUGIN_AGENT_DEFAULT = 'Codex'
                    MCP_PLUGIN_HOST = 'codex'
                    MCP_PLUGIN_ROOT = $pluginRoot
                    MCP_CACHE_DIR_OVERRIDE = ''
                    PLUGIN_ROOT_OVERRIDE = ''
                    TEST_ACTIVE_CACHE = $activeCache
                    TEST_REPL_LOG = $replLog
                } `
                -InputText '{"prompt":"Explicit workspace cache regression"}'

            $result.ExitCode | Should -Be 0
            $output = $result.Stdout | ConvertFrom-Json
            $output.hookSpecificOutput.status | Should -Be 'turn-opened'
            Test-Path -LiteralPath (Join-Path $activeCache 'current-turn.yaml') | Should -BeTrue
            Test-Path -LiteralPath (Join-Path $staleCache 'current-turn.yaml') | Should -BeFalse
            Test-Path -LiteralPath (Join-Path $staleCache 'no-session-recovery.yaml') | Should -BeFalse
            $turn = Read-McpYamlObject -Path (Join-Path $activeCache 'current-turn.yaml')
            $turn['sessionId'] | Should -Be 'Codex-20260712T000000Z-active'
            [System.IO.File]::ReadAllText($replLog) | Should -Match 'workflow.sessionlog.beginTurn'
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

    It 'TEST-MCP-PLUGIN-PSONLY-001 resolves explicit override, active workspace, and fails closed without workspace' {
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
        $oldLocation = (Get-Location).ProviderPath
        [void][System.IO.Directory]::CreateDirectory($workspace)
        [void][System.IO.Directory]::CreateDirectory($pluginRoot)

        try {
            $env:MCP_CACHE_DIR_OVERRIDE = $override
            Resolve-McpCacheDir | Should -Be $override

            Remove-Item Env:\MCP_CACHE_DIR_OVERRIDE -ErrorAction SilentlyContinue
            $env:MCPSERVER_WORKSPACE_PATH = $workspace
            $env:MCP_AGENT_NAME = 'Codex'
            Set-Location -LiteralPath $workspace
            Resolve-McpCacheDir | Should -Be (Join-Path $workspace '.mcpServer\codex')

            Remove-Item Env:\MCPSERVER_WORKSPACE_PATH -ErrorAction SilentlyContinue
            Remove-Item Env:\MCP_WORKSPACE_PATH -ErrorAction SilentlyContinue
            $env:MCP_PLUGIN_ROOT = $pluginRoot
            $env:MCP_WORKSPACE_START_DIR = $pluginRoot
            $env:PLUGIN_ROOT_OVERRIDE = (Join-Path $root 'legacy-cache-root')
            [void][System.IO.Directory]::CreateDirectory($env:PLUGIN_ROOT_OVERRIDE)
            Set-Location -LiteralPath $pluginRoot
            { Resolve-McpCacheDir } | Should -Throw '*Unable to resolve the active workspace cache*'
        } finally {
            Set-Location -LiteralPath $oldLocation
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
        $previousFailsafeOverride = $env:MCPSERVER_FAILSAFE_DIR
        $previousRaw = $null

        try {
            $env:MCP_CACHE_DIR_OVERRIDE = $cacheDir
            # Hermetic failsafe dir: the plugin hooks export MCPSERVER_FAILSAFE_DIR
            # for the live workspace queue, and an inherited value would make this
            # test count (and pollute) live records instead of its own sandbox.
            $env:MCPSERVER_FAILSAFE_DIR = Join-Path $pluginRoot 'failsafe'
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
            if ($null -ne $previousFailsafeOverride) {
                $env:MCPSERVER_FAILSAFE_DIR = $previousFailsafeOverride
            } else {
                Remove-Item Env:\MCPSERVER_FAILSAFE_DIR -ErrorAction SilentlyContinue
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
        $previousFailsafeOverride = $env:MCPSERVER_FAILSAFE_DIR
        $previousRaw = $null

        try {
            $env:MCP_CACHE_DIR_OVERRIDE = $cacheDir
            # Hermetic failsafe dir: an inherited live MCPSERVER_FAILSAFE_DIR would
            # strand this test's deliberately retained record in the live queue,
            # where the plugin drain would replay the synthetic turn.
            $env:MCPSERVER_FAILSAFE_DIR = Join-Path $pluginRoot 'failsafe'
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
            if ($null -ne $previousFailsafeOverride) {
                $env:MCPSERVER_FAILSAFE_DIR = $previousFailsafeOverride
            } else {
                Remove-Item Env:\MCPSERVER_FAILSAFE_DIR -ErrorAction SilentlyContinue
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

Describe 'TEST-MCP-REPL-031 failsafe queue drain' {
    <#
    .SYNOPSIS
        Covers BUG-TRIAGE-097: the failsafe queue captures session-log submits but
        never replays them, so records accumulate on disk forever.
    .DESCRIPTION
        Validates TR-MCP-REPL-016 (oldest-first replay that clears a record only
        after a confirmed submit, keeps rejected records, and skips the in-flight
        record) and TR-MCP-REPL-017 (quarantine of malformed or attempt-exhausted
        records plus Status reporting of the real queue depth).

        Fixtures: a throwaway failsafe directory supplied through
        MCPSERVER_FAILSAFE_DIR, seeded with synthetic client.SessionLog.SubmitAsync
        records. Invoke-ReplRaw is stubbed, so no real backend and no real queue is
        touched.
    #>

    BeforeAll {
        function New-FailsafeDrainSandbox {
            <#
            .SYNOPSIS
                Creates an isolated cache plus failsafe directory pair for one test.
            #>
            $root = Join-Path $script:SmokeCache ("drain-" + [guid]::NewGuid().ToString('N'))
            $cacheDir = Join-Path $root 'cache'
            $failsafeDir = Join-Path $root 'failsafe'
            [void][System.IO.Directory]::CreateDirectory($cacheDir)
            [void][System.IO.Directory]::CreateDirectory($failsafeDir)
            [pscustomobject]@{
                Root = $root
                CacheDir = $cacheDir
                FailsafeDir = $failsafeDir
            }
        }

        function New-FailsafeDrainRecord {
            <#
            .SYNOPSIS
                Writes one synthetic session-submit failsafe record into the queue.
            #>
            param(
                [Parameter(Mandatory)][string]$FailsafeDir,
                [Parameter(Mandatory)][string]$Stamp,
                [Parameter(Mandatory)][string]$RequestId,
                [int]$DrainAttempts = -1
            )

            $record = [ordered]@{
                method = 'client.SessionLog.SubmitAsync'
                label = 'session_submit'
                timestamp = $Stamp
                params = [ordered]@{
                    sessionLog = [ordered]@{
                        sourceType = 'ClaudeCode'
                        sessionId = 'ClaudeCode-20260714T154733Z-plugin-session'
                        turns = @(
                            [ordered]@{
                                requestId = $RequestId
                                status = 'completed'
                            }
                        )
                    }
                }
            }
            if ($DrainAttempts -ge 0) { $record['drainAttempts'] = $DrainAttempts }

            $path = Join-Path $FailsafeDir ("{0}-session_submit-{1}.yaml" -f $Stamp, $RequestId.Substring($RequestId.Length - 4))
            Write-McpYamlObject -Path $path -Document $record
            return $path
        }
    }

    It 'TEST-MCP-REPL-031 replays queued records oldest-first and clears each one after a confirmed submit' {
        $sandbox = New-FailsafeDrainSandbox
        $previousCacheOverride = $env:MCP_CACHE_DIR_OVERRIDE
        $previousFailsafeOverride = $env:MCPSERVER_FAILSAFE_DIR
        $previousRaw = $null

        try {
            $env:MCP_CACHE_DIR_OVERRIDE = $sandbox.CacheDir
            $env:MCPSERVER_FAILSAFE_DIR = $sandbox.FailsafeDir
            . (Join-Path $script:LibRoot 'repl-invoke.ps1')

            New-FailsafeDrainRecord -FailsafeDir $sandbox.FailsafeDir -Stamp '20260714T154733Z' -RequestId 'req-20260714T154733Z-001-aaaa' | Out-Null
            New-FailsafeDrainRecord -FailsafeDir $sandbox.FailsafeDir -Stamp '20260716T112949Z' -RequestId 'req-20260716T112949Z-002-bbbb' | Out-Null
            New-FailsafeDrainRecord -FailsafeDir $sandbox.FailsafeDir -Stamp '20260720T230559Z' -RequestId 'req-20260720T230559Z-003-cccc' | Out-Null

            $previousRaw = Get-Command Invoke-ReplRaw -CommandType Function -ErrorAction SilentlyContinue
            $script:drainReplayOrder = [System.Collections.Generic.List[string]]::new()
            function Invoke-ReplRaw {
                param([string]$Method, [string]$ParamsYaml = '')
                $script:drainReplayOrder.Add("$Method|$([regex]::Match($ParamsYaml, 'req-[0-9TZ]+-\d+-\w+').Value)")
                return New-McpPluginReplResult -Success $true -Output "type: result" -ExitCode 0
            }

            $summary = Invoke-ReplFailsafeDrain

            $summary.scanned | Should -Be 3
            $summary.replayed | Should -Be 3
            $summary.failed | Should -Be 0
            $summary.quarantined | Should -Be 0
            $summary.aborted | Should -BeFalse
            $script:drainReplayOrder.Count | Should -Be 3
            $script:drainReplayOrder[0] | Should -Be 'client.SessionLog.SubmitAsync|req-20260714T154733Z-001-aaaa'
            $script:drainReplayOrder[1] | Should -Be 'client.SessionLog.SubmitAsync|req-20260716T112949Z-002-bbbb'
            $script:drainReplayOrder[2] | Should -Be 'client.SessionLog.SubmitAsync|req-20260720T230559Z-003-cccc'
            @(Get-ChildItem -LiteralPath $sandbox.FailsafeDir -Filter '*.yaml' -File).Count | Should -Be 0
        } finally {
            if ($previousRaw) {
                Set-Item -Path Function:\Invoke-ReplRaw -Value $previousRaw.ScriptBlock
            } else {
                Remove-Item Function:\Invoke-ReplRaw -ErrorAction SilentlyContinue
            }
            if ($null -ne $previousCacheOverride) { $env:MCP_CACHE_DIR_OVERRIDE = $previousCacheOverride } else { Remove-Item Env:\MCP_CACHE_DIR_OVERRIDE -ErrorAction SilentlyContinue }
            if ($null -ne $previousFailsafeOverride) { $env:MCPSERVER_FAILSAFE_DIR = $previousFailsafeOverride } else { Remove-Item Env:\MCPSERVER_FAILSAFE_DIR -ErrorAction SilentlyContinue }
            Remove-Variable -Name drainReplayOrder -Scope Script -ErrorAction SilentlyContinue
            Remove-Item -LiteralPath $sandbox.Root -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'TEST-MCP-REPL-032 keeps a rejected record on disk and still replays the newer records behind it' {
        $sandbox = New-FailsafeDrainSandbox
        $previousCacheOverride = $env:MCP_CACHE_DIR_OVERRIDE
        $previousFailsafeOverride = $env:MCPSERVER_FAILSAFE_DIR
        $previousRaw = $null

        try {
            $env:MCP_CACHE_DIR_OVERRIDE = $sandbox.CacheDir
            $env:MCPSERVER_FAILSAFE_DIR = $sandbox.FailsafeDir
            . (Join-Path $script:LibRoot 'repl-invoke.ps1')

            New-FailsafeDrainRecord -FailsafeDir $sandbox.FailsafeDir -Stamp '20260714T154733Z' -RequestId 'req-20260714T154733Z-001-aaaa' | Out-Null
            $poisonPath = New-FailsafeDrainRecord -FailsafeDir $sandbox.FailsafeDir -Stamp '20260716T112949Z' -RequestId 'req-20260716T112949Z-002-bbbb'
            New-FailsafeDrainRecord -FailsafeDir $sandbox.FailsafeDir -Stamp '20260720T230559Z' -RequestId 'req-20260720T230559Z-003-cccc' | Out-Null

            $previousRaw = Get-Command Invoke-ReplRaw -CommandType Function -ErrorAction SilentlyContinue
            $script:drainReplayCount = 0
            function Invoke-ReplRaw {
                param([string]$Method, [string]$ParamsYaml = '')
                $script:drainReplayCount++
                if ($ParamsYaml -match 'req-20260716T112949Z-002-bbbb') {
                    return New-McpPluginReplResult -Success $false -Output "type: error`npayload:`n  code: validation_failed" -ExitCode 1
                }
                return New-McpPluginReplResult -Success $true -Output "type: result" -ExitCode 0
            }

            $summary = Invoke-ReplFailsafeDrain

            $script:drainReplayCount | Should -Be 3
            $summary.replayed | Should -Be 2
            $summary.failed | Should -Be 1
            $summary.aborted | Should -BeFalse
            Test-Path -LiteralPath $poisonPath | Should -BeTrue
            @(Get-ChildItem -LiteralPath $sandbox.FailsafeDir -Filter '*.yaml' -File).Count | Should -Be 1
            $retained = Read-McpYamlObject -Path $poisonPath
            [int]$retained['drainAttempts'] | Should -Be 1
        } finally {
            if ($previousRaw) {
                Set-Item -Path Function:\Invoke-ReplRaw -Value $previousRaw.ScriptBlock
            } else {
                Remove-Item Function:\Invoke-ReplRaw -ErrorAction SilentlyContinue
            }
            if ($null -ne $previousCacheOverride) { $env:MCP_CACHE_DIR_OVERRIDE = $previousCacheOverride } else { Remove-Item Env:\MCP_CACHE_DIR_OVERRIDE -ErrorAction SilentlyContinue }
            if ($null -ne $previousFailsafeOverride) { $env:MCPSERVER_FAILSAFE_DIR = $previousFailsafeOverride } else { Remove-Item Env:\MCPSERVER_FAILSAFE_DIR -ErrorAction SilentlyContinue }
            Remove-Variable -Name drainReplayCount -Scope Script -ErrorAction SilentlyContinue
            Remove-Item -LiteralPath $sandbox.Root -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'TEST-MCP-REPL-033 quarantines a malformed record with a reason instead of replaying or deleting it' {
        $sandbox = New-FailsafeDrainSandbox
        $previousCacheOverride = $env:MCP_CACHE_DIR_OVERRIDE
        $previousFailsafeOverride = $env:MCPSERVER_FAILSAFE_DIR
        $previousRaw = $null

        try {
            $env:MCP_CACHE_DIR_OVERRIDE = $sandbox.CacheDir
            $env:MCPSERVER_FAILSAFE_DIR = $sandbox.FailsafeDir
            . (Join-Path $script:LibRoot 'repl-invoke.ps1')

            $malformedPath = Join-Path $sandbox.FailsafeDir '20260714T154733Z-session_submit-dead.yaml'
            [System.IO.File]::WriteAllText($malformedPath, "method: client.SessionLog.SubmitAsync`nparams: [oops`n  unbalanced: '")
            New-FailsafeDrainRecord -FailsafeDir $sandbox.FailsafeDir -Stamp '20260720T230559Z' -RequestId 'req-20260720T230559Z-003-cccc' | Out-Null

            $previousRaw = Get-Command Invoke-ReplRaw -CommandType Function -ErrorAction SilentlyContinue
            $script:drainReplayCount = 0
            function Invoke-ReplRaw {
                param([string]$Method, [string]$ParamsYaml = '')
                $script:drainReplayCount++
                return New-McpPluginReplResult -Success $true -Output "type: result" -ExitCode 0
            }

            $summary = Invoke-ReplFailsafeDrain

            $script:drainReplayCount | Should -Be 1
            $summary.quarantined | Should -Be 1
            $summary.replayed | Should -Be 1
            Test-Path -LiteralPath $malformedPath | Should -BeFalse
            $quarantineDir = Get-ReplFailsafeQuarantineDir
            Test-Path -LiteralPath $quarantineDir | Should -BeTrue
            @(Get-ChildItem -LiteralPath $quarantineDir -Filter '*.yaml' -File).Count | Should -Be 1
            $reasonFile = @(Get-ChildItem -LiteralPath $quarantineDir -Filter '*.reason.txt' -File)
            $reasonFile.Count | Should -Be 1
            [System.IO.File]::ReadAllText($reasonFile[0].FullName) | Should -Not -BeNullOrEmpty
            @(Get-ChildItem -LiteralPath $sandbox.FailsafeDir -Filter '*.yaml' -File).Count | Should -Be 0
        } finally {
            if ($previousRaw) {
                Set-Item -Path Function:\Invoke-ReplRaw -Value $previousRaw.ScriptBlock
            } else {
                Remove-Item Function:\Invoke-ReplRaw -ErrorAction SilentlyContinue
            }
            if ($null -ne $previousCacheOverride) { $env:MCP_CACHE_DIR_OVERRIDE = $previousCacheOverride } else { Remove-Item Env:\MCP_CACHE_DIR_OVERRIDE -ErrorAction SilentlyContinue }
            if ($null -ne $previousFailsafeOverride) { $env:MCPSERVER_FAILSAFE_DIR = $previousFailsafeOverride } else { Remove-Item Env:\MCPSERVER_FAILSAFE_DIR -ErrorAction SilentlyContinue }
            Remove-Variable -Name drainReplayCount -Scope Script -ErrorAction SilentlyContinue
            Remove-Item -LiteralPath $sandbox.Root -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'TEST-MCP-REPL-034 quarantines a record that exhausted its drain attempt budget instead of retrying forever' {
        $sandbox = New-FailsafeDrainSandbox
        $previousCacheOverride = $env:MCP_CACHE_DIR_OVERRIDE
        $previousFailsafeOverride = $env:MCPSERVER_FAILSAFE_DIR
        $previousRaw = $null

        try {
            $env:MCP_CACHE_DIR_OVERRIDE = $sandbox.CacheDir
            $env:MCPSERVER_FAILSAFE_DIR = $sandbox.FailsafeDir
            . (Join-Path $script:LibRoot 'repl-invoke.ps1')

            $exhaustedPath = New-FailsafeDrainRecord -FailsafeDir $sandbox.FailsafeDir -Stamp '20260714T154733Z' -RequestId 'req-20260714T154733Z-001-aaaa' -DrainAttempts 5

            $previousRaw = Get-Command Invoke-ReplRaw -CommandType Function -ErrorAction SilentlyContinue
            $script:drainReplayCount = 0
            function Invoke-ReplRaw {
                param([string]$Method, [string]$ParamsYaml = '')
                $script:drainReplayCount++
                return New-McpPluginReplResult -Success $true -Output "type: result" -ExitCode 0
            }

            $summary = Invoke-ReplFailsafeDrain -MaxAttempts 5

            $script:drainReplayCount | Should -Be 0
            $summary.quarantined | Should -Be 1
            $summary.replayed | Should -Be 0
            Test-Path -LiteralPath $exhaustedPath | Should -BeFalse
            @(Get-ChildItem -LiteralPath (Get-ReplFailsafeQuarantineDir) -Filter '*.yaml' -File).Count | Should -Be 1
        } finally {
            if ($previousRaw) {
                Set-Item -Path Function:\Invoke-ReplRaw -Value $previousRaw.ScriptBlock
            } else {
                Remove-Item Function:\Invoke-ReplRaw -ErrorAction SilentlyContinue
            }
            if ($null -ne $previousCacheOverride) { $env:MCP_CACHE_DIR_OVERRIDE = $previousCacheOverride } else { Remove-Item Env:\MCP_CACHE_DIR_OVERRIDE -ErrorAction SilentlyContinue }
            if ($null -ne $previousFailsafeOverride) { $env:MCPSERVER_FAILSAFE_DIR = $previousFailsafeOverride } else { Remove-Item Env:\MCPSERVER_FAILSAFE_DIR -ErrorAction SilentlyContinue }
            Remove-Variable -Name drainReplayCount -Scope Script -ErrorAction SilentlyContinue
            Remove-Item -LiteralPath $sandbox.Root -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'TEST-MCP-REPL-035 aborts the drain and consumes no attempts when the backend is unreachable' {
        $sandbox = New-FailsafeDrainSandbox
        $previousCacheOverride = $env:MCP_CACHE_DIR_OVERRIDE
        $previousFailsafeOverride = $env:MCPSERVER_FAILSAFE_DIR
        $previousRaw = $null

        try {
            $env:MCP_CACHE_DIR_OVERRIDE = $sandbox.CacheDir
            $env:MCPSERVER_FAILSAFE_DIR = $sandbox.FailsafeDir
            . (Join-Path $script:LibRoot 'repl-invoke.ps1')

            $firstPath = New-FailsafeDrainRecord -FailsafeDir $sandbox.FailsafeDir -Stamp '20260714T154733Z' -RequestId 'req-20260714T154733Z-001-aaaa'
            New-FailsafeDrainRecord -FailsafeDir $sandbox.FailsafeDir -Stamp '20260720T230559Z' -RequestId 'req-20260720T230559Z-003-cccc' | Out-Null

            $previousRaw = Get-Command Invoke-ReplRaw -CommandType Function -ErrorAction SilentlyContinue
            $script:drainReplayCount = 0
            function Invoke-ReplRaw {
                param([string]$Method, [string]$ParamsYaml = '')
                $script:drainReplayCount++
                return New-McpPluginReplResult -Success $false -Output '' -Error 'MCP_UNTRUSTED: marker refresh failed before REPL request'
            }

            $summary = Invoke-ReplFailsafeDrain

            $script:drainReplayCount | Should -Be 1
            $summary.aborted | Should -BeTrue
            $summary.abortReason | Should -Not -BeNullOrEmpty
            $summary.replayed | Should -Be 0
            $summary.quarantined | Should -Be 0
            @(Get-ChildItem -LiteralPath $sandbox.FailsafeDir -Filter '*.yaml' -File).Count | Should -Be 2
            $retained = Read-McpYamlObject -Path $firstPath
            $retained.Contains('drainAttempts') | Should -BeFalse
        } finally {
            if ($previousRaw) {
                Set-Item -Path Function:\Invoke-ReplRaw -Value $previousRaw.ScriptBlock
            } else {
                Remove-Item Function:\Invoke-ReplRaw -ErrorAction SilentlyContinue
            }
            if ($null -ne $previousCacheOverride) { $env:MCP_CACHE_DIR_OVERRIDE = $previousCacheOverride } else { Remove-Item Env:\MCP_CACHE_DIR_OVERRIDE -ErrorAction SilentlyContinue }
            if ($null -ne $previousFailsafeOverride) { $env:MCPSERVER_FAILSAFE_DIR = $previousFailsafeOverride } else { Remove-Item Env:\MCPSERVER_FAILSAFE_DIR -ErrorAction SilentlyContinue }
            Remove-Variable -Name drainReplayCount -Scope Script -ErrorAction SilentlyContinue
            Remove-Item -LiteralPath $sandbox.Root -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'TEST-MCP-REPL-036 skips the in-flight failsafe record written by the submit currently in progress' {
        $sandbox = New-FailsafeDrainSandbox
        $previousCacheOverride = $env:MCP_CACHE_DIR_OVERRIDE
        $previousFailsafeOverride = $env:MCPSERVER_FAILSAFE_DIR
        $previousRaw = $null

        try {
            $env:MCP_CACHE_DIR_OVERRIDE = $sandbox.CacheDir
            $env:MCPSERVER_FAILSAFE_DIR = $sandbox.FailsafeDir
            . (Join-Path $script:LibRoot 'repl-invoke.ps1')

            $inFlightPath = Write-ReplFailsafe -Method 'client.SessionLog.SubmitAsync' -ParamsYaml "sessionLog:`n  sessionId: ClaudeCode-20260720T230559Z-plugin-session" -Label 'session_submit'
            $inFlightPath | Should -Not -BeNullOrEmpty

            $previousRaw = Get-Command Invoke-ReplRaw -CommandType Function -ErrorAction SilentlyContinue
            $script:drainReplayCount = 0
            function Invoke-ReplRaw {
                param([string]$Method, [string]$ParamsYaml = '')
                $script:drainReplayCount++
                return New-McpPluginReplResult -Success $true -Output "type: result" -ExitCode 0
            }

            $summary = Invoke-ReplFailsafeDrain

            $script:drainReplayCount | Should -Be 0
            $summary.skipped | Should -Be 1
            $summary.replayed | Should -Be 0
            Test-Path -LiteralPath $inFlightPath | Should -BeTrue

            Clear-ReplFailsafe -Path $inFlightPath
            Test-Path -LiteralPath $inFlightPath | Should -BeFalse
        } finally {
            if ($previousRaw) {
                Set-Item -Path Function:\Invoke-ReplRaw -Value $previousRaw.ScriptBlock
            } else {
                Remove-Item Function:\Invoke-ReplRaw -ErrorAction SilentlyContinue
            }
            if ($null -ne $previousCacheOverride) { $env:MCP_CACHE_DIR_OVERRIDE = $previousCacheOverride } else { Remove-Item Env:\MCP_CACHE_DIR_OVERRIDE -ErrorAction SilentlyContinue }
            if ($null -ne $previousFailsafeOverride) { $env:MCPSERVER_FAILSAFE_DIR = $previousFailsafeOverride } else { Remove-Item Env:\MCPSERVER_FAILSAFE_DIR -ErrorAction SilentlyContinue }
            Remove-Variable -Name drainReplayCount -Scope Script -ErrorAction SilentlyContinue
            Remove-Item -LiteralPath $sandbox.Root -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'TEST-MCP-REPL-037 triggers exactly one queue drain after the first successful REPL call' {
        $sandbox = New-FailsafeDrainSandbox
        $previousCacheOverride = $env:MCP_CACHE_DIR_OVERRIDE
        $previousFailsafeOverride = $env:MCPSERVER_FAILSAFE_DIR
        $previousDrain = $null

        try {
            $env:MCP_CACHE_DIR_OVERRIDE = $sandbox.CacheDir
            $env:MCPSERVER_FAILSAFE_DIR = $sandbox.FailsafeDir
            . (Join-Path $script:LibRoot 'repl-invoke.ps1')

            $previousDrain = Get-Command Invoke-ReplFailsafeDrain -CommandType Function -ErrorAction SilentlyContinue
            $script:drainInvocationCount = 0
            function Invoke-ReplFailsafeDrain {
                param([int]$MaxRecords = 0, [int]$MaxAttempts = 5)
                $script:drainInvocationCount++
                return [ordered]@{ scanned = 0; replayed = 0; failed = 0; quarantined = 0; skipped = 0; aborted = $false }
            }

            Invoke-ReplFailsafeDrainOnFirstSuccess
            Invoke-ReplFailsafeDrainOnFirstSuccess
            Invoke-ReplFailsafeDrainOnFirstSuccess

            $script:drainInvocationCount | Should -Be 1

            $source = [System.IO.File]::ReadAllText((Join-Path $script:LibRoot 'repl-invoke.ps1'))
            $rawMatch = [regex]::Match($source, '(?s)function Invoke-ReplRaw\s*\{(?<body>.*?)\r?\n\}[\r\n]+function Get-ReplSessionStateValue')
            $rawMatch.Success | Should -BeTrue
            $rawMatch.Groups['body'].Value | Should -Match 'Invoke-ReplFailsafeDrainOnFirstSuccess'
        } finally {
            if ($previousDrain) {
                Set-Item -Path Function:\Invoke-ReplFailsafeDrain -Value $previousDrain.ScriptBlock
            } else {
                Remove-Item Function:\Invoke-ReplFailsafeDrain -ErrorAction SilentlyContinue
            }
            if ($null -ne $previousCacheOverride) { $env:MCP_CACHE_DIR_OVERRIDE = $previousCacheOverride } else { Remove-Item Env:\MCP_CACHE_DIR_OVERRIDE -ErrorAction SilentlyContinue }
            if ($null -ne $previousFailsafeOverride) { $env:MCPSERVER_FAILSAFE_DIR = $previousFailsafeOverride } else { Remove-Item Env:\MCPSERVER_FAILSAFE_DIR -ErrorAction SilentlyContinue }
            Remove-Variable -Name drainInvocationCount -Scope Script -ErrorAction SilentlyContinue
            Remove-Item -LiteralPath $sandbox.Root -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'TEST-MCP-REPL-038 dispatches workflow.failsafe.drain locally without a server round trip' {
        $sandbox = New-FailsafeDrainSandbox
        $previousCacheOverride = $env:MCP_CACHE_DIR_OVERRIDE
        $previousFailsafeOverride = $env:MCPSERVER_FAILSAFE_DIR
        $previousDrain = $null
        $previousRaw = $null

        try {
            $env:MCP_CACHE_DIR_OVERRIDE = $sandbox.CacheDir
            $env:MCPSERVER_FAILSAFE_DIR = $sandbox.FailsafeDir
            . (Join-Path $script:LibRoot 'repl-invoke.ps1')

            $previousDrain = Get-Command Invoke-ReplFailsafeDrain -CommandType Function -ErrorAction SilentlyContinue
            $previousRaw = Get-Command Invoke-ReplRaw -CommandType Function -ErrorAction SilentlyContinue
            $script:drainInvocationCount = 0
            $script:drainMaxRecords = -1
            $script:rawInvocationCount = 0
            function Invoke-ReplFailsafeDrain {
                param([int]$MaxRecords = 0, [int]$MaxAttempts = 5)
                $script:drainInvocationCount++
                $script:drainMaxRecords = $MaxRecords
                return [ordered]@{ scanned = 2; replayed = 2; failed = 0; quarantined = 0; skipped = 0; aborted = $false; abortReason = '' }
            }
            function Invoke-ReplRaw {
                param([string]$Method, [string]$ParamsYaml = '')
                $script:rawInvocationCount++
                return New-McpPluginReplResult -Success $true -Output "type: result" -ExitCode 0
            }

            $output = Invoke-ReplMethod -Method 'workflow.failsafe.drain' -ParamsYaml 'maxRecords: 7'

            $script:drainInvocationCount | Should -Be 1
            $script:drainMaxRecords | Should -Be 7
            $script:rawInvocationCount | Should -Be 0
            ($output -join "`n") | Should -Match 'replayed'
        } finally {
            if ($previousDrain) {
                Set-Item -Path Function:\Invoke-ReplFailsafeDrain -Value $previousDrain.ScriptBlock
            } else {
                Remove-Item Function:\Invoke-ReplFailsafeDrain -ErrorAction SilentlyContinue
            }
            if ($previousRaw) {
                Set-Item -Path Function:\Invoke-ReplRaw -Value $previousRaw.ScriptBlock
            } else {
                Remove-Item Function:\Invoke-ReplRaw -ErrorAction SilentlyContinue
            }
            if ($null -ne $previousCacheOverride) { $env:MCP_CACHE_DIR_OVERRIDE = $previousCacheOverride } else { Remove-Item Env:\MCP_CACHE_DIR_OVERRIDE -ErrorAction SilentlyContinue }
            if ($null -ne $previousFailsafeOverride) { $env:MCPSERVER_FAILSAFE_DIR = $previousFailsafeOverride } else { Remove-Item Env:\MCPSERVER_FAILSAFE_DIR -ErrorAction SilentlyContinue }
            Remove-Variable -Name drainInvocationCount, drainMaxRecords, rawInvocationCount -Scope Script -ErrorAction SilentlyContinue
            Remove-Item -LiteralPath $sandbox.Root -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'TEST-MCP-REPL-039 status counts queued failsafe records in pendingCount' {
        $sandbox = New-FailsafeDrainSandbox

        try {
            . (Join-Path $script:LibRoot 'yaml-object-mutation.ps1')
            Import-McpYamlSerializer
            $record = [ordered]@{
                method = 'client.SessionLog.SubmitAsync'
                label = 'session_submit'
                timestamp = '20260714T154733Z'
                params = [ordered]@{ sessionLog = [ordered]@{ sessionId = 'ClaudeCode-20260714T154733Z-plugin-session' } }
            }
            Write-McpYamlObject -Path (Join-Path $sandbox.FailsafeDir '20260714T154733Z-session_submit-aaaa.yaml') -Document $record
            Write-McpYamlObject -Path (Join-Path $sandbox.FailsafeDir '20260716T112949Z-session_submit-bbbb.yaml') -Document $record
            Write-McpYamlObject -Path (Join-Path (Join-Path $sandbox.FailsafeDir 'quarantine') '20260716T112949Z-session_submit-cccc.yaml') -Document $record

            $result = Invoke-PluginChildProcess `
                -ScriptPath (Join-Path $script:LibRoot 'mcp-status.ps1') `
                -Environment @{
                    MCP_PLUGIN_ROOT = $script:StagedRoot
                    MCP_PLUGIN_HOST = 'claude-code'
                    MCP_AGENT_NAME = 'ClaudeCode'
                    MCP_CACHE_DIR_OVERRIDE = $sandbox.CacheDir
                    MCPSERVER_FAILSAFE_DIR = $sandbox.FailsafeDir
                    MCP_WORKSPACE_PATH = $script:RepoRoot
                    MCPSERVER_WORKSPACE_PATH = $script:RepoRoot
                }

            $result.ExitCode | Should -Be 0
            $status = $result.Stdout | ConvertFrom-Json
            $status.failsafeDir | Should -Be $sandbox.FailsafeDir
            $status.failsafeCount | Should -Be 2
            $status.failsafeQuarantineCount | Should -Be 1
            $status.pendingCount | Should -Be 2
        } finally {
            Remove-Item -LiteralPath $sandbox.Root -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'TEST-MCP-FAILSAFE-001 HTTP 503 abort does not increment drainAttempts and a later drain in the same process can replay' {
        $sandbox = New-FailsafeDrainSandbox
        $previousCacheOverride = $env:MCP_CACHE_DIR_OVERRIDE
        $previousFailsafeOverride = $env:MCPSERVER_FAILSAFE_DIR
        $previousRaw = $null

        try {
            $env:MCP_CACHE_DIR_OVERRIDE = $sandbox.CacheDir
            $env:MCPSERVER_FAILSAFE_DIR = $sandbox.FailsafeDir
            . (Join-Path $script:LibRoot 'repl-invoke.ps1')

            $recordPath = New-FailsafeDrainRecord -FailsafeDir $sandbox.FailsafeDir -Stamp '20260819T191800Z' -RequestId 'req-20260819T191800Z-001-drain'

            $previousRaw = Get-Command Invoke-ReplRaw -CommandType Function -ErrorAction SilentlyContinue
            $script:drain503Calls = 0
            function Invoke-ReplRaw {
                param([string]$Method, [string]$ParamsYaml = '')
                $script:drain503Calls++
                if ($script:drain503Calls -eq 1) {
                    return New-McpPluginReplResult -Success $false -Output "type: error`npayload:`n  code: backend_unavailable`n  retryable: true" -Error 'HTTP 503 backend_unavailable' -ExitCode 1
                }
                return New-McpPluginReplResult -Success $true -Output "type: result" -ExitCode 0
            }

            Invoke-ReplFailsafeDrainOnFirstSuccess
            Test-Path -LiteralPath $recordPath | Should -BeTrue
            $retained = Read-McpYamlObject -Path $recordPath
            $retained.Contains('drainAttempts') | Should -BeFalse

            Invoke-ReplFailsafeDrainOnFirstSuccess
            Test-Path -LiteralPath $recordPath | Should -BeFalse
            $script:drain503Calls | Should -BeGreaterThan 1
        } finally {
            if ($previousRaw) {
                Set-Item -Path Function:\Invoke-ReplRaw -Value $previousRaw.ScriptBlock
            } else {
                Remove-Item Function:\Invoke-ReplRaw -ErrorAction SilentlyContinue
            }
            if ($null -ne $previousCacheOverride) { $env:MCP_CACHE_DIR_OVERRIDE = $previousCacheOverride } else { Remove-Item Env:\MCP_CACHE_DIR_OVERRIDE -ErrorAction SilentlyContinue }
            if ($null -ne $previousFailsafeOverride) { $env:MCPSERVER_FAILSAFE_DIR = $previousFailsafeOverride } else { Remove-Item Env:\MCPSERVER_FAILSAFE_DIR -ErrorAction SilentlyContinue }
            Remove-Variable -Name drain503Calls -Scope Script -ErrorAction SilentlyContinue
            Remove-Item -LiteralPath $sandbox.Root -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}

Describe 'TEST-MCP-REPL-040 session-log turn persistence hardening' {
    <#
    .SYNOPSIS
        Covers BUG-TRIAGE-086/087/089/091/098/099: deliberately empty turn titles
        fail parameter binding, superseding a turn can clobber a refined title,
        and workflow.sessionlog.failTurn falls through to the REPL where the
        in-process workflow throws 'No active session exists'.
    .DESCRIPTION
        Validates TR-MCP-REPL-018 (an empty Title is a deliberate "omit the
        title" value accepted end to end through the real Invoke-ReplPersistTurn
        and Invoke-ReplTurnUpsertParams), TR-MCP-REPL-019 (a supersede keeps a
        locally refined title without a server round trip, defers a raw or empty
        local title to the server-side title fetched through the existing
        client.SessionLog.QueryAsync passthrough, and never re-sends raw prompt
        text as a title), and TR-MCP-REPL-020 (workflow.sessionlog.failTurn is
        handled locally against cache-only state: it persists status failed with
        the failure note and clears current-turn.yaml instead of dispatching to
        the REPL).

        Fixtures: throwaway cache and failsafe directories supplied through
        MCP_CACHE_DIR_OVERRIDE and MCPSERVER_FAILSAFE_DIR (the same temp-dir
        idiom as the TEST-MCP-REPL-031..039 failsafe-drain tests), seeded
        session-state.yaml and current-turn.yaml documents, a stubbed
        Assert-ReplCurrentTurnFresh, and a stubbed Invoke-ReplRaw that captures
        client.SessionLog.SubmitAsync payloads, answers
        client.SessionLog.QueryAsync with a canned server title, and answers any
        unexpected fall-through with the live REPL error envelope
        (method_invocation_error: No active session exists). The real
        Invoke-ReplPersistTurn and Invoke-ReplTurnUpsertParams run unstubbed so
        the parameter-binding path under test is exercised.
    #>

    BeforeAll {
        function New-TurnShimSandbox {
            <#
            .SYNOPSIS
                Creates an isolated cache plus failsafe directory pair for one test.
            #>
            $root = Join-Path $script:SmokeCache ("turnshim-" + [guid]::NewGuid().ToString('N'))
            $cacheDir = Join-Path $root 'cache'
            $failsafeDir = Join-Path $root 'failsafe'
            [void][System.IO.Directory]::CreateDirectory($cacheDir)
            [void][System.IO.Directory]::CreateDirectory($failsafeDir)
            [pscustomobject]@{
                Root = $root
                CacheDir = $cacheDir
                FailsafeDir = $failsafeDir
                SessionId = 'ClaudeCode-20260721T000000Z-plugin-session'
            }
        }

        function Initialize-TurnShimCache {
            <#
            .SYNOPSIS
                Seeds session-state.yaml plus current-turn.yaml for one test.
            #>
            param(
                [Parameter(Mandatory)][string]$CacheDir,
                [Parameter(Mandatory)][string]$RequestId,
                [AllowEmptyString()][string]$QueryTitle = 'User prompt',
                [string]$QueryText = 'Prompt text'
            )

            Write-McpYamlObject -Path (Join-Path $CacheDir 'session-state.yaml') -Document ([ordered]@{
                status = 'verified'
                sessionId = 'ClaudeCode-20260721T000000Z-plugin-session'
                agent = 'ClaudeCode'
            })
            Write-McpYamlObject -Path (Join-Path $CacheDir 'current-turn.yaml') -Document ([ordered]@{
                turnRequestId = $RequestId
                queryTitle = $QueryTitle
                openedAt = '2026-07-21T00:00:01Z'
                status = 'in_progress'
                sessionId = 'ClaudeCode-20260721T000000Z-plugin-session'
                codeEdits = 0
                queryText = $QueryText
            })
        }

        function Initialize-TurnShimCapture {
            <#
            .SYNOPSIS
                Resets the script-scoped capture state consumed by the stub.
            #>
            param([Parameter(Mandatory)][string]$SessionId)

            $script:t40Submits = [System.Collections.Generic.List[string]]::new()
            $script:t40QueryCalls = [System.Collections.Generic.List[string]]::new()
            $script:t40FallThrough = [System.Collections.Generic.List[string]]::new()
            $script:t40SessionId = $SessionId
            $script:t40ServerRequestId = ''
            $script:t40ServerTitle = ''
        }

        # Shared Invoke-ReplRaw stub body. Applied per test with
        # Set-Item Function:\Invoke-ReplRaw so it replaces the dot-sourced real
        # function in the It scope (same idiom as the finally-block restores).
        # SubmitAsync payloads are captured into $script:t40Submits and
        # confirmed as persisted. QueryAsync answers with
        # $script:t40ServerTitle for $script:t40ServerRequestId inside the
        # seeded session. Every other method is recorded as a fall-through in
        # $script:t40FallThrough and answered with the live REPL error envelope
        # observed for cache-only failTurn calls.
        $script:TurnShimReplRawStub = {
            param([string]$Method, [string]$ParamsYaml = '')

            if ($Method -eq 'client.SessionLog.SubmitAsync') {
                $script:t40Submits.Add($ParamsYaml)
                $ok = [ordered]@{
                    type = 'result'
                    payload = [ordered]@{
                        result = [ordered]@{
                            persisted = $true
                            degraded = $false
                            persistenceStrategy = 'mcp-service'
                        }
                    }
                }
                return New-McpPluginReplResult -Success $true -Output (ConvertTo-Yaml -Data $ok -Options WithIndentedSequences) -ExitCode 0
            }

            if ($Method -eq 'client.SessionLog.QueryAsync') {
                $script:t40QueryCalls.Add($ParamsYaml)
                $turn = [ordered]@{ requestId = $script:t40ServerRequestId }
                if (-not [string]::IsNullOrWhiteSpace($script:t40ServerTitle)) {
                    $turn.queryTitle = $script:t40ServerTitle
                }
                $queryResult = [ordered]@{
                    type = 'result'
                    payload = [ordered]@{
                        result = [ordered]@{
                            totalCount = 1
                            items = @(
                                [ordered]@{
                                    sessionId = $script:t40SessionId
                                    turns = @($turn)
                                }
                            )
                        }
                    }
                }
                return New-McpPluginReplResult -Success $true -Output (ConvertTo-Yaml -Data $queryResult -Options WithIndentedSequences) -ExitCode 0
            }

            $script:t40FallThrough.Add($Method)
            return New-McpPluginReplResult -Success $false -Output "type: error`npayload:`n  code: method_invocation_error`n  message: No active session exists" -ExitCode 1
        }

        # Freshness stub body applied with Set-Item in tests that exercise the
        # appendDialog/appendActions/completeTurn/failTurn freshness gate.
        $script:TurnShimFreshStub = { return $true }

        function Get-TurnShimSubmittedTurn {
            <#
            .SYNOPSIS
                Parses one captured SubmitAsync payload back into its turn map.
            #>
            param([Parameter(Mandatory)][string]$ParamsYaml)

            $submit = Convert-ReplParamsYamlToObject -ParamsYaml $ParamsYaml
            $sessionLog = Get-ReplObjectValue -InputObject $submit -Name 'sessionLog'
            return @(Get-ReplObjectValue -InputObject $sessionLog -Name 'turns')[0]
        }

        function Get-TurnShimSubmittedSessionLog {
            <#
            .SYNOPSIS
                Parses one captured SubmitAsync payload back into its session log map.
            #>
            param([Parameter(Mandatory)][string]$ParamsYaml)

            $submit = Convert-ReplParamsYamlToObject -ParamsYaml $ParamsYaml
            return (Get-ReplObjectValue -InputObject $submit -Name 'sessionLog')
        }

        function Remove-TurnShimState {
            <#
            .SYNOPSIS
                Clears the script-scoped capture variables between tests.
            #>
            Remove-Variable -Name t40Submits, t40QueryCalls, t40FallThrough, t40SessionId, t40ServerTitle, t40ServerRequestId -Scope Script -ErrorAction SilentlyContinue
        }
    }

    It 'TEST-MCP-REPL-040 persists an appendDialog without queryTitle instead of failing the empty-title bind' {
        # TR-MCP-REPL-018 (BUG-TRIAGE-087/089/091/098): the deliberate empty
        # Title must flow through the real Invoke-ReplPersistTurn into
        # Invoke-ReplTurnUpsertParams and reach SubmitAsync with the title
        # omitted, instead of dying on ParameterBindingValidationException.
        $sandbox = New-TurnShimSandbox
        $previousCacheOverride = $env:MCP_CACHE_DIR_OVERRIDE
        $previousFailsafeOverride = $env:MCPSERVER_FAILSAFE_DIR
        $previousFresh = $null
        $previousRaw = $null

        try {
            $env:MCP_CACHE_DIR_OVERRIDE = $sandbox.CacheDir
            $env:MCPSERVER_FAILSAFE_DIR = $sandbox.FailsafeDir
            . (Join-Path $script:LibRoot 'repl-invoke.ps1')
            $previousFresh = Get-Command Assert-ReplCurrentTurnFresh -CommandType Function -ErrorAction Stop
            $previousRaw = Get-Command Invoke-ReplRaw -CommandType Function -ErrorAction SilentlyContinue
            Set-Item -Path Function:\Assert-ReplCurrentTurnFresh -Value $script:TurnShimFreshStub
            Initialize-TurnShimCapture -SessionId $sandbox.SessionId
            Set-Item -Path Function:\Invoke-ReplRaw -Value $script:TurnShimReplRawStub
            Initialize-TurnShimCache -CacheDir $sandbox.CacheDir -RequestId 'req-20260721T000002Z-dialog'

            Invoke-ReplMethod -Method 'workflow.sessionlog.appendDialog' -ParamsYaml "dialogItems:`n- role: assistant`n  content: probing the failure`n  category: diagnostic"

            $script:LastInvokeReplMethodSuccess | Should -BeTrue
            $script:t40Submits.Count | Should -Be 1
            $turn = Get-TurnShimSubmittedTurn -ParamsYaml $script:t40Submits[0]
            (Get-ReplObjectValue -InputObject $turn -Name 'queryTitle') | Should -BeNullOrEmpty
            [string](Get-ReplObjectValue -InputObject $turn -Name 'status') | Should -Be 'in_progress'
        } finally {
            if ($previousFresh) { Set-Item -Path Function:\Assert-ReplCurrentTurnFresh -Value $previousFresh.ScriptBlock }
            if ($previousRaw) { Set-Item -Path Function:\Invoke-ReplRaw -Value $previousRaw.ScriptBlock } else { Remove-Item Function:\Invoke-ReplRaw -ErrorAction SilentlyContinue }
            if ($null -ne $previousCacheOverride) { $env:MCP_CACHE_DIR_OVERRIDE = $previousCacheOverride } else { Remove-Item Env:\MCP_CACHE_DIR_OVERRIDE -ErrorAction SilentlyContinue }
            if ($null -ne $previousFailsafeOverride) { $env:MCPSERVER_FAILSAFE_DIR = $previousFailsafeOverride } else { Remove-Item Env:\MCPSERVER_FAILSAFE_DIR -ErrorAction SilentlyContinue }
            Remove-TurnShimState
            Remove-Item -LiteralPath $sandbox.Root -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'TEST-MCP-REPL-040 populates agent runtime headers from session-state when env vars are absent' {
        $sandbox = New-TurnShimSandbox
        $previousCacheOverride = $env:MCP_CACHE_DIR_OVERRIDE
        $previousFailsafeOverride = $env:MCPSERVER_FAILSAFE_DIR
        $previousAgentSessionId = $env:MCP_AGENT_SESSION_ID
        $previousTranscript = $env:MCP_AGENT_SESSION_TRANSCRIPT_FILE
        $previousExecutablePath = $env:MCP_AGENT_EXECUTABLE_PATH
        $previousExecutableVersion = $env:MCP_AGENT_EXECUTABLE_VERSION
        $previousFresh = $null
        $previousRaw = $null

        try {
            $env:MCP_CACHE_DIR_OVERRIDE = $sandbox.CacheDir
            $env:MCPSERVER_FAILSAFE_DIR = $sandbox.FailsafeDir
            Remove-Item Env:\MCP_AGENT_SESSION_ID -ErrorAction SilentlyContinue
            Remove-Item Env:\MCP_AGENT_SESSION_TRANSCRIPT_FILE -ErrorAction SilentlyContinue
            Remove-Item Env:\MCP_AGENT_EXECUTABLE_PATH -ErrorAction SilentlyContinue
            Remove-Item Env:\MCP_AGENT_EXECUTABLE_VERSION -ErrorAction SilentlyContinue
            . (Join-Path $script:LibRoot 'repl-invoke.ps1')
            $previousFresh = Get-Command Assert-ReplCurrentTurnFresh -CommandType Function -ErrorAction Stop
            $previousRaw = Get-Command Invoke-ReplRaw -CommandType Function -ErrorAction SilentlyContinue
            Set-Item -Path Function:\Assert-ReplCurrentTurnFresh -Value $script:TurnShimFreshStub
            Initialize-TurnShimCapture -SessionId $sandbox.SessionId
            Set-Item -Path Function:\Invoke-ReplRaw -Value $script:TurnShimReplRawStub
            Initialize-TurnShimCache -CacheDir $sandbox.CacheDir -RequestId 'req-20260721T000004Z-agent-runtime'
            # TR-MCP-PLUGIN-HEADER-001: the transcript fixture must be a REAL file.
            # A session-state path pointing at a missing file is a fabrication and is
            # deliberately dropped (covered by TEST-MCP-PLUGIN-HEADER-005).
            $realTranscript = Join-Path $sandbox.CacheDir 'codex-rollout.jsonl'
            [System.IO.File]::WriteAllText($realTranscript, '{"t":1}')
            $state = Read-McpYamlObject -Path (Join-Path $sandbox.CacheDir 'session-state.yaml')
            $state['agentSessionId'] = 'codex-root-session-001'
            $state['agentSessionTranscriptFile'] = $realTranscript
            $state['agentExecutablePath'] = 'C:\Users\kingd\AppData\Roaming\npm\codex.cmd'
            $state['agentExecutableVersion'] = '1.82.0'
            Write-McpYamlObject -Path (Join-Path $sandbox.CacheDir 'session-state.yaml') -Document $state

            Invoke-ReplMethod -Method 'workflow.sessionlog.appendActions' -ParamsYaml "actions:`n- type: note`n  description: runtime header enforcement`n  status: completed"

            $script:LastInvokeReplMethodSuccess | Should -BeTrue
            $script:t40Submits.Count | Should -Be 1
            $sessionLog = Get-TurnShimSubmittedSessionLog -ParamsYaml $script:t40Submits[0]
            [string](Get-ReplObjectValue -InputObject $sessionLog -Name 'agentSessionId') | Should -Be 'codex-root-session-001'
            [string](Get-ReplObjectValue -InputObject $sessionLog -Name 'agentSessionTranscriptFile') | Should -Be $realTranscript
            [string](Get-ReplObjectValue -InputObject $sessionLog -Name 'agentExecutablePath') | Should -Be 'C:\Users\kingd\AppData\Roaming\npm\codex.cmd'
            [string](Get-ReplObjectValue -InputObject $sessionLog -Name 'agentExecutableVersion') | Should -Be '1.82.0'
        } finally {
            if ($previousFresh) { Set-Item -Path Function:\Assert-ReplCurrentTurnFresh -Value $previousFresh.ScriptBlock }
            if ($previousRaw) { Set-Item -Path Function:\Invoke-ReplRaw -Value $previousRaw.ScriptBlock } else { Remove-Item Function:\Invoke-ReplRaw -ErrorAction SilentlyContinue }
            if ($null -ne $previousCacheOverride) { $env:MCP_CACHE_DIR_OVERRIDE = $previousCacheOverride } else { Remove-Item Env:\MCP_CACHE_DIR_OVERRIDE -ErrorAction SilentlyContinue }
            if ($null -ne $previousFailsafeOverride) { $env:MCPSERVER_FAILSAFE_DIR = $previousFailsafeOverride } else { Remove-Item Env:\MCPSERVER_FAILSAFE_DIR -ErrorAction SilentlyContinue }
            if ($null -ne $previousAgentSessionId) { $env:MCP_AGENT_SESSION_ID = $previousAgentSessionId } else { Remove-Item Env:\MCP_AGENT_SESSION_ID -ErrorAction SilentlyContinue }
            if ($null -ne $previousTranscript) { $env:MCP_AGENT_SESSION_TRANSCRIPT_FILE = $previousTranscript } else { Remove-Item Env:\MCP_AGENT_SESSION_TRANSCRIPT_FILE -ErrorAction SilentlyContinue }
            if ($null -ne $previousExecutablePath) { $env:MCP_AGENT_EXECUTABLE_PATH = $previousExecutablePath } else { Remove-Item Env:\MCP_AGENT_EXECUTABLE_PATH -ErrorAction SilentlyContinue }
            if ($null -ne $previousExecutableVersion) { $env:MCP_AGENT_EXECUTABLE_VERSION = $previousExecutableVersion } else { Remove-Item Env:\MCP_AGENT_EXECUTABLE_VERSION -ErrorAction SilentlyContinue }
            Remove-TurnShimState
            Remove-Item -LiteralPath $sandbox.Root -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'TEST-MCP-REPL-040 persists an incidental appendActions with the turn title omitted' {
        # TR-MCP-REPL-018: an appendActions call without queryTitle persists
        # through the real code path with the title omitted from the payload.
        $sandbox = New-TurnShimSandbox
        $previousCacheOverride = $env:MCP_CACHE_DIR_OVERRIDE
        $previousFailsafeOverride = $env:MCPSERVER_FAILSAFE_DIR
        $previousFresh = $null
        $previousRaw = $null

        try {
            $env:MCP_CACHE_DIR_OVERRIDE = $sandbox.CacheDir
            $env:MCPSERVER_FAILSAFE_DIR = $sandbox.FailsafeDir
            . (Join-Path $script:LibRoot 'repl-invoke.ps1')
            $previousFresh = Get-Command Assert-ReplCurrentTurnFresh -CommandType Function -ErrorAction Stop
            $previousRaw = Get-Command Invoke-ReplRaw -CommandType Function -ErrorAction SilentlyContinue
            Set-Item -Path Function:\Assert-ReplCurrentTurnFresh -Value $script:TurnShimFreshStub
            Initialize-TurnShimCapture -SessionId $sandbox.SessionId
            Set-Item -Path Function:\Invoke-ReplRaw -Value $script:TurnShimReplRawStub
            Initialize-TurnShimCache -CacheDir $sandbox.CacheDir -RequestId 'req-20260721T000003Z-actions'

            Invoke-ReplMethod -Method 'workflow.sessionlog.appendActions' -ParamsYaml "actions:`n- type: note`n  description: incidental work`n  status: completed"

            $script:LastInvokeReplMethodSuccess | Should -BeTrue
            $script:t40Submits.Count | Should -Be 1
            $turn = Get-TurnShimSubmittedTurn -ParamsYaml $script:t40Submits[0]
            (Get-ReplObjectValue -InputObject $turn -Name 'queryTitle') | Should -BeNullOrEmpty
            [string](Get-ReplObjectValue -InputObject $turn -Name 'status') | Should -Be 'in_progress'
        } finally {
            if ($previousFresh) { Set-Item -Path Function:\Assert-ReplCurrentTurnFresh -Value $previousFresh.ScriptBlock }
            if ($previousRaw) { Set-Item -Path Function:\Invoke-ReplRaw -Value $previousRaw.ScriptBlock } else { Remove-Item Function:\Invoke-ReplRaw -ErrorAction SilentlyContinue }
            if ($null -ne $previousCacheOverride) { $env:MCP_CACHE_DIR_OVERRIDE = $previousCacheOverride } else { Remove-Item Env:\MCP_CACHE_DIR_OVERRIDE -ErrorAction SilentlyContinue }
            if ($null -ne $previousFailsafeOverride) { $env:MCPSERVER_FAILSAFE_DIR = $previousFailsafeOverride } else { Remove-Item Env:\MCPSERVER_FAILSAFE_DIR -ErrorAction SilentlyContinue }
            Remove-TurnShimState
            Remove-Item -LiteralPath $sandbox.Root -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'TEST-MCP-REPL-040 completes a turn without queryTitle instead of reporting a failed persist' {
        # TR-MCP-REPL-018 (BUG-TRIAGE-098): completeTurn without queryTitle must
        # persist status completed; today the binding failure is swallowed by
        # the completeTurn catch and the call reports failure.
        $sandbox = New-TurnShimSandbox
        $previousCacheOverride = $env:MCP_CACHE_DIR_OVERRIDE
        $previousFailsafeOverride = $env:MCPSERVER_FAILSAFE_DIR
        $previousFresh = $null
        $previousRaw = $null

        try {
            $env:MCP_CACHE_DIR_OVERRIDE = $sandbox.CacheDir
            $env:MCPSERVER_FAILSAFE_DIR = $sandbox.FailsafeDir
            . (Join-Path $script:LibRoot 'repl-invoke.ps1')
            $previousFresh = Get-Command Assert-ReplCurrentTurnFresh -CommandType Function -ErrorAction Stop
            $previousRaw = Get-Command Invoke-ReplRaw -CommandType Function -ErrorAction SilentlyContinue
            Set-Item -Path Function:\Assert-ReplCurrentTurnFresh -Value $script:TurnShimFreshStub
            Initialize-TurnShimCapture -SessionId $sandbox.SessionId
            Set-Item -Path Function:\Invoke-ReplRaw -Value $script:TurnShimReplRawStub
            Initialize-TurnShimCache -CacheDir $sandbox.CacheDir -RequestId 'req-20260721T000004Z-complete'

            Invoke-ReplMethod -Method 'workflow.sessionlog.completeTurn' -ParamsYaml 'response: All acceptance criteria satisfied.'

            $script:LastInvokeReplMethodSuccess | Should -BeTrue
            $script:t40Submits.Count | Should -Be 1
            $turn = Get-TurnShimSubmittedTurn -ParamsYaml $script:t40Submits[0]
            (Get-ReplObjectValue -InputObject $turn -Name 'queryTitle') | Should -BeNullOrEmpty
            [string](Get-ReplObjectValue -InputObject $turn -Name 'status') | Should -Be 'completed'
            [string](Get-ReplObjectValue -InputObject $turn -Name 'response') | Should -Be 'All acceptance criteria satisfied.'
        } finally {
            if ($previousFresh) { Set-Item -Path Function:\Assert-ReplCurrentTurnFresh -Value $previousFresh.ScriptBlock }
            if ($previousRaw) { Set-Item -Path Function:\Invoke-ReplRaw -Value $previousRaw.ScriptBlock } else { Remove-Item Function:\Invoke-ReplRaw -ErrorAction SilentlyContinue }
            if ($null -ne $previousCacheOverride) { $env:MCP_CACHE_DIR_OVERRIDE = $previousCacheOverride } else { Remove-Item Env:\MCP_CACHE_DIR_OVERRIDE -ErrorAction SilentlyContinue }
            if ($null -ne $previousFailsafeOverride) { $env:MCPSERVER_FAILSAFE_DIR = $previousFailsafeOverride } else { Remove-Item Env:\MCPSERVER_FAILSAFE_DIR -ErrorAction SilentlyContinue }
            Remove-TurnShimState
            Remove-Item -LiteralPath $sandbox.Root -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'TEST-MCP-REPL-040 supersede keeps a locally refined title without a server round trip' {
        # TR-MCP-REPL-018 + TR-MCP-REPL-019 (BUG-TRIAGE-086): the superseded
        # turn is persisted as canceled; a locally refined title (differs from
        # the raw prompt default) is kept verbatim and no QueryAsync fetch runs.
        $sandbox = New-TurnShimSandbox
        $previousCacheOverride = $env:MCP_CACHE_DIR_OVERRIDE
        $previousFailsafeOverride = $env:MCPSERVER_FAILSAFE_DIR
        $previousRaw = $null

        try {
            $env:MCP_CACHE_DIR_OVERRIDE = $sandbox.CacheDir
            $env:MCPSERVER_FAILSAFE_DIR = $sandbox.FailsafeDir
            . (Join-Path $script:LibRoot 'repl-invoke.ps1')
            $previousRaw = Get-Command Invoke-ReplRaw -CommandType Function -ErrorAction SilentlyContinue
            Initialize-TurnShimCapture -SessionId $sandbox.SessionId
            Set-Item -Path Function:\Invoke-ReplRaw -Value $script:TurnShimReplRawStub
            $script:t40ServerRequestId = 'req-20260721T000005Z-old'
            $script:t40ServerTitle = 'SERVER TITLE MUST NOT BE FETCHED'
            Initialize-TurnShimCache -CacheDir $sandbox.CacheDir -RequestId 'req-20260721T000005Z-old' `
                -QueryTitle 'Refined: harden the login retry path' -QueryText "Fix the login flow`nIt fails on retry."

            Invoke-ReplSupersedeCurrentTurnIfInProgress -NextRequestId 'req-20260721T000006Z-next'

            $script:t40Submits.Count | Should -Be 1
            $script:t40QueryCalls.Count | Should -Be 0
            $turn = Get-TurnShimSubmittedTurn -ParamsYaml $script:t40Submits[0]
            [string](Get-ReplObjectValue -InputObject $turn -Name 'requestId') | Should -Be 'req-20260721T000005Z-old'
            [string](Get-ReplObjectValue -InputObject $turn -Name 'status') | Should -Be 'canceled'
            [string](Get-ReplObjectValue -InputObject $turn -Name 'queryTitle') | Should -Be 'Refined: harden the login retry path'
        } finally {
            if ($previousRaw) { Set-Item -Path Function:\Invoke-ReplRaw -Value $previousRaw.ScriptBlock } else { Remove-Item Function:\Invoke-ReplRaw -ErrorAction SilentlyContinue }
            if ($null -ne $previousCacheOverride) { $env:MCP_CACHE_DIR_OVERRIDE = $previousCacheOverride } else { Remove-Item Env:\MCP_CACHE_DIR_OVERRIDE -ErrorAction SilentlyContinue }
            if ($null -ne $previousFailsafeOverride) { $env:MCPSERVER_FAILSAFE_DIR = $previousFailsafeOverride } else { Remove-Item Env:\MCPSERVER_FAILSAFE_DIR -ErrorAction SilentlyContinue }
            Remove-TurnShimState
            Remove-Item -LiteralPath $sandbox.Root -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'TEST-MCP-REPL-040 supersede prefers the refined server title over a raw prompt-derived local title' {
        # TR-MCP-REPL-019 (BUG-TRIAGE-086): when the local title equals the
        # hook's raw default (prompt first line), the supersede fetches the
        # server-side title through the client passthrough and persists it, so
        # an agent-refined title survives the supersede.
        $sandbox = New-TurnShimSandbox
        $previousCacheOverride = $env:MCP_CACHE_DIR_OVERRIDE
        $previousFailsafeOverride = $env:MCPSERVER_FAILSAFE_DIR
        $previousRaw = $null

        try {
            $env:MCP_CACHE_DIR_OVERRIDE = $sandbox.CacheDir
            $env:MCPSERVER_FAILSAFE_DIR = $sandbox.FailsafeDir
            . (Join-Path $script:LibRoot 'repl-invoke.ps1')
            $previousRaw = Get-Command Invoke-ReplRaw -CommandType Function -ErrorAction SilentlyContinue
            Initialize-TurnShimCapture -SessionId $sandbox.SessionId
            Set-Item -Path Function:\Invoke-ReplRaw -Value $script:TurnShimReplRawStub
            $script:t40ServerRequestId = 'req-20260721T000007Z-old'
            $script:t40ServerTitle = 'Refined: login flow retry hardening'
            Initialize-TurnShimCache -CacheDir $sandbox.CacheDir -RequestId 'req-20260721T000007Z-old' `
                -QueryTitle 'Fix the login flow' -QueryText "Fix the login flow`nIt fails on retry."

            Invoke-ReplSupersedeCurrentTurnIfInProgress -NextRequestId 'req-20260721T000008Z-next'

            $script:t40Submits.Count | Should -Be 1
            $script:t40QueryCalls.Count | Should -Be 1
            $turn = Get-TurnShimSubmittedTurn -ParamsYaml $script:t40Submits[0]
            [string](Get-ReplObjectValue -InputObject $turn -Name 'status') | Should -Be 'canceled'
            [string](Get-ReplObjectValue -InputObject $turn -Name 'queryTitle') | Should -Be 'Refined: login flow retry hardening'
        } finally {
            if ($previousRaw) { Set-Item -Path Function:\Invoke-ReplRaw -Value $previousRaw.ScriptBlock } else { Remove-Item Function:\Invoke-ReplRaw -ErrorAction SilentlyContinue }
            if ($null -ne $previousCacheOverride) { $env:MCP_CACHE_DIR_OVERRIDE = $previousCacheOverride } else { Remove-Item Env:\MCP_CACHE_DIR_OVERRIDE -ErrorAction SilentlyContinue }
            if ($null -ne $previousFailsafeOverride) { $env:MCPSERVER_FAILSAFE_DIR = $previousFailsafeOverride } else { Remove-Item Env:\MCPSERVER_FAILSAFE_DIR -ErrorAction SilentlyContinue }
            Remove-TurnShimState
            Remove-Item -LiteralPath $sandbox.Root -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'TEST-MCP-REPL-040 supersede omits the title when the local default is raw and the server has none' {
        # TR-MCP-REPL-019: with the literal 'User prompt' local default and no
        # server-side title, the canceled persist omits the title entirely; raw
        # prompt text is never re-sent as a title.
        $sandbox = New-TurnShimSandbox
        $previousCacheOverride = $env:MCP_CACHE_DIR_OVERRIDE
        $previousFailsafeOverride = $env:MCPSERVER_FAILSAFE_DIR
        $previousRaw = $null

        try {
            $env:MCP_CACHE_DIR_OVERRIDE = $sandbox.CacheDir
            $env:MCPSERVER_FAILSAFE_DIR = $sandbox.FailsafeDir
            . (Join-Path $script:LibRoot 'repl-invoke.ps1')
            $previousRaw = Get-Command Invoke-ReplRaw -CommandType Function -ErrorAction SilentlyContinue
            Initialize-TurnShimCapture -SessionId $sandbox.SessionId
            Set-Item -Path Function:\Invoke-ReplRaw -Value $script:TurnShimReplRawStub
            $script:t40ServerRequestId = 'req-20260721T000009Z-old'
            $script:t40ServerTitle = ''
            Initialize-TurnShimCache -CacheDir $sandbox.CacheDir -RequestId 'req-20260721T000009Z-old' `
                -QueryTitle 'User prompt' -QueryText 'User prompt'

            Invoke-ReplSupersedeCurrentTurnIfInProgress -NextRequestId 'req-20260721T000010Z-next'

            $script:t40Submits.Count | Should -Be 1
            $turn = Get-TurnShimSubmittedTurn -ParamsYaml $script:t40Submits[0]
            [string](Get-ReplObjectValue -InputObject $turn -Name 'status') | Should -Be 'canceled'
            (Get-ReplObjectValue -InputObject $turn -Name 'queryTitle') | Should -BeNullOrEmpty
        } finally {
            if ($previousRaw) { Set-Item -Path Function:\Invoke-ReplRaw -Value $previousRaw.ScriptBlock } else { Remove-Item Function:\Invoke-ReplRaw -ErrorAction SilentlyContinue }
            if ($null -ne $previousCacheOverride) { $env:MCP_CACHE_DIR_OVERRIDE = $previousCacheOverride } else { Remove-Item Env:\MCP_CACHE_DIR_OVERRIDE -ErrorAction SilentlyContinue }
            if ($null -ne $previousFailsafeOverride) { $env:MCPSERVER_FAILSAFE_DIR = $previousFailsafeOverride } else { Remove-Item Env:\MCPSERVER_FAILSAFE_DIR -ErrorAction SilentlyContinue }
            Remove-TurnShimState
            Remove-Item -LiteralPath $sandbox.Root -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'TEST-MCP-REPL-040 handles failTurn locally against cache-only state instead of falling through to the REPL' {
        # TR-MCP-REPL-020 (BUG-TRIAGE-099): workflow.sessionlog.failTurn must be
        # intercepted like appendDialog/appendActions, persist status failed
        # with the failure note, and clear current-turn.yaml. Today it falls
        # through to the REPL, which answers method_invocation_error
        # 'No active session exists' for plugin cache-only sessions.
        $sandbox = New-TurnShimSandbox
        $previousCacheOverride = $env:MCP_CACHE_DIR_OVERRIDE
        $previousFailsafeOverride = $env:MCPSERVER_FAILSAFE_DIR
        $previousFresh = $null
        $previousRaw = $null

        try {
            $env:MCP_CACHE_DIR_OVERRIDE = $sandbox.CacheDir
            $env:MCPSERVER_FAILSAFE_DIR = $sandbox.FailsafeDir
            . (Join-Path $script:LibRoot 'repl-invoke.ps1')
            $previousFresh = Get-Command Assert-ReplCurrentTurnFresh -CommandType Function -ErrorAction Stop
            $previousRaw = Get-Command Invoke-ReplRaw -CommandType Function -ErrorAction SilentlyContinue
            Set-Item -Path Function:\Assert-ReplCurrentTurnFresh -Value $script:TurnShimFreshStub
            Initialize-TurnShimCapture -SessionId $sandbox.SessionId
            Set-Item -Path Function:\Invoke-ReplRaw -Value $script:TurnShimReplRawStub
            Initialize-TurnShimCache -CacheDir $sandbox.CacheDir -RequestId 'req-20260721T000011Z-fail'

            Invoke-ReplMethod -Method 'workflow.sessionlog.failTurn' -ParamsYaml "errorMessage: Build failed with CS1591 missing XML docs`nerrorCode: build_failed"

            $script:LastInvokeReplMethodSuccess | Should -BeTrue
            $script:t40FallThrough.Count | Should -Be 0
            $script:t40Submits.Count | Should -Be 1
            $turn = Get-TurnShimSubmittedTurn -ParamsYaml $script:t40Submits[0]
            [string](Get-ReplObjectValue -InputObject $turn -Name 'requestId') | Should -Be 'req-20260721T000011Z-fail'
            [string](Get-ReplObjectValue -InputObject $turn -Name 'status') | Should -Be 'failed'
            [string](Get-ReplObjectValue -InputObject $turn -Name 'response') | Should -Match 'Build failed with CS1591 missing XML docs'
            [string](Get-ReplObjectValue -InputObject $turn -Name 'response') | Should -Match 'build_failed'
            Test-Path -LiteralPath (Join-Path $sandbox.CacheDir 'current-turn.yaml') | Should -BeFalse
        } finally {
            if ($previousFresh) { Set-Item -Path Function:\Assert-ReplCurrentTurnFresh -Value $previousFresh.ScriptBlock }
            if ($previousRaw) { Set-Item -Path Function:\Invoke-ReplRaw -Value $previousRaw.ScriptBlock } else { Remove-Item Function:\Invoke-ReplRaw -ErrorAction SilentlyContinue }
            if ($null -ne $previousCacheOverride) { $env:MCP_CACHE_DIR_OVERRIDE = $previousCacheOverride } else { Remove-Item Env:\MCP_CACHE_DIR_OVERRIDE -ErrorAction SilentlyContinue }
            if ($null -ne $previousFailsafeOverride) { $env:MCPSERVER_FAILSAFE_DIR = $previousFailsafeOverride } else { Remove-Item Env:\MCPSERVER_FAILSAFE_DIR -ErrorAction SilentlyContinue }
            Remove-TurnShimState
            Remove-Item -LiteralPath $sandbox.Root -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'TEST-MCP-REPL-040 rejects a failTurn without errorMessage locally and keeps the turn cache' {
        # TR-MCP-REPL-020: the shim enforces the REPL contract (errorMessage is
        # required) locally: no submit, no REPL fall-through, and the
        # current-turn cache stays so the turn can still be closed truthfully.
        $sandbox = New-TurnShimSandbox
        $previousCacheOverride = $env:MCP_CACHE_DIR_OVERRIDE
        $previousFailsafeOverride = $env:MCPSERVER_FAILSAFE_DIR
        $previousFresh = $null
        $previousRaw = $null

        try {
            $env:MCP_CACHE_DIR_OVERRIDE = $sandbox.CacheDir
            $env:MCPSERVER_FAILSAFE_DIR = $sandbox.FailsafeDir
            . (Join-Path $script:LibRoot 'repl-invoke.ps1')
            $previousFresh = Get-Command Assert-ReplCurrentTurnFresh -CommandType Function -ErrorAction Stop
            $previousRaw = Get-Command Invoke-ReplRaw -CommandType Function -ErrorAction SilentlyContinue
            Set-Item -Path Function:\Assert-ReplCurrentTurnFresh -Value $script:TurnShimFreshStub
            Initialize-TurnShimCapture -SessionId $sandbox.SessionId
            Set-Item -Path Function:\Invoke-ReplRaw -Value $script:TurnShimReplRawStub
            Initialize-TurnShimCache -CacheDir $sandbox.CacheDir -RequestId 'req-20260721T000012Z-fail-noreason'

            Invoke-ReplMethod -Method 'workflow.sessionlog.failTurn' -ParamsYaml ''

            $script:LastInvokeReplMethodSuccess | Should -BeFalse
            $script:t40FallThrough.Count | Should -Be 0
            $script:t40Submits.Count | Should -Be 0
            Test-Path -LiteralPath (Join-Path $sandbox.CacheDir 'current-turn.yaml') | Should -BeTrue
        } finally {
            if ($previousFresh) { Set-Item -Path Function:\Assert-ReplCurrentTurnFresh -Value $previousFresh.ScriptBlock }
            if ($previousRaw) { Set-Item -Path Function:\Invoke-ReplRaw -Value $previousRaw.ScriptBlock } else { Remove-Item Function:\Invoke-ReplRaw -ErrorAction SilentlyContinue }
            if ($null -ne $previousCacheOverride) { $env:MCP_CACHE_DIR_OVERRIDE = $previousCacheOverride } else { Remove-Item Env:\MCP_CACHE_DIR_OVERRIDE -ErrorAction SilentlyContinue }
            if ($null -ne $previousFailsafeOverride) { $env:MCPSERVER_FAILSAFE_DIR = $previousFailsafeOverride } else { Remove-Item Env:\MCPSERVER_FAILSAFE_DIR -ErrorAction SilentlyContinue }
            Remove-TurnShimState
            Remove-Item -LiteralPath $sandbox.Root -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'TEST-MCP-XAGENT-001 CompleteTurn refuses a GrokCode current-turn on a Codex session and still completes same-agent rotation' {
        $sandbox = New-TurnShimSandbox
        $previousCacheOverride = $env:MCP_CACHE_DIR_OVERRIDE
        $previousFailsafeOverride = $env:MCPSERVER_FAILSAFE_DIR
        $previousRaw = $null
        $previousAgent = $env:MCP_AGENT_NAME

        try {
            $env:MCP_CACHE_DIR_OVERRIDE = $sandbox.CacheDir
            $env:MCPSERVER_FAILSAFE_DIR = $sandbox.FailsafeDir
            $env:MCP_AGENT_NAME = 'Codex'
            . (Join-Path $script:LibRoot 'repl-invoke.ps1')
            $previousRaw = Get-Command Invoke-ReplRaw -CommandType Function -ErrorAction SilentlyContinue
            Initialize-TurnShimCapture -SessionId 'Codex-20260819T000000Z-plugin-session'
            Set-Item -Path Function:\Invoke-ReplRaw -Value $script:TurnShimReplRawStub

            Write-McpYamlObject -Path (Join-Path $sandbox.CacheDir 'session-state.yaml') -Document ([ordered]@{
                status = 'verified'
                sessionId = 'Codex-20260819T000000Z-plugin-session'
                agent = 'Codex'
            })
            Write-McpYamlObject -Path (Join-Path $sandbox.CacheDir 'current-turn.yaml') -Document ([ordered]@{
                turnRequestId = 'req-20260819T000000Z-001-foreign'
                queryTitle = 'foreign turn'
                queryText = 'foreign turn prompt'
                openedAt = '2026-08-19T00:00:01Z'
                status = 'in_progress'
                sessionId = 'GrokCode-20260819T000000Z-plugin-session'
            })

            Invoke-ReplMethod -Method 'workflow.sessionlog.completeTurn' -ParamsYaml "response: Done`n"
            $script:LastInvokeReplMethodSuccess | Should -BeFalse
            $script:t40Submits.Count | Should -Be 0

            Write-McpYamlObject -Path (Join-Path $sandbox.CacheDir 'session-state.yaml') -Document ([ordered]@{
                status = 'verified'
                sessionId = 'Codex-20260819T010000Z-rotated'
                agent = 'Codex'
            })
            Write-McpYamlObject -Path (Join-Path $sandbox.CacheDir 'current-turn.yaml') -Document ([ordered]@{
                turnRequestId = 'req-20260819T000000Z-002-same'
                queryTitle = 'same agent'
                queryText = 'same agent prompt'
                openedAt = '2026-08-19T00:00:01Z'
                status = 'in_progress'
                sessionId = 'Codex-20260819T000000Z-plugin-session'
            })

            Invoke-ReplMethod -Method 'workflow.sessionlog.completeTurn' -ParamsYaml "response: Done`n"
            $script:LastInvokeReplMethodSuccess | Should -BeTrue
            $script:t40Submits.Count | Should -Be 1
        } finally {
            if ($previousRaw) { Set-Item -Path Function:\Invoke-ReplRaw -Value $previousRaw.ScriptBlock } else { Remove-Item Function:\Invoke-ReplRaw -ErrorAction SilentlyContinue }
            if ($null -ne $previousCacheOverride) { $env:MCP_CACHE_DIR_OVERRIDE = $previousCacheOverride } else { Remove-Item Env:\MCP_CACHE_DIR_OVERRIDE -ErrorAction SilentlyContinue }
            if ($null -ne $previousFailsafeOverride) { $env:MCPSERVER_FAILSAFE_DIR = $previousFailsafeOverride } else { Remove-Item Env:\MCPSERVER_FAILSAFE_DIR -ErrorAction SilentlyContinue }
            if ($null -ne $previousAgent) { $env:MCP_AGENT_NAME = $previousAgent } else { Remove-Item Env:\MCP_AGENT_NAME -ErrorAction SilentlyContinue }
            Remove-TurnShimState
            Remove-Item -LiteralPath $sandbox.Root -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'TEST-MCP-XAGENT-001 CompleteTurn never closes a different requestId and empty title omits queryTitle' {
        $sandbox = New-TurnShimSandbox
        $previousCacheOverride = $env:MCP_CACHE_DIR_OVERRIDE
        $previousFailsafeOverride = $env:MCPSERVER_FAILSAFE_DIR
        $previousRaw = $null
        $previousFresh = $null

        try {
            $env:MCP_CACHE_DIR_OVERRIDE = $sandbox.CacheDir
            $env:MCPSERVER_FAILSAFE_DIR = $sandbox.FailsafeDir
            . (Join-Path $script:LibRoot 'repl-invoke.ps1')
            $previousFresh = Get-Command Assert-ReplCurrentTurnFresh -CommandType Function -ErrorAction Stop
            $previousRaw = Get-Command Invoke-ReplRaw -CommandType Function -ErrorAction SilentlyContinue
            Set-Item -Path Function:\Assert-ReplCurrentTurnFresh -Value $script:TurnShimFreshStub
            Initialize-TurnShimCapture -SessionId $sandbox.SessionId
            Set-Item -Path Function:\Invoke-ReplRaw -Value $script:TurnShimReplRawStub
            Initialize-TurnShimCache -CacheDir $sandbox.CacheDir -RequestId 'req-20260819T000000Z-003-keep' -QueryTitle ''

            Invoke-ReplMethod -Method 'workflow.sessionlog.completeTurn' -ParamsYaml "requestId: req-20260819T000000Z-999-other`nresponse: hijack`n"
            $script:LastInvokeReplMethodSuccess | Should -BeFalse
            $script:t40Submits.Count | Should -Be 0

            Invoke-ReplMethod -Method 'workflow.sessionlog.completeTurn' -ParamsYaml "response: Done`n"
            $script:LastInvokeReplMethodSuccess | Should -BeTrue
            $script:t40Submits.Count | Should -Be 1
            $turn = Get-TurnShimSubmittedTurn -ParamsYaml $script:t40Submits[0]
            [string](Get-ReplObjectValue -InputObject $turn -Name 'requestId') | Should -Be 'req-20260819T000000Z-003-keep'
            $title = Get-ReplObjectValue -InputObject $turn -Name 'queryTitle'
            [string]$title | Should -BeNullOrEmpty
        } finally {
            if ($previousFresh) { Set-Item -Path Function:\Assert-ReplCurrentTurnFresh -Value $previousFresh.ScriptBlock }
            if ($previousRaw) { Set-Item -Path Function:\Invoke-ReplRaw -Value $previousRaw.ScriptBlock } else { Remove-Item Function:\Invoke-ReplRaw -ErrorAction SilentlyContinue }
            if ($null -ne $previousCacheOverride) { $env:MCP_CACHE_DIR_OVERRIDE = $previousCacheOverride } else { Remove-Item Env:\MCP_CACHE_DIR_OVERRIDE -ErrorAction SilentlyContinue }
            if ($null -ne $previousFailsafeOverride) { $env:MCPSERVER_FAILSAFE_DIR = $previousFailsafeOverride } else { Remove-Item Env:\MCPSERVER_FAILSAFE_DIR -ErrorAction SilentlyContinue }
            Remove-TurnShimState
            Remove-Item -LiteralPath $sandbox.Root -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}

Describe 'TEST-MCP-STRICTCOUNT-001 updateTurn StrictMode collection Count' {
    It 'workflow.sessionlog.updateTurn omitted empty and scalar tags exit 0 with silent stdout' {
        $root = Join-Path $script:SmokeCache ('strictcount-' + [guid]::NewGuid().ToString('N'))
        $cache = Join-Path $root 'cache'
        $persistLog = Join-Path $root 'persist.jsonl'
        [void][System.IO.Directory]::CreateDirectory($cache)
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

        $cases = @(
            @{ Name = 'omitted'; Yaml = "response: ok`n" },
            @{ Name = 'empty'; Yaml = "response: ok`ntags: []`ncontextList: []`n" },
            @{ Name = 'scalar'; Yaml = "response: ok`ntags: one-tag`ncontextList: one-context`n" }
        )

        try {
            foreach ($case in $cases) {
                $result = Invoke-PluginChildProcess `
                    -ScriptPath (Join-Path $script:LibRoot 'repl-invoke.ps1') `
                    -Arguments @('-Method', 'workflow.sessionlog.updateTurn', '-ParamsYaml', $case.Yaml) `
                    -Environment @{
                        MCP_PLUGIN_ROOT = $script:LibRoot
                        MCP_PLUGIN_HOST = 'grok'
                        MCP_AGENT_NAME = 'GrokCode'
                        MCP_CACHE_DIR_OVERRIDE = $cache
                        MCP_PLUGIN_PERSIST_LOG = $persistLog
                        MCP_WORKSPACE_PATH = $script:RepoRoot
                        MCPSERVER_WORKSPACE_PATH = $script:RepoRoot
                    }

                $result.ExitCode | Should -Be 0 -Because $case.Name
                $result.Stdout | Should -Be '' -Because $case.Name
                $result.Stderr | Should -Not -Match 'Count cannot be found' -Because $case.Name
            }
        } finally {
            Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}

Describe 'TEST-MCP-SESSIONEND-001 SessionEnd unresolved cache is a silent no-op' {
    It 'session-end without a resolvable workspace exits 0 and writes {}' {
        $cwd = Join-Path $script:SmokeCache ('sessionend-empty-' + [guid]::NewGuid().ToString('N'))
        [void][System.IO.Directory]::CreateDirectory($cwd)

        try {
            $result = Invoke-PluginChildProcess `
                -ScriptPath (Join-Path $script:LibRoot 'plugin-hook.ps1') `
                -Arguments @('-HookName', 'session-end', '-HostName', 'claude-code') `
                -WorkingDirectory $cwd `
                -Environment @{
                    MCP_PLUGIN_ROOT = $script:LibRoot
                    MCP_PLUGIN_HOST = 'claude-code'
                    MCP_AGENT_NAME = 'ClaudeCode'
                    MCP_CACHE_DIR_OVERRIDE = ''
                    MCP_WORKSPACE_PATH = ''
                    MCPSERVER_WORKSPACE_PATH = ''
                    MCP_WORKSPACE_START_DIR = ''
                    CLAUDE_PROJECT_DIR = ''
                    CODEX_CWD = ''
                    CODEX_WORKSPACE_PATH = ''
                    CODEX_PROJECT_DIR = ''
                    PLUGIN_ROOT_OVERRIDE = ''
                }

            $result.ExitCode | Should -Be 0
            $result.Stdout | Should -Be '{}'
            $result.Stderr | Should -Not -Match 'Unable to resolve the active workspace cache'
        } finally {
            Remove-Item -LiteralPath $cwd -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'session-end flushes pending YAML identified by CLAUDE_PROJECT_DIR' {
        $root = Join-Path $script:SmokeCache ('sessionend-flush-' + [guid]::NewGuid().ToString('N'))
        $workspace = Join-Path $root 'workspace'
        $cwd = Join-Path $root 'cwd'
        $cacheDir = Join-Path $workspace '.mcpServer\claude'
        $pendingDir = Join-Path $cacheDir 'pending'
        $failsafeDir = Join-Path $cacheDir 'failsafe'
        [void][System.IO.Directory]::CreateDirectory($pendingDir)
        [void][System.IO.Directory]::CreateDirectory($failsafeDir)
        [void][System.IO.Directory]::CreateDirectory($cwd)
        Set-Content -LiteralPath (Join-Path $workspace 'AGENTS-README-FIRST.yaml') -Value "workspace: sessionend`n"
        $pending = Join-Path $pendingDir '001-client-Health-GetAsync.yaml'
        Set-Content -LiteralPath $pending -Value "id: `"001`"`ntimestamp: `"2026-08-19T00:00:00Z`"`nmethod: client.Health.GetAsync`nparams: {}`nretryCount: 0"
        $failsafe = Join-Path $failsafeDir '20260819T000000Z-session_submit-aaaa.yaml'
        Set-Content -LiteralPath $failsafe -Value "method: client.SessionLog.SubmitAsync`nlabel: session_submit`nparams:`n  sessionLog:`n    sessionId: ClaudeCode-20260819T000000Z-plugin-session`n"

        $stub = Join-Path $root 'flush-repl.ps1'
        Set-Content -LiteralPath $stub -Value "param([string]`$Method,[string]`$ParamsYaml='')`nexit 0`n"

        try {
            $result = Invoke-PluginChildProcess `
                -ScriptPath (Join-Path $script:LibRoot 'plugin-hook.ps1') `
                -Arguments @('-HookName', 'session-end', '-HostName', 'claude-code') `
                -WorkingDirectory $cwd `
                -Environment @{
                    MCP_PLUGIN_ROOT = $script:LibRoot
                    MCP_PLUGIN_HOST = 'claude-code'
                    MCP_AGENT_NAME = 'ClaudeCode'
                    CLAUDE_PROJECT_DIR = $workspace
                    MCP_WORKSPACE_PATH = ''
                    MCPSERVER_WORKSPACE_PATH = ''
                    MCP_CACHE_DIR_OVERRIDE = ''
                    PLUGIN_ROOT_OVERRIDE = ''
                    MCP_CACHE_FLUSH_REPL = $stub
                    MCP_FAILSAFE_DRAIN_DISABLED = '1'
                }

            $result.ExitCode | Should -Be 0
            $result.Stdout | Should -Be '{}'
            Test-Path -LiteralPath $pending | Should -BeFalse
            @(Get-ChildItem -LiteralPath $pendingDir -Filter '*.yaml' -File -ErrorAction SilentlyContinue).Count | Should -Be 0
        } finally {
            Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'session-end identified-workspace flush failure is not a silent {} success' {
        $root = Join-Path $script:SmokeCache ('sessionend-flushfail-' + [guid]::NewGuid().ToString('N'))
        $workspace = Join-Path $root 'workspace'
        $cwd = Join-Path $root 'cwd'
        $cacheDir = Join-Path $workspace '.mcpServer\claude'
        $pendingDir = Join-Path $cacheDir 'pending'
        [void][System.IO.Directory]::CreateDirectory($pendingDir)
        [void][System.IO.Directory]::CreateDirectory($cwd)
        Set-Content -LiteralPath (Join-Path $workspace 'AGENTS-README-FIRST.yaml') -Value "workspace: sessionend`n"
        $pending = Join-Path $pendingDir '001-client-DoesNotExist.yaml'
        Set-Content -LiteralPath $pending -Value "id: `"001`"`ntimestamp: `"2026-08-19T00:00:00Z`"`nmethod: client.DoesNotExist.Nope`nparams: {}`nretryCount: 0"

        $stub = Join-Path $root 'flush-repl-fail.ps1'
        Set-Content -LiteralPath $stub -Value "param([string]`$Method,[string]`$ParamsYaml='')`nthrow 'flush-replay-failed'`n"

        try {
            $result = Invoke-PluginChildProcess `
                -ScriptPath (Join-Path $script:LibRoot 'plugin-hook.ps1') `
                -Arguments @('-HookName', 'session-end', '-HostName', 'claude-code') `
                -WorkingDirectory $cwd `
                -Environment @{
                    MCP_PLUGIN_ROOT = $script:LibRoot
                    MCP_PLUGIN_HOST = 'claude-code'
                    MCP_AGENT_NAME = 'ClaudeCode'
                    CLAUDE_PROJECT_DIR = $workspace
                    MCP_WORKSPACE_PATH = ''
                    MCPSERVER_WORKSPACE_PATH = ''
                    MCP_CACHE_DIR_OVERRIDE = ''
                    PLUGIN_ROOT_OVERRIDE = ''
                    MCP_CACHE_FLUSH_REPL = $stub
                    MCP_FAILSAFE_DRAIN_DISABLED = '1'
                }

            $looksLikeUnresolvedSuccess = ($result.ExitCode -eq 0 -and $result.Stdout -eq '{}')
            $looksLikeUnresolvedSuccess | Should -BeFalse
            Test-Path -LiteralPath $pending | Should -BeTrue
        } finally {
            Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}

Describe 'TEST-MCP-VERIFYWRAP-001 code-verify disk-full and wrapper timeout' {
    It 'wrapper template documents a hard timeout for code-verify' {
        $template = [System.IO.File]::ReadAllText((Join-Path $script:RepoRoot 'plugins\core\hooks-templates\wrapper.ps1.template'))
        $template | Should -Match 'MCP_CODE_VERIFY_TIMEOUT_SECONDS'
        $template | Should -Match 'WaitForExit'
        $hook = [System.IO.File]::ReadAllText((Join-Path $script:LibRoot 'plugin-hook.ps1'))
        $hook | Should -Match 'Test-PluginDiskFullException'
        $hook | Should -Match 'Invoke-PluginBoundedProcess'
    }
}

Describe 'TEST-MCP-TRIAGEPLUGIN-004 beginTurn wrapper timeout is classified' {
    It 'Invoke-McpPlugin sessionlog timeout emits classified retryable command_timeout instead of unclassified throw' {
        $invokeSource = [System.IO.File]::ReadAllText((Join-Path $script:LibRoot 'Invoke-McpPlugin.ps1'))
        $invokeSource | Should -Not -Match 'throw "Plugin command timed out after \$\{boundedTimeout\}s\."'
        $invokeSource | Should -Match 'command_timeout'
        $invokeSource | Should -Match 'retryable'
    }
}
