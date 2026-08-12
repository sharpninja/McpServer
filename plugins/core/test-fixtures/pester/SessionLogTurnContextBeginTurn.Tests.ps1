#Requires -Version 7.0

# AC-TR-MCP-SESSIONLOG-006-006 / TEST-MCP-SESSIONLOG-006
# Drives shipped plugin-hook.ps1 Open-PluginTurn resolution through user-prompt-submit
# and the MCP_PLUGIN_REPL_LOG seam (no live server).

Describe 'MCP-SESSIONLOG-002 workflow.sessionlog.beginTurn planFile/todoId' {
    BeforeAll {
        $script:RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')).ProviderPath
        $script:HookScript = Join-Path $script:RepoRoot 'plugins\core\lib-ps\plugin-hook.ps1'
        $script:ReplScript = Join-Path $script:RepoRoot 'plugins\core\lib-ps\repl-invoke.ps1'
        $script:Work = Join-Path ([System.IO.Path]::GetTempPath()) ('mcp-plugin-beginturn-' + [guid]::NewGuid().ToString('N'))
        [void][System.IO.Directory]::CreateDirectory($script:Work)
        . $script:ReplScript

        function Invoke-WorkflowBeginTurnCapture {
            param(
                [Parameter(Mandatory)][string]$CacheDir,
                [Parameter(Mandatory)][string]$BeginParamsYaml
            )

            if (-not (Test-Path -LiteralPath $CacheDir)) {
                [void][System.IO.Directory]::CreateDirectory($CacheDir)
            }

            $session = Join-Path $CacheDir 'session-state.yaml'
            if (-not (Test-Path -LiteralPath $session)) {
                @(
                    'status: verified'
                    'sessionId: ClaudeCode-20260304T113901Z-plugin'
                    'agent: ClaudeCode'
                    'timestamp: 2026-08-12T19:00:00Z'
                    'title: plugin begin turn fixture'
                ) | Set-Content -LiteralPath $session -Encoding utf8
            }

            $priorCache = $env:MCP_CACHE_DIR_OVERRIDE
            $priorAgent = $env:MCP_AGENT_NAME
            $priorWorkspace = $env:MCP_WORKSPACE_PATH
            $priorPersistLog = $env:MCP_PLUGIN_PERSIST_LOG
            $persistLog = Join-Path $CacheDir ('persist-' + [guid]::NewGuid().ToString('N') + '.jsonl')
            $env:MCP_CACHE_DIR_OVERRIDE = $CacheDir
            $env:MCP_AGENT_NAME = 'ClaudeCode'
            $env:MCP_WORKSPACE_PATH = $script:RepoRoot
            $env:MCP_PLUGIN_PERSIST_LOG = $persistLog

            try {
                $ok = [bool](Invoke-WorkflowBeginTurn -ParamsYaml $BeginParamsYaml)
                $calls = @()
                if (Test-Path -LiteralPath $persistLog) {
                    $calls = @(Get-Content -LiteralPath $persistLog | ForEach-Object { $_ | ConvertFrom-Json })
                }
                return [pscustomobject]@{
                    Success = $ok
                    Calls = $calls
                }
            } finally {
                if ($null -eq $priorCache) { Remove-Item Env:MCP_CACHE_DIR_OVERRIDE -ErrorAction SilentlyContinue } else { $env:MCP_CACHE_DIR_OVERRIDE = $priorCache }
                if ($null -eq $priorAgent) { Remove-Item Env:MCP_AGENT_NAME -ErrorAction SilentlyContinue } else { $env:MCP_AGENT_NAME = $priorAgent }
                if ($null -eq $priorWorkspace) { Remove-Item Env:MCP_WORKSPACE_PATH -ErrorAction SilentlyContinue } else { $env:MCP_WORKSPACE_PATH = $priorWorkspace }
                if ($null -eq $priorPersistLog) { Remove-Item Env:MCP_PLUGIN_PERSIST_LOG -ErrorAction SilentlyContinue } else { $env:MCP_PLUGIN_PERSIST_LOG = $priorPersistLog }
            }
        }

        function Invoke-UserPromptSubmit {
            param(
                [Parameter(Mandatory)][string]$CacheDir,
                [string]$ToolInput = '',
                [string]$Prompt = 'Continue the plan'
            )

            $log = Join-Path $CacheDir 'repl.log'
            $session = Join-Path $CacheDir 'session-state.yaml'
            @(
                'status: verified'
                'sessionId: ClaudeCode-20260304T113901Z-plugin'
                'agent: ClaudeCode'
                'timestamp: 2026-08-12T19:00:00Z'
            ) | Set-Content -LiteralPath $session -Encoding utf8

            $psi = [System.Diagnostics.ProcessStartInfo]::new()
            $psi.FileName = (Get-Command pwsh -ErrorAction Stop).Source
            foreach ($a in @('-NoLogo', '-NoProfile', '-NonInteractive', '-File', $script:HookScript, '-HookName', 'user-prompt-submit', '-WorkspacePath', $script:RepoRoot)) {
                $psi.ArgumentList.Add($a)
            }
            $psi.WorkingDirectory = $script:RepoRoot
            $psi.UseShellExecute = $false
            $psi.RedirectStandardInput = $true
            $psi.RedirectStandardOutput = $true
            $psi.RedirectStandardError = $true
            $psi.Environment['MCP_PLUGIN_HOST'] = 'claude-code'
            $psi.Environment['MCP_AGENT_NAME'] = 'ClaudeCode'
            $psi.Environment['MCP_CACHE_DIR_OVERRIDE'] = $CacheDir
            $psi.Environment['MCP_PLUGIN_REPL_LOG'] = $log
            $psi.Environment['MCP_PLUGIN_REPL_RESPONSE'] = 'ok'
            $psi.Environment['MCP_WORKSPACE_PATH'] = $script:RepoRoot
            $psi.Environment['MCPSERVER_WORKSPACE_PATH'] = $script:RepoRoot
            if ($ToolInput) { $psi.Environment['TOOL_INPUT'] = $ToolInput }

            $p = [System.Diagnostics.Process]::Start($psi)
            $payload = (@{ prompt = $Prompt } | ConvertTo-Json -Compress)
            $p.StandardInput.Write($payload)
            $p.StandardInput.Close()
            $out = $p.StandardOutput.ReadToEndAsync()
            $err = $p.StandardError.ReadToEndAsync()
            $p.WaitForExit(60000) | Should -BeTrue
            [pscustomobject]@{
                ExitCode = $p.ExitCode
                Stdout   = $out.Result
                Stderr   = $err.Result
                Log      = if (Test-Path -LiteralPath $log) { [System.IO.File]::ReadAllText($log) } else { '' }
            }
        }
    }

    AfterAll {
        if (Test-Path -LiteralPath $script:Work) {
            Remove-Item -LiteralPath $script:Work -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'Invoke-WorkflowBeginTurn_MissingFields_FailsValidation' {
        $cache = Join-Path $script:Work ('miss-' + [guid]::NewGuid().ToString('N'))
        [void][System.IO.Directory]::CreateDirectory($cache)
        $yaml = @(
            'requestId: req-20260304T113901Z-plugin-miss'
            'queryTitle: first persist omitted fields'
            'queryText: first persist omitted fields'
        ) -join "`n"
        $captured = Invoke-WorkflowBeginTurnCapture -CacheDir $cache -BeginParamsYaml $yaml

        $captured.Success | Should -BeTrue
        $captured.Calls.Count | Should -Be 1
        $captured.Calls[0].PlanFile | Should -Be 'None'
        $captured.Calls[0].TodoId | Should -Be 'None'
        $captured.Calls[0].RequestId | Should -Be 'req-20260304T113901Z-plugin-miss'
    }

    It 'Invoke-WorkflowBeginTurn_FirstTurn_SendsNoneWhenNoPlanMap' {
        $cache = Join-Path $script:Work ('none-' + [guid]::NewGuid().ToString('N'))
        [void][System.IO.Directory]::CreateDirectory($cache)
        $r = Invoke-UserPromptSubmit -CacheDir $cache
        $r.ExitCode | Should -Be 0
        $r.Log | Should -Match 'method:\s*workflow\.sessionlog\.beginTurn'
        $r.Log | Should -Match '(?m)^\s*planFile:\s*None\s*$'
        $r.Log | Should -Match '(?m)^\s*todoId:\s*None\s*$'
    }

    It 'Invoke-WorkflowBeginTurn_FirstTurn_SendsMappedPlanAndTodo' {
        $cache = Join-Path $script:Work ('map-' + [guid]::NewGuid().ToString('N'))
        [void][System.IO.Directory]::CreateDirectory($cache)
        $plan = Join-Path $cache 'plan.md'
        [System.IO.File]::WriteAllText($plan, "# Mapped Plan`n")
        $map = @(
            'entries:'
            "  - planFile: $plan"
            '    todoId: MCP-SESSIONLOG-002'
        ) -join "`n"
        [System.IO.File]::WriteAllText((Join-Path $cache 'plan-todo-map.yaml'), $map + "`n")

        $r = Invoke-UserPromptSubmit -CacheDir $cache -ToolInput $plan
        $r.ExitCode | Should -Be 0
        $r.Log | Should -Match 'method:\s*workflow\.sessionlog\.beginTurn'
        $r.Log | Should -Match ([regex]::Escape($plan))
        $r.Log | Should -Match '(?m)^\s*todoId:\s*MCP-SESSIONLOG-002\s*$'
    }

    It 'Invoke-WorkflowBeginTurn_Reopen_OmitsFieldsAndDoesNotOverwrite' {
        $cache = Join-Path $script:Work ('reopen-' + [guid]::NewGuid().ToString('N'))
        [void][System.IO.Directory]::CreateDirectory($cache)
        $firstYaml = @(
            'requestId: req-20260304T113901Z-plugin-reopen'
            'queryTitle: first persist mapped'
            'queryText: first persist mapped'
            'planFile: docs/plans/foo.md'
            'todoId: MCP-SESSIONLOG-002'
        ) -join "`n"
        $first = Invoke-WorkflowBeginTurnCapture -CacheDir $cache -BeginParamsYaml $firstYaml
        $first.Success | Should -BeTrue
        $first.Calls.Count | Should -Be 1
        $first.Calls[0].PlanFile | Should -Be 'docs/plans/foo.md'
        $first.Calls[0].TodoId | Should -Be 'MCP-SESSIONLOG-002'
        $first.Calls[0].BoundPlanFile | Should -BeTrue
        $first.Calls[0].BoundTodoId | Should -BeTrue

        $reopenYaml = @(
            'requestId: req-20260304T113901Z-plugin-reopen'
            'queryTitle: first persist mapped'
            'queryText: first persist mapped'
        ) -join "`n"
        $reopen = Invoke-WorkflowBeginTurnCapture -CacheDir $cache -BeginParamsYaml $reopenYaml
        $reopen.Success | Should -BeTrue
        $reopen.Calls.Count | Should -Be 1
        $reopen.Calls[0].BoundPlanFile | Should -BeFalse
        $reopen.Calls[0].BoundTodoId | Should -BeFalse
        $reopen.Calls[0].PlanFile | Should -BeNullOrEmpty
        $reopen.Calls[0].TodoId | Should -BeNullOrEmpty
    }
}
