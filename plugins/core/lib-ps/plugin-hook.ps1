#Requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet('session-start', 'session-end', 'pre-compact', 'post-compact', 'user-prompt-submit', 'stop-gate', 'code-verify', 'plan-approved', 'plan-modified', 'cache-flush', 'health-check', 'subagent-import')]
    [string]$HookName,

    [string]$HostName = $(if ($env:MCP_PLUGIN_HOST) { $env:MCP_PLUGIN_HOST } else { 'codex' }),

    [ValidateSet('flat', 'scoped')]
    [string]$CacheMode = 'scoped',

    [string]$WorkspacePath,

    [string]$Params,

    [string]$ParamsPath,

    [Parameter(ValueFromPipeline = $true)]
    [object]$InputObject
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
    param([string]$StartPath)

    $cacheDir = Resolve-McpCacheDir -StartPath $StartPath
    if (-not (Test-Path -LiteralPath $cacheDir)) {
        [void][System.IO.Directory]::CreateDirectory($cacheDir)
    }
    return $cacheDir
}

function Get-PluginStartPath {
    param([string]$PreferredPath)

    if ($PreferredPath) { return $PreferredPath }
    if ($WorkspacePath) { return $WorkspacePath }

    $currentPath = (Get-Location).ProviderPath
    if ($currentPath -and (Get-Command Find-MarkerFile -ErrorAction SilentlyContinue)) {
        try {
            if (Find-MarkerFile -StartDir $currentPath) { return $currentPath }
        } catch {
        }
    }

    if ($env:MCP_WORKSPACE_START_DIR) { return $env:MCP_WORKSPACE_START_DIR }
    if ($env:CLAUDE_PROJECT_DIR) { return $env:CLAUDE_PROJECT_DIR }
    if ($env:CODEX_CWD) { return $env:CODEX_CWD }
    if ($env:CODEX_WORKSPACE_PATH) { return $env:CODEX_WORKSPACE_PATH }
    if ($env:CODEX_PROJECT_DIR) { return $env:CODEX_PROJECT_DIR }
    if ($env:COWORK_WORKSPACE_PATH) { return $env:COWORK_WORKSPACE_PATH }
    if ($env:COPILOT_WORKSPACE_PATH) { return $env:COPILOT_WORKSPACE_PATH }
    if ($env:CLINE_WORKSPACE_PATH) { return $env:CLINE_WORKSPACE_PATH }
    if ($env:OPENCODE_WORKSPACE_PATH) { return $env:OPENCODE_WORKSPACE_PATH }
    if ($env:MCPSERVER_WORKSPACE_PATH) { return $env:MCPSERVER_WORKSPACE_PATH }
    if ($env:MCP_WORKSPACE_PATH) { return $env:MCP_WORKSPACE_PATH }
    return $currentPath
}

function Get-YamlScalar {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$Key
    )

    if (-not (Test-Path -LiteralPath $Path)) { return $null }
    $document = Read-McpYamlObject -Path $Path
    if ($document -isnot [System.Collections.IDictionary]) { return $null }
    if (-not $document.Contains($Key) -or $null -eq $document[$Key]) { return $null }
    return ([string]$document[$Key]).Trim().Trim('"')
}

function Set-YamlScalar {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$Key,
        [Parameter(Mandatory)][string]$Value
    )

    if (-not (Test-Path -LiteralPath $Path)) { return }
    Set-McpYamlObjectValue -Path $Path -KeyPath @($Key) -Value $Value | Out-Null
}

function Read-HookInput {
    if ($Params) {
        return $Params
    }

    if ($ParamsPath) {
        if (-not (Test-Path -LiteralPath $ParamsPath)) {
            throw "Hook params file was not found: $ParamsPath"
        }

        return [System.IO.File]::ReadAllText($ParamsPath)
    }

    if ($null -ne $InputObject) {
        if ($InputObject -is [string]) {
            return [string]$InputObject
        }

        return ($InputObject | ConvertTo-Json -Depth 20 -Compress)
    }

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

    $script:LastPluginReplExitCode = 0
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
    $exitCodeVariable = Get-Variable -Name LASTEXITCODE -Scope Global -ErrorAction SilentlyContinue
    $script:LastPluginReplExitCode = if ($null -ne $exitCodeVariable -and $null -ne $exitCodeVariable.Value) { [int]$exitCodeVariable.Value } else { 0 }
}

