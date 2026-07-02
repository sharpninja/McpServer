#Requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet('session-start', 'session-end', 'pre-compact', 'post-compact', 'user-prompt-submit', 'stop-gate', 'code-verify', 'plan-approved', 'plan-modified', 'cache-flush', 'health-check', 'subagent-import')]
    [string]$HookName,

    [string]$HostName = $(if ($env:MCP_PLUGIN_HOST) { $env:MCP_PLUGIN_HOST } else { 'codex' }),

    [ValidateSet('flat', 'scoped')]
    [string]$CacheMode = 'scoped',

    [string]$WorkspacePath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script:ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$env:MCP_PLUGIN_HOST = $HostName
. (Join-Path $script:ScriptDir 'plugin-env.ps1')
. (Join-Path $script:ScriptDir 'resolve-cache-dir.ps1')
. (Join-Path $script:ScriptDir 'marker-resolver.ps1')
. (Join-Path $script:ScriptDir 'yaml-object-mutation.ps1')
Import-McpYamlSerializer

function ConvertTo-PluginParamsYaml {
    param([Parameter(Mandatory)]$Params)

    return (ConvertTo-Yaml -Data $Params -Options WithIndentedSequences)
}

function New-PluginSessionId {
    param([Parameter(Mandatory)][string]$AgentName)

    if ($env:MCP_SESSION_ID) { return $env:MCP_SESSION_ID }

    $timestamp = (Get-Date).ToUniversalTime().ToString('yyyyMMddTHHmmssZ')
    $suffix = if ($env:MCP_SESSION_SUFFIX) { $env:MCP_SESSION_SUFFIX } else { 'plugin-session' }
    $suffix = ($suffix.ToLowerInvariant() -replace '[^a-z0-9]+', '-').Trim('-')
    if (-not $suffix) { $suffix = 'plugin-session' }

    return '{0}-{1}-{2}' -f $AgentName, $timestamp, $suffix
}

function Write-PluginJson {
    param([Parameter(Mandatory)]$Value)
    $Value | ConvertTo-Json -Depth 20 -Compress
}

function Stop-PluginUnavailable {
    $agent = if ($env:MCP_AGENT_NAME) { $env:MCP_AGENT_NAME } else { 'Agent' }
    Write-Output "MCP_PLUGIN_UNAVAILABLE:$agent"
    exit 0
}

function Confirm-PowerShellMcpRuntime {
    if ($env:MCP_PLUGIN_REFUSE_POWERSHELL -eq '1') {
        return $false
    }

    try {
        if (-not (Get-Module -ListAvailable -Name PowerShell.MCP)) {
            if (Get-Command Install-PSResource -ErrorAction SilentlyContinue) {
                Install-PSResource -Name PowerShell.MCP -Scope CurrentUser -TrustRepository -Quiet -ErrorAction Stop
            } else {
                Install-Module -Name PowerShell.MCP -Repository PSGallery -Scope CurrentUser -Force -AllowClobber -ErrorAction Stop
            }
        }

        Import-Module PowerShell.MCP -ErrorAction Stop
        return $true
    } catch {
        return $false
    }
}

function Get-PluginCacheDir {
    $cacheDir = Resolve-McpCacheDir
    if (-not (Test-Path -LiteralPath $cacheDir)) {
        [void][System.IO.Directory]::CreateDirectory($cacheDir)
    }
    return $cacheDir
}

function Get-PluginStartPath {
    param([string]$PreferredPath)

    if ($PreferredPath) { return $PreferredPath }
    if ($WorkspacePath) { return $WorkspacePath }
    if ($env:MCP_WORKSPACE_PATH) { return $env:MCP_WORKSPACE_PATH }
    if ($env:MCPSERVER_WORKSPACE_PATH) { return $env:MCPSERVER_WORKSPACE_PATH }
    if ($env:MCP_WORKSPACE_START_DIR) { return $env:MCP_WORKSPACE_START_DIR }
    if ($env:CLAUDE_PROJECT_DIR) { return $env:CLAUDE_PROJECT_DIR }
    return (Get-Location).ProviderPath
}

function Get-YamlScalar {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$Key
    )

    if (-not (Test-Path -LiteralPath $Path)) { return $null }
    $match = Select-String -LiteralPath $Path -Pattern "^$([regex]::Escape($Key)):\s*(.*)$" | Select-Object -First 1
    if (-not $match) { return $null }
    return $match.Matches[0].Groups[1].Value.Trim().Trim('"')
}

