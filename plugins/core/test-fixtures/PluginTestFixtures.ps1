# SPDX-License-Identifier: Apache-2.0
<#
.SYNOPSIS
    Shared fixtures for the plugin-core Pester suite (TR-MCP-NUKE-012 AC-006; PLAN-SESSIONTRIAGEFIX-001 D7 to D9): the sibling plugin
    repositories found by the TR-MCP-NUKE-006 walk-up, a staged plugin written into a temporary root, and PowerShell fakes that start as
    executables on Windows and on Linux. Dot-source this file from a test's BeforeAll.
#>

function Find-TestPluginSiblingParent {
    <#
    .SYNOPSIS
        TR-MCP-NUKE-006: the directory that holds the official sibling plugin repositories, walking up from a checkout exactly as
        ResolveOfficialPluginSiblingParent does in build/Build.SyncAgentPlugins.cs (worktrees under .worktrees, worktrees, or a dot-folder
        such as .claude/worktrees resolve to the checkout's parent).
    #>
    param([Parameter(Mandatory)][string]$CheckoutRoot)

    $markers = @('mcpserver-codex-plugin', 'mcpserver-grok-plugin', 'mcpserver-claude-code-plugin')
    $current = [System.IO.DirectoryInfo]::new([System.IO.Path]::GetFullPath($CheckoutRoot))
    while ($null -ne $current) {
        $parent = $current.Parent
        if ($null -eq $parent) { break }
        foreach ($marker in $markers) {
            if ([System.IO.Directory]::Exists([System.IO.Path]::Combine($parent.FullName, $marker))) { return $parent.FullName }
        }
        if ($current.Name -eq '.worktrees' -or $current.Name -eq 'worktrees') {
            $checkout = $parent
            if ($checkout.Name.StartsWith('.') -and $null -ne $checkout.Parent) { $checkout = $checkout.Parent }
            if ($null -ne $checkout.Parent) { return $checkout.Parent.FullName }
            return $parent.FullName
        }
        $current = $parent
    }

    $fallback = [System.IO.Directory]::GetParent([System.IO.Path]::GetFullPath($CheckoutRoot))
    if ($null -eq $fallback) { return $null }
    return $fallback.FullName
}

function Get-TestPluginSiblingRepository {
    <#
    .SYNOPSIS
        The named sibling plugin repository found by the TR-MCP-NUKE-006 walk-up; a missing one fails the test with the precondition named.
    #>
    param(
        [Parameter(Mandatory)][string]$CheckoutRoot,
        [Parameter(Mandatory)][string]$Name
    )

    $parent = Find-TestPluginSiblingParent -CheckoutRoot $CheckoutRoot
    $repository = if ($parent) { Join-Path $parent $Name } else { '' }
    if (-not $repository -or -not (Test-Path -LiteralPath $repository -PathType Container)) {
        throw "Test precondition: no $Name checkout was found by the TR-MCP-NUKE-006 walk-up from $CheckoutRoot (looked in '$parent'); clone it beside the McpServer checkout."
    }
    return $repository
}

function Write-TestStagedPlugin {
    <#
    .SYNOPSIS
        Stages the plugin core into Root the way Nuke SyncAgentPlugins stages plugins/core/.staged-plugin (sync-plugin-core.ps1, then
        generate-wrappers.ps1 for claude-code; both write only under their -PluginRoot), so a test reads a staged plugin in any checkout.
        Returns Root.
    #>
    param(
        [Parameter(Mandatory)][string]$RepoRoot,
        [Parameter(Mandatory)][string]$Root
    )

    $pwsh = (Get-Command pwsh -ErrorAction Stop).Source
    [void][System.IO.Directory]::CreateDirectory($Root)
    $sync = Join-Path $RepoRoot 'plugins/core/sync/sync-plugin-core.ps1'
    $output = & $pwsh -NoLogo -NoProfile -NonInteractive -File $sync -PluginRoot $Root 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0) { throw "sync-plugin-core.ps1 exited $LASTEXITCODE staging ${Root}: $output" }
    $wrappers = Join-Path $RepoRoot 'plugins/core/hooks-templates/generate-wrappers.ps1'
    $output = & $pwsh -NoLogo -NoProfile -NonInteractive -File $wrappers -HostName 'claude-code' -PluginRoot $Root 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0) { throw "generate-wrappers.ps1 exited $LASTEXITCODE staging ${Root}: $output" }
    return $Root
}

