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

$status = [ordered]@{
    status = if (Test-Path -LiteralPath $sessionFile) { 'available' } else { 'no-session' }
    agent = $env:MCP_AGENT_NAME
    namespaces = $namespaces
    cacheDir = $cacheDir
    hasSession = Test-Path -LiteralPath $sessionFile
    hasCurrentTurn = Test-Path -LiteralPath $turnFile
    pendingCount = if (Test-Path -LiteralPath $pendingDir) { @(Get-ChildItem -LiteralPath $pendingDir -Filter '*.yaml' -File -ErrorAction SilentlyContinue).Count } else { 0 }
}

$status | ConvertTo-Json -Compress