function Set-YamlScalar {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$Key,
        [Parameter(Mandatory)][string]$Value
    )

    if (-not (Test-Path -LiteralPath $Path)) { return }
    $text = [System.IO.File]::ReadAllText($Path)
    $pattern = "(?m)^$([regex]::Escape($Key)):\s*.*$"
    if ($text -match $pattern) {
        $text = [regex]::Replace($text, $pattern, "${Key}: $Value")
    } else {
        $text = $text.TrimEnd() + "`n${Key}: $Value`n"
    }
    [System.IO.File]::WriteAllText($Path, $text)
}

function Read-HookInput {
    if ([Console]::IsInputRedirected) {
        return [Console]::In.ReadToEnd()
    }
    return ''
}

function Get-HookPayloadValue {
    param(
        [string]$Payload,
        [string]$Name
    )

    if (-not $Payload) { return '' }
    try {
        $json = $Payload | ConvertFrom-Json -Depth 20 -ErrorAction Stop
        return [string]$json.$Name
    } catch {
        return ''
    }
}

function Invoke-PluginRepl {
    param(
        [Parameter(Mandatory)][string]$Method,
        [string]$ParamsYaml = ''
    )

    if ($env:MCP_PLUGIN_REPL_LOG) {
        $entry = @(
            "method: $Method"
            'params: |'
            (($ParamsYaml -replace "`r`n", "`n" -replace "`r", "`n") -split "`n" | ForEach-Object { "  $_" })
            '---'
        ) -join "`n"
        Add-Content -LiteralPath $env:MCP_PLUGIN_REPL_LOG -Value $entry
        if ($env:MCP_PLUGIN_REPL_RESPONSE) {
            Write-Output $env:MCP_PLUGIN_REPL_RESPONSE
        }
        return
    }

    & (Join-Path $script:ScriptDir 'repl-invoke.ps1') -Method $Method -ParamsYaml $ParamsYaml
}

function Start-PluginSession {
    param([string]$StartPath)

    $start = Get-PluginStartPath -PreferredPath $StartPath
    $cacheDir = Get-PluginCacheDir
    $sessionFile = Join-Path $cacheDir 'session-state.yaml'
    $markerSnapshot = $null
    try {
        $markerSnapshot = Get-MarkerFileSnapshot -StartDir $start
    } catch {
        $markerSnapshot = $null
    }

    $verified = $false
    try {
        $verified = Invoke-FullBootstrap -StartDir $start
    } catch {
        $verified = $false
    }
    if (-not $verified) {
        $untrustedState = [ordered]@{
            status = 'MCP_UNTRUSTED'
            lastUpdated = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
        }
        if ($markerSnapshot) {
            $untrustedState['markerFilePath'] = $markerSnapshot.markerFilePath
            $untrustedState['markerLastWriteUtc'] = $markerSnapshot.markerLastWriteUtc
        }
        Write-McpYamlObject -Path $sessionFile -Document $untrustedState
        Write-PluginJson ([ordered]@{})
        return
    }

    if (-not $markerSnapshot) {
        $markerSnapshot = Get-MarkerFileSnapshot -StartDir $start
    }

    $sessionId = New-PluginSessionId -AgentName $env:MCP_AGENT_NAME
    $now = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
    $sessionState = [ordered]@{
        status = 'verified'
        sessionId = $sessionId
        agent = $env:MCP_AGENT_NAME
        started = $now
        lastUpdated = $now
        markerFilePath = $markerSnapshot.markerFilePath
        markerLastWriteUtc = $markerSnapshot.markerLastWriteUtc
    }
    Write-McpYamlObject -Path $sessionFile -Document $sessionState
    Write-PluginJson ([ordered]@{})
}

