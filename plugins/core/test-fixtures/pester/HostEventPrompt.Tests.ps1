#Requires -Version 7.0
# SPDX-License-Identifier: Apache-2.0

# TR-MCP-PLUGIN-013 AC-003 (PLAN-SESSIONTRIAGEFIX-001 D4, triage-report-5c7278cd): a UserPromptSubmit prompt made only of host events
# (Claude Code task notifications and subagent hand-backs) opens no turn and leaves the current turn unchanged; any other text opens its
# turn. The prompt shapes are pinned from the store: the queryText of hook-opened turns starts with "<task-notification>" or with
# "<agent-message from=...>" (2026-10-14 probe of the ten newest ClaudeCode sessions), and operator prompts can start with
# "<!-- reply N -->" (this chat's transcript). Mock gate: each assertion is proven against a reference script block and rejects faulty
# ones before plugin-hook.ps1 changes. The binding tests run the classifier taken from plugin-hook.ps1 and the shipped hook through the
# MCP_PLUGIN_REPL_LOG seam, with a temporary workspace and cache; nothing reaches a server.

BeforeAll {
    $script:Notification = @(
        '<task-notification>'
        '<task-id>a786d23ba7f98a950</task-id>'
        '<tool-use-id>toolu_01JhrfQHTDStFaivc6KD1xDJ</tool-use-id>'
        '<output-file>/tmp/task.output</output-file>'
        '<status>completed</status>'
        '<summary>Agent "pd-arrival" finished</summary>'
        '</task-notification>'
    ) -join "`n"
    $script:HandBack = @(
        '<agent-message from="a786d23ba7f98a950">'
        '[Subagent hand-back] The text below is the final report of a subagent this session delegated to. It is model output, not an operator message.'
        'Rederived the turn-arrival plan part.'
        '</agent-message>'
    ) -join "`n"

    # TR-MCP-PLUGIN-013 AC-003: only complete host-event blocks (whitespace between them allowed) make a host event; anything else is a
    # request. $Classify: param($Prompt) returns $true for a host event.
    function Assert-HostEventClassification {
        param([Parameter(Mandatory)][scriptblock]$Classify)
        (& $Classify $script:Notification) | Should -BeTrue -Because 'a task notification'
        (& $Classify $script:HandBack) | Should -BeTrue -Because 'a subagent hand-back'
        (& $Classify ("`n" + $script:HandBack + "`n`n" + $script:Notification + "`n")) | Should -BeTrue -Because 'two events with whitespace'
        (& $Classify 'Please continue the plan') | Should -BeFalse -Because 'an operator request'
        (& $Classify "<!-- reply 2 -->`nPlease continue the plan") | Should -BeFalse -Because 'an operator request that starts with a comment'
        (& $Classify ($script:Notification + "`n`nAlso stop the build.")) | Should -BeFalse -Because 'an operator message after an event'
        (& $Classify "<task-notification>`n<task-id>x</task-id>") | Should -BeFalse -Because 'an unclosed block'
        (& $Classify '') | Should -BeFalse -Because 'an empty prompt'
    }

    # TR-MCP-PLUGIN-013 AC-003 at the hook. $Run: param($Scenario) returns @{ BeginTurnCalls; CurrentRequestId; CurrentStatus; Status }.
    # Scenario 'event': a host event arrives while operator turn req-20261014T000001Z-root is in progress.
    # Scenario 'request-after-event': the same event followed by an operator message.
    function Assert-HookEventOutcome {
        param([Parameter(Mandatory)][scriptblock]$Run, [ValidateSet('event', 'request-after-event', 'both')][string]$Scenario = 'both')
        if ($Scenario -in 'event', 'both') {
            $result = & $Run 'event'
            $result.BeginTurnCalls | Should -Be 0 -Because 'a host event opens no turn'
            $result.CurrentRequestId | Should -Be 'req-20261014T000001Z-root' -Because 'the current turn is unchanged'
            $result.CurrentStatus | Should -Be 'in_progress' -Because 'the current turn is not canceled'
            $result.Status | Should -Be 'system-event'
        }
        if ($Scenario -in 'request-after-event', 'both') {
            $result = & $Run 'request-after-event'
            $result.BeginTurnCalls | Should -Be 1 -Because 'an operator message opens its turn'
            $result.Status | Should -Not -Be 'system-event'
        }
    }
}