function Start-PluginSession {
    param([string]$StartPath)

    $start = Get-PluginStartPath -PreferredPath $StartPath
    $cacheDir = Get-PluginCacheDir -StartPath $start
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

function ConvertTo-PluginAgentKey {
    param([string]$AgentName)

    if ([string]::IsNullOrWhiteSpace($AgentName)) { return '' }

    switch ($AgentName.Trim().ToLowerInvariant()) {
        'claude' { return 'claude' }
        'claudecode' { return 'claude' }
        'claude-code' { return 'claude' }
        'claudecowork' { return 'cowork' }
        'claude-cowork' { return 'cowork' }
        'codex' { return 'codex' }
        'copilot' { return 'copilot' }
        'grok' { return 'grok' }
        'grokcode' { return 'grok' }
        'grok-code' { return 'grok' }
        'cline' { return 'cline' }
        'cline-v2' { return 'cline' }
        'opencode' { return 'opencode' }
        'open-code' { return 'opencode' }
    }

    return (($AgentName.Trim() -replace '[^A-Za-z0-9]+', '-').Trim('-').ToLowerInvariant())
}

function Get-PluginExpectedAgentKey {
    $agent = @(
        $env:MCP_AGENT_NAME,
        $env:PLUGIN_AGENT_DEFAULT,
        $env:MCP_PLUGIN_HOST
    ) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -First 1

    return (ConvertTo-PluginAgentKey -AgentName $agent)
}

function Test-PluginSessionStateValid {
    param($State)

    if ($State -isnot [System.Collections.IDictionary]) { return $false }
    if ([string]$State['status'] -ne 'verified') { return $false }

    $sessionId = if ($State.Contains('sessionId')) { [string]$State['sessionId'] } else { '' }
    if ([string]::IsNullOrWhiteSpace($sessionId.Trim().Trim('''').Trim('"'))) { return $false }

    $expectedAgentKey = Get-PluginExpectedAgentKey
    if ($expectedAgentKey) {
        $stateAgent = if ($State.Contains('agent')) { [string]$State['agent'] } else { '' }
        if ((ConvertTo-PluginAgentKey -AgentName $stateAgent) -ne $expectedAgentKey) { return $false }
    }

    return $true
}

function Get-PluginNoSessionRecoveryPath {
    param([Parameter(Mandatory)][string]$CacheDir)

    return (Join-Path $CacheDir 'no-session-recovery.yaml')
}

function Read-PluginNoSessionRecoveryState {
    param([Parameter(Mandatory)][string]$CacheDir)

    $path = Get-PluginNoSessionRecoveryPath -CacheDir $CacheDir
    if (-not (Test-Path -LiteralPath $path)) { return $null }

    try {
        return Read-McpYamlObject -Path $path
    } catch {
        return $null
    }
}

function Get-PluginNoSessionRootId {
    param($Snapshot)

    if ($Snapshot) {
        return 'no-session:{0}:{1}' -f $Snapshot.markerFilePath, $Snapshot.markerLastWriteUtc
    }

    return 'no-session:unknown-marker'
}

function ConvertTo-PluginSafeFileStem {
    param([Parameter(Mandatory)][string]$Value)

    $hashBytes = [System.Security.Cryptography.SHA256]::HashData([System.Text.Encoding]::UTF8.GetBytes($Value))
    $hash = ([Convert]::ToHexString($hashBytes)).Substring(0, 12).ToLowerInvariant()
    $stem = (($Value -replace '[^A-Za-z0-9._-]+', '-').Trim('-'))
    if (-not $stem) { $stem = 'root' }
    if ($stem.Length -gt 80) { $stem = $stem.Substring(0, 80).Trim('-') }
    return ('{0}-{1}' -f $stem, $hash)
}

function Write-PluginRecoveryFailsafe {
    param(
        [Parameter(Mandatory)][string]$CacheDir,
        [Parameter(Mandatory)][string]$Label,
        [Parameter(Mandatory)][string]$RootId,
        [Parameter(Mandatory)][string]$Method,
        [Parameter(Mandatory)]$Payload
    )

    try {
        $dir = Join-Path $CacheDir 'failsafe\pending'
        [void][System.IO.Directory]::CreateDirectory($dir)
        $fileName = '{0}-{1}.yaml' -f $Label, (ConvertTo-PluginSafeFileStem -Value $RootId)
        $path = Join-Path $dir $fileName
        $record = [ordered]@{
            method = $Method
            label = $Label
            rootId = $RootId
            timestamp = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
            payload = $Payload
        }
        Write-McpYamlObject -Path $path -Document $record
        return $path
    } catch {
        return ''
    }
}

function Clear-PluginRecoveryFailsafe {
    param([string]$Path)

    if ($Path -and (Test-Path -LiteralPath $Path)) {
        Remove-Item -LiteralPath $Path -Force -ErrorAction SilentlyContinue
    }
}

function Test-PluginNoSessionRetrySuppressed {
    param(
        [Parameter(Mandatory)][string]$CacheDir,
        $Snapshot
    )

    $state = Read-PluginNoSessionRecoveryState -CacheDir $CacheDir
    if ($state -isnot [System.Collections.IDictionary]) { return $false }
    if ([string]$state['status'] -ne 'session-create-failed') { return $false }
    if (-not $Snapshot) { return $true }

    return ([string]$state['markerFilePath'] -eq [string]$Snapshot.markerFilePath -and
        [string]$state['markerLastWriteUtc'] -eq [string]$Snapshot.markerLastWriteUtc)
}

function Submit-PluginNoSessionTriage {
    param(
        [Parameter(Mandatory)][string]$CacheDir,
        [Parameter(Mandatory)][string]$StartPath,
        $Snapshot,
        [Parameter(Mandatory)][string]$Reason
    )

    $agent = if ($env:MCP_AGENT_NAME) { $env:MCP_AGENT_NAME } elseif ($env:PLUGIN_AGENT_DEFAULT) { $env:PLUGIN_AGENT_DEFAULT } else { $HostName }
    $rootId = 'triage:{0}' -f (Get-PluginNoSessionRootId -Snapshot $Snapshot)
    $params = [ordered]@{
        title = 'Plugin session recovery could not create verified session state'
        summary = 'The plugin health check succeeded, but session-state.yaml could not be recreated. Work should continue with degraded failsafe session logging until the marker timestamp changes.'
        component = 'mcpserver-plugin-core'
        affectedPaths = @('plugins/core/lib-ps/plugin-hook.ps1', 'plugins/core/lib-ps/repl-invoke.ps1')
        affectedSymbols = @('Open-PluginTurn', 'Start-PluginSession', 'Ensure-PluginMarkerFresh')
        errorSignature = 'plugin-no-session-create-failed'
        dedupeKey = $rootId
        evidence = $Reason
        reporterAgent = $agent
        workspacePath = $StartPath
        tags = @('BUG-TRIAGE-047', 'plugin', 'session-recovery')
        idempotencyKey = $rootId
    }
    if ($Snapshot) {
        $params['reproductionHints'] = @(
            ('markerFilePath={0}' -f $Snapshot.markerFilePath),
            ('markerLastWriteUtc={0}' -f $Snapshot.markerLastWriteUtc)
        )
    }

    $failsafePath = Write-PluginRecoveryFailsafe -CacheDir $CacheDir -Label 'no-session-triage' -RootId $rootId -Method 'workflow.triage.report' -Payload $params
    $paramsYaml = ConvertTo-PluginParamsYaml $params
    $output = Invoke-PluginRepl -Method 'workflow.triage.report' -ParamsYaml $paramsYaml
    if ($script:LastPluginReplExitCode -eq 0) {
        Clear-PluginRecoveryFailsafe -Path $failsafePath
        return [ordered]@{ submitted = $true; failsafePath = $null; output = (($output | Out-String).Trim()) }
    }

    return [ordered]@{ submitted = $false; failsafePath = $failsafePath; output = (($output | Out-String).Trim()) }
}

function Invoke-PluginNoSessionRecovery {
    param(
        [Parameter(Mandatory)][string]$CacheDir,
        [Parameter(Mandatory)][string]$SessionFile,
        [Parameter(Mandatory)][string]$StartPath
    )

    $snapshot = $null
    try {
        $snapshot = Get-MarkerFileSnapshot -StartDir $StartPath
    } catch {
        $snapshot = $null
    }

    $rootId = Get-PluginNoSessionRootId -Snapshot $snapshot
    $healthOk = $false
    try {
        $healthOk = [bool](Invoke-FullBootstrap -StartDir $StartPath)
    } catch {
        $healthOk = $false
    }

    if (-not $healthOk) {
        $state = [ordered]@{
            status = 'health-failed'
            healthStatus = 'failed'
            lastCheckedUtc = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
        }
        if ($snapshot) {
            $state['markerFilePath'] = $snapshot.markerFilePath
            $state['markerLastWriteUtc'] = $snapshot.markerLastWriteUtc
        }
        Write-McpYamlObject -Path (Get-PluginNoSessionRecoveryPath -CacheDir $CacheDir) -Document $state
        return $state
    }

    if (Test-PluginNoSessionRetrySuppressed -CacheDir $CacheDir -Snapshot $snapshot) {
        $state = Read-PluginNoSessionRecoveryState -CacheDir $CacheDir
        $state['healthStatus'] = 'healthy'
        $state['lastCheckedUtc'] = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
        $state['message'] = 'Session creation retry suppressed until AGENTS-README-FIRST.yaml marker timestamp changes.'
        $failsafePayload = [ordered]@{
            workspacePath = $StartPath
            markerFilePath = if ($snapshot) { $snapshot.markerFilePath } else { '' }
            markerLastWriteUtc = if ($snapshot) { $snapshot.markerLastWriteUtc } else { '' }
            status = 'session-create-failed'
        }
        $state['failsafePath'] = Write-PluginRecoveryFailsafe -CacheDir $CacheDir -Label 'no-session-recovery' -RootId $rootId -Method 'workflow.sessionlog.recovery' -Payload $failsafePayload
        Write-McpYamlObject -Path (Get-PluginNoSessionRecoveryPath -CacheDir $CacheDir) -Document $state
        return $state
    }

    Start-PluginSession -StartPath $StartPath | Out-Null
    $stateAfterCreate = if (Test-Path -LiteralPath $SessionFile) { Read-McpYamlObject -Path $SessionFile } else { $null }
    if (Test-PluginSessionStateValid -State $stateAfterCreate) {
        $recoveryPath = Get-PluginNoSessionRecoveryPath -CacheDir $CacheDir
        if (Test-Path -LiteralPath $recoveryPath) { Remove-Item -LiteralPath $recoveryPath -Force -ErrorAction SilentlyContinue }
        return [ordered]@{ status = 'recovered'; healthStatus = 'healthy' }
    }

    $reason = 'health check succeeded, but session-state.yaml remained missing or unverified after Start-PluginSession.'
    $triage = Submit-PluginNoSessionTriage -CacheDir $CacheDir -StartPath $StartPath -Snapshot $snapshot -Reason $reason
    $failsafePayload = [ordered]@{
        workspacePath = $StartPath
        markerFilePath = if ($snapshot) { $snapshot.markerFilePath } else { '' }
        markerLastWriteUtc = if ($snapshot) { $snapshot.markerLastWriteUtc } else { '' }
        reason = $reason
        triageSubmitted = [bool]$triage['submitted']
    }
    $failsafePath = Write-PluginRecoveryFailsafe -CacheDir $CacheDir -Label 'no-session-recovery' -RootId $rootId -Method 'workflow.sessionlog.recovery' -Payload $failsafePayload
    $failureState = [ordered]@{
        status = 'session-create-failed'
        healthStatus = 'healthy'
        lastCheckedUtc = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
        markerFilePath = if ($snapshot) { $snapshot.markerFilePath } else { '' }
        markerLastWriteUtc = if ($snapshot) { $snapshot.markerLastWriteUtc } else { '' }
        triageSubmitted = [bool]$triage['submitted']
        triageFailsafePath = [string]$triage['failsafePath']
        failsafePath = $failsafePath
        message = 'Session creation failed after a healthy marker/bootstrap check; retry suppressed until the marker timestamp changes.'
    }
    Write-McpYamlObject -Path (Get-PluginNoSessionRecoveryPath -CacheDir $CacheDir) -Document $failureState
    return $failureState
}

function Ensure-PluginMarkerFresh {
    param([string]$StartPath)

    $start = Get-PluginStartPath -PreferredPath $StartPath
    $cacheDir = Get-PluginCacheDir -StartPath $start
    $sessionFile = Join-Path $cacheDir 'session-state.yaml'

    try {
        $snapshotForRetry = $null
        try {
            $snapshotForRetry = Get-MarkerFileSnapshot -StartDir $start
        } catch {
            $snapshotForRetry = $null
        }

        if (-not (Test-Path -LiteralPath $sessionFile)) {
            if (Test-PluginNoSessionRetrySuppressed -CacheDir $cacheDir -Snapshot $snapshotForRetry) {
                return $false
            }
            Start-PluginSession -StartPath $start | Out-Null
        }

        if (-not (Test-Path -LiteralPath $sessionFile)) {
            return $false
        }

        $state = Read-McpYamlObject -Path $sessionFile
        if (-not (Test-PluginSessionStateValid -State $state)) {
            if (Test-PluginNoSessionRetrySuppressed -CacheDir $cacheDir -Snapshot $snapshotForRetry) {
                return $false
            }
            Start-PluginSession -StartPath $start | Out-Null
            if (-not (Test-Path -LiteralPath $sessionFile)) {
                return $false
            }

            $state = Read-McpYamlObject -Path $sessionFile
            if (-not (Test-PluginSessionStateValid -State $state)) {
                return $false
            }
        }
        $snapshot = if ($snapshotForRetry) { $snapshotForRetry } else { Get-MarkerFileSnapshot -StartDir $start }
        $cachedPath = [string]$state['markerFilePath']
        $cachedWriteUtc = [string]$state['markerLastWriteUtc']

        if ($cachedPath -ne $snapshot.markerFilePath -or $cachedWriteUtc -ne $snapshot.markerLastWriteUtc) {
            Start-PluginSession -StartPath $start | Out-Null
            $state = Read-McpYamlObject -Path $sessionFile
        }

        return (Test-PluginSessionStateValid -State $state)
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
    $startPath = Get-PluginStartPath -PreferredPath $WorkspacePath
    $cacheDir = Get-PluginCacheDir -StartPath $startPath
    $sessionFile = Join-Path $cacheDir 'session-state.yaml'
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

    $openSessionState = if (Test-Path -LiteralPath $sessionFile) { Read-McpYamlObject -Path $sessionFile } else { $null }
    if (-not (Test-PluginSessionStateValid -State $openSessionState)) {
        $recovery = Invoke-PluginNoSessionRecovery -CacheDir $cacheDir -SessionFile $sessionFile -StartPath $startPath
        $openSessionState = if (Test-Path -LiteralPath $sessionFile) { Read-McpYamlObject -Path $sessionFile } else { $null }
        if (-not (Test-PluginSessionStateValid -State $openSessionState)) {
            Write-PluginJson ([ordered]@{
                hookSpecificOutput = [ordered]@{
                    hookEventName = 'UserPromptSubmit'
                    status = 'no-session'
                    recoveryStatus = [string]$recovery['status']
                    healthStatus = [string]$recovery['healthStatus']
                    failsafePath = [string]$recovery['failsafePath']
                    message = 'MCP session unavailable after health recovery. Continue user work with degraded failsafe session logging; retry session creation after AGENTS-README-FIRST.yaml marker timestamp changes.'
                }
            })
            return
        }
    }

    $payload = Read-HookInput
    $prompt = Get-HookPayloadValue -Payload $payload -Name 'prompt'
    if (-not $prompt) { $prompt = 'Continuation or hook-triggered turn.' }
    $title = (($prompt -split "`r?`n")[0]).Trim()
    if (-not $title) { $title = 'Continuation turn' }
    if ($title.Length -gt 60) { $title = $title.Substring(0, 60) }

    # Double hook registration (plugin hooks.json plus the settings.json bridge) delivers the
    # same prompt twice per user message; reuse the turn the first delivery opened instead of
    # opening a duplicate session-log turn (triage-report-7c84e6437f7b42d0a67fbe32679a686a).
    $existingTurnFile = Join-Path $cacheDir 'current-turn.yaml'
    if (Test-Path -LiteralPath $existingTurnFile) {
        $openTurn = Read-McpYamlObject -Path $existingTurnFile
        if ($openTurn -is [System.Collections.IDictionary]) {
            $openStatus = if ($openTurn.Contains('status')) { [string]$openTurn['status'] } else { '' }
            $openQuery = if ($openTurn.Contains('queryText')) { [string]$openTurn['queryText'] } else { '' }
            $openRequestId = if ($openTurn.Contains('turnRequestId')) { [string]$openTurn['turnRequestId'] } else { '' }
            $openedAtText = if ($openTurn.Contains('openedAt')) { [string]$openTurn['openedAt'] } else { '' }
            $openedRecently = $false
            $openedAtValue = [datetime]::MinValue
            if ($openedAtText -and [datetime]::TryParse($openedAtText.Trim('"'), [ref]$openedAtValue)) {
                $openedRecently = ($openedAtValue.ToUniversalTime() -gt (Get-Date).ToUniversalTime().AddMinutes(-2))
            }
            if ($openStatus -eq 'in_progress' -and
                -not [string]::IsNullOrWhiteSpace($openRequestId) -and
                $openedRecently -and
                $openQuery.Trim() -eq $prompt.Trim()) {
                Write-PluginJson ([ordered]@{
                    hookSpecificOutput = [ordered]@{
                        hookEventName = 'UserPromptSubmit'
                        status = 'turn-already-open'
                        turnRequestId = $openRequestId
                        additionalContext = "session log turn $openRequestId is now active. Continue the current task after any incidental triage submission."
                    }
                })
                return
            }
        }
    }

    $markerSnapshot = $null
    try {
        $markerSnapshot = Get-MarkerFileSnapshot -StartDir $startPath
    } catch {
    }
    $sessionState = Read-McpYamlObject -Path $sessionFile -Create
    $sessionId = if ($sessionState.Contains('sessionId')) { [string]$sessionState['sessionId'] } else { '' }

    $turnRequestId = 'req-{0}-prompt-{1:x4}' -f (Get-Date).ToUniversalTime().ToString('yyyyMMddTHHmmssZ'), (Get-Random -Maximum 0xffff)
    $paramsYaml = ConvertTo-PluginParamsYaml ([ordered]@{
        requestId = $turnRequestId
        queryTitle = $title
        queryText = $prompt
    })
    $beginOutput = Invoke-PluginRepl -Method 'workflow.sessionlog.beginTurn' -ParamsYaml $paramsYaml
    if ($script:LastPluginReplExitCode -ne 0) {
        Write-PluginJson ([ordered]@{
            hookSpecificOutput = [ordered]@{
                hookEventName = 'UserPromptSubmit'
                status = 'turn-open-failed'
                turnRequestId = $turnRequestId
                reason = 'workflow.sessionlog.beginTurn failed'
                output = (($beginOutput | Out-String).Trim())
            }
        })
        return
    }

    $turnFile = Join-Path $cacheDir 'current-turn.yaml'
    if (-not (Test-Path -LiteralPath $turnFile)) {
        Write-PluginJson ([ordered]@{
            hookSpecificOutput = [ordered]@{
                hookEventName = 'UserPromptSubmit'
                status = 'turn-open-failed'
                turnRequestId = $turnRequestId
                reason = 'workflow.sessionlog.beginTurn did not create current-turn.yaml'
            }
        })
        return
    }
    $openedRequestId = Get-YamlScalar -Path $turnFile -Key 'turnRequestId'
    if ($openedRequestId -ne $turnRequestId) {
        Write-PluginJson ([ordered]@{
            hookSpecificOutput = [ordered]@{
                hookEventName = 'UserPromptSubmit'
                status = 'turn-open-failed'
                turnRequestId = $turnRequestId
                reason = "workflow.sessionlog.beginTurn opened '$openedRequestId' instead of '$turnRequestId'"
            }
        })
        return
    }

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
    $startPath = Get-PluginStartPath -PreferredPath $WorkspacePath
    $cacheDir = Get-PluginCacheDir -StartPath $startPath
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
        $completeOutput = Invoke-PluginRepl -Method 'workflow.sessionlog.completeTurn' -ParamsYaml $paramsYaml
        if ($script:LastPluginReplExitCode -ne 0) {
            Write-PluginJson ([ordered]@{ decision = 'block'; reason = 'workflow.sessionlog.completeTurn failed; current turn remains in_progress'; output = (($completeOutput | Out-String).Trim()) })
            return
        }
        $status = Get-YamlScalar -Path $turnFile -Key 'status'
        if ($status -ne 'completed') {
            Write-PluginJson ([ordered]@{ decision = 'block'; reason = 'workflow.sessionlog.completeTurn did not mark the current turn completed' })
            return
        }
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
    $startPath = Get-PluginStartPath -PreferredPath $WorkspacePath
    $cacheDir = Get-PluginCacheDir -StartPath $startPath
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
