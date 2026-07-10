<#
.SYNOPSIS
    Invokes transcript ingestion or normalization through the MCP REPL workflow.
.DESCRIPTION
    Builds a PowerShell object, serializes it with ConvertTo-Yaml, and invokes
    repl.sessionlog.ingestTranscripts or repl.sessionlog.normalizeTranscripts
    through the shared plugin REPL bridge. This helper is intended for Claude,
    Codex, and Grok plugin skills; Cline, Copilot, and OpenCode are source
    adapters, not plugin hosts for this iteration.
#>
[CmdletBinding()]
param(
    [string]$Path,
    [ValidateSet('Auto', 'Claude', 'Codex', 'Grok', 'Cline', 'Copilot', 'OpenCode')]
    [string]$Source = 'Auto',
    [string]$Agent = $(if ($env:PLUGIN_AGENT_NAME) { $env:PLUGIN_AGENT_NAME } elseif ($env:MCP_AGENT_NAME) { $env:MCP_AGENT_NAME } else { 'Codex' }),
    [switch]$Normalize,
    [ValidateSet('Claude', 'Codex', 'Grok')]
    [string]$TargetProfile,
    [bool]$Recursive = $true,
    [bool]$Strict = $true,
    [switch]$Persist,
    [switch]$NoPersist
)

$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'yaml-object-mutation.ps1')
Import-McpYamlSerializer

function Resolve-DefaultTranscriptProfile {
    param([Parameter(Mandatory)][string]$AgentName)

    switch -Regex ($AgentName) {
        'Claude' { return 'Claude' }
        'Grok' { return 'Grok' }
        default { return 'Codex' }
    }
}


$cacheResolver = Join-Path $PSScriptRoot 'resolve-cache-dir.ps1'
if (Test-Path -LiteralPath $cacheResolver) {
    . $cacheResolver
}

function Get-TranscriptEnvironmentValue {
    param([Parameter(Mandatory)][string[]]$Names)

    foreach ($name in $Names) {
        $value = [Environment]::GetEnvironmentVariable($name)
        if (-not [string]::IsNullOrWhiteSpace($value)) {
            return $value
        }
    }

    return $null
}

function Resolve-ExistingTranscriptPath {
    param([string]$Candidate)

    if ([string]::IsNullOrWhiteSpace($Candidate)) {
        return $null
    }

    try {
        if (Test-Path -LiteralPath $Candidate) {
            return (Resolve-Path -LiteralPath $Candidate).ProviderPath
        }
    } catch {
        return $null
    }

    return $null
}

function Resolve-LatestTranscriptInRoot {
    param([string]$Root)

    $resolvedRoot = Resolve-ExistingTranscriptPath -Candidate $Root
    if (-not $resolvedRoot) {
        return $null
    }

    if (Test-Path -LiteralPath $resolvedRoot -PathType Leaf) {
        return $resolvedRoot
    }

    $latest = Get-ChildItem -LiteralPath $resolvedRoot -File -Recurse -ErrorAction SilentlyContinue |
        Where-Object { $_.Extension -in @('.jsonl', '.json') } |
        Sort-Object LastWriteTimeUtc, FullName -Descending |
        Select-Object -First 1

    if ($latest) {
        return $latest.FullName
    }

    return $null
}

function Resolve-CodexTranscriptPath {
    foreach ($candidate in @(
            (Get-TranscriptEnvironmentValue -Names @('CODEX_SESSION_FILE')),
            (Get-TranscriptEnvironmentValue -Names @('CODEX_ROLLOUT_FILE')))) {
        $resolved = Resolve-ExistingTranscriptPath -Candidate $candidate
        if ($resolved) {
            return $resolved
        }
    }

    if ((Get-Command Resolve-McpCacheDir -ErrorAction SilentlyContinue) -and (Get-Command Read-McpYamlObject -ErrorAction SilentlyContinue)) {
        try {
            $turnPath = Join-Path (Resolve-McpCacheDir) 'current-turn.yaml'
            if (Test-Path -LiteralPath $turnPath -PathType Leaf) {
                $turn = Read-McpYamlObject -Path $turnPath
                foreach ($key in @('codexJsonlPath', 'codexSessionFile', 'codexRolloutFile')) {
                    $resolved = Resolve-ExistingTranscriptPath -Candidate ([string]$turn[$key])
                    if ($resolved) {
                        return $resolved
                    }
                }
            }
        } catch {
            # Continue to session-root discovery.
        }
    }

    $sessionRoot = Get-TranscriptEnvironmentValue -Names @('CODEX_SESSION_DIR')
    if ([string]::IsNullOrWhiteSpace($sessionRoot)) {
        $sessionRoot = Join-Path $HOME '.codex\sessions'
    }

    return Resolve-LatestTranscriptInRoot -Root $sessionRoot
}

