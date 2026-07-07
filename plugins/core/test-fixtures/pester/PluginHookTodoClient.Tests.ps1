#Requires -Version 7.0

# TEST-MCP-PLUGIN-CLIENTMIGRATION-001..003
#
# The plugin's TODO mutations (plan-approved / plan-modified hooks) must go through
# the client.Todo.* passthrough surface, NOT the deprecated workflow.todo.* namespace
# that the REPL stamps deprecated:true on (FR-MCP-REPL-006). The create must supply a
# non-empty id (the server requires params.id) and both calls must build params
# object-first. These behaviors were previously untested, which let workflow.todo.*
# usage and the deprecated:true leak persist.
#
# The hook's Invoke-PluginRepl honors an MCP_PLUGIN_REPL_LOG seam: it records the
# invoked method + params to that file and returns MCP_PLUGIN_REPL_RESPONSE instead of
# dispatching to a live REPL, so these tests are hermetic (no server required).

Describe 'plugin-hook TODO mutations use client.Todo.*' {
    BeforeAll {
        $script:RepoRoot   = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')).ProviderPath
        $script:LibRoot    = Join-Path $script:RepoRoot 'plugins\core\lib-ps'
        $script:HookScript = Join-Path $script:LibRoot 'plugin-hook.ps1'
        $script:Work       = Join-Path ([System.IO.Path]::GetTempPath()) ('mcp-plugin-clientmig-' + [guid]::NewGuid().ToString('N'))
        [void][System.IO.Directory]::CreateDirectory($script:Work)

        function Invoke-Hook {
            param(
                [Parameter(Mandatory)][string]$HookName,
                [Parameter(Mandatory)][hashtable]$Environment
            )
            $psi = [System.Diagnostics.ProcessStartInfo]::new()
            $psi.FileName = (Get-Command pwsh -ErrorAction Stop).Source
            foreach ($a in @('-NoLogo', '-NoProfile', '-NonInteractive', '-File', $script:HookScript, '-HookName', $HookName)) {
                $psi.ArgumentList.Add($a)
            }
            $psi.WorkingDirectory = $script:RepoRoot
            $psi.UseShellExecute = $false
            $psi.RedirectStandardInput = $true
            $psi.RedirectStandardOutput = $true
            $psi.RedirectStandardError = $true
            foreach ($k in $Environment.Keys) { $psi.Environment[$k] = [string]$Environment[$k] }
            $p = [System.Diagnostics.Process]::Start($psi)
            $p.StandardInput.Close()
            $out = $p.StandardOutput.ReadToEndAsync()
            $err = $p.StandardError.ReadToEndAsync()
            $p.WaitForExit(60000) | Should -BeTrue
            [pscustomobject]@{ ExitCode = $p.ExitCode; Stdout = $out.Result; Stderr = $err.Result }
        }

        function New-HookCase {
            $case = Join-Path $script:Work ([guid]::NewGuid().ToString('N'))
            [void][System.IO.Directory]::CreateDirectory($case)
            $plan = Join-Path $case 'plan.md'
            [System.IO.File]::WriteAllText($plan, "# Sample Plan Title`n`nBody line one.`n")
            [pscustomobject]@{ Dir = $case; Plan = $plan; Log = (Join-Path $case 'repl.log'); Cache = $case }
        }

        function New-HookEnv {
            param($Case, [string]$Response = '')
            @{
                MCP_PLUGIN_HOST         = 'claude-code'
                MCP_AGENT_NAME          = 'ClaudeCode'
                MCP_CACHE_DIR_OVERRIDE  = $Case.Cache
                TOOL_INPUT              = $Case.Plan
                MCP_PLUGIN_REPL_LOG     = $Case.Log
                MCP_PLUGIN_REPL_RESPONSE = $Response
                MCP_WORKSPACE_PATH      = $script:RepoRoot
                MCPSERVER_WORKSPACE_PATH = $script:RepoRoot
            }
        }
    }

    AfterAll {
        Remove-Item -LiteralPath $script:Work -Recurse -Force -ErrorAction SilentlyContinue
    }

    It 'TEST-MCP-PLUGIN-CLIENTMIGRATION-001 plan-approved creates via client.Todo.CreateAsync with a non-empty id' {
        $case = New-HookCase
        $resp = "type: result`npayload:`n  result:`n    item:`n      id: PLAN-SAMPLEPLANTITLE-001"
        $r = Invoke-Hook -HookName 'plan-approved' -Environment (New-HookEnv -Case $case -Response $resp)

        $r.ExitCode | Should -Be 0
        Test-Path -LiteralPath $case.Log | Should -BeTrue
        $log = [System.IO.File]::ReadAllText($case.Log)
        $log | Should -Match 'method:\s*client\.Todo\.CreateAsync'
        $log | Should -Not -Match 'workflow\.todo\.create'
        # object-first request DTO carrying a non-empty id + the plan title
        $log | Should -Match 'request:'
        $log | Should -Match '(?m)^\s*id:\s*\S+'
        $log | Should -Match 'Sample Plan Title'
        # the server rejects create without a valid priority (high|medium|low)
        $log | Should -Match 'priority:\s*(high|medium|low)'
    }

    It 'TEST-MCP-PLUGIN-CLIENTMIGRATION-002 plan-modified updates via client.Todo.UpdateAsync with id + doneSummary' {
        $case = New-HookCase
        $map = @(
            'entries:'
            "  - planFile: $($case.Plan)"
            '    todoId: PLAN-SAMPLEPLANTITLE-001'
        ) -join "`n"
        [System.IO.File]::WriteAllText((Join-Path $case.Cache 'plan-todo-map.yaml'), $map + "`n")

        $r = Invoke-Hook -HookName 'plan-modified' -Environment (New-HookEnv -Case $case)

        $r.ExitCode | Should -Be 0
        Test-Path -LiteralPath $case.Log | Should -BeTrue
        $log = [System.IO.File]::ReadAllText($case.Log)
        $log | Should -Match 'method:\s*client\.Todo\.UpdateAsync'
        $log | Should -Not -Match 'workflow\.todo\.update'
        $log | Should -Match '(?m)^\s*id:\s*PLAN-SAMPLEPLANTITLE-001'
        $log | Should -Match 'request:'
        $log | Should -Match 'doneSummary'
    }

    It 'TEST-MCP-PLUGIN-CLIENTMIGRATION-003 plugin-hook source has no deprecated workflow.todo.* calls' {
        $src = [System.IO.File]::ReadAllText($script:HookScript)
        $src | Should -Not -Match 'workflow\.todo\.'
        $src | Should -Match 'client\.Todo\.CreateAsync'
        $src | Should -Match 'client\.Todo\.UpdateAsync'
    }
}