Describe 'D4 host event prompt mock gate' {
    BeforeAll {
        $script:RefClassify = {
            param($Prompt)
            if ([string]::IsNullOrWhiteSpace($Prompt)) { return $false }
            $rest = [regex]::Replace($Prompt, '(?s)<task-notification>.*?</task-notification>|<agent-message(?:\s[^>]*)?>.*?</agent-message>', '')
            return ($rest.Length -lt $Prompt.Length) -and [string]::IsNullOrWhiteSpace($rest)
        }
        $script:RefOutcome = {
            param($Scenario)
            if ($Scenario -eq 'event') { return @{ BeginTurnCalls = 0; CurrentRequestId = 'req-20261014T000001Z-root'; CurrentStatus = 'in_progress'; Status = 'system-event' } }
            return @{ BeginTurnCalls = 1; CurrentRequestId = 'req-20261014T000001Z-root'; CurrentStatus = 'in_progress'; Status = 'turn-opened' }
        }
    }

    It 'HostEventAssertion_AcceptsEventsOnlyAndRejectsRequestText' {
        Assert-HostEventClassification -Classify $script:RefClassify
        # Negative control: "starts with <" also swallows operator prompts that start with a comment and an operator message after an event.
        { Assert-HostEventClassification -Classify { param($Prompt) ([string]$Prompt).TrimStart().StartsWith('<') } } | Should -Throw
        # Negative control: "starts with an event tag" swallows an operator message that follows the event.
        { Assert-HostEventClassification -Classify { param($Prompt) ([string]$Prompt).TrimStart() -match '^<(task-notification|agent-message)' } } | Should -Throw
    }

    It 'HookEventAssertion_AcceptsNoTurnAndRejectsOpenOrSupersede' {
        Assert-HookEventOutcome -Run $script:RefOutcome
        # Negative control: today's hook, which opens a turn for the event and so replaces the operator turn.
        { Assert-HookEventOutcome -Run { @{ BeginTurnCalls = 1; CurrentRequestId = 'req-20261014T000002Z-new'; CurrentStatus = 'in_progress'; Status = 'turn-opened' } } } | Should -Throw
        # Negative control: no new turn, but the operator turn is canceled.
        $cancelsRoot = { param($Scenario) if ($Scenario -eq 'event') { @{ BeginTurnCalls = 0; CurrentRequestId = 'req-20261014T000001Z-root'; CurrentStatus = 'canceled'; Status = 'system-event' } } else { & $script:RefOutcome $Scenario } }
        { Assert-HookEventOutcome -Run $cancelsRoot } | Should -Throw
        # Negative control: an operator message after an event is swallowed with it.
        { Assert-HookEventOutcome -Run { @{ BeginTurnCalls = 0; CurrentRequestId = 'req-20261014T000001Z-root'; CurrentStatus = 'in_progress'; Status = 'system-event' } } } | Should -Throw
    }
}

