#Requires -Version 7.0

<#
.SYNOPSIS
    Implements plugin-owned operator-profile reload behavior after context compaction.
.DESCRIPTION
    FR-MCP-190 and TR-MCP-PROFILE-001 require an immediate host-specific add-profile
    instruction after compaction and one additional instruction when measured context
    usage first reaches 70 percent of the context window reported by the active host.
    State is kept in the plugin cache and unknown usage remains armed and fail-soft.

    Change log:
    2026-11-14 - Added dynamic Codex and Claude usage parsing, host-specific instructions,
    compact-event fallback injection, and exactly-once threshold reload for TEST-MCP-238.
#>

function Get-McpProfileReloadInstruction {
    <#
    .SYNOPSIS
        Returns the add-profile invocation instruction for one plugin host.
    .PARAMETER HostName
        Canonical plugin host name.
    #>
    param([Parameter(Mandatory)][string]$HostName)

    $normalized = $HostName.Trim().ToLowerInvariant()
    $command = switch ($normalized) {
        'codex' { '$add-profile' }
        'claude' { '/add-profile' }
        'claude-code' { '/add-profile' }
        'cowork' { '/add-profile' }
        'claude-cowork' { '/add-profile' }
        'copilot' { 'add-profile' }
        'grok' { '/add-profile' }
        'grokcode' { '/add-profile' }
        'grok-code' { '/add-profile' }
        'grok-bot' { '/add-profile' }
        'cline' { 'add-profile skill' }
        'cline-v2' { 'add-profile skill' }
        'opencode' { '/add-profile' }
        'open-code' { '/add-profile' }
        default { 'add-profile skill' }
    }

    return "PROFILE RELOAD: Context was compacted or crossed the reload threshold. Run $command now and follow it exactly. Read every global profile file in full and reload live Global and active Workspace MCP memories through this agent's plugin before continuing the active task."
}

function Get-McpProfileReloadStatePath {
    <#
    .SYNOPSIS
        Returns the state path for a plugin cache.
    #>
    param([Parameter(Mandatory)][string]$CacheDir)

    return (Join-Path $CacheDir 'profile-reload-state.json')
}

function Read-McpProfileReloadState {
    <#
    .SYNOPSIS
        Reads profile reload state, returning null for missing or invalid state.
    #>
    param([Parameter(Mandatory)][string]$CacheDir)

    $path = Get-McpProfileReloadStatePath -CacheDir $CacheDir
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { return $null }
    try {
        return ([System.IO.File]::ReadAllText($path) | ConvertFrom-Json -AsHashtable -Depth 20 -ErrorAction Stop)
    } catch {
        return $null
    }
}

function Write-McpProfileReloadState {
    <#
    .SYNOPSIS
        Serializes profile reload state into the plugin cache.
    #>
    param(
        [Parameter(Mandatory)][string]$CacheDir,
        [Parameter(Mandatory)]$State
    )

    [void][System.IO.Directory]::CreateDirectory($CacheDir)
    $json = $State | ConvertTo-Json -Depth 20
    [System.IO.File]::WriteAllText(
        (Get-McpProfileReloadStatePath -CacheDir $CacheDir),
        ($json.TrimEnd() + [Environment]::NewLine),
        [System.Text.UTF8Encoding]::new($false))
}

function Initialize-McpProfileReloadCompaction {
    <#
    .SYNOPSIS
        Arms a reload cycle for one compaction.
    .PARAMETER InjectionAvailable
        Indicates that the caller can inject the immediate instruction in this event.
        When omitted, the next UserPromptSubmit supplies the immediate instruction.
    #>
    param(
        [Parameter(Mandatory)][string]$CacheDir,
        [Parameter(Mandatory)][string]$HostName,
        [switch]$InjectionAvailable,
        [string]$Source = 'compact'
    )

    $now = [DateTime]::UtcNow.ToString('o')
    $state = [ordered]@{
        armed = $true
        immediatePending = -not $InjectionAvailable
        threshold = 0.7
        hostName = $HostName
        source = $Source
        armedAt = $now
        firedAt = $null
        lastInputTokens = 0L
        lastContextWindow = 0L
        lastUsageFormat = 'unavailable'
    }
    Write-McpProfileReloadState -CacheDir $CacheDir -State $state

    return [pscustomobject]@{
        AdditionalContext = if ($InjectionAvailable) { Get-McpProfileReloadInstruction -HostName $HostName } else { '' }
        Armed = $true
        ImmediatePending = -not $InjectionAvailable
    }
}

function Get-McpProfileReloadPayloadObject {
    <#
    .SYNOPSIS
        Parses a hook payload without throwing on malformed or absent JSON.
    #>
    param([string]$Payload)

    if ([string]::IsNullOrWhiteSpace($Payload)) { return $null }
    try {
        return ($Payload | ConvertFrom-Json -Depth 30 -ErrorAction Stop)
    } catch {
        return $null
    }
}