function Resolve-ClaudeTranscriptPath {
    foreach ($candidate in @(
            (Get-TranscriptEnvironmentValue -Names @('transcript_path')),
            (Get-TranscriptEnvironmentValue -Names @('CLAUDE_TRANSCRIPT_PATH')),
            (Get-TranscriptEnvironmentValue -Names @('CLAUDE_TRANSCRIPT_FILE')))) {
        $resolved = Resolve-ExistingTranscriptPath -Candidate $candidate
        if ($resolved) {
            return $resolved
        }
    }

    return $null
}

function Resolve-GrokTranscriptPath {
    foreach ($candidate in @(
            (Get-TranscriptEnvironmentValue -Names @('transcript_path')),
            (Get-TranscriptEnvironmentValue -Names @('GROK_TRANSCRIPT_PATH')),
            (Get-TranscriptEnvironmentValue -Names @('MCP_GROK_TRANSCRIPT_PATH')))) {
        $resolved = Resolve-ExistingTranscriptPath -Candidate $candidate
        if ($resolved) {
            return $resolved
        }
    }

    foreach ($root in @(
            (Get-TranscriptEnvironmentValue -Names @('GROK_TRANSCRIPT_ROOT')),
            (Get-TranscriptEnvironmentValue -Names @('MCP_GROK_TRANSCRIPT_ROOT')))) {
        $resolved = Resolve-LatestTranscriptInRoot -Root $root
        if ($resolved) {
            return $resolved
        }
    }

    return $null
}

function Resolve-TranscriptPath {
    param(
        [string]$Path,
        [Parameter(Mandatory)][string]$AgentName,
        [Parameter(Mandatory)][string]$SourceKind
    )

    $explicit = Resolve-ExistingTranscriptPath -Candidate $Path
    if ($explicit) {
        return $explicit
    }

    if (-not [string]::IsNullOrWhiteSpace($Path)) {
        throw "Transcript path '$Path' does not exist or is inaccessible."
    }

    $agent = $AgentName.Trim()
    $source = $SourceKind.Trim()
    $candidates = switch -Regex ($agent) {
        'Claude' { @('Claude'); break }
        'Grok' { @('Grok'); break }
        'Codex' { @('Codex'); break }
        default { @($source, 'Codex') }
    }

    foreach ($candidate in $candidates) {
        $resolved = switch ($candidate) {
            'Claude' { Resolve-ClaudeTranscriptPath }
            'Grok' { Resolve-GrokTranscriptPath }
            'Codex' { Resolve-CodexTranscriptPath }
            default { $null }
        }
        if ($resolved) {
            return $resolved
        }
    }

    throw "No transcript path was provided or discovered for agent '$AgentName'. Pass -Path or configure transcript_path, CODEX_SESSION_FILE, CODEX_ROLLOUT_FILE, CODEX_SESSION_DIR, GROK_TRANSCRIPT_PATH, or GROK_TRANSCRIPT_ROOT."
}

$method = if ($Normalize) { 'repl.sessionlog.normalizeTranscripts' } else { 'repl.sessionlog.ingestTranscripts' }
$profile = if ($TargetProfile) { $TargetProfile } elseif ($Normalize) { Resolve-DefaultTranscriptProfile -AgentName $Agent } else { 'None' }
$resolvedPath = Resolve-TranscriptPath -Path $Path -AgentName $Agent -SourceKind $Source

$params = [ordered]@{
    path = $resolvedPath
    agent = $Agent
    source = $Source
    recursive = $Recursive
    strict = $Strict
}

if ($Normalize) {
    $params['targetProfile'] = $profile
    $params['persist'] = $Persist.IsPresent
} else {
    $params['persist'] = -not $NoPersist.IsPresent
    if ($profile -ne 'None') {
        $params['compatibilityProfile'] = $profile
        $params['emitNormalizedProfile'] = $true
    }
}

$paramsYaml = $params | ConvertTo-Yaml
& (Join-Path $PSScriptRoot 'repl-invoke.ps1') -Method $method -ParamsYaml $paramsYaml