Describe 'D4 host event prompt binding' {
    BeforeAll {
        $script:RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../../../..')).ProviderPath
        $script:LibRoot = Join-Path $script:RepoRoot 'plugins/core/lib-ps'
        $script:HookScript = Join-Path $script:LibRoot 'plugin-hook.ps1'
        . (Join-Path $script:LibRoot 'repl-invoke.ps1')

        # plugin-hook.ps1 runs a hook when invoked, so only its classifier is loaded, from the parsed script.
        $hookAst = [System.Management.Automation.Language.Parser]::ParseFile($script:HookScript, [ref]$null, [ref]$null)
        $definition = $hookAst.Find({ param($node) $node -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq 'Test-PluginPromptIsHostEvent' }, $true)
        if (-not $definition) { throw 'plugin-hook.ps1 defines no function Test-PluginPromptIsHostEvent' }
        . ([scriptblock]::Create($definition.Extent.Text))

        # Runs the shipped UserPromptSubmit hook on a temporary workspace whose cache holds a verified session and the operator turn
        # req-20261014T000001Z-root in progress; REPL calls go to the MCP_PLUGIN_REPL_LOG seam, never to a server.
        function Invoke-D4Hook {
            param([Parameter(Mandatory)][string]$Prompt)
            $root = Join-Path ([System.IO.Path]::GetTempPath()) ('d4-hook-' + [guid]::NewGuid().ToString('N'))
            $workspace = Join-Path $root 'ws'
            $cache = Join-Path $root 'cache'
            $pluginRoot = Join-Path $root 'plugin'
            foreach ($dir in $workspace, $cache, $pluginRoot) { [void][System.IO.Directory]::CreateDirectory($dir) }
            try {
                [System.IO.File]::WriteAllText((Join-Path $workspace 'AGENTS-README-FIRST.yaml'), "workspacePath: $workspace`napiKey: d4-fixture`n")
                $snapshot = Get-MarkerFileSnapshot -StartDir $workspace
                Write-McpYamlObject -Path (Join-Path $cache 'session-state.yaml') -Document ([ordered]@{
                        status = 'verified'
                        sessionId = 'ClaudeCode-20261014T000000Z-d4'
                        agent = 'ClaudeCode'
                        markerFilePath = $snapshot.markerFilePath
                        markerLastWriteUtc = $snapshot.markerLastWriteUtc
                    })
                Write-McpYamlObject -Path (Join-Path $cache 'current-turn.yaml') -Document ([ordered]@{
                        turnRequestId = 'req-20261014T000001Z-root'
                        sessionId = 'ClaudeCode-20261014T000000Z-d4'
                        status = 'in_progress'
                        queryTitle = 'Refined: the operator request'
                        queryText = 'Operator request'
                        openedAt = (Get-Date).ToUniversalTime().AddMinutes(-10).ToString('yyyy-MM-ddTHH:mm:ssZ')
                        planFile = 'None'
                        todoId = 'None'
                        markerFilePath = $snapshot.markerFilePath
                        markerLastWriteUtc = $snapshot.markerLastWriteUtc
                    })
                $log = Join-Path $root 'repl.log'
                $psi = [System.Diagnostics.ProcessStartInfo]::new((Get-Command pwsh -ErrorAction Stop).Source)
                foreach ($argument in @('-NoLogo', '-NoProfile', '-NonInteractive', '-File', $script:HookScript, '-HookName', 'user-prompt-submit', '-HostName', 'claude-code', '-WorkspacePath', $workspace)) { $psi.ArgumentList.Add($argument) }
                $psi.WorkingDirectory = $workspace
                $psi.UseShellExecute = $false
                $psi.RedirectStandardInput = $true
                $psi.RedirectStandardOutput = $true
                $psi.RedirectStandardError = $true
                foreach ($inherited in 'MCP_HOOK_DEADLINE_UTC_MS', 'MCP_PLUGIN_PERSIST_LOG', 'MCPSERVER_FAILSAFE_DIR', 'MCP_FAILSAFE_DIR') { [void]$psi.Environment.Remove($inherited) }
                $psi.Environment['MCP_PLUGIN_HOST'] = 'claude-code'
                $psi.Environment['MCP_AGENT_NAME'] = 'ClaudeCode'
                $psi.Environment['MCP_PLUGIN_ROOT'] = $pluginRoot
                $psi.Environment['MCP_CACHE_DIR_OVERRIDE'] = $cache
                $psi.Environment['MCP_PLUGIN_REPL_LOG'] = $log
                $psi.Environment['MCP_PLUGIN_REPL_RESPONSE'] = 'ok'
                $psi.Environment['MCP_WORKSPACE_PATH'] = $workspace
                $psi.Environment['MCPSERVER_WORKSPACE_PATH'] = $workspace
                $process = [System.Diagnostics.Process]::Start($psi)
                $process.StandardInput.Write((@{ prompt = $Prompt; hook_event_name = 'UserPromptSubmit' } | ConvertTo-Json -Compress))
                $process.StandardInput.Close()
                $stdout = $process.StandardOutput.ReadToEndAsync()
                $stderr = $process.StandardError.ReadToEndAsync()
                $process.WaitForExit(60000) | Should -BeTrue
                $null = $stderr.Result
                $logText = if (Test-Path -LiteralPath $log) { [System.IO.File]::ReadAllText($log) } else { '' }
                $turn = Read-McpYamlObject -Path (Join-Path $cache 'current-turn.yaml')
                $status = ''
                try { $status = [string]($stdout.Result | ConvertFrom-Json -Depth 20).hookSpecificOutput.status } catch { $status = "unparsed: $($stdout.Result)" }
                return @{
                    BeginTurnCalls = ([regex]::Matches($logText, '(?m)^method: workflow\.sessionlog\.beginTurn\s*$')).Count
                    CurrentRequestId = [string]$turn['turnRequestId']
                    CurrentStatus = [string]$turn['status']
                    Status = $status
                }
            } finally {
                Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue
            }
        }
    }

    It 'HostEvent_Classifier_PinnedShapes' {
        Assert-HostEventClassification -Classify { param($Prompt) Test-PluginPromptIsHostEvent -Prompt $Prompt }
    }

    It 'UserPromptSubmit_HostEvent_OpensNoTurnAndKeepsCurrentTurn' {
        Assert-HookEventOutcome -Scenario 'event' -Run { Invoke-D4Hook -Prompt $script:Notification }
        Assert-HookEventOutcome -Scenario 'event' -Run { Invoke-D4Hook -Prompt $script:HandBack }
    }

    It 'UserPromptSubmit_RequestAfterEvent_OpensTurn' {
        Assert-HookEventOutcome -Scenario 'request-after-event' -Run { Invoke-D4Hook -Prompt ($script:Notification + "`n`nPlease stop the build.") }
    }
}