function Get-McpProfileReloadPropertyValue {
    <#
    .SYNOPSIS
        Reads the first non-null named property from a JSON-derived object.
    #>
    param(
        $InputObject,
        [Parameter(Mandatory)][string[]]$Names
    )

    if ($null -eq $InputObject) { return $null }
    foreach ($name in $Names) {
        $property = $InputObject.PSObject.Properties[$name]
        if ($property -and $null -ne $property.Value) { return $property.Value }
    }
    return $null
}

function Get-McpProfileReloadTranscriptPath {
    <#
    .SYNOPSIS
        Resolves a transcript path from the current hook payload and agent environment.
    #>
    param([string]$Payload)

    $payloadObject = Get-McpProfileReloadPayloadObject -Payload $Payload
    $payloadPath = Get-McpProfileReloadPropertyValue -InputObject $payloadObject -Names @(
        'transcript_path',
        'transcriptPath',
        'transcript_file',
        'transcriptFile'
    )
    $candidates = @(
        [string]$payloadPath,
        [string]$env:MCP_AGENT_SESSION_TRANSCRIPT_FILE,
        [string]$env:CODEX_TRANSCRIPT_PATH,
        [string]$env:CLAUDE_TRANSCRIPT_PATH
    ) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }

    foreach ($candidate in $candidates) {
        try {
            if (Test-Path -LiteralPath $candidate -PathType Leaf) {
                return (Resolve-Path -LiteralPath $candidate).ProviderPath
            }
        } catch {
            continue
        }
    }
    return ''
}

function Get-McpProfileReloadContextWindow {
    <#
    .SYNOPSIS
        Returns a positive context-window value supplied by the hook payload or environment.
    #>
    param([string]$Payload)

    $payloadObject = Get-McpProfileReloadPayloadObject -Payload $Payload
    $value = Get-McpProfileReloadPropertyValue -InputObject $payloadObject -Names @(
        'model_context_window',
        'context_window',
        'contextWindow'
    )
    $window = 0L
    if ($null -ne $value -and [long]::TryParse([string]$value, [ref]$window) -and $window -gt 0) {
        return $window
    }
    foreach ($name in @('MCP_MODEL_CONTEXT_WINDOW', 'CODEX_MODEL_CONTEXT_WINDOW', 'CLAUDE_MODEL_CONTEXT_WINDOW')) {
        $text = [Environment]::GetEnvironmentVariable($name)
        if ([long]::TryParse($text, [ref]$window) -and $window -gt 0) { return $window }
    }
    return 0L
}

function Get-McpProfileReloadUsage {
    <#
    .SYNOPSIS
        Reads the latest usable Codex or Claude context usage from a JSONL transcript.
    .DESCRIPTION
        Codex uses event_msg/token_count with last_token_usage and a reported model
        context window. Claude uses assistant/message.usage and relies on a window
        reported by the hook payload or environment. Malformed lines are ignored.
    #>
    param(
        [string]$TranscriptPath,
        [string]$Payload
    )

    $contextWindow = Get-McpProfileReloadContextWindow -Payload $Payload
    $inputTokens = 0L
    $usageFormat = 'unavailable'
    if ([string]::IsNullOrWhiteSpace($TranscriptPath) -or -not (Test-Path -LiteralPath $TranscriptPath -PathType Leaf)) {
        return [pscustomobject]@{ InputTokens = $inputTokens; ContextWindow = $contextWindow; UsageFormat = $usageFormat }
    }

    foreach ($line in [System.IO.File]::ReadLines($TranscriptPath)) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        try {
            $entry = $line | ConvertFrom-Json -Depth 40 -ErrorAction Stop
        } catch {
            continue
        }

        $entryType = [string](Get-McpProfileReloadPropertyValue -InputObject $entry -Names @('type'))
        if ($entryType -eq 'event_msg') {
            $eventPayload = Get-McpProfileReloadPropertyValue -InputObject $entry -Names @('payload')
            $eventType = [string](Get-McpProfileReloadPropertyValue -InputObject $eventPayload -Names @('type'))
            if ($eventType -eq 'token_count') {
                $info = Get-McpProfileReloadPropertyValue -InputObject $eventPayload -Names @('info')
                $lastUsage = Get-McpProfileReloadPropertyValue -InputObject $info -Names @('last_token_usage')
                $inputValue = Get-McpProfileReloadPropertyValue -InputObject $lastUsage -Names @('input_tokens')
                $windowValue = Get-McpProfileReloadPropertyValue -InputObject $info -Names @('model_context_window')
                $candidate = 0L
                if ($null -ne $inputValue -and [long]::TryParse([string]$inputValue, [ref]$candidate) -and $candidate -ge 0) {
                    $inputTokens = $candidate
                    $usageFormat = 'codex-token-count'
                }
                if ($null -ne $windowValue -and [long]::TryParse([string]$windowValue, [ref]$candidate) -and $candidate -gt 0) {
                    $contextWindow = $candidate
                }
            } elseif ($eventType -eq 'task_started') {
                $windowValue = Get-McpProfileReloadPropertyValue -InputObject $eventPayload -Names @('model_context_window')
                $candidate = 0L
                if ($null -ne $windowValue -and [long]::TryParse([string]$windowValue, [ref]$candidate) -and $candidate -gt 0) {
                    $contextWindow = $candidate
                }
            }
            continue
        }

        if ($entryType -eq 'assistant') {
            $message = Get-McpProfileReloadPropertyValue -InputObject $entry -Names @('message')
            $usage = Get-McpProfileReloadPropertyValue -InputObject $message -Names @('usage')
            if ($null -eq $usage) { continue }
            $sum = 0L
            $found = $false
            foreach ($name in @('input_tokens', 'cache_creation_input_tokens', 'cache_read_input_tokens')) {
                $value = Get-McpProfileReloadPropertyValue -InputObject $usage -Names @($name)
                $candidate = 0L
                if ($null -ne $value -and [long]::TryParse([string]$value, [ref]$candidate) -and $candidate -ge 0) {
                    $sum += $candidate
                    $found = $true
                }
            }
            if ($found) {
                $inputTokens = $sum
                $usageFormat = 'claude-assistant-usage'
            }
        }
    }

    return [pscustomobject]@{
        InputTokens = $inputTokens
        ContextWindow = $contextWindow
        UsageFormat = $usageFormat
    }
}

