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
    [Parameter(Mandatory)][string]$Path,
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

$method = if ($Normalize) { 'repl.sessionlog.normalizeTranscripts' } else { 'repl.sessionlog.ingestTranscripts' }
$profile = if ($TargetProfile) { $TargetProfile } elseif ($Normalize) { Resolve-DefaultTranscriptProfile -AgentName $Agent } else { 'None' }

$params = [ordered]@{
    path = $Path
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