#Requires -Version 7.0

<#
.SYNOPSIS
    Contract double for the plugin-owned profile reload state machine.
.DESCRIPTION
    TEST-MCP-238 mock-gate implementation used before the production provider exists.
    Change log: 2026-11-14 - Added for FR-MCP-190, TR-MCP-PROFILE-001, and
    TEST-MCP-238 to prove the profile-reload assertions against controlled data.
#>

function Get-McpProfileReloadInstruction {
    param([Parameter(Mandatory)][string]$HostName)

    $command = switch ($HostName) {
        'codex' { '$add-profile' }
        'claude-code' { '/add-profile' }
        'claude-cowork' { '/add-profile' }
        'copilot' { 'add-profile' }
        'grok' { '/add-profile' }
        'grok-bot' { '/add-profile' }
        'cline' { 'add-profile skill' }
        'cline-v2' { 'add-profile skill' }
        'opencode' { '/add-profile' }
        default { 'add-profile skill' }
    }
    return "PROFILE RELOAD: Run $command now and follow it exactly."
}

function Get-McpProfileReloadStatePath {
    param([Parameter(Mandatory)][string]$CacheDir)
    return (Join-Path $CacheDir 'profile-reload-state.json')
}

function Read-McpProfileReloadState {
    param([Parameter(Mandatory)][string]$CacheDir)
    $path = Get-McpProfileReloadStatePath -CacheDir $CacheDir
    if (-not (Test-Path -LiteralPath $path)) { return $null }
    return (Get-Content -LiteralPath $path -Raw | ConvertFrom-Json -AsHashtable -Depth 20)
}

function Write-McpProfileReloadState {
    param([Parameter(Mandatory)][string]$CacheDir, [Parameter(Mandatory)]$State)
    [void][System.IO.Directory]::CreateDirectory($CacheDir)
    $State | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath (Get-McpProfileReloadStatePath -CacheDir $CacheDir)
}

function Initialize-McpProfileReloadCompaction {
    param(
        [Parameter(Mandatory)][string]$CacheDir,
        [Parameter(Mandatory)][string]$HostName,
        [switch]$InjectionAvailable,
        [string]$Source = 'compact'
    )
    $state = [ordered]@{
        armed = $true
        immediatePending = -not $InjectionAvailable
        threshold = 0.7
        hostName = $HostName
        source = $Source
        firedAt = $null
    }
    Write-McpProfileReloadState -CacheDir $CacheDir -State $state
    [pscustomobject]@{
        AdditionalContext = if ($InjectionAvailable) { Get-McpProfileReloadInstruction -HostName $HostName } else { '' }
        Armed = $true
        ImmediatePending = -not $InjectionAvailable
    }
}

function Get-McpProfileReloadUsage {
    param([string]$TranscriptPath, [string]$Payload)

    $window = 0L
    if ($Payload) {
        $payloadObject = $Payload | ConvertFrom-Json -Depth 20
        foreach ($name in @('model_context_window', 'context_window', 'contextWindow')) {
            $property = $payloadObject.PSObject.Properties[$name]
            if ($property -and [long]$property.Value -gt 0) { $window = [long]$property.Value }
        }
    }
    if (-not $TranscriptPath -or -not (Test-Path -LiteralPath $TranscriptPath)) {
        return [pscustomobject]@{ InputTokens = 0L; ContextWindow = $window; UsageFormat = 'unavailable' }
    }

    $inputTokens = 0L
    $format = 'unavailable'
    foreach ($line in Get-Content -LiteralPath $TranscriptPath) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        $entry = $line | ConvertFrom-Json -Depth 30
        if ($entry.type -eq 'event_msg' -and $entry.payload.type -eq 'token_count') {
            $inputTokens = [long]$entry.payload.info.last_token_usage.input_tokens
            $window = [long]$entry.payload.info.model_context_window
            $format = 'codex-token-count'
        } elseif ($entry.type -eq 'assistant' -and $entry.message.usage) {
            $usage = $entry.message.usage
            $inputTokens = [long]$usage.input_tokens + [long]$usage.cache_creation_input_tokens + [long]$usage.cache_read_input_tokens
            $format = 'claude-assistant-usage'
        }
    }
    return [pscustomobject]@{ InputTokens = $inputTokens; ContextWindow = $window; UsageFormat = $format }
}

function Invoke-McpProfileReloadCheck {
    param(
        [Parameter(Mandatory)][string]$CacheDir,
        [Parameter(Mandatory)][string]$HostName,
        [string]$Payload = '{}',
        [string]$TranscriptPath
    )
    $state = Read-McpProfileReloadState -CacheDir $CacheDir
    if (-not $state -or -not [bool]$state.armed) {
        return [pscustomobject]@{ AdditionalContext = ''; Armed = $false; Fired = $false; InputTokens = 0L; ContextWindow = 0L; UsageFormat = 'unavailable' }
    }
    if ([bool]$state.immediatePending) {
        $state.immediatePending = $false
        Write-McpProfileReloadState -CacheDir $CacheDir -State $state
        return [pscustomobject]@{ AdditionalContext = Get-McpProfileReloadInstruction -HostName $HostName; Armed = $true; Fired = $false; InputTokens = 0L; ContextWindow = 0L; UsageFormat = 'immediate-fallback' }
    }
    $usage = Get-McpProfileReloadUsage -TranscriptPath $TranscriptPath -Payload $Payload
    $fire = $usage.ContextWindow -gt 0 -and $usage.InputTokens -ge [math]::Ceiling($usage.ContextWindow * 0.7)
    if ($fire) {
        $state.armed = $false
        $state.firedAt = [DateTime]::UtcNow.ToString('o')
        Write-McpProfileReloadState -CacheDir $CacheDir -State $state
    }
    return [pscustomobject]@{
        AdditionalContext = if ($fire) { Get-McpProfileReloadInstruction -HostName $HostName } else { '' }
        Armed = -not $fire
        Fired = $fire
        InputTokens = $usage.InputTokens
        ContextWindow = $usage.ContextWindow
        UsageFormat = $usage.UsageFormat
    }
}
