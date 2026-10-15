#!/usr/bin/env -S pwsh -NoLogo -NoProfile -NonInteractive -File
# SPDX-License-Identifier: Apache-2.0
<#
.SYNOPSIS
TEST-MCP-PLUGININT-001 AC-004 (PLAN-SESSIONTRIAGEFIX-001 E6): the aiUnit cli command of the plugin Session Log AI lane.
.DESCRIPTION
aiUnit starts this script as <command> <prompt>: the evaluator's system prompt and the redacted receipt arrive as one argument. The
script asks the Grok Build CLI for the plugin evaluator's own JSON (valid, missingFields and contradictions, nothing else) and writes
grok's structured output to stdout. It sets its exit code on every path: grok's own non-zero code when grok fails, 0 after writing an
answer, 1 when grok gave nothing usable. Linux starts this file directly through its shebang (the file carries its execute bit); Windows
starts it through aiunit-grok-evaluator.cmd.
#>
param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]] $PromptParts
)

$ErrorActionPreference = 'Stop'
$prompt = $PromptParts -join ' '
$schema = [ordered]@{
    type                 = 'object'
    additionalProperties = $false
    required             = @('valid', 'missingFields', 'contradictions')
    properties           = [ordered]@{
        valid          = [ordered]@{ type = 'boolean' }
        missingFields  = [ordered]@{ type = 'array'; items = [ordered]@{ type = 'string' } }
        contradictions = [ordered]@{ type = 'array'; items = [ordered]@{ type = 'string' } }
    }
} | ConvertTo-Json -Depth 5 -Compress

$response = & grok --single $prompt --json-schema $schema
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

$text = ($response | Out-String).Trim()
$envelope = $null
try {
    $envelope = $text | ConvertFrom-Json
}
catch {
    $envelope = $null
}

if ($null -ne $envelope -and $null -ne $envelope.structuredOutput) {
    ConvertTo-Json -InputObject $envelope.structuredOutput -Depth 20 -Compress
    exit 0
}

if ($null -ne $envelope -and -not [string]::IsNullOrWhiteSpace([string]$envelope.text)) {
    [string]$envelope.text
    exit 0
}

if (-not [string]::IsNullOrWhiteSpace($text)) {
    $text
    exit 0
}

[Console]::Error.WriteLine('grok returned no output')
exit 1
