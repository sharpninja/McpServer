#Requires -Version 7.0
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
. (Join-Path $scriptDir 'plugin-env.ps1')
. (Join-Path $scriptDir 'resolve-cache-dir.ps1')

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
)
$requirementMethods = @(
    'workflow.requirements.listLayers'
    'workflow.requirements.createLayer'
    'workflow.requirements.updateLayer'
    'workflow.requirements.effective'
)
$requirementClientMethods = @(
    'client.Requirements.ListRequirementLayersAsync'
    'client.Requirements.CreateRequirementLayerAsync'
    'client.Requirements.UpdateRequirementLayerAsync'
    'client.Requirements.GetEffectiveRequirementsAsync'
)

function Test-McpStatusSessionState {
    param([Parameter(Mandatory)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) { return $false }

    try {
        $statusLine = Select-String -LiteralPath $Path -Pattern '^status:\s*verified\s*$' | Select-Object -First 1
        if (-not $statusLine) { return $false }

        $sessionLine = Select-String -LiteralPath $Path -Pattern '^sessionId:\s*(?<value>.+)$' | Select-Object -First 1
        if (-not $sessionLine) { return $false }

        $sessionId = [string]$sessionLine.Matches[0].Groups['value'].Value
        $sessionId = $sessionId.Trim().Trim('''').Trim('"')
        return -not [string]::IsNullOrWhiteSpace($sessionId)
    } catch {
        return $false
    }
}

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
    pendingCount = if (Test-Path -LiteralPath $pendingDir) { @(Get-ChildItem -LiteralPath $pendingDir -Filter '*.yaml' -File -ErrorAction SilentlyContinue).Count } else { 0 }
}

$status | ConvertTo-Json -Compress
