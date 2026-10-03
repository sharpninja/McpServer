#Requires -Version 7.0

# TEST-MCP-SESSIONLIFE-002 / TEST-MCP-SESSIONLIFE-005
# FR-MCP-SESSIONLIFE-004 AC001 and FR-MCP-SESSIONLIFE-005 AC001-AC003.
# P3 red slice: Stop enforcement and bounded child execution.
# Production behavior is unchanged. SessionLogClient.GetAsync is not added.
# Authoritative proof is supplied on the existing plugin bridge
# (MCP_PLUGIN_REPL_RESPONSE) for the future exact-turn read. This file does
# not edit requirements, generated YAML, or failsafe stores in the repo.

Describe 'P3 Stop enforcement' {
    BeforeAll {
        $script:RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')).ProviderPath
        $script:HookScript = Join-Path $script:RepoRoot 'plugins\core\lib-ps\plugin-hook.ps1'
        $script:Work = Join-Path ([System.IO.Path]::GetTempPath()) ('sessionlife-p3-stop-' + [guid]::NewGuid().ToString('N'))
        [void][System.IO.Directory]::CreateDirectory($script:Work)
        $script:ModuleRoot = Join-Path $script:Work 'modules'
        $moduleDir = Join-Path $script:ModuleRoot 'PowerShell.MCP'
        [void][System.IO.Directory]::CreateDirectory($moduleDir)
        @'
@{
    RootModule = 'PowerShell.MCP.psm1'
    ModuleVersion = '1.14.0'
    GUID = '8f4e2a1c-6b7d-4e9a-9c31-0a6f5d4e3b21'
    FunctionsToExport = @()
    CmdletsToExport = @()
    VariablesToExport = @()
    AliasesToExport = @()
}
'@ | Set-Content -LiteralPath (Join-Path $moduleDir 'PowerShell.MCP.psd1') -Encoding utf8NoBOM
        '# P3 test stub. Stop-gate only imports the module; it does not call it.' | Set-Content -LiteralPath (Join-Path $moduleDir 'PowerShell.MCP.psm1') -Encoding utf8NoBOM
        . (Join-Path $script:RepoRoot 'plugins\core\lib-ps\yaml-object-mutation.ps1')
        . (Join-Path $script:RepoRoot 'plugins\core\lib-ps\marker-resolver.ps1')
        Import-McpYamlSerializer

        function New-P3StopLayout {
            param([Parameter(Mandatory)][string]$Name)
            $root = Join-Path $script:Work $Name
            $workspace = Join-Path $root 'ws'
            $cache = Join-Path $root 'cache'
            [void][System.IO.Directory]::CreateDirectory($workspace)
            [void][System.IO.Directory]::CreateDirectory($cache)
            $marker = Join-Path $workspace 'AGENTS-README-FIRST.yaml'
            [System.IO.File]::WriteAllText($marker, "workspacePath: $workspace`napiKey: p3-stop`n")
            $snapshot = Get-MarkerFileSnapshot -StartDir $workspace
            $sessionId = 'Codex-20261003T100000Z-p3'
            return [pscustomobject]@{
                Root = $root
                Workspace = $workspace
                Cache = $cache
                SessionId = $sessionId
                Snapshot = $snapshot
                ReplLog = Join-Path $cache 'plugin-repl.log'
            }
        }

        function Write-P3Session {
            param(
                $Layout,
                [string]$Timestamp,
                [string]$LastUpdated,
                [hashtable]$Extra = @{}
            )
            $document = [ordered]@{
                status = 'verified'
                sessionId = $Layout.SessionId
                agent = 'Codex'
                workspacePath = $Layout.Workspace
                markerFilePath = [string]$Layout.Snapshot.markerFilePath
                markerLastWriteUtc = [string]$Layout.Snapshot.markerLastWriteUtc
            }
            if ($PSBoundParameters.ContainsKey('Timestamp')) { $document['timestamp'] = $Timestamp }
            if ($PSBoundParameters.ContainsKey('LastUpdated')) { $document['lastUpdated'] = $LastUpdated }
            foreach ($key in $Extra.Keys) { $document[$key] = $Extra[$key] }
            Write-McpYamlObject -Path (Join-Path $Layout.Cache 'session-state.yaml') -Document $document
        }

        function Write-P3Turn {
            param(
                $Layout,
                [Parameter(Mandatory)][string]$RequestId,
                [Parameter(Mandatory)][string]$Status,
                [int]$CodeEdits = 0,
                [string]$LastBuildStatus = '',
                [hashtable]$Audit = @{ auditActions = 2; auditFiles = 0; auditDialog = 0; auditDecisions = 0; auditCommits = 0 }
            )
            $document = [ordered]@{
                turnRequestId = $RequestId
                sessionId = $Layout.SessionId
                status = $Status
                queryText = 'real stop-gate work'
                queryTitle = 'P3 stop'
                codeEdits = $CodeEdits
                markerFilePath = [string]$Layout.Snapshot.markerFilePath
                markerLastWriteUtc = [string]$Layout.Snapshot.markerLastWriteUtc
            }
            if (-not [string]::IsNullOrWhiteSpace($LastBuildStatus)) { $document['lastBuildStatus'] = $LastBuildStatus }
            foreach ($key in $Audit.Keys) { $document[$key] = $Audit[$key] }
            Write-McpYamlObject -Path (Join-Path $Layout.Cache 'current-turn.yaml') -Document $document
        }

        function Write-P3Proof {
            param(
                $Layout,
                [Parameter(Mandatory)][string]$Outcome,
                [Parameter(Mandatory)][string]$RequestId,
                [string]$WorkspacePath = '',
                [string]$ServerStatus = ''
            )
            if ([string]::IsNullOrWhiteSpace($WorkspacePath)) { $WorkspacePath = $Layout.Workspace }
            if ([string]::IsNullOrWhiteSpace($ServerStatus)) {
                $ServerStatus = switch ($Outcome) {
                    'Completed' { 'completed' }
                    'Active' { 'in_progress' }
                    default { '' }
                }
            }
            $proof = [ordered]@{
                type = 'result'
                payload = [ordered]@{
                    result = [ordered]@{
                        outcome = $Outcome
                        workspacePath = $WorkspacePath
                        sourceType = 'Codex'
                        sessionId = $Layout.SessionId
                        requestId = $RequestId
                        serverStatus = $ServerStatus
                        observedAtUtc = '2026-10-03T10:00:00.0000000Z'
                    }
                }
            }
            $path = Join-Path $Layout.Cache 'authoritative-turn-proof.yaml'
            Write-McpYamlObject -Path $path -Document $proof
            return (ConvertTo-Yaml -Data $proof -Options WithIndentedSequences)
        }

        function Invoke-P3StopGate {
            param($Layout, [Parameter(Mandatory)][string]$ProofYaml)
            $psi = [System.Diagnostics.ProcessStartInfo]::new()
            $psi.FileName = (Get-Command pwsh -ErrorAction Stop).Source
            foreach ($arg in @(
                    '-NoLogo', '-NoProfile', '-NonInteractive', '-File', $script:HookScript,
                    '-HookName', 'stop-gate', '-HostName', 'codex', '-WorkspacePath', $Layout.Workspace)) {
                $psi.ArgumentList.Add($arg)
            }
            $psi.WorkingDirectory = $Layout.Workspace
            $psi.UseShellExecute = $false
            $psi.RedirectStandardOutput = $true
            $psi.RedirectStandardError = $true
            foreach ($key in @($psi.Environment.Keys)) {
                if ($key -match '^(MCP_|MCPSERVER_|CODEX_|CLAUDE_|GROK_|PLUGIN_|REPL_|P2_|P3_|CT2R_)') {
                    [void]$psi.Environment.Remove($key)
                }
            }
            $psi.Environment['MCP_CACHE_DIR_OVERRIDE'] = $Layout.Cache
            $psi.Environment['MCP_WORKSPACE_PATH'] = $Layout.Workspace
            $psi.Environment['MCPSERVER_WORKSPACE_PATH'] = $Layout.Workspace
            $psi.Environment['MCP_PLUGIN_HOST'] = 'codex'
            $psi.Environment['MCP_PLUGIN_REPL_LOG'] = $Layout.ReplLog
            $psi.Environment['MCP_PLUGIN_REPL_RESPONSE'] = $ProofYaml
            $existingModulePath = [System.Environment]::GetEnvironmentVariable('PSModulePath')
            $psi.Environment['PSModulePath'] = $script:ModuleRoot + [System.IO.Path]::PathSeparator + $existingModulePath
            $proc = [System.Diagnostics.Process]::Start($psi)
            $stdout = $proc.StandardOutput.ReadToEndAsync()
            $stderr = $proc.StandardError.ReadToEndAsync()
            if (-not $proc.WaitForExit(20000)) {
                try { $proc.Kill($true) } catch { }
                throw "stop-gate hung past 20s. stderr=$($stderr.Result)"
            }
            $text = $stdout.Result.Trim()
            if ($text -match 'MCP_PLUGIN_UNAVAILABLE') {
                throw "stop-gate did not reach Close-PluginTurnIfNeeded. stdout=$text stderr=$($stderr.Result)"
            }
            $jsonLine = @($text -split "`r?`n" | Where-Object { $_.Trim().StartsWith('{') } | Select-Object -Last 1)
            $parsed = $null
            if ($jsonLine.Count -gt 0 -and -not [string]::IsNullOrWhiteSpace([string]$jsonLine[-1])) {
                $parsed = [string]$jsonLine[-1] | ConvertFrom-Json
            }
            $log = ''
            if (Test-Path -LiteralPath $Layout.ReplLog) {
                $log = [System.IO.File]::ReadAllText($Layout.ReplLog)
            }
            [pscustomobject]@{
                ExitCode = $proc.ExitCode
                Stdout = $text
                Stderr = $stderr.Result.Trim()
                Parsed = $parsed
                ReplLog = $log
            }
        }

        function Read-P3Yaml {
            param([Parameter(Mandatory)][string]$Path)
            if (-not (Test-Path -LiteralPath $Path)) { return $null }
            return (Read-McpYamlObject -Path $Path)
        }
    }

    AfterAll {
        if ($script:Work -and (Test-Path -LiteralPath $script:Work)) {
            Remove-Item -LiteralPath $script:Work -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'P3 authoritative completed A does not clear still-active B' {
        $layout = New-P3StopLayout -Name 'completed-a-active-b'
        $old = (Get-Date).ToUniversalTime().AddHours(-30).ToString('o')
        Write-P3Session -Layout $layout -Timestamp $old -LastUpdated $old
        Write-P3Turn -Layout $layout -RequestId 'req-b-active' -Status 'in_progress'
        $beforeSession = Read-P3Yaml -Path (Join-Path $layout.Cache 'session-state.yaml')
        $proof = Write-P3Proof -Layout $layout -Outcome 'Completed' -RequestId 'req-a-completed' -ServerStatus 'completed'
        $hook = Invoke-P3StopGate -Layout $layout -ProofYaml $proof
        $turn = Read-P3Yaml -Path (Join-Path $layout.Cache 'current-turn.yaml')
        $session = Read-P3Yaml -Path (Join-Path $layout.Cache 'session-state.yaml')

        [string]$turn['turnRequestId'] | Should -Be 'req-b-active'
        [string]$turn['status'] | Should -Be 'in_progress'
        [string]$turn['auditActions'] | Should -Be '2'
        [string]$session['lastUpdated'] | Should -Be ([string]$beforeSession['lastUpdated'])
        $hook.ReplLog | Should -Not -Match 'completeTurn'
        [string]$hook.Parsed.decision | Should -Be 'block'
        [string]$hook.Parsed.reason | Should -Match '(?i)identity-changed'
    }

    It 'P3 stale local A reconciles when the exact server turn A is completed' {
        $layout = New-P3StopLayout -Name 'stale-a-server-completed'
        $old = (Get-Date).ToUniversalTime().AddHours(-30).ToString('o')
        Write-P3Session -Layout $layout -Timestamp $old -LastUpdated $old
        Write-P3Turn -Layout $layout -RequestId 'req-a-stale' -Status 'in_progress'
        $proof = Write-P3Proof -Layout $layout -Outcome 'Completed' -RequestId 'req-a-stale' -ServerStatus 'completed'
        $hook = Invoke-P3StopGate -Layout $layout -ProofYaml $proof
        $turn = Read-P3Yaml -Path (Join-Path $layout.Cache 'current-turn.yaml')

        [string]$turn['turnRequestId'] | Should -Be 'req-a-stale'
        [string]$turn['status'] | Should -Be 'completed' -Because 'only an exact authoritative completion may reconcile stale local A'
        $hook.ReplLog | Should -Match 'client\.SessionLog\.GetAsync'
        $hook.ReplLog | Should -Not -Match 'workflow\.sessionlog\.completeTurn'
        [string]$hook.Parsed.reason | Should -Not -Match 'stale cached session cannot be reused'
    }

    It 'P3 missing server turn fails closed and retains stale local A' {
        $layout = New-P3StopLayout -Name 'missing-server-turn'
        $old = (Get-Date).ToUniversalTime().AddHours(-30).ToString('o')
        Write-P3Session -Layout $layout -Timestamp $old -LastUpdated $old
        Write-P3Turn -Layout $layout -RequestId 'req-a-stale' -Status 'in_progress' -Audit @{ auditActions = 4; auditFiles = 1; auditDialog = 0; auditDecisions = 0; auditCommits = 0 }
        $beforeSession = Read-P3Yaml -Path (Join-Path $layout.Cache 'session-state.yaml')
        $proof = Write-P3Proof -Layout $layout -Outcome 'Missing' -RequestId 'req-a-stale' -ServerStatus ''
        $hook = Invoke-P3StopGate -Layout $layout -ProofYaml $proof
        $turn = Read-P3Yaml -Path (Join-Path $layout.Cache 'current-turn.yaml')
        $session = Read-P3Yaml -Path (Join-Path $layout.Cache 'session-state.yaml')

        [string]$turn['status'] | Should -Be 'in_progress'
        [string]$turn['turnRequestId'] | Should -Be 'req-a-stale'
        [string]$turn['auditActions'] | Should -Be '4'
        [string]$turn['auditFiles'] | Should -Be '1'
        [string]$session['lastUpdated'] | Should -Be ([string]$beforeSession['lastUpdated'])
        $hook.ReplLog | Should -Not -Match 'completeTurn'
        [string]$hook.Parsed.decision | Should -Be 'block'
        [string]$hook.Parsed.reason | Should -Match '(?i)missing'
    }

    It 'P3 query failure fails closed and retains stale local A' {
        $layout = New-P3StopLayout -Name 'query-failure'
        $old = (Get-Date).ToUniversalTime().AddHours(-30).ToString('o')
        Write-P3Session -Layout $layout -Timestamp $old -LastUpdated $old
        Write-P3Turn -Layout $layout -RequestId 'req-a-stale' -Status 'in_progress'
        $beforeSession = Read-P3Yaml -Path (Join-Path $layout.Cache 'session-state.yaml')
        $proof = Write-P3Proof -Layout $layout -Outcome 'Unavailable' -RequestId 'req-a-stale' -ServerStatus ''
        $hook = Invoke-P3StopGate -Layout $layout -ProofYaml $proof
        $turn = Read-P3Yaml -Path (Join-Path $layout.Cache 'current-turn.yaml')
        $session = Read-P3Yaml -Path (Join-Path $layout.Cache 'session-state.yaml')

        [string]$turn['status'] | Should -Be 'in_progress'
        [string]$turn['turnRequestId'] | Should -Be 'req-a-stale'
        [string]$session['lastUpdated'] | Should -Be ([string]$beforeSession['lastUpdated'])
        $hook.ReplLog | Should -Not -Match 'completeTurn'
        [string]$hook.Parsed.decision | Should -Be 'block'
        [string]$hook.Parsed.reason | Should -Match '(?i)unavailable'
    }

    It 'P3 workspace mismatch does not authorize stale completion' {
        $layout = New-P3StopLayout -Name 'workspace-mismatch'
        $old = (Get-Date).ToUniversalTime().AddHours(-30).ToString('o')
        Write-P3Session -Layout $layout -Timestamp $old -LastUpdated $old
        Write-P3Turn -Layout $layout -RequestId 'req-a-stale' -Status 'in_progress'
        $beforeSession = Read-P3Yaml -Path (Join-Path $layout.Cache 'session-state.yaml')
        $other = Join-Path $layout.Root 'other-workspace'
        $proof = Write-P3Proof -Layout $layout -Outcome 'Completed' -RequestId 'req-a-stale' -ServerStatus 'completed' -WorkspacePath $other
        $hook = Invoke-P3StopGate -Layout $layout -ProofYaml $proof
        $turn = Read-P3Yaml -Path (Join-Path $layout.Cache 'current-turn.yaml')
        $session = Read-P3Yaml -Path (Join-Path $layout.Cache 'session-state.yaml')

        [string]$turn['status'] | Should -Be 'in_progress'
        [string]$turn['turnRequestId'] | Should -Be 'req-a-stale'
        [string]$session['lastUpdated'] | Should -Be ([string]$beforeSession['lastUpdated'])
        $hook.ReplLog | Should -Not -Match 'completeTurn'
        [string]$hook.Parsed.decision | Should -Be 'block'
        [string]$hook.Parsed.reason | Should -Match '(?i)workspace'
    }

    It 'P3 a fresh lastUpdated does not prove completion of a stale timestamp pin' {
        $layout = New-P3StopLayout -Name 'fresh-lastupdated'
        $old = (Get-Date).ToUniversalTime().AddHours(-30).ToString('o')
        $fresh = (Get-Date).ToUniversalTime().ToString('o')
        Write-P3Session -Layout $layout -Timestamp $old -LastUpdated $fresh
        Write-P3Turn -Layout $layout -RequestId 'req-a-stale' -Status 'in_progress'
        $beforeSession = Read-P3Yaml -Path (Join-Path $layout.Cache 'session-state.yaml')
        $proof = Write-P3Proof -Layout $layout -Outcome 'Active' -RequestId 'req-a-stale' -ServerStatus 'in_progress'
        $hook = Invoke-P3StopGate -Layout $layout -ProofYaml $proof
        $turn = Read-P3Yaml -Path (Join-Path $layout.Cache 'current-turn.yaml')
        $session = Read-P3Yaml -Path (Join-Path $layout.Cache 'session-state.yaml')

        [string]$turn['status'] | Should -Be 'in_progress'
        [string]$session['timestamp'] | Should -Be ([string]$beforeSession['timestamp'])
        [string]$session['lastUpdated'] | Should -Be ([string]$beforeSession['lastUpdated'])
        $hook.ReplLog | Should -Not -Match 'completeTurn'
        [string]$hook.Parsed.decision | Should -Be 'block'
        [string]$hook.Parsed.reason | Should -Match 'stale cached session cannot be reused'
    }

    It 'P3 both stale timestamp fields do not auto-close an in-progress turn' {
        $layout = New-P3StopLayout -Name 'both-stale-fields'
        $old = (Get-Date).ToUniversalTime().AddHours(-30).ToString('o')
        Write-P3Session -Layout $layout -Timestamp $old -LastUpdated $old
        Write-P3Turn -Layout $layout -RequestId 'req-a-stale' -Status 'in_progress'
        $beforeSession = Read-P3Yaml -Path (Join-Path $layout.Cache 'session-state.yaml')
        $proof = Write-P3Proof -Layout $layout -Outcome 'Active' -RequestId 'req-a-stale' -ServerStatus 'in_progress'
        $hook = Invoke-P3StopGate -Layout $layout -ProofYaml $proof
        $turn = Read-P3Yaml -Path (Join-Path $layout.Cache 'current-turn.yaml')
        $session = Read-P3Yaml -Path (Join-Path $layout.Cache 'session-state.yaml')

        [string]$turn['status'] | Should -Be 'in_progress'
        [string]$session['timestamp'] | Should -Be ([string]$beforeSession['timestamp'])
        [string]$session['lastUpdated'] | Should -Be ([string]$beforeSession['lastUpdated'])
        $hook.ReplLog | Should -Not -Match 'completeTurn'
        [string]$hook.Parsed.decision | Should -Be 'block'
        [string]$hook.Parsed.reason | Should -Match 'stale cached session cannot be reused for req-a-stale'
    }

    It 'P3 an invalid timestamp is unknown age and does not auto-close the turn' {
        $layout = New-P3StopLayout -Name 'invalid-timestamp'
        $fresh = (Get-Date).ToUniversalTime().ToString('o')
        Write-P3Session -Layout $layout -Timestamp 'not-a-date' -LastUpdated $fresh
        Write-P3Turn -Layout $layout -RequestId 'req-a-invalid-age' -Status 'in_progress'
        $beforeSession = Read-P3Yaml -Path (Join-Path $layout.Cache 'session-state.yaml')
        $proof = Write-P3Proof -Layout $layout -Outcome 'Active' -RequestId 'req-a-invalid-age' -ServerStatus 'in_progress'
        $hook = Invoke-P3StopGate -Layout $layout -ProofYaml $proof
        $turn = Read-P3Yaml -Path (Join-Path $layout.Cache 'current-turn.yaml')
        $session = Read-P3Yaml -Path (Join-Path $layout.Cache 'session-state.yaml')

        $hook.ReplLog | Should -Not -Match 'completeTurn' -Because 'unknown age cannot prove that an in-progress turn may be auto-closed'
        [string]$turn['status'] | Should -Be 'in_progress'
        [string]$session['lastUpdated'] | Should -Be ([string]$beforeSession['lastUpdated'])
        [string]$hook.Parsed.decision | Should -Be 'block'
    }

    It 'P3 missing timestamp and lastUpdated do not auto-close an in-progress turn' {
        $layout = New-P3StopLayout -Name 'missing-both-timestamps'
        Write-P3Session -Layout $layout
        Write-P3Turn -Layout $layout -RequestId 'req-a-no-age' -Status 'in_progress'
        $proof = Write-P3Proof -Layout $layout -Outcome 'Active' -RequestId 'req-a-no-age' -ServerStatus 'in_progress'
        $hook = Invoke-P3StopGate -Layout $layout -ProofYaml $proof
        $turn = Read-P3Yaml -Path (Join-Path $layout.Cache 'current-turn.yaml')
        $session = Read-P3Yaml -Path (Join-Path $layout.Cache 'session-state.yaml')

        [string]$turn['status'] | Should -Be 'in_progress'
        $session.Contains('lastUpdated') | Should -BeFalse
        $hook.ReplLog | Should -Not -Match 'completeTurn'
        [string]$hook.Parsed.decision | Should -Be 'block'
        [string]$hook.Parsed.reason | Should -Match 'stale cached session cannot be reused'
    }

    It 'P3 failed-build blocks before a stale recency refresh on a local completed turn' {
        $layout = New-P3StopLayout -Name 'completed-failed-build'
        $old = (Get-Date).ToUniversalTime().AddHours(-30).ToString('o')
        Write-P3Session -Layout $layout -Timestamp $old -LastUpdated $old
        Write-P3Turn -Layout $layout -RequestId 'req-a-done' -Status 'completed' -CodeEdits 4 -LastBuildStatus 'failed' -Audit @{ auditActions = 1; auditFiles = 0; auditDialog = 0; auditDecisions = 0; auditCommits = 0 }
        $beforeSession = Read-P3Yaml -Path (Join-Path $layout.Cache 'session-state.yaml')
        $proof = Write-P3Proof -Layout $layout -Outcome 'Completed' -RequestId 'req-a-done' -ServerStatus 'completed'
        $hook = Invoke-P3StopGate -Layout $layout -ProofYaml $proof
        $turn = Read-P3Yaml -Path (Join-Path $layout.Cache 'current-turn.yaml')
        $session = Read-P3Yaml -Path (Join-Path $layout.Cache 'session-state.yaml')

        [string]$session['lastUpdated'] | Should -Be ([string]$beforeSession['lastUpdated']) -Because 'failed-build enforcement must run before a stale recency mutation'
        [string]$turn['lastBuildStatus'] | Should -Be 'failed'
        [string]$turn['codeEdits'] | Should -Be '4'
        [string]$hook.Parsed.decision | Should -Be 'block'
        [string]$hook.Parsed.reason | Should -Match 'Last build in this turn failed after 4 code edit'
    }

    It 'P3 incomplete-audit blocks before a stale recency refresh on a local completed turn' {
        $layout = New-P3StopLayout -Name 'completed-incomplete-audit'
        $old = (Get-Date).ToUniversalTime().AddHours(-30).ToString('o')
        Write-P3Session -Layout $layout -Timestamp $old -LastUpdated $old
        Write-P3Turn -Layout $layout -RequestId 'req-a-done' -Status 'completed' -CodeEdits 2 -LastBuildStatus 'succeeded' -Audit @{ auditActions = 0; auditFiles = 0; auditDialog = 0; auditDecisions = 0; auditCommits = 0 }
        $beforeSession = Read-P3Yaml -Path (Join-Path $layout.Cache 'session-state.yaml')
        $proof = Write-P3Proof -Layout $layout -Outcome 'Completed' -RequestId 'req-a-done' -ServerStatus 'completed'
        $hook = Invoke-P3StopGate -Layout $layout -ProofYaml $proof
        $turn = Read-P3Yaml -Path (Join-Path $layout.Cache 'current-turn.yaml')
        $session = Read-P3Yaml -Path (Join-Path $layout.Cache 'session-state.yaml')

        [string]$session['lastUpdated'] | Should -Be ([string]$beforeSession['lastUpdated']) -Because 'incomplete-audit enforcement must run before a stale recency mutation'
        [string]$turn['auditActions'] | Should -Be '0'
        [string]$turn['auditCommits'] | Should -Be '0'
        [string]$turn['codeEdits'] | Should -Be '2'
        [string]$hook.Parsed.decision | Should -Be 'block'
        [string]$hook.Parsed.reason | Should -Match 'audit is incomplete for a completed code-edit turn'
    }

    It 'P3 failed-build is enforced before a stale in-progress pin decision' {
        $layout = New-P3StopLayout -Name 'inprogress-failed-build-first'
        $old = (Get-Date).ToUniversalTime().AddHours(-30).ToString('o')
        Write-P3Session -Layout $layout -Timestamp $old -LastUpdated $old
        Write-P3Turn -Layout $layout -RequestId 'req-a-stale' -Status 'in_progress' -CodeEdits 4 -LastBuildStatus 'failed' -Audit @{ auditActions = 1; auditFiles = 0; auditDialog = 0; auditDecisions = 0; auditCommits = 0 }
        $beforeSession = Read-P3Yaml -Path (Join-Path $layout.Cache 'session-state.yaml')
        $proof = Write-P3Proof -Layout $layout -Outcome 'Completed' -RequestId 'req-a-stale' -ServerStatus 'completed'
        $hook = Invoke-P3StopGate -Layout $layout -ProofYaml $proof
        $turn = Read-P3Yaml -Path (Join-Path $layout.Cache 'current-turn.yaml')
        $session = Read-P3Yaml -Path (Join-Path $layout.Cache 'session-state.yaml')

        [string]$hook.Parsed.reason | Should -Match 'Last build in this turn failed after 4 code edit' -Because 'failed-build enforcement must precede the stale-pin decision'
        [string]$hook.Parsed.reason | Should -Not -Match 'stale cached session cannot be reused'
        [string]$turn['status'] | Should -Be 'in_progress'
        [string]$turn['lastBuildStatus'] | Should -Be 'failed'
        [string]$session['lastUpdated'] | Should -Be ([string]$beforeSession['lastUpdated'])
    }

    It 'P3 a fresh completed turn blocks failed-build without a recency mutation' {
        $layout = New-P3StopLayout -Name 'fresh-completed-failed-build'
        $fresh = (Get-Date).ToUniversalTime().ToString('o')
        Write-P3Session -Layout $layout -Timestamp $fresh -LastUpdated $fresh
        Write-P3Turn -Layout $layout -RequestId 'req-a-done' -Status 'completed' -CodeEdits 4 -LastBuildStatus 'failed' -Audit @{ auditActions = 1; auditFiles = 0; auditDialog = 0; auditDecisions = 0; auditCommits = 0 }
        $beforeSession = Read-P3Yaml -Path (Join-Path $layout.Cache 'session-state.yaml')
        $proof = Write-P3Proof -Layout $layout -Outcome 'Completed' -RequestId 'req-a-done' -ServerStatus 'completed'
        $hook = Invoke-P3StopGate -Layout $layout -ProofYaml $proof
        $turn = Read-P3Yaml -Path (Join-Path $layout.Cache 'current-turn.yaml')
        $session = Read-P3Yaml -Path (Join-Path $layout.Cache 'session-state.yaml')

        [string]$session['lastUpdated'] | Should -Be ([string]$beforeSession['lastUpdated'])
        [string]$turn['lastBuildStatus'] | Should -Be 'failed'
        [string]$turn['codeEdits'] | Should -Be '4'
        [string]$hook.Parsed.decision | Should -Be 'block'
        [string]$hook.Parsed.reason | Should -Match 'Last build in this turn failed after 4 code edit'
    }

    It 'P3 a missing local turn does not allow or erase session-level failed-build evidence' {
        $layout = New-P3StopLayout -Name 'missing-turn-failed-build'
        $old = (Get-Date).ToUniversalTime().AddHours(-30).ToString('o')
        Write-P3Session -Layout $layout -Timestamp $old -LastUpdated $old -Extra @{
            codeEdits = 4
            lastBuildStatus = 'failed'
            auditActions = 1
            auditFiles = 0
            auditDialog = 0
            auditDecisions = 0
            auditCommits = 0
        }
        $beforeSession = Read-P3Yaml -Path (Join-Path $layout.Cache 'session-state.yaml')
        $proof = Write-P3Proof -Layout $layout -Outcome 'Missing' -RequestId 'req-a-absent' -ServerStatus ''
        $hook = Invoke-P3StopGate -Layout $layout -ProofYaml $proof
        $session = Read-P3Yaml -Path (Join-Path $layout.Cache 'session-state.yaml')

        Test-Path -LiteralPath (Join-Path $layout.Cache 'current-turn.yaml') | Should -BeFalse
        [string]$session['lastBuildStatus'] | Should -Be 'failed'
        [string]$session['codeEdits'] | Should -Be '4'
        [string]$session['auditActions'] | Should -Be '1'
        [string]$session['lastUpdated'] | Should -Be ([string]$beforeSession['lastUpdated']) -Because 'a missing turn file must not refresh recency over available failed-build evidence'
        [string]$hook.Parsed.decision | Should -Be 'block'
        [string]$hook.Parsed.reason | Should -Match 'Last build'
    }

    It 'P3 a missing local turn with unknown build and audit state is not allowed as clean' {
        $layout = New-P3StopLayout -Name 'missing-turn-unknown'
        $old = (Get-Date).ToUniversalTime().AddHours(-30).ToString('o')
        Write-P3Session -Layout $layout -Timestamp $old -LastUpdated $old
        $beforeSession = Read-P3Yaml -Path (Join-Path $layout.Cache 'session-state.yaml')
        $proof = Write-P3Proof -Layout $layout -Outcome 'Missing' -RequestId 'req-a-absent' -ServerStatus ''
        $hook = Invoke-P3StopGate -Layout $layout -ProofYaml $proof
        $session = Read-P3Yaml -Path (Join-Path $layout.Cache 'session-state.yaml')

        [string]$hook.Parsed.decision | Should -Be 'block' -Because 'unknown failed-build and audit state must not be fabricated as a clean allow'
        [string]$session['lastUpdated'] | Should -Be ([string]$beforeSession['lastUpdated'])
        [string]$session['sessionId'] | Should -Be $layout.SessionId
    }
}

Describe 'P3 bounded child execution' {
    BeforeAll {
        $script:RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')).ProviderPath
        $script:ReplScript = Join-Path $script:RepoRoot 'plugins\core\lib-ps\repl-invoke.ps1'
        $script:Work = Join-Path ([System.IO.Path]::GetTempPath()) ('sessionlife-p3-child-' + [guid]::NewGuid().ToString('N'))
        [void][System.IO.Directory]::CreateDirectory($script:Work)
        . (Join-Path $script:RepoRoot 'plugins\core\lib-ps\yaml-object-mutation.ps1')
        . (Join-Path $script:RepoRoot 'plugins\core\lib-ps\marker-resolver.ps1')
        Import-McpYamlSerializer

        function New-P3ChildLayout {
            param([Parameter(Mandatory)][string]$Name)
            $root = Join-Path $script:Work $Name
            $workspace = Join-Path $root 'ws'
            $cache = Join-Path $root 'cache'
            $failsafe = Join-Path $root 'failsafe'
            $pidDir = Join-Path $root 'pids'
            [void][System.IO.Directory]::CreateDirectory($workspace)
            [void][System.IO.Directory]::CreateDirectory($cache)
            [void][System.IO.Directory]::CreateDirectory($failsafe)
            [void][System.IO.Directory]::CreateDirectory($pidDir)
            $marker = Join-Path $workspace 'AGENTS-README-FIRST.yaml'
            [System.IO.File]::WriteAllText($marker, "workspacePath: $workspace`napiKey: p3-child`n")
            $snapshot = Get-MarkerFileSnapshot -StartDir $workspace
            $sessionId = 'Codex-20261003T100000Z-p3child'
            Write-McpYamlObject -Path (Join-Path $cache 'session-state.yaml') -Document ([ordered]@{
                    status = 'verified'
                    sessionId = $sessionId
                    agent = 'Codex'
                    workspacePath = $workspace
                    markerFilePath = $snapshot.markerFilePath
                    markerLastWriteUtc = $snapshot.markerLastWriteUtc
                })
            $payloadPath = Join-Path $failsafe '20261003T100000Z-session_submit-p3.yaml'
            Write-McpYamlObject -Path $payloadPath -Document ([ordered]@{
                    method = 'client.SessionLog.SubmitAsync'
                    label = 'session_submit'
                    timestamp = '20261003T100000Z'
                    params = [ordered]@{
                        sessionLog = [ordered]@{
                            sourceType = 'Codex'
                            sessionId = $sessionId
                            turns = @(
                                [ordered]@{
                                    requestId = 'req-p3-write-ahead'
                                    status = 'in_progress'
                                    queryText = 'write-ahead payload'
                                }
                            )
                        }
                    }
                })
            return [pscustomobject]@{
                Root = $root
                Workspace = $workspace
                Cache = $cache
                Failsafe = $failsafe
                Payload = $payloadPath
                PidDir = $pidDir
                PayloadHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $payloadPath).Hash
            }
        }

        function Write-P3Executable {
            param([Parameter(Mandatory)][string]$Directory, [Parameter(Mandatory)][string]$Body)
            $path = Join-Path $Directory 'child.sh'
            $text = "#!/bin/bash`n" + $Body.Trim() + "`n"
            [System.IO.File]::WriteAllText($path, $text.Replace("`r`n", "`n"))
            & chmod +x -- $path
            return $path
        }

        function Stop-P3RecordedProcesses {
            param([string]$PidDir)
            if (-not (Test-Path -LiteralPath $PidDir)) { return }
            foreach ($file in @(Get-ChildItem -LiteralPath $PidDir -Filter '*.pid' -File -ErrorAction SilentlyContinue)) {
                $raw = ([System.IO.File]::ReadAllText($file.FullName)).Trim()
                $processId = 0
                if ([int]::TryParse($raw, [ref]$processId) -and $processId -gt 0 -and (Test-Path -LiteralPath "/proc/$processId")) {
                    try { Stop-Process -Id $processId -Force -ErrorAction Stop } catch { }
                }
            }
        }

        function Test-P3ProcessAlive {
            param([string]$PidFile)
            if (-not (Test-Path -LiteralPath $PidFile)) { return $false }
            $processId = 0
            if (-not [int]::TryParse(([System.IO.File]::ReadAllText($PidFile)).Trim(), [ref]$processId)) { return $false }
            if ($processId -le 0) { return $false }
            return (Test-Path -LiteralPath "/proc/$processId")
        }

        function Invoke-P3ChildRaw {
            param(
                $Layout,
                [Parameter(Mandatory)][string]$Executable,
                [int]$TimeoutSeconds = 2,
                [string]$ParamsYaml = 'probe: 1',
                [int]$WatchdogSeconds = 8
            )
            $saved = @{
                REPL_TIMEOUT = $env:REPL_TIMEOUT
                REPL_FAILSAFE_DRAIN_TIMEOUT = $env:REPL_FAILSAFE_DRAIN_TIMEOUT
                MCP_REPL_EXECUTABLE = $env:MCP_REPL_EXECUTABLE
                MCP_CACHE_DIR_OVERRIDE = $env:MCP_CACHE_DIR_OVERRIDE
                MCPSERVER_FAILSAFE_DIR = $env:MCPSERVER_FAILSAFE_DIR
                MCP_WORKSPACE_PATH = $env:MCP_WORKSPACE_PATH
                MCPSERVER_WORKSPACE_PATH = $env:MCPSERVER_WORKSPACE_PATH
                MCP_PLUGIN_HOST = $env:MCP_PLUGIN_HOST
                MCP_AGENT_NAME = $env:MCP_AGENT_NAME
                P3_PID_DIR = $env:P3_PID_DIR
            }
            $ps = [powershell]::Create()
            $null = $ps.AddScript({
                    param($ReplScript, $Cache, $Workspace, $Failsafe, $Executable, $TimeoutSeconds, $ParamsYaml, $PidDir)
                    $ErrorActionPreference = 'Stop'
                    Set-Location -LiteralPath $Workspace
                    $env:MCP_CACHE_DIR_OVERRIDE = $Cache
                    $env:MCPSERVER_FAILSAFE_DIR = $Failsafe
                    $env:MCP_WORKSPACE_PATH = $Workspace
                    $env:MCPSERVER_WORKSPACE_PATH = $Workspace
                    $env:MCP_PLUGIN_HOST = 'codex'
                    $env:MCP_AGENT_NAME = 'Codex'
                    $env:PLUGIN_AGENT_NAME = 'Codex'
                    $env:MCP_REPL_EXECUTABLE = $Executable
                    $env:REPL_TIMEOUT = [string]$TimeoutSeconds
                    $env:P3_PID_DIR = $PidDir
                    Remove-Item Env:REPL_FAILSAFE_DRAIN_TIMEOUT -ErrorAction SilentlyContinue
                    . $ReplScript
                    $sw = [System.Diagnostics.Stopwatch]::StartNew()
                    $result = Invoke-ReplRawCore -Method 'client.Health.GetAsync' -ParamsYaml $ParamsYaml
                    $sw.Stop()
                    [pscustomobject]@{
                        Success = [bool]$result.Success
                        Error = [string]$result.Error
                        Output = [string]$result.Output
                        ElapsedSeconds = $sw.Elapsed.TotalSeconds
                        Hung = $false
                    }
                }).AddArgument($script:ReplScript).AddArgument($Layout.Cache).AddArgument($Layout.Workspace).AddArgument($Layout.Failsafe).AddArgument($Executable).AddArgument($TimeoutSeconds).AddArgument($ParamsYaml).AddArgument($Layout.PidDir)
            $handle = $ps.BeginInvoke()
            try {
                if (-not $handle.AsyncWaitHandle.WaitOne($WatchdogSeconds * 1000)) {
                    try { $ps.Stop() } catch { }
                    return [pscustomobject]@{
                        Success = $false
                        Error = "watchdog: child execution hung past ${WatchdogSeconds}s"
                        Output = ''
                        ElapsedSeconds = [double]$WatchdogSeconds
                        Hung = $true
                    }
                }
                $returned = @($ps.EndInvoke($handle))
                return $returned[0]
            }
            finally {
                foreach ($name in $saved.Keys) {
                    if ($null -eq $saved[$name]) {
                        Remove-Item -Path "Env:$name" -ErrorAction SilentlyContinue
                    }
                    else {
                        Set-Item -Path "Env:$name" -Value $saved[$name]
                    }
                }
            }
        }
    }

    AfterAll {
        if ($script:Work -and (Test-Path -LiteralPath $script:Work)) {
            Get-ChildItem -LiteralPath $script:Work -Recurse -Filter '*.pid' -File -ErrorAction SilentlyContinue | ForEach-Object {
                $processId = 0
                if ([int]::TryParse(([System.IO.File]::ReadAllText($_.FullName)).Trim(), [ref]$processId) -and $processId -gt 0) {
                    try { Stop-Process -Id $processId -Force -ErrorAction SilentlyContinue } catch { }
                }
            }
            Remove-Item -LiteralPath $script:Work -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'P3 blocked stdin is killed inside one child deadline and write-ahead data is retained' {
        $layout = New-P3ChildLayout -Name 'blocked-stdin'
        $executable = Write-P3Executable -Directory $layout.Root -Body @'
echo $$ > "$P3_PID_DIR/self.pid"
sleep 60
'@
        try {
            $result = Invoke-P3ChildRaw -Layout $layout -Executable $executable -TimeoutSeconds 2 -ParamsYaml ('probe: ' + ('x' * 200000)) -WatchdogSeconds 8
            $result.Hung | Should -BeFalse
            $result.Success | Should -BeFalse
            $result.Error | Should -Match 'timed out'
            (Get-FileHash -Algorithm SHA256 -LiteralPath $layout.Payload).Hash | Should -Be $layout.PayloadHash
            $result.ElapsedSeconds | Should -BeLessThan 3.5
            (Test-P3ProcessAlive -PidFile (Join-Path $layout.PidDir 'self.pid')) | Should -BeFalse
        }
        finally {
            Stop-P3RecordedProcesses -PidDir $layout.PidDir
        }
    }

    It 'P3 nonclosing stdout is killed inside one child deadline and write-ahead data is retained' {
        $layout = New-P3ChildLayout -Name 'nonclosing-stdout'
        $executable = Write-P3Executable -Directory $layout.Root -Body @'
echo $$ > "$P3_PID_DIR/self.pid"
cat >/dev/null
printf 'partial-stdout'
sleep 60
'@
        try {
            $result = Invoke-P3ChildRaw -Layout $layout -Executable $executable -TimeoutSeconds 2 -ParamsYaml 'probe: 1' -WatchdogSeconds 8
            $result.Hung | Should -BeFalse
            $result.Success | Should -BeFalse
            $result.Error | Should -Match 'timed out'
            (Get-FileHash -Algorithm SHA256 -LiteralPath $layout.Payload).Hash | Should -Be $layout.PayloadHash
            $result.ElapsedSeconds | Should -BeLessThan 3.5
            (Test-P3ProcessAlive -PidFile (Join-Path $layout.PidDir 'self.pid')) | Should -BeFalse
        }
        finally {
            Stop-P3RecordedProcesses -PidDir $layout.PidDir
        }
    }

    It 'P3 saturated stderr still lets a finite child finish inside one deadline and keeps write-ahead data' {
        $layout = New-P3ChildLayout -Name 'saturated-stderr'
        $executable = Write-P3Executable -Directory $layout.Root -Body @'
echo $$ > "$P3_PID_DIR/self.pid"
dd if=/dev/zero bs=1024 count=256 status=none >&2
printf 'stdout-done'
exit 0
'@
        $runner = Join-Path $layout.Root 'runner.ps1'
        @'
param([string]$ReplScript, [string]$Cache, [string]$Workspace, [string]$Failsafe, [string]$Executable, [string]$ResultPath, [string]$PidDir)
$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $Workspace
$env:MCP_CACHE_DIR_OVERRIDE = $Cache
$env:MCPSERVER_FAILSAFE_DIR = $Failsafe
$env:MCP_WORKSPACE_PATH = $Workspace
$env:MCPSERVER_WORKSPACE_PATH = $Workspace
$env:MCP_PLUGIN_HOST = 'codex'
$env:MCP_AGENT_NAME = 'Codex'
$env:PLUGIN_AGENT_NAME = 'Codex'
$env:MCP_REPL_EXECUTABLE = $Executable
$env:REPL_TIMEOUT = '3'
$env:P3_PID_DIR = $PidDir
Remove-Item Env:REPL_FAILSAFE_DRAIN_TIMEOUT -ErrorAction SilentlyContinue
. $ReplScript
$sw = [System.Diagnostics.Stopwatch]::StartNew()
$result = Invoke-ReplRawCore -Method 'client.Health.GetAsync' -ParamsYaml 'probe: 1'
$sw.Stop()
[pscustomobject]@{
    Success = [bool]$result.Success
    Error = [string]$result.Error
    Output = [string]$result.Output
    ElapsedSeconds = $sw.Elapsed.TotalSeconds
} | ConvertTo-Json -Compress | Set-Content -LiteralPath $ResultPath -Encoding utf8
'@ | Set-Content -LiteralPath $runner -Encoding utf8NoBOM
        $resultPath = Join-Path $layout.Root 'result.json'
        $psi = [System.Diagnostics.ProcessStartInfo]::new()
        $psi.FileName = (Get-Command pwsh -ErrorAction Stop).Source
        foreach ($arg in @('-NoLogo', '-NoProfile', '-NonInteractive', '-File', $runner, $script:ReplScript, $layout.Cache, $layout.Workspace, $layout.Failsafe, $executable, $resultPath, $layout.PidDir)) {
            $psi.ArgumentList.Add($arg)
        }
        $psi.WorkingDirectory = $layout.Workspace
        $psi.UseShellExecute = $false
        $psi.RedirectStandardOutput = $true
        $psi.RedirectStandardError = $true
        $proc = [System.Diagnostics.Process]::Start($psi)
        $stdout = $proc.StandardOutput.ReadToEndAsync()
        try {
            $exited = $proc.WaitForExit(10000)
            if (-not $exited) {
                try { $proc.Kill($true) } catch { }
                throw 'saturated-stderr runner hung past 10s'
            }
            $null = $stdout.Result
            try { $null = $proc.StandardError.ReadToEnd() } catch { }
            $payload = Get-Content -LiteralPath $resultPath -Raw | ConvertFrom-Json
            $payload.Success | Should -BeTrue -Because 'stderr must be drained inside the same deadline so a finite write cannot stall the child'
            [string]$payload.Output | Should -Match 'stdout-done'
            [double]$payload.ElapsedSeconds | Should -BeLessThan 3.5
            (Get-FileHash -Algorithm SHA256 -LiteralPath $layout.Payload).Hash | Should -Be $layout.PayloadHash
            (Test-P3ProcessAlive -PidFile (Join-Path $layout.PidDir 'self.pid')) | Should -BeFalse
        }
        finally {
            if (-not $proc.HasExited) { try { $proc.Kill($true) } catch { } }
            Stop-P3RecordedProcesses -PidDir $layout.PidDir
        }
    }

    It 'P3 cancellation kills the child tree and retains write-ahead data' {
        $layout = New-P3ChildLayout -Name 'cancellation'
        $executable = Write-P3Executable -Directory $layout.Root -Body @'
echo $$ > "$P3_PID_DIR/self.pid"
sleep 60 &
echo $! > "$P3_PID_DIR/descendant.pid"
wait
'@
        $saved = @{
            REPL_TIMEOUT = $env:REPL_TIMEOUT
            MCP_REPL_EXECUTABLE = $env:MCP_REPL_EXECUTABLE
            MCP_CACHE_DIR_OVERRIDE = $env:MCP_CACHE_DIR_OVERRIDE
            MCPSERVER_FAILSAFE_DIR = $env:MCPSERVER_FAILSAFE_DIR
            MCP_WORKSPACE_PATH = $env:MCP_WORKSPACE_PATH
            P3_PID_DIR = $env:P3_PID_DIR
        }
        $ps = [powershell]::Create()
        $null = $ps.AddScript({
                param($ReplScript, $Cache, $Workspace, $Failsafe, $Executable, $PidDir)
                $ErrorActionPreference = 'Stop'
                Set-Location -LiteralPath $Workspace
                $env:MCP_CACHE_DIR_OVERRIDE = $Cache
                $env:MCPSERVER_FAILSAFE_DIR = $Failsafe
                $env:MCP_WORKSPACE_PATH = $Workspace
                $env:MCPSERVER_WORKSPACE_PATH = $Workspace
                $env:MCP_PLUGIN_HOST = 'codex'
                $env:MCP_AGENT_NAME = 'Codex'
                $env:MCP_REPL_EXECUTABLE = $Executable
                $env:REPL_TIMEOUT = '30'
                $env:P3_PID_DIR = $PidDir
                . $ReplScript
                Invoke-ReplRawCore -Method 'client.Health.GetAsync' -ParamsYaml 'probe: 1'
            }).AddArgument($script:ReplScript).AddArgument($layout.Cache).AddArgument($layout.Workspace).AddArgument($layout.Failsafe).AddArgument($executable).AddArgument($layout.PidDir)
        $handle = $ps.BeginInvoke()
        if ($null -eq $handle) { throw 'cancellation pipeline did not start' }
        $stopper = $null
        try {
            $seen = $false
            $started = [datetime]::UtcNow.AddSeconds(4)
            while ([datetime]::UtcNow -lt $started) {
                if (Test-Path -LiteralPath (Join-Path $layout.PidDir 'descendant.pid')) { $seen = $true; break }
                Start-Sleep -Milliseconds 50
            }
            $seen | Should -BeTrue -Because 'the child tree must start before cancellation is requested'
            # Stop() can block inside the child wait. Request it off the assertion thread
            # and require the tree to die well before REPL_TIMEOUT (30s) can kill it.
            $stopper = [powershell]::Create()
            $null = $stopper.AddScript({ param($Pipeline) $Pipeline.Stop() }).AddArgument($ps)
            $null = $stopper.BeginInvoke()
            $cancelDeadline = [datetime]::UtcNow.AddSeconds(2)
            do {
                $selfAlive = Test-P3ProcessAlive -PidFile (Join-Path $layout.PidDir 'self.pid')
                $descendantAlive = Test-P3ProcessAlive -PidFile (Join-Path $layout.PidDir 'descendant.pid')
                if (-not $selfAlive -and -not $descendantAlive) { break }
                Start-Sleep -Milliseconds 50
            } while ([datetime]::UtcNow -lt $cancelDeadline)
            $selfAlive | Should -BeFalse -Because 'cancellation must kill the direct child before the 30s hook deadline'
            $descendantAlive | Should -BeFalse -Because 'cancellation must kill descendants before the 30s hook deadline'
            (Get-FileHash -Algorithm SHA256 -LiteralPath $layout.Payload).Hash | Should -Be $layout.PayloadHash
        }
        finally {
            Stop-P3RecordedProcesses -PidDir $layout.PidDir
            try { if ($stopper) { $stopper.Stop() } } catch { }
            $finalStop = [powershell]::Create()
            $null = $finalStop.AddScript({ param($Pipeline) $Pipeline.Stop() }).AddArgument($ps)
            $finalHandle = $finalStop.BeginInvoke()
            $null = $finalHandle.AsyncWaitHandle.WaitOne(3000)
            foreach ($name in $saved.Keys) {
                if ($null -eq $saved[$name]) { Remove-Item -Path "Env:$name" -ErrorAction SilentlyContinue }
                else { Set-Item -Path "Env:$name" -Value $saved[$name] }
            }
        }
    }

    It 'P3 child descendants are killed and awaited when the deadline expires' {
        $layout = New-P3ChildLayout -Name 'descendants'
        $executable = Write-P3Executable -Directory $layout.Root -Body @'
echo $$ > "$P3_PID_DIR/self.pid"
sleep 60 &
echo $! > "$P3_PID_DIR/descendant.pid"
wait
'@
        try {
            $result = Invoke-P3ChildRaw -Layout $layout -Executable $executable -TimeoutSeconds 2 -ParamsYaml 'probe: 1' -WatchdogSeconds 8
            $result.Hung | Should -BeFalse
            $result.Success | Should -BeFalse
            $result.Error | Should -Match 'timed out'
            (Test-P3ProcessAlive -PidFile (Join-Path $layout.PidDir 'self.pid')) | Should -BeFalse
            (Test-P3ProcessAlive -PidFile (Join-Path $layout.PidDir 'descendant.pid')) | Should -BeFalse
            (Get-FileHash -Algorithm SHA256 -LiteralPath $layout.Payload).Hash | Should -Be $layout.PayloadHash
            $result.ElapsedSeconds | Should -BeLessThan 6
        }
        finally {
            Stop-P3RecordedProcesses -PidDir $layout.PidDir
        }
    }

    It 'P3 one remaining deadline covers stdin stdout stderr and cleanup' {
        $layout = New-P3ChildLayout -Name 'one-deadline'
        $executable = Write-P3Executable -Directory $layout.Root -Body @'
echo $$ > "$P3_PID_DIR/self.pid"
sleep 3
cat >/dev/null
dd if=/dev/zero bs=1024 count=256 status=none >&2 || true
printf 'phase-stdout'
sleep 60
'@
        try {
            $result = Invoke-P3ChildRaw -Layout $layout -Executable $executable -TimeoutSeconds 5 -ParamsYaml ('probe: ' + ('y' * 200000)) -WatchdogSeconds 12
            $result.Hung | Should -BeFalse
            $result.Success | Should -BeFalse
            $result.Error | Should -Match 'timed out'
            $result.ElapsedSeconds | Should -BeLessThan 6.5 -Because 'stdin, stdout, stderr, and cleanup share one remaining deadline'
            (Get-FileHash -Algorithm SHA256 -LiteralPath $layout.Payload).Hash | Should -Be $layout.PayloadHash
            (Test-P3ProcessAlive -PidFile (Join-Path $layout.PidDir 'self.pid')) | Should -BeFalse
        }
        finally {
            Stop-P3RecordedProcesses -PidDir $layout.PidDir
        }
    }
}

Describe 'P3 drain timeout stays isolated from the hook child deadline' {
    BeforeAll {
        $script:RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')).ProviderPath
        $script:ReplScript = Join-Path $script:RepoRoot 'plugins\core\lib-ps\repl-invoke.ps1'

        function Invoke-P3TimeoutProbe {
            param([hashtable]$Environment)
            $probe = Join-Path ([System.IO.Path]::GetTempPath()) ('sessionlife-p3-timeout-' + [guid]::NewGuid().ToString('N') + '.ps1')
            @'
param([string]$ReplScript)
$ErrorActionPreference = 'Stop'
. $ReplScript
$script:ReplFailsafeDraining = $true
$drain = Get-ReplMethodTimeoutSeconds -Method 'client.SessionLog.SubmitAsync'
$hookWhileDraining = Get-ReplMethodTimeoutSeconds -Method 'workflow.sessionlog.completeTurn'
$script:ReplFailsafeDraining = $false
$hook = Get-ReplMethodTimeoutSeconds -Method 'workflow.sessionlog.completeTurn'
Write-Output ("drain={0}" -f $drain)
Write-Output ("hookWhileDraining={0}" -f $hookWhileDraining)
Write-Output ("hook={0}" -f $hook)
'@ | Set-Content -LiteralPath $probe -Encoding utf8NoBOM
            try {
                $psi = [System.Diagnostics.ProcessStartInfo]::new()
                $psi.FileName = (Get-Command pwsh -ErrorAction Stop).Source
                foreach ($arg in @('-NoLogo', '-NoProfile', '-NonInteractive', '-File', $probe, $script:ReplScript)) {
                    $psi.ArgumentList.Add($arg)
                }
                $psi.UseShellExecute = $false
                $psi.RedirectStandardOutput = $true
                $psi.RedirectStandardError = $true
                foreach ($key in @('REPL_TIMEOUT', 'REPL_FAILSAFE_DRAIN_TIMEOUT', 'REPL_LONG_TIMEOUT', 'REPL_HELPER_TIMEOUT')) {
                    if ($psi.Environment.ContainsKey($key)) { [void]$psi.Environment.Remove($key) }
                }
                foreach ($key in $Environment.Keys) { $psi.Environment[$key] = [string]$Environment[$key] }
                $proc = [System.Diagnostics.Process]::Start($psi)
                $stdout = $proc.StandardOutput.ReadToEndAsync()
                $stderr = $proc.StandardError.ReadToEndAsync()
                if (-not $proc.WaitForExit(30000)) {
                    try { $proc.Kill($true) } catch { }
                    throw 'timeout probe hung'
                }
                if ($proc.ExitCode -ne 0) {
                    throw "timeout probe failed: $($stderr.Result)"
                }
                $map = @{}
                foreach ($line in ($stdout.Result -split "`n")) {
                    if ($line -match '^(?<k>[^=]+)=(?<v>-?\d+)\s*$') { $map[$Matches.k] = [int]$Matches.v }
                }
                return $map
            }
            finally {
                Remove-Item -LiteralPath $probe -Force -ErrorAction SilentlyContinue
            }
        }
    }

    It 'P3 BUG-TRIAGE-170 drain default stays 120 seconds and the hook child default stays 30 seconds' {
        $isolated = Invoke-P3TimeoutProbe -Environment @{}
        $isolated['drain'] | Should -Be 120
        $isolated['hookWhileDraining'] | Should -Be 30
        $isolated['hook'] | Should -Be 30
    }

    It 'P3 a drain timeout setting does not change the 30 second hook child contract' {
        $isolated = Invoke-P3TimeoutProbe -Environment @{ REPL_FAILSAFE_DRAIN_TIMEOUT = '90' }
        $isolated['drain'] | Should -Be 90
        $isolated['hookWhileDraining'] | Should -Be 30
        $isolated['hook'] | Should -Be 30
    }

    It 'P3 REPL_TIMEOUT in one process does not contaminate the 120 second drain default in another' {
        $raised = Invoke-P3TimeoutProbe -Environment @{ REPL_TIMEOUT = '180' }
        $clean = Invoke-P3TimeoutProbe -Environment @{}
        $raised['hook'] | Should -Be 180
        $raised['drain'] | Should -Be 180
        $clean['drain'] | Should -Be 120
        $clean['hook'] | Should -Be 30
    }
}