function Write-TestPwshExecutable {
    <#
    .SYNOPSIS
        Writes a PowerShell fake that starts as an executable: on Windows a .cmd launcher (Name.cmd) of Name.fake.ps1, so a lookup of Name
        finds only the launcher; on Linux a pwsh shebang script named Name with its execute bit set through .NET. No bash. Returns the path
        to start.
    #>
    param(
        [Parameter(Mandatory)][string]$Directory,
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][string]$Body
    )

    [void][System.IO.Directory]::CreateDirectory($Directory)
    $utf8 = [System.Text.UTF8Encoding]::new($false)
    if ($IsWindows) {
        [System.IO.File]::WriteAllText((Join-Path $Directory "$Name.fake.ps1"), $Body, $utf8)
        $pwsh = (Get-Command pwsh -ErrorAction Stop).Source
        $launcher = Join-Path $Directory "$Name.cmd"
        $lines = @('@echo off', ('"{0}" -NoLogo -NoProfile -NonInteractive -File "%~dp0{1}.fake.ps1" %*' -f $pwsh, $Name), 'exit /b %ERRORLEVEL%')
        [System.IO.File]::WriteAllText($launcher, (($lines -join "`r`n") + "`r`n"), [System.Text.Encoding]::ASCII)
        return $launcher
    }

    $path = Join-Path $Directory $Name
    [System.IO.File]::WriteAllText($path, ('#!/usr/bin/env -S pwsh -NoLogo -NoProfile -NonInteractive -File' + "`n" + $Body), $utf8)
    [System.IO.File]::SetUnixFileMode($path, [System.IO.UnixFileMode]'UserRead, UserWrite, UserExecute, GroupRead, GroupExecute, OtherRead, OtherExecute')
    return $path
}

function Initialize-TestHookCache {
    <#
    .SYNOPSIS
        TR-MCP-PLUGIN-013 AC-005 (PLAN-SESSIONTRIAGEFIX-001 D3): a temporary workspace with its own test marker and a root plugin cache whose
        session-state.yaml is verified, fresh and bound to that marker, so the plugin's freshness checks pass without a server; plus the
        root's current-turn.yaml when RootTurnId is given. Never a live marker or the live server. Returns the paths and ids.
    #>
    param(
        [Parameter(Mandatory)][string]$LibRoot,
        [Parameter(Mandatory)][string]$Root,
        [string]$SessionId = 'ClaudeCode-20261015T000000Z-plugin-session',
        [string]$RootAgentSessionId = 'root-host-session',
        [string]$RootTurnId = '',
        [string]$RootTurnStatus = 'in_progress'
    )

    . (Join-Path $LibRoot 'yaml-object-mutation.ps1')
    Import-McpYamlSerializer
    $workspace = Join-Path $Root 'workspace'
    $cache = Join-Path $Root 'cache'
    [void][System.IO.Directory]::CreateDirectory($workspace)
    [void][System.IO.Directory]::CreateDirectory($cache)
    $marker = Join-Path $workspace 'AGENTS-README-FIRST.yaml'
    [System.IO.File]::WriteAllText($marker, "workspace: d3-background-test`n", [System.Text.UTF8Encoding]::new($false))
    $markerUtc = (Get-Item -LiteralPath $marker).LastWriteTimeUtc.ToString('O')
    $now = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
    Write-McpYamlObject -Path (Join-Path $cache 'session-state.yaml') -Document ([ordered]@{
            status = 'verified'
            sessionId = $SessionId
            agent = 'ClaudeCode'
            title = 'root session'
            started = $now
            lastUpdated = $now
            markerFilePath = $marker
            markerLastWriteUtc = $markerUtc
            agentSessionId = $RootAgentSessionId
        })
    if ($RootTurnId) {
        Write-McpYamlObject -Path (Join-Path $cache 'current-turn.yaml') -Document ([ordered]@{
                turnRequestId = $RootTurnId
                queryTitle = 'root work'
                queryText = 'Please remediate the root work'
                openedAt = $now
                status = $RootTurnStatus
                sessionId = $SessionId
                markerFilePath = $marker
                markerLastWriteUtc = $markerUtc
            })
    }
    return [pscustomobject]@{
        Root = $Root
        Workspace = $workspace
        Cache = $cache
        Marker = $marker
        SessionId = $SessionId
        RootAgentSessionId = $RootAgentSessionId
        RootTurnId = $RootTurnId
        PersistLog = (Join-Path $Root 'persist.jsonl')
    }
}