function Ensure-PluginMarkerFresh {
    param([string]$StartPath)

    $start = Get-PluginStartPath -PreferredPath $StartPath
    $cacheDir = Get-PluginCacheDir
    $sessionFile = Join-Path $cacheDir 'session-state.yaml'

    try {
        if (-not (Test-Path -LiteralPath $sessionFile)) {
            Start-PluginSession -StartPath $start | Out-Null
        }

        if (-not (Test-Path -LiteralPath $sessionFile)) {
            return $false
        }

        $state = Read-McpYamlObject -Path $sessionFile
        if ([string]$state['status'] -ne 'verified') {
            return $false
        }

        $snapshot = Get-MarkerFileSnapshot -StartDir $start
        $cachedPath = [string]$state['markerFilePath']
        $cachedWriteUtc = [string]$state['markerLastWriteUtc']

        if ($cachedPath -ne $snapshot.markerFilePath -or $cachedWriteUtc -ne $snapshot.markerLastWriteUtc) {
            Start-PluginSession -StartPath $start | Out-Null
            $state = Read-McpYamlObject -Path $sessionFile
        }

        return ([string]$state['status'] -eq 'verified')
    } catch {
        $untrustedState = [ordered]@{
            status = 'MCP_UNTRUSTED'
            lastUpdated = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
        }
        try {
            $snapshot = Get-MarkerFileSnapshot -StartDir $start
            $untrustedState['markerFilePath'] = $snapshot.markerFilePath
            $untrustedState['markerLastWriteUtc'] = $snapshot.markerLastWriteUtc
        } catch {
        }
        Write-McpYamlObject -Path $sessionFile -Document $untrustedState
        return $false
    }
}

