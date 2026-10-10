<#
.SYNOPSIS
    Copy S7b memory skill/descriptor payloads into a plugin repository.
.DESCRIPTION
    FR-MCP-MEMORY-017 / FR-MCP-MEMORY-018-17..23:
    Copies plugins/core/hosts/<plugin>/SKILL.md and memory-descriptor.json into
    the target plugin repo. Does not require cloud keys. Does not edit YAML by
    line splicing.
.PARAMETER PluginRoot
    Root of the target plugin repository.
.PARAMETER HostName
    Plugin id (claude-code, claude-cowork, cline, cline-v2, codex, copilot, opencode).
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$PluginRoot,
    [Parameter(Mandatory)][string]$HostName
)

$ErrorActionPreference = 'Stop'
$coreRoot = Split-Path -Parent $PSScriptRoot
$hostRoot = Join-Path $coreRoot "hosts/$HostName"
if (-not (Test-Path -LiteralPath $hostRoot)) {
    throw "S7b host payload not found: $hostRoot"
}
if (-not (Test-Path -LiteralPath $PluginRoot)) {
    throw "plugin root not found: $PluginRoot"
}

$skillSource = Join-Path $hostRoot 'SKILL.md'
$descriptorSource = Join-Path $hostRoot 'memory-descriptor.json'
$skillTargetDir = Join-Path $PluginRoot 'skills/memory'
New-Item -ItemType Directory -Force -Path $skillTargetDir | Out-Null
Copy-Item -LiteralPath $skillSource -Destination (Join-Path $skillTargetDir 'SKILL.md') -Force
Copy-Item -LiteralPath $descriptorSource -Destination (Join-Path $PluginRoot 'memory-descriptor.json') -Force

$testsSource = Join-Path $hostRoot 'tests'
if (Test-Path -LiteralPath $testsSource) {
    $testsTarget = Join-Path $PluginRoot 'tests'
    New-Item -ItemType Directory -Force -Path $testsTarget | Out-Null
    Get-ChildItem -LiteralPath $testsSource -File | ForEach-Object {
        Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $testsTarget $_.Name) -Force
    }
}