function Invoke-TestPluginHook {
    <#
    .SYNOPSIS
        Runs the shipped plugin-hook.ps1 for HookName as its own pwsh process against a cache from Initialize-TestHookCache: the cache folder through
        MCP_CACHE_DIR_OVERRIDE, durable writes through the persist seam (MCP_PLUGIN_PERSIST_LOG, no server), the failsafe drain disabled,
        the payload as JSON on stdin, and Environment's extra variables. Returns the exit code, stdout and stderr.
    #>
    param(
        [Parameter(Mandatory)][string]$LibRoot,
        [Parameter(Mandatory)]$Cache,
        [Parameter(Mandatory)][string]$HookName,
        [Parameter(Mandatory)][System.Collections.IDictionary]$Payload,
        [System.Collections.IDictionary]$Environment = @{}
    )

    $psi = [System.Diagnostics.ProcessStartInfo]::new()
    $psi.FileName = (Get-Command pwsh -ErrorAction Stop).Source
    foreach ($argument in @('-NoLogo', '-NoProfile', '-NonInteractive', '-File', (Join-Path $LibRoot 'plugin-hook.ps1'), '-HookName', $HookName, '-WorkspacePath', $Cache.Workspace, '-HostName', 'claude-code')) {
        $psi.ArgumentList.Add($argument)
    }
    $psi.WorkingDirectory = $Cache.Workspace
    $psi.UseShellExecute = $false
    $psi.RedirectStandardInput = $true
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    foreach ($name in @('MCP_SUBAGENT_ID', 'GROK_SUBAGENT_ID', 'MCP_PLUGIN_REPL_LOG', 'MCP_PLUGIN_REPL_RESPONSE', 'CLAUDE_STOP_HOOK_ACTIVE')) {
        [void]$psi.Environment.Remove($name)
    }
    $psi.Environment['MCP_PLUGIN_HOST'] = 'claude-code'
    $psi.Environment['MCP_AGENT_NAME'] = 'ClaudeCode'
    $psi.Environment['MCP_CACHE_DIR_OVERRIDE'] = $Cache.Cache
    $psi.Environment['MCP_PLUGIN_PERSIST_LOG'] = $Cache.PersistLog
    $psi.Environment['MCP_FAILSAFE_DRAIN_DISABLED'] = '1'
    $psi.Environment['MCP_WORKSPACE_PATH'] = $Cache.Workspace
    $psi.Environment['MCPSERVER_WORKSPACE_PATH'] = $Cache.Workspace
    foreach ($key in $Environment.Keys) { $psi.Environment[[string]$key] = [string]$Environment[$key] }
    $process = [System.Diagnostics.Process]::Start($psi)
    $process.StandardInput.Write(($Payload | ConvertTo-Json -Compress))
    $process.StandardInput.Close()
    $stdout = $process.StandardOutput.ReadToEndAsync()
    $stderr = $process.StandardError.ReadToEndAsync()
    if (-not $process.WaitForExit(120000)) {
        $process.Kill($true)
        throw "plugin-hook.ps1 $HookName did not exit within 120 s"
    }
    return [pscustomobject]@{ ExitCode = $process.ExitCode; Stdout = $stdout.Result; Stderr = $stderr.Result }
}