function Invoke-McpProfileReloadCheck {
    <#
    .SYNOPSIS
        Returns pending immediate or threshold profile-reload context for a request boundary.
    .DESCRIPTION
        The immediate fallback is delivered first and leaves the threshold armed. The
        threshold reload fires once when input tokens reach 70 percent of a positive,
        dynamically detected context window. Missing evidence never disarms the state.
    #>
    param(
        [Parameter(Mandatory)][string]$CacheDir,
        [Parameter(Mandatory)][string]$HostName,
        [string]$Payload = '{}',
        [string]$TranscriptPath
    )

    $state = Read-McpProfileReloadState -CacheDir $CacheDir
    if ($null -eq $state -or -not [bool]$state['armed']) {
        return [pscustomobject]@{ AdditionalContext = ''; Armed = $false; Fired = $false; InputTokens = 0L; ContextWindow = 0L; UsageFormat = 'unavailable' }
    }

    if ([bool]$state['immediatePending']) {
        $state['immediatePending'] = $false
        Write-McpProfileReloadState -CacheDir $CacheDir -State $state
        return [pscustomobject]@{
            AdditionalContext = Get-McpProfileReloadInstruction -HostName $HostName
            Armed = $true
            Fired = $false
            InputTokens = 0L
            ContextWindow = 0L
            UsageFormat = 'immediate-fallback'
        }
    }

    if ([string]::IsNullOrWhiteSpace($TranscriptPath)) {
        $TranscriptPath = Get-McpProfileReloadTranscriptPath -Payload $Payload
    }
    $usage = Get-McpProfileReloadUsage -TranscriptPath $TranscriptPath -Payload $Payload
    $threshold = if ($state.Contains('threshold')) { [double]$state['threshold'] } else { 0.7 }
    $thresholdTokens = if ($usage.ContextWindow -gt 0) { [long][math]::Ceiling($usage.ContextWindow * $threshold) } else { 0L }
    $fire = $thresholdTokens -gt 0 -and $usage.InputTokens -ge $thresholdTokens

    $state['lastInputTokens'] = [long]$usage.InputTokens
    $state['lastContextWindow'] = [long]$usage.ContextWindow
    $state['lastUsageFormat'] = [string]$usage.UsageFormat
    if ($fire) {
        $state['armed'] = $false
        $state['firedAt'] = [DateTime]::UtcNow.ToString('o')
    }
    Write-McpProfileReloadState -CacheDir $CacheDir -State $state

    return [pscustomobject]@{
        AdditionalContext = if ($fire) { Get-McpProfileReloadInstruction -HostName $HostName } else { '' }
        Armed = -not $fire
        Fired = $fire
        InputTokens = [long]$usage.InputTokens
        ContextWindow = [long]$usage.ContextWindow
        UsageFormat = [string]$usage.UsageFormat
    }
}

function Test-McpProfileReloadCompactionPayload {
    <#
    .SYNOPSIS
        Identifies a SessionStart payload caused by context compaction.
    #>
    param([string]$Payload)

    $payloadObject = Get-McpProfileReloadPayloadObject -Payload $Payload
    foreach ($name in @('source', 'matcher', 'reason', 'trigger')) {
        $value = Get-McpProfileReloadPropertyValue -InputObject $payloadObject -Names @($name)
        if ($null -ne $value -and [string]$value -match '(?i)compact') { return $true }
    }
    return $false
}
