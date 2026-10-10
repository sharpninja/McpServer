#Requires -Version 7.0
<#
.SYNOPSIS
    Invokes documented workflow.handoff.ingest/get/approve methods from the handoff skill.
.DESCRIPTION
    TEST-HANDOFF-006 / FR-HANDOFF-007 / TR-HANDOFF-SURFACE-001: skill-file invoke that
    deserializes SKILL.md YAML examples into objects, serializes params with ConvertTo-Yaml,
    and dispatches through the plugin REPL seam (MCP_PLUGIN_REPL_LOG when set).
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$SkillPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-HandoffSkillLibRoot {
    param([Parameter(Mandatory)][string]$SkillFile)

    $skillDir = Split-Path -Parent ([System.IO.Path]::GetFullPath($SkillFile))
    $pluginRoot = Split-Path -Parent (Split-Path -Parent $skillDir)
    $candidates = @(
        (Join-Path $pluginRoot 'lib-ps')
        (Join-Path $pluginRoot 'lib')
        (Join-Path $PSScriptRoot '..\..\lib-ps')
        (Join-Path $PSScriptRoot '..\..\lib')
    )
    foreach ($candidate in $candidates) {
        $resolved = [System.IO.Path]::GetFullPath($candidate)
        if (Test-Path -LiteralPath (Join-Path $resolved 'yaml-object-mutation.ps1')) {
            return $resolved
        }
    }

    throw "Handoff skill invoke could not find yaml-object-mutation.ps1 from $SkillFile"
}

function Invoke-HandoffSkillRepl {
    param(
        [Parameter(Mandatory)][string]$Method,
        [Parameter(Mandatory)][string]$LibRoot,
        $Params
    )

    $paramsYaml = if ($null -eq $Params) {
        ''
    } else {
        ConvertTo-Yaml -Data $Params -Options WithIndentedSequences
    }

    if ($env:MCP_PLUGIN_REPL_LOG) {
        $entry = @(
            "method: $Method"
            'params: |'
            (($paramsYaml -replace "`r`n", "`n" -replace "`r", "`n") -split "`n" | ForEach-Object { "  $_" })
            '---'
        ) -join "`n"
        Add-Content -LiteralPath $env:MCP_PLUGIN_REPL_LOG -Value $entry
        if ($env:MCP_PLUGIN_REPL_RESPONSE) {
            Write-Output $env:MCP_PLUGIN_REPL_RESPONSE
        }
        return
    }

    $replInvoke = Join-Path $LibRoot 'repl-invoke.ps1'
    if (-not (Test-Path -LiteralPath $replInvoke)) {
        throw "Handoff skill invoke could not find repl-invoke.ps1 at $replInvoke"
    }

    & $replInvoke -Method $Method -ParamsYaml $paramsYaml
    if ($LASTEXITCODE -ne 0) {
        throw "Handoff skill invoke failed for $Method with exit code $LASTEXITCODE"
    }
}

$resolvedSkill = [System.IO.Path]::GetFullPath($SkillPath)
if (-not (Test-Path -LiteralPath $resolvedSkill)) {
    throw "Handoff SKILL.md was not found: $resolvedSkill"
}

$libRoot = Get-HandoffSkillLibRoot -SkillFile $resolvedSkill
. (Join-Path $libRoot 'yaml-object-mutation.ps1')
Import-McpYamlSerializer

$skillText = [System.IO.File]::ReadAllText($resolvedSkill)
$blocks = [regex]::Matches($skillText, '(?ms)^```ya?ml\r?\n(?<body>.*?)^```')
if ($blocks.Count -eq 0) {
    throw "Handoff SKILL.md has no YAML workflow examples: $resolvedSkill"
}

$invoked = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($block in $blocks) {
    $document = ConvertFrom-Yaml -Yaml $block.Groups['body'].Value -Ordered -ErrorAction Stop
    if ($null -eq $document) {
        continue
    }

    $method = [string]$document['method']
    if ($method -notmatch '^workflow\.handoff\.(ingest|get|approve)$') {
        continue
    }

    Invoke-HandoffSkillRepl -Method $method -LibRoot $libRoot -Params $document['params']
    [void]$invoked.Add($method)
}

foreach ($required in @('workflow.handoff.ingest', 'workflow.handoff.get', 'workflow.handoff.approve')) {
    if (-not $invoked.Contains($required)) {
        throw "Handoff SKILL.md did not yield an object payload for $required"
    }
}