function Open-PluginTurn {
    $cacheDir = Get-PluginCacheDir
    $sessionFile = Join-Path $cacheDir 'session-state.yaml'
    $startPath = Get-PluginStartPath -PreferredPath $WorkspacePath
    Ensure-PluginMarkerFresh -StartPath $startPath | Out-Null
    if (Test-Path -LiteralPath $sessionFile) {
        $timestampText = Get-YamlScalar -Path $sessionFile -Key 'timestamp'
        if (-not $timestampText) { $timestampText = Get-YamlScalar -Path $sessionFile -Key 'lastUpdated' }
        if ($timestampText) {
            $parsedTimestamp = [datetime]::MinValue
            if ([datetime]::TryParse($timestampText.Trim('"'), [ref]$parsedTimestamp) -and $parsedTimestamp.ToUniversalTime() -lt (Get-Date).ToUniversalTime().AddHours(-24)) {
                Set-YamlScalar -Path $sessionFile -Key 'lastUpdated' -Value $((Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ'))
            }
        }
    }

    if ((Get-YamlScalar -Path $sessionFile -Key 'status') -ne 'verified') {
        Write-PluginJson ([ordered]@{ hookSpecificOutput = [ordered]@{ hookEventName = 'UserPromptSubmit'; status = 'no-session' } })
        return
    }

    $payload = Read-HookInput
    $prompt = Get-HookPayloadValue -Payload $payload -Name 'prompt'
    if (-not $prompt) { $prompt = 'User prompt' }
    $title = (($prompt -split "`r?`n")[0]).Trim()
    if (-not $title) { $title = 'User prompt' }
    if ($title.Length -gt 60) { $title = $title.Substring(0, 60) }

    $turnRequestId = 'req-{0}-prompt-{1:x4}' -f (Get-Date).ToUniversalTime().ToString('yyyyMMddTHHmmssZ'), (Get-Random -Maximum 0xffff)
    $paramsYaml = ConvertTo-PluginParamsYaml ([ordered]@{
        requestId = $turnRequestId
        queryTitle = $title
        queryText = $prompt
    })
    Invoke-PluginRepl -Method 'workflow.sessionlog.beginTurn' -ParamsYaml $paramsYaml | Out-Null

    $turnFile = Join-Path $cacheDir 'current-turn.yaml'
    $turnState = [ordered]@{
        turnRequestId = $turnRequestId
        queryTitle = $title
        openedAt = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
        status = 'in_progress'
        codeEdits = 0
        lastBuildStatus = 'unknown'
        auditActions = 0
        auditFiles = 0
        auditDialog = 0
        auditDecisions = 0
        queryText = $prompt
    }
    [System.IO.File]::WriteAllText($turnFile, (ConvertTo-PluginParamsYaml $turnState))

    Write-PluginJson ([ordered]@{
        hookSpecificOutput = [ordered]@{
            hookEventName = 'UserPromptSubmit'
            status = 'turn-opened'
            turnRequestId = $turnRequestId
            additionalContext = if ($env:MCP_CODEX_INTERNAL_TODO -eq '1' -or $env:MCPSERVER_CODEX_INTERNAL_TODO -eq '1' -or $env:CODEX_MCP_TODO -eq '1') {
                "session log turn $turnRequestId is now active. MCP-backed internal TODO tracking is enabled. Continue the current task after any incidental triage submission."
            } else {
                "session log turn $turnRequestId is now active. Continue the current task after any incidental triage submission."
            }
        }
    })
}

function Close-PluginTurnIfNeeded {
    $cacheDir = Get-PluginCacheDir
    $turnFile = Join-Path $cacheDir 'current-turn.yaml'
    if ($env:CLAUDE_STOP_HOOK_ACTIVE -eq 'true') {
        Write-PluginJson ([ordered]@{})
        return
    }

    $sessionFile = Join-Path $cacheDir 'session-state.yaml'
    if (Test-Path -LiteralPath $sessionFile) {
        $timestampText = Get-YamlScalar -Path $sessionFile -Key 'timestamp'
        if (-not $timestampText) { $timestampText = Get-YamlScalar -Path $sessionFile -Key 'lastUpdated' }
        if ($timestampText) {
            $parsedTimestamp = [datetime]::MinValue
            if ([datetime]::TryParse($timestampText.Trim('"'), [ref]$parsedTimestamp)) {
                if ($parsedTimestamp.ToUniversalTime() -lt (Get-Date).ToUniversalTime().AddHours(-24)) {
                    $requestId = if (Test-Path -LiteralPath $turnFile) { Get-YamlScalar -Path $turnFile -Key 'turnRequestId' } else { '' }
                    Write-PluginJson ([ordered]@{ decision = 'block'; reason = "stale cached session cannot be reused for $requestId" })
                    return
                }
            }
        }
    }

    if (-not (Test-Path -LiteralPath $turnFile)) {
        Write-PluginJson ([ordered]@{})
        return
    }

    $status = Get-YamlScalar -Path $turnFile -Key 'status'
    if ($status -eq 'in_progress') {
        if ($env:MCP_STOP_GATE_COMPLETE_TIMEOUT_SECONDS -and $env:MCP_STOP_GATE_FORCE_TIMEOUT -eq '1') {
            Write-PluginJson ([ordered]@{ decision = 'block'; reason = "in_progress turn could not be auto-closed within $($env:MCP_STOP_GATE_COMPLETE_TIMEOUT_SECONDS)s" })
            return
        }

        $response = 'Auto-closed by PowerShell stop gate.'
        $paramsYaml = ConvertTo-PluginParamsYaml ([ordered]@{
            response = $response
        })
        Invoke-PluginRepl -Method 'workflow.sessionlog.completeTurn' -ParamsYaml $paramsYaml | Out-Null
        Set-YamlScalar -Path $turnFile -Key 'status' -Value 'completed'
    }

    $edits = [int]((Get-YamlScalar -Path $turnFile -Key 'codeEdits') ?? '0')
    $buildStatus = Get-YamlScalar -Path $turnFile -Key 'lastBuildStatus'
    if ($edits -gt 0 -and $buildStatus -eq 'failed') {
        $acceptFailure = Join-Path $cacheDir 'turn-accept-failure.marker'
        if (Test-Path -LiteralPath $acceptFailure) {
            Remove-Item -LiteralPath $acceptFailure -Force
            Write-PluginJson ([ordered]@{})
            return
        }

        Write-PluginJson ([ordered]@{ decision = 'block'; reason = "Last build in this turn failed after $edits code edit(s)." })
        return
    }

    $auditActions = Get-YamlScalar -Path $turnFile -Key 'auditActions'
    $auditFiles = Get-YamlScalar -Path $turnFile -Key 'auditFiles'
    $auditDialog = Get-YamlScalar -Path $turnFile -Key 'auditDialog'
    $auditDecisions = Get-YamlScalar -Path $turnFile -Key 'auditDecisions'
    $hasAuditSchema = $null -ne $auditActions -or $null -ne $auditFiles -or $null -ne $auditDialog -or $null -ne $auditDecisions
    if ($edits -gt 0 -and $hasAuditSchema) {
        $auditTotal = [int](($auditActions ?? '0')) + [int](($auditFiles ?? '0')) + [int](($auditDialog ?? '0')) + [int](($auditDecisions ?? '0'))
        if ($auditTotal -le 0) {
            $acceptAudit = Join-Path $cacheDir 'turn-accept-incomplete-audit.marker'
            if (Test-Path -LiteralPath $acceptAudit) {
                Remove-Item -LiteralPath $acceptAudit -Force
                Write-PluginJson ([ordered]@{})
                return
            }

            Write-PluginJson ([ordered]@{ decision = 'block'; reason = 'audit is incomplete for a completed code-edit turn' })
            return
        }
    }

    Write-PluginJson ([ordered]@{})
}

function Invoke-CodeVerify {
    $cacheDir = Get-PluginCacheDir
    $turnFile = Join-Path $cacheDir 'current-turn.yaml'
    $payload = Read-HookInput
    $filePath = Get-HookPayloadValue -Payload $payload -Name 'file_path'
    if (-not $filePath) {
        try {
            $json = $payload | ConvertFrom-Json -Depth 20 -ErrorAction Stop
            $filePath = [string]$json.tool_input.file_path
        } catch {
        }
    }

    if (-not $filePath -or -not (Test-Path -LiteralPath $filePath)) {
        Write-PluginJson ([ordered]@{ status = 'skipped'; reason = 'no-file' })
        return
    }

    $dir = Split-Path -Parent (Resolve-Path -LiteralPath $filePath).ProviderPath
    $project = $null
    while ($dir) {
        $project = Get-ChildItem -LiteralPath $dir -Filter '*.csproj' -File -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($project) { break }
        $parent = Split-Path -Parent $dir
        if ($parent -eq $dir) { break }
        $dir = $parent
    }

    $buildStatus = 'succeeded'
    if ($project) {
        $buildLog = Join-Path $cacheDir 'last-build.log'
        $output = & dotnet build $project.FullName --nologo -clp:NoSummary 2>&1
        $exitCode = $LASTEXITCODE
        [System.IO.File]::WriteAllText($buildLog, ($output | Out-String))
        if ($exitCode -ne 0) { $buildStatus = 'failed' }
    }

    if (Test-Path -LiteralPath $turnFile) {
        Set-YamlScalar -Path $turnFile -Key 'lastBuildStatus' -Value $buildStatus
    }

    Write-PluginJson ([ordered]@{ status = $buildStatus })
}

function Invoke-CacheFlushHook {
    $result = & (Join-Path $script:ScriptDir 'cache-manager.ps1') -Action flush
    if ($HookName -eq 'cache-flush') {
        Write-Output $result
    } else {
        Write-PluginJson ([ordered]@{})
    }
}

function Get-PlanFilePathFromInput {
    $payload = Read-HookInput
    $filePath = Get-HookPayloadValue -Payload $payload -Name 'file_path'
    if (-not $filePath) {
        try {
            $json = $payload | ConvertFrom-Json -Depth 20 -ErrorAction Stop
            $filePath = [string]$json.tool_input.file_path
            if (-not $filePath) { $filePath = [string]$json.tool_input.path }
        } catch {
        }
    }
    if (-not $filePath -and $env:TOOL_INPUT) { $filePath = $env:TOOL_INPUT }
    return $filePath
}

function Write-PostToolUseOutput {
    param([string]$Status = 'skipped')

    Write-PluginJson ([ordered]@{
        hookSpecificOutput = [ordered]@{
            hookEventName = 'PostToolUse'
            status = $Status
        }
    })
}

function Get-PlanTitle {
    param([Parameter(Mandatory)][string]$PlanFile)

    if (-not (Test-Path -LiteralPath $PlanFile -PathType Leaf)) { return $null }
    foreach ($line in [System.IO.File]::ReadLines($PlanFile)) {
        if ($line -match '^#\s+(.+)$') {
            return $Matches[1].Trim()
        }
    }

    return [System.IO.Path]::GetFileNameWithoutExtension($PlanFile)
}

function Get-PlanTodoMapPath {
    Join-Path (Get-PluginCacheDir) 'plan-todo-map.yaml'
}

function Get-TodoIdFromReplOutput {
    param([string[]]$Output)

    $text = ($Output -join "`n")
    if ($text -match '(?m)^\s*id:\s*(?<id>\S+)') { return $Matches['id'] }
    if ($text -match '(?m)^\s*todoId:\s*(?<id>\S+)') { return $Matches['id'] }
    return $null
}

function New-PlanTodoId {
    param([Parameter(Mandatory)][string]$Title)

    $slug = ($Title.ToUpperInvariant() -replace '[^A-Z0-9]', '')
    if (-not $slug) { $slug = 'PLAN' }
    if ($slug.Length -gt 40) { $slug = $slug.Substring(0, 40) }
    return "PLAN-$slug-001"
}

function Invoke-PlanApprovedHook {
    $planFile = Get-PlanFilePathFromInput
    if (-not $planFile -or -not (Test-Path -LiteralPath $planFile -PathType Leaf)) {
        Write-PostToolUseOutput -Status 'skipped'
        return
    }

    $title = Get-PlanTitle -PlanFile $planFile
    # client.Todo.CreateAsync(TodoCreateRequest request): the server requires a
    # non-empty params.id, so derive a canonical PLAN-<slug>-001 id from the title.
    $paramsYaml = ConvertTo-PluginParamsYaml -Params ([ordered]@{
        request = [ordered]@{
            id          = New-PlanTodoId -Title $title
            title       = $title
            section     = 'Planning'
            priority    = 'medium'
            description = @("Plan approved from $planFile")
        }
    })
    $output = @(Invoke-PluginRepl -Method 'client.Todo.CreateAsync' -ParamsYaml $paramsYaml)
    $todoId = Get-TodoIdFromReplOutput -Output $output
    if ($todoId) {
        $mapPath = Get-PlanTodoMapPath
        $entry = @(
            'entries:'
            "  - planFile: $planFile"
            "    todoId: $todoId"
        ) -join "`n"
        [System.IO.File]::WriteAllText($mapPath, $entry + "`n")
    }

    Write-PostToolUseOutput -Status 'created'
}

function Find-PlanTodoId {
    param([Parameter(Mandatory)][string]$PlanFile)

    $mapPath = Get-PlanTodoMapPath
    if (-not (Test-Path -LiteralPath $mapPath)) { return $null }
    $lines = [System.IO.File]::ReadAllLines($mapPath)
    for ($i = 0; $i -lt $lines.Length; $i++) {
        if ($lines[$i] -match '^\s*-\s*planFile:\s*(.+)$' -and $Matches[1].Trim().Trim('"') -eq $PlanFile) {
            for ($j = $i + 1; $j -lt [Math]::Min($lines.Length, $i + 4); $j++) {
                if ($lines[$j] -match '^\s*todoId:\s*(.+)$') {
                    return $Matches[1].Trim().Trim('"')
                }
            }
        }
    }

    return $null
}

function Invoke-PlanModifiedHook {
    $planFile = Get-PlanFilePathFromInput
    if (-not $planFile) {
        Write-PostToolUseOutput -Status 'skipped'
        return
    }

    $todoId = Find-PlanTodoId -PlanFile $planFile
    if (-not $todoId) {
        Write-PostToolUseOutput -Status 'skipped'
        return
    }

    # client.Todo.UpdateAsync(string id, TodoUpdateRequest request)
    $paramsYaml = ConvertTo-PluginParamsYaml -Params ([ordered]@{
        id      = $todoId
        request = [ordered]@{ doneSummary = "Plan modified: $planFile" }
    })
    Invoke-PluginRepl -Method 'client.Todo.UpdateAsync' -ParamsYaml $paramsYaml | Out-Null
    Write-PostToolUseOutput -Status 'updated'
}

if (-not (Confirm-PowerShellMcpRuntime)) {
    Stop-PluginUnavailable
}

switch ($HookName) {
    'session-start' { Start-PluginSession -StartPath $WorkspacePath }
    'session-end' { Invoke-CacheFlushHook }
    'pre-compact' { Invoke-CacheFlushHook }
    'post-compact' { Start-PluginSession -StartPath $WorkspacePath }
    'user-prompt-submit' { Open-PluginTurn }
    'stop-gate' { Close-PluginTurnIfNeeded }
    'code-verify' { Invoke-CodeVerify }
    'plan-approved' { Invoke-PlanApprovedHook }
    'plan-modified' { Invoke-PlanModifiedHook }
    'cache-flush' { Invoke-CacheFlushHook }
    'health-check' { Invoke-PluginRepl -Method 'client.Health.GetAsync' }
    'subagent-import' { Write-PluginJson ([ordered]@{ status = 'skipped' }) }
}
