#Requires -Version 7.0
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
. (Join-Path $scriptDir 'plugin-env.ps1')
. (Join-Path $scriptDir 'resolve-cache-dir.ps1')
. (Join-Path $scriptDir 'yaml-object-mutation.ps1')

$cacheDir = Resolve-McpCacheDir
$sessionFile = Join-Path $cacheDir 'session-state.yaml'
$turnFile = Join-Path $cacheDir 'current-turn.yaml'
$pendingDir = Join-Path $cacheDir 'pending'
$namespaces = @(
    'workflow.sessionlog'
    'workflow.todo'
    'workflow.requirements'
    'workflow.triage'
    'workflow.graphrag'
    'workflow.memory'
    'workflow.failsafe'
)
$requirementMethods = @(
    'workflow.requirements.listLayers'
    'workflow.requirements.createLayer'
    'workflow.requirements.updateLayer'
    'workflow.requirements.effective'
    'workflow.requirements.planRecovery'
    'workflow.requirements.applyRecovery'
    'workflow.requirements.getRecovery'
)
$requirementClientMethods = @(
    'client.Requirements.ListRequirementLayersAsync'
    'client.Requirements.CreateRequirementLayerAsync'
    'client.Requirements.UpdateRequirementLayerAsync'
    'client.Requirements.GetEffectiveRequirementsAsync'
    'client.Requirements.PlanRecoveryAsync'
    'client.Requirements.ApplyRecoveryAsync'
    'client.Requirements.GetRecoveryAsync'
)

function Test-McpStatusSessionState {
    param([Parameter(Mandatory)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) { return $false }

    try {
        $state = Read-McpYamlObject -Path $Path
        return ($state -is [Collections.IDictionary] -and $state.Contains('status') -and
            $state.status -is [string] -and $state.status -ceq 'verified' -and
            $state.Contains('sessionId') -and $state.sessionId -is [string] -and
            -not [string]::IsNullOrWhiteSpace($state.sessionId))
    } catch {
        return $false
    }
}

function Measure-McpStatusYamlFile {
    <#
    .SYNOPSIS
        TR-MCP-REPL-017: counts YAML records directly inside one directory.
    .DESCRIPTION
        Returns 0 for a missing directory. Not recursive, so the quarantine
        subdirectory is never folded into the live queue depth.
    .PARAMETER Path
        Directory to count.
    #>
    param([Parameter(Mandatory)][AllowEmptyString()][string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path)) { return 0 }
    if (-not (Test-Path -LiteralPath $Path -PathType Container)) { return 0 }
    return @(Get-ChildItem -LiteralPath $Path -Filter '*.yaml' -File -ErrorAction SilentlyContinue).Count
}

# TR-MCP-REPL-017 (BUG-TRIAGE-097): the failsafe queue is real pending work. Status
# used to report only the 'pending' directory, so it printed pendingCount 0 while
# captured session submits sat undrained on disk.
$failsafeDir = try { Get-McpFailsafeDir } catch { '' }
$quarantineDir = try { Get-McpFailsafeQuarantineDir } catch { '' }
$pendingTurnCount = Measure-McpStatusYamlFile -Path $pendingDir
$failsafeCount = Measure-McpStatusYamlFile -Path $failsafeDir
$quarantineCount = Measure-McpStatusYamlFile -Path $quarantineDir

$hasSession = Test-McpStatusSessionState -Path $sessionFile
$status = [ordered]@{
    status = if ($hasSession) { 'available' } else { 'no-session' }
    agent = $env:MCP_AGENT_NAME
    namespaces = $namespaces
    requirementMethods = $requirementMethods
    requirementClientMethods = $requirementClientMethods
    cacheDir = $cacheDir
    hasSession = $hasSession
    hasCurrentTurn = Test-Path -LiteralPath $turnFile
    pendingCount = $pendingTurnCount + $failsafeCount
    pendingTurnCount = $pendingTurnCount
    failsafeDir = $failsafeDir
    failsafeCount = $failsafeCount
    failsafeQuarantineCount = $quarantineCount
}

$status | ConvertTo-Json -Compress