function Read-TestTurnState {
    <#
    .SYNOPSIS
        The turnRequestId, status and sessionId of a current-turn.yaml; Exists false and empty values when the file is absent.
    #>
    param([Parameter(Mandatory)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) { return [pscustomobject]@{ Exists = $false; Id = ''; Status = ''; SessionId = '' } }
    $text = [System.IO.File]::ReadAllText($Path)
    $fields = @{}
    foreach ($name in @('turnRequestId', 'status', 'sessionId')) {
        $match = [regex]::Match($text, "(?m)^$($name):\s*(.+)$")
        $fields[$name] = if ($match.Success) { $match.Groups[1].Value.Trim().Trim('"', "'") } else { '' }
    }
    return [pscustomobject]@{ Exists = $true; Id = $fields['turnRequestId']; Status = $fields['status']; SessionId = $fields['sessionId'] }
}

function Read-TestPersistLog {
    <#
    .SYNOPSIS
        The records the persist seam (MCP_PLUGIN_PERSIST_LOG) wrote, one object per JSON line; empty when the file is absent.
    #>
    param([Parameter(Mandatory)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) { return @() }
    return @([System.IO.File]::ReadAllLines($Path) | Where-Object { $_ } | ForEach-Object { $_ | ConvertFrom-Json })
}

function Test-BackgroundTurnOutcome {
    <#
    .SYNOPSIS
        TR-MCP-PLUGIN-013 AC-005 (D3): the violations in one background-brief UserPromptSubmit outcome; none means the outcome meets the
        criterion. Outcome: RootSessionId; RootBefore and RootAfter (Id, Status); Scope (Id, Status, SessionId), $null when no background
        turn exists; Persist (persist-seam records); HookStatus; HookIsolation.
    #>
    param([Parameter(Mandatory)]$Outcome)

    $violations = [System.Collections.Generic.List[string]]::new()
    $scope = $Outcome.Scope
    if ($null -eq $scope -or [string]::IsNullOrWhiteSpace([string]$scope.Id)) {
        $violations.Add('no background turn was opened')
    } else {
        if ([string]$scope.Status -ne 'in_progress') { $violations.Add("background turn status is '$($scope.Status)'") }
        if ([string]$scope.Id -eq [string]$Outcome.RootBefore.Id) { $violations.Add('background turn reuses the root turn id') }
        if ([string]$scope.SessionId -ne [string]$Outcome.RootSessionId) { $violations.Add("background turn is in session '$($scope.SessionId)'") }
        $first = @($Outcome.Persist | Where-Object { [string]$_.requestId -eq [string]$scope.Id }) | Select-Object -First 1
        if ($null -eq $first) {
            $violations.Add('background turn has no durable write')
        } else {
            if ([string]$first.status -ne 'in_progress') { $violations.Add("first background write has status '$($first.status)'") }
            $context = @($first.contextList | ForEach-Object { [string]$_ })
            if ($context -notcontains "root session: $($Outcome.RootSessionId)") { $violations.Add('first background write lacks the root session id') }
            if (-not [string]::IsNullOrWhiteSpace([string]$Outcome.RootBefore.Id) -and $context -notcontains "root turn: $($Outcome.RootBefore.Id)") {
                $violations.Add('first background write lacks the root turn id')
            }
            if (@($first.tags | ForEach-Object { [string]$_ }) -notcontains 'background-agent') { $violations.Add('first background write lacks the background-agent tag') }
        }
    }
    if ([string]$Outcome.RootAfter.Id -ne [string]$Outcome.RootBefore.Id -or [string]$Outcome.RootAfter.Status -ne [string]$Outcome.RootBefore.Status) {
        $violations.Add("root current-turn changed from $($Outcome.RootBefore.Id)/$($Outcome.RootBefore.Status) to $($Outcome.RootAfter.Id)/$($Outcome.RootAfter.Status)")
    }
    if (@($Outcome.Persist | Where-Object { [string]$_.requestId -eq [string]$Outcome.RootBefore.Id -and [string]$_.status -eq 'canceled' }).Count -gt 0) {
        $violations.Add('root turn was canceled')
    }
    if ([string]$Outcome.HookIsolation -in @('reuse', 'isolate-skip') -or [string]$Outcome.HookStatus -in @('root-turn-isolated', 'turn-already-open')) {
        $violations.Add("hook reused or skipped ($($Outcome.HookStatus) $($Outcome.HookIsolation))")
    }
    return $violations.ToArray()
}

function Test-RootPromptOutcome {
    <#
    .SYNOPSIS
        TR-MCP-PLUGIN-013 AC-005 (D3): the violations in the outcome of a brief whose identity equals the root's; it is the root host's own
        prompt and opens a root turn as an operator prompt does. Outcome: RootSessionId; RootBefore (Id, Status); RootAfter (Id, Status,
        SessionId); ScopeFolders (background folders that exist afterwards); Persist.
    #>
    param([Parameter(Mandatory)]$Outcome)

    $violations = [System.Collections.Generic.List[string]]::new()
    if ([string]::IsNullOrWhiteSpace([string]$Outcome.RootAfter.Id) -or [string]$Outcome.RootAfter.Id -eq [string]$Outcome.RootBefore.Id) { $violations.Add('no new root turn was opened') }
    if ([string]$Outcome.RootAfter.Status -ne 'in_progress') { $violations.Add("new root turn status is '$($Outcome.RootAfter.Status)'") }
    if ([string]$Outcome.RootAfter.SessionId -ne [string]$Outcome.RootSessionId) { $violations.Add("new root turn is in session '$($Outcome.RootAfter.SessionId)'") }
    if ([int]$Outcome.ScopeFolders -gt 0) { $violations.Add('a background folder was created for a brief with the root identity') }
    if ([string]$Outcome.RootBefore.Status -eq 'in_progress' -and
        @($Outcome.Persist | Where-Object { [string]$_.requestId -eq [string]$Outcome.RootBefore.Id -and [string]$_.status -eq 'canceled' }).Count -eq 0) {
        $violations.Add('the previous in-progress root turn was not superseded')
    }
    return $violations.ToArray()
}

function Test-BackgroundStopOutcome {
    <#
    .SYNOPSIS
        TR-MCP-PLUGIN-013 AC-005 (D3): the violations in a background host's Stop outcome; it completes its own in-progress turn and never
        the root's. Outcome: RootBefore, RootAfter, ScopeBefore, ScopeAfter (Id, Status); Persist.
    #>
    param([Parameter(Mandatory)]$Outcome)

    $violations = [System.Collections.Generic.List[string]]::new()
    if ([string]$Outcome.ScopeAfter.Id -ne [string]$Outcome.ScopeBefore.Id) { $violations.Add("background turn changed from $($Outcome.ScopeBefore.Id) to $($Outcome.ScopeAfter.Id)") }
    if ([string]$Outcome.ScopeAfter.Status -ne 'completed') { $violations.Add("background turn status is '$($Outcome.ScopeAfter.Status)'") }
    if (@($Outcome.Persist | Where-Object { [string]$_.requestId -eq [string]$Outcome.ScopeBefore.Id -and [string]$_.status -eq 'completed' }).Count -eq 0) {
        $violations.Add('the background completion was not written')
    }
    if ([string]$Outcome.RootAfter.Id -ne [string]$Outcome.RootBefore.Id -or [string]$Outcome.RootAfter.Status -ne [string]$Outcome.RootBefore.Status) {
        $violations.Add("root current-turn changed from $($Outcome.RootBefore.Id)/$($Outcome.RootBefore.Status) to $($Outcome.RootAfter.Id)/$($Outcome.RootAfter.Status)")
    }
    if (@($Outcome.Persist | Where-Object { [string]$_.requestId -eq [string]$Outcome.RootBefore.Id }).Count -gt 0) { $violations.Add('the background Stop wrote the root turn') }
    return $violations.ToArray()
}

function Get-TestBackgroundScopeCase {
    <#
    .SYNOPSIS
        TR-MCP-PLUGIN-013 AC-005 (D3): pinned host identities and the scope key each must give (MCP_SUBAGENT_ID, else GROK_SUBAGENT_ID, else
        the payload session_id; none when empty or equal to the root's recorded agentSessionId).
    #>
    return @(
        [pscustomobject]@{ Name = 'subagent id first'; Mcp = 'sub-1'; Grok = 'grok-7'; Payload = 'host-2'; Root = 'host-1'; Expected = 'sub-1' }
        [pscustomobject]@{ Name = 'grok id without a subagent id'; Mcp = ''; Grok = 'grok-7'; Payload = 'host-2'; Root = 'host-1'; Expected = 'grok-7' }
        [pscustomobject]@{ Name = 'payload session id without an environment id'; Mcp = ''; Grok = ''; Payload = 'host-2'; Root = 'host-1'; Expected = 'host-2' }
        [pscustomobject]@{ Name = 'payload session id equal to the root'; Mcp = ''; Grok = ''; Payload = 'host-1'; Root = 'host-1'; Expected = '' }
        [pscustomobject]@{ Name = 'no identity'; Mcp = ''; Grok = ''; Payload = ''; Root = 'host-1'; Expected = '' }
        [pscustomobject]@{ Name = 'root without a recorded host id'; Mcp = ''; Grok = ''; Payload = 'host-2'; Root = ''; Expected = 'host-2' }
        [pscustomobject]@{ Name = 'environment id equal to the root'; Mcp = 'host-1'; Grok = ''; Payload = 'host-2'; Root = 'host-1'; Expected = '' }
    )
}

function Get-TestBackgroundRoutingCase {
    <#
    .SYNOPSIS
        TR-MCP-PLUGIN-013 AC-005 (D3): pinned cases for where a session-log verb the agent calls itself resolves its cache: an explicit
        MCP_CACHE_DIR_OVERRIDE first; with MCP_SUBAGENT_ID or GROK_SUBAGENT_ID set and that agent's background folder holding a
        current-turn.yaml, the folder; otherwise the root cache.
    #>
    return @(
        [pscustomobject]@{ Name = 'explicit override wins'; Override = $true; Mcp = 'sub-1'; Grok = ''; ScopeExists = $true; Expected = 'override' }
        [pscustomobject]@{ Name = 'subagent id with its folder'; Override = $false; Mcp = 'sub-1'; Grok = ''; ScopeExists = $true; Expected = 'scope' }
        [pscustomobject]@{ Name = 'grok id with its folder'; Override = $false; Mcp = ''; Grok = 'grok-7'; ScopeExists = $true; Expected = 'scope' }
        [pscustomobject]@{ Name = 'subagent id without a folder'; Override = $false; Mcp = 'sub-1'; Grok = ''; ScopeExists = $false; Expected = 'root' }
        [pscustomobject]@{ Name = 'no environment id'; Override = $false; Mcp = ''; Grok = ''; ScopeExists = $true; Expected = 'root' }
    )
}
