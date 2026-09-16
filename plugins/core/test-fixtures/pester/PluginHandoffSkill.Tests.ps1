#Requires -Version 7.0

# TEST-HANDOFF-006 / FR-HANDOFF-007 / TR-HANDOFF-SURFACE-001
#
# Remaining D1 gap: skill-file invoke of workflow.handoff.ingest/get/approve.
# PluginSync_HandoffSkill_MatchesCoreArtifact is checksum-only.
# PluginSkillWorkflow_InvokesTypedClientHandoffEndpoints is C# dispatcher invoke.
# This Pester drives the core and Grok plugin SKILL.md files through a skill-local
# invoke script (hermetic MCP_PLUGIN_REPL_LOG seam; no live :7147).

Describe 'TEST-HANDOFF-006 plugin handoff skill-file invoke' {
    BeforeAll {
        $script:RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')).ProviderPath
        $script:CoreSkill = Join-Path $script:RepoRoot 'plugins\core\skills\handoff\SKILL.md'
        $script:GrokSkill = Join-Path (Split-Path -Parent $script:RepoRoot) 'mcpserver-grok-plugin\skills\handoff\SKILL.md'
        $script:RequiredMethods = @(
            'workflow.handoff.ingest',
            'workflow.handoff.get',
            'workflow.handoff.approve'
        )

        function Get-DocumentedHandoffMethods {
            param([Parameter(Mandatory)][string]$SkillText)
            [regex]::Matches($SkillText, 'method:\s*(workflow\.handoff\.(?:ingest|get|approve))') |
                ForEach-Object { $_.Groups[1].Value } |
                Select-Object -Unique
        }
    }

    It 'PluginHandoffSkill_InvokeIngestGetApprove_UsesDocumentedWorkflowMethods' {
        foreach ($skillPath in @($script:CoreSkill, $script:GrokSkill)) {
            Test-Path -LiteralPath $skillPath | Should -BeTrue -Because "handoff SKILL.md must exist at $skillPath"
            $text = [System.IO.File]::ReadAllText($skillPath)
            $documented = @(Get-DocumentedHandoffMethods -SkillText $text)
            foreach ($method in $script:RequiredMethods) {
                $documented | Should -Contain $method -Because "$skillPath must document $method"
            }

            $invokePath = Join-Path (Split-Path -Parent $skillPath) 'invoke.ps1'
            Test-Path -LiteralPath $invokePath | Should -BeTrue -Because "TEST-HANDOFF-006 remaining gap is skill-file invoke beside $skillPath, not checksum or C# dispatch"

            $work = Join-Path ([System.IO.Path]::GetTempPath()) ('handoff-skill-invoke-' + [guid]::NewGuid().ToString('N'))
            [void][System.IO.Directory]::CreateDirectory($work)
            $log = Join-Path $work 'repl.log'
            $priorLog = $env:MCP_PLUGIN_REPL_LOG
            $priorResponse = $env:MCP_PLUGIN_REPL_RESPONSE
            try {
                $env:MCP_PLUGIN_REPL_LOG = $log
                $env:MCP_PLUGIN_REPL_RESPONSE = "type: result`npayload:`n  result:`n    success: true"
                $psi = [System.Diagnostics.ProcessStartInfo]::new()
                $psi.FileName = (Get-Command pwsh -ErrorAction Stop).Source
                foreach ($argument in @('-NoLogo', '-NoProfile', '-NonInteractive', '-File', $invokePath, '-SkillPath', $skillPath)) {
                    $psi.ArgumentList.Add($argument)
                }
                $psi.WorkingDirectory = $script:RepoRoot
                $psi.UseShellExecute = $false
                $psi.RedirectStandardInput = $true
                $psi.RedirectStandardOutput = $true
                $psi.RedirectStandardError = $true
                $psi.Environment['MCP_PLUGIN_REPL_LOG'] = $log
                $psi.Environment['MCP_PLUGIN_REPL_RESPONSE'] = $env:MCP_PLUGIN_REPL_RESPONSE
                $psi.Environment['MCP_WORKSPACE_PATH'] = $script:RepoRoot
                $psi.Environment['MCPSERVER_WORKSPACE_PATH'] = $script:RepoRoot
                $process = [System.Diagnostics.Process]::Start($psi)
                $process.StandardInput.Close()
                $stdout = $process.StandardOutput.ReadToEndAsync()
                $stderr = $process.StandardError.ReadToEndAsync()
                $process.WaitForExit(60000) | Should -BeTrue
                $process.ExitCode | Should -Be 0 -Because "invoke.ps1 stdout=$($stdout.Result) stderr=$($stderr.Result)"
                Test-Path -LiteralPath $log | Should -BeTrue -Because 'skill-file invoke must record workflow.handoff methods on the plugin REPL seam'
                $logged = [System.IO.File]::ReadAllText($log)
                foreach ($method in $script:RequiredMethods) {
                    $logged | Should -Match ([regex]::Escape($method)) -Because "invoke.ps1 for $skillPath must invoke $method"
                }
            }
            finally {
                if ($null -ne $priorLog) { $env:MCP_PLUGIN_REPL_LOG = $priorLog } else { Remove-Item Env:\MCP_PLUGIN_REPL_LOG -ErrorAction SilentlyContinue }
                if ($null -ne $priorResponse) { $env:MCP_PLUGIN_REPL_RESPONSE = $priorResponse } else { Remove-Item Env:\MCP_PLUGIN_REPL_RESPONSE -ErrorAction SilentlyContinue }
                Remove-Item -LiteralPath $work -Recurse -Force -ErrorAction SilentlyContinue
            }
        }
    }
}
